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

            Type[] ArgumentTypes { get => Delegate.GetType().GetGenericArguments(); }

            public Delegate Delegate { get { return _delegate; } set { _delegate = value; } }
            public bool HasContext { get => ArgumentTypes[0] == typeof(Script); }

            public VGPFunction(string label, params object[] argv) : base(label) {
                _argv = argv;
            }

            public override bool IsValid() {
                var offset = HasContext ? 1 : 0;
                return
                    Delegate != null &&
                    ArgumentTypes.Count() == _argv.Length + offset &&
                    _argv.Select((x, i) => x.GetType() == ArgumentTypes[i + offset]).All(x => x);
            }

            Action GetAction(Script script = null) {

                if (HasContext) {
                    if (script == null) throw new Exception("Missing Script reference!");
                    object[] args = { script };
                    args = args.Concat(_argv).ToArray();
                    return () => Delegate.DynamicInvoke(args.ToArray());
                } else {
                    return () => Delegate.DynamicInvoke(_argv);
                }

            }

            public override IScriptLine ToWrapper(Script script = null) {
                return new Reference(Tag, GetAction(script));
            }

        }

    }

}