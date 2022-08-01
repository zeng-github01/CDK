using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rocket.API.Collections;
using Rocket.Core.Plugins;
using Rocket.Unturned.Player;
using Rocket.Unturned.Chat;
using UnityEngine;
using Rocket.Unturned;
using Rocket.API;
using Rocket.Core;
using SDG.Unturned;
using fr34kyn01535.Uconomy;
using System.IO;
using Rocket.Core.Logging;

namespace CDK
{
    public class Main : RocketPlugin<Config>
    {
        public DatabaseManager Database;
        public static Main Instance; 
        protected override void Load()
        {
            Instance = this;  
            //if(!Configuration.Instance.MySQLSupport)
            //{
            //    DirectoryInfo info = new DirectoryInfo(Assembly.Location);
            //    if (!File.Exists(info.Parent + "\\CDK\\Redeem.log"))
            //    {
            //        File.CreateText(info.Parent + "\\CDK\\Redeem.log");
            //    }
            //}
            //else
            //{
                Database = new DatabaseManager();
                U.Events.OnPlayerConnected += PlayerConnect;
            //}
            
            Rocket.Core.Logging.Logger.Log("CDK Plugin loaded");
        }

        protected override void Unload()
        {
            U.Events.OnPlayerConnected -= PlayerConnect;
            Rocket.Core.Logging.Logger.Log("CDK Plugin unloaded");
        }
        //#region Redeem
        //public void CDKRedeem(UnturnedPlayer player,string CDK)
        //{
        //    CDK key = Configuration.Instance.CDKs.Where(x => x.Key == CDK).FirstOrDefault();
        //    if(key != null)
        //    {
        //        //Admin
        //        if (player.IsAdmin && Configuration.Instance.BypassAdmin)
        //        {
        //            string[] Items = key.Items.Split(',');
        //            foreach (string item in Items)
        //            {
        //                if (ushort.TryParse(item, out ushort s))
        //                {
        //                    if (!player.GiveItem(s, 1))
        //                    {
        //                        UnturnedChat.Say(player, Instance.Translate("items_give_fail"), Color.red);
        //                    }
        //                }
        //            }
        //            if (key.Vehicle.HasValue && key.Vehicle > 0)
        //            {
        //                player.GiveVehicle(key.Vehicle.Value);
        //            }
        //            if (key.XP.HasValue && key.XP != 0)
        //            {
        //                player.Experience += key.XP.Value;
        //            }
        //            if (key.Money.HasValue && key.Money.Value != 0)
        //            {
        //                Main.ExecuteDependencyCode("Uconomy", (IRocketPlugin plugin) =>
        //                {
        //                    Uconomy.Instance.Database.IncreaseBalance(player.CSteamID.ToString(), key.Money.Value);
        //                });
        //            }
        //            UnturnedChat.Say(player, Instance.Translate("success"));
        //            return;
        //        }
        //        //重复领取判断
        //        DirectoryInfo info = new DirectoryInfo(Assembly.Location);
        //        if (File.Exists(info.Parent + "\\CDK\\Redeem.log"))
        //        {
        //           string[] logsreach = File.ReadAllLines(info.Parent + "\\CDK\\Redeem.log");
        //            if(logsreach.Contains(player+","+CDK))
        //            {
        //                UnturnedChat.Say(player, Instance.Translate("already_redeemed"), Color.red);
        //                return;
        //            }
        //        }
        //        else
        //        { 
        //            File.CreateText(info.Parent + "\\CDK\\Redeem.log");
        //        }

        //        if (key.RedeemPermission != null && player.HasPermission(key.RedeemPermission) && !player.IsAdmin)
        //        {
        //            string[] Items = key.Items.Split(',');
        //            foreach (string item in Items)
        //            {  
        //                if(ushort.TryParse(item,out ushort s))
        //                {
        //                    if(!player.GiveItem(s, 1))
        //                    {
        //                        UnturnedChat.Say(player, Instance.Translate("items_give_fail"), Color.red);
        //                    }
        //                }
        //            }
        //            if (key.Vehicle.HasValue && key.Vehicle > 0)
        //            {
        //                player.GiveVehicle(key.Vehicle.Value);
        //            }
        //            if (key.XP.HasValue && key.XP.Value != 0)
        //            {
        //                player.Experience += key.XP.Value;
        //            }
        //            if (key.Money.HasValue && key.Money.Value != 0)
        //            {
        //                Main.ExecuteDependencyCode("Uconomy", (IRocketPlugin plugin) =>
        //                {
        //                    Uconomy.Instance.Database.IncreaseBalance(player.CSteamID.ToString(), key.Money.Value);
        //                });
        //            }

