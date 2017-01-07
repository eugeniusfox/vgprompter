using System;

namespace VGPrompter {

    public partial class Script {

        interface ITranspilable {

            string[] Transpile();

        }

    }

}