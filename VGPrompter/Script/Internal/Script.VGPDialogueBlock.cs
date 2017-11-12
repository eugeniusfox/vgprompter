using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VGPrompter {

    public partial class Script {

        class VGPDialogueBlock : IterableContainer {

            public override bool IsValid() => true;

            public void AddDialogueLine(VGPDialogueLine line) {
                Contents.Add(line);
            }
        }

    }

}
