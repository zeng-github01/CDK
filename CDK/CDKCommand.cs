using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rocket.API;
using Rocket.Unturned.Player;
using Steamworks;
using Rocket.Unturned.Chat;

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
                    switch (Main.Instance.Database.RedeemCDK(UnturnedPlayer.FromName(caller.DisplayName), args[0]))
                    {
                        case DatabaseManager.RedeemCDKResult.Success:
                            UnturnedChat.Say(caller, Main.Instance.Translate("success"));
                            break;
                        case DatabaseManager.RedeemCDKResult.Redeemed:
                            UnturnedChat.Say(caller, Main.Instance.Translate("already_redeemed"), UnityEngine.Color.red);
                            break;
                        case DatabaseManager.RedeemCDKResult.KeyNotFound:
                            UnturnedChat.Say(caller, Main.Instance.Translate("key_dones't_exist"), UnityEngine.Color.red);
                            break;
                        case DatabaseManager.RedeemCDKResult.MaxRedeemed:
                            UnturnedChat.Say(caller, Main.Instance.Translate("maxcount_reached"), UnityEngine.Color.red);
                            break;
                        case DatabaseManager.RedeemCDKResult.Renewed:
                            UnturnedChat.Say(caller, Main.Instance.Translate("key_renewed"));
                            break;
                        case DatabaseManager.RedeemCDKResult.Error:
                            UnturnedChat.Say(caller, Main.Instance.Translate("error"), UnityEngine.Color.red);
                            break;
                        case DatabaseManager.RedeemCDKResult.PlayerNotMatch:
                            UnturnedChat.Say(caller, Main.Instance.Translate("player_not_match"), UnityEngine.Color.red);
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
    }
}
