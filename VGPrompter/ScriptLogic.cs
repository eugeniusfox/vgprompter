using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace VGPrompter {

    public abstract class ScriptLogic {

        public Script Script { get; private set; }

        public Dictionary<string, Func<bool>> Conditions { get; private set; }
        public Dictionary<string, Action> Actions { get; private set; }

        public ScriptLogic() {
            Conditions = GetConditions();
            Actions = GetActions();
        }

        /*public void LoadRawScript(string path, string starting_label = null, bool validate = false) {

            Script = LoadRenPyScript(path);

            if (Script != null) {
                if (!string.IsNullOrEmpty(starting_label)) {
                    Script.StartingLabel = starting_label;
                }

                Script.Prime();
            }

            if (validate) {
                Script.Validate();
            }

            Debug.Log(Script.StartingLabel);
        }*/

        public void CompileScript(string path, Script script) {
            var formatter = new BinaryFormatter();
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None)) {
                formatter.Serialize(stream, script);
            }
        }

        public Script LoadCompiledScript(string path) {
            var formatter = new BinaryFormatter();
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                return (Script)formatter.Deserialize(stream);
            }
        }

        /*public NewScript LoadRenPyScript(string path) {
            var sp = new ScriptParser(path);
            return sp.CompileScript(this, validate: false);
        }*/


        /* =============================================================================================================== */


        public Dictionary<string, Func<bool>> GetConditions() {
            var methods = GetMethodsBySig(GetType(), typeof(bool), new Type[] { });
            return GetDictionary<Func<bool>>(methods).ToDictionary(x => x.Key, x => (Func<bool>)x.Value);
        }

        public static bool True() {
            return true;
        }

        public static bool False() {
            return false;
        }

        public Dictionary<string, Action> GetActions() {
            var methods = GetMethodsBySig(GetType(), typeof(void), new Type[] { });
            //methods.ToList().ForEach(x => Debug.Log(x));
            var tmp = GetDictionary<Action>(methods);
            return tmp.ToDictionary(x => x.Key, x => (Action)x.Value);
        }

        static Dictionary<string, T> GetDictionary<T>(List<T> functions) where T : class {
            return functions
                .Select(x => new KeyValuePair<string, T>((x as Delegate).Method.Name, x))
                .ToDictionary(x => x.Key, x => x.Value);
        }


        static Dictionary<string, Delegate> GetDictionary<T>(IEnumerable<MethodInfo> functions) where T : class {
            return functions
                .Select(x => new KeyValuePair<string, Delegate>(x.Name, Delegate.CreateDelegate(typeof(T), x)))
                .ToDictionary(x => x.Key, x => x.Value);
        }

        // Adapted from: http://stackoverflow.com/questions/5152346/get-only-methods-with-specific-signature-out-of-type-getmethods
        static IEnumerable<MethodInfo> GetMethodsBySig(Type type, Type returnType, params Type[] parameterTypes) {
            return type.GetMethods().Where((m) => {
                if (!m.IsPublic) return false;

                if (m.ReturnType != returnType)
                    return false;

                var parameters = m.GetParameters();
                if ((parameterTypes == null || parameterTypes.Length == 0))
                    return parameters.Length == 0;

                if (parameters.Length != parameterTypes.Length)
                    return false;

                for (int i = 0; i < parameterTypes.Length; i++) {
                    if (parameters[i].ParameterType != parameterTypes[i])
                        return false;
                }

                return true;
            });
        }

    }
}