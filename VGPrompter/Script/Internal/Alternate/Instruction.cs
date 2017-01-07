using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VGPrompter {
    class Instruction {

        public enum EType {
            If,
            ElseIf,
            Else,
            Return,
            Jump,
            Call,
            DialogueLine,
            Condition,
            Action
        }

        public EType Type { get; private set; }
        public string Text { get; private set; }
        public string[] Tags { get; private set; }
        public string ConditionName { get; private set; }
        public string ActionName { get; private set; }


    }
}
