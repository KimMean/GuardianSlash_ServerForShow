using GS_Server.MySQL;
using GS_ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GS_Server.Packets
{
    public struct WeaponData
    {
        public string WeaponCode;
        public string WeaponName;
        public int AttackLevel;
    }

    public struct UserWeaponData
    {
        public string WeaponCode;
        public int Level;
        public int Quantity;
        public int AttackLevel;
    }

    public class WeaponPacket
    {
        static List<WeaponData> WeaponDatas = new List<WeaponData>();
        public static void InitWeaponData()
        {
            Console.WriteLine("InitWeaponData");
            MySQLManager.Instance.GetWeaponData(ref WeaponDatas);
        }

        public ArraySegment<byte> GetWeaponDataPacket()
        {
            ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);

            ushort count = sizeof(ushort);

            // Command 입력
            BitConverter.GetBytes((ushort)Command.Weapon).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            // 결과 입력
            BitConverter.GetBytes((ushort)ResultCommand.Success).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            // 총 개수 입력
            ushort weaponCount = (ushort)WeaponDatas.Count;
            BitConverter.GetBytes(weaponCount).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            for(ushort i = 0; i < weaponCount; i++)
            {
                // weaponData 가져오기
                byte[] code = Encoding.UTF8.GetBytes(WeaponDatas[i].WeaponCode);
                ushort codeSize = (ushort)code.Length;
                byte[] name = Encoding.UTF8.GetBytes(WeaponDatas[i].WeaponName);
                ushort nameSize = (ushort)name.Length;
                ushort atkLevel = (ushort)WeaponDatas[i].AttackLevel;

                BitConverter.GetBytes(codeSize).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(ushort);
                code.CopyTo(openSegment.Array, openSegment.Offset + count);
                count += codeSize;

                BitConverter.GetBytes(nameSize).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(ushort);
                name.CopyTo(openSegment.Array, openSegment.Offset + count);
                count += nameSize;

                BitConverter.GetBytes(atkLevel).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(ushort);
            }

            // 총 사이즈 입력
            BitConverter.GetBytes(count).CopyTo(openSegment.Array, openSegment.Offset);

            return SendBufferHelper.Close(count);
        }

        public ArraySegment<byte> GetUserWeaponData(ArraySegment<byte> buffer)
        {
            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            string token = Encoding.UTF8.GetString(buffer.Array, buffer.Offset + 2, size);

            // DB에 Access Token을 저장 ~
            // AccessToken 반환 ~
            List<UserWeaponData> userData = new List<UserWeaponData>();
            MySQLManager.Instance.BeginTransaction();
            bool result = MySQLManager.Instance.GetUserWeaponData(token, ref userData);


            if (result) MySQLManager.Instance.CommitTransaction();
            else MySQLManager.Instance.RollbackTransaction();

            ResultCommand RC = result ? ResultCommand.Success : ResultCommand.Failed;
            return GetUserWeaponDataPacket(RC, userData);
        }

        ArraySegment<byte> GetUserWeaponDataPacket(ResultCommand resultCommand, List<UserWeaponData> userData)
        {
            ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);

            ushort count = sizeof(ushort);

            // Command 입력
            BitConverter.GetBytes((ushort)Command.UserWeapon).CopyTo(openSegment.Array, openSegment.Offset + count);
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
                    byte[] code = Encoding.UTF8.GetBytes(userData[i].WeaponCode);
                    ushort codeSize = (ushort)code.Length;
                    BitConverter.GetBytes(codeSize).CopyTo(openSegment.Array, openSegment.Offset + count);
                    count += sizeof(ushort);
                    code.CopyTo(openSegment.Array, openSegment.Offset + count);
                    count += codeSize;

                    // 무기 레벨
                    BitConverter.GetBytes((ushort)userData[i].Level).CopyTo(openSegment.Array, openSegment.Offset + count);
                    count += sizeof(ushort);
                    // 무기 개수
                    BitConverter.GetBytes((ushort)userData[i].Quantity).CopyTo(openSegment.Array, openSegment.Offset + count);
                    count += sizeof(ushort);
                    // 무기 공격 레벨
                    BitConverter.GetBytes((ushort)userData[i].AttackLevel).CopyTo(openSegment.Array, openSegment.Offset + count);
                    count += sizeof(ushort);
                }

            }
            // 총 사이즈 입력
            BitConverter.GetBytes(count).CopyTo(openSegment.Array, openSegment.Offset);

            return SendBufferHelper.Close(count);
        }

        public ArraySegment<byte> WeaponEnhancement(ArraySegment<byte> buffer)
        {
            int readCount = 0;

            // token
            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset + readCount);
            readCount += sizeof(ushort);
            string token = Encoding.UTF8.GetString(buffer.Array, buffer.Offset + readCount, size);
            readCount += size;

            // weaponCode
            size = BitConverter.ToUInt16(buffer.Array, buffer.Offset + readCount);
            readCount += sizeof(ushort);
            string itemCode = Encoding.UTF8.GetString(buffer.Array, buffer.Offset + readCount, size);
            readCount += size;

            // upgrade Quantity Required
            ushort quantity = BitConverter.ToUInt16(buffer.Array, buffer.Offset + readCount);
            readCount += sizeof(ushort);

            // upgrade cost
            int cost = BitConverter.ToInt32(buffer.Array, buffer.Offset + readCount);
            readCount += sizeof(int);

            UserWeaponData weaponData = new UserWeaponData();
            weaponData.WeaponCode = itemCode;
            weaponData.Quantity = quantity;
            int coin = 0;


            MySQLManager.Instance.BeginTransaction();
            MySQLManager.Instance.UserWeaponEnhancement(token, cost, ref weaponData, ref coin, out bool result);

            Console.WriteLine($"WeaponEnhancement, User : {token}, Target Item : {itemCode}, Result : {result}");

            if (result) MySQLManager.Instance.CommitTransaction();
            else MySQLManager.Instance.RollbackTransaction();

            ResultCommand rc = result ? ResultCommand.Success : ResultCommand.Failed;
            return GetWeaponEnhancementPacket(rc, weaponData, coin);
        }

        ArraySegment<byte> GetWeaponEnhancementPacket(ResultCommand resultCommand, UserWeaponData weaponData, int coin)
        {
            ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);

            ushort count = sizeof(ushort);

            // Command 입력
            BitConverter.GetBytes((ushort)Command.WeaponEnhancement).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            BitConverter.GetBytes((ushort)resultCommand).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            if(resultCommand == ResultCommand.Success)
            {
                byte[] code = Encoding.UTF8.GetBytes(weaponData.WeaponCode);
                ushort codeSize = (ushort)code.Length;
                BitConverter.GetBytes(codeSize).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(ushort);
                code.CopyTo(openSegment.Array, openSegment.Offset + count);
                count += codeSize;

                BitConverter.GetBytes((ushort)weaponData.Level).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(ushort);
                BitConverter.GetBytes((ushort)weaponData.AttackLevel).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(ushort);
                BitConverter.GetBytes((ushort)weaponData.Quantity).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(ushort);

                BitConverter.GetBytes(coin).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(int);
            }


            // 총 사이즈 입력
            BitConverter.GetBytes(count).CopyTo(openSegment.Array, openSegment.Offset);

            return SendBufferHelper.Close(count);
        }
    }
}
