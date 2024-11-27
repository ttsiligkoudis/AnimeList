using AnimeList.Models;
using AnimeList.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;

public class HomeController : Controller
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly ITokenService _tokenService;

    public HomeController(IHttpClientFactory clientFactory, ITokenService tokenService)
    {
        _clientFactory = clientFactory;
        _tokenService = tokenService;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public async Task<IActionResult> Index(AnimeService animeService = AnimeService.Kitsu)
    {
        var tokenData = await _tokenService.GetAccessTokenAsync();

        if (!string.IsNullOrEmpty(tokenData?.access_token)) 
        { 
            tokenData.refresh_token = null;
            tokenData.expires_in = null;
            if (tokenData.anime_service == AnimeService.Kitsu)
            {
                tokenData.access_token = null;
            }
            
            ViewBag.TokenData = Uri.EscapeDataString(CompressString(SerializeObject(tokenData, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })));
        }

        ViewBag.AnimeService = tokenData?.anime_service ?? animeService;

        return View();
    }
}
