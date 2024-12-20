using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GS_ServerCore
{
    public abstract class PacketSession : Session
    {
        public static readonly int HEADER_SIZE = 2;
        public sealed override int OnReceive(ArraySegment<byte> buffer)
        {
            // [size(2)][packetID(2)][...][size(2)][packetID(2)][...]
            int processLen = 0;

            while (true)
            {
                // 최소한 헤더는 파싱할 수 있는지 확인
                if (buffer.Count < HEADER_SIZE)
                    break;

                ushort dataSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
                if (buffer.Count < dataSize)
                    break;

                // 패킷 조립 가능
                //Console.WriteLine($"ReceiveData : {buffer.Count}");
                OnReceivePacket(new ArraySegment<byte>(buffer.Array, buffer.Offset, dataSize));

                processLen += dataSize;
                buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + dataSize, buffer.Count - dataSize);
            }

            return processLen;
        }

        public abstract void OnReceivePacket(ArraySegment<byte> buffer);
    }

    public abstract class Session
    {
        Socket _Socket;
        int _Disconnected = 0;

        SocketAsyncEventArgs RecvArgs = new SocketAsyncEventArgs();
        SocketAsyncEventArgs SendArgs = new SocketAsyncEventArgs();

        ReceiveBuffer _ReceiveBuffer = new ReceiveBuffer(2048);

        Queue<ArraySegment<byte>> SendQueue = new Queue<ArraySegment<byte>>();

        List<ArraySegment<byte>> SendPendingList = new List<ArraySegment<byte>>();
        
        object Lock = new object();

        public abstract void OnConnected(EndPoint endPoint);
        public abstract int OnReceive(ArraySegment<byte> buffer);
        public abstract void OnSend(int numOfBytes);
        public abstract void OnDisconnected(EndPoint endPoint);

        public void Start(Socket socket)
        {
            _Socket = socket;

            // Receive
            RecvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceiveCompleted);
            // Send
            SendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

            RegisterReceive();
        }

        public void Send(ArraySegment<byte> sendBuff)
        {
            lock(Lock)
            {
                //_Socket.Send(sendBuff);
                SendQueue.Enqueue(sendBuff);
                if (SendPendingList.Count == 0)
                    RegisterSend();
            }
        }

        public void Disconnect()
        {
            if (Interlocked.Exchange(ref _Disconnected, 1) == 1)
                return;

            OnDisconnected(_Socket.RemoteEndPoint);

            _Socket.Shutdown(SocketShutdown.Both);
            _Socket.Close();
        }

        #region [네트워크 통신]

        void RegisterSend()
        {
            while (SendQueue.Count > 0)
            {
                ArraySegment<byte> buff = SendQueue.Dequeue();
                //SendArgs.SetBuffer(buff, 0, buff.Length);
                SendPendingList.Add(buff);
            }
            SendArgs.BufferList = SendPendingList;

            bool pending = _Socket.SendAsync(SendArgs);
            if (pending == false)
                OnSendCompleted(null, SendArgs);
        }
        void OnSendCompleted(object? sender, SocketAsyncEventArgs args)
        {
            lock (Lock)
            {
                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
                {
                    try
                    {
                        SendArgs.BufferList = null;
                        SendPendingList.Clear();

                        //Console.WriteLine($"Transferred bytes : {SendArgs.BytesTransferred}");

                        OnSend(SendArgs.BytesTransferred);
                        if (SendQueue.Count > 0)
                            RegisterSend();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"OnSendCompleted Failed : {ex}");
                    }
                }
                else
                {
                    //Console.WriteLine($"args.BytesTransferred > 0 && args.SocketError == SocketError.Success");
                    Disconnect();
                }
            }
        }

        void RegisterReceive()
        {
            _ReceiveBuffer.Clean();
            ArraySegment<byte> segment = _ReceiveBuffer.WriteSegment;
            RecvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

            bool pending = _Socket.ReceiveAsync(RecvArgs);
            if (pending == false)
                OnReceiveCompleted(null, RecvArgs);
        }

        void OnReceiveCompleted(object? sender, SocketAsyncEventArgs args)
        {
            if(args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                try
                {
                    // Write Cursor 이동
                    if (_ReceiveBuffer.OnWrite(args.BytesTransferred) == false)
                    {
                        Console.WriteLine("_ReceiveBuffer.OnWrite(args.BytesTransferred) == false");
                        Disconnect();
                        return;
                    }

                    int processLen = OnReceive(_ReceiveBuffer.ReadSegment);
                    if(processLen < 0 || _ReceiveBuffer.DataSize < processLen)
                    {
                        Console.WriteLine("processLen < 0 || _ReceiveBuffer.DataSize < processLen");
                        Disconnect();
                        return;
                    }

                    if(_ReceiveBuffer.OnRead(processLen) == false)
                    {
                        Console.WriteLine("_ReceiveBuffer.OnRead(processLen) == false");
                        Disconnect();
                        return;
                    }

                    //OnReceive(new ArraySegment<byte>(args.Buffer, args.Offset, args.BytesTransferred));
                    //string recvData = Encoding.UTF8.GetString(args.Buffer, args.Offset, args.BytesTransferred);
                    //Console.WriteLine($"[From Client] {recvData}");
                    RegisterReceive();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"OnReceiveCompleted Failed : {ex}");
                }
            }
            else
            {
                //Console.WriteLine("args.BytesTransferred > 0 && args.SocketError == SocketError.Success");
                // TODO Disconnect
                Disconnect();
            }

        }
        #endregion
    }
}
