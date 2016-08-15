namespace VGPrompter {

    public partial class Script {

        interface IWrappable {
            IScriptLineWrapper ToWrapper();
        }

    }

}