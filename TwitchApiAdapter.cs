using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

using TwitchLib.Api.Core;
using TwitchLib.Api.Core.Common;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.Core.Interfaces;
using TwitchLib.Api.Core.RateLimiter;
using TwitchLib.Api.Core.HttpCallHandlers;

using TwitchLib.Api.Auth;
using TwitchLib.Api.Helix;
using TwitchLib.Api.Helix.Models.Predictions.CreatePrediction;

using Hearthstone_Deck_Tracker.Utility.Logging;
using Nito.AsyncEx;

namespace TwitchPredictionsBG
{

    public class AuthExtended : Auth
    {
        public AuthExtended(IApiSettings settings, IRateLimiter rateLimiter, IHttpCallHandler http) : base(settings, rateLimiter, http)
        {
        }

        new public Task<AuthCodeResponse> GetAccessTokenFromCodeAsync(string code, string clientSecret, string redirectUri, string clientId = null)
        {
            var internalClientId = clientId;

            if (string.IsNullOrWhiteSpace(code))
                throw new BadParameterException("The code is not valid. It is not allowed to be null, empty or filled with whitespaces.");

            if (string.IsNullOrWhiteSpace(clientSecret))
                throw new BadParameterException("The client secret is not valid. It is not allowed to be null, empty or filled with whitespaces.");

            if (string.IsNullOrWhiteSpace(redirectUri))
                throw new BadParameterException("The redirectUri is not valid. It is not allowed to be null, empty or filled with whitespaces.");

            if (string.IsNullOrWhiteSpace(internalClientId))
                throw new BadParameterException("The clientId is not valid. It is not allowed to be null, empty or filled with whitespaces.");

            var getParams = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("client_id", internalClientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("redirect_uri", redirectUri)
            };

            return TwitchPostGenericAsync<AuthCodeResponse>("/oauth2/token", ApiVersion.V5, null, getParams, customBase: "https://id.twitch.tv");
        }
    }

        public class TwitchApiAdapter
    {
        public const string CLIENT_ID = "00iimxu2jkbzhiei8bdsvz8kn1behn";
        public const string CLIENT_SECRET = "en1k63d1mott4votunsinas5t729y9";

        private IApiSettings settings;
        private IRateLimiter rateLimiter;
        private IHttpCallHandler http;

        public Config config;

        private TokenStorage tokenStorage;

        private TwitchLibPredicitonsExtender predictions;
        private Users users;

        private Streams streams;

        private AuthExtended auth;

        public TwitchApiAdapter(Config config)
        {
            settings = new ApiSettings();
            rateLimiter = new BypassLimiter();
            http = new TwitchHttpClient();

            this.config = config;

            if (this.config.authToken != null)
            {
                settings.AccessToken = this.config.authToken.AccessToken;
            }

            settings.ClientId = CLIENT_ID;
            settings.Secret = CLIENT_SECRET;
            settings.SkipAutoServerTokenGeneration = true;

            tokenStorage = new TokenStorage(config);

            predictions = new TwitchLibPredicitonsExtender(settings, rateLimiter, http);
            users = new Users(settings, rateLimiter, http);
            streams = new Streams(settings, rateLimiter, http);

            auth = new AuthExtended(settings, rateLimiter, http);
        }

        public string AuthScopeToString(AuthScopes scope)
        {
            switch (scope)
            {
                case AuthScopes.Helix_Channel_Manage_Predictions:
                    return "channel:manage:predictions";
                case AuthScopes.Helix_Channel_Read_Predictions:
                    return "channel:read:predictions";
                default:
                    return Helpers.AuthScopesToString(scope);
            }
        }

        public string GetAuthorizationCodeUrl(string redirectUri, IEnumerable<AuthScopes> scopes, bool forceVerify = false, string state = null, string clientId = null)
        {
            var internalClientId = clientId;

            string scopesStr = null;
            foreach (var scope in scopes)
                if (scopesStr == null)
                    scopesStr = AuthScopeToString(scope);
                else
                    scopesStr += $"+{AuthScopeToString(scope)}";

            if (string.IsNullOrWhiteSpace(internalClientId))
                throw new BadParameterException("The clientId is not valid. It is not allowed to be null, empty or filled with whitespaces.");

            return "https://id.twitch.tv/oauth2/authorize?" +
                   $"client_id={internalClientId}&" +
                   $"redirect_uri={System.Web.HttpUtility.UrlEncode(redirectUri)}&" +
                   "response_type=code&" +
                   $"scope={scopesStr}&" +
                   $"state={state}&" +
                   $"force_verify={forceVerify}";
        }

        public void OpenAuthorizationLink(string redirectUri, IEnumerable<AuthScopes> scopes, string clientId)
        {
            var uri = GetAuthorizationCodeUrl(redirectUri, scopes, clientId: clientId);

            Process.Start(uri);
        }

        public async Task<TwitchApiTokenAdapter> HandleCodeRequest(string uri, string redirectUri, string clientId, string clientSecret)
        {
            var completionSource = new TaskCompletionSource<AuthCodeResponse>();

            Action<string> codeToToken = async code => completionSource.TrySetResult(await auth.GetAccessTokenFromCodeAsync(code, clientSecret, redirectUri, clientId));
            OauthCallbackListener.ServeCallback(uri, codeToToken);

            Log.Info($"Waiting for token");
            var tokenAdapter = new TwitchApiTokenAdapter(await completionSource.Task);

            Log.Info($"Got token " + tokenAdapter.AccessToken);

            tokenStorage.Store(tokenAdapter);

            return tokenAdapter;
        }

