using System;

namespace VGPrompter {

    public class Logger {

        public bool Enabled { get; set; }
        public string Name { get; private set; }
        Action<object> _logger;

        public Logger(bool enabled = true) : this("Default", enabled: enabled) { }

        public Logger(string name, Action<object> f = null, bool enabled = true) {
            Enabled = enabled;
            Name = name;
            _logger = f ?? Console.WriteLine;
        }

        public void Log(object s) {
            if (Enabled) _logger(s);
        }

    }

}