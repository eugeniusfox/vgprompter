namespace VGPrompter {

    public abstract class VGPBaseDialogueLine : VGPTaggedTextCallable {

        public VGPBaseDialogueLine(string text, string character_tag = null)
            : base(text, character_tag) { }

    }

}
