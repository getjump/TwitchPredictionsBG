using HearthDb.Enums;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.Hearthstone.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchPredictionsBG.PredictionStrategy;

namespace TwitchPredictionsBG
{
    public class TwitchPredictionsBG
    {
        // We can store rating start, rating current, rating high for display on stream
        // Maybe also game stats, like 
        // 8. Bulnik, tvari BloodTrail
        // 1. Siski, murloki Kappa

        private static PropertyFileWriter _ratingWriter = new PropertyFileWriter("rating");
        private static PropertyFileWriter _ratingStartWriter = new PropertyFileWriter("ratingStart");
        private static PropertyFileWriter _ratingHighWriter = new PropertyFileWriter("ratingHigh");

        private static StringPropertyFileWriter _turnWriter = new StringPropertyFileWriter("turn");

        private static Config config;

        private static Version version;

        public static bool isTokenLoaded = false;
        public static bool isActivated = false;

        private static TwitchApiAdapter apiAdapter;
        private static IPredictionOutcomeStrategy predictionStrategy;

        private static int _rating
        {
            get => _ratingWriter.load();
            set
            {
                _ratingWriter.update(value);
            }
        }

        private static int _ratingStart
        {
            get => _ratingStartWriter.load();
            set
            {
                _ratingStartWriter.update(value);
            }
        }

        private static int _ratingHigh
        {
            get => _ratingHighWriter.load();
            set
            {
                _ratingHighWriter.update(value);
            }
        }

        private static int _roundCounter = 0;
        private static int _lastRoundNr = 0;

        private static bool _isStart = true;

        public static void OnLoad(Config _config, TwitchApiAdapter _apiAdapter, Version version)
        {
            config = _config;

            apiAdapter = _apiAdapter;

            switch (config.predictionStrategy)
            {
                case "PredictionOutcomeStrategy13_BLUE_57_PINK_48_RETURN":
                    predictionStrategy = new PredictionOutcomeStrategy13_BLUE_57_PINK_48_RETURN(apiAdapter);
                    break;
                case "ConfigurablePredictionOutcomeStrategy":
                    predictionStrategy = new ConfigurablePredictionOutcomeStrategy(apiAdapter);
                    break;
                case "PredictionOutcomeStrategy13_BLUE_47_PINK_8_RETURN":
                default:
                    predictionStrategy = new PredictionOutcomeStrategy13_BLUE_47_PINK_8_RETURN(apiAdapter);
                    break;
            }

            TwitchPredictionsBG.version = version;
        }

        // On Game Start Event We Should Start Prediction on Twitch via API Call
        // We Should Respect Stream Delay, via Task Delay for example
        // We Can Get Stream Delay From OBS, or Just Put Variable in Settings 
        internal static void GameStart()
        {
            if (!isActivated)
            {
                return;
            }

            if (Core.Game.Spectator)
            {
                return;
            }

            if (!InBgMode())
            {
                return;
            }

            if (!isTokenLoaded)
            {
                return;
            }

            new Thread(async () =>
            {
                await Task.Delay(config.delay * 1000);
                if (await apiAdapter.IsCurrentUserStreamLive())
                {
                    await predictionStrategy.PublishPrediction();
                }
            }).Start();
        }

        internal static void TurnStart(ActivePlayer player)
        {
            if (!isActivated)
            {
                return;
            }

            if (Core.Game.Spectator)
            {
                return;
            }

            if (!InBgMode())
            {
                return;
            }

            var turn = Core.Game.GetTurnNumber();
            _turnWriter.update(turn.ToString());

            if (isTokenLoaded && turn > 1)
            {
                var livingHeroes = Core.Game.Entities.Values
                .Where(x => x.IsHero && x.Health > 0 && !x.IsInZone(Zone.REMOVEDFROMGAME) && x.HasTag(GameTag.PLAYER_TECH_LEVEL) && (x.IsControlledBy(Core.Game.Player.Id) || !x.IsInPlay));

                int positionAtLeast = livingHeroes.Count();

                if (positionAtLeast != 0)
                {
                    new Thread(async () =>
                    {
                        await Task.Delay(config.delay * 1000);
                        if (await apiAdapter.IsCurrentUserStreamLive())
                        {
                            await predictionStrategy.OnGameProgress(positionAtLeast);
                        }
                    }).Start();
                }
            }
        }

        internal static void CreateWriter(string name, ref PropertyFileWriter propertyFileWriter)
        {
            propertyFileWriter = new PropertyFileWriter(name);
        }

