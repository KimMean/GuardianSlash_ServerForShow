using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GS_Server.MySQL;
using GS_ServerCore;

namespace GS_Server.Packets
{
    public struct InAPP_Products
    {
        public string productID;
        public string productName;
        public ushort price;
        public Products currencyType;
    }

    public class ProductPacket
    {

        static List<InAPP_Products> ProductDatas = new List<InAPP_Products>();

        public static void InitProductData()
        {
            Console.WriteLine("InitProductData");
            MySQLManager.Instance.GetProductData(ref ProductDatas);
        }

        public ArraySegment<byte> GetProductDataPacket()
        {
            ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);

            ushort count = sizeof(ushort);

            // Command 입력
            BitConverter.GetBytes((ushort)Command.Product).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            // 결과 입력
            BitConverter.GetBytes((ushort)ResultCommand.Success).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            // 총 개수 입력
            ushort productCount = (ushort)ProductDatas.Count;
            BitConverter.GetBytes(productCount).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            for (ushort i = 0; i < productCount; i++)
            {
                // weaponData 가져오기
                byte[] id = Encoding.UTF8.GetBytes(ProductDatas[i].productID);
                ushort idSize = (ushort)id.Length;
                byte[] name = Encoding.UTF8.GetBytes(ProductDatas[i].productName);
                ushort nameSize = (ushort)name.Length;
                ushort currencyType = (ushort)ProductDatas[i].currencyType;
                ushort price = ProductDatas[i].price;

                BitConverter.GetBytes(idSize).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(ushort);
                id.CopyTo(openSegment.Array, openSegment.Offset + count);
                count += idSize;

                BitConverter.GetBytes(nameSize).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(ushort);
                name.CopyTo(openSegment.Array, openSegment.Offset + count);
                count += nameSize;

                BitConverter.GetBytes(currencyType).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(ushort);

                BitConverter.GetBytes(price).CopyTo(openSegment.Array, openSegment.Offset + count);
                count += sizeof(ushort);


                //Console.WriteLine($"Product Packet Product ID : {ProductDatas[i].productID}, ProductName : {ProductDatas[i].productName}, CurrencyType : {ProductDatas[i].currencyType}, Price : {ProductDatas[i].price}");
            }

            // 총 사이즈 입력
            BitConverter.GetBytes(count).CopyTo(openSegment.Array, openSegment.Offset);

            return SendBufferHelper.Close(count);
        }
    }
}
