using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraEconomy.Util;
using TShockAPI;

namespace TerraEconomy
{
    public class TeconomyScript
    {
        public void InfoLog(string msg) => TShock.Log.ConsoleInfo(msg);
        public void InfoLog(string format, params object[] args) => TShock.Log.ConsoleInfo(format, args);
        public ConfigFile GetConfig() => TerraEconomy.Config;

        // TODO: Add all necessary functions here

        public virtual void Initialize()
        {
        }

        public virtual void Dispose(bool disposing)
        {

        }
    }
}
