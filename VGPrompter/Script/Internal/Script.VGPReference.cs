using System;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        class VGPReference : Line, IWrappable {

            [NonSerialized]
            Action _action;

            public string Tag { get; private set; }
            public string[] Arguments { get; private set; }

            public Action Action { get { return _action; } set { _action = value; } }

            public VGPReference(string label, string[] argv = null) {
                Tag = label;
                Arguments = argv;
            }

            public override bool IsValid() {
                return Action != null;
            }

            public IScriptLine ToWrapper(Script script = null) {
                return new Reference(Tag, Action);
            }
        }

    }

}