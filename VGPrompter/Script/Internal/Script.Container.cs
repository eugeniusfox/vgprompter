using System.Collections.Generic;
using System;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        abstract class Container<T> : Line where T : Line {

            public abstract Script Script { get; set; }

            public List<T> Contents { get; set; }

            public int Count { get { return Contents != null ? Contents.Count : 0; } }

            public void InitializeContainer() {
                Contents = new List<T>();
            }

            public virtual bool IsEmpty {
                get { return Contents == null || Contents.Count == 0; }
            }

            public override void Validate() {
                if (Contents == null || Contents.Count == 0)
                    throw new Exception(string.Format("Empty container '{0}'!", ToString()));

                foreach (var x in Contents)
                    x.Validate();

                base.Validate();
            }

            public virtual void Prime() {
                if (Contents == null)
                    Script.Logger.Log("Null contents!");
                foreach (var item in Contents) {
                    if (item is Reference) {
                        var reference = item as Reference;
                        reference.Action = Script.GetAction(reference.Tag);
                        Script.Logger.Log(reference.Action);
                    } else if (item is Conditional) {
                        (item as Conditional).Prime();
                    } else if (item is Choice) {
                        (item as Choice).Prime();
                    } else if (item is GoTo) {
                        var gt = item as GoTo;
                        gt.SetTarget(Script);
                    } else if (item is Menu) {
                        (item as Menu).Prime();
                    } else if (item is IfElse) {
                        var ifelse = item as IfElse;
                        ifelse.Prime();
                    } else if (item is IterableContainer) {
                        if (item is IConditional) {
                            var conditional = item as IConditional;
                            conditional.Condition = Script.GetCondition(conditional.Tag);
                        }
                        (item as IterableContainer).Prime();
                    }
                }
            }
        }
    }
}