using System;
using System.IO;

namespace TwitchPredictionsBG
{
    class StringPropertyFileWriter
    {
        protected string value;

        protected string _configLocation = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\TwitchPredictionsBG\data\";

        public StringPropertyFileWriter(string fileName)
        {
            _configLocation += fileName + ".config";

            this.value = load();
        }

        private void _update(string value)
        {
            File.WriteAllText(_configLocation, value);
        }

        public void update(string value)
        {
            if (this.value == null || !this.value.Equals(value))
            {
                _update(value);
            }

            this.value = value;
        }

        public string load()
        {
            string value = null;
            if (File.Exists(_configLocation))
            {
                // load config from file, if available
                value = File.ReadAllText(_configLocation);
            }
            else
            {
                _update(value);
            }

            return value;
        }
    }
}
