using System;

namespace VGPrompter {

    public partial class Script {

        interface IConditional {

            string Tag { get; }
            Func<bool> Condition { get; set; }
            bool IsTrue { get; }

        }

    }

}