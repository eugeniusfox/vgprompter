using System.Collections.Generic;
using System.Linq;

namespace VGPrompter {

    public partial class Script {

        public class Menu : IScriptLine {

            const string MENU = "MENU";

            public int Count {
                get { return Choices != null ? Choices.Count : 0; }
            }

            public int TrueCount {
                get { return Choices != null ? Choices.Count(x => x.IsTrue) : 0; }
            }

            public List<Choice> Choices { get; private set; }

            public List<Choice> TrueChoices {
                get { return Choices.Where(x => x.IsTrue).ToList(); }
            }

            public Menu(List<Choice> choices) {
                Choices = choices;
            }

            public new string ToString() {
                return string.Format("{0}: {1}", MENU,
                    string.Join(COMMA, Choices.Select(
                        (x, i) => string.Format("[{0}] {1}", i, x.ToString())).ToArray()));
            }

        }

    }

}
