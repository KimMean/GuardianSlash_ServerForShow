using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GS_Server.MySQL;
using GS_ServerCore;

namespace GS_Server.Packets
{
    public struct AppInformation
    {
        public string platform;
        public string version;
        public string url;
    }
    public class InformationPacket
    {
        static Dictionary<string, AppInformation> appInformations ;

        public static void InitInformationData()
        {
            appInformations = new Dictionary<string, AppInformation>();
            Console.WriteLine("InitInformationData");
            MySQLManager.Instance.GetInformationData(ref appInformations);
        }

        public ArraySegment<byte> GetInformationDataPacket(ArraySegment<byte> buffer)
        {
            // 플랫폼 데이터를 전달 받습니다.
            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            string platform = Encoding.UTF8.GetString(buffer.Array, buffer.Offset + 2, size);


            ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);

            ushort count = sizeof(ushort);

            // Command 입력
            BitConverter.GetBytes((ushort)Command.Information).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            // 결과 입력
            BitConverter.GetBytes((ushort)ResultCommand.Success).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            // Version 가져오기
            byte[] version = Encoding.UTF8.GetBytes(appInformations[platform].version);
            ushort versionSize = (ushort)version.Length;

            // URL 가져오기
            byte[] url = Encoding.UTF8.GetBytes(appInformations[platform].url);
            ushort urlSize = (ushort)url.Length;

            BitConverter.GetBytes(versionSize).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);
            version.CopyTo(openSegment.Array, openSegment.Offset + count);
            count += versionSize;

            BitConverter.GetBytes(urlSize).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);
            url.CopyTo(openSegment.Array, openSegment.Offset + count);
            count += urlSize;

            // 총 사이즈 입력
            BitConverter.GetBytes(count).CopyTo(openSegment.Array, openSegment.Offset);

            return SendBufferHelper.Close(count);
        }
    }
}
