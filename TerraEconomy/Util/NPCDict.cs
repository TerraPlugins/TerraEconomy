using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerraEconomy.Util
{

    public static class NPCDict
    {
        public static Dictionary<string, float> NPCMoney = new Dictionary<string, float>()
        {
            {"Slime", 2},
            {"Zombie", 5},
        };
    }
}
