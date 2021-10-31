using AutoUpdaterDotNET;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Plugins;
using Hearthstone_Deck_Tracker.Utility.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;
using System.Windows.Controls;
using TwitchLib.Api.Core.Enums;

namespace TwitchPredictionsBG
{
    public class Plugin : IPlugin
    {
        private Config _config;
        private TwitchApiAdapter apiAdapter;

        public void OnLoad()
        {
            AutoUpdate();
            TwitchPredictionsBG.isActivated = true;

            // Triggered upon startup and when the user ticks the plugin on
            CreateFileEnviroment();

            GameEvents.OnGameStart.Add(TwitchPredictionsBG.GameStart);
            GameEvents.OnInMenu.Add(TwitchPredictionsBG.InMenu);
            GameEvents.OnTurnStart.Add(TwitchPredictionsBG.TurnStart);
            GameEvents.OnGameEnd.Add(TwitchPredictionsBG.GameEnd);

            // GameEvents.OnInMenu.Add(TwitchPredictionsBG.Update);

            apiAdapter = new TwitchApiAdapter(_config);

            var t = new Thread(LoadTokenOnLoad);

            t.Name = "Twitch Authorization Thread";
            t.Priority = ThreadPriority.Normal;

            t.Start();

            TwitchPredictionsBG.OnLoad(_config, apiAdapter, Version);
        }

        public void OnUnload()
        {
            TwitchPredictionsBG.isActivated = false;
            // Triggered when the user unticks the plugin, however, HDT does not completely unload the plugin.
            // see https://git.io/vxEcH
        }

        private void LoadTokenOnLoad()
        {
            var token = apiAdapter.FetchToken(TwitchApiAdapter.CLIENT_ID, TwitchApiAdapter.CLIENT_SECRET).Result;

            if (token == null)
            {
                return;
            }

            TwitchPredictionsBG.isTokenLoaded = true;

            string live = apiAdapter.IsCurrentUserStreamLive().Result ? "live" : "not live";

            Log.Info($"User is {live}");
        }

        private void HandleButtonPress()
        {
            // Triggered when the user clicks your button in the plugin list
            var token = apiAdapter.FetchOrCreateToken("http://localhost:8110/", "http://localhost:8110/", new[] { AuthScopes.Helix_Channel_Manage_Predictions, AuthScopes.Helix_Channel_Read_Predictions }, TwitchApiAdapter.CLIENT_ID, TwitchApiAdapter.CLIENT_SECRET).Result;

            TwitchPredictionsBG.isTokenLoaded = true;
        }

        public void OnButtonPress()
        {
            var t = new Thread(HandleButtonPress);

            t.Name = "Twitch Authorization Thread";
            t.Priority = ThreadPriority.Normal;

            t.Start();
        }

        public void OnUpdate()
        {
            TwitchPredictionsBG.Update();
        }

        private void CreateFileEnviroment()
        {
            var pluginDateFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\TwitchPredictionsBG\data\";
            if (!Directory.Exists(pluginDateFolderPath))
            {
                Directory.CreateDirectory(pluginDateFolderPath);
            }
            if (File.Exists(Config._configLocation))
            {
                // load config from file, if available
                _config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(Config._configLocation));
            }
            else
            { // create config file
                _config = new Config();
                _config.save();
            }
        }

        private void AutoUpdate()
        {
            AutoUpdater.InstalledVersion = Version;
            AutoUpdater.AppTitle = "Twitch Predictions BG";
            AutoUpdater.Start("https://drive.google.com/uc?export=download&id=1-37tfKg0fhFmMin944Pwvo0tTPLP5Lf-");
            AutoUpdater.DownloadPath = Hearthstone_Deck_Tracker.Config.AppDataPath + @"\Plugins\";
            var currentDirectory = new DirectoryInfo(Hearthstone_Deck_Tracker.Config.AppDataPath + @"\Plugins\TwitchPredictionsBG\");
            if (currentDirectory.Parent != null)
            {
                AutoUpdater.InstallationPath = currentDirectory.Parent.FullName;
            }

        }

        public string Name => "Battlegrounds Twitch Predictions";

        public string Description => "Integrates Twitch Predictions with Battlegrounds";

        public string ButtonText => "Authorize Twitch";

        public string Author => "getjump";

        public Version Version => new Version(0, 0, 1, 1);

        public MenuItem MenuItem => null;
    }
}
