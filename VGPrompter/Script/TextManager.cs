using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace VGPrompter {

    public partial class Script {

        internal class TextManager {

            const string
                GLOBAL_TAG = "$",
                GLOBAL_FORMAT = GLOBAL_TAG + ";{0};{1}\n",
                DIALOGUE_FORMAT = "{0};{1};{2}\n";

            static readonly char[] SEPARATOR = new char[1] { ';' };

            MD5 _md5;

            Dictionary<string, Dictionary<string, string>> _dialogue_strings;
            Dictionary<string, string> _global_strings;

            public string GetHash(string text) {

                var data = _md5.ComputeHash(Encoding.UTF8.GetBytes(text));
                var sb = new StringBuilder();

                foreach (var b in data) {
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();

                //return string.Join("", data.Select(x => x.ToString("x2")).ToArray());
            }

            public TextManager() {
                _md5 = MD5.Create();
                _dialogue_strings = new Dictionary<string, Dictionary<string, string>>();
                _global_strings = new Dictionary<string, string>();
            }

            public string GetText(string label, string hash) {
                return _dialogue_strings[label][hash];
            }

            public string AddText(string label, string text) {
                var hash = GetHash(text);
                if (!_dialogue_strings.ContainsKey(label)) {
                    _dialogue_strings[label] = new Dictionary<string, string>();
                }

                _dialogue_strings[label][hash] = text;

                /*try {
                    _dialogue_strings[label][hash] = text;
                } catch (KeyNotFoundException ex) {
                    throw new System.Exception(ex.Data.ToString());
                }*/
                return hash;
            }

            public string GetGlobalText(string key) {
                return _global_strings[key];
            }

            public bool TryGetGlobalText(string key, out string text) {
                return _global_strings.TryGetValue(key, out text);
            }

            public void AddGlobalText(string key, string text) {
                _global_strings[key] = text;
            }

            public bool IsGlobalTextDefined(string key) {
                return _global_strings.ContainsKey(key);
            }

            public void ToCSV(string path) {

                var sb = new StringBuilder();

                /*foreach (var s in _global_strings) {
                    sb.Append(string.Format(GLOBAL_FORMAT, s.Key, s.Value));
                }*/

                foreach (var label in _dialogue_strings) {
                    foreach (var s in label.Value) {
                        sb.Append(string.Format(DIALOGUE_FORMAT, label.Key, s.Key, s.Value));
                    }
                }

                File.WriteAllText(path, sb.ToString());
            }

            public void FromCSV(string path) {

                var tokens = new string[3];
                foreach (var row in File.ReadAllLines(path)) {

                    tokens = row.Split(SEPARATOR, 3);

                    if (tokens[0] == GLOBAL_FORMAT) {
                        _global_strings.Add(tokens[1], tokens[2]);
                    } else {
                        _dialogue_strings[tokens[0]][tokens[1]] = tokens[2];
                    }

                }

            }

        }

    }
}
