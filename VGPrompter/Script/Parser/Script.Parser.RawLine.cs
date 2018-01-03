namespace VGPrompter {

    public partial class Script {

        public static partial class Parser {

            struct RawLine {

                public string Source { get; private set; }
                public string Text { get; private set; }
                public int LineNumber { get; private set; }

                public string ExceptionString => string.Format("'{0}' at line {1}: {2}!", Source, LineNumber, Text);

                public RawLine(string source, string text, int line_number) : this() {
                    Source = source;
                    Text = text;
                    LineNumber = line_number;
                }

                public RawLine Trim() {
                    return new RawLine(Source, Text.Trim(), LineNumber);
                }

            }

        }

    }

}