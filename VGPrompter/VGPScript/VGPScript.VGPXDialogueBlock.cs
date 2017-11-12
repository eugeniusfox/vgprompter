using System;
using System.Collections.Generic;

namespace VGPrompter {

    public partial class VGPScript<TDialogueLine, TMenu, TChoice>
        where TDialogueLine : VGPBaseDialogueLine
        where TMenu : VGPBaseMenu
        where TChoice : VGPBaseChoice {

        [Serializable]
        class VGPXDialogueBlock : VGPXLine {

            public List<TDialogueLine> Lines { get; private set; }

            public VGPXDialogueBlock(int i) : base(i) { }

            public VGPXDialogueBlock(int i, List<TDialogueLine> lines) : this(i) {
                Lines = lines;
            }

            public void Add(TDialogueLine line) => Lines.Add(line);
            public int Count => Lines.Count;
            public bool IsEmpty => Count == 0;

        }

    }

}
