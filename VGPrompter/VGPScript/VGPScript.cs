using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VGPrompter {

    [Serializable]
    public partial class VGPScript<TDialogueLine, TMenu, TChoice>
        where TDialogueLine : VGPBaseDialogueLine
        where TMenu : VGPBaseMenu
        where TChoice : VGPBaseChoice {

        class VGPXBlock {

            public string Language { get; private set; }
            public int RankOffset { get; private set; }
            public string Label { get; set; }
            public List<VGPXDialogueBlock> DialogueBlocks { get; set; }

            public VGPXBlock(string label, string language, int rank_offset, List<VGPXDialogueBlock> dialogue_blocks = null) {
                Label = label;
                Language = language;
                RankOffset = rank_offset;
                DialogueBlocks = dialogue_blocks;
            }

            public static VGPXBlock FromVGPBlock(Script.VGPBlock b, string language, int rank_offset) {

                string dlineclassname = typeof(TDialogueLine).FullName;

                var bx = new VGPXBlock(b.Label, language, rank_offset);
                VGPXDialogueBlock current_dialogue_block = null;
                var i = rank_offset;
                foreach (var c in b.Contents) {
                    if (c is Script.VGPDialogueLine) {
                        if (current_dialogue_block == null) {
                            current_dialogue_block = new VGPXDialogueBlock(i);
                        }
                        // current_dialogue_block.Add(typeof(TDialogueLine).GetConstructor().Invoke());
                        // WIP
                    } else {
                        // One-to-one object mapping
                        i++;
                    }
                }
                return bx;
            }

        }

        public VGPScript() { }

        static void ParseVGPBlock(Script.VGPBlock b) {

        }

        public static VGPScript<TDialogueLine, TMenu, TChoice> FromScript(Script syntax_tree) {
            var script = new VGPScript<TDialogueLine, TMenu, TChoice>();

            foreach (var b in syntax_tree.Blocks.Values) {

            }

            return script;
        }

    }
}
