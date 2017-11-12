namespace VGPrompter {

    public partial class Script {

        interface IGenerator {
            string ToCSharpCode(int indent);
        }

    }

}