using AnimeList.Models;
using AnimeList.Services.Interfaces;
using System.Net.Http.Headers;
using System.Text;

namespace AnimeList.Services
{
    public class KitsuService : IKitsuService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _kitsuApi = "https://kitsu.io/api/edge";
        private HttpClient _client => _clientFactory.CreateClient();

        public KitsuService(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<List<Meta>> GetAnimeListAsync(TokenData tokenData, ListType? list = null, string id = null, string skip = null)
        {
            var tmpStr = string.IsNullOrEmpty(id)
                ? (list.HasValue ? $"&filter[status]={GetListTypeString(list.Value, tokenData)}" : "")
                : $"&filter[animeId]={id.Replace(kitsuPrefix, "")}";

            var str = list == ListType.Trending_Desc
                ? $"{_kitsuApi}/trending/anime?filter[status]={GetListTypeString(ListType.Current, tokenData)}"
                : $"{_kitsuApi}/users/{tokenData.user_id}/library-entries?filter[kind]=anime&include=anime{tmpStr}";

            str = $"{str}&page[limit]=20";

            if (!string.IsNullOrEmpty(skip))
            {
                str = $"{str}&page[offset]={skip}";
            }

            var request = new HttpRequestMessage(HttpMethod.Get, str);

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenData.access_token);

            var response = await _client.SendAsync(request);
            if (!response.IsSuccessStatusCode) return [];

            var content = await response.Content.ReadAsStringAsync();
            var entries = DeserializeObject<dynamic>(content);

            var animeList = new List<Meta>();
            foreach (var entry in entries.data)
            {
                dynamic included = null;

                if (list == ListType.Trending_Desc)
                {
                    included = entry;
                }
                else
                {
                    foreach (var inc in entries.included)
                    {
                        if (inc.id == entry.relationships.anime.data.id)
                        {
                            included = inc;
                            break;
                        }
                    }
                }

                animeList.Add(new Meta
                {
                    id = $"{kitsuPrefix}{included.id}",
                    name = included.attributes.titles.en,
                    poster = included.attributes.posterImage.large,
                    descriptionRich = included.attributes.description,
                    entryId = list == ListType.Trending_Desc ? null : entry.id,
                });
            }

            return animeList;
        }

        public async Task<Meta> GetAnimeByIdAsync(string id, TokenData tokenData, HttpContext context, string config)
        {
            if (string.IsNullOrEmpty(id) || !id.StartsWith(kitsuPrefix)) return null;

            var request = new HttpRequestMessage(HttpMethod.Get, $"{_kitsuApi}/anime/{id.Replace(kitsuPrefix, "")}?include=genres,episodes");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenData.access_token);

            var response = await _client.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var content = await response.Content.ReadAsStringAsync();
            var results = DeserializeObject<dynamic>(content);

            var entry = results.data;

            var anime = new Meta
            {
                id = id,
                name = entry.attributes.titles.en,
                poster = entry.attributes.posterImage.large,
                descriptionRich = entry.attributes.description,
                //genres = entry.relationships.genres.data.ToObject<List<string>>(),
                background = entry.attributes.coverImage.large
            };

            if (!string.IsNullOrEmpty((string)entry.attributes.youtubeVideoId))
            {
                anime.trailers.Add(new Trailer(entry.attributes.youtubeVideoId));
                anime.trailerStreams.Add(new TrailerStream(entry.attributes.youtubeVideoId, anime.name));
            }

            foreach (var episode in results.included)
            {
                var video = new Video
                {
                    thumbnail = episode.attributes.thumbnail != null ? episode.attributes.thumbnail.large : null,
                    season = episode.attributes.seasonNumber,
                    episode = episode.attributes.number
                };

                video.id = $"{id}:{video.episode}";
                video.title = string.IsNullOrEmpty((string)episode.attributes.canonicalTitle)
                    ? $"Episode {video.episode}"
                    : episode.attributes.canonicalTitle;

                if (string.IsNullOrEmpty(video.title)) continue;

                anime.videos.Add(video);
            }

            var entryId = (await GetAnimeListAsync(tokenData, null, id) ?? []).FirstOrDefault()?.entryId;

            var url = $"{context.Request.Scheme}://{context.Request.Host}/{config}/Meta/{anime.type}/{id}/{!string.IsNullOrEmpty(entryId)}/View";

            if (!string.IsNullOrEmpty(entryId))
            {
                url = $"{url}?entryId={entryId}";
            }

            anime.links.Add(new Link
            {
                name = !string.IsNullOrEmpty(entryId) ? "Remove" : "Add",
                url = url,
                category = LinkCategory.follow.ToString()
            });

            return anime;
        }

        public async Task UpdateEntry(TokenData tokenData, string id, bool exists, string entryId = null)
        {
            if (exists)
            {
                var request = new HttpRequestMessage(HttpMethod.Delete, $"{_kitsuApi}/library-entries/{entryId}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenData.access_token);
                var response = await _client.SendAsync(request);
            }
            else
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_kitsuApi}/library-entries");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenData.access_token);
                var obj = new
                {
                    data = new
                    {
                        type = "libraryEntries",
                        attributes = new
                        {
                            status = GetListTypeString(ListType.Current, tokenData)
                        },
                        relationships = new
                        {
                            user = new
                            {
                                data = new
                                {
                                    type = "users",
                                    id = tokenData.user_id
                                }
                            },
                            anime = new
                            {
                                data = new
                                {
                                    type = "anime",
                                    id = id.Replace(kitsuPrefix, "")
                                }
                            }
                        }
                    }
                };
                request.Content = new StringContent(SerializeObject(obj), Encoding.UTF8, "application/vnd.api+json");
                var response = await _client.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
            }
        }
    }
}

