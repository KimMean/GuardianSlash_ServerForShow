using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GS_ServerCore
{
    public class ReceiveBuffer
    {
        // [r][][w][][][][][][][]
        ArraySegment<byte> RecvBuffer;
        int ReadPosition;
        int WritePosition;

        public ReceiveBuffer(int bufferSize)
        {
            RecvBuffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);
        }

        public int DataSize { get { return WritePosition - ReadPosition; } }
        public int FreeSize { get { return RecvBuffer.Count - WritePosition; } }

        public ArraySegment<byte> ReadSegment 
        { 
            get 
            {
                return new ArraySegment<byte>(RecvBuffer.Array, RecvBuffer.Offset + ReadPosition, DataSize);
            } 
        }
        public ArraySegment<byte> WriteSegment
        {
            get
            {
                return new ArraySegment<byte>(RecvBuffer.Array, RecvBuffer.Offset + WritePosition, FreeSize);
            }
        }

        public void Clean()
        {
            int dataSize = DataSize;
            if(dataSize == 0)
            {
                // 남은 데이터가 없음
                ReadPosition = WritePosition = 0;
            }
            else
            {
                Array.Copy(RecvBuffer.Array, RecvBuffer.Offset + ReadPosition, RecvBuffer.Array, RecvBuffer.Offset, dataSize);
                ReadPosition = 0;
                WritePosition = dataSize;
            }
        }

        public bool OnRead(int numOfBytes)
        {
            if (numOfBytes > DataSize)
                return false;

            ReadPosition += numOfBytes;
            return true;
        }

        public bool OnWrite(int numOfBytes)
        {
            if (numOfBytes > FreeSize)
                return false;

            WritePosition += numOfBytes;
            return true;
        }
    }
}
