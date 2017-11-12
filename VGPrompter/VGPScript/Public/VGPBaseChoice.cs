namespace VGPrompter {

    public abstract class VGPBaseChoice : VGPTaggedTextCallable {

        public string Condition { get; protected set; }

        public VGPBaseChoice(string text, string character_tag = null, string condition = null)
            : base(text, character_tag) {
            Condition = condition;
        }

    }

}
