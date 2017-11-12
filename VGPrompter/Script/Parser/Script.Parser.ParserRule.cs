using System;

namespace VGPrompter {

    public partial class Script {

        public static partial class Parser {
            struct ParserRule {
                public string Keyword { get; private set; }
                public Func<string[], VGPBlock, Line> Constructor { get; private set; }
                public Func<string[], bool> Validator { get; private set; }
                public int? Count { get; private set; }

                public ParserRule(string keyword, Func<string[], VGPBlock, Line> constructor, int? count = null, Func<string[], bool> validator = null) : this() {
                    Keyword = keyword;
                    Constructor = constructor;
                    Validator = validator;
                    Count = count;
                }
            }

        }

    }

}