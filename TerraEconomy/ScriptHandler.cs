using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using TShockAPI;
using CSScriptLibrary;
using System.Reflection;

namespace TerraEconomy
{
    public class ScriptHandler
    {
        private string ScriptDir;
        private List<string> Scripts = new List<string>();
        private List<Tuple<TeconomyScript, string>> CompiledScripts = new List<Tuple<TeconomyScript, string>>();

        public ScriptHandler(string script_dir)
        {
            ScriptDir = script_dir;

            Scripts = Directory.GetFiles(script_dir, "*.cs").ToList();
            TShock.Log.ConsoleInfo("[TerraEconomy] Scripts loaded: {0}", string.Join(", ", Scripts.Select(x => new FileInfo(x).Name.Replace(".cs", ""))));
            CSScript.EvaluatorConfig.Access = EvaluatorAccess.Singleton;
            CSScript.Evaluator.ReferenceAssembly(Assembly.GetExecutingAssembly());
            CSScript.Evaluator.ReferenceAssemblyByName("System");
            CSScript.Evaluator.ReferenceAssembly(typeof(TShock).Assembly);
            CSScript.Evaluator.ReferenceAssembly(typeof(Mono.Data.Sqlite.SqliteConvert).Assembly);
            CSScript.Evaluator.ReferenceAssembly(typeof(MySql.Data.MySqlClient.MySqlConnection).Assembly);

            foreach (var s in Scripts)
            {
                var name = new FileInfo(s).Name.Replace(".cs", "");
                try
                {
                    var scr = new Tuple<TeconomyScript, string>((TeconomyScript)CSScript.Evaluator.LoadFile(s), name);
                    CompiledScripts.Add(scr);
                }
                catch(Exception e)
                {
                    TShock.Log.ConsoleError("[TerraEconomy] Error compiling the script <{0}>:\n{1}",
                       name, e.Message);
                }
            }
        }

        public void CallInit()
        {
            foreach(var script in CompiledScripts)
            {
                try
                {
                    script.Item1?.Initialize();
                }
                catch(Exception err)
                {
                    TShock.Log.ConsoleError("[TerraEconomy] The script <{0}> has thrown an error when initializing: {1}",
                        script.Item2, err.Message);
                }
            }
        }

        public void CallDispose(bool disposing)
        {
            foreach (var script in CompiledScripts)
            {
                try
                {
                    script.Item1?.Dispose(disposing);
                }
                catch (Exception err)
                {
                    TShock.Log.ConsoleError("[TerraEconomy] The script <{0}> has thrown an error when disposing: {1}",
                        script.Item2, err.Message);
                }
            }
        }
    }
}
