using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Minecraft_Server_Status
{
    /// <summary>
    /// 无符号Int与VarInt转换
    /// </summary>
    internal class VarInt
    {
        /// <summary>
        /// Int to VarInt
        /// </summary>
        /// <param name="source">源数字</param>
        /// <returns>VarInt字节</returns>
        public static byte[] ToVarInt(int source) 
        {
            List<byte> list = new List<byte>();
            while(source>=0x80)
            {
                byte temp;
                if (BitConverter.IsLittleEndian)
                {
                    temp = BitConverter.GetBytes(source%0x80)[0];
                }
                else
                {
                    temp = BitConverter.GetBytes(source % 0x80).Reverse().ToArray()[0];
                }
                temp += 0x80;
                list.Add(temp);
                source /= 0x80;
            }
            byte source_byte;
            if (BitConverter.IsLittleEndian)
            {
                source_byte = BitConverter.GetBytes(source % 0x80)[0];
            }
            else
            {
                source_byte = BitConverter.GetBytes(source % 0x80).Reverse().ToArray()[0];
            }
            list.Add(source_byte);
            return list.ToArray();
        }

        public static int ToInt(byte[] source) 
        {
            int reslut = 0;
            for(int i =source.Length-1;i>0;i--)
            {
                byte temp = source[i];
                if(temp>=0x80)
                {
                    temp-= 0x80;
                }
                reslut += temp;
                reslut *= 0x80;
            }
            byte the_last=source[0];
            the_last -= 0x80;
            reslut += the_last;
            return reslut;
        }

        public static bool IsEnd(byte value)
        {
            if(value>=0x80)
            {
                return false;
            }    
            else
            {
                return true;
            }
        }
    }
}
