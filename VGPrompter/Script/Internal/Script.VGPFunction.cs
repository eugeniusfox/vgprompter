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

            int ArgumentIndexOffset { get => HasContext ? 1 : 0; }
            bool HasDelegate { get => Delegate != null; }
            bool IsArityValid { get => ArgumentTypes.Count() == _argv.Length + ArgumentIndexOffset; }
            bool AreArgumentsValid { get => _argv.Select((x, i) => x.GetType() == ArgumentTypes[i + ArgumentIndexOffset]).All(x => x); }

            protected string ValidationErrorMessage { get {

                    if (!HasDelegate) return "Null Delegate!";

                    if (!IsArityValid) return string.Format(
                        "Expected {0} arguments, got {1}!",
                        ArgumentTypes.Count() - ArgumentIndexOffset, _argv.Length);

                    if (!AreArgumentsValid) return string.Format(
                        "Expected argument types <{1}>, got <{0}>!",
                        string.Join(", ", _argv.Select(x => x.GetType().ToString()).ToArray()),
                        string.Join(", ", ArgumentTypes.Select(x => x.ToString()).ToArray()));

                    return string.Empty;

                }
            }

            public override bool IsValid() {
                var result = HasDelegate && IsArityValid && AreArgumentsValid;
                if (result) {
                    return true;
                } else {
                    throw new Exception(Tag + ": " + ValidationErrorMessage);
                }
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