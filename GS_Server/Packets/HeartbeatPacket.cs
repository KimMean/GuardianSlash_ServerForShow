using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GS_ServerCore;

namespace GS_Server.Packets
{
    public class HeartbeatPacket
    {

        public ArraySegment<byte> GetHeartbeatPacket()
        {
            ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
            ushort count = sizeof(ushort);

            BitConverter.GetBytes((ushort)Command.Heartbeat).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            // 총 사이즈 입력
            BitConverter.GetBytes(count).CopyTo(openSegment.Array, openSegment.Offset);

            return SendBufferHelper.Close(count);
        }
    }
}
