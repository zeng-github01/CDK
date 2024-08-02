using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rocket.API;
using Rocket.Unturned.Player;
using Steamworks;
using Rocket.Unturned.Chat;
using CDK.Data;
using CDK.Enum;
using fr34kyn01535.Uconomy;
using Rocket.Core;
using Rocket.Core.Logging;
using SDG.Unturned;

namespace CDK
{
  public class CDKCommand : IRocketCommand
    {
        public string Name => "CDK";

        public string Help => "";

        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Syntax => "CDK <Key>";

        public List<string> Permissions => new List<string>() { "CDK" };

        public List<string> Aliases => new List<string>();

        public void Execute(IRocketPlayer caller,string[] args)
        {
            var isRich = Main.Instance.Configuration.Instance.EnableRichText;
            if(args.Length == 1)
              {
                if (!Main.Instance.Database.IsPurchased(UnturnedPlayer.FromName(caller.DisplayName), args[0]))
                {
                    switch (RedeemCDK(UnturnedPlayer.FromName(caller.DisplayName), args[0]))
                    {
                        case EKeyReemeResult.Success:
                            UnturnedChat.Say(caller, Main.Instance.Translate("success"), isRich);
                            break;
                        case EKeyReemeResult.Redeemed:
                            UnturnedChat.Say(caller, Main.Instance.Translate("already_redeemed"), UnityEngine.Color.red, isRich);
                            break;
                        case EKeyReemeResult.KeyNotFound:
                            UnturnedChat.Say(caller, Main.Instance.Translate("key_dones't_exist"), UnityEngine.Color.red, isRich);
                            break;
                        case EKeyReemeResult.MaxRedeemed:
                            UnturnedChat.Say(caller, Main.Instance.Translate("maxcount_reached"), UnityEngine.Color.red, isRich);
                            break;
                        case EKeyReemeResult.Renewed:
                            UnturnedChat.Say(caller, Main.Instance.Translate("key_renewed"), isRich);
                            break;
                        case EKeyReemeResult.Error:
                            UnturnedChat.Say(caller, Main.Instance.Translate("error"), UnityEngine.Color.red, isRich);
                            break;
                        case EKeyReemeResult.PlayerNotMatch:
                            UnturnedChat.Say(caller, Main.Instance.Translate("player_not_match"), UnityEngine.Color.red, isRich);
                            break;
                        case EKeyReemeResult.KeyNotValid:
                            UnturnedChat.Say(caller, Main.Instance.Translate("cdk_config_error"), UnityEngine.Color.red, isRich);
                            break;
                    }
                }
                else
                {
                    UnturnedChat.Say(caller, Main.Instance.Translate("already_purchased"), UnityEngine.Color.red, isRich);
                }
             }
            else
            {
                UnturnedChat.Say(caller, Main.Instance.Translate("invalid_param",Syntax), UnityEngine.Color.red, isRich);
            }
        }

        private EKeyReemeResult RedeemCDK(UnturnedPlayer player, string CDK)
        {
            var isRich = Main.Instance.Configuration.Instance.EnableRichText;
            var Database = Main.Instance.Database;
            try
            {
                var cdkdata = Database.GetCDKData(CDK);
                var logdata = Database.GetLogData(player.CSteamID.m_SteamID, ELogQueryType.ByCDK, CDK);
                if (cdkdata != null)
                {
                    List<string> listItem = cdkdata.Items.Split(',').ToList();
                    List<string> listAmount = cdkdata.Amount.Split(',').ToList();
                    if (cdkdata.Owner != 0 && cdkdata.Owner != player.CSteamID.m_SteamID)
                    {
                        return EKeyReemeResult.PlayerNotMatch;
                    }

                    if (cdkdata.RedeemedTimes >= cdkdata.MaxRedeem)
                    {
                        return EKeyReemeResult.MaxRedeemed;
                    }

                    if (logdata.Count > 0 && !cdkdata.Renew)
                    {
                        return EKeyReemeResult.Redeemed;
                    }
                    else if (logdata.Count == 0)
                    {
                        if (!KeyVailed(cdkdata))
                        {
                            return EKeyReemeResult.KeyNotValid;
                        }

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
                                        UnityEngine.Color.red, isRich);
                                }
                            }
                        }

                        if (ushort.TryParse(cdkdata.Vehicle, out ushort vehicleID)) 
                        {
                            if (vehicleID != 0)
                            {
                                player.GiveVehicle(vehicleID);
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(cdkdata.Vehicle))
                            {
                                try
                                {
                                    var asset = Assets.find(Guid.Parse(cdkdata.Vehicle));
                                    VehicleTool.SpawnVehicleForPlayer(player.Player, asset);
                                }
                                catch (Exception e)
                                {
                                    Logger.LogException(e);
                                }
                            }
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
                            ExecuteUconomy(player, cdkdata);
                        }

                        if (cdkdata.GrantPermissionGroup != string.Empty && !cdkdata.UsePermissionSync)
                        {
                            switch (R.Permissions.AddPlayerToGroup(cdkdata.GrantPermissionGroup, player))
                            {
                                case Rocket.API.RocketPermissionsProviderResult.Success:
                                    UnturnedChat.Say(player,
                                        Main.Instance.Translate("permission_granted", cdkdata.GrantPermissionGroup), isRich);
                                    break;
                                case Rocket.API.RocketPermissionsProviderResult.DuplicateEntry:
                                    UnturnedChat.Say(player,
                                        Main.Instance.Translate("permission_duplicate_entry",
                                            cdkdata.GrantPermissionGroup), UnityEngine.Color.yellow, isRich);
                                    break;
                                default:
                                    UnturnedChat.Say(player, Main.Instance.Translate("permission_grant_error",cdkdata.GrantPermissionGroup),
                                        UnityEngine.Color.red, isRich);
                                    break;
                            }
                        }
                        else if (cdkdata.GrantPermissionGroup != string.Empty && cdkdata.UsePermissionSync)
                        {
                            ExecutePermissionSync(player, cdkdata,EExecutePermissionMethod.Add);
                        }

