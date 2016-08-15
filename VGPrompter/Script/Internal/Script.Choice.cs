using System.Collections.Generic;
using System;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        abstract class Choice : IterableContainer, IConditional {

            [Serializable]
            public class NamedChoice : Choice {
                public string Label { get; private set; }

                public NamedChoice() { }
                public NamedChoice(string label, string text, Block parent, string condition_label = null)
                    : base(text, parent, condition_label) {
                    Label = label;
                }

                public override bool IsValid() {
                    return base.IsValid() && !string.IsNullOrEmpty(Label);
                }

                public new string ToString() {
                    return string.Format(@"{0} ""{1}"" {2}", Label, Text, Condition.Method.Name);
                }

                public override ChoiceWrapper ToWrapper(int index) {
                    return new ChoiceWrapper(index, Text, IsTrue, Label);
                }
            }

            [Serializable]
            public class AnonymousChoice : Choice {
                public AnonymousChoice() { }
                public AnonymousChoice(string text, Block parent, string condition_label = null)
                    : base(text, parent, condition_label) { }

                public new string ToString() {
                    return string.Format(@"""{1}"" {2}", Text, Condition.Method.Name);
                }

                public override ChoiceWrapper ToWrapper(int index) {
                    return new ChoiceWrapper(index, Text, IsTrue);
                }
            }

            public string Tag { get; private set; }
            public Func<bool> Condition { get; set; }
            public string Text { get; private set; }
            public bool IsTrue { get { return Condition == null || Condition(); } }

            public abstract ChoiceWrapper ToWrapper(int index);

            public Choice() {
                Contents = new List<Line>();
            }

            public Choice(string text, Block parent, string condition_label = null)
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

        }

    }

}