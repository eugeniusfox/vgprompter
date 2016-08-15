using System.Collections.Generic;
using System;
using System.Linq;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        class IfElse : PickableContainer<Conditional> {

            public IfElse(Block parent) {
                Parent = parent;
                Contents = new List<Conditional>();
            }

            public bool IsClosed { get { return !IsEmpty && Contents.Last() is Conditional.Else; } }

            public IfElse(Conditional first_condition, Block parent)
                : this(parent) {
                Contents.Add(first_condition);
            }

            public IfElse(List<Conditional> conditions, Block parent)
                : this(parent) {
                Contents = conditions;
            }

            public override bool IsEmpty {
                get { return Contents == null || Contents.Count == 0; }
            }

            public void AddCondition(Conditional condition) {

                if (IsClosed) throw new Exception("The IfElse block is finalized already!");

                if (condition is Conditional.If) {
                    if (!IsEmpty) throw new Exception("Invalid If block position!");
                } else if (condition is Conditional.ElseIf) {
                    if (IsEmpty) throw new Exception("Invalid ElseIf block position!");
                } else if (condition is Conditional.Else) {
                    if (IsEmpty) throw new Exception("Invalid Else block position!");
                }

                Contents.Add(condition);
            }

            public override Conditional GetContent() {
                Validate();
                return Contents.FirstOrDefault(x => x.Condition());
            }

            public override bool IsValid() {
                //return Contents != null && Contents.All(x => x.IsValid());
                return true;
            }

            public new void Prime() {
                foreach (var item in Contents)
                    item.Prime();
            }

        }

    }

}