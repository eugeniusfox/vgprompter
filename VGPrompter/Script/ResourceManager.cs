using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VGPrompter {

    public partial class Script {

        public class ResourceManager {
            public EmbeddedCodeManager CodeManager { get; set; }
            public TextManager TextManager { get; set; }

            public ResourceManager() {
                CodeManager = new EmbeddedCodeManager();
                TextManager = new TextManager();
            }

            public ResourceManager(EmbeddedCodeManager cm, TextManager tm) {
                CodeManager = cm;
                TextManager = tm;
            }
        }

        public class EmbeddedCodeManager {

            struct VGPMethod {

                public string Name { get; private set; }
                public string Docstring { get; private set; }
                public string[] Code { get; private set; }
                public Type ReturnType { get; private set; }
                public bool IsExpression { get; private set; }

                /*public VGPMethod(string name, string docstring, string code) : this() {
                    Name = name;
                    Docstring = docstring;
                    Code = code;
                    ReturnType = null;
                }*/

                public VGPMethod(int id, string source_file, int line_number, string code, Type return_type = null, bool is_expression = true) : this() {
                    Name = string.Format("__vgp_{0}", id);
                    Docstring = string.Format("/// {0}:{1}", source_file, line_number);
                    // Ideally: drop empty lines at the end of the snippet
                    Code = code.Split('\n').Select(x => x.TrimEnd()).ToArray();
                    ReturnType = return_type;
                    IsExpression = is_expression;
                }

                public string ToString(int indent = 0) {

                    var pad = indent > 0 ? new string('\t', indent) : string.Empty;

                    var s = string.Format(@"{0}{{ ""{1}"", () => ", pad, Name);

                    switch (Code.Length) {
                        case 1:
                            s += string.Format("{0}", Code[0].Trim());
                            break;
                        default:
                            // Replace indent characters with tabs...
                            // var min_indent = Code.Select(x => x.Length - x.TrimStart().Length).Where(x => x > 0).Min();
                            s += string.Format("{{{1}\n{0}}}", pad, string.Join(string.Empty,
                                Code.Select(x => string.Format("\n{0}{1}", pad, x)).ToArray()));
                            
                            break;
                    }

                    s += " }";

                    return s;

                }

            }

            List<VGPMethod> Methods { get; set; }

            public EmbeddedCodeManager() {
                Methods = new List<VGPMethod>();
            }

            public int RegisterMethod(string source_file, int line_number, string code, Type return_type = null) {
                var id = Methods.Count;
                var m = new VGPMethod(id, source_file, line_number, code, return_type);
                Methods.Add(m);
                Console.WriteLine(m.ToString(1));
                return id;
            }


        }

    }
}
