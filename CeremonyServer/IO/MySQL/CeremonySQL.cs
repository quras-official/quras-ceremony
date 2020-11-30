using System;
using System.Collections.Generic;
using Ceremony.IO.Json;
using Ceremony.Wallets;
using CeremonyServer;
using MySql.Data.MySqlClient;

namespace Ceremony.IO.MySQL
{
    class CeremonySQL : IDisposable
    {
        public const string SQL_SIGN_UP = "INSERT INTO tbl_user(name, email, password, address, pubkey, country, `order`, created_at, updated_at) VALUES(@name, @email, MD5(@password), @address, @pubkey, @country, @order, NOW(), NOW())";
        public const string SQL_SING_IN = "SELECT * FROM tbl_user WHERE email = @email AND password = MD5(@password);";
        public const string SQL_GET_USER_BY_PUBKEY = "SELECT * FROM tbl_user WHERE pubkey = @pubkey";
        public const string SQL_GET_PARTICIPANTS_CNT_BY_STATUS = "SELECT Count(id) From tbl_user WHERE status = @status";

        private MySqlConnection connector;
        private MySqlCommand command;

        public CeremonySQL ()
        {
            InitializeMysql();
        }

        private void InitializeMysql()
        {
            if (connect("server", "user", "password", "quras_ceremony_db"))
            {
                LogUtil.Default.Log("Database connection successed.");
            }
            else
            {
                LogUtil.Default.Log("Database connection failed.");
            }
        }

        public bool connect(string address, string userid, string password, string database)
        {
            try
            {
                string connectParam = @"server=" + address + ";userid=" + userid + ";password=" + password + ";database=" + database;
                connector = new MySqlConnection(connectParam);
                connector.Open();
                command = new MySqlCommand();
                command.Connection = connector;
                return true;

            }
            catch(Exception ex)
            {
                LogUtil.Default.Log(ex.ToString());
                return false;
            }
        }

        public CeremonySQL reconnect()
        {
            try
            {
                Dispose();
                InitializeMysql();
                return this;
            }
            catch (Exception ex)
            {
                LogUtil.Default.Log(ex.ToString());
                return null;
            }
        }

        public bool SignUpUser(JObject param)
        {
            LogUtil.Default.Log("Signning Up... : " + param.ToString());
            try
            {
                int index = 1;
                using (CeremonySQL temp = new CeremonySQL())
                {
                    index = temp.GetUserIndex() + 1;
                }
                command.CommandText = SQL_SIGN_UP;
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@name", param["username"].AsString());
                command.Parameters.AddWithValue("@email", param["email"].AsString());
                command.Parameters.AddWithValue("@password", param["password"].AsString());
                command.Parameters.AddWithValue("@address", param["address"].AsString());
                command.Parameters.AddWithValue("@pubkey", param["pubkey"].AsString());
                command.Parameters.AddWithValue("@country", param["country"].AsString());
                command.Parameters.AddWithValue("@order", index);
                command.ExecuteNonQuery();

                LogUtil.Default.Log("Signning Up Successe.");
                return true;
            }
            catch (Exception ex)
            {
                LogUtil.Default.Log("Signning Up Failed.");
                LogUtil.Default.Log(ex.ToString());
                return false;
            }
        }

        public User SignInUser(JObject param)
        {
            User ret = new User();
            MySqlDataReader rdr = null;

            try
            {
                command.CommandText = SQL_SING_IN;
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@email", param["email"].AsString());
                command.Parameters.AddWithValue("@password", param["password"].AsString());

                rdr = command.ExecuteReader();

                if (!rdr.Read())
                {
                    throw new Exception("No Selected User");
                }

                ret.Id = rdr.GetInt32("id");
                ret.Name = rdr.GetString("name");
                ret.Email = rdr.GetString("email");
                ret.Address = rdr.GetString("address");
                ret.Pubkey = rdr.GetString("pubkey");
                ret.Country = rdr.GetString("country");
                ret.Order = rdr.GetInt32("order");
                ret.Status = (UserStatus)rdr.GetInt32("status");
                ret.SpentTime = rdr.GetInt32("spent_time");
                //ret.UserType = rdr.GetUInt32("user_type");
                //ret.ScheduleTime = ((DateTime)rdr["schedule_time"]).ToTimestamp();
                ret.CreatedAt = ((DateTime) rdr["created_at"]).ToTimestamp();
                ret.UpdatedAt = ((DateTime) rdr["updated_at"]).ToTimestamp();
            }
            catch (Exception ex)
            {
                LogUtil.Default.Log(ex.ToString());
                ret = null;
            }
            finally
            {
                if (rdr != null && !rdr.IsClosed)
                {
                    rdr.Close();
                }
            }

            return ret;
        }

