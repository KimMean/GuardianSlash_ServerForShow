using GS_Server.MySQL;
using GS_ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GS_Server.Packets
{
    public struct NecklaceData
    {
        public string NecklaceCode;
        public string NecklaceName;
        public int Twilight;
        public int Void;
        public int Hell;
    }
    public class NecklacePacket
    {
        static List<NecklaceData> NecklaceDatas = new List<NecklaceData>();
        public static void InitNecklaceData()
        {
            Console.WriteLine("InitNecklaceData");
            MySQLManager.Instance.GetNecklaceData(ref NecklaceDatas);
        }

        public ArraySegment<byte> GetNecklaceDataPacket()
        {
            ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);

            ushort count = sizeof(ushort);

            // Command 입력
            BitConverter.GetBytes((ushort)Command.Necklace).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            // 결과 입력
            BitConverter.GetBytes((ushort)ResultCommand.Success).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            // 총 개수 입력
            ushort necklaceCount = (ushort)NecklaceDatas.Count;
            BitConverter.GetBytes(necklaceCount).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            for (ushort i = 0; i < necklaceCount; i++)
            {
                // necklaceData 가져오기
                byte[] code = Encoding.UTF8.GetBytes(NecklaceDatas[i].NecklaceCode);
                ushort codeSize = (ushort)code.Length;
                byte[] name = Encoding.UTF8.GetBytes(NecklaceDatas[i].NecklaceName);
                ushort nameSize = (ushort)name.Length;
                ushort twilight = (ushort)NecklaceDatas[i].Twilight;
                ushort varVoid = (ushort)NecklaceDatas[i].Void;
                ushort hell = (ushort)NecklaceDatas[i].Hell;

                BitConverter.GetBytes(codeSize).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(ushort);
                code.CopyTo(openSegment.Array, openSegment.Offset + count);
                count += codeSize;

                BitConverter.GetBytes(nameSize).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(ushort);
                name.CopyTo(openSegment.Array, openSegment.Offset + count);
                count += nameSize;

                BitConverter.GetBytes(twilight).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(ushort);
                BitConverter.GetBytes(varVoid).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(ushort);
                BitConverter.GetBytes(hell).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(ushort);
            }

            // 총 사이즈 입력
            BitConverter.GetBytes(count).CopyTo(openSegment.Array, openSegment.Offset);

            return SendBufferHelper.Close(count);
        }

        public ArraySegment<byte> GetUserNecklaceData(ArraySegment<byte> buffer)
        {
            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            string token = Encoding.UTF8.GetString(buffer.Array, buffer.Offset + 2, size);

            // DB에 Access Token을 저장 ~
            // AccessToken 반환 ~
            List<string> userData = new List<string>();
            MySQLManager.Instance.BeginTransaction();
            bool result = MySQLManager.Instance.GetUserNecklaceData(token, ref userData);


            if (result) MySQLManager.Instance.CommitTransaction();
            else MySQLManager.Instance.RollbackTransaction();

            ResultCommand RC = result ? ResultCommand.Success : ResultCommand.Failed;
            return GetUserNecklaceDataPacket(RC, userData);
        }

        ArraySegment<byte> GetUserNecklaceDataPacket(ResultCommand resultCommand, List<string> userData)
        {
            ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);

            ushort count = sizeof(ushort);

            // Command 입력
            BitConverter.GetBytes((ushort)Command.UserNecklace).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            BitConverter.GetBytes((ushort)resultCommand).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            if(resultCommand == ResultCommand.Success)
            {
                // 데이터 개수
                BitConverter.GetBytes((ushort)userData.Count).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(ushort);

                for (int i = 0; i < userData.Count; i++)
                {
                    // 무기 코드
                    byte[] code = Encoding.UTF8.GetBytes(userData[i]);
                    ushort codeSize = (ushort)code.Length;
                    BitConverter.GetBytes(codeSize).CopyTo(openSegment.Array, openSegment.Offset + count);
                    count += sizeof(ushort);
                    code.CopyTo(openSegment.Array, openSegment.Offset + count);
                    count += codeSize;
                }
            }

            // 총 사이즈 입력
            BitConverter.GetBytes(count).CopyTo(openSegment.Array, openSegment.Offset);

            return SendBufferHelper.Close(count);
        }

    }
}
