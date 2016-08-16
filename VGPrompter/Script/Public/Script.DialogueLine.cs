namespace VGPrompter {

    public partial class Script {

        public class DialogueLine : IScriptLine {

            public string Tag { get; private set; }
            public string Text { get; private set; }

            public DialogueLine(string text, string tag = null) {
                Tag = tag ?? string.Empty;
                Text = text;
            }

            public new string ToString() {
                return (string.IsNullOrEmpty(Tag) ? "<Anonymous>" : Tag) + ": " + Text;
            }

        }

    }

}
