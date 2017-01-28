using System;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        class Conditional : IterableContainer, IConditional {

            [NonSerialized]
            Func<bool> _condition;

            public string Tag { get; private set; }
            public Func<bool> Condition { get { return _condition; } set { _condition = value; } }
            public bool IsTrue { get { return Condition(); } }

            [Serializable]
            public class If : Conditional, ICompiled<If> {
                static readonly int StatementLength = 8;
                public If(string label, VGPBlock parent = null) : base(label, parent) { }
                public If FromBinary(byte[] bytes) {
                    Convert.ToUInt16(bytes);
                    // TODO
                    var x = 8 << bytes[0];
                    return new If("");
                }
            }

            [Serializable]
            public class ElseIf : Conditional {
                static readonly int StatementLength = 8;
                public ElseIf(string label, VGPBlock parent = null) : base(label, parent) { }
            }

            [Serializable]
            public class Else : Conditional {
                static readonly int StatementLength = 4;
                public Else(VGPBlock parent = null) : base(TRUE, parent) { }
            }

            public Conditional() {
                InitializeContainer();
            }

            Conditional(string label, VGPBlock parent = null)
                : this() {
                Tag = label;
                Parent = parent;
            }

            public override bool IsValid() {
                if (Condition == null) Script.Logger.Log(Tag);
                return Condition != null;
            }

            public override void Prime() {
                Condition = Script.GetCondition(Tag);
                Script.Logger.Log(Condition);
                base.Prime();
            }

        }

    }

}