using System;

namespace VGPrompter {

    [Serializable]
    public class Logger {

        const string
            DEFAULT_NAME = "Logger",
            NULL = "Null";

        public bool Enabled { get; set; }
        public string Name { get; private set; }
        Action<object> _logger;

        public Logger(bool enabled = true) : this(DEFAULT_NAME, enabled: enabled) { }

        public Logger(string name, Action<object> f = null, bool enabled = true) {
            Enabled = enabled;
            Name = name;
            _logger = f ?? Console.WriteLine;
        }

        public void Log(object s, bool logger_prefix = true) {
            if (Enabled)
                _logger(logger_prefix ? string.Format("[{0}] {1}", Name, s ?? NULL) : s);
        }

    }

}