                        Database.SaveLogToDB(new LogData(CDK, player.CSteamID.m_SteamID, DateTime.Now, cdkdata.ValidUntil,
                            cdkdata.GrantPermissionGroup, cdkdata.UsePermissionSync));
                        Database.IncreaseRedeemedTime(CDK);
                        return EKeyReemeResult.Success;
                    }
                    else if (logdata.Count > 0 && cdkdata.Renew)
                    {
                        if (!cdkdata.UsePermissionSync)
                        {
                            R.Permissions.AddPlayerToGroup(cdkdata.GrantPermissionGroup, player);
                            Database.UpdateLogInDB(new LogData(CDK, player.CSteamID.m_SteamID, DateTime.Now, cdkdata.ValidUntil,
                                cdkdata.GrantPermissionGroup, cdkdata.UsePermissionSync));
                            Database.UpdateRenew(CDK);
                            return EKeyReemeResult.Renewed;
                        }
                        else
                        {
                            ExecutePermissionSync(player, cdkdata, EExecutePermissionMethod.Update);
                            Database.UpdateLogInDB(new LogData(CDK, player.CSteamID.m_SteamID, DateTime.Now, cdkdata.ValidUntil,
                                cdkdata.GrantPermissionGroup, cdkdata.UsePermissionSync));
                            Database.UpdateRenew(CDK);
                            return EKeyReemeResult.Renewed;
                        }
                    }
                }
                else
                {
                    return EKeyReemeResult.KeyNotFound;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return EKeyReemeResult.Error;
        }

        private bool KeyVailed(CDKData cdk)
        {
            if (Main.Instance.Configuration.Instance.ByPassKeyVailed)
            {
                return true;
            }

            // 密钥验证逻辑
            // 首先判断载具栏是否为空字符串，如果不是，则尝试解析GUID 如果是空GUID则为无效KEY，反之尝试用尼尔森接口解析GUID，如果GUID无效，则视为无效KET
            // 然后尝试解析数字ID，如果不是数字ID，则视为无效KEY
            // 物品字符串验证逻辑为将物品字符串使用英文逗号分割，检查分割后的列表，如果存在非数字值，则视为无效KEY
            // 数量字符串验证逻辑为，将字符串使用英文逗号分割，如果物品和数量的长度不同，则视为无效KEY
            // 除此之外验证数量是否为无效值，例如超出最大值，或非数字
            if (!string.IsNullOrEmpty(cdk.Vehicle))
            {
                if (Guid.TryParse(cdk.Vehicle, out Guid result))
                {
                    if (result == Guid.Empty)
                    {
                        Logger.LogError(string.Format("can't use a empty GUID in CDK:{0}", cdk.CDK));
                        return false;
                    }

                    var gid = Assets.find(result);
                    if (gid == null)
                    {
                        Logger.LogError(string.Format("CDK: {0} use an invailed vehicle GUID",cdk.CDK));
                        return false;
                    }
                }
                else
                {
                    if (!ushort.TryParse(cdk.Vehicle,out ushort vehicleID))
                    {
                        Logger.LogError(string.Format("CDK: {0} use an invailed vehicle id", cdk.CDK));
                        return false;
                    }
                }
            }

            if (cdk.Amount == string.Empty && cdk.Items != string.Empty)
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
            }
            
            if (cdk.Amount != string.Empty && cdk.Items != string.Empty)
            {
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
            }

            return true;
        }

        private void ExecuteUconomy(UnturnedPlayer player, CDKData cdkdata)
        {
            Main.ExecuteDependencyCode("Uconomy", (IRocketPlugin uconomy) =>
            {
                if (uconomy.State == PluginState.Loaded)
                {
                    Uconomy.Instance.Database.IncreaseBalance(player.Id, cdkdata.Money.Value);
                    UnturnedChat.Say(player,
                        Main.Instance.Translate("uconomy_gain", cdkdata.Money.Value,
                            Uconomy.Instance.Configuration.Instance.MoneyName), Main.Instance.Configuration.Instance.EnableRichText);
                }
            });
        }

        private void ExecutePermissionSync(UnturnedPlayer player, CDKData cdkdata,EExecutePermissionMethod method)
        {
            switch (method)
            {
                case EExecutePermissionMethod.Add:
                    Main.ExecuteDependencyCode("PermissionSync", (IRocketPlugin ps) =>
                    {
                        if (ps.State == PluginState.Loaded)
                        {
                            PermissionSync.Main.Instance.databese.AddPermission(Main.Instance.Name, player,
                                cdkdata.GrantPermissionGroup, cdkdata.ValidUntil.ToString());
                        }
                    });
                    break;
                case EExecutePermissionMethod.Update:
                    Main.ExecuteDependencyCode("PermissionSync", (IRocketPlugin ps) =>
                    {
                        if (ps.State == PluginState.Loaded)
                        {
                            PermissionSync.Main.Instance.databese.UpdatePermission(player,
                                cdkdata.GrantPermissionGroup, cdkdata.ValidUntil, Main.Instance.Name);
                        }
                    });
                    break;

            }
        }
    }
}
