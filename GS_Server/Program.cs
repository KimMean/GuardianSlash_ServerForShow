using System;
using System.Net;
using System.Text;
using System.Threading;
using GS_Server.MySQL;
using GS_Server.Packets;
using GS_Server.Session;
using GS_ServerCore;


namespace GS_Server
{
    
    class Program
    {

        static Listener _Listener = new Listener();

        static void Main(string[] args)
        {
            MySQLManager.Instance.Init();
            MySQLManager.Instance.BeginTransaction();
            InformationPacket.InitInformationData();
            WeaponPacket.InitWeaponData();
            NecklacePacket.InitNecklaceData();
            RingPacket.InitRingData();
            ProductPacket.InitProductData();
            MySQLManager.Instance.CommitTransaction();

            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ip = ipHost.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(ip, private);

            _Listener.Init(endPoint, () => { return new GameSession(); });
            Console.WriteLine("Listening...");

            while (true)
            {
                ;
            }
        }
    }
}