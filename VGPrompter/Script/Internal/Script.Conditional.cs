using System;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        class Conditional : IterableContainer, IConditional {

            public string Tag { get; private set; }
            public Func<bool> Condition { get; set; }
            public bool IsTrue { get { return Condition(); } }

            [Serializable]
            public class If : Conditional {
                public If(string label, Block parent = null) : base(label, parent) { }
            }

            [Serializable]
            public class ElseIf : Conditional {
                public ElseIf(string label, Block parent = null) : base(label, parent) { }
            }

            [Serializable]
            public class Else : Conditional {
                public Else(Block parent = null) : base(TRUE, parent) { }
            }

            public Conditional() {
                InitializeContainer();
            }

            Conditional(string label, Block parent = null)
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