        static bool isCustomRatingSet = false;

        internal static void Update()
        {
            if (!isActivated)
            {
                return;
            }

            if (Core.Game.Spectator)
            {
                return;
            }

            var coreGame = Core.Game;

            string gameRegion;

            switch (Core.Game.CurrentRegion)
            {
                case Region.ASIA:
                    gameRegion = "Asia";
                    break;
                case Region.CHINA:
                    gameRegion = "China";
                    break;
                case Region.EU:
                    gameRegion = "EU";
                    break;
                case Region.UNKNOWN:
                    gameRegion = "Unk";
                    break;
                case Region.US:
                    gameRegion = "US";
                    break;
                default:
                    gameRegion = "";
                    break;

            }

            if (gameRegion != "" && gameRegion != "Unk" && config.hsRegion != gameRegion)
            {
                config.hsRegion = gameRegion;
                config.save();
            }

            // If we could get player name from game, we should store rating to it
            // If not, we should store to default path and after we can resolve player name
            // We should restore to it
            var gamePlayerName = Core.Game.Player.Name;
            var playerName = gamePlayerName == "" ? config.hsUserName : gamePlayerName;
            if (!String.IsNullOrEmpty(playerName) && !isCustomRatingSet)
            {
                if (gamePlayerName != "" && gamePlayerName != config.hsUserName)
                {
                    config.hsUserName = gamePlayerName;
                    config.save();
                }

                // Config switch -> Save MMR to new Config

                int _tmpRating = 0;
                int _tmpRatingStart = 0;

                // If we cant get there until is start, it means that current rating and start is set before config switch
                // So we should backup current rating and start before new rating writer
                if (!_isStart)
                {
                    _tmpRating = _rating;
                    _tmpRatingStart = _ratingStart;
                }

                CreateWriter($"rating_{playerName}_{config.hsRegion}", ref _ratingWriter);
                CreateWriter($"ratingStart_{playerName}_{config.hsRegion}", ref _ratingStartWriter);
                CreateWriter($"ratingHigh_{playerName}_{config.hsRegion}", ref _ratingHighWriter);

                if (!_isStart)
                {
                    _rating = _tmpRating;
                    _ratingStart = _tmpRatingStart;

                    // Highest rating if unknown is equal to current
                    if (_ratingHigh == 0)
                    {
                        _ratingHigh = _rating;
                    }
                }

                isCustomRatingSet = true;
            }


            if (!InBgMenu())
            {
                return;
            }


            if (_isStart)
            {
                _ratingStart = Core.Game.BattlegroundsRatingInfo.Rating;
                _rating = _ratingStart;

                if (_ratingHigh == 0)
                {
                    _ratingHigh = _ratingStart;
                    _rating = _ratingStart;
                }

                _isStart = false;
            }

            if (_lastRoundNr > _roundCounter)
            {
                _roundCounter = _lastRoundNr;
                int latestRating = Core.Game.BattlegroundsRatingInfo.Rating;
                int mmrChange = latestRating - _ratingStart;
                _rating = latestRating;

                if (_rating > _ratingHigh)
                {
                    _ratingHigh = _rating;
                }
            }
        }

        internal static bool InBgMenu()
        {
            return Core.Game.CurrentMode == Hearthstone_Deck_Tracker.Enums.Hearthstone.Mode.BACON;
        }

        internal static bool InBgMode()
        {
            if (Core.Game.CurrentGameMode != GameMode.Battlegrounds) return false;
            else return true;
        }

        internal static void InMenu()
        {
        }

        internal static void GameEnd()
        {
            if (!isActivated)
            {
                return;
            }

            if (Core.Game.Spectator)
            {
                return;
            }

            _lastRoundNr++;

            Entity hero = Core.Game.Entities.Values
                .Where(x => x.IsHero && x.GetTag(GameTag.PLAYER_ID) == Core.Game.Player.Id)
                .First();

            int endGamePosition = hero.GetTag(GameTag.PLAYER_LEADERBOARD_PLACE);
            string heroName = hero.Card.LocalizedName;
            string heroId = hero.Card.Id;

            if (!isTokenLoaded)
            {
                return;
            }

            new Thread(async () =>
            {
                await Task.Delay(config.delay * 1000);
                if (await apiAdapter.IsCurrentUserStreamLive())
                {
                    await predictionStrategy.OnGameEnd(endGamePosition);
                }
            }).Start();
        }
    }
}
