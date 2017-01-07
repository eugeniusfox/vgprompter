using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;

namespace VGPrompter {
    public partial class Script {

        class Loader {

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

        }

    }
}
