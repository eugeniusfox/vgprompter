using System.Collections.Generic;
using System;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        abstract class VGPChoice : IterableContainer, IConditional {

            [Serializable]
            public class VGPNamedChoice : VGPChoice {
                public string Label { get; private set; }

                public VGPNamedChoice() { }
                public VGPNamedChoice(string label, string text, VGPBlock parent, string condition_label = null)
                    : base(text, parent, condition_label) {
                    Label = label;
                }

                public override bool IsValid() {
                    return base.IsValid() && !string.IsNullOrEmpty(Label);
                }

                public new string ToString() {
                    return string.Format(@"{0} ""{1}"" {2}", Label, Text, Condition.Method.Name);
                }

                public override Choice ToWrapper(int index) {
                    return new Choice(index, Text, IsTrue, Label);
                }
            }

            [Serializable]
            public class VGPAnonymousChoice : VGPChoice {
                public VGPAnonymousChoice() { }
                public VGPAnonymousChoice(string text, VGPBlock parent, string condition_label = null)
                    : base(text, parent, condition_label) { }

                public new string ToString() {
                    return string.Format(@"""{1}"" {2}", Text, Condition.Method.Name);
                }

                public override Choice ToWrapper(int index) {
                    return new Choice(index, Text, IsTrue);
                }
            }

            [NonSerialized]
            Func<bool> _condition;

            public string Tag { get; private set; }
            public Func<bool> Condition { get { return _condition; } set { _condition = value; } }
            public string Text { get; private set; }
            public bool IsTrue { get { return Condition == null || Condition(); } }

            public abstract Choice ToWrapper(int index);

            public VGPChoice() {
                Contents = new List<Line>();
            }

            public VGPChoice(string text, VGPBlock parent, string condition_label = null)
                : this() {
                Text = text;
                Tag = condition_label;
                Parent = parent;
            }

            public override bool IsValid() {
                return !string.IsNullOrEmpty(Text) && (string.IsNullOrEmpty(Tag) || Condition != null);
            }

            public override void Prime() {
                Condition = Script.GetCondition(Tag);
                Script.Logger.Log(Condition);
                base.Prime();
            }

            /*public string[] Transpile() {
                //var choice = this.ToWrapper();
                return new string[] {"yield return new Choice()" };
            }*/

        }

    }

}