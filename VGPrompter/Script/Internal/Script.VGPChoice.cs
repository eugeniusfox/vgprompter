using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        abstract class VGPChoice : IterableContainer, IConditional, ITextual {

            [Serializable]
            public class VGPNamedChoice : VGPChoice {
                public string Label { get; private set; }

                public VGPNamedChoice() { }
                public VGPNamedChoice(string label, string text, VGPBlock parent, bool to_interpolate, string condition_label = null)
                    : base(text, parent, to_interpolate, condition_label) {
                    Label = label;
                }

                public override bool IsValid() {
                    return base.IsValid() && !string.IsNullOrEmpty(Label);
                }

                public new string ToString() {
                    return string.Format(@"{0} ""{1}"" {2}", Label, TextHash, Condition.Method.Name);
                }

                public override Choice ToWrapper(Script script, int index) {
                    return new Choice(index, GetInterpolatedText(script), IsTrue, Label);
                }
            }

            [Serializable]
            public class VGPAnonymousChoice : VGPChoice {
                public VGPAnonymousChoice() { }
                public VGPAnonymousChoice(string hash, VGPBlock parent, bool to_interpolate, string condition_label = null)
                    : base(hash, parent, to_interpolate, condition_label) { }

                public new string ToString() {
                    return string.Format(@"""{1}"" {2}", TextHash, Condition.Method.Name);
                }

                public override Choice ToWrapper(Script script, int index) {
                    return new Choice(index, GetInterpolatedText(script), IsTrue);
                }
            }

            [NonSerialized]
            Func<bool> _condition;

            public string Tag { get; private set; }
            public Func<bool> Condition { get { return _condition; } set { _condition = value; } }
            public string TextHash { get; private set; }
            public bool IsTrue { get { return Condition == null || Condition(); } }
            public bool ToInterpolate { get; private set; }

            public abstract Choice ToWrapper(Script script, int index);

            public string GetInterpolatedText(Script script) {
                return script._text_manager.GetText(Parent.Label, TextHash, ToInterpolate);
            }

            public VGPChoice() {
                Contents = new List<Line>();
            }

            public VGPChoice(string hash, VGPBlock parent, bool to_interpolate, string condition_label = null)
                : this() {
                TextHash = hash;
                Tag = condition_label;
                Parent = parent;
                ToInterpolate = to_interpolate;
            }

            public override bool IsValid() {
                return !string.IsNullOrEmpty(TextHash) && (string.IsNullOrEmpty(Tag) || Condition != null);
            }

            public override void Prime() {
                Condition = Script.GetCondition(Tag);
                Script.Logger.Log(Condition);
                base.Prime();
            }

        }

    }

}