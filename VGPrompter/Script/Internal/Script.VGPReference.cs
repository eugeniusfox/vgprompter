using System;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        class VGPReference : VGPBaseReference {

            [NonSerialized]
            Action _action;

            public Action Action { get { return _action; } set { _action = value; } }

            public VGPReference(string label) : base(label) { }

            public override bool IsValid() {
                return Action != null;
            }

            public override IScriptLine ToWrapper(Script script = null) {
                return new Reference(Tag, Action);
            }

        }

    }

}