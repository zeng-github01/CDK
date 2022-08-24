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
//using UnityEngine;
using Rocket.Core.Plugins;
using CDK.Enum;
using PermissionSync;

namespace CDK
{
    public class DatabaseManager
    {
        public enum RedeemCDKResult { Success, Redeemed, KeyNotFound, MaxRedeemed, Renewed, Error, PlayerNotMatch,KeyNotValid }

        //public enum CreateCDKResult { Success,Failure,KeyExist,Error}
        internal DatabaseManager()
        {
            CheckSchema();
        }

        private bool KeyVailed(CDKData cdk)
        {
            if (cdk.Items.Length != 0 && cdk.Amount.Length != 0)
            {
                if (cdk.Items.Length != cdk.Amount.Length)
                {
                    Logger.LogError(String.Format("CDK:{0} Items and Amount Column length not equal! ", cdk.CDK));
                    return false;
                }
                for (int i = 0; i < cdk.Items.Length; i++)
                {
                    if (!ushort.TryParse(cdk.Items[i].ToString(), out ushort id))
                    {
                        Logger.LogError(String.Format("CDK:{0} has id in Items not a ushort!", cdk.CDK));
                        return false;
                    }
                }
                for (int i = 0; i < cdk.Amount.Length; i++)
                {
                    if (!byte.TryParse(cdk.Amount[i].ToString(), out byte am))
                    {
                        Logger.LogError(String.Format("CDK:{0} has amount in Amount not a byte. MAX 255!", cdk.CDK));
                        return false;
                    }
                }
            }

            return true;
        }

