using System.Collections.Generic;
using System;
using System.Collections;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        abstract class IterableContainer : ChildContainer<Line> {

            int _current_index = -1;

            IEnumerator OnMenu(
                MenuWrapper menu,
                Func<MenuWrapper, IEnumerator<int?>> SelectChoice,
                Func<MenuWrapper, ChoiceWrapper, IEnumerator> OnChoiceSelected) {

                Script.CurrentChoiceIndex = null;

                int choice_index = 0;

                using (var z = SelectChoice(menu)) {
                    while (z.MoveNext()) {
                        if (z.Current.HasValue) {
                            choice_index = z.Current.Value;
                            break;
                        }
                        yield return null;
                    }
                }

                var choice = menu.Choices.Find(x => x.Index == choice_index);

                if (choice == null)
                    throw new Exception("Invalid choice index!");

                Script.CurrentChoiceIndex = (uint)choice.Index;

                yield return OnChoiceSelected(menu, choice);
            }

            public IEnumerator GetEnumerator(
                Func<LineWrapper, IEnumerator> OnLine,
                Func<MenuWrapper, IEnumerator<int?>> SelectChoice,
                Func<MenuWrapper, ChoiceWrapper, IEnumerator> OnChoiceSelected,
                Func<IEnumerator> OnReturn = null,
                Func<ReferenceWrapper, IEnumerator> OnReference = null) {

                yield return GetEnumerator(OnLine, menu => OnMenu(menu, SelectChoice, OnChoiceSelected), OnReturn, OnReference);
            }

            public IEnumerator GetEnumerator(
                Func<LineWrapper, IEnumerator> OnLine,
                Func<MenuWrapper, IEnumerator> OnMenu,
                Func<IEnumerator> OnReturn = null,
                Func<ReferenceWrapper, IEnumerator> OnReference = null) {

                IScriptLineWrapper w;

                foreach (IWrappable x in this) {

                    if (x == null)
                        throw new Exception("Can't wrap this line!");

                    w = x.ToWrapper();

                    if (w is MenuWrapper) {

                        var menu = w as MenuWrapper;
                        yield return OnMenu(menu);

                    } else if (w is LineWrapper) {

                        var line = w as LineWrapper;
                        yield return OnLine(line);

                    } else if (w is ReferenceWrapper) {

                        var reference = w as ReferenceWrapper;
                        if (OnReference != null) {
                            yield return OnReference(reference);
                        } else {
                            reference.Action();
                        }

                    }

                }

                if (OnReturn != null)
                    yield return OnReturn();
            }

            public IEnumerator<IScriptLineWrapper> GetWrapperEnumerator() {
                foreach (IWrappable x in this) {
                    if (x == null) throw new Exception("Can't wrap this line!");
                    yield return x.ToWrapper();
                }
            }

            public IEnumerator<Line> GetEnumerator() {

                // Reset current state 
                if (Script.CurrentIterable != this) {
                    Script.CurrentIterable = this;
                    Script.CurrentIterableLine = -1;
                }

                var parent = this as Block ?? Parent;

                var id = Script.CurrentBlockID++;

                var i = 0;

                foreach (var x in Contents) {

                    // Skip already shown lines
                    if (i++ < Script.CurrentIterableLine - Script.CurrentIterableLineOffset)
                        continue;

                    // Update current line
                    Script.CurrentIterableLine++;

                    //Logger.Log(x.ToString());

                    if (Script.HasReturned || parent.FromInstanceIDs.Contains(id))
                        break;

                    if (x is Menu) {

                        var menu = x as Menu;

                        yield return menu;

                        var ichoice = menu.Script.CurrentChoiceIndex;

                        if (!ichoice.HasValue)
                            throw new Exception("Choice not selected!");

                        var j = (int)ichoice.Value;
                        
                        menu.Script.CurrentChoiceIndex = null;

                        var choice = menu.Contents[j];

                        foreach (var y in choice)
                            yield return y;

                    } else if (x is IfElse) {

                        var ifelse = x as IfElse;
                        var option = ifelse.GetContent();

                        if (option == null)
                            Script.Logger.Log("No condition evaluated to true!");

                        foreach (var y in option)
                            yield return y;

                    } else if (x is IterableContainer) {

                        foreach (var y in (x as IterableContainer))
                            yield return y;

                    } else if (x is GoTo) {

                        var gt = x as GoTo;

                        if (!gt.IsCall)
                            parent.RegisterID(id);

                        foreach (var y in gt.Target)
                            yield return y;

                    } else if (x is Return) {

                        Script.HasReturned = true;
                        break;

                    } else {

                        yield return x;

                    }
                }

                if (!Script.HasReturned && this is While && (this as While).Evaluate()) {
                    foreach (var y in this)
                        yield return y;
                }
            }

        }

    }

}