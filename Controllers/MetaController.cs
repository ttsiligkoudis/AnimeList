using AnimeList.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AnimeList.Controllers
{
    [ApiController]
    public class MetaController : Controller
    {
        private readonly ITokenService _tokenService;
        private readonly IAnilistService _anilistService;
        private readonly IKitsuService _kitsuService;

        public MetaController(ITokenService tokenService, IAnilistService anilistService, IKitsuService kitsuService)
        {
            _tokenService = tokenService;
            _anilistService = anilistService;
            _kitsuService = kitsuService;
        }

        [HttpGet("{config}/[controller]/{metaType}/{id}.json")]
        public async Task<JsonResult> GetByID(string config, MetaType metaType, string id)
        {
            var tokenData = await _tokenService.GetAccessTokenAsync(config);

            if (IsTokenExpired(tokenData?.expiration_date))
            {
                return new JsonResult(new { meta = ExpiredMeta() });
            }

            var anime = tokenData.anime_service == AnimeService.Anilist
                ? await _anilistService.GetAnimeByIdAsync(id, tokenData, HttpContext, config)
                : await _kitsuService.GetAnimeByIdAsync(id, tokenData, HttpContext, config);

            return new JsonResult(new { meta = anime });
        }

        [Route("{config}/[controller]/{metaType}/{id}/{exists}/View")]
        public async Task<ActionResult> GetViewByID(string config, MetaType metaType, string id, bool exists, string entryId = null)
        {
            var tokenData = await _tokenService.GetAccessTokenAsync(config);

            if (!IsTokenExpired(tokenData?.expiration_date))
            {
                if (tokenData.anime_service == AnimeService.Anilist)
                    await _anilistService.UpdateEntry(tokenData, id, exists, entryId);
                else
                    await _kitsuService.UpdateEntry(tokenData, id, exists, entryId);
            }

            ViewBag.AppUrl = $"stremio://detail/{metaType}/{id}";
            ViewBag.Url = $"https://web.stremio.com/#/detail/{metaType}/{id}";

            return View("Details");
        }
    }
}

