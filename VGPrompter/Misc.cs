using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace VGPrompter {

    public static class Utils {

        const string
            COMMA = ", ",
            RPY_EXT = "*.rpy";

        public static void WriteSerialized<T>(string path, T thing) {
            var formatter = new BinaryFormatter();
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None)) {
                formatter.Serialize(stream, thing);
            }
        }

        public static T LoadSerialized<T>(string path) {
            var formatter = new BinaryFormatter();
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                return (T)formatter.Deserialize(stream);
            }
        }

        public static T FromBinary<T>(byte[] bytes) {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream(bytes)) {
                return (T)formatter.Deserialize(stream);
            }
        }

        public static byte[] GetBinary<T>(T thing) {
            byte[] result;
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream()) {
                formatter.Serialize(stream, thing);
                result = stream.ToArray();
            }
            return result;
        }


        // http://stackoverflow.com/questions/10443461/c-sharp-array-findallindexof-which-findall-indexof
        public static int[] FindAllIndexOf<T>(this IEnumerable<T> values, T val) {
            return values.Select((b, i) => object.Equals(b, val) ? i : -1).Where(i => i != -1).ToArray();
        }

        public static int[] FindAllIndexOf2<T>(IEnumerable<T> items, T value) {
            int i = 0;
            var result = new List<int>();
            foreach (var item in items) {
                if (item.Equals(value)) {
                    result.Add(i);
                }
                i++;
            }
            return result.ToArray();
        }

        public static void LogArray<T>(string label, T[] a, Logger logger) {
            logger.Log(string.Format("{0}: {1}", label, string.Join(COMMA, a.Select(x => x.ToString()).ToArray())));
        }

        public static string PrintKeys<T>(Dictionary<string, T> d) {
            return "DICT: " + string.Join(COMMA, d.Keys.ToArray());
        }

        public static string[] GetScriptFiles(string dir, bool recursive = false) {

            if (!Directory.Exists(dir)) throw new Exception("Directory not found!");

            if (recursive) {
                var files = new List<string>();
                files.AddRange(Directory.GetFiles(dir, RPY_EXT));
                foreach (var d in Directory.GetDirectories(dir))
                    files.AddRange(GetScriptFiles(d, recursive: true));
                return files.ToArray();
            } else {
                return Directory.GetFiles(dir, RPY_EXT);
            }
        }

        public static T GetFromDictionary<T>(string key, Dictionary<string, T> d, Logger logger = null) {
            try {
                return !string.IsNullOrEmpty(key) ? d[key] : default(T);
            } catch (KeyNotFoundException ex) {
                if (logger != null)
                    logger.Log(string.Format("Tag not found: '{0}'", key));
                throw ex;
            }
        }

    }
}