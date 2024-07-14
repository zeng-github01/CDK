using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Rocket.Core.Logging;
using Rocket.Unturned.Player;
using Rocket.Unturned.Chat;
using Rocket.Core;
using fr34kyn01535.Uconomy;
using Rocket.API;
using CDK.Data;
using Steamworks;
using Rocket.Core.Plugins;
using CDK.Enum;
using PermissionSync;
using Dapper;

namespace CDK
{
    public class DatabaseManager
    {

        internal DatabaseManager()
        {
            CheckSchema();
        }

        internal void CheckValid(UnturnedPlayer player)
        {
            var logList = GetLogData(player.CSteamID.m_SteamID, ELogQueryType.ByTime);
            foreach(LogData log in logList)
            {
                if (log.GrantPermissionGroup != string.Empty && !log.UsePermissionSync)
                {
                    CDKData cDKData = GetCDKData(log.CDK);
                    R.Permissions.RemovePlayerFromGroup(cDKData.GrantPermissionGroup, player);
                    UnturnedChat.Say(player, Main.Instance.Translate("key_expired", log.CDK));
                }
            }
        }

       
        public CDKData GetCDKData(string key)
        {
            CDKData cdkData = null;
            var con = CreateConnection();

            try
            {
                var parameter = new DynamicParameters();
                parameter.Add("@CDK", key);
                cdkData = con.QueryFirstOrDefault<CDKData>(
                    $"SELECT * from `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` where `CDK` = @CDK;", parameter);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            finally
            {
                con.Close();
            }

            return cdkData;
        }

        public List<LogData> GetLogData(ulong steamid, ELogQueryType type, string keyword = "")
        {
            List<LogData> logDatas = new List<LogData>();
            var con = CreateConnection();
            try
            {
                var parameter = new DynamicParameters();
                parameter.Add("@SteamID", steamid);
                switch (type)
                {
                    case ELogQueryType.ByCDK:
                        parameter.Add("@CDK", keyword);
                        logDatas = con.Query<LogData>(
                            $"SELECT * FROM `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` where `SteamID` = @SteamID and `CDK` = @CDK", parameter).AsList();
                        break;
                    case ELogQueryType.ByTime:
                        logDatas = con.Query<LogData>(
                            $"SELECT * FROM `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` where `SteamID` = @SteamID and ValidUntil < now()",parameter).AsList();
                        break;
                    case ELogQueryType.ByPermissionGroup:
                        parameter.Add("@permission", keyword);
                        logDatas = con.Query<LogData>(
                            $"SELECT * FROM `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` WHERE `SteamID` = @SteamID and `GrantPermissionGroup` = @permission", parameter).AsList();
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.LogException(e);
            }
            finally
            {
                con.Close();
            }

            return logDatas;
        }

        internal void SaveLogToDB(LogData logData)
        {
            int usePermissionSync;
            if (logData.UsePermissionSync)
            {
                usePermissionSync = 1;
            }
            else
            {
                usePermissionSync = 0;
            }

            ExecuteQuery(true,
                $"INSERT INTO `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` (CDK,SteamID,`Redeemed Time`,ValidUntil,GrantPermissionGroup,UsePermissionSync) VALUES('{logData.CDK}','{logData.SteamID}','{logData.RedeemTime}','{logData.ValidUntil}','{logData.GrantPermissionGroup}','{usePermissionSync}')");
        }

        internal void UpdateLogInDB(LogData logData)
        {
            int usePermissionSync;
            if (logData.UsePermissionSync)
            {
                usePermissionSync = 1;
            }
            else
            {
                usePermissionSync = 0;
            }

            ExecuteQuery(true,
                $"UPDATE `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` SET `ValidUntil` = '{logData.ValidUntil}',`Redeemed Time` = '{logData.RedeemTime}',`UsePermissionSync` = '{usePermissionSync}' WHERE `SteamID` = '{logData.SteamID}' AND `CDK` = '{logData.CDK}'");
        }

        internal void UpdateRenew(string cdk)
        {
            ExecuteQuery(true,
                $"UPDATE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` SET `EnableRenew` = 0 WHERE `CDK` = {cdk}");
        }

        internal void IncreaseRedeemedTime(string cdk)
        {
            ExecuteQuery(true,
                $"UPDATE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` SET `RedeemedTimes` = RedeemedTimes +1 where `CDK` = '{cdk}' ");
        }

        internal bool IsPurchased(UnturnedPlayer player, string CDK) //check player if is first purchase
        {
            bool result = false;
            var CdkData = GetCDKData(CDK);
            if (CdkData != null && !string.IsNullOrEmpty(CdkData.GrantPermissionGroup))
            {
                var logList = GetLogData(player.CSteamID.m_SteamID, ELogQueryType.ByPermissionGroup, CdkData.GrantPermissionGroup);
                if(logList.Count > 0)
                {
                    result = true;
                }
            }

            return result;
        }

        private void CheckSchema() // intial mysql table
        {
            var cdk = ExecuteQuery(true,
                $"show tables like '{Main.Instance.Configuration.Instance.DatabaseCDKTableName}'");

            if (cdk == null)
                ExecuteQuery(false,
                    $"CREATE TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` (`CDK` varchar(32) NOT NULL,`Items` varchar(32) , `Amount` varchar(32), `Vehicle` int(16) UNSIGNED, `Experience` int(32), `Reputation` int , `Money` decimal(16,2), `GrantPermissionGroup` varchar(32), `MaxRedeem` int(32) NOT NULL DEFAULT '1', `RedeemedTimes` int(6) NOT NULL DEFAULT '0', `ValidUntil` datetime(6) NOT NULL DEFAULT '{DateTime.MaxValue}', `EnableRenew` BOOLEAN NOT NULL DEFAULT '0', `Owner` BIGINT, `UsePermissionSync` BOOLEAN NOT NULL DEFAULT '0',PRIMARY KEY (`CDK`))");

            var log = ExecuteQuery(true,
                $"show tables like '{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}'");

            if (log == null)
                ExecuteQuery(false,
                    $"CREATE TABLE `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` (`CDK` varchar(32) NOT NULL, `SteamID` varchar(32) NOT NULL, `Redeemed Time` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP, `ValidUntil` datetime(6) NOT NULL, `GrantPermissionGroup` VARCHAR(32), `UsePermissionSync` BOOLEAN NOT NULL DEFAULT '0')");
            if (Main.Instance.Configuration.Instance.MySQLTableVer == 1)
            {
                ExecuteQuery(true,
                    $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` ADD `UsePermissionSync` BOOLEAN NOT NULL DEFAULT '0'");
                ExecuteQuery(true,
                    $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` ADD `UsePermissionSync` BOOLEAN NOT NULL DEFAULT '0'");
                Main.Instance.Configuration.Instance.MySQLTableVer = 2;
                Main.Instance.Configuration.Save();
            }

            if (Main.Instance.Configuration.Instance.MySQLTableVer == 2)
            {
                ExecuteQuery(true,
                    $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` MODIFY `Items` VARCHAR(32)");
                ExecuteQuery(true,
                    $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` MODIFY `Amount` VARCHAR(32)");
                ExecuteQuery(true,
                    $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` MODIFY `Vehicle` INT(16) UNSIGNED");
                ExecuteQuery(true,
                    $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` MODIFY `Experience` INT(32) ");
                ExecuteQuery(true,
                    $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` MODIFY `Reputation` INT ");
                ExecuteQuery(true,
                    $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` MODIFY `Money` decimal(16,2)");
                ExecuteQuery(true,
                    $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` MODIFY `GrantPermissionGroup` VARCHAR(32)");
                Main.Instance.Configuration.Instance.MySQLTableVer = 3;
                Main.Instance.Configuration.Save();
            }

            if (Main.Instance.Configuration.Instance.MySQLTableVer == 3)
            {
                ExecuteQuery(true,
                    $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` MODIFY `GrantPermissionGroup` VARCHAR(32)");
                ExecuteQuery(true,
                    $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` MODIFY `Owner` BIGINT UNSIGNED");
                Main.Instance.Configuration.Instance.MySQLTableVer = 4;
                Main.Instance.Configuration.Save();
            }

            if (Main.Instance.Configuration.Instance.MySQLTableVer == 4)
            {
                ExecuteQuery(true,
                    $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` ADD foreign key(`CDK`) references {Main.Instance.Configuration.Instance.DatabaseCDKTableName}(`CDK`) on DELETE cascade on update cascade ");
                Main.Instance.Configuration.Instance.MySQLTableVer = 5;
                Main.Instance.Configuration.Save();
            }

            if(Main.Instance.Configuration.Instance.MySQLTableVer == 5)
            {
                ExecuteQuery(true,
                    $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` MODIFY `Vehicle` VARCHAR(128)");
                Main.Instance.Configuration.Instance.MySQLTableVer++;
                Main.Instance.Configuration.Save();
            }
        }

        private MySqlConnection CreateConnection()
        {
            MySqlConnection connection = null;
            try
            {
                if (Main.Instance.Configuration.Instance.DatabasePort == 0)
                    Main.Instance.Configuration.Instance.DatabasePort = 3306;
                connection = new MySqlConnection(
                    $"SERVER={Main.Instance.Configuration.Instance.DatabaseAddress};DATABASE={Main.Instance.Configuration.Instance.DatabaseName};UID={Main.Instance.Configuration.Instance.DatabaseUsername};PASSWORD={Main.Instance.Configuration.Instance.DatabasePassword};PORT={Main.Instance.Configuration.Instance.DatabasePort};");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return connection;
        }

        /// <summary>
        /// Executes a MySql query.
        /// </summary>
        /// <param name="isScalar">If the query is expected to return a value.</param>
        /// <param name="query">The query to execute.</param>
        /// <returns>The value if isScalar is true, null otherwise.</returns>
        private object ExecuteQuery(bool isScalar, string query)
        {
            // This method is to reduce the amount of copy paste that there was within this class.
            // Initiate result and connection globally instead of within TryCatch context.
            var connection = CreateConnection();
            object result = null;

            try
            {
                // Initialize command within try context, and execute within it as well.
                var command = connection.CreateCommand();
                command.CommandText = query;
                connection.Open();
                if (isScalar)
                    result = command.ExecuteScalar();
                else
                    command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                // Catch and log any errors during execution, like connection or similar.
                Logger.LogException(ex);
            }
            finally
            {
                // No matter what happens, close the connection at the end of execution.+


                connection.Close();
            }

            return result;
        }
    }
}