        public int GetUserIndex()
        {
            LogUtil.Default.Log("Getting User Index...");
            try
            {
                command.CommandText = "SELECT COUNT(id) FROM tbl_user";
                var response = command.ExecuteScalar().ToString();
                LogUtil.Default.Log("Get USer Index: " + response);
                //return true;
                return int.Parse(response);
            }
            catch (Exception ex)
            {
                LogUtil.Default.Log(ex.ToString());
                return 0;
            }
        }
        
        public int GetSelectedUserIndex()
        {
            LogUtil.Default.Log("Getting Selected User Index...");
            try
            {
                command.CommandText = "SELECT `order` FROM tbl_user WHERE status = 1";
                var response = command.ExecuteScalar().ToString();
                LogUtil.Default.Log("Get Selected USer Index: " + response);
                return int.Parse(response);
            }
            catch (Exception ex)
            {
                LogUtil.Default.Log(ex.ToString());
                return 0;
            }
        }

        public CeremonyStatus GetCeremonyStatus()
        {
            LogUtil.Default.Log("Getting Ceremony Status...");
            try
            {
                command.CommandText = "SELECT status From tbl_status WHERE id = 1";
                var response = command.ExecuteScalar().ToString();
                LogUtil.Default.Log("Get Ceremony Status: " + response);
                return (CeremonyStatus)int.Parse(response);
            }
            catch (Exception ex)
            {
                LogUtil.Default.Log(ex.ToString());
                return 0;
            }
        }

        public int GetSegmentNumber()
        {
            LogUtil.Default.Log("Getting Ceremony Status...");
            try
            {
                command.CommandText = "SELECT segment_num From tbl_status WHERE id = 1";
                var response = command.ExecuteScalar().ToString();
                LogUtil.Default.Log("Get Segment Number: " + response);
                return int.Parse(response);
            }
            catch (Exception ex)
            {
                LogUtil.Default.Log(ex.ToString());
                return 0;
            }
        }

        public User GetSelectedUser()
        {
            User ret = new User();
            MySqlDataReader rdr = null;

            try
            {
                command.CommandText = "SELECT * From tbl_user WHERE status = 1";

                rdr = command.ExecuteReader();

                if (!rdr.Read())
                {
                    throw new Exception("No Selected User");
                }

                ret.Id = rdr.GetInt32("id");
                ret.Name = rdr.GetString("name");
                ret.Email = rdr.GetString("email");
                ret.Address = rdr.GetString("address");
                ret.Pubkey = rdr.GetString("pubkey");
                ret.Country = rdr.GetString("country");
                ret.Order = rdr.GetInt32("order");
                ret.Status = (UserStatus) rdr.GetInt32("status");
                ret.SpentTime = rdr.GetInt32("spent_time");
                ret.CreatedAt = ((DateTime)rdr["created_at"]).ToTimestamp();
                ret.UpdatedAt = ((DateTime)rdr["updated_at"]).ToTimestamp();
            }
            catch (Exception ex)
            {
                LogUtil.Default.Log(ex.ToString());
                ret = null;
            }
            finally
            {
                if (rdr != null && !rdr.IsClosed)
                {
                    rdr.Close();
                }
            }

            return ret;
        }

        public User GetUserByPubkey(string pubkey)
        {
            User ret = new User();
            MySqlDataReader rdr = null;

            try
            {
                command.CommandText = SQL_GET_USER_BY_PUBKEY;
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@pubkey", pubkey);

                rdr = command.ExecuteReader();

                if (!rdr.Read())
                {
                    throw new Exception("No User");
                }

                ret.Id = rdr.GetInt32("id");
                ret.Name = rdr.GetString("name");
                ret.Email = rdr.GetString("email");
                ret.Address = rdr.GetString("address");
                ret.Pubkey = rdr.GetString("pubkey");
                ret.Country = rdr.GetString("country");
                ret.Order = rdr.GetInt32("order");
                ret.Status = (UserStatus)rdr.GetInt32("status");
                ret.SpentTime = rdr.GetInt32("spent_time");
                ret.CreatedAt = ((DateTime)rdr["created_at"]).ToTimestamp();
                ret.UpdatedAt = ((DateTime)rdr["updated_at"]).ToTimestamp();
            }
            catch (Exception ex)
            {
                LogUtil.Default.Log(ex.ToString());
                ret = null;
            }
            finally
            {
                if (rdr != null && !rdr.IsClosed)
                {
                    rdr.Close();
                }
            }

            return ret;
        }

