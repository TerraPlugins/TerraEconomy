using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI.DB;
using MySql.Data.MySqlClient;
using System.Data;
using TShockAPI;
using Mono.Data.Sqlite;
using System.IO;
using System.Threading.Tasks;

namespace TerraEconomy.Util
{
    public static class DBHelper
    {
        public static IDbConnection db;
        public static SqlTableCreator sqlcreator;

        public static async Task<bool> CreateTableAsync(SqlTable table)
        {
            return await Task.Run(() => sqlcreator.EnsureTableStructure(table));
        }

        public static bool CreateTable(SqlTable table)
        {
            return sqlcreator.EnsureTableStructure(table);
        }

        public static async Task ConnectAsync()
        {
            await Task.Run(() =>
            {
                switch (TShock.Config.StorageType.ToLower())
                {
                    case "mysql":
                        string[] dbHost = TShock.Config.MySqlHost.Split(':');
                        db = new MySqlConnection()
                        {
                            ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4};",
                                dbHost[0],
                                dbHost.Length == 1 ? "3306" : dbHost[1],
                                TShock.Config.MySqlDbName,
                                TShock.Config.MySqlUsername,
                                TShock.Config.MySqlPassword)

                        };
                        break;

                    case "sqlite":
                        string sql = Path.Combine(TShock.SavePath, "TerraEconomy.sqlite");
                        db = new SqliteConnection(string.Format("uri=file://{0},Version=3", sql));
                        break;
                }

                sqlcreator = new SqlTableCreator(db,
                    db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
            });
        }

        public static void Connect()
        {
            switch (TShock.Config.StorageType.ToLower())
            {
                case "mysql":
                    string[] dbHost = TShock.Config.MySqlHost.Split(':');
                    db = new MySqlConnection()
                    {
                        ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4};",
                            dbHost[0],
                            dbHost.Length == 1 ? "3306" : dbHost[1],
                            TShock.Config.MySqlDbName,
                            TShock.Config.MySqlUsername,
                            TShock.Config.MySqlPassword)

                    };
                    break;

                case "sqlite":
                    string sql = Path.Combine(TShock.SavePath, "TerraEconomy.sqlite");
                    db = new SqliteConnection(string.Format("uri=file://{0},Version=3", sql));
                    break;
            }

            sqlcreator = new SqlTableCreator(db,
                db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
        }

        public static async Task<int> QueryAsync(string query, params object[] args)
        {
            return await Task.Run(() => db.Query(query, args));
        }

        public static int Query(string query, params object[] args)
        {
            return db.Query(query, args);
        }

        public static async Task<QueryResult> QueryResultAsync(string query, params object[] args)
        {
            return await Task.Run(() => db.QueryReader(query, args));
        }

        public static QueryResult QueryResult(string query, params object[] args)
        {
            return db.QueryReader(query, args);
        }
    }
}
