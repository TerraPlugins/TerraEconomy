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
        public string ConfigVersion = "1.0.0";
        // End of config variables

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
