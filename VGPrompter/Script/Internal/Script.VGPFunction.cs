using System;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        class VGPFunction : VGPBaseReference {

            [NonSerialized]
            Delegate _delegate;

            object[] _argv;

            public Delegate Delegate { get { return _delegate; } set { _delegate = value; } }

            public VGPFunction(string label, params object[] argv) : base(label) {
                _argv = argv;
            }

            public override bool IsValid() {
                return Delegate != null;
            }

            public override IScriptLine ToWrapper(Script script = null) {
                return new Reference(Tag, () => Delegate.DynamicInvoke(_argv));
            }

        }

    }

}