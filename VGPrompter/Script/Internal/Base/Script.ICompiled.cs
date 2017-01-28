namespace VGPrompter {

    public partial class Script {

        interface ICompiled { }

        interface ICompiled<T> : ICompiled where T : Line {
            T FromBinary(byte[] bytes);
        }

    }

}