        public async Task<TwitchApiTokenAdapter> FetchToken(string clientId, string clientSecret)
        {
            var token = tokenStorage.Retrieve();

            if (token == null)
            {
                Log.Info($"Token not found");
                return null;
            }

            Log.Info($"Token found");

            var validateResponse = await auth.ValidateAccessTokenAsync(token.AccessToken);

            if (validateResponse == null)
            {
                Log.Info($"Token invalid");
                var refresh = await auth.RefreshAuthTokenAsync(token.RefreshToken, clientSecret, clientId);
                token = new TwitchApiTokenAdapter(refresh);
                tokenStorage.Store(token);
                Log.Info($"Token refreshed");
            }

            this.settings.AccessToken = token.AccessToken;

            this.config.user = await GetCurrentUser();
            Log.Info($"User found {this.config.user.DisplayName}");
            this.config.save();

            return token;
        }

        public async Task<TwitchApiTokenAdapter> FetchOrCreateToken(string uri, string redirectUri, IEnumerable<AuthScopes> scopes, string clientId, string clientSecret)
        {
            var token = await FetchToken(clientId, clientSecret);

            if (token != null)
            {
                return token;
            } else
            {
                Log.Info("Open Authorization Link");
                OpenAuthorizationLink(redirectUri, scopes, clientId);

                Log.Info("Handling Code Request");
                token = await HandleCodeRequest(uri, redirectUri, clientId, clientSecret);
            }

            this.settings.AccessToken = token.AccessToken;

            tokenStorage.Store(token);

            this.config.user = await GetCurrentUser();
            Log.Info($"User authenticated {this.config.user.DisplayName}");
            this.config.save();

            return token;
        }
        
        public async Task<TwitchLib.Api.Helix.Models.Users.GetUsers.User> GetCurrentUser()
        {
            var response = await RefreshTokenIfExpired(async () => await this.users.GetUsersAsync());

            return response.Users.First();
        }

        public async Task<Prediction> GetLatestPrediction()
        {
            var response = await RefreshTokenIfExpired(async () => await this.predictions.GetPredictions(this.config.user.Id));

            if (response == null)
            {
                return null;
            }

            return response.Data.FirstOrDefault();
        }

        public async Task<Prediction> CreatePrediction(string title, string[] outcomeTitles, int predictionWindowSeconds)
        {
            var request = new CreatePredictionRequest();

            request.BroadcasterId = this.config.user.Id;
            request.Title = title;
            request.PredictionWindowSeconds = predictionWindowSeconds;

            request.Outcomes = new Outcome[] 
            {
                new Outcome
                {
                    Title = outcomeTitles[0]
                },

                new Outcome
                {
                    Title = outcomeTitles[1]
                },
            };

            Log.Info($"Token {settings.AccessToken} scopes {String.Join(", ", tokenStorage.Retrieve().Scopes.ToString())}");
            var result = await RefreshTokenIfExpired(async () => await this.predictions.CreatePrediction(request));

            return result.Data.First();
        }

        public async Task EndPrediction(string id, PredictionStatusEnum status, string winningOutcomeId)
        {
            Log.Info($"Token {settings.AccessToken} scopes {String.Join(", ", tokenStorage.Retrieve().Scopes.ToString())}");
            await RefreshTokenIfExpired(async () => await this.predictions.EndPrediction(this.config.user.Id, id, status, winningOutcomeId));
        }

        public async Task<TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream> GetStreamInfoForCurrentUser()
        {
            var result = await RefreshTokenIfExpired(async() => await streams.GetStreamsAsync(userIds: new List<string> { config.user.Id.ToString() }));
            return result.Streams.FirstOrDefault();
        }

        private readonly AsyncLock _mutex = new AsyncLock();
        public async Task<T> RefreshTokenIfExpired<T>(Func<Task<T>> wrappedFunction, string clientSecret = null, string clientId = null)
        {
            var token = tokenStorage.Retrieve();
            
            if (token == null)
            {
                return default(T);
            }

            using (await _mutex.LockAsync())
            {
                try
                {
                    return await wrappedFunction();
                }
                catch (Exception ex) when (ex is BadScopeException || ex is TokenExpiredException)
                {
                    var refresh = await auth.RefreshAuthTokenAsync(token.RefreshToken, clientSecret ?? settings.Secret, clientId ?? settings.ClientId);
                    token = new TwitchApiTokenAdapter(refresh);
                    Log.Info($"Token after refresh {token.AccessToken} scopes {String.Join(", ", token.Scopes.ToString())}");
                    tokenStorage.Store(token);
                    settings.AccessToken = token.AccessToken;
                    config.authToken = token;
                    config.save();
                }
            }

            return await wrappedFunction();
        }

        // Way to check for is live with delay?
        // Create timer for delay, postpone prediction requests if stream became live send prediction requests
        public async Task<bool> IsCurrentUserStreamLive()
        {
            if (config.debug)
            {
                return true;
            }
            
            return await GetStreamInfoForCurrentUser() != null;
        }

        public bool IsPredictionInProgress(Prediction prediction)
        {
            if (prediction == null)
            {
                return false;
            }

            if (prediction.Status == PredictionStatus.Active || prediction.Status == PredictionStatus.Locked)
            {
                return true;
            }

            return false;
        }
    }
}
