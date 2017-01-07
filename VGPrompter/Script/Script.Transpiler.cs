using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VGPrompter {
    public partial class Script {

        public class Transpiler {

            const string
                INDEX = "i",
                SWITCH = "switch";

            //public string 

            public Transpiler(Script script) {

                

            }

            /*string TranspileLine(Line line) {
                var x = line as VGPMenu;
                if (x != null) {
                    return 
                }
            }*/

            /*string line2case(IScriptLine line) {
                if (line is VGPDialogueLine) {
                    return 
                }
            }*/

            string GetLabelSwitch(List<IScriptLine> lines) {
                return GetSwitch(INDEX, "");
            }

            string GetSwitch(string var, string contents, int level = 0) {
                return GetBlock(SWITCH, var, contents, level);
            }

            string GetBlock(string keyword, string args, string contents, int level = 0) {
                var indent = new string('\t', level);
                return string.Format("{0}{1} ({2}) {{\n{0}\t{3}\n{0}}}", indent, keyword, args, contents);
            }

        }

    }
}
