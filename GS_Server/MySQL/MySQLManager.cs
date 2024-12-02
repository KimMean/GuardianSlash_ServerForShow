using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Xml.Linq;
using GS_Server.Packets;
using GS_Server.Utilities;
using MySql.Data.MySqlClient;
using Mysqlx.Session;
using MySqlX.XDevAPI.Common;

namespace GS_Server.MySQL
{

    public class MySQLManager
    {
        private static MySQLManager instance;
        public static MySQLManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new MySQLManager();
                return instance;
            }
        }

        const string ServerIP = "localhost";      // DB 서버 주소, 로컬일 경우 localhost
        const int ServerPort = 0000;                  //DB 서버 포트
        const string DB_Name = "private";       //DB 이름
        const string UserID = "private";            //계정 아이디
        const string UserPWD = "private";        //계정 비밀번호

        const string Polling = "true";
        const int MinPoolSize = 0;
        const int MaxPoolSize = 10;     // (CPU Core * 2) + effective_spindle_count

        private MySqlConnection connection;
        private MySqlTransaction transaction;

        int ConnectCount = 0;

        static string connectionAddress = string.Format(
            "Server={0};" +
            "Port={1};" +
            "Database={2};" +
            "Uid={3};" +
            "Pwd={4};" +
            "Pooling={5};" +
            "Min Pool Size={6};" +
            "Max Pool Size={7};"
            , ServerIP, ServerPort, DB_Name, UserID, UserPWD, Polling, MinPoolSize, MaxPoolSize
            );

        public MySQLManager()
        {
            Console.WriteLine("MySQLManager constructor");
            connection = new MySqlConnection(connectionAddress);
        }

        public void Init()
        {
            Console.WriteLine("MySQLManager Initialize");
        }

        // 트랜잭션 시작
        public void BeginTransaction()
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
                ConnectCount++;
                Console.WriteLine($"Connection Open : {ConnectCount}");
            }
            transaction = connection.BeginTransaction();
            //Console.WriteLine("Transaction started.");
        }

        // 트랜잭션 커밋
        public void CommitTransaction()
        {
            try
            {
                if (transaction != null)
                {
                    transaction.Commit();
                    //Console.WriteLine("Transaction committed.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during commit: {ex.Message}");
                RollbackTransaction(); // 오류 시 롤백
                throw;
            }
            finally
            {
                CleanupTransaction(); // 트랜잭션 정리 및 연결 닫기
            }
        }

        // 트랜잭션 롤백
        public void RollbackTransaction()
        {
            try
            {
                if (transaction != null)
                {
                    transaction.Rollback();
                    Console.WriteLine("Transaction rolled back.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during rollback: {ex.Message}");
            }
            finally
            {
                CleanupTransaction(); // 트랜잭션 정리 및 연결 닫기
            }
        }
        // 트랜잭션 정리 및 연결 종료
        private void CleanupTransaction()
        {
            if (transaction != null)
            {
                transaction.Dispose();
                transaction = null;
            }

            if (connection != null && connection.State == System.Data.ConnectionState.Open)
            {
                connection.Close();

                ConnectCount--;
                Console.WriteLine($"Connection Close : {ConnectCount}");
                //Console.WriteLine("Connection closed.");
            }
        }

        /*
         * 회원가입을 진행
         * 가입 성공시 true, 실패시 false
         */
        public bool Registration(Provider providerCode, string userID)
        {
            bool result = false;
            //using (MySqlConnection connection = new MySqlConnection(connectionAddress))
            try
            {
                string query = $"SELECT SignUp({(int)providerCode}, '{userID}') AS Result;";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            result = bool.Parse(reader["Result"].ToString());
                        }
                    }
                }                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
            return result;
        }

        // Unuesd
        public bool GuestLogin(string userID, string accessToken)
        {
            bool result = false;
            try
            {
                string query = $"SELECT GuestLogin('{userID}', '{accessToken}') AS Result;";

                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            result = bool.Parse(reader["Result"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
            return result;
        }

        /*
         * 로그인을 진행합니다.
         * 게스트 로그인과 구글 로그인이 지원됩니다.
         * 
         */
        public bool Login(Provider provider, string userID, string accessToken)
        {
            bool result = false;
            try
            {
                string query = $"SELECT Login({(int)provider}, '{userID}', '{accessToken}') AS Result;";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            result = bool.Parse(reader["Result"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }

            return result;

        }

        public bool GetUserClearStage(string accessToken, out uint clearStage)
        {
            bool result = false;
            clearStage = 0;
            try
            {
                using (MySqlCommand cmd = new MySqlCommand("GetUserClearStage", connection))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // 입력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@token", accessToken));
                    //Console.WriteLine($"accessToken : {accessToken}");

                    // 출력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@stage", MySqlDbType.UInt32));
                    cmd.Parameters["@stage"].Direction = System.Data.ParameterDirection.Output;

                    // 프로시저 실행
                    cmd.ExecuteNonQuery();

                    // 출력 파라미터 값 가져오기
                    clearStage = (uint)cmd.Parameters["@stage"].Value;

                    // 결과 출력
                    //Console.WriteLine($"ClearStage : {clearStage}");

                    result = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
            return result;
        }

        public bool GetUserCurrency(string accessToken, out int coin, out int dia)
        {
            bool result = false;
            coin = 0;
            dia = 0;
            try
            {
                using (MySqlCommand cmd = new MySqlCommand("GetUserCurrencies", connection))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // 입력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@accessToken", MySqlDbType.String));
                    cmd.Parameters["@accessToken"].Value = accessToken;

                    // 출력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@coinAmount", MySqlDbType.Int32));
                    cmd.Parameters["@coinAmount"].Direction = System.Data.ParameterDirection.Output;
                    
                    cmd.Parameters.Add(new MySqlParameter("@diamondAmount", MySqlDbType.Int32));
                    cmd.Parameters["@diamondAmount"].Direction = System.Data.ParameterDirection.Output;

                    // 프로시저 실행
                    cmd.ExecuteNonQuery();

                    // 출력 파라미터 값 가져오기
                    coin = Convert.ToInt32(cmd.Parameters["@coinAmount"].Value);
                    dia = Convert.ToInt32(cmd.Parameters["@diamondAmount"].Value);

                    // 결과 출력
                    //Console.WriteLine($"Coin Amount: {coin}");
                    //Console.WriteLine($"Diamond Amount: {dia}");
                    result = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
            return result;
        }

        public void GetWeaponData(ref List<WeaponData> weaponDatas)
        {
            try
            {
                using (MySqlCommand cmd = new MySqlCommand("GetWeaponData", connection))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        WeaponPacket weaponPacket = new WeaponPacket();
                        while (reader.Read())
                        {
                            string weaponCode = reader["WeaponCode"].ToString();
                            string weaponName = reader["WeaponName"].ToString();
                            int attackLevel = Convert.ToInt32(reader["AttackLevel"]);

                            WeaponData weaponData = new WeaponData();
                            weaponData.WeaponCode = weaponCode;
                            weaponData.WeaponName = weaponName;
                            weaponData.AttackLevel = attackLevel;
                            weaponDatas.Add(weaponData);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

        public bool GetUserWeaponData(string token, ref List<UserWeaponData> weaponDatas)
        {
            bool result = false;
            try
            {
                using (MySqlCommand cmd = new MySqlCommand("GetUserWeaponData", connection))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // 입력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@accessToken", token));

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        WeaponPacket weaponPacket = new WeaponPacket();
                        while (reader.Read())
                        {
                            string weaponCode = reader["WeaponCode"].ToString();
                            int level = Convert.ToInt32(reader["EnhancementLevel"]);
                            int quantity = Convert.ToInt32(reader["Quantity"]);
                            int attackLevel = Convert.ToInt32(reader["AttackLevel"]);

                            UserWeaponData weaponData = new UserWeaponData();
                            weaponData.WeaponCode = weaponCode;
                            weaponData.Level = level;
                            weaponData.Quantity = quantity;
                            weaponData.AttackLevel = attackLevel;
                            weaponDatas.Add(weaponData);
                        }
                        result = true;
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
            return result;
        }

        public bool UserWeaponEnhancement(string token, int cost, ref UserWeaponData weaponData, ref int coin, out bool result)
        {
            result = false;
            try
            {
                using (MySqlCommand cmd = new MySqlCommand("WeaponEnhancement", connection))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // 입력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@token", MySqlDbType.String));
                    cmd.Parameters["@token"].Value = token;
                    cmd.Parameters.Add(new MySqlParameter("@_itemCode", MySqlDbType.String));
                    cmd.Parameters["@_itemCode"].Value = weaponData.WeaponCode;
                    cmd.Parameters.Add(new MySqlParameter("@_count", MySqlDbType.Int32));
                    cmd.Parameters["@_count"].Value = weaponData.Quantity;  // 업그레이드에 필요한 수량을 대체함
                    cmd.Parameters.Add(new MySqlParameter("@_cost", MySqlDbType.Int32));
                    cmd.Parameters["@_cost"].Value = cost;

                    // 출력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@_itemQuantity", MySqlDbType.Int32));
                    cmd.Parameters["@_itemQuantity"].Direction = System.Data.ParameterDirection.Output;
                    cmd.Parameters.Add(new MySqlParameter("@_itemEnhancementLevel", MySqlDbType.Int32));
                    cmd.Parameters["@_itemEnhancementLevel"].Direction = System.Data.ParameterDirection.Output;
                    cmd.Parameters.Add(new MySqlParameter("@_itemAttackLevel", MySqlDbType.Int32));
                    cmd.Parameters["@_itemAttackLevel"].Direction = System.Data.ParameterDirection.Output;
                    cmd.Parameters.Add(new MySqlParameter("@_coin", MySqlDbType.Int32));
                    cmd.Parameters["@_coin"].Direction = System.Data.ParameterDirection.Output;
                    cmd.Parameters.Add(new MySqlParameter("@result", MySqlDbType.Byte));
                    cmd.Parameters["@result"].Direction = System.Data.ParameterDirection.Output;

                    // 프로시저 실행
                    cmd.ExecuteNonQuery();

                    // 출력 파라미터 값 가져오기
                    result = Convert.ToBoolean(cmd.Parameters["@result"].Value);

                    if(result)
                    {
                        weaponData.Quantity = Convert.ToInt32(cmd.Parameters["@_itemQuantity"].Value);
                        weaponData.Level = Convert.ToInt32(cmd.Parameters["@_itemEnhancementLevel"].Value);
                        weaponData.AttackLevel = Convert.ToInt32(cmd.Parameters["@_itemAttackLevel"].Value);
                        coin = Convert.ToInt32(cmd.Parameters["@_coin"].Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
            return result;
        }


        public void GetNecklaceData(ref List<NecklaceData> necklaceDatas)
        {
            try
            {
                using (MySqlCommand cmd = new MySqlCommand("GetNecklaceData", connection))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        NecklacePacket necklacePacket = new NecklacePacket();
                        while (reader.Read())
                        {
                            string necklaceCode = reader["NecklaceCode"].ToString();
                            string necklaceName = reader["NecklaceName"].ToString();
                            int twilight = Convert.ToInt32(reader["Twilight"]);
                            int varVoid = Convert.ToInt32(reader["Void"]);
                            int hell = Convert.ToInt32(reader["Hell"]);

                            NecklaceData necklaceData = new NecklaceData();
                            necklaceData.NecklaceCode = necklaceCode;
                            necklaceData.NecklaceName = necklaceName;
                            necklaceData.Twilight = twilight;
                            necklaceData.Void = varVoid;
                            necklaceData.Hell = hell;
                            necklaceDatas.Add(necklaceData);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

        public bool GetUserNecklaceData(string token, ref List<string> necklaceDatas)
        {
            bool result = false;
            try
            {
                using (MySqlCommand cmd = new MySqlCommand("GetUserNecklaceData", connection))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // 입력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@accessToken", token));

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        NecklacePacket necklacePacket = new NecklacePacket();
                        while (reader.Read())
                        {
                            string necklaceCode = reader["NecklaceCode"].ToString();

                            necklaceDatas.Add(necklaceCode);
                        }
                        result = true;
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
            return result;
        }

        public void GetRingData(ref List<RingData> ringDatas)
        {
            try
            {
                using (MySqlCommand cmd = new MySqlCommand("GetRingData", connection))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        RingPacket ringPacket = new RingPacket();
                        while (reader.Read())
                        {
                            string ringCode = reader["RingCode"].ToString();
                            string ringName = reader["RingName"].ToString();
                            int attack = Convert.ToInt32(reader["Attack"]);
                            int gold = Convert.ToInt32(reader["Gold"]);
                            int jump = Convert.ToInt32(reader["Jump"]);

                            RingData ringData = new RingData();
                            ringData.RingCode = ringCode;
                            ringData.RingName = ringName;
                            ringData.Attack = attack;
                            ringData.Gold = gold;
                            ringData.Jump = jump;
                            ringDatas.Add(ringData);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }
        public bool GetUserRingData(string token, ref List<string> ringDatas)
        {
            bool result = false;
            try
            {
                using (MySqlCommand cmd = new MySqlCommand("GetUserRingData", connection))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // 입력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@accessToken", token));

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        RingPacket ringPacket = new RingPacket();
                        while (reader.Read())
                        {
                            string ringCode = reader["RingCode"].ToString();

                            ringDatas.Add(ringCode);
                        }
                        result = true;
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
            return result;
        }

        public bool GetUserEquipment(string token, out string weaponCode, out string necklaceCode, out string ringCode)
        {
            bool result = false;
            weaponCode = "";
            necklaceCode = "";
            ringCode = "";
            try
            {
                using (MySqlCommand cmd = new MySqlCommand("GetUserEquipment", connection))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // 입력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@token", token));

                    // 출력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@_weaponCode", MySqlDbType.VarChar, 4));
                    cmd.Parameters["@_weaponCode"].Direction = System.Data.ParameterDirection.Output;
                    cmd.Parameters.Add(new MySqlParameter("@_necklaceCode", MySqlDbType.VarChar, 4));
                    cmd.Parameters["@_necklaceCode"].Direction = System.Data.ParameterDirection.Output;
                    cmd.Parameters.Add(new MySqlParameter("@_ringCode", MySqlDbType.VarChar, 4));
                    cmd.Parameters["@_ringCode"].Direction = System.Data.ParameterDirection.Output;

                    // 프로시저 실행
                    cmd.ExecuteNonQuery();

                    // 출력 파라미터 값 가져오기
                    weaponCode = cmd.Parameters["@_weaponCode"].Value.ToString();
                    necklaceCode = cmd.Parameters["@_necklaceCode"].Value.ToString();
                    ringCode = cmd.Parameters["@_ringCode"].Value.ToString();
                    
                    result = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
            return result;
        }

        public void ChangeUserEquipment(string token, Products item, string itemCode, out bool result)
        {
            result = false;

            string procedureString = item switch
            {
                Products.Weapon => "ChangeUserWeapon",
                Products.Necklace => "ChangeUserNecklace",
                Products.Ring => "ChangeUserRing"
            };
            try
            {
                using (MySqlCommand cmd = new MySqlCommand(procedureString, connection))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // 입력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@token", MySqlDbType.String));
                    cmd.Parameters["@token"].Value = token;
                    cmd.Parameters.Add(new MySqlParameter("@itemCode", MySqlDbType.String));
                    cmd.Parameters["@itemCode"].Value = itemCode;

                    // 출력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@result", MySqlDbType.Byte));
                    cmd.Parameters["@result"].Direction = System.Data.ParameterDirection.Output;

                    // 프로시저 실행
                    cmd.ExecuteNonQuery();

                    // 출력 파라미터 값 가져오기
                    result = Convert.ToBoolean(cmd.Parameters["@result"].Value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

        public void UpdateUserCurrency(string token, string currencyType, int amount, out bool result)
        {
            result = false;
            try
            {
                using (MySqlCommand cmd = new MySqlCommand("UpdateUserCurrency", connection))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // 입력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@token", MySqlDbType.String));
                    cmd.Parameters["@token"].Value = token;
                    cmd.Parameters.Add(new MySqlParameter("@currencyType", MySqlDbType.String));
                    cmd.Parameters["@currencyType"].Value = currencyType;
                    cmd.Parameters.Add(new MySqlParameter("@amount", MySqlDbType.Int32));
                    cmd.Parameters["@amount"].Value = amount;

                    // 출력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@result", MySqlDbType.Byte));
                    cmd.Parameters["@result"].Direction = System.Data.ParameterDirection.Output;

                    // 프로시저 실행
                    cmd.ExecuteNonQuery();

                    // 출력 파라미터 값 가져오기
                    result = Convert.ToBoolean(cmd.Parameters["@result"].Value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

        public void UpdateUserCurrency(string token, Products currencyType, int amount, out bool result)
        {
            result = false;
            try
            {
                using (MySqlCommand cmd = new MySqlCommand("UpdateUserCurrency", connection))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // 입력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@token", MySqlDbType.String));
                    cmd.Parameters["@token"].Value = token;
                    cmd.Parameters.Add(new MySqlParameter("@_currencyType", MySqlDbType.String));
                    cmd.Parameters["@_currencyType"].Value = currencyType.ToCustomString();
                    cmd.Parameters.Add(new MySqlParameter("@_amount", MySqlDbType.Int32));
                    cmd.Parameters["@_amount"].Value = amount;

                    // 출력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@result", MySqlDbType.Byte));
                    cmd.Parameters["@result"].Direction = System.Data.ParameterDirection.Output;

                    // 프로시저 실행
                    cmd.ExecuteNonQuery();

                    // 출력 파라미터 값 가져오기
                    result = Convert.ToBoolean(cmd.Parameters["@result"].Value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

        public void EndGame(string token, int stage, bool gameResult, ref int coin, ref int dia, out bool result)
        {
            result = false;
            try
            {
                using (MySqlCommand cmd = new MySqlCommand("EndGame", connection))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // 입력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@token", MySqlDbType.String));
                    cmd.Parameters["@token"].Value = token;
                    cmd.Parameters.Add(new MySqlParameter("@stage", MySqlDbType.String));
                    cmd.Parameters["@stage"].Value = stage;
                    cmd.Parameters.Add(new MySqlParameter("@gameResult", MySqlDbType.Byte));
                    cmd.Parameters["@gameResult"].Value = gameResult ? 1 : 0;

                    // INOUT 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@coin", MySqlDbType.Int32));
                    cmd.Parameters["@coin"].Direction = System.Data.ParameterDirection.InputOutput;
                    cmd.Parameters["@coin"].Value = coin;
                    cmd.Parameters.Add(new MySqlParameter("@dia", MySqlDbType.Int32));
                    cmd.Parameters["@dia"].Direction = System.Data.ParameterDirection.InputOutput;
                    cmd.Parameters["@dia"].Value = dia;

                    // 출력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@result", MySqlDbType.Byte));
                    cmd.Parameters["@result"].Direction = System.Data.ParameterDirection.Output;

                    // 프로시저 실행
                    cmd.ExecuteNonQuery();

                    // 업데이트된 coin과 dia 값을 받아옴
                    coin = Convert.ToInt32(cmd.Parameters["@coin"].Value);
                    dia = Convert.ToInt32(cmd.Parameters["@dia"].Value);

                    // 출력 파라미터 값 가져오기
                    result = Convert.ToBoolean(cmd.Parameters["@result"].Value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

        public void GoogleInAppPurchase(string token, string purchaseToken, string productID, string transactionID, string receipt, out bool result)
        {
            result = false;
            try
            {
                using (MySqlCommand cmd = new MySqlCommand("GoogleInAppPurchase", connection))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // 입력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@token", MySqlDbType.String));
                    cmd.Parameters["@token"].Value = token;
                    cmd.Parameters.Add(new MySqlParameter("@_purchaseToken", MySqlDbType.String));
                    cmd.Parameters["@_purchaseToken"].Value = purchaseToken;
                    cmd.Parameters.Add(new MySqlParameter("@_productID", MySqlDbType.String));
                    cmd.Parameters["@_productID"].Value = productID;
                    cmd.Parameters.Add(new MySqlParameter("@_transactionID", MySqlDbType.String));
                    cmd.Parameters["@_transactionID"].Value = transactionID;
                    cmd.Parameters.Add(new MySqlParameter("@_receipt", MySqlDbType.String));
                    cmd.Parameters["@_receipt"].Value = receipt;

                    // 출력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@result", MySqlDbType.Byte));
                    cmd.Parameters["@result"].Direction = System.Data.ParameterDirection.Output;

                    // 프로시저 실행
                    cmd.ExecuteNonQuery();

                    // 출력 파라미터 값 가져오기
                    result = Convert.ToBoolean(cmd.Parameters["@result"].Value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

        public void InGameItemPurchase(string token, string purchaseToken, string productID, out bool result)
        {
            result = false;
            //using (MySqlConnection connection = new MySqlConnection(connectionAddress))
            try
            {
                using (MySqlCommand cmd = new MySqlCommand("InGameItemPurchase", connection))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // 입력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@token", MySqlDbType.String));
                    cmd.Parameters["@token"].Value = token;
                    cmd.Parameters.Add(new MySqlParameter("@_purchaseToken", MySqlDbType.String));
                    cmd.Parameters["@_purchaseToken"].Value = purchaseToken;
                    cmd.Parameters.Add(new MySqlParameter("@_productID", MySqlDbType.String));
                    cmd.Parameters["@_productID"].Value = productID;

                    // 출력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@result", MySqlDbType.Byte));
                    cmd.Parameters["@result"].Direction = System.Data.ParameterDirection.Output;

                    // 프로시저 실행
                    cmd.ExecuteNonQuery();

                    // 출력 파라미터 값 가져오기
                    //Console.WriteLine($"Result : {cmd.Parameters["@result"].Value}");
                    result = Convert.ToBoolean(cmd.Parameters["@result"].Value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

        public void ItemGrantedComplete(string token, string purchaseToken)
        {
            try
            {
                using (MySqlCommand cmd = new MySqlCommand("ItemGrantedCompleted", connection))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // 입력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@token", MySqlDbType.String));
                    cmd.Parameters["@token"].Value = token;
                    cmd.Parameters.Add(new MySqlParameter("@_purchaseToken", MySqlDbType.String));
                    cmd.Parameters["@_purchaseToken"].Value = purchaseToken;

                    // 프로시저 실행
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

        public void AddItemList(string token, Products itemType, string itemList, out bool result)
        {
            result = false;

            // itemType에 따라 저장 프로시저 이름을 결정
            string storedProcedureName = itemType switch
            {
                Products.Weapon => "AddWeaponList",
                Products.Necklace => "AddNecklaceList",
                Products.Ring => "AddRingList",
                _ => throw new ArgumentOutOfRangeException(nameof(itemType), "유효하지 않은 아이템 타입입니다.")
            };

            try
            {
                using (MySqlCommand cmd = new MySqlCommand(storedProcedureName, connection))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // 입력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@token", MySqlDbType.String));
                    cmd.Parameters["@token"].Value = token;
                    cmd.Parameters.Add(new MySqlParameter("@itemList", MySqlDbType.Text));
                    cmd.Parameters["@itemList"].Value = itemList;

                    // 출력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@result", MySqlDbType.Byte));
                    cmd.Parameters["@result"].Direction = System.Data.ParameterDirection.Output;

                    // 프로시저 실행
                    cmd.ExecuteNonQuery();

                    result = Convert.ToBoolean(cmd.Parameters["@result"].Value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

        public void AddWeaponList(string token, string itemList, out bool result)
        {
            result = false;
            using (MySqlConnection connection = new MySqlConnection(connectionAddress))
            {
                using (MySqlCommand cmd = new MySqlCommand("AddWeaponList", connection))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // 입력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@token", MySqlDbType.String));
                    cmd.Parameters["@token"].Value = token;
                    cmd.Parameters.Add(new MySqlParameter("@itemList", MySqlDbType.Text));
                    cmd.Parameters["@itemList"].Value = itemList;

                    // 출력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@result", MySqlDbType.Byte));
                    cmd.Parameters["@result"].Direction = System.Data.ParameterDirection.Output;

                    // 프로시저 실행
                    cmd.ExecuteNonQuery();

                    result = Convert.ToBoolean(cmd.Parameters["@result"].Value);
                }
            }
        }
        public void AddNecklaceList(string token, string itemList, out bool result)
        {
            result = false;
            try
            {
                using (MySqlCommand cmd = new MySqlCommand("AddNecklaceList", connection))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // 입력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@token", MySqlDbType.String));
                    cmd.Parameters["@token"].Value = token;
                    cmd.Parameters.Add(new MySqlParameter("@itemList", MySqlDbType.Text));
                    cmd.Parameters["@itemList"].Value = itemList;

                    // 출력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@result", MySqlDbType.Byte));
                    cmd.Parameters["@result"].Direction = System.Data.ParameterDirection.Output;

                    // 프로시저 실행
                    cmd.ExecuteNonQuery();

                    result = Convert.ToBoolean(cmd.Parameters["@result"].Value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }
        public void AddRingList(string token, string itemList, out bool result)
        {
            result = false;
            try
            {
                using (MySqlCommand cmd = new MySqlCommand("AddRingList", connection))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // 입력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@token", MySqlDbType.String));
                    cmd.Parameters["@token"].Value = token;
                    cmd.Parameters.Add(new MySqlParameter("@itemList", MySqlDbType.Text));
                    cmd.Parameters["@itemList"].Value = itemList;

                    // 출력 파라미터 설정
                    cmd.Parameters.Add(new MySqlParameter("@result", MySqlDbType.Byte));
                    cmd.Parameters["@result"].Direction = System.Data.ParameterDirection.Output;

                    // 프로시저 실행
                    cmd.ExecuteNonQuery();

                    result = Convert.ToBoolean(cmd.Parameters["@result"].Value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

        public void GetProductData(ref List<InAPP_Products> productDatas)
        {
            try
            {
                using (MySqlCommand cmd = new MySqlCommand("GetProductData", connection))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string productID = reader["ProductID"].ToString();
                            string productName = reader["ProductName"].ToString();
                            string currencyType = reader["CurrencyType"].ToString();
                            ushort price = Convert.ToUInt16(reader["Price"]);

                            InAPP_Products productData = new InAPP_Products();
                            productData.productID = productID;
                            productData.productName = productName;
                            productData.price = price;
                            productData.currencyType = currencyType switch
                            {
                                "COIN" => Products.Coin,
                                "DIA" => Products.Diamond,
                                "KRW" => Products.KRW,
                                _ => throw new ArgumentOutOfRangeException()
                            };
                            productDatas.Add(productData);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }
    }
}
