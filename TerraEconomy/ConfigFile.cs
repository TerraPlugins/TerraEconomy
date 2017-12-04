using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;

namespace TerraEconomy
{
    public class ConfigFile
    {
        // Config variables here:
        public int MaxItems = 100;
        public string ConfigVersion = "1.0.2";
        public bool DisableMissingMobWarning = false;
        public int[] MessageColor = { 51, 0, 102 };
        public Dictionary<string, float> NPCMoney = new Dictionary<string, float>()
        {
            {"Slime", 2},
            {"Zombie", 5},
        };
        // End of config variables

        public Microsoft.Xna.Framework.Color GetColor()
        {
            return new Microsoft.Xna.Framework.Color(MessageColor[0], MessageColor[1], MessageColor[2]);
        }

        public static ConfigFile Read(string path)
        {
            if (!File.Exists(path))
            {
                ConfigFile config = new ConfigFile();

                File.WriteAllText(path, JsonConvert.SerializeObject(config, Formatting.Indented));
                return config;
            }

            var conf = JsonConvert.DeserializeObject<ConfigFile>(File.ReadAllText(path));

            if(string.IsNullOrWhiteSpace(conf.ConfigVersion) || conf.ConfigVersion != (new ConfigFile().ConfigVersion))
            {
                // Outdated Version!
                File.Move(path, Path.Combine(new FileInfo(path).DirectoryName, "TerraEconomy.old.json"));

                TShock.Log.ConsoleError("[TerraEconomy] Configuration file is outdated! Making a backup of the current one and generating a new.");

                ConfigFile config = new ConfigFile();

                File.WriteAllText(path, JsonConvert.SerializeObject(config, Formatting.Indented));
                
            }

            return conf;
        }
    }
}
