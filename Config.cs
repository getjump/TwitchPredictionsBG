using System;
using System.IO;

using Newtonsoft.Json;

using TwitchLib.Api.Helix.Models.Users.GetUsers;


namespace TwitchPredictionsBG
{
    public class Config
    {
        public static readonly string _configLocation = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\TwitchPredictionsBG\data\TwitchPredictionsBG.config";

        public TwitchApiTokenAdapter authToken;
        public User user;
        public int delay = 0;

        public bool debug = false;

        public string hsUserName;
        public string hsRegion;

        public string[] blueOutcomeTitle;
        public string[] pinkOutcomeTitle;
        public string[] predictionTitle;
        
        public string predictionStrategy = "PredictionOutcomeStrategy13_BLUE_47_PINK_8_RETURN";

        public int[] blueOutcomePlacements = null;
        public int[] pinkOutcomePlacements = null;
        public int[] returnOutcomePlacements = null;

        public int roflanRatio = 0;

        public bool outcomesSwapped = false;

        public int predictionWindowSeconds = 120;

        public Prediction prediction;

        public void save()
        {
            File.WriteAllText(_configLocation, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public Config load()
        {
            if (File.Exists(_configLocation))
            {
                // load config from file, if available
                var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(_configLocation));

                return config;
            }
            return null;
        }
    }
}
