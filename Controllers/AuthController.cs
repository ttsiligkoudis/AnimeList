using AnimeList.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AnimeList.Controllers
{
    public class AuthController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ITokenService _tokenService;

        public AuthController(IHttpClientFactory clientFactory, ITokenService tokenService)
        {
            _clientFactory = clientFactory;
            _tokenService = tokenService;
        }

        [HttpGet]
        public async Task<IActionResult> Login(AnimeService animeService = AnimeService.Kitsu, string username = null, string password = null)
        {
            if (animeService == AnimeService.Anilist)
            {
                return Redirect($"https://anilist.co/api/v2/oauth/authorize?client_id=20850&response_type=code");
            }
            else
            {
                var tokenData = await _tokenService.GetAccessTokenByCredsAsync(username, password, true);
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Callback(string code)
        {
            HttpContext.Session.Remove("AccessToken");

            var tokenData = await _tokenService.GetAccessTokenByCodeAsync(code);

            return RedirectToAction("Index", "Home");
        }
    }
}