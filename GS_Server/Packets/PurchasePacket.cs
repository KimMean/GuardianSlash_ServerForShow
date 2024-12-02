using GS_Server.MySQL;
using GS_ServerCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ZstdSharp.Unsafe;

namespace GS_Server.Packets
{
    public class PurchasePacket
    {
        private readonly Dictionary<string, IRewardHandler> rewardHandlers;

        static ushort PurchaseIndex = 0;

        public PurchasePacket()
        {
            // ProductID에 따른 처리기 등록
            rewardHandlers = new Dictionary<string, IRewardHandler>
            {
                //{ "diamond250", new DiamondRewardHandler(250) },
                { "diamond10", new CurrencyRewardHandler(Products.Diamond, 10) },   // 광고 보상용
                { "diamond250", new CurrencyRewardHandler(Products.Diamond, 250) },
                { "diamond550", new CurrencyRewardHandler(Products.Diamond, 550) },
                { "diamond1200", new CurrencyRewardHandler(Products.Diamond, 1200) },
                { "diamond2800", new CurrencyRewardHandler(Products.Diamond, 2800) },
                { "diamond6000", new CurrencyRewardHandler(Products.Diamond, 6000) },

                { "gold_5000", new CurrencyRewardHandler(Products.Coin, 5000) },
                { "gold_55000", new CurrencyRewardHandler(Products.Coin, 55000) },
                { "gold_600000", new CurrencyRewardHandler(Products.Coin, 600000) },

                //{ "starter_package", new CoinRewardHandler(1) },        // 패키지 상품은 아직 사용되지 않음
                //{ "mercenary_package", new CoinRewardHandler(1) },
                //{ "knight_package", new CoinRewardHandler(1) },
                //{ "guardian_package", new CoinRewardHandler(1) },

                //{ "weapon_wood_single", new WeaponRewardHandler(GradeType.Wood, 1) }, 
                { "weapon_wood_single", new ItemRewardHandler(Products.Weapon, GradeType.Wood, 1) }, 
                { "weapon_wood_multi", new ItemRewardHandler(Products.Weapon, GradeType.Wood, 10) },
                { "weapon_silver_single", new ItemRewardHandler(Products.Weapon, GradeType.Silver, 1) },
                { "weapon_silver_multi", new ItemRewardHandler(Products.Weapon, GradeType.Silver, 10) },
                { "weapon_gold_single", new ItemRewardHandler(Products.Weapon, GradeType.Gold, 1) },
                { "weapon_gold_multi", new ItemRewardHandler(Products.Weapon, GradeType.Gold, 10) },

                { "necklace_wood_single", new ItemRewardHandler(Products.Necklace, GradeType.Wood, 1) },
                { "necklace_wood_multi", new ItemRewardHandler(Products.Necklace, GradeType.Wood, 10) },
                { "necklace_silver_single", new ItemRewardHandler(Products.Necklace, GradeType.Silver, 1) },
                { "necklace_silver_multi", new ItemRewardHandler(Products.Necklace, GradeType.Silver, 10) },
                { "necklace_gold_single", new ItemRewardHandler(Products.Necklace, GradeType.Gold, 1) },
                { "necklace_gold_multi", new ItemRewardHandler(Products.Necklace, GradeType.Gold, 10) },

                { "ring_wood_single", new ItemRewardHandler(Products.Ring, GradeType.Wood, 1) }, 
                { "ring_wood_multi", new ItemRewardHandler(Products.Ring, GradeType.Wood, 10) },
                { "ring_silver_single", new ItemRewardHandler(Products.Ring, GradeType.Silver, 1) },
                { "ring_silver_multi", new ItemRewardHandler(Products.Ring, GradeType.Silver, 10) },
                { "ring_gold_single", new ItemRewardHandler(Products.Ring, GradeType.Gold, 1) },
                { "ring_gold_multi", new ItemRewardHandler(Products.Ring, GradeType.Gold, 10) },
            };
        }
        /*
         * Token            - 사용자의 액세스 토큰
         * ProductID        - 상품의 고유 코드
         * TransactionID    - 구글에서 제공한 고유 결제 트랜잭션 ID
         * Receipt          - 구글에서 제공한 영수증
         * PurchaseDate     - 구매 날짜
         * PurchaseState    - 결제 상태 (0 : 완료, 1 : 취소, 2 : 환불)
         * IsValid          - 서버에서 영수증 검증 여부?
         * IsItemGranted    - 아이템 지급 여부
         * CreatedAt        - 레코드 생성 시간 (DB에서 생성)
         * UpdatedAt        - 레코드 수정 시간 (DB에서 생성)
         */
        public ArraySegment<byte> InAppPurchase(ArraySegment<byte> buffer)
        {
            int readCount = 0;

            Console.WriteLine("InAppPurchase");
            Payment payment = (Payment)BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            readCount += sizeof(ushort);
            //Console.WriteLine(payment);
            // AccessToken
            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset + readCount);
            readCount += sizeof(ushort);
            string token = Encoding.UTF8.GetString(buffer.Array, buffer.Offset + readCount, size);
            readCount += size;
            //Console.WriteLine($"Size : {size}, ReadCount : {readCount}, BufferCount : {buffer.Count}, Token : {token}");
            // ProductID
            size = BitConverter.ToUInt16(buffer.Array, buffer.Offset + readCount);
            readCount += sizeof(ushort);
            string productID = Encoding.UTF8.GetString(buffer.Array, buffer.Offset + readCount, size);
            readCount += size;
            //Console.WriteLine($"Size : {size}, ReadCount : {readCount}, BufferCount : {buffer.Count}, ProductID : {productID}");
            Console.WriteLine($"InAppPurchase ProductID : {productID}");

