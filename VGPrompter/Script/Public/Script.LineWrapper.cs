namespace VGPrompter {

    public partial class Script {

        public class LineWrapper : IScriptLineWrapper {

            public string Tag { get; private set; }
            public string Text { get; private set; }

            public LineWrapper(string text, string tag = null) {
                Tag = tag ?? string.Empty;
                Text = text;
            }

            public new string ToString() {
                return (string.IsNullOrEmpty(Tag) ? "<Anonymous>" : Tag) + ": " + Text;
            }

        }

    }

}
