using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchPredictionsBG.PredictionStrategy
{
    class PredictionOutcomeStrategy13_BLUE_47_PINK_8_RETURN : BasePredictionStrategy
    {
        protected new string DEFAULT_BLUE_TITLE = "1-3";
        protected new string DEFAULT_PINK_TITLE = "4-7";
        protected new string DEFAULT_TITLE = "Predict places";

        public PredictionOutcomeStrategy13_BLUE_47_PINK_8_RETURN(TwitchApiAdapter apiAdapter) : base(apiAdapter)
        {
        }

        public async override Task OnGameProgress(int placementAtLeast)
        {
            var prediction = FetchPrediction();
            if (prediction == null)
            {
                return;
            }

            if (placementAtLeast <= 3)
            {
                var predictionId = prediction.Id;
                UnsetPrediction();
                await this.apiAdapter.EndPrediction(predictionId, TwitchLib.Api.Core.Enums.PredictionStatusEnum.RESOLVED, _outcomeBlueId);
            }
        }

        public async override Task OnGameEnd(int finalPlacement)
        {
            var prediction = FetchPrediction();
            if (prediction == null)
            {
                return;
            }

            string chosenOutcomeId = null;
            TwitchLib.Api.Core.Enums.PredictionStatusEnum predictionStatus = TwitchLib.Api.Core.Enums.PredictionStatusEnum.RESOLVED;

            if (finalPlacement >= 1 && finalPlacement <= 3)
            {
                // Blue Win
                chosenOutcomeId = _outcomeBlueId;
            }
            else if (finalPlacement >= 4 && finalPlacement <= 7)
            {
                // Pink Win
                chosenOutcomeId = _outcomePinkId;
            }
            else if (finalPlacement == 8)
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
