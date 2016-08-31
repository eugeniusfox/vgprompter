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
            public class If : Conditional {
                public If(string label, VGPBlock parent = null) : base(label, parent) { }
            }

            [Serializable]
            public class ElseIf : Conditional {
                public ElseIf(string label, VGPBlock parent = null) : base(label, parent) { }
            }

            [Serializable]
            public class Else : Conditional {
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