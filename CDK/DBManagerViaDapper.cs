using CDK.Data;
using Dapper;
using MySql.Data.MySqlClient;
using Rocket.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CDK.Enum;

namespace CDK
{
    public class DBManagerViaDapper
    {
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

        public CDKData GetCDKData(string key)
        {
            CDKData cdkData = null;
            var con = CreateConnection();

            try
            {
                cdkData = con.QuerySingle<CDKData>(
                    $"SELECT * from `{Main.Instance.Configuration.Instance.DatabaseCDKTableName}` where `CDK` = @CDK;",
                    new {CDK = key});
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

        public LogData GetLogData(string parameter, ELogQueryType type)
        {
            LogData logData = null;
            var con = CreateConnection();
            try
            {
                switch (type)
                {
                    case ELogQueryType.ByCDK:
                        logData = con.QuerySingle<LogData>(
                            $"SELECT * FROM `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` where `CDK` = '@CDK'",
                            new {CDK = parameter});
                        break;
                    case ELogQueryType.ByTime:
                        logData = con.QuerySingle<LogData>(
                            $"SELECT * FROM `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` where `SteamID` = '@id' and ValidUtil < now()",
                            new {id = parameter});
                        break;
                    case ELogQueryType.ByPermissionGroup:
                        logData = con.QuerySingle<LogData>(
                            $"SELECT * FROM `{Main.Instance.Configuration.Instance.DatabaseRedeemLogTableName}` WHERE `GrantPermissionGroup` = '@permission'",
                            new {permission = parameter});
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
    }
}