using System.Collections.Generic;
using System;
using System.Linq;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        class Menu : PickableContainer<Choice>, IWrappable {

            public Menu(Block parent) {
                Parent = parent;
                InitializeContainer();
            }

            public Menu(List<Choice> choices, Block parent)
                : this(parent) {
                Contents = choices ?? new List<Choice>();
            }

            public Menu FilteredMenu() {
                return IsEmpty() ? null : new Menu(Contents.FindAll(
                    x => x.Condition == null || x.Condition() == true), Parent);
            }

            public new bool IsEmpty() {
                return Contents.TrueForAll(
                    x => x.Condition != null && x.Condition() == false);
            }

            public override Choice GetContent() {
                return null;
            }

            public new string ToString() {
                return "MENU: " + string.Join(COMMA, Contents.Select(x => x.Text).ToArray());
            }

            public new void Prime() {
                Script.NumberOfMenus++;
                foreach (var item in Contents)
                    item.Prime();
            }

            public IScriptLineWrapper ToWrapper() {

                return new MenuWrapper(Contents
                    .Select((x, i) => x.ToWrapper(i))
                    .ToList());

                /*var choices = new List<ChoiceWrapper>();

                for (int i = 0; i < Count; i++)
                    if (Contents[i].IsTrue)
                        choices.Add(Contents[i].ToWrapper(i));

                return new MenuWrapper(choices);*/

                /*return new MenuWrapper(Contents
                    .Where(x => x.IsTrue)
                    .Select(x => x.ToWrapper())
                    .ToList());*/
            }

            public override bool IsValid() { return true; }

        }

    }

}