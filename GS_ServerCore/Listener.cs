using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GS_ServerCore
{
    public class Listener
    {
        Socket ListenSocket;
        Func<Session> SessionFactory;

        const int BACKLOG = 10;     // Listener 최대 대기 클라이언트 수

        // 초기화
        public void Init(IPEndPoint endPoint, Func<Session> sessionFactory)
        {
            
            ListenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            SessionFactory += sessionFactory;

            ListenSocket.Bind(endPoint);
            // backlog : 최대 대기수
            ListenSocket.Listen(BACKLOG);

            SocketAsyncEventArgs socketAsyncEvent = new SocketAsyncEventArgs();
            socketAsyncEvent.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
            RegisterAccept(socketAsyncEvent);
        }

        public void RegisterAccept(SocketAsyncEventArgs args)
        {
            args.AcceptSocket = null;

            bool pending = ListenSocket.AcceptAsync(args);
            if (pending == false)
                OnAcceptCompleted(null, args);
        }

        public void OnAcceptCompleted(object? sender, SocketAsyncEventArgs args)
        {
            if(args.SocketError == SocketError.Success)
            {
                //TODO
                Session session = SessionFactory.Invoke();
                session.Start(args.AcceptSocket);
                session.OnConnected(args.AcceptSocket.RemoteEndPoint);
                //OnAcceptHandler.Invoke(args.AcceptSocket);
            }
            else
                Console.WriteLine(args.SocketError.ToString());

            RegisterAccept(args);
        }


    }
}
