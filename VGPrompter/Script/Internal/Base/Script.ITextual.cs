namespace VGPrompter {

    public partial class Script {

        interface ITextual {
            bool ToInterpolate { get; }
            string GetInterpolatedText(Script script);
        }

    }

}