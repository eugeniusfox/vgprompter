using System;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        class VGPReference : Line, IWrappable {

            [NonSerialized]
            Action _action;

            public string Tag { get; private set; }
            public Action Action { get { return _action; } set { _action = value; } }

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