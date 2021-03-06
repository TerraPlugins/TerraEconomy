﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using System.Data;
using System.IO;
using Mono.Data.Sqlite;
using System.Reflection;
using TerraEconomy.Util;
using TShockAPI.DB;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;

namespace TerraEconomy
{
    [ApiVersion(2, 1)]
    public class TerraEconomy : TerrariaPlugin
    {
        #region Plugin Info
        public override string Name => "TerraEconomy";
        public override string Author => "Ryozuki";
        public override string Description => "I do this and that.";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        #endregion

        public static ConfigFile Config = new ConfigFile();
        private ScriptHandler scriptHandler;

        string ScriptsPath = Path.Combine(TShock.SavePath, "terra_scripts");

        private static string ScriptTemplate =
@"using System;
using System.IO;
using System.Data;

using TShockAPI;
using TShockAPI.DB;
using Newtonsoft.Json;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;

using TerraEconomy;
using TerraEconomy.Util;


public class MyScript : TeconomyScript
{
    public override void Initialize()
    {
        // await DBHelper.CreateTableAsync(new SqlTable(""MyScript_Experience"",
        //                new SqlColumn(""UserID"", MySqlDbType.Int32) { NotNull = true },
        //                new SqlColumn(""Level"", MySqlDbType.Int32) { DefaultValue = ""1"" },
        //                new SqlColumn(""XP"", MySqlDbType.Int64) { DefaultValue = ""0"" }
        //                ));
        TShockAPI.Hooks.PlayerHooks.PlayerChat += PlayerHooks_PlayerChat;
        TerraEconomy.Hooks.BankHooks.OnTransaction += OnTransaction;
    }

    public override void Dispose(bool disposing)
    {
        if (disposing)
        {
            TShockAPI.Hooks.PlayerHooks.PlayerChat -= PlayerHooks_PlayerChat;
            TerraEconomy.Hooks.BankHooks.OnTransaction -= OnTransaction;
        }
    }

    private void PlayerHooks_PlayerChat(TShockAPI.Hooks.PlayerChatEventArgs e)
    {
    }

    private void OnTransaction(TSPlayer sender, Transaction t)
    {
        // TShock.Log.ConsoleInfo(t.ToString());
    }
}
";

        public TerraEconomy(Main game) : base(game)
        {
        }

        private void LoadConfig()
        {
            string path = Path.Combine(TShock.SavePath, "TerraEconomy.json");
            Config = ConfigFile.Read(path);
        }

        public override void Initialize()
        {
            LoadConfig();

            Directory.CreateDirectory(ScriptsPath);

            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInitialize);
            ServerApi.Hooks.NetGetData.Register(this, OnData);

            TShockAPI.Hooks.PlayerHooks.PlayerPostLogin += PlayerHooks_PlayerPostLogin;
            TShockAPI.Hooks.PlayerHooks.PlayerLogout += PlayerHooks_PlayerLogout;
        }

        protected override void Dispose(bool disposing)
        {
            scriptHandler.CallDispose(disposing);

            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInitialize);
                ServerApi.Hooks.NetGetData.Deregister(this, OnData);

