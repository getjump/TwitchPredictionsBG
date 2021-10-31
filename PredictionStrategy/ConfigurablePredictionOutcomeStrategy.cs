using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchPredictionsBG.PredictionStrategy
{
    class ConfigurablePredictionOutcomeStrategy: BasePredictionStrategy
    {
        protected new string DEFAULT_BLUE_TITLE = null;
        protected new string DEFAULT_PINK_TITLE = null;
        protected new string DEFAULT_TITLE = null;

        private HashSet<int> blueOutcomePlacementsSet;
        private HashSet<int> pinkOutcomePlacementsSet;
        private HashSet<int> returnOutcomePlacementsSet;

        public ConfigurablePredictionOutcomeStrategy(TwitchApiAdapter apiAdapter) : base(apiAdapter)
        {
            if (apiAdapter.config.blueOutcomePlacements != null)
            {
                blueOutcomePlacementsSet = new HashSet<int>(apiAdapter.config.blueOutcomePlacements);
            }
            
            if (apiAdapter.config.pinkOutcomePlacements != null)
            {
                pinkOutcomePlacementsSet = new HashSet<int>(apiAdapter.config.pinkOutcomePlacements);
            }

            if (apiAdapter.config.returnOutcomePlacements != null)
            {
                returnOutcomePlacementsSet = new HashSet<int>(apiAdapter.config.returnOutcomePlacements);
            }
        }

        public override Task OnGameProgress(int placementAtLeast)
        {

            // This strategy should not work for on game progress

            // Can we close if winning (yes), ??
            // Blue [1, 2, 3]
            // Pink [5, 6, 7, 8]
            // Return [4]
            // Placement at least 3
            return Task.CompletedTask;
        }

        public override async Task OnGameEnd(int finalPlacement)
        {
            var prediction = FetchPrediction();
            if (prediction == null)
            {
                return;
            }

            string chosenOutcomeId = null;
            TwitchLib.Api.Core.Enums.PredictionStatusEnum predictionStatus = TwitchLib.Api.Core.Enums.PredictionStatusEnum.RESOLVED;

            if (blueOutcomePlacementsSet.Contains(finalPlacement))
            {
                // Blue Win
                chosenOutcomeId = _outcomeBlueId;
            }
            else if (pinkOutcomePlacementsSet.Contains(finalPlacement))
            {
                // Pink Win
                chosenOutcomeId = _outcomePinkId;
            }
            else if (returnOutcomePlacementsSet.Contains(finalPlacement))
            {
                // Return
                predictionStatus = TwitchLib.Api.Core.Enums.PredictionStatusEnum.CANCELED;
            }

            var predictionId = prediction.Id;
            UnsetPrediction();
            await this.apiAdapter.EndPrediction(predictionId, predictionStatus, chosenOutcomeId);
        }
    }
}