        //            if(key.GrantPermissionGroup !="")
        //            {
        //                switch (R.Permissions.AddPlayerToGroup(key.GrantPermissionGroup, player))
        //                {
        //                    case RocketPermissionsProviderResult.Success:
        //                        UnturnedChat.Say(player, Instance.Translate("permission_granted"));
        //                        return;
        //                    case RocketPermissionsProviderResult.DuplicateEntry:
        //                        UnturnedChat.Say(player, Instance.Translate("permission_duplicate_entry",key.GrantPermissionGroup),Color.yellow);
        //                        return;
        //                    default:
        //                        UnturnedChat.Say(player, Instance.Translate("permission_grant_error"), Color.red);
        //                        break;
        //                }
        //            }
        //            if (File.Exists(info.Parent + "\\CDK\\Redeem.log"))
        //            {
        //                StreamWriter sw = new StreamWriter(info.Parent + "\\CDK\\Redeem.log");
        //                sw.WriteLine(player + "," + CDK);
        //                sw.Dispose();
        //                UnturnedChat.Say(player, Instance.Translate("success"));
        //                return;
        //            }
        //            else
        //            {
        //                File.CreateText(info.Parent + "\\CDK\\Redeem.log");
        //                StreamWriter sw = new StreamWriter(info.Parent + "\\CDK\\Redeem.log");
        //                sw.WriteLine(player + "," + CDK);
        //                sw.Dispose();
        //                UnturnedChat.Say(player, Instance.Translate("success"));
        //                return;
        //            }
        //        }
        //        else if (key.RedeemPermission != "" && !player.HasPermission(key.RedeemPermission))
        //        {
        //            UnturnedChat.Say(player, Instance.Translate("don't_have_permisson"), Color.red);
        //            return;
        //        }
        //        else if (key.RedeemPermission == "")
        //        {
        //            if (key.MaxCount.HasValue && key.MaxCount.Value > 0)
        //            {
        //                int Count = key.CurrentCount.Value;

        //                if ( key.CurrentCount.Value >= key.MaxCount.Value)
        //                {
        //                    UnturnedChat.Say(player, Instance.Translate("maxcount_reached"), Color.red);
        //                    return;
        //                }
        //                else
        //                {
        //                    //计次
        //                    string[] Items = key.Items.Split(',');
        //                    foreach (string item in Items)
        //                    {
        //                        if (ushort.TryParse(item, out ushort s))
        //                        {
        //                            if (!player.GiveItem(s, 1))
        //                            {
        //                                UnturnedChat.Say(player, Instance.Translate("items_give_fail"), Color.red);
        //                            }
        //                        }
        //                    }
        //                    if (key.Vehicle.HasValue && key.Vehicle > 0)
        //                    {
        //                        player.GiveVehicle(key.Vehicle.Value);
        //                    }
        //                    if (key.XP.HasValue && key.XP != 0)
        //                    {
        //                        player.Experience += key.XP.Value;
        //                    }
        //                    if (key.Money.HasValue && key.Money.Value != 0)
        //                    {
        //                        Main.ExecuteDependencyCode("Uconomy", (IRocketPlugin plugin) =>
        //                        {
        //                            Uconomy.Instance.Database.IncreaseBalance(player.CSteamID.ToString(), key.Money.Value);
        //                        });
        //                    }

        //                    if (key.GrantPermissionGroup != "")
        //                    {
        //                        switch (R.Permissions.AddPlayerToGroup(key.GrantPermissionGroup, player))
        //                        {
        //                            case RocketPermissionsProviderResult.Success:
        //                                UnturnedChat.Say(player, Instance.Translate("permission_granted"));
        //                                return;
        //                            case RocketPermissionsProviderResult.DuplicateEntry:
        //                                UnturnedChat.Say(player, Instance.Translate("permission_duplicate_entry",key.GrantPermissionGroup),Color.yellow);
        //                                return;
        //                            default:
        //                                UnturnedChat.Say(player, Instance.Translate("permission_grant_error"), Color.red);
        //                                break;
        //                        }
        //                    }
        //                    if (File.Exists(info.Parent + "\\CDK\\Redeem.log"))
        //                    {
        //                        StreamWriter sw = new StreamWriter(info.Parent + "\\CDK\\Redeem.log");
        //                        sw.WriteLine(player + "," + CDK);
        //                        sw.Dispose();
        //                        UnturnedChat.Say(player, Instance.Translate("success"));
        //                        Count++;
        //                        key.CurrentCount = Count;
        //                        Configuration.Save();
        //                        return;
        //                    }
        //                    else
        //                    {
        //                        File.CreateText(info.Parent + "\\CDK\\Redeem.log");
        //                        StreamWriter sw = new StreamWriter(info.Parent + "\\CDK\\Redeem.log");
        //                        sw.WriteLine(player + "," + CDK);
        //                        sw.Dispose();
        //                        UnturnedChat.Say(player, Instance.Translate("success"));
        //                        Count++;
        //                        key.CurrentCount = Count;
        //                        Configuration.Save();
        //                        return;
        //                    }
        //                }
                        
