namespace VGPrompter {

    public partial class Script {

        public static partial class Parser {

            struct RawLine {

                public string Source { get; private set; }
                public string Text { get; private set; }
                public int Index { get; private set; }

                public string ExceptionString => string.Format("'{0}' at line {1}: {2}!", Source, Index, Text);

                public RawLine(string source, string text, int index) : this() {
                    Source = source;
                    Text = text;
                    Index = index;
                }

                public RawLine Trim() {
                    return new RawLine(Source, Text.Trim(), Index);
                }

            }

        }

    }

}