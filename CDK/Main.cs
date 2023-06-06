using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
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
using Newtonsoft.Json.Linq;

namespace CDK
{
    public class Main : RocketPlugin<Config>
    {
        public DatabaseManager Database;
        public static Main Instance;
        private static readonly string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/106.0.0.0 Safari/537.36";
        protected override void Load()
        {
            Instance = this;
            CheckUpdate();  
            Database = new DatabaseManager();
            U.Events.OnPlayerConnected += PlayerConnect;
            Rocket.Core.Logging.Logger.Log("CDK Plugin loaded");
        }

        protected override void Unload()
        {
            U.Events.OnPlayerConnected -= PlayerConnect;
            Rocket.Core.Logging.Logger.Log("CDK Plugin unloaded");
        }


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
                {"permission_grant_error","Failed to add permission group {0}" },
                {"uconomy_gain","You got {0} {1}" },
                {"error","error!" },
                {"invaild_parameter","out of patamter! correct syntax:{0}"},
                {"key_renewed","Your key has been renewed!" },
                {"key_expired","Your key has been expired:{0}" },
                {"already_purchased","You already purchased this permission group" },
                {"invaild_param","Wrong usage.usage:{0}"},
                {"player_not_match","This CDK not belong to you!" },
                {"cdk_config_error","CDK configuration error.please contact server owner!" }
            };

        private void CheckUpdate()
        {
            
            string dlstring = "https://api.github.com/repos/zeng-github01/CDKey-CodeReward/releases/latest";
            WebClient webClient = new WebClient();
            webClient.Headers.Add("user-agent",USER_AGENT);
             string jsonstring =  webClient.DownloadString(dlstring);
              var json = JObject.Parse(jsonstring);
            Version version = new Version(json["tag_name"].ToString());
            Version crv = Assembly.GetName().Version;
            if(version > crv)
            {
                var changelog = json["body"].ToString();
                Rocket.Core.Logging.Logger.Log(String.Format("New Update {0} has been released",version.ToString()),ConsoleColor.Green);
                Rocket.Core.Logging.Logger.LogWarning(String.Format("Changelog: {0}",changelog));
                Rocket.Core.Logging.Logger.LogWarning($"{Name} has been unload");
                Rocket.Core.Logging.Logger.Log("Go to " + "https://github.com/zeng-github01/CDKey-CodeReward/releases/latest "+"to get latest update", ConsoleColor.Yellow);
            }
        }
    }
}
