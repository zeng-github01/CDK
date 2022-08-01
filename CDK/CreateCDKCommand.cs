using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rocket.API;
using Rocket.Unturned.Chat;

namespace CDK
{
    public class CreateCDKCommand : IRocketCommand
    {
        public string Name => "createcdk";

        public string Syntax => "createcdk <CDK> <ItemString> <Vehicle> <Experience> <Money> [GrantPermissionGoup] [MaxRedeem]";

        public string Help => "Create a CDK with give paramters";

        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string>() { "cdk.create" };

        public void Execute(IRocketPlayer caller,string[] args)
        {
            UnturnedChat.Say(caller, "This command is under developing!",UnityEngine.Color.yellow);
            return;
        }
    }
}