            bool result = false;
            ushort count = 0;
            string purchaseToken = PurchaseTokenGeneration();
            string transactionID = null;
            ArraySegment<byte> segment = new ArraySegment<byte>();


            MySQLManager.Instance.BeginTransaction();
            try
            {
                if (payment == Payment.Local)
                {
                    MySQLManager.Instance.InGameItemPurchase(token, purchaseToken, productID, out result);
                }
                else if (payment == Payment.Google)
                {
                    // TransactionID
                    size = BitConverter.ToUInt16(buffer.Array, buffer.Offset + readCount);
                    readCount += sizeof(ushort);
                    transactionID = Encoding.UTF8.GetString(buffer.Array, buffer.Offset + readCount, size);
                    readCount += size;

                    // Receipt
                    size = BitConverter.ToUInt16(buffer.Array, buffer.Offset + readCount);
                    readCount += sizeof(ushort);
                    string receipt = Encoding.UTF8.GetString(buffer.Array, buffer.Offset + readCount, size);
                    readCount += size;

                    MySQLManager.Instance.GoogleInAppPurchase(token, purchaseToken, productID, transactionID, receipt, out result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"payment Error: {ex.Message}");
                MySQLManager.Instance.RollbackTransaction();
            }

            try
            {
                if (result)
                {
                    if (rewardHandlers.TryGetValue(productID, out IRewardHandler rewardHandler))
                    {
                        // 아이템 지급
                        if (rewardHandler.HandleReward(token))
                        {
                            // 남은 재화 반환
                            MySQLManager.Instance.GetUserCurrency(token, out int coin, out int dia);
                            // 아이템 지급 완료
                            MySQLManager.Instance.ItemGrantedComplete(token, purchaseToken);

                            ArraySegment<byte> rewardPacket = rewardHandler.GetRewardPacket();
                            ArraySegment<byte> currencyPacket = GenerateCurrencyPacket(coin, dia);
                            segment = CombinePackets(rewardPacket, currencyPacket);
                            // transactionID 추가
                            segment = CombinePackets(segment, GetTransactionPacket(payment, transactionID));

                            count = (ushort)segment.Count;
                        }
                        else
                        {
                            MySQLManager.Instance.RollbackTransaction();
                            return GetInAppPurchaseResultPacket(ResultCommand.Failed, segment, count);
                        }
                    }
                    else
                    {
                        MySQLManager.Instance.RollbackTransaction();
                        Console.WriteLine("알 수 없는 ProductID 입니다.");
                        return GetInAppPurchaseResultPacket(ResultCommand.Failed, segment, count);
                    }
                }
                else
                {
                    Console.WriteLine("아이템 구매에 실패하였습니다.");
                    MySQLManager.Instance.RollbackTransaction();
                    return GetInAppPurchaseResultPacket(ResultCommand.Failed, segment, count);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"result Error: {ex.Message}");
                MySQLManager.Instance.RollbackTransaction();
                return GetInAppPurchaseResultPacket(ResultCommand.Failed, segment, count);
            }

            MySQLManager.Instance.CommitTransaction();

            return GetInAppPurchaseResultPacket(ResultCommand.Success, segment, count);
        }

