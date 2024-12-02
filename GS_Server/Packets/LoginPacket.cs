using GS_Server.MySQL;
using GS_ServerCore;
using MySqlX.XDevAPI.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GS_Server.Packets
{
    public class LoginPacket
    {
        static ushort TokenIndex = 0;

        #region [GuestRegistration]
        /*
         * 게스트 회원가입
         */
        public ArraySegment<byte> GuestRegistration()
        {
            // id 랜덤 생성
            string uuid = Guid.NewGuid().ToString();
            Console.WriteLine($"Guest Registration : {uuid}");

            // DB 등록
            MySQLManager.Instance.BeginTransaction();
            bool result = MySQLManager.Instance.Registration(Provider.GUEST, uuid);
            
            if (result) MySQLManager.Instance.CommitTransaction();
            else MySQLManager.Instance.RollbackTransaction();

            ResultCommand RC = result ? ResultCommand.Success : ResultCommand.Failed;
            return GetRegistrationPacket(Command.GuestSignUP, RC, uuid);
        }

        /*
         * 구글 회원가입
         */
        public ArraySegment<byte> GoogleRegistration(ArraySegment<byte> buffer)
        {
            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            string userId = Encoding.UTF8.GetString(buffer.Array, buffer.Offset + 2, size);

            Console.WriteLine($"Google Registration, UserID : {userId}");
            // DB 등록
            MySQLManager.Instance.BeginTransaction();
            bool result = MySQLManager.Instance.Registration(Provider.GOOGLE, userId);

            if (result) MySQLManager.Instance.CommitTransaction();
            else MySQLManager.Instance.RollbackTransaction();

            ResultCommand RC = result ? ResultCommand.Success : ResultCommand.Failed;
            return GetRegistrationPacket(Command.GoogleSignUP, RC, userId);
        }
        // 회원 가입 결과 패킷
        ArraySegment<byte> GetRegistrationPacket(Command command, ResultCommand resultCommand, string userid = null)
        {
            ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);

            ushort count = sizeof(ushort);

            // Command 입력
            BitConverter.GetBytes((ushort)command).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            BitConverter.GetBytes((ushort)resultCommand).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            if (resultCommand == ResultCommand.Success)
            {
                // ID 입력
                byte[] id = Encoding.UTF8.GetBytes(userid);
                ushort idSize = (ushort)userid.Length;

                BitConverter.GetBytes(idSize).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(ushort);
                id.CopyTo(openSegment.Array, openSegment.Offset + count);
                count += idSize;
            }

            // 총 사이즈 입력
            BitConverter.GetBytes(count).CopyTo(openSegment.Array, openSegment.Offset);

            return SendBufferHelper.Close(count);
        }

        
        #endregion
       
        /*
        * 로그인
        */
        public ArraySegment<byte> Login(Provider provider, ArraySegment<byte> buffer)
        {
            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            string id = Encoding.UTF8.GetString(buffer.Array, buffer.Offset + 2, size);

            string accessToken = AccessTokenGeneration();

            // DB에 Access Token을 저장 ~
            // AccessToken 반환 ~
            Command cmd = Command.GuestLogin;
            switch(provider)
            {
                case Provider.GUEST : 
                    cmd = Command.GuestLogin;
                    Console.WriteLine($"Guest Login AccessToken : {accessToken}");
                    break;
                case Provider.GOOGLE:
                    cmd = Command.GoogleLogin;
                    Console.WriteLine($"Google Login AccessToken : {accessToken}");
                    break;
            }

            MySQLManager.Instance.BeginTransaction();
            bool result = MySQLManager.Instance.Login(provider, id, accessToken);

            if (result) MySQLManager.Instance.CommitTransaction();
            else MySQLManager.Instance.RollbackTransaction();

            ResultCommand RC = result ? ResultCommand.Success : ResultCommand.Failed;
            return GetLoginResultPacket(cmd, RC, accessToken);
            //return GetLoginResultPacket(cmd, ResultCommand.Failed);
        }

        // 로그인 결과 패킷
        ArraySegment<byte> GetLoginResultPacket(Command command, ResultCommand result, string accessToken = null)
        {
            ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);

            ushort count = sizeof(ushort);

            // Command 입력
            BitConverter.GetBytes((ushort)command).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            BitConverter.GetBytes((ushort)result).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            if(result == ResultCommand.Success)
            {
                // UUID 입력
                byte[] token = Encoding.UTF8.GetBytes(accessToken);
                ushort tokenSize = (ushort)accessToken.Length;

                BitConverter.GetBytes(tokenSize).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(ushort);
                token.CopyTo(openSegment.Array, openSegment.Offset + count);
                count += tokenSize;
            }

            // 총 사이즈 입력
            BitConverter.GetBytes(count).CopyTo(openSegment.Array, openSegment.Offset);

            return SendBufferHelper.Close(count);
        }

        /*
         * AccessToken을 생성합니다.
         * UnixTime을 16진수로 바꾸고 중복을 막기 위해 16진수 4자리의 인덱스를 추가합니다.
         * 
         */
        string AccessTokenGeneration()
        {
            long unixTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            string accessToken = unixTime.ToString("X8");
            accessToken += TokenIndex.ToString("X4");  // 16진수로 변환하고 4자리 문자열로 포맷팅

            if (TokenIndex == ushort.MaxValue)
                TokenIndex = 0;
            else
                TokenIndex++;

            return accessToken;
        }
    }
}