        public int GetParticipantCntByStatus(UserStatus status)
        {
            try
            {
                command.CommandText = SQL_GET_PARTICIPANTS_CNT_BY_STATUS;
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@status", status);
                var response = command.ExecuteScalar().ToString();
                return int.Parse(response);
            }
            catch (Exception ex)
            {
                LogUtil.Default.Log(ex.ToString());
                return 0;
            }
        }

        public bool StartCeremony()
        {
            KeyPair NewKeyPair = Wallet.CreateKey();
            LogUtil.Default.Log("Updating ceremony status : " + ((int)CeremonyStatus.CEREMONY_STATUS_STARTED));

            MySqlTransaction transaction = connector.BeginTransaction();
            try
            {
                command.CommandText = "UPDATE tbl_status " +
                    "SET status = " + ((int)CeremonyStatus.CEREMONY_STATUS_STARTED) + ", started_at = NOW(), prev_participant_time = NOW() ," +
                    " pubkey = '" + NewKeyPair.PublicKey.ToString() + "', privkey = '" + NewKeyPair.PrivateKey.ToHexString() + "' " +
                    "WHERE id = 1";
                command.ExecuteNonQuery();

                command.CommandText = "UPDATE tbl_user " +
                    "SET status = " + ((int)UserStatus.USER_STATUS_OPERATING) + ", updated_at = NOW() where `order` = 1";
                command.ExecuteNonQuery();

                transaction.Commit();

                LogUtil.Default.Log("Updating ceremony status successed.");
                return true;
            }
            catch (Exception ex)
            {
                LogUtil.Default.Log("Updating ceremony status failed.");
                LogUtil.Default.Log(ex.ToString());
                transaction.Rollback();
                return false;
            }
        }

        public uint GetLastParticipantTime()
        {
            try
            {
                command.CommandText = "SELECT prev_participant_time FROM tbl_status WHERE id = 1";
                var response = ((DateTime)command.ExecuteScalar()).ToTimestamp();

                return response;
            }
            catch (Exception ex)
            {
                LogUtil.Default.Log(ex.ToString());
                return 0;
            }
        }

        public bool ToNextUser()
        {
            int selectedIndex = GetSelectedUserIndex();
            LogUtil.Default.Log("To next user: index = " + GetSelectedUserIndex());

            MySqlTransaction transaction = connector.BeginTransaction();
            try
            {
                long spentTime = 0;
                DateTime now = DateTime.Now;
                spentTime = DateTimeUtil.Default.GetTimeStamp(now) - GetLastParticipantTime();

                command.CommandText = "UPDATE tbl_user " +
                    "SET status = " + ((int)UserStatus.USER_STATUS_SUCCESSED) + ", updated_at = NOW(), spent_time = " + spentTime +
                    " WHERE `order` = " + selectedIndex;
                command.ExecuteNonQuery();

                command.CommandText = "UPDATE tbl_user " +
                    "SET status = " + ((int)UserStatus.USER_STATUS_OPERATING) + ", updated_at = NOW() " +
                    "WHERE `order` = " + (selectedIndex + 1);
                command.ExecuteNonQuery();

                command.CommandText = "UPDATE tbl_status " +
                    "SET prev_participant_time = NOW() WHERE id = 1";
                command.ExecuteNonQuery();

                transaction.Commit();

                LogUtil.Default.Log("To next user action successed.");

                return true;
            }
            catch (Exception ex)
            {
                LogUtil.Default.Log("To next user action failed.");
                LogUtil.Default.Log(ex.ToString());
                transaction.Rollback();
                return false;
            }
        }

        public bool UpdateSegmentNumber(int nSegmentNumber)
        {
            LogUtil.Default.Log("Update segment number: number = " + nSegmentNumber);

            MySqlTransaction transaction = connector.BeginTransaction();
            try
            {
                command.CommandText = "UPDATE tbl_status " +
                    "SET segment_num = " + nSegmentNumber + " WHERE id = 1";
                command.ExecuteNonQuery();

                transaction.Commit();

                LogUtil.Default.Log("Update segment number action successed.");

                return true;
            }
            catch (Exception ex)
            {
                LogUtil.Default.Log("Update segment number action failed.");
                LogUtil.Default.Log(ex.ToString());
                transaction.Rollback();
                return false;
            }
        }

