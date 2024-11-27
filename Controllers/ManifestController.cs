using AnimeList.Models;
using Microsoft.AspNetCore.Mvc;

namespace AnimeList.Controllers
{
    [ApiController]
    public class ManifestController : ControllerBase
    {
        [HttpGet("{config}/manifest.json")]
        public JsonResult Get(string config)
        {
            var configiration = DeserializeObject<Configuration>(config);
            TokenData? tokenData = null;

            var manifest = new Manifest
            {
                id = "community.MyAnime",
                version = "1.0.0",
                name = "MyAnime",
                description = "Fetches anime list from Kitsu/AniList to track your anime progress while using stremio",
                resources = [ "catalog", "meta" ],
                types = [ MetaType.movie.ToString(), MetaType.series.ToString() ],
            };

            if (!string.IsNullOrEmpty(configiration.tokenData))
            {
                tokenData = DeserializeObject<TokenData>(DecompressString(Uri.UnescapeDataString(configiration.tokenData)));

                manifest.config.Add(new Config
                {
                    key = "token",
                    type = "text",
                    title = "token"
                });

                if (tokenData.anime_service == AnimeService.Kitsu)
                    manifest.idPrefixes.Add(kitsuPrefix);
                else
                    manifest.idPrefixes.Add(anilistPrefix);
            }

            if (configiration.showCurrent)
            {
                manifest.catalogs.Add(new Catalog
                {
                    type = MetaType.anime.ToString(),
                    id = GetListTypeString(ListType.Current, tokenData),
                    name = "Currently watching",
                    extra = [new Extra("skip")]
                });
            }

            if (configiration.showCompleted)
            {
                manifest.catalogs.Add(new Catalog
                {
                    type = MetaType.anime.ToString(),
                    id = GetListTypeString(ListType.Completed, tokenData),
                    name = "Completed",
                    extra = [ new Extra("skip")]
                });
            }

            if (configiration.showTrending)
            {
                manifest.catalogs.Add(new Catalog
                {
                    type = MetaType.anime.ToString(),
                    id = GetListTypeString(ListType.Trending_Desc, tokenData),
                    name = "Trending Now",
                    extra = [new Extra("skip")]
                });
            }

            return new JsonResult(manifest);
        }
    }
}


