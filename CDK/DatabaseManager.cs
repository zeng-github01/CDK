﻿using System;
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
        public enum RedeemCDKResult
        {
            Success,
            Redeemed,
            KeyNotFound,
            MaxRedeemed,
            Renewed,
            Error,
            PlayerNotMatch,
            KeyNotValid
        }

        internal DatabaseManager()
        {
            CheckSchema();
        }

        private bool KeyVailed(CDKData cdk)
        {
            if(cdk.Amount == string.Empty && cdk.Items != string.Empty)
            {
                var list = cdk.Items.Split(',').ToList();
                for (int i = 0; i < list.Count; i++)
                {
                    if (!ushort.TryParse(list[i], out ushort res))
                    {
                        Logger.LogError(String.Format("CDK:{0} has id in Items not a ushort!", cdk.CDK));
                        return false;
                    }
                }
                return true;
            }
            List<string> listitem = cdk.Items.Split(',').ToList();
            List<string> listamount = cdk.Amount.Split(',').ToList();
            if (listitem.Count != 0 && listamount.Count != 0)
            {
                if (listitem.Count != listamount.Count)
                {
                    Logger.LogError(String.Format("CDK:{0} Items and Amount Column length not equal! ", cdk.CDK));
                    return false;
                }

                for (int i = 0; i < listitem.Count; i++)
                {
                    if (!ushort.TryParse(listitem[i], out ushort id))
                    {
                        Logger.LogError(String.Format("CDK:{0} has id in Items not a ushort!", cdk.CDK));
                        return false;
                    }
                }

                for (int i = 0; i < listamount.Count; i++)
                {
                    if (!byte.TryParse(listamount[i], out byte am))
                    {
                        Logger.LogError(String.Format("CDK:{0} has amount in Amount not a byte. MAX 255!", cdk.CDK));
                        return false;
                    }
                }
            }

            return false;
        }

        public RedeemCDKResult RedeemCDK(UnturnedPlayer player, string CDK)
        {
            try
            {
                var cdkdata = GetCDKData(CDK);
                var logdata = GetLogData(player.CSteamID.m_SteamID, ELogQueryType.ByCDK, CDK);
                if (cdkdata != null)
                {
                    List<string> listItem = cdkdata.Items.Split(',').ToList();
                    List<string> listAmount = cdkdata.Amount.Split(',').ToList();
                    if (cdkdata.Owner != 0 && cdkdata.Owner != player.CSteamID.m_SteamID)
                    {
                        return RedeemCDKResult.PlayerNotMatch;
                    }

                    if (cdkdata.RedeemedTimes >= cdkdata.MaxRedeem)
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
                        if (listItem.Count != 0 && listAmount.Count == 0)
                        {
                            for (int i = 0; i < listItem.Count; i++)
                            {
                                player.GiveItem(ushort.Parse(listItem[i]), 1);
                            }
                        }
                        else if (cdkdata.Items.Length != 0 && cdkdata.Amount.Length != 0)
                        {
                           

                            for (int i = 0; i < listAmount.Count; i++)
                            {
                                if (!player.GiveItem(Convert.ToUInt16(listItem[i]), Convert.ToByte(listAmount[i])))
                                {
                                    UnturnedChat.Say(player, Main.Instance.Translate("items_give_fail"),
                                        UnityEngine.Color.red);
                                }
                            }
                        }

                        if (cdkdata.Vehicle.HasValue)
                        {
                            player.GiveVehicle(cdkdata.Vehicle.Value);
                        }

                        if (cdkdata.Reputation.HasValue)
                        {
                            player.Player.skills.askRep(cdkdata.Reputation.Value);
                        }

                        if (cdkdata.Experience.HasValue)
                        {
                            player.Experience += cdkdata.Experience.Value;
                        }

                        if (cdkdata.Money.HasValue)
                        {
                            Main.ExecuteDependencyCode("Uconomy", (IRocketPlugin uconomy) =>
                            {
                                if (uconomy.State == PluginState.Loaded)
                                {
                                    Uconomy.Instance.Database.IncreaseBalance(player.Id, cdkdata.Money.Value);
                                    UnturnedChat.Say(player,
                                        Main.Instance.Translate("uconomy_gain", Convert.ToDecimal(cdkdata.Money.Value),
                                            Uconomy.Instance.Configuration.Instance.MoneyName));
                                }
                            });
                        }

                        if (cdkdata.GrantPermissionGroup != string.Empty && !cdkdata.UsePermissionSync)
                        {
                            switch (R.Permissions.AddPlayerToGroup(cdkdata.GrantPermissionGroup, player))
                            {
                                case Rocket.API.RocketPermissionsProviderResult.Success:
                                    UnturnedChat.Say(player,
                                        Main.Instance.Translate("permission_granted", cdkdata.GrantPermissionGroup));
                                    break;
                                case Rocket.API.RocketPermissionsProviderResult.DuplicateEntry:
                                    UnturnedChat.Say(player,
                                        Main.Instance.Translate("permission_duplicate_entry",
                                            cdkdata.GrantPermissionGroup), UnityEngine.Color.yellow);
                                    break;
                                default:
                                    UnturnedChat.Say(player, Main.Instance.Translate("permission_grant_error"),
                                        UnityEngine.Color.red);
                                    break;
                            }
                        }
                        else if (cdkdata.GrantPermissionGroup != string.Empty && cdkdata.UsePermissionSync)
                        {
                            Main.ExecuteDependencyCode("PermissionSync", (IRocketPlugin ps) =>
                            {
                                if (ps.State == PluginState.Loaded)
                                {
                                    PermissionSync.Main.Instance.databese.AddPermission("CDKPlugin", player,
                                        cdkdata.GrantPermissionGroup, cdkdata.ValidUntil.ToString());
                                }
                            });
                        }

                        SaveLogToDB(new LogData(CDK, player.CSteamID.m_SteamID, DateTime.Now, cdkdata.ValidUntil,
                            cdkdata.GrantPermissionGroup, cdkdata.UsePermissionSync));
                        IncreaseRedeemedTime(CDK);
                        return RedeemCDKResult.Success;
                        //}
                    }
                    else if (logdata != null && cdkdata.Renew)
                    {
                        if (!cdkdata.UsePermissionSync)
                        {
                            R.Permissions.AddPlayerToGroup(cdkdata.GrantPermissionGroup, player);
                            UpdateLogInDB(new LogData(CDK, player.CSteamID.m_SteamID, DateTime.Now, cdkdata.ValidUntil,
                                cdkdata.GrantPermissionGroup, cdkdata.UsePermissionSync));
                            UpdateRenew(CDK);
                            return RedeemCDKResult.Renewed;
                        }
                        else
                        {
                            Main.ExecuteDependencyCode("PermissionSync", (IRocketPlugin ps) =>
                            {
                                if (ps.State == PluginState.Loaded)
                                {
                                    PermissionSync.Main.Instance.databese.UpdatePermission(player,
                                        cdkdata.GrantPermissionGroup, cdkdata.ValidUntil, "CDKPlugin");
                                }
                            });
                            UpdateLogInDB(new LogData(CDK, player.CSteamID.m_SteamID, DateTime.Now, cdkdata.ValidUntil,
                                cdkdata.GrantPermissionGroup, cdkdata.UsePermissionSync));
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
            LogData logData = GetLogData(player.CSteamID.m_SteamID, ELogQueryType.ByTime);
            if (logData != null && logData.GrantPermissionGroup != string.Empty && !logData.UsePermissionSync)
            {
                do
                {
                    CDKData cDKData = GetCDKData(logData.CDK);
                    R.Permissions.RemovePlayerFromGroup(cDKData.GrantPermissionGroup, player);
                    UnturnedChat.Say(player, Main.Instance.Translate("key_expired", logData.CDK));
                    logData = GetLogData(player.CSteamID.m_SteamID, ELogQueryType.ByTime);
                } while (logData == null);
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

        public LogData GetLogData(ulong steamid, ELogQueryType type, string keyword = "")
        {
            LogData logData = null;
            var con = CreateConnection();
            try
            {
                var parameter = new DynamicParameters();
                parameter.Add("@SteamID", steamid);
                switch (type)
                {
                    case ELogQueryType.ByCDK:
                        parameter.Add("@CDK", keyword);
                        logData = con.QueryFirstOrDefault<LogData>(
                            $"SELECT * FROM `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` where `SteamID` = @SteamID and `CDK` = @CDK", parameter);
                        break;
                    case ELogQueryType.ByTime:
                        logData = con.QueryFirstOrDefault<LogData>(
                            $"SELECT * FROM `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` where `SteamID` = @SteamID and ValidUntil < now()",parameter);
                        break;
                    case ELogQueryType.ByPermissionGroup:
                        parameter.Add("@permission", keyword);
                        logData = con.QueryFirstOrDefault<LogData>(
                            $"SELECT * FROM `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` WHERE `SteamID` = @SteamID and `GrantPermissionGroup` = @permission", parameter);
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

            return logData;
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

        public bool IsPurchased(UnturnedPlayer player, string CDK) //check player if is first purchase
        {
            bool result = false;
            var CdkData = GetCDKData(CDK);
            if (CdkData != null)
            {
                var log = GetLogData(player.CSteamID.m_SteamID, ELogQueryType.ByPermissionGroup, CdkData.GrantPermissionGroup);
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