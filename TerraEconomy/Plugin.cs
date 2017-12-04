using System;
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

        public ConfigFile Config = new ConfigFile();
        private ScriptHandler scriptHandler;

        string ScriptsPath = Path.Combine(TShock.SavePath, "terra_scripts");

        private static string ScriptTemplate =
@"using System;
using System.IO;
using System.Data;

using TShockAPI;
using Newtonsoft.Json;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;

using TerraEconomy;
using TerraEconomy.Util;


public class MyScript : TeconomyScript
{
    public override void Initialize()
    {
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
        private void OnInitialize(EventArgs args)
        {
            Database.Connect();
            Commands.ChatCommands.Add(new Command("TerraEconomy.admin".ToLower(), CreateScript, "cscript")
            {
                HelpText = "Usage: /cscript <name>"
            });
        }

        private void OnPostInitialize(EventArgs args)
        {
            scriptHandler = new ScriptHandler(ScriptsPath);
            scriptHandler.CallInit();
        }

        private void OnData(GetDataEventArgs e)
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

                                if(NPCInfo.NPCMoney.ContainsKey(npc.FullName))
                                {
                                    Transaction t = new Transaction(sender.User.ID, -1, NPCInfo.NPCMoney[npc.FullName], String.Format("Killed a {0}", npc.FullName));
                                    t.InsertToDB();
                                    t.IsMobKill = true;
                                    Hooks.BankHooks.InvokeOnTransaction(sender, t);
                                }
                                else
                                    TShock.Log.ConsoleInfo("[TerraEconomy] Unhandled npc dict money! Add it: {0}", npc.FullName);
                            }
                            break;
                        }
                }
            }
            
        }

        private void OnNpcKill(NpcKilledEventArgs args)
        {
            TShock.Log.ConsoleInfo("ID: {0}", args.npc.lastInteraction);

            if (args.npc.lastInteraction < 0 || args.npc.lastInteraction > TShock.Players.Length)
                return;
            
            var name = TShock.Players[args.npc.lastInteraction].Name;
            TShock.Log.ConsoleInfo("Player {0} killed a npc: {1}", name, args.npc.ToString());
        }

        private void PlayerHooks_PlayerPostLogin(TShockAPI.Hooks.PlayerPostLoginEventArgs e)
        {

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
