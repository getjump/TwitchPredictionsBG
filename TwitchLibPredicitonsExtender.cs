using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TwitchLib.Api.Helix;

using TwitchLib.Api.Core;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.Interfaces;
using TwitchLib.Api.Helix.Models.Predictions.CreatePrediction;
using TwitchLib.Api.Helix.Models.Predictions.EndPrediction;
using TwitchLib.Api.Helix.Models.Predictions.GetPredictions;

using Newtonsoft.Json;
using TwitchLib.Api.Helix.Models.Common;

using System.Runtime.Serialization;

namespace TwitchPredictionsBG
{
    public class Prediction
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; protected set; }
        [JsonProperty(PropertyName = "broadcaster_id")]
        public string BroadcasterId { get; protected set; }
        [JsonProperty(PropertyName = "broadcaster_name")]
        public string BroadcasterName { get; protected set; }
        [JsonProperty(PropertyName = "broadcaster_login")]
        public string BroadcasterLogin { get; protected set; }
        [JsonProperty(PropertyName = "title")]
        public string Title { get; protected set; }
        [JsonProperty(PropertyName = "winning_outcome_id")]
        public string WinningOutcomeId { get; protected set; }
        [JsonProperty(PropertyName = "outcomes")]
        public PredictionOutcome[] Outcomes { get; protected set; }
        [JsonProperty(PropertyName = "prediction_window")]
        public int PredictionWindow { get; protected set; }
        [JsonProperty(PropertyName = "status")]
        public PredictionStatus Status { get; protected set; }
        [JsonProperty(PropertyName = "created_at")]
        public DateTime CreatedAt { get; protected set; }
        [JsonProperty(PropertyName = "ended_at")]
        public DateTime EndeddAt { get; protected set; }
        [JsonProperty(PropertyName = "locked_at")]
        public DateTime LockedAt { get; protected set; }
    }

    public class PredictionOutcome
    {
        [JsonProperty(PropertyName = "id")]
        public string Id;
        [JsonProperty(PropertyName = "title")]
        public string Title;
        [JsonProperty(PropertyName = "users")]
        public int Users;
        [JsonProperty(PropertyName = "channel_points")]
        public int ChannelPoints;
        [JsonProperty(PropertyName = "color")]
        public PredictionOutcomeColor Color;
        [JsonProperty(PropertyName = "top_predictors")]
        public PredictionOutcomeUser[] TopPredictors;
    }

    public class PredictionOutcomeUser
    {
        [JsonProperty(PropertyName = "id")]
        public string Id;
        [JsonProperty(PropertyName = "name")]
        public string Name;
        [JsonProperty(PropertyName = "login")]
        public string Login;
        [JsonProperty(PropertyName = "channel_points_used")]
        public int ChannelPointsUsed;
        [JsonProperty(PropertyName = "channel_points_won")]
        public int ChannelPointsWon;
    }

    [DataContract]
    public enum PredictionOutcomeColor
    {
        [EnumMember(Value = "BLUE")]
        Blue,

        [EnumMember(Value = "PINK")]
        Pink
    }

    [DataContract]
    public enum PredictionStatus
    {
        [EnumMember(Value = "RESOLVED")]
        Resolved,

        [EnumMember(Value = "ACTIVE")]
        Active,

        [EnumMember(Value = "CANCELED")]
        Canceled,

        [EnumMember(Value = "LOCKED")]
        Locked
    }

    public class GetPredictionsResponse
    {
        [JsonProperty(PropertyName = "data")]
        public Prediction[] Data { get; protected set; }
        [JsonProperty(PropertyName = "pagination")]
        public Pagination Pagination { get; protected set; }
    }

    public class CreatePredictionResponse
    {
        [JsonProperty(PropertyName = "data")]
        public Prediction[] Data { get; protected set; }
    }

    public class TwitchLibPredicitonsExtender: Predictions
    {
        public TwitchLibPredicitonsExtender(IApiSettings settings, IRateLimiter rateLimiter, IHttpCallHandler http) : base(settings, rateLimiter, http)
        {
        }

        public new Task<GetPredictionsResponse> GetPredictions(string broadcasterId, List<string> ids = null, string after = null, int first = 20, string accessToken = null)
        {
            var getParams = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("broadcaster_id", broadcasterId),
                new KeyValuePair<string, string>("first", first.ToString())
            };

            if (ids != null && ids.Count > 0)
            {
                foreach (var id in ids)
                {
                    getParams.Add(new KeyValuePair<string, string>("id", id));
                }
            }
            if (after != null)
                getParams.Add(new KeyValuePair<string, string>("after", after));

            return TwitchGetGenericAsync<GetPredictionsResponse>("/predictions", ApiVersion.Helix, getParams, accessToken);
        }

        public new Task<CreatePredictionResponse> CreatePrediction(CreatePredictionRequest request, string accessToken = null)
        {
            return TwitchPostGenericAsync<CreatePredictionResponse>("/predictions", ApiVersion.Helix, JsonConvert.SerializeObject(request), accessToken: accessToken);
        }
    }
}
