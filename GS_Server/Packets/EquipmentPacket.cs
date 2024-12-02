using GS_Server.MySQL;
using GS_ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GS_Server.Packets
{
    public class EquipmentPacket
    {
        public ArraySegment<byte> GetUserEquipment(ArraySegment<byte> buffer)
        {
            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            string token = Encoding.UTF8.GetString(buffer.Array, buffer.Offset + 2, size);

            MySQLManager.Instance.BeginTransaction();
            bool result = MySQLManager.Instance.GetUserEquipment(token, out string weaponCode, out string necklaceCode, out string ringCode);

            if (result) MySQLManager.Instance.CommitTransaction();
            else MySQLManager.Instance.RollbackTransaction();

            ResultCommand RC = result ? ResultCommand.Success : ResultCommand.Failed;
            return GetUserEquipmentPacket(RC, weaponCode, necklaceCode, ringCode);
        }

        ArraySegment<byte> GetUserEquipmentPacket(ResultCommand resultCommand, string weapon, string necklace, string ring)
        {
            ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);

            ushort count = sizeof(ushort);

            // Command 입력
            BitConverter.GetBytes((ushort)Command.Equipment).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            BitConverter.GetBytes((ushort)resultCommand).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            if(resultCommand == ResultCommand.Success)
            {
                byte[] weaponCode = Encoding.UTF8.GetBytes(weapon);
                ushort weaponCodeSize = (ushort)weaponCode.Length;
                byte[] necklaceCode = Encoding.UTF8.GetBytes(necklace);
                ushort necklaceCodeSize = (ushort)necklaceCode.Length;
                byte[] ringCode = Encoding.UTF8.GetBytes(ring);
                ushort ringCodeSize = (ushort)ringCode.Length;

                BitConverter.GetBytes(weaponCodeSize).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(ushort);
                weaponCode.CopyTo(openSegment.Array, openSegment.Offset + count);
                count += weaponCodeSize;

                BitConverter.GetBytes(necklaceCodeSize).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(ushort);
                necklaceCode.CopyTo(openSegment.Array, openSegment.Offset + count);
                count += necklaceCodeSize;

                BitConverter.GetBytes(ringCodeSize).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(ushort);
                ringCode.CopyTo(openSegment.Array, openSegment.Offset + count);
                count += ringCodeSize;

                Console.WriteLine($"User Equipment Information, Weapon : {weapon}, Necklace : {necklace}, Ring : {ring}, ");
            }

            // 총 사이즈 입력
            BitConverter.GetBytes(count).CopyTo(openSegment.Array, openSegment.Offset);

            return SendBufferHelper.Close(count);
        }

        public ArraySegment<byte> ChangeUserEquipment(ArraySegment<byte> buffer)
        {
            int readCount = 0;

            // token
            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset + readCount);
            readCount += sizeof(ushort);
            string token = Encoding.UTF8.GetString(buffer.Array, buffer.Offset + readCount, size);
            readCount += size;

            // Weapon, Necklace, Ring
            Products product = (Products)BitConverter.ToUInt16(buffer.Array, buffer.Offset + readCount);
            readCount += sizeof(ushort);

            // itemCode
            ushort codeSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset + readCount);
            readCount += sizeof(ushort);
            string itemCode = Encoding.UTF8.GetString(buffer.Array, buffer.Offset + readCount, codeSize);
            readCount += codeSize;

            Console.WriteLine($"Change Equipment Item : {itemCode}");

            MySQLManager.Instance.BeginTransaction();
            MySQLManager.Instance.ChangeUserEquipment(token, product, itemCode, out bool result);

            if (result) MySQLManager.Instance.CommitTransaction();
            else MySQLManager.Instance.RollbackTransaction();

            ResultCommand RC = result ? ResultCommand.Success : ResultCommand.Failed;
            return GetChangeUserEquipmentPacket(RC, product, itemCode);
        }

        ArraySegment<byte> GetChangeUserEquipmentPacket(ResultCommand resultCommand, Products product, string itemCode)
        {
            ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);

            ushort count = sizeof(ushort);

            // Command 입력
            BitConverter.GetBytes((ushort)Command.ChangeEquipment).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            BitConverter.GetBytes((ushort)resultCommand).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            if(resultCommand == ResultCommand.Success)
            {
                BitConverter.GetBytes((ushort)product).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(ushort);

                byte[] code = Encoding.UTF8.GetBytes(itemCode);
                ushort codeSize = (ushort)code.Length;

                BitConverter.GetBytes(codeSize).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(ushort);
                code.CopyTo(openSegment.Array, openSegment.Offset + count);
                count += codeSize;
            }

            // 총 사이즈 입력
            BitConverter.GetBytes(count).CopyTo(openSegment.Array, openSegment.Offset);

            return SendBufferHelper.Close(count);
        }
    }
}
