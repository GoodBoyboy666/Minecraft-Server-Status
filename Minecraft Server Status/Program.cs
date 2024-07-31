using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Minecraft_Server_Status
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            List<byte> handshake_packet = new List<byte>();
            string address = "mc.goodboyboy.top";
            byte[] byte_address = Encoding.UTF8.GetBytes(address);

            ushort port = 25565;
            byte[] byte_port;
            //实际抓包发现port使用大端排序
            if (!BitConverter.IsLittleEndian)
            {
                byte_port = BitConverter.GetBytes(port);
            }
            else
            {
                byte_port = BitConverter.GetBytes(port).Reverse().ToArray();
            }

            int protocol_version = 766;
            byte[] varint_protocol_version = VarInt.ToVarInt(protocol_version);

            int next_state = 1;
            byte[] varint_next_state =VarInt.ToVarInt(next_state);

            byte[] length_byte_address=VarInt.ToVarInt(byte_address.Length);

            //组建handshake packet
            handshake_packet.Add(0x00);
            handshake_packet.AddRange(varint_protocol_version);
            handshake_packet.AddRange(length_byte_address);
            handshake_packet.AddRange(byte_address);
            handshake_packet.AddRange(byte_port);
            handshake_packet.AddRange(varint_next_state);

            int packet_length=handshake_packet.Count;
            handshake_packet.InsertRange(0, VarInt.ToVarInt(packet_length));

            byte[] request_packet = { 0x01,0x00 };

            //初始化Soket
            IPAddress ip = Dns.GetHostAddresses(address)[0];
            Socket socket;
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            }
            else if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }
            else
            {
                throw new Exception("Socket为Null");
            }

            IPEndPoint endpoint = new IPEndPoint(ip, port);

            try
            {
                //连接服务器并发送HandSake与Request包
                socket.Connect(endpoint);
                socket.Send(handshake_packet.ToArray());
                socket.Send(request_packet);

                //获取包长度
                List<byte> byte_package_len=new List<byte>();
                bool is_timeout = false;
                System.Timers.Timer timer = new System.Timers.Timer(30000);
                timer.Elapsed += (sender, e) => { is_timeout = true; };
                timer.Enabled = true;
                timer.Start();
                while (true)
                {
                    byte[] temp = new byte[1];
                    socket.Receive(temp, 1, 0);
                    if (VarInt.IsEnd(temp[0]))
                    {
                        byte_package_len.Add(temp[0]);
                        break;
                    }
                    else
                    {
                        byte_package_len.Add(temp[0]);
                    }

                    if(is_timeout)
                    {
                        throw new Exception("包长度获取超时！");
                    }
                }
                timer.Stop();
                timer.Enabled=false;
                is_timeout=false;
                int package_len = VarInt.ToInt(byte_package_len.ToArray());

                //检查包ID
                byte[] byte_packet_id=new byte[1];
                socket.Receive(byte_packet_id, 1, 0);
                if(byte_packet_id[0]!=0x00)
                {
                    throw new Exception("包ID错误！");
                }

                //获取包的有效数据长度
                List<byte> byte_package_data_len = new List<byte>();
                timer.Enabled = true;
                timer.Start();
                while (true)
                {
                    byte[] temp = new byte[1];
                    socket.Receive(temp, 1, 0);
                    if (VarInt.IsEnd(temp[0]))
                    {
                        byte_package_data_len.Add(temp[0]);
                        break;
                    }
                    else
                    {
                        byte_package_data_len.Add(temp[0]);
                    }

                    if (is_timeout)
                    {
                        throw new Exception("包长度获取超时！");
                    }
                }
                timer.Stop();
                timer.Enabled = false;
                is_timeout = false;
                int package_data_len = VarInt.ToInt(byte_package_data_len.ToArray());

                //获取数据内容
                List<byte> byte_data=new List<byte>();
                timer.Enabled = true;
                timer.Start();
                for (int i= 0; i<package_data_len;i=byte_data.Count)
                {
                    byte[] respond_pack = new byte[2048];
                    int respond_count=socket.Receive(respond_pack, 2048, 0);
                    byte[] temp=new byte[respond_count];
                    Buffer.BlockCopy(respond_pack,0,temp,0,respond_count);
                    byte_data.AddRange(temp);
                }
                timer.Stop();
                timer.Enabled = false;
                is_timeout = false;

                //测试延迟
                byte[] ping_packet = { 0x09, 0x01,0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                Stopwatch stopwatch = new Stopwatch();
                List<byte> pong_packet = new List<byte>();
                int byte_pong_count = 0;
                timer.Enabled = true;
                timer.Start();
                socket.Send(ping_packet);
                stopwatch.Start();
                for (int i=0;i<10;)
                {
                    byte[] temp_pong = new byte[10];
                    byte_pong_count=socket.Receive(temp_pong, 10, 0);
                    i += byte_pong_count;
                    byte[] temp = new byte[byte_pong_count];
                    Buffer.BlockCopy(temp_pong, 0, temp, 0, byte_pong_count);
                    pong_packet.AddRange(temp);
                    if(is_timeout)
                    {
                        break;
                    }
                }
                stopwatch.Stop();
                timer.Stop();
                timer.Enabled = false;
                is_timeout=false;
                if (!ping_packet.SequenceEqual(pong_packet))
                {
                    //throw new Exception("延迟测试异常");
                    Console.WriteLine("延迟测试异常");
                }

                socket.Close();

                string data=Encoding.UTF8.GetString(byte_data.ToArray());
                Console.WriteLine(data);
                Console.WriteLine("延迟：{0}", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            
        }
    }
}
