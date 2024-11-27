using AnimeList.Models;
using AnimeList.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;

namespace AnimeList.Services
{
    public class TokenService : ITokenService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private HttpClient _client => _clientFactory.CreateClient();

        public TokenService(IHttpClientFactory clientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _clientFactory = clientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<TokenData> GetAccessTokenAsync(string config = null)
        {
            string tokenDataStr;

            if (!string.IsNullOrEmpty(config))
            {
                var configuration = DeserializeObject<Configuration>(config);
                tokenDataStr = DecompressString(Uri.UnescapeDataString(configuration?.tokenData));
            }
            else
            {
                tokenDataStr = _httpContextAccessor.HttpContext.Session.GetString("AccessToken");
            }

            if (string.IsNullOrEmpty(tokenDataStr))
                return null;

            var tokenData = DeserializeObject<TokenData>(tokenDataStr);

            if (tokenData == null) return null;

            if (tokenData.anime_service == AnimeService.Anilist)
            {
                if (string.IsNullOrEmpty(tokenData?.access_token))
                    return null;
            }
            else
            {
                tokenData = await GetAccessTokenByCredsAsync(tokenData.username, tokenData.password, false, tokenData.user_id);
            }

            return tokenData;
        }

        #region Anilist
        public async Task<TokenData> GetAccessTokenByCodeAsync(string code)
        {
            var context = _httpContextAccessor.HttpContext;
            var client = _clientFactory.CreateClient();
            var response = await client.PostAsync("https://anilist.co/api/v2/oauth/token", new FormUrlEncodedContent(new[]
            {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("client_id", "20850"),
            new KeyValuePair<string, string>("client_secret", "bAgns7Q0rGxXnhGRRoq84slYleN4NIe2SkoSDOZ1"),
            new KeyValuePair<string, string>("redirect_uri", "https://tools.myportofolio.eu/Auth/Callback"),
            new KeyValuePair<string, string>("code", code)
            }));

            if (!response.IsSuccessStatusCode) return null;

            var content = await response.Content.ReadAsStringAsync();
            var tokenData = DeserializeObject<TokenData>(content);
            if (!string.IsNullOrEmpty(tokenData?.access_token))
            {
                tokenData.anime_service = AnimeService.Anilist;
                tokenData.expiration_date = DateTime.UtcNow.AddSeconds(tokenData.expires_in ?? 0);
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(tokenData.access_token);
                var claims = jwtToken.Claims;
                tokenData.user_id = claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                context.Session.SetString("AccessToken", SerializeObject(tokenData));
            }

            return tokenData;
        }

        private async Task<TokenData> RefreshAccessToken(string refreshToken)
        {
            var context = _httpContextAccessor.HttpContext;
            var requestData = new FormUrlEncodedContent(new[]
            {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("client_id", "20850"),
            new KeyValuePair<string, string>("client_secret", "bAgns7Q0rGxXnhGRRoq84slYleN4NIe2SkoSDOZ1"),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        });

            var response = await _client.PostAsync("https://anilist.co/api/v2/oauth/token", requestData);
            if (!response.IsSuccessStatusCode) return null;

            var content = await response.Content.ReadAsStringAsync();
            var tokenData = DeserializeObject<TokenData>(content);
            if (!string.IsNullOrEmpty(tokenData?.access_token))
            {
                tokenData.anime_service = AnimeService.Anilist;
                tokenData.expiration_date = DateTime.UtcNow.AddSeconds(tokenData.expires_in ?? 0);
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(tokenData.access_token);
                var claims = jwtToken.Claims;
                tokenData.user_id = claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                context.Session.SetString("AccessToken", SerializeObject(tokenData));
            }

            return tokenData;
        }
        #endregion Anilist

        #region Kitsu
        public async Task<TokenData> GetAccessTokenByCredsAsync(string username, string password, bool setContext = false, string userId = null)
        {
            var context = _httpContextAccessor.HttpContext;
            var requestBody = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "username", username },
                { "password", password }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://kitsu.io/api/oauth/token")
            {
                Content = new FormUrlEncodedContent(requestBody)
            };

            var response = await _client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var tokenData = DeserializeObject<TokenData>(content);
            if (!string.IsNullOrEmpty(tokenData?.access_token))
            {
                tokenData.anime_service = AnimeService.Kitsu;
                tokenData.expiration_date = DateTime.UtcNow.AddSeconds(tokenData.expires_in ?? 0);
                tokenData.username = username;
                tokenData.password = password;
                tokenData.user_id = await GetUserIdAsync(tokenData.access_token);
                if (setContext)
                {
                    context.Session.SetString("AccessToken", SerializeObject(tokenData));
                }
            }

            return tokenData;
        }

        public async Task<string> GetUserIdAsync(string accessToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://kitsu.io/api/edge/users?filter[self]=true");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _client.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var content = await response.Content.ReadAsStringAsync();
            var user = DeserializeObject<dynamic>(content);

            return user.data[0].id;
        }
        #endregion Kitsu
    }
}

