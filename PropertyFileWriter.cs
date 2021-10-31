using System;
using System.IO;

namespace TwitchPredictionsBG
{
    class PropertyFileWriter
    {
        //protected IDictionary<Type, Func<string, T>> TypeConversionTable = new Dictionary<Type, Func<string, T>>
        //{
        //    {  }
        //};

        protected int value;

        protected string _configLocation = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\TwitchPredictionsBG\data\";

        public PropertyFileWriter(string fileName)
        {
            _configLocation += fileName + ".config";

            this.value = load();
        }

        private void _update(int value)
        {
            File.WriteAllText(_configLocation, value.ToString());
        }

        public void update(int value)
        {
            if (!this.value.Equals(value))
            {
                _update(value);
            }

            this.value = value;
        }

        public int load()
        {
            int value = 0;
            if (File.Exists(_configLocation))
            {
                // load config from file, if available
                var config = File.ReadAllText(_configLocation);

                value = Int32.Parse(config);
            } else
            {
                _update(value);
            }

            return value;
        }
    }
}
