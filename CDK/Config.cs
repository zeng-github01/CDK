using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rocket.API;

namespace CDK
{
    public class Config : IRocketPluginConfiguration
    {
        public int MySQLTableVer;
        //public bool MySQLSupport;
        //public List<CDK> CDKs;
        //public bool BypassAdmin;
        public string DatabaseAddress;
        public string DatabaseUsername;
        public string DatabasePassword;
        public string DatabaseName;
        public string DatabaseCDKTableName;
        public string DatabaseRedeemLogTableName;
        public int DatabasePort;

        public void LoadDefaults()
        {
            MySQLTableVer = 2;
            //MySQLSupport = false;
            //BypassAdmin = true;
            DatabaseAddress = "127.0.0.1";
            DatabaseUsername = "unturned";
            DatabasePassword = "password";
            DatabaseName = "unturned";
            DatabaseCDKTableName = "cdk";
            DatabaseRedeemLogTableName = "redeemedlog";
            DatabasePort = 3306;
            //CDKs = new List<CDK>() { new CDK() { Key = "Key", Items = "100,1000,1500",XP = 111 , RedeemPermission = "Test",GrantPermissionGroup="VIP1" ,MaxCount = 100,CurrentCount = 0,Vehicle = 1,Money = 1000 } };  
        }
    }

    //public class CDK
    //{
    //    public CDK() { }

    //    public string Key;
    //    public string Items;
    //    public ushort? Vehicle = null;
    //    public uint? XP = null;
    //    public decimal? Money = null;
    //    public string RedeemPermission = "";
    //    public int? MaxCount = 0;
    //    public int? CurrentCount = 0;
    //    public string GrantPermissionGroup = "";

    //}
}
