using System;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        class VGPDialogueLine : Line, IWrappable, ITranspilable {

            public string Label { get; private set; }
            public string Text { get; private set; }

            public VGPDialogueLine() { }

            public VGPDialogueLine(string label, string text) {
                Label = label;
                Text = text;
            }

            public override bool IsValid() { return !string.IsNullOrEmpty(Text); }

            public new string ToString() {
                return (string.IsNullOrEmpty(Label) ? "<Anonymous>" : Label) + ": " + Text;
            }

            public IScriptLine ToWrapper() {
                return new DialogueLine(Text, Label);
            }

            public string[] Transpile() {
                return new string[] {
                    string.Format("yield return new DialogueLine({0}, {1});", Text, Label)
                };
            }

        }

    }

}