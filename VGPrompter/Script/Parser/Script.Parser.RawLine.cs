namespace VGPrompter {

    public partial class Script {

        public static partial class Parser {

            class RawLine {

                public string Source { get; private set; }
                public string Text { get; private set; }
                public int LineNumber { get; private set; }
                public int Level { get; set; }
                public Token[] Tokens { get; private set; }

                public string ExceptionString => string.Format("'{0}' at line {1}: {2}!", Source, LineNumber, Text);

                public RawLine() { }

                public RawLine(string source, string text, int line_number, int level = 0) {
                    Source = source;
                    Text = text;
                    LineNumber = line_number;
                    Level = level;
                    Tokens = Tokenize(text);
                }

                public RawLine Trim() {
                    return new RawLine(Source, Text.Trim(), LineNumber, Level);
                }

            }

        }

    }

}