        public bool ToNextUser(int userid)
        {
            LogUtil.Default.Log("To next user: index = " + GetSelectedUserIndex());

            MySqlTransaction transaction = connector.BeginTransaction();
            try
            {
                long spentTime = 0;
                DateTime now = DateTime.Now;
                spentTime = DateTimeUtil.Default.GetTimeStamp(now) - GetLastParticipantTime();

                command.CommandText = "UPDATE tbl_user " +
                    "SET status = " + ((int)UserStatus.USER_STATUS_SUCCESSED) + ", updated_at = NOW(), spent_time = " + spentTime +
                    " WHERE id = " + userid;
                command.ExecuteNonQuery();
                
                command.CommandText = "UPDATE tbl_status " +
                    "SET prev_participant_time = NOW() WHERE id = 1";
                command.ExecuteNonQuery();

                transaction.Commit();

                LogUtil.Default.Log("To next user action successed.");

                return true;
            }
            catch (Exception ex)
            {
                LogUtil.Default.Log("To next user action failed.");
                LogUtil.Default.Log(ex.ToString());
                transaction.Rollback();
                return false;
            }
        }

        public bool TimeOutUser()
        {
            int selectedIndex = GetSelectedUserIndex();
            LogUtil.Default.Log("Timeout user: index = " + selectedIndex);

            MySqlTransaction transaction = connector.BeginTransaction();
            try
            {
                command.CommandText = "UPDATE tbl_user " +
                    "SET status = " + ((int)UserStatus.USER_STATUS_TIMEOUT) + ", updated_at = NOW() " +
                    "WHERE `order` = " + selectedIndex;
                command.ExecuteNonQuery();

                command.CommandText = "UPDATE tbl_user " +
                    "SET status = " + ((int)UserStatus.USER_STATUS_OPERATING) + ", updated_at = NOW() " +
                    "WHERE `order` = " + (selectedIndex + 1);
                command.ExecuteNonQuery();

                command.CommandText = "UPDATE tbl_status " +
                    "SET prev_participant_time = NOW() WHERE id = 1";
                command.ExecuteNonQuery();

                transaction.Commit();

                LogUtil.Default.Log("Timeout user action successed.");

                return true;
            }
            catch(Exception ex)
            {
                LogUtil.Default.Log("Timeout user action failed.");
                LogUtil.Default.Log(ex.ToString());
                transaction.Rollback();
                return false;
            }
        }

        public bool CompleteCeremony()
        {
            LogUtil.Default.Log("Updating ceremony status : " + ((int)CeremonyStatus.CEREMONY_STATUS_COMPLETED));
            MySqlTransaction transaction = connector.BeginTransaction();
            try
            {
                long spentTime = 0;
                DateTime now = DateTime.Now;
                spentTime = DateTimeUtil.Default.GetTimeStamp(now) - GetLastParticipantTime();

                command.CommandText = "UPDATE tbl_status " +
                    "SET status = " + ((int)CeremonyStatus.CEREMONY_STATUS_COMPLETED) + ", completed_at = NOW() " +
                    "WHERE id = 1";
                command.ExecuteNonQuery();

                transaction.Commit();

                LogUtil.Default.Log("Updating ceremony status successed.");
                return true;
            }
            catch (Exception ex)
            {
                LogUtil.Default.Log("Updating ceremony status failed.");
                LogUtil.Default.Log(ex.ToString());
                transaction.Rollback();
                return false;
            }
        }

        public List<User> GetAllUser()
        {
            MySqlDataReader rdr = null;
            MySqlTransaction transaction = connector.BeginTransaction();
            List<User> list = new List<User>();

            try
            {
                command.CommandText = "SELECT * From tbl_user WHERE status=" + (int)UserStatus.USER_STATUS_SUCCESSED;

                rdr = command.ExecuteReader();

                while (rdr.Read())
                {
                    User ret = new User();
                    ret.Id = rdr.GetInt32("id");
                    ret.Name = rdr.GetString("name");
                    ret.Email = rdr.GetString("email");
                    ret.Address = rdr.GetString("address");
                    ret.Pubkey = rdr.GetString("pubkey");
                    ret.Country = rdr.GetString("country");
                    ret.Order = rdr.GetInt32("order");
                    ret.Status = (UserStatus)rdr.GetInt32("status");
                    ret.SpentTime = rdr.GetInt32("spent_time");
                    ret.CreatedAt = ((DateTime)rdr["created_at"]).ToTimestamp();
                    ret.UpdatedAt = ((DateTime)rdr["updated_at"]).ToTimestamp();

                    list.Add(ret);
                }
            }
            catch (Exception ex)
            {
                LogUtil.Default.Log(ex.ToString());
                list = null;
            }
            finally
            {
                if (rdr != null && !rdr.IsClosed)
                {
                    rdr.Close();
                }
            }

            return list;
        }

        public void Dispose()
        {
            if (connector != null && connector.State == System.Data.ConnectionState.Open)
            {
                connector.Close();
            }

            connector = null;
        }
    }
}
