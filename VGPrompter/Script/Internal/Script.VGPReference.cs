using System;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        class VGPReference : Line, IWrappable {

            public string Tag { get; private set; }
            public Action Action { get; set; }

            public VGPReference(string label) {
                Tag = label;
            }

            public override bool IsValid() {
                return Action != null;
            }

            public IScriptLine ToWrapper() {
                return new Reference(Tag, Action);
            }
        }

    }

}