        //            }
        //            else
        //            {
        //                string[] Items = key.Items.Split(',');
        //                foreach (string item in Items)
        //                {
        //                    if (ushort.TryParse(item, out ushort s))
        //                    {
        //                        if (!player.GiveItem(s, 1))
        //                        {
        //                            UnturnedChat.Say(player, Instance.Translate("items_give_fail"), Color.red);
        //                        }
        //                    }
        //                }
        //                if (key.Vehicle.HasValue && key.Vehicle > 0)
        //                {
        //                    player.GiveVehicle(key.Vehicle.Value);
        //                }
        //                if(key.XP.HasValue && key.XP != 0)
        //                {
        //                    player.Experience += key.XP.Value;
        //                }
        //                if(key.Money.HasValue && key.Money.Value !=0)
        //                {           
        //                    Main.ExecuteDependencyCode("Uconomy", (IRocketPlugin plugin) =>
        //                     {
        //                         Uconomy.Instance.Database.IncreaseBalance(player.CSteamID.ToString(), key.Money.Value);
        //                     });
        //                }

        //                if (key.GrantPermissionGroup != "")
        //                {
        //                    switch (R.Permissions.AddPlayerToGroup(key.GrantPermissionGroup, player))
        //                    {
        //                        case RocketPermissionsProviderResult.Success:
        //                            UnturnedChat.Say(player, Instance.Translate("permission_granted"));
        //                            return;
        //                        case RocketPermissionsProviderResult.DuplicateEntry:
        //                            UnturnedChat.Say(player, Instance.Translate("permission_duplicate_entry",key.GrantPermissionGroup),Color.yellow);
        //                            return;
        //                        default:
        //                            UnturnedChat.Say(player, Instance.Translate("permission_grant_error"), Color.red);
        //                            break;
        //                    }
        //                }
        //                if (File.Exists(info.Parent + "\\CDK\\Redeem.log"))
        //                {
        //                    StreamWriter sw = new StreamWriter(info.Parent + "\\CDK\\Redeem.log");
        //                    sw.WriteLine(player + "," + CDK);
        //                    sw.Dispose();
        //                    UnturnedChat.Say(player, Instance.Translate("success"));
        //                    //return;
        //                }
                        
                        
        //            }
        //        }
        //    }
        //    else
        //    {
        //        UnturnedChat.Say(player,Instance.Translate("key_dones't_exist",CDK),Color.red);
        //        return;
        //    }
        //}
        //#endregion

        //#region ResetCount
        //public bool ResetKeyCount(string CDK)
        //{
        //    bool success = false;
        //    CDK key = Configuration.Instance.CDKs.Where(y => y.Key == CDK).FirstOrDefault();
        //    if(key != null)
        //    {
        //        key.CurrentCount = 0;
        //        Configuration.Save();
        //        success = true;
        //    }
        //    return success;
        //}
        //#endregion

        #region Rendom
        //public string GenerateKey()
        //{
             
        //}
        #endregion


        private void PlayerConnect(UnturnedPlayer player)
        {
            Database.CheckValid(player);
        }
        public override TranslationList DefaultTranslations =>
            new TranslationList
            {
                {"success","You successfully redeemed CDK."},
                {"key_dones't_exist","The Key dones't exist!"},
                {"don't_have_permisson","You don't have permission to redeem this CDK!" },
                {"maxcount_reached","This CDK already reached max redeemed!" },
                {"items_give_fail","Failed to give items!" },
                {"already_redeemed","You already redeemed this CDK!" },
                {"permission_duplicate_entry","You already in permission group:{0}." },
                {"permission_granted","You are added permission group: {0}" },
                {"permission_grant_error","Failed to add permission group" },
                //{"reset_count","reset key {0}'s maxcount!" },
                {"uconomy_gain","You got {0} {1}" },
                {"error","error!" },
                //{"cdk_created","You created CDK: {0}" },
                //{"create_cdk_exist","This CDK already created! " },
                //{"cdk_create_failure","Failed to create cdk.please check command syntax!" },
                {"invaild_parameter","out of patamter! correct syntax:{0}"},
                {"key_renewed","Your key has been renewed!" },
                {"key_expired","Your key has been expired:{0}" },
                {"already_purchased","You already purchased this permission group" },
                {"invaild_param",""}
            };
    }
}
