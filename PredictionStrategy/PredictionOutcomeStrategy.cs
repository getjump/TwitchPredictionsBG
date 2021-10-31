using System.Threading.Tasks;

namespace TwitchPredictionsBG.PredictionStrategy
{
    /*
     * Randomize Outcomes Position? (how to detect?)

        Outcomes:
        Top1-3
        Top5-7
        4, 8 Return

        PredictionStrategy:
        Should know end game result (position)
        Should know randomization result (publish prediction)
     */
    interface IPredictionOutcomeStrategy
    {
        /*
         * finalPlacement - from 1 to 8 for BG
         */
        Task OnGameEnd(int finalPlacement);
        Task OnGameProgress(int placementAtLeast);
        Task PublishPrediction();
        bool IsApplicable();
    }

    class BasePredictionStrategy : IPredictionOutcomeStrategy
    {
        protected string TITLE;
        protected string BLUE_OUTCOME_TITLE;
        protected string PINK_OUTCOME_TITLE;

        protected string DEFAULT_BLUE_TITLE = null;
        protected string DEFAULT_PINK_TITLE = null;
        protected string DEFAULT_TITLE = null;


        int WINDOW_SECONDS;
        protected TwitchApiAdapter apiAdapter;

        protected Prediction prediction;

        protected string _outcomeBlueId;
        protected string _outcomePinkId;

        public BasePredictionStrategy(TwitchApiAdapter apiAdapter)
        {
            this.apiAdapter = apiAdapter;

            WINDOW_SECONDS = apiAdapter.config.predictionWindowSeconds;
        }

        protected virtual string PickRandomIfArraySetOrDefault(string[] stringArray, string defaultString)
        {
            System.Random random = new System.Random();

            if (stringArray != null && stringArray.Length > 0)
            {
                return stringArray[random.Next(0, stringArray.Length)];
            }

            return defaultString;
        }

        public virtual bool IsApplicable()
        {
            return true;
        }

        public void RotateStrings()
        {
            BLUE_OUTCOME_TITLE = PickRandomIfArraySetOrDefault(apiAdapter.config.blueOutcomeTitle, DEFAULT_BLUE_TITLE);
            PINK_OUTCOME_TITLE = PickRandomIfArraySetOrDefault(apiAdapter.config.pinkOutcomeTitle, DEFAULT_PINK_TITLE);
            TITLE = PickRandomIfArraySetOrDefault(apiAdapter.config.predictionTitle, DEFAULT_TITLE);
        }

        public virtual async Task PublishPrediction()
        {
            var activePrediction = await this.apiAdapter.GetLatestPrediction();

            if (apiAdapter.IsPredictionInProgress(activePrediction))
            {
                return;
            }

            RotateStrings();

            var random = new System.Random();
            var rn = random.NextDouble();

            bool swapOutcomes = false;
            this.apiAdapter.config.outcomesSwapped = false;

            if (apiAdapter.config.roflanRatio > 0)
            {

                if (rn * 100.0 < apiAdapter.config.roflanRatio)
                {
                    swapOutcomes = true;
                    this.apiAdapter.config.outcomesSwapped = true;
                }
            }

            // If swapped b <-> p -> SetOutcomeIds(p, b) -> x.Title == p => b
            var blueOutcomeTitle = swapOutcomes ? PINK_OUTCOME_TITLE : BLUE_OUTCOME_TITLE;
            var pinkOutcomeTitle = swapOutcomes ? BLUE_OUTCOME_TITLE : PINK_OUTCOME_TITLE;

            this.apiAdapter.config.save();

            prediction = await this.apiAdapter.CreatePrediction(TITLE, new[] { blueOutcomeTitle, pinkOutcomeTitle }, WINDOW_SECONDS);

            this.apiAdapter.config.prediction = prediction;
            this.apiAdapter.config.save();

            SetOutcomeIds(BLUE_OUTCOME_TITLE, PINK_OUTCOME_TITLE);
        }

        protected void SetOutcomeIds(string blueOutcomeTitle, string pinkOutcomeTitle)
        {
            if (prediction == null)
            {
                return;
            }

            foreach (var x in prediction.Outcomes)
            {
                if (x.Title == blueOutcomeTitle)
                {
                    _outcomeBlueId = x.Id;
                }
                else if (x.Title == pinkOutcomeTitle)
                {
                    _outcomePinkId = x.Id;
                }
                else
                {
                    // throw? Something went completely off
                }
            }
        }

        protected Prediction FetchPrediction()
        {
            var blueOutcomeTitle = BLUE_OUTCOME_TITLE;
            var pinkOutcomeTitle = PINK_OUTCOME_TITLE;

            if (this.apiAdapter.config.prediction != null)
            {
                var swapped = false;

                if (blueOutcomeTitle == null)
                {
                    blueOutcomeTitle = apiAdapter.config.prediction.Outcomes[0].Title;
                    swapped = true;
                }

                if (pinkOutcomeTitle == null)
                {
                    pinkOutcomeTitle = apiAdapter.config.prediction.Outcomes[1].Title;
                    swapped = true;
                }

                if (apiAdapter.config.outcomesSwapped && swapped)
                {
                    (blueOutcomeTitle, pinkOutcomeTitle) = (pinkOutcomeTitle, blueOutcomeTitle);
                }
            }

            prediction = apiAdapter.config.prediction;

            SetOutcomeIds(blueOutcomeTitle, pinkOutcomeTitle);
            return prediction;
        }

        protected void UnsetPrediction()
        {
            apiAdapter.config.prediction = null;
            prediction = null;
            apiAdapter.config.save();
        }

        public virtual Task OnGameProgress(int placementAtLeast)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnGameEnd(int finalPlacement)
        {
            return Task.CompletedTask;
        }
    }
}
