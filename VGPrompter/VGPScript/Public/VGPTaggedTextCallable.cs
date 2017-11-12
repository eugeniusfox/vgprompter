namespace VGPrompter {

    public abstract class VGPTaggedTextCallable : VGPCallable {

        public string Text { get; protected set; }
        public string CharacterTag { get; protected set; }

        public VGPTaggedTextCallable(string text, string character_tag = null) {
            Text = text;
            CharacterTag = character_tag;
        }

    }

}
