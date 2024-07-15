using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Steamworks;

namespace CDK.Data
{
    public class CDKData
    {
        public string CDK { get; internal set; }
        public string Items { get; internal set; }
        public string Amount { get; internal set; }
        public string Vehicle { get; internal set; }
        public uint? Experience { get; internal set; }
        public decimal? Money { get; internal set; }
        public int? Reputation { get; internal set; }
        public string GrantPermissionGroup { get; internal set; }
        public int RedeemedTimes { get; internal set; }
        public int MaxRedeem { get; internal set; }
        public DateTime ValidUntil { get; internal set; }
        public bool Renew { get; internal set; }
        public ulong Owner { get; internal set; }

        public bool UsePermissionSync { get; internal set; }
        public CDKData(string cdk, string items, string amount, string vehicle, ushort? exp, decimal? money, int? reputation, string permissonGroup, int redeemedTimes, int maxredeem, DateTime validuntill, ulong steamID, bool renew, bool usePermissionSync)
        {
            CDK = cdk;
            Items = items;
            Amount = amount;
            Vehicle = vehicle;
            Experience = exp;
            Money = money;
            Reputation = reputation;
            GrantPermissionGroup = permissonGroup;
            RedeemedTimes = redeemedTimes;
            MaxRedeem = maxredeem;
            ValidUntil = validuntill;
            Owner = steamID;
            Renew = renew;
            UsePermissionSync = usePermissionSync;
        }

        public CDKData() { }
    }
}
