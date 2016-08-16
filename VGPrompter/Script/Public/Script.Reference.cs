using System;

namespace VGPrompter {

    public partial class Script {

        public class Reference : IScriptLine {

            public string Tag { get; private set; }
            public Action Action { get; private set; }

            public Reference(string tag, Action action) {
                Tag = tag;
                Action = action;
            }

            public new string ToString() {
                return "REFERENCE: " + Tag;
            }

        }

    }

}
