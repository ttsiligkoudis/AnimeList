using AnimeList.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AnimeList.Controllers
{
    [ApiController]
    public class CatalogController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly IAnilistService _anilistService;
        private readonly IKitsuService _kitsuService;

        public CatalogController(ITokenService tokenService, IAnilistService anilistService, IKitsuService kitsuService)
        {
            _tokenService = tokenService;
            _anilistService = anilistService;
            _kitsuService = kitsuService;
        }

        [HttpGet("{config}/[controller]/{metaType}/{listType}/skip={skip:int}.json")]
        public async Task<ActionResult> GetListWithSkip(string config, MetaType metaType, ListType listType, string skip)
        {
            return await GetList(config, metaType, listType, skip);
        }

        [HttpGet("{config}/[controller]/{metaType}/{listType}.json")]
        public async Task<ActionResult> GetList(string config, MetaType metaType, ListType listType, string skip = null)
        {
            var tokenData = await _tokenService.GetAccessTokenAsync(config);

            if (IsTokenExpired(tokenData?.expiration_date))
            {
                return new JsonResult(new { metas = ExpiredMetas() });
            }

            var metas = tokenData.anime_service == AnimeService.Anilist
                ? await _anilistService.GetAnimeListAsync(tokenData, listType, null, skip)
                : await _kitsuService.GetAnimeListAsync(tokenData, listType, null, skip);

            return new JsonResult(new { metas });
        }
    }
}

