using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;

namespace VGPrompter {
    public partial class Script {

        class Loader {

            enum StmtType {
                Else = 2,                           // 0	4
                Return = 3,                         // 0	4
                Pass = 4,                           // 0	-
                Menu = 5,                           // 0	4
                UnDef = 17,                         // 0	-
                If = 0,                             // 1 c	8
                ElseIf = 1,                         // 1 c	8
                Choice = 6,                         // 1 t	8
                DialogueLine = 7,                   // 1 t	8
                Call = 8,                           // 1 r	8
                Jump = 9,                           // 1 r	8
                Reference = 10,                     // 1 c	8
                Label = 11,                         // 1 r	6
                While = 12,                         // 1 c	8
                TaggedChoice = 13,                  // 2 rt	12
                ConditionalChoice = 14,             // 2 ct	12
                TaggedConditionalChoice = 15,       // 3	16
                TaggedDialogueLine = 16             // 2 rt	12
            };

            public string VGPCompilerPath { get; set; }
            public string InputFolder { get; set; }
            public string OutputFolder { get; set; }

            public Loader(string in_folder, string out_folder) {
                InputFolder = in_folder;
                OutputFolder = out_folder;
            }

            public bool Build() {

                var tmp = Path.Combine(OutputFolder, ".tmp");

                try {
                    var info = Directory.CreateDirectory(tmp);

                    using (var prc = new Process()) {
                        prc.StartInfo.FileName = VGPCompilerPath;
                        prc.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                        prc.StartInfo.Arguments = string.Format("%s %s", InputFolder, tmp);
                        prc.Start();
                        prc.WaitForExit();
                    }

                } catch (Exception ex) {

                    Console.WriteLine(ex.Message);

                }

                return true;
            }

            /*struct Stmt<T> where T : Line {
                StmtType Type { get; }
            }*/

            delegate T GetStatement<T>(byte[] bytes) where T : Line;

            public Script LoadScript(byte[] bytes) {

                var script = new Script();
                byte[] tmp;
                ushort n = 0;
                Line current_statement = null;
                VGPBlock current_block = null;

                tmp = bytes.Take(5).ToArray();
                StmtType t = (StmtType)tmp[0];

                // If the first statement is not a label, exit
                if (t != StmtType.Label) return null;

                int i = 5;
                var sttype = StmtType.UnDef;
                while (i < bytes.Length) {
                    sttype = (StmtType)bytes[i];

                    if (sttype != StmtType.Label) {
                        switch (sttype) {

                            case StmtType.Else:
                                n = 4;
                                break;

                            /*case 1:
                                current_statement = new VGPDialogueLine();
                                break;*/

                            default:
                                throw new Exception();

                        }

                        current_block.Contents.Add(current_statement);

                    } else {

                        script.Blocks.Add(current_block.Label, current_block);

                        current_block = new VGPBlock();

                    }


                }

                return null;
            }

        }

    }
}
