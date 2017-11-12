using System.Linq;
using System.Text.RegularExpressions;

namespace VGPrompter {

    public partial class Script {

        public static partial class Parser {

            public struct RawLine {

                const char COMMENT_CHAR = '#';
                static Regex comment_quotes_re = new Regex(@"(?<=(?:"".*?"").*?)\#.*?$", RegexOptions.Compiled);
                static Regex comment_no_quotes_re = new Regex(@"(\#.*?)$", RegexOptions.Compiled);

                public string Source { get; private set; }
                public string Text { get; private set; }
                public int Index { get; private set; }

                public string ExceptionString => string.Format("'{0}' at line {1}: {2}!", Source, Index, Text);

                public bool IsEmptyOrComment {
                    get {
                        var trimmed_text = Text.Trim();
                        return !string.IsNullOrEmpty(trimmed_text) && trimmed_text[0] != COMMENT_CHAR;
                    }
                }

                public RawLine(string source, string text, int index) : this() {
                    Source = source;
                    Index = index;

                    // Handle in-line comments
                    Text = (text.Contains('#') ?
                        (text.Contains('"') ?
                            comment_quotes_re.Replace(text, string.Empty) :
                            comment_no_quotes_re.Replace(text, string.Empty)) :
                        text).TrimEnd();
                }

                public RawLine Trim() {
                    return new RawLine(Source, Text.Trim(), Index);
                }

            }

        }

    }

}