        /*
         * Payment와 TransactionID를 추가합니다.
         * Payment가 Local일 경우 TransactionID는 null입니다.
         */
        private ArraySegment<byte> GetTransactionPacket(Payment payment, string transactionID = null)
        {
            ArraySegment<byte> packet = new ArraySegment<byte>(new byte[1024]);
            ushort count = 0;

            BitConverter.GetBytes((ushort)payment).CopyTo(packet.Array, packet.Offset + count);
            count += sizeof(ushort);

            if (transactionID != null)
            {
                byte[] tid = Encoding.UTF8.GetBytes(transactionID);
                ushort size = (ushort)tid.Length;

                BitConverter.GetBytes(size).CopyTo(packet.Array, packet.Offset + count);
                count += sizeof(ushort);
                tid.CopyTo(packet.Array, packet.Offset + count);
                count += size;
            }

            return new ArraySegment<byte>(packet.Array, 0, count); // 패킷의 크기 설정 후 반환
        }
        public ArraySegment<byte> GenerateCurrencyPacket(int coin, int diamond)
        {
            ushort count = 0;
            ArraySegment<byte> packet = new ArraySegment<byte>(new byte[1024]);

            // Coin 정보 저장
            BitConverter.GetBytes((ushort)Products.Coin).CopyTo(packet.Array, packet.Offset + count);
            count += sizeof(ushort);
            BitConverter.GetBytes(coin).CopyTo(packet.Array, packet.Offset + count);
            count += sizeof(int);

            // Diamond 정보 저장
            BitConverter.GetBytes((ushort)Products.Diamond).CopyTo(packet.Array, packet.Offset + count);
            count += sizeof(ushort);
            BitConverter.GetBytes(diamond).CopyTo(packet.Array, packet.Offset + count);
            count += sizeof(int);

            return new ArraySegment<byte>(packet.Array, 0, count); // 패킷의 크기 설정 후 반환
        }
        public ArraySegment<byte> CombinePackets(ArraySegment<byte> packetA, ArraySegment<byte> packetB)
        {
            // 두 패킷을 결합하기 위해 새로운 배열 생성
            byte[] combinedPacket = new byte[packetA.Count + packetB.Count];

            // 아이템 보상 패킷 복사 (재화 패킷 뒤에 붙임)
            Array.Copy(packetA.Array, packetA.Offset, combinedPacket, 0, packetA.Count);

            // 재화 패킷 복사
            Array.Copy(packetB.Array, packetB.Offset, combinedPacket, packetA.Count, packetB.Count);


            //Console.WriteLine($"rewardPacket : {packetA.Count}, currencyPacket : {packetB.Count}");
            // 결합된 패킷 반환
            return new ArraySegment<byte>(combinedPacket, 0, packetA.Count + packetB.Count);
        }

        ArraySegment<byte> GetInAppPurchaseResultPacket(ResultCommand resultCmd, ArraySegment<byte> copySegment, ushort copyCount)
        {
            ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);

            ushort count = sizeof(ushort);

            // Command 입력
            BitConverter.GetBytes((ushort)Command.Purchase).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            BitConverter.GetBytes((ushort)resultCmd).CopyTo(openSegment.Array, openSegment.Offset + count);
            count += sizeof(ushort);

            if(resultCmd == ResultCommand.Success) 
            {
                Array.Copy(copySegment.Array, copySegment.Offset, openSegment.Array, openSegment.Offset + count, copyCount);
                count += copyCount;
            }
            // 총 사이즈 입력
            BitConverter.GetBytes(count).CopyTo(openSegment.Array, openSegment.Offset);
            //Console.WriteLine($"GetInAppPurchaseResultPacket : {count}");

