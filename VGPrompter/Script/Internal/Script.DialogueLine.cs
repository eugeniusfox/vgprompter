using System;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        class DialogueLine : Line, IWrappable {

            public string Label { get; private set; }
            public string Text { get; private set; }

            public DialogueLine() { }

            public DialogueLine(string label, string text) {
                Label = label;
                Text = text;
            }

            public override bool IsValid() { return !string.IsNullOrEmpty(Text); }

            public new string ToString() {
                return (string.IsNullOrEmpty(Label) ? "<Anonymous>" : Label) + ": " + Text;
            }

            public IScriptLineWrapper ToWrapper() {
                return new LineWrapper(Text, Label);
            }

        }

    }

}