using GS_Server.MySQL;
using GS_ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GS_Server.Packets
{

    public struct RingData
    {
        public string RingCode;
        public string RingName;
        public int Attack;
        public int Gold;
        public int Jump;
    }
    public class RingPacket
    {
        static List<RingData> RingDatas = new List<RingData>();
        public static void InitRingData()
        {
            Console.WriteLine("InitRingData");
            MySQLManager.Instance.GetRingData(ref RingDatas);
        }

        public ArraySegment<byte> GetRingDataPacket()
        {
            ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);

            ushort count = sizeof(ushort);

            // Command 입력
            BitConverter.GetBytes((ushort)Command.Ring).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            // 결과 입력
            BitConverter.GetBytes((ushort)ResultCommand.Success).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            // 총 개수 입력
            ushort ringCount = (ushort)RingDatas.Count;
            BitConverter.GetBytes(ringCount).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            for (ushort i = 0; i < ringCount; i++)
            {
                // ringData 가져오기
                byte[] code = Encoding.UTF8.GetBytes(RingDatas[i].RingCode);
                ushort codeSize = (ushort)code.Length;
                byte[] name = Encoding.UTF8.GetBytes(RingDatas[i].RingName);
                ushort nameSize = (ushort)name.Length;
                ushort attack = (ushort)RingDatas[i].Attack;
                ushort gold = (ushort)RingDatas[i].Gold;
                ushort jump = (ushort)RingDatas[i].Jump;

                BitConverter.GetBytes(codeSize).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(ushort);
                code.CopyTo(openSegment.Array, openSegment.Offset + count);
                count += codeSize;

                BitConverter.GetBytes(nameSize).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(ushort);
                name.CopyTo(openSegment.Array, openSegment.Offset + count);
                count += nameSize;

                BitConverter.GetBytes(attack).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(ushort);
                BitConverter.GetBytes(gold).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(ushort);
                BitConverter.GetBytes(jump).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(ushort);
            }

            // 총 사이즈 입력
            BitConverter.GetBytes(count).CopyTo(openSegment.Array, openSegment.Offset);

            return SendBufferHelper.Close(count);
        }

        public ArraySegment<byte> GetUserRingData(ArraySegment<byte> buffer)
        {
            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            string token = Encoding.UTF8.GetString(buffer.Array, buffer.Offset + 2, size);

            // DB에 Access Token을 저장 ~
            // AccessToken 반환 ~
            List<string> userData = new List<string>();

            MySQLManager.Instance.BeginTransaction();
            bool result = MySQLManager.Instance.GetUserRingData(token, ref userData);


            if (result) MySQLManager.Instance.CommitTransaction();
            else MySQLManager.Instance.RollbackTransaction();

            ResultCommand RC = result ? ResultCommand.Success : ResultCommand.Failed;
            return GetUserRingDataPacket(RC, userData);
        }
        ArraySegment<byte> GetUserRingDataPacket(ResultCommand resultCommand, List<string> userData)
        {
            ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);

            ushort count = sizeof(ushort);

            // Command 입력
            BitConverter.GetBytes((ushort)Command.UserRing).CopyTo(openSegment.Array, openSegment.Offset + count);
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
