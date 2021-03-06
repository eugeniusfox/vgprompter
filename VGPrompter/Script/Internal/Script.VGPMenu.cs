﻿using System.Collections.Generic;
using System;
using System.Linq;

namespace VGPrompter {

    public partial class Script {

        [Serializable]
        class VGPMenu : PickableContainer<VGPChoice>, IWrappable {

            public int? Duration { get; private set; }

            public VGPMenu(VGPBlock parent, int? duration = null) {
                Parent = parent;
                Duration = duration;
                InitializeContainer();
            }

            public VGPMenu(List<VGPChoice> choices, VGPBlock parent, int? duration = null)
                : this(parent, duration) {
                Contents = choices ?? new List<VGPChoice>();
            }

            public VGPMenu FilteredMenu() {
                return IsEmpty() ? null : new VGPMenu(Contents.FindAll(
                    x => x.Condition == null || x.Condition() == true), Parent);
            }

            public new bool IsEmpty() {
                return Contents.TrueForAll(
                    x => x.Condition != null && x.Condition() == false);
            }

            public override VGPChoice GetContent() {
                return null;
            }

            public new string ToString() {
                return "MENU: " + string.Join(COMMA, Contents.Select(x => x.TextHash).ToArray());
            }

            public new void Prime() {
                Script.NumberOfMenus++;
                foreach (var item in Contents)
                    item.Prime();
            }

            public IScriptLine ToWrapper(Script script = null) {

                return new Menu(Contents
                    .Select((x, i) => x.ToWrapper(script, i))
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