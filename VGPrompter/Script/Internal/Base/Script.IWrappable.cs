namespace VGPrompter {

    public partial class Script {

        interface IWrappable {
            IScriptLine ToWrapper(Script script = null);
        }

    }

}