                TShockAPI.Hooks.PlayerHooks.PlayerPostLogin -= PlayerHooks_PlayerPostLogin;
            }
            base.Dispose(disposing);
        }

        #region Hooks
        private async void OnInitialize(EventArgs args)
        {
            Commands.ChatCommands.Add(new Command("TerraEconomy.admin".ToLower(), CreateScript, "cscript")
            {
                HelpText = "Usage: /cscript <name>"
            });
            await DBHelper.ConnectAsync();
        }

        private async void OnPostInitialize(EventArgs args)
        {
            await DBHelper.CreateTableAsync(new SqlTable("Accounts",
                new SqlColumn("ID", MySqlDbType.Int32) { Primary = true, Unique = true, Length = 7, AutoIncrement = true },
                new SqlColumn("UserID", MySqlDbType.Int32) { Length = 6 },
                new SqlColumn("Balance", MySqlDbType.Float) { DefaultValue = "0" }
                ));
            await DBHelper.CreateTableAsync(new SqlTable("Transactions",
                new SqlColumn("Amount", MySqlDbType.Float) { NotNull = true },
                new SqlColumn("SenderID", MySqlDbType.Int32) { DefaultValue = "-1" }, // -1 = NPC
                new SqlColumn("RecieverID", MySqlDbType.Int32) { NotNull = true },
                new SqlColumn("Hash", MySqlDbType.String) { NotNull = true },
                new SqlColumn("Message", MySqlDbType.String) { Length = 100 },
                new SqlColumn("Date", MySqlDbType.Int64) { NotNull = true }
                ));

            scriptHandler = new ScriptHandler(ScriptsPath);
            scriptHandler.CallInit();
        }

        private async void OnData(GetDataEventArgs e)
        {
            if (e.Handled)
                return;

            //TShock.Log.ConsoleInfo("Whoiam {0}", a.Msg.whoAmI);

            TSPlayer sender = TShock.Players[e.Msg.whoAmI];

            if (sender == null)
                return;

            using (var reader = new BinaryReader(new MemoryStream(e.Msg.readBuffer, e.Index, e.Length)))
            {
                switch (e.MsgID)
                {
                    case PacketTypes.NpcStrike:
                        {
                            var npcid = reader.ReadInt16();
                            var damage = reader.ReadInt16();

                            var npc = Main.npc[npcid];

                            var current_life = npc.life - damage;

                            if (current_life <= 0)
                            {
                                // kill
                                TShock.Log.ConsoleInfo("Player {0} killed a npc <{1}>", sender.Name, npc.FullName);
                                // give money based on the npc

                                if(Config.NPCMoney.ContainsKey(npc.FullName))
                                {
                                    Transaction t = new Transaction(sender.User.ID, -1, Config.NPCMoney[npc.FullName], String.Format("Killed a {0}", npc.FullName));
                                    t.IsMobKill = true;
                                    sender.SendMessage(String.Format("[TerraEconomy] Recieved {0} for killing a {1}", 
                                        Config.NPCMoney[npc.FullName], npc.FullName), Config.GetColor());
                                    await t.InsertToDBAsync();
                                    Hooks.BankHooks.InvokeOnTransaction(sender, t);
                                }
                                else if(!Config.DisableMissingMobWarning)
                                    TShock.Log.ConsoleInfo("[TerraEconomy] Unhandled/Unknown mob money reward! Add it, the mob name is '{0}'" +
                                        "\nYou can dissable this warning by setting DisableMissingMobWarning to false.", npc.FullName);
                            }
                            break;
                        }
                }
            }
        }

        private void PlayerHooks_PlayerPostLogin(TShockAPI.Hooks.PlayerPostLoginEventArgs e)
        {
            BankAccount account;

            if(e.Player.IsLoggedIn)
            {
                account = BankAccount.GetByUserID(e.Player.User.ID);

                if (account == null)
                {
                    account = new BankAccount(e.Player.User.ID);
                    account.InsertToDB();
                    TShock.Log.ConsoleInfo("[TerraEconomy] Created bank account for {0}", e.Player.User.Name);
                    e.Player.SendMessage("[TerraEconomy] Created a bank account for you, enjoy!", Config.GetColor());
                }
                e.Player.SendMessage(String.Format("[TerraEconomy] Your balance is {0}", account.Balance), Config.GetColor());
                Hooks.BankHooks.InvokeOnBankAccountLogin(e.Player, account);
            }
        }

        private void PlayerHooks_PlayerLogout(TShockAPI.Hooks.PlayerLogoutEventArgs e)
        {
            BankAccount account;
            if (e.Player.IsLoggedIn)
            {
                account = BankAccount.GetByUserID(e.Player.User.ID);

                if (account == null)
                {
                    TShock.Log.ConsoleError("[TerraEconomy] A player ({0}) without a bank account left, how?", e.Player.User.Name);
                    return;
                }
                Hooks.BankHooks.InvokeOnBankAccountLogout(e.Player, account);
            }
        }
        #endregion

        #region Commands
        private void CreateScript(CommandArgs args)
        {
            if(args.Parameters.Count < 1)
            {
                args.Player.SendInfoMessage("[TerraEconomy] You must provide a script name.");
                return;
            }

            string source = ScriptTemplate.Replace("MyScript", args.Parameters[0]);
            var path = Path.Combine(ScriptsPath, args.Parameters[0] + ".cs");
            if(!File.Exists(path))
            {
                File.WriteAllText(path, source);
                args.Player.SendSuccessMessage("[TerraEconomy] Script created succesfully.");
                return;
            }
            args.Player.SendErrorMessage("[TerraEconomy] A script with that name already exists.");
        }
        #endregion
    }
}