        public RedeemCDKResult RedeemCDK(UnturnedPlayer player, string CDK)
        {
            try
            {
                var cdkdata = GetCDKData(CDK);
                var logdata = GetLogData(player.CSteamID,ELogQueryType.ByCDK,CDK);
                if (cdkdata != null)
                {
                    if (/*cdkdata.Owner != CSteamID.Nil &&*/ cdkdata.Owner != player.CSteamID)
                    {
                        return RedeemCDKResult.PlayerNotMatch;
                    }
                    if (cdkdata.MaxRedeem.HasValue && cdkdata.RedeemedTimes >= cdkdata.MaxRedeem.Value)
                    {
                        return RedeemCDKResult.MaxRedeemed;
                    }
                    if (logdata != null && !cdkdata.Renew)
                    {
                        return RedeemCDKResult.Redeemed;
                    }
                    else if (logdata == null)
                    {
                        if (!KeyVailed(cdkdata))
                        {
                            return RedeemCDKResult.KeyNotValid;
                        }
                        //else
                        //{
                            if (cdkdata.Items.Length != 0 && cdkdata.Amount.Length == 0)
                            {

                                var items = cdkdata.Items;
                                for (int i = 0; i < items.Length; i++)
                                {
                                    player.GiveItem(ushort.Parse(items[i]), 1);
                                }
                            }
                            else if (cdkdata.Items.Length != 0 && cdkdata.Amount.Length != 0)
                            {
                                var items = cdkdata.Items;
                                var amount = cdkdata.Amount;

                                for (int i = 0; i < amount.Length; i++)
                                {
                                    if (!player.GiveItem(Convert.ToUInt16(items[i]), Convert.ToByte(amount[i])))
                                    {
                                        UnturnedChat.Say(player, Main.Instance.Translate("items_give_fail"), UnityEngine.Color.red);
                                    }
                                }
                            }

                            if (cdkdata.Vehicle != 0)
                            {
                                player.GiveVehicle(cdkdata.Vehicle.Value);
                            }
                            if (cdkdata.Reputation != 0)
                            {
                                player.Player.skills.askRep(cdkdata.Reputation.Value);
                            }
                            if (cdkdata.Experience != 0)
                            {
                                player.Experience += cdkdata.Experience.Value;
                            }
                            if (cdkdata.Money != 0)
                            {
                                Main.ExecuteDependencyCode("Uconomy", (IRocketPlugin uconomy) =>
                                {
                                    if (uconomy.State == PluginState.Loaded)
                                    {
                                        Uconomy.Instance.Database.IncreaseBalance(player.Id, cdkdata.Money.Value);
                                        UnturnedChat.Say(player, Main.Instance.Translate("uconomy_gain", Convert.ToDecimal(cdkdata.Money.Value), Uconomy.Instance.Configuration.Instance.MoneyName));
                                    }
                                });
                            }

                            if (cdkdata.GrantPermissionGroup != string.Empty && !cdkdata.UsePermissionSync)
                            {
                                switch (R.Permissions.AddPlayerToGroup(cdkdata.GrantPermissionGroup, player))
                                {
                                    case Rocket.API.RocketPermissionsProviderResult.Success:
                                        UnturnedChat.Say(player, Main.Instance.Translate("permission_granted", cdkdata.GrantPermissionGroup));
                                        break;
                                    case Rocket.API.RocketPermissionsProviderResult.DuplicateEntry:
                                        UnturnedChat.Say(player, Main.Instance.Translate("permission_duplicate_entry", cdkdata.GrantPermissionGroup), UnityEngine.Color.yellow);
                                        break;
                                    default:
                                        UnturnedChat.Say(player, Main.Instance.Translate("permission_grant_error"), UnityEngine.Color.red);
                                        break;
                                }
                            }
                            if (cdkdata.GrantPermissionGroup != string.Empty && cdkdata.UsePermissionSync)
                            {
                                Main.ExecuteDependencyCode("PermissionSync", (IRocketPlugin ps) =>
                                {
                                    if (ps.State == PluginState.Loaded)
                                    {
                                        PermissionSync.Main.Instance.databese.AddPermission("CDKPlugin", player, cdkdata.GrantPermissionGroup, cdkdata.ValidUntil.ToString());
                                    }
                                });
                            }

                            SaveLogToDB(new LogData(CDK, player.CSteamID, DateTime.Now, cdkdata.ValidUntil, cdkdata.GrantPermissionGroup, cdkdata.UsePermissionSync));
                            IncreaseRedeemedTime(CDK);
                            return RedeemCDKResult.Success;
                        //}
                    }
                    else if (logdata != null && cdkdata.Renew)
                    {
                        if (!cdkdata.UsePermissionSync)
                        {
                            R.Permissions.AddPlayerToGroup(cdkdata.GrantPermissionGroup, player);
                            UpdateLogInDB(new LogData(CDK, player.CSteamID, DateTime.Now, cdkdata.ValidUntil, cdkdata.GrantPermissionGroup, cdkdata.UsePermissionSync));
                            UpdateRenew(CDK);
                            return RedeemCDKResult.Renewed;
                        }
                        else
                        {
                            Main.ExecuteDependencyCode("PermissionSync", (IRocketPlugin ps) =>
                            {
                                if (ps.State == PluginState.Loaded)
                                {
                                    PermissionSync.Main.Instance.databese.UpdatePermission(player, cdkdata.GrantPermissionGroup, cdkdata.ValidUntil, "CDKPlugin");
                                }
                            });
                            UpdateLogInDB(new LogData(CDK, player.CSteamID, DateTime.Now, cdkdata.ValidUntil, cdkdata.GrantPermissionGroup, cdkdata.UsePermissionSync));
                            UpdateRenew(CDK);
                            return RedeemCDKResult.Renewed;
                        }
                    }
                }
                else
                {
                    return RedeemCDKResult.KeyNotFound;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            return RedeemCDKResult.Error;
        }

        internal void CheckValid(UnturnedPlayer player)
        {
            LogData logData = GetLogData(player.CSteamID,ELogQueryType.ByTime);
            if (logData != null && logData.GrantPermissionGroup != string.Empty && !logData.UsePermissionSync)
            {
                do
                {
                    CDKData cDKData = GetCDKData(logData.CDK);
                    R.Permissions.RemovePlayerFromGroup(cDKData.GrantPermissionGroup, player);
                    UnturnedChat.Say(player, Main.Instance.Translate("key_expired", logData.CDK));
                    logData = GetLogData(player.CSteamID,ELogQueryType.ByTime);
                } while (logData == null);
            }
        }

        private CDKData BuildCDKData(MySqlDataReader reader)
        {
            var cid = Convert.ToUInt64(reader[12]);
            CSteamID owner = CSteamID.Nil;
            if (cid != 0)
            {
                owner = new CSteamID(cid);
            }
            return new CDKData(reader.GetString(0), Convert.ToString(reader["Items"]), Convert.ToString(reader["Amount"]), Convert.ToUInt16(reader["Vehicle"]), Convert.ToUInt16(reader["Experience"]), Convert.ToDecimal(reader["Money"]), Convert.ToInt32(reader["Reputation"]), Convert.ToString(reader[7]), reader.GetInt32(9), reader.GetInt32(8), reader.GetDateTime(10), owner, reader.GetBoolean(11), reader.GetBoolean(13));
        }
        private LogData BuildLogData(MySqlDataReader reader)
        {
            return new LogData(reader.GetString(0), new CSteamID(Convert.ToUInt64(reader[1])), reader.GetDateTime(2), reader.GetDateTime(3),Convert.ToString(reader[4]),reader.GetBoolean(5));
        }

        public CDKData GetCDKData(string cdk)
        {
            CDKData data = null;
            MySqlConnection connection = CreateConnection();
            try
            {
                MySqlCommand command = connection.CreateCommand();
                command.Parameters.AddWithValue("@CDK", cdk);
                command.CommandText = $"SELECT * from `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` where `CDK` = @CDK;";
                connection.Open();
                MySqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    data = BuildCDKData(reader);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("[Error] GetCDKData:");
                Logger.LogException(ex);
            }
            finally
            {
                connection.Close();
            }

            return data;
        }

        public LogData GetLogData(CSteamID steamID,ELogQueryType type,string parameter = "")
        {
            LogData logData = null;
            MySqlConnection connection = CreateConnection();
            try
            {
                MySqlCommand command = connection.CreateCommand();
                switch(type)
                {
                    case ELogQueryType.ByCDK:
                        command.Parameters.AddWithValue("@steamid", steamID);
                        command.Parameters.AddWithValue("@cdk", parameter);
                        command.CommandText = $"SELECT * FROM `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` WHERE `SteamID` = @steamid AND `CDK` = @cdk;";
                        break;
                    case ELogQueryType.ByPermissionGroup:
                        command.Parameters.AddWithValue("@steamid", steamID);
                        command.Parameters.AddWithValue("@PermissionGroup", parameter);
                        command.CommandText = $"SELECT * FROM `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` WHERE `SteamID` = @steamid AND `GrantPermissionGroup` = '@PermissionGroup';";
                        break;
                    case ELogQueryType.ByTime:
                        command.Parameters.AddWithValue("@steamid", steamID);
                        command.CommandText = $"select * from `{ Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` where `SteamID` = @steamid  and `ValidUntil` < now() LIMIT 1;";
                        break;
                }
                connection.Open();
                MySqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    logData = BuildLogData(reader);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("[Error] GetLogData");
                Logger.LogException(ex);
            }
            finally
            {
                connection.Close();
            }
            return logData;
        }

        internal void SaveLogToDB(LogData logData)
        {
            int usePermissionSync;
            if(logData.UsePermissionSync)
            {
                usePermissionSync = 1;
            }
            else
            {
                usePermissionSync = 0;
            }

            ExecuteQuery(true, $"INSERT INTO `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` (CDK,SteamID,`Redeemed Time`,ValidUntil,GrantPermissionGroup,UsePermissionSync) VALUES('{logData.CDK}','{logData.SteamID}','{logData.RedeemTime}','{logData.ValidUntil}','{logData.GrantPermissionGroup}','{usePermissionSync}')");
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
            ExecuteQuery(true, $"UPDATE `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` SET `ValidUntil` = '{logData.ValidUntil}',`Redeemed Time` = '{logData.RedeemTime}',`UsePermissionSync` = '{usePermissionSync}' WHERE `SteamID` = '{logData.SteamID}' AND `CDK` = '{logData.CDK}'");
        }
        internal void UpdateRenew(string cdk)
        {
            ExecuteQuery(true, $"UPDATE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` SET `EnableRenew` = 0 WHERE `CDK` = {cdk}");
        }

        internal void IncreaseRedeemedTime(string cdk)
        {
            ExecuteQuery(true, $"UPDATE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` SET `RedeemedTimes` = RedeemedTimes +1 where `CDK` = '{cdk}' ");
        }

        public bool IsPurchased(UnturnedPlayer player, string CDK) //check player if is first purchase
        {
            bool result = false;
            var CdkData = GetCDKData(CDK);
            if (CdkData != null)
            {
                var log = GetLogData(player.CSteamID, ELogQueryType.ByPermissionGroup, CdkData.GrantPermissionGroup);
                if (log != null)
                {
                    result = true;
                }
                else
                {
                    result = false;
                }
            }
            
            return result;
        }
        internal void CheckSchema() // intial mysql table
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
            if(Main.Instance.Configuration.Instance.MySQLTableVer == 1)
            {
                ExecuteQuery(true,
                    $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` ADD `UsePermissionSync` BOOLEAN NOT NULL DEFAULT '0'");
                ExecuteQuery(true,
                    $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` ADD `UsePermissionSync` BOOLEAN NOT NULL DEFAULT '0'");
                Main.Instance.Configuration.Instance.MySQLTableVer = 2;
                Main.Instance.Configuration.Save();
            }
            if(Main.Instance.Configuration.Instance.MySQLTableVer == 2)
            {
                ExecuteQuery(true, $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` MODIFY `Items` VARCHAR(32)");
                ExecuteQuery(true, $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` MODIFY `Amount` VARCHAR(32)");
                ExecuteQuery(true, $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` MODIFY `Vehicle` INT(16) UNSIGNED");
                ExecuteQuery(true, $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` MODIFY `Experience` INT(32) ");
                ExecuteQuery(true, $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` MODIFY `Reputation` INT ");
                ExecuteQuery(true, $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` MODIFY `Money` decimal(16,2)");
                ExecuteQuery(true, $"ALTER TABLE `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` MODIFY `GrantPermissionGroup` VARCHAR(32)");
                Main.Instance.Configuration.Instance.MySQLTableVer = 3;
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
        public object ExecuteQuery(bool isScalar, string query)
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