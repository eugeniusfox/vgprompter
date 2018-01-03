using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace VGPrompter {

    public partial class Script {

        public static partial class Parser {

            struct Arguments {

                public List<object> PositionalArguments { get; private set; }
                public Dictionary<string, object> KeywordArguments { get; private set; }

                public Arguments(List<object> args, Dictionary<string, object> kwargs) : this() {
                    PositionalArguments = args;
                    KeywordArguments = kwargs;
                }

                public static Arguments Parse(string s) {
                    var args = new List<object>();
                    var kwargs = new Dictionary<string, object>();

                    var argv = s.Split(COMMA_C).Select(a => a.Trim());

                    foreach (var a in argv) {
                        if (a.Contains(EQUAL_C)) {
                            var kvp = a.Split(EQUAL_C).Select(t => t.Trim()).ToArray();
                            if (!identifier_re.Match(kvp[0]).Success) throw new Exception(string.Format("Invalid key '{0}'!", kvp[0]));
                            kwargs.Add(kvp[0], kvp[1]);
                        } else {
                            if (kwargs.Count > 0) throw new Exception("!!!");
                            args.Add(a);
                        }
                    }

                    return new Arguments(args, kwargs);
                }

            }

        }

    }

}
