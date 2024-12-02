﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using GS_ServerCore;
using GS_Server.Packets;
using GS_Server.MySQL;

namespace GS_Server.Session
{
    class GameSession : PacketSession
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected {endPoint}");

            //byte[] sendBuff = Encoding.UTF8.GetBytes("Welcome to Game Server ! ");
            //Send(sendBuff);

            //Thread.Sleep(5000);
            //Disconnect();
        }

        public override void OnReceivePacket(ArraySegment<byte> buffer)
        {
            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);   // Unused
            ushort cmd = BitConverter.ToUInt16(buffer.Array, buffer.Offset+2);

            Console.WriteLine($"Command : {cmd}");
            Command command = (Command)cmd;
            switch (command)
            {
                case Command.NONE: break;
                case Command.GuestSignUP:
                    {
                        GuestSignUp();
                        break;
                    }
                case Command.GoogleSignUP:
                    {
                        GoogleSignUp(new ArraySegment<byte>(buffer.Array, buffer.Offset + 4, buffer.Count - 4));
                        break;
                    }
                case Command.GuestLogin:
                    {
                        GuestLogin(new ArraySegment<byte>(buffer.Array, buffer.Offset + 4, buffer.Count - 4));
                        break;
                    }
                case Command.GoogleLogin:
                    {
                        GoogleLogin(new ArraySegment<byte>(buffer.Array, buffer.Offset + 4, buffer.Count - 4));
                        break;
                    }
                case Command.ClearStage:
                    {
                        GetUserClearStage(new ArraySegment<byte>(buffer.Array, buffer.Offset + 4, buffer.Count - 4));
                        break;
                    }
                case Command.Currency:
                    {
                        GetUserCurrency(new ArraySegment<byte>(buffer.Array, buffer.Offset + 4, buffer.Count - 4));
                        break;
                    }
                case Command.Weapon:
                    {
                        GetWeaponData();
                        break;
                    }
                case Command.UserWeapon:
                    {
                        GetUserWeaponData(new ArraySegment<byte>(buffer.Array, buffer.Offset + 4, buffer.Count - 4));
                        break;
                    }
                case Command.WeaponEnhancement:
                    {
                        UserWeaponEnhancement(new ArraySegment<byte>(buffer.Array, buffer.Offset + 4, buffer.Count - 4));
                        break;
                    }
                case Command.Necklace:
                    {
                        GetNecklaceData();
                        break;
                    }
                case Command.UserNecklace:
                    {
                        GetUserNecklaceData(new ArraySegment<byte>(buffer.Array, buffer.Offset + 4, buffer.Count - 4));
                        break;
                    }
                case Command.Ring:
                    {
                        GetRingData();
                        break;
                    }
                case Command.UserRing:
                    {
                        GetUserRingData(new ArraySegment<byte>(buffer.Array, buffer.Offset + 4, buffer.Count - 4));
                        break;
                    }
                case Command.Equipment:
                    {
                        GetUserEquipmentData(new ArraySegment<byte>(buffer.Array, buffer.Offset + 4, buffer.Count - 4));
                        break;
                    }
                case Command.ChangeEquipment:
                    {
                        ChangeUserEquipment(new ArraySegment<byte>(buffer.Array, buffer.Offset + 4, buffer.Count - 4));
                        break;
                    }
                case Command.EndGame:
                    {
                        EndGame(new ArraySegment<byte>(buffer.Array, buffer.Offset + 4, buffer.Count - 4));
                        break;
                    }
                case Command.Purchase:
                    {
                        PurchaseProduct(new ArraySegment<byte>(buffer.Array, buffer.Offset + 4, buffer.Count - 4));
                        break;
                    }
                case Command.Product:
                    {
                        GetInAPP_ProductData();
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }
        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisconnected {endPoint}");
        }

        public override void OnSend(int numOfBytes)
        {
            //Console.WriteLine($"Transferred bytes : {numOfBytes}");
        }

        void GuestSignUp()
        {
            LoginPacket loginPacket = new LoginPacket();
            
            Send(loginPacket.GuestRegistration());
        }

        void GoogleSignUp(ArraySegment<byte> buffer)
        {
            LoginPacket loginPacket = new LoginPacket();

            Send(loginPacket.GoogleRegistration(buffer));
        }

        void GuestLogin(ArraySegment<byte> buffer)
        {
            LoginPacket loginPacket = new LoginPacket();

            //Send(loginPacket.GuestLogin(buffer));
            Send(loginPacket.Login(Provider.GUEST, buffer));
        }
        void GoogleLogin(ArraySegment<byte> buffer)
        {
            LoginPacket loginPacket = new LoginPacket();

            Send(loginPacket.Login(Provider.GOOGLE, buffer));
        }

        void GetUserClearStage(ArraySegment<byte> buffer)
        {
            StagePacket stagePacket = new StagePacket();
            Send(stagePacket.GetUserClearStage(buffer));
        }

        void GetUserCurrency(ArraySegment<byte> buffer)
        {
            CurrencyPacket currencyPacket = new CurrencyPacket();
            Send(currencyPacket.GetUserCurrency(buffer));
        }

        void GetWeaponData()
        {
            WeaponPacket weaponPacket = new WeaponPacket();

            Send(weaponPacket.GetWeaponDataPacket());
        }

        void GetUserWeaponData(ArraySegment<byte> buffer)
        {
            WeaponPacket weaponPacket = new WeaponPacket();
            
            Send(weaponPacket.GetUserWeaponData(buffer));
        }

        void UserWeaponEnhancement(ArraySegment<byte> buffer)
        {
            WeaponPacket weaponPacket = new WeaponPacket();

            Send(weaponPacket.WeaponEnhancement(buffer));
        }

        void GetNecklaceData()
        {
            NecklacePacket necklacePacket = new NecklacePacket();

            Send(necklacePacket.GetNecklaceDataPacket());
        }
        void GetUserNecklaceData(ArraySegment<byte> buffer)
        {
            NecklacePacket necklacePacket = new NecklacePacket();

            Send(necklacePacket.GetUserNecklaceData(buffer));
        }

        void GetRingData()
        {
            RingPacket ringPacket = new RingPacket();

            Send(ringPacket.GetRingDataPacket());
        }
        void GetUserRingData(ArraySegment<byte> buffer)
        {
            RingPacket ringPacket = new RingPacket();

            Send(ringPacket.GetUserRingData(buffer));
        }

        void GetUserEquipmentData(ArraySegment<byte> buffer)
        {
            EquipmentPacket equipmentPacket = new EquipmentPacket();

            Send(equipmentPacket.GetUserEquipment(buffer));
        }

        void ChangeUserEquipment(ArraySegment<byte> buffer)
        {
            EquipmentPacket equipmentPacket = new EquipmentPacket();

            Send(equipmentPacket.ChangeUserEquipment(buffer));
        }

        void EndGame(ArraySegment<byte> buffer)
        {
            StagePacket stagePacket = new StagePacket();
            Send(stagePacket.GetEndGameResult(buffer));
        }

        void PurchaseProduct(ArraySegment<byte> buffer)
        {
            PurchasePacket purchasePacket = new PurchasePacket();

            Send(purchasePacket.InAppPurchase(buffer));
        }

        void GetInAPP_ProductData()
        {
            ProductPacket productPacket = new ProductPacket();

            Send(productPacket.GetProductDataPacket());
        }
    }
}
