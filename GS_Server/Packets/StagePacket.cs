using GS_Server.MySQL;
using GS_ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GS_Server.Packets
{
    public class StagePacket
    {
        public ArraySegment<byte> GetUserClearStage(ArraySegment<byte> buffer)
        {
            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            string token = Encoding.UTF8.GetString(buffer.Array, buffer.Offset + 2, size);

            MySQLManager.Instance.BeginTransaction();
            bool result = MySQLManager.Instance.GetUserClearStage(token, out uint clearStage);
            Console.WriteLine($"User : {token}, Clear Stage : {clearStage}");

            if (result) MySQLManager.Instance.CommitTransaction();
            else MySQLManager.Instance.RollbackTransaction();

            ResultCommand RC = result ? ResultCommand.Success : ResultCommand.Failed;
            return GetUserClearStagePacket(RC, clearStage);
        }

        ArraySegment<byte> GetUserClearStagePacket(ResultCommand resultCommand, uint clearStage)
        {
            ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);

            ushort count = sizeof(ushort);

            // Command 입력
            BitConverter.GetBytes((ushort)Command.ClearStage).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            BitConverter.GetBytes((ushort)resultCommand).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            if(resultCommand == ResultCommand.Success)
            {
                BitConverter.GetBytes(clearStage).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(uint);
            }

            // 총 사이즈 입력
            BitConverter.GetBytes(count).CopyTo(openSegment.Array, openSegment.Offset);

            return SendBufferHelper.Close(count);
        }

        public ArraySegment<byte> GetEndGameResult(ArraySegment<byte> buffer)
        {
            int readCount = 0;

            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset + readCount);
            readCount += sizeof(ushort);

            string token = Encoding.UTF8.GetString(buffer.Array, buffer.Offset + readCount, size);
            readCount += size;

            GameState state = (GameState)BitConverter.ToUInt16(buffer.Array, buffer.Offset + readCount);
            readCount += sizeof(ushort);

            ushort stage = BitConverter.ToUInt16(buffer.Array, buffer.Offset + readCount);
            readCount += sizeof(ushort);

            Products productCoin = (Products)BitConverter.ToUInt16(buffer.Array, buffer.Offset + readCount);
            readCount += sizeof(ushort);
            int coin = BitConverter.ToInt32(buffer.Array, buffer.Offset + readCount);
            readCount += sizeof(int);

            Products productDia = (Products)BitConverter.ToUInt16(buffer.Array, buffer.Offset + readCount);
            readCount += sizeof(ushort);
            int dia = BitConverter.ToInt32(buffer.Array, buffer.Offset + readCount);
            readCount += sizeof(int);

            if(state == GameState.GameClear) Console.WriteLine($"Game Clear, Stage : {stage}");
            else Console.WriteLine($"Game Over, Stage : {stage}");

            bool result = false;
            bool gameResult = (int)state == 1 ? true : false;


            MySQLManager.Instance.BeginTransaction();
            MySQLManager.Instance.EndGame(token, stage, gameResult, ref coin, ref dia, out result);

            if (result) MySQLManager.Instance.CommitTransaction();
            else MySQLManager.Instance.RollbackTransaction();

            ResultCommand RC = result ? ResultCommand.Success : ResultCommand.Failed;
            return GetGameResultPacket(RC, gameResult, stage, coin, dia);
        }

        ArraySegment<byte> GetGameResultPacket(ResultCommand resultCommand, bool gameResult, int stage, int coin, int dia )
        {
            ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);

            ushort count = sizeof(ushort);

            // Command 입력
            BitConverter.GetBytes((ushort)Command.EndGame).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            BitConverter.GetBytes((ushort)resultCommand).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            if (resultCommand == ResultCommand.Success)
            {
                BitConverter.GetBytes(gameResult).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(bool);

                BitConverter.GetBytes((ushort)stage).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(ushort);
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