            return SendBufferHelper.Close(count);
        }

        /*
         * PurchaseToken을 생성합니다.
         * UnixTime을 16진수로 바꾸고 중복을 막기 위해 16진수 4자리의 인덱스를 추가합니다.
         * 
         */
        string PurchaseTokenGeneration()
        {
            long unixTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            string purchaseToken = unixTime.ToString("X8");
            purchaseToken += PurchaseIndex.ToString("X4");  // 16진수로 변환하고 4자리 문자열로 포맷팅

            if (PurchaseIndex == ushort.MaxValue)
                PurchaseIndex = 0;
            else
                PurchaseIndex++;

            return purchaseToken;
        }
    }

    /*
     * Reward Interface
     */
    public interface IRewardHandler
    {
        bool HandleReward(string token);
        ArraySegment<byte> GetRewardPacket();
        ushort GetPacketCount();
    }

    public class CurrencyRewardHandler : IRewardHandler
    {
        Products currencyType;
        int amount;
        ushort count;

        public CurrencyRewardHandler(Products currencyType, int amount)
        {
            this.currencyType = currencyType;
            this.amount = amount;
        }

        public bool HandleReward(string token)
        {
            bool result;
            MySQLManager.Instance.UpdateUserCurrency(token, currencyType, amount, out result);
            Console.WriteLine($"{currencyType} {amount} 지급 완료");
            return result;
        }

        public ArraySegment<byte> GetRewardPacket()
        {
            count = 0;
            ArraySegment<byte> packet = new ArraySegment<byte>(new byte[1024]);

            BitConverter.GetBytes((ushort)currencyType).CopyTo(packet.Array, packet.Offset);
            count += sizeof(ushort);

            return new ArraySegment<byte>(packet.Array, 0, count);
        }

        public ushort GetPacketCount() => count;
    }

    public class ItemRewardHandler : IRewardHandler
    {
         Products itemType;
         GradeType grade;
         int amount;
         ushort count;
         string itemCodesString;

        public ItemRewardHandler(Products itemType, GradeType grade, int amount)
        {
            this.itemType = itemType;
            this.grade = grade;
            this.amount = amount;
        }

        public bool HandleReward(string token)
        {
            List<string> itemCodes = GenerateRandomItemCodesWithWeight(itemType, amount);
            itemCodesString = string.Join(",", itemCodes);
            bool result;
            MySQLManager.Instance.AddItemList(token, itemType, itemCodesString, out result);
            Console.WriteLine($"아이템 {itemCodesString} 지급 완료");
            return result;
        }

        public ArraySegment<byte> GetRewardPacket()
        {
            count = 0;
            ArraySegment<byte> packet = new ArraySegment<byte>(new byte[1024]);

            BitConverter.GetBytes((ushort)itemType).CopyTo(packet.Array, packet.Offset);
            count += sizeof(ushort);

            byte[] code = Encoding.UTF8.GetBytes(itemCodesString);
            BitConverter.GetBytes((ushort)code.Length).CopyTo(packet.Array, packet.Offset + count);
            count += sizeof(ushort);

            code.CopyTo(packet.Array, packet.Offset + count);
            count += (ushort)code.Length;

            return new ArraySegment<byte>(packet.Array, 0, count);
        }

        public ushort GetPacketCount() => count;

        private List<string> GenerateRandomItemCodesWithWeight(Products itemType, int amount)
        {
            string[] itemCodesArray = itemType switch
            {
                Products.Weapon => new string[20],
                Products.Necklace or Products.Ring => new string[15],
                _ => throw new ArgumentOutOfRangeException()
            };

            int itemCount = itemType switch
            {
                Products.Weapon => 20,
                Products.Necklace or Products.Ring => 15,
                _ => throw new ArgumentOutOfRangeException()
            };

            // 아이템 코드 초기화
            for (int i = 0; i < itemCodesArray.Length; i++)
            {
                itemCodesArray[i] = $"{itemType.ToString().Substring(0, 1)}{(i + 1).ToString("D3")}"; // 예: "W001", "N001", "R001" 등
            }

            // 나무, 은, 금 상자 확률 계산
            double[] woodBox = CalculateProbabilities(30.0, 0.1, itemCount);
            double[] silverBox = CalculateProbabilities(25.0, 0.5, itemCount);
            double[] goldBox = CalculateProbabilities(20.0, 1.0, itemCount);

            double[] probabilities = grade switch
            {
                GradeType.Wood => CalculateProbabilities(30.0, 0.1, itemCount),
                GradeType.Silver => CalculateProbabilities(25.0, 0.5, itemCount),
                GradeType.Gold => CalculateProbabilities(20.0, 1.0, itemCount),
                _ => throw new ArgumentOutOfRangeException()
            };

            // 랜덤으로 아이템 선택
            List<string> selectedItems = new List<string>();

            for (int i = 0; i < amount; i++)
            {
                //string selectedItem = SelectItemCodeWithWeight(itemCodesArray, selectedWeights);
                string selectedItem = SelectItemCodeWithWeight(itemCodesArray, probabilities);
                selectedItems.Add(selectedItem);
            }

            return selectedItems;
        }

        private string SelectItemCodeWithWeight(string[] itemCodes, double[] probabilities)
        {
            Random random = new Random();
            double randomValue = random.NextDouble() * 100;

            double sum = 0;

            for (int i = 0; i < itemCodes.Length; i++)
            {
                sum += probabilities[i];
                //Console.WriteLine($"i : {i} => RandomValue : {randomValue}, < Sum : {sum}, Current Probability : {probabilities[i]}");
                if (randomValue < sum)
                {
                    //Console.WriteLine($"i : {i} 번째 아이템 {itemCodes[i]} 선택됌");
                    return itemCodes[i];
                }
            }
            return itemCodes[0]; // 기본값으로 첫 번째 아이템 반환 (이론적으로 여기까지 오지 않음)
        }

        // 가중치 기반으로 아이템 코드 선택하는 함수
        private string SelectItemCodeWithWeight(string[] itemCodes, int[] weights)
        {
            int totalWeight = weights.Sum(); // 가중치 합계 계산
            Random random = new Random();
            int randomValue = random.Next(0, totalWeight); // 0 ~ totalWeight - 1 사이의 랜덤 값

            int cumulativeWeight = 0;

            // 가중치에 따라 아이템 코드 선택
            for (int i = 0; i < itemCodes.Length; i++)
            {
                cumulativeWeight += weights[i];

                if (randomValue < cumulativeWeight)
                {
                    return itemCodes[i];
                }
            }

            return itemCodes[0]; // 기본값으로 첫 번째 아이템 반환 (이론적으로 여기까지 오지 않음)
        }

        /// <summary>
        /// 아이템 확률을 계산하는 메서드
        /// </summary>
        /// <param name="startProb">첫 번째 아이템 확률(%)</param>
        /// <param name="endProb">마지막 아이템 확률(%)</param>
        /// <param name="itemCount">아이템 개수</param>
        /// <returns>정규화된 확률 배열</returns>
        public double[] CalculateProbabilities(double startProb, double endProb, int itemCount)
        {
            // 로그 스케일로 확률 계산
            double logStart = Math.Log10(startProb);
            double logEnd = Math.Log10(endProb);
            double[] logProbs = Enumerable.Range(0, itemCount)
                                           .Select(i => logStart + (logEnd - logStart) * i / (itemCount - 1))
                                           .ToArray();

            // 로그 값을 일반 확률 값으로 변환 및 정규화
            double[] probabilities = logProbs.Select(x => Math.Pow(10, x)).ToArray();
            double total = probabilities.Sum();
            return probabilities.Select(p => (p / total) * 100).ToArray();
        }
    }

    /*
     * Package 상품 지급
     */
    public class PackageRewardHandler : IRewardHandler
    {
        ushort count;
        public bool HandleReward(string token)
        {
            return true;
        }
        public ArraySegment<byte> GetRewardPacket()
        {
            ArraySegment<byte> packet = new ArraySegment<byte>();

            return packet;
        }
        public ushort GetPacketCount()
        {
            return count;
        }
    }

}
