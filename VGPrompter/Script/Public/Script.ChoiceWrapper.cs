using System;

namespace VGPrompter {

    public partial class Script {

        public class ChoiceWrapper : IScriptLineWrapper {

            public int Index { get; private set; }
            public string Tag { get; private set; }
            public string Text { get; private set; }
            public bool IsTrue { get; set; }

            public ChoiceWrapper(int index, string text, bool is_true, string tag = null) {

                Index = index;
                Tag = tag ?? string.Empty;
                Text = text;
                IsTrue = is_true;

            }

            public new string ToString() {
                return (string.IsNullOrEmpty(Tag) ? string.Empty : (Tag + ": ")) + Text;
            }

        }

    }

}
