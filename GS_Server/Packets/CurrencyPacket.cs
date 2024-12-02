using GS_Server.MySQL;
using GS_ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GS_Server.Packets
{
    public class CurrencyPacket
    {
        public ArraySegment<byte> GetUserCurrency(ArraySegment<byte> buffer)
        {
            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            string token = Encoding.UTF8.GetString(buffer.Array, buffer.Offset + 2, size);

            //Console.WriteLine($"ReceivePacket Size : {size}, User Token : {token}");

            // DB에 Access Token을 저장 ~
            // AccessToken 반환 ~
            int coin = 0;
            int dia = 0;
            MySQLManager.Instance.BeginTransaction();
            bool result = MySQLManager.Instance.GetUserCurrency(token, out coin, out dia);
            Console.WriteLine($"User Currency, Coin : {coin}, Diamond : {dia}");

            if (result) MySQLManager.Instance.CommitTransaction();
            else MySQLManager.Instance.RollbackTransaction();

            ResultCommand RC = result ? ResultCommand.Success : ResultCommand.Failed;
            return GetUserCurrencyPacket(RC, coin, dia);
        }

        ArraySegment<byte> GetUserCurrencyPacket(ResultCommand resultCommand, int coin, int dia)
        {
            ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);

            ushort count = sizeof(ushort);

            // Command 입력
            BitConverter.GetBytes((ushort)Command.Currency).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            BitConverter.GetBytes((ushort)resultCommand).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            if(resultCommand == ResultCommand.Success)
            {
                BitConverter.GetBytes(coin).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(int);
                BitConverter.GetBytes(dia).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(int);
            }
            
            // 총 사이즈 입력
            BitConverter.GetBytes(count).CopyTo(openSegment.Array, openSegment.Offset);

            return SendBufferHelper.Close(count);
        }
    }

}
