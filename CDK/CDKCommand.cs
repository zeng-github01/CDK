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
            if(args.Length == 1)
              {
                if (!Main.Instance.Database.IsPurchased(UnturnedPlayer.FromName(caller.DisplayName), args[0]))
                {
                    switch (RedeemCDK(UnturnedPlayer.FromName(caller.DisplayName), args[0]))
                    {
                        case EKeyReemeResult.Success:
                            UnturnedChat.Say(caller, Main.Instance.Translate("success"));
                            break;
                        case EKeyReemeResult.Redeemed:
                            UnturnedChat.Say(caller, Main.Instance.Translate("already_redeemed"), UnityEngine.Color.red);
                            break;
                        case EKeyReemeResult.KeyNotFound:
                            UnturnedChat.Say(caller, Main.Instance.Translate("key_dones't_exist"), UnityEngine.Color.red);
                            break;
                        case EKeyReemeResult.MaxRedeemed:
                            UnturnedChat.Say(caller, Main.Instance.Translate("maxcount_reached"), UnityEngine.Color.red);
                            break;
                        case EKeyReemeResult.Renewed:
                            UnturnedChat.Say(caller, Main.Instance.Translate("key_renewed"));
                            break;
                        case EKeyReemeResult.Error:
                            UnturnedChat.Say(caller, Main.Instance.Translate("error"), UnityEngine.Color.red);
                            break;
                        case EKeyReemeResult.PlayerNotMatch:
                            UnturnedChat.Say(caller, Main.Instance.Translate("player_not_match"), UnityEngine.Color.red);
                            break;
                        case EKeyReemeResult.KeyNotValid:
                            UnturnedChat.Say(caller, Main.Instance.Translate("cdk_config_error"), UnityEngine.Color.red);
                            break;
                    }
                }
                else
                {
                    UnturnedChat.Say(caller, Main.Instance.Translate("already_purchased"), UnityEngine.Color.red);
                }
             }
            else
            {
                UnturnedChat.Say(caller, Main.Instance.Translate("invaild_param",Syntax), UnityEngine.Color.red);
            }
        }

        private EKeyReemeResult RedeemCDK(UnturnedPlayer player, string CDK)
        {
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

                    if (logdata != null && !cdkdata.Renew)
                    {
                        return EKeyReemeResult.Redeemed;
                    }
                    else if (logdata == null)
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
                                        Main.Instance.Translate("uconomy_gain", cdkdata.Money.Value,
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

                        Database.SaveLogToDB(new LogData(CDK, player.CSteamID.m_SteamID, DateTime.Now, cdkdata.ValidUntil,
                            cdkdata.GrantPermissionGroup, cdkdata.UsePermissionSync));
                        Database.IncreaseRedeemedTime(CDK);
                        return EKeyReemeResult.Success;
                    }
                    else if (logdata != null && cdkdata.Renew)
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
                            Main.ExecuteDependencyCode("PermissionSync", (IRocketPlugin ps) =>
                            {
                                if (ps.State == PluginState.Loaded)
                                {
                                    PermissionSync.Main.Instance.databese.UpdatePermission(player,
                                        cdkdata.GrantPermissionGroup, cdkdata.ValidUntil, "CDKPlugin");
                                }
                            });
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
    }
}
