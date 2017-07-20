using System;
using System.Collections.Generic;
using System.Linq;

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

            Action GetAction(Script script = null) {
                var arg_types = Delegate.GetType().GetGenericArguments();
                if (arg_types[0] == typeof(Script)) {
                    if (script == null) throw new Exception("Missing Script reference!");

                    object[] args = { script };
                    args = args.Concat(_argv).ToArray();
                    return () => Delegate.DynamicInvoke(args);
                }

                return () => Delegate.DynamicInvoke(_argv);
            }

            public override IScriptLine ToWrapper(Script script = null) {
                return new Reference(Tag, GetAction(script));
            }

        }

    }

}