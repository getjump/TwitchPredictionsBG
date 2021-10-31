using TwitchLib.Api.Auth;

namespace TwitchPredictionsBG
{
    public class TwitchApiTokenAdapter
    {
        protected string _accessToken;
        protected string _refreshToken;
        protected int _expiresIn;
        protected string[] _scopes;

        public TwitchApiTokenAdapter()
        {
        }

        public TwitchApiTokenAdapter(AuthCodeResponse response)
        {
            _accessToken = response.AccessToken;
            _refreshToken = response.RefreshToken;
            _expiresIn = response.ExpiresIn;
            _scopes = response.Scopes;
        }

        public TwitchApiTokenAdapter(RefreshResponse response)
        {
            _accessToken = response.AccessToken;
            _refreshToken = response.RefreshToken;
            _expiresIn = response.ExpiresIn;
            _scopes = response.Scopes;
        }

        public string AccessToken { get => _accessToken; set => _accessToken = value; }
        public string RefreshToken { get => _refreshToken; set => _refreshToken = value; }
        public int ExpiresIn { get => _expiresIn; set => _expiresIn = value; }
        public string[] Scopes { get => _scopes; set => _scopes = value; }
    }

    class TokenStorage
    {
        protected Config _config;

        public TokenStorage(Config config)
        {
            _config = config;
        }

        public TwitchApiTokenAdapter Retrieve()
        {
            return _config.authToken;
        }

        public bool Store(TwitchApiTokenAdapter token)
        {
            _config.authToken = token;
            _config.save();

            return true;
        }
    }
}
