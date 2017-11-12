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
                public string Code { get; private set; }

                public VGPMethod(string name, string docstring, string code) : this() {
                    Name = name;
                    Docstring = docstring;
                    Code = code;
                }

                public VGPMethod(int id, string source_file, int line_number, string code) : this() {
                    Name = string.Format("__vgp_{0}", id);
                    Docstring = string.Format("/// {0}:{1}", source_file, line_number);
                    Code = code;
                }
            }

            List<VGPMethod> Methods { get; set; }

            public EmbeddedCodeManager() {
                Methods = new List<VGPMethod>();
            }

            public int RegisterMethod(string source_file, int line_number, string code) {
                var id = Methods.Count;
                Methods.Add(new VGPMethod(id, source_file, line_number, code));
                return id;
            }


        }

    }
}
