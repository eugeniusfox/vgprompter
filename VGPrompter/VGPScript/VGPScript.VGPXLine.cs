namespace VGPrompter {

    public partial class VGPScript<TDialogueLine, TMenu, TChoice>
        where TDialogueLine : VGPBaseDialogueLine
        where TMenu : VGPBaseMenu
        where TChoice : VGPBaseChoice {

        abstract class VGPXLine {
            public int Rank { get; set; }

            public VGPXLine(int i) {
                Rank = i;
            }
        }

    }

}
