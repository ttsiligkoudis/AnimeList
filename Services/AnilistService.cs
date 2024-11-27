using AnimeList.Models;
using AnimeList.Services.Interfaces;
using System.Net.Http.Headers;

namespace AnimeList.Services
{
    public class AnilistService : IAnilistService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _anilistApi = "https://graphql.anilist.co";
        private HttpClient _client => _clientFactory.CreateClient();
        private readonly List<ListType> _userLists = [ListType.Current, ListType.Completed ];

        public AnilistService(IHttpClientFactory clientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _clientFactory = clientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<Meta>> GetAnimeListAsync(TokenData tokenData, ListType? list = null, string id = null, string skip = null)
        {
            string requestBody = string.Empty;
            if (!list.HasValue || _userLists.Contains(list.Value))
            {
                var tmpStr = "userId: $userId, type: ANIME";

                if (list.HasValue)
                {
                    tmpStr = $"{tmpStr}, status: {GetListTypeString(list.Value, tokenData)}";
                }

                requestBody = SerializeObject(new
                {
                    query = @"
                    query (tmpStr) {
                        MediaListCollection() {
                            lists {
                                entries {
                                    media {
                                        id
                                        title {
                                            english
                                        }
                                        coverImage {
                                            large
                                        }
                                        description
                                    }
                                }
                            }
                        }
                    }",
                    variables = new { userId = tokenData?.user_id }
                });
            }
            else
            {
                requestBody = SerializeObject(new
                { 
                    query = @"
                    query ($sort: [MediaSort]) {
                        Page {
                            media(sort: $sort, type: ANIME) {
                                id
                                title {
                                    english
                                }
                                coverImage {
                                    large
                                }
                                description
                            }
                        }
                    }",
                    variables = new { sort = new List<string> { GetListTypeString(list.Value, tokenData) } }
                });
            }

            var request = new HttpRequestMessage(HttpMethod.Post, _anilistApi)
            {
                Content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json"),
            };
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenData?.access_token);

            var response = await _client.SendAsync(request);

            if (!response.IsSuccessStatusCode) return [];

            var content = await response.Content.ReadAsStringAsync();
            dynamic? result;

            if (list == ListType.Trending_Desc)
            {
                result = DeserializeObject<dynamic>(content).data.Page.media;
            }
            else
            {
                result = DeserializeObject<dynamic>(content).data.MediaListCollection.lists[0].entries;
            }

            var animeList = new List<Meta>();
            foreach (var entry in result)
            {
                var tmpEntry = entry;
                if (list != ListType.Trending_Desc)
                {
                    tmpEntry = entry.media;
                }

                animeList.Add(new Meta
                {
                    id = $"{anilistPrefix}{tmpEntry.id}",
                    name = tmpEntry.title.english,
                    poster = tmpEntry.coverImage.large,
                    descriptionRich = tmpEntry.description,
                });
            }

            return animeList;
        }

        public async Task<Meta> GetAnimeByIdAsync(string id, TokenData tokenData, HttpContext context, string config)
        {
            if (string.IsNullOrEmpty(id) || !id.StartsWith(anilistPrefix)) return null;

            id = id.Replace(anilistPrefix, "");

            var query = @"
                query ($id: Int) {
                    Media(id: $id) {
                        id
                        title {
                            english
                        }
                        coverImage {
                            extraLarge
                        }
                        description,
                        genres,
                        trailer {
                            id,
                            site
                        },
                        streamingEpisodes {
                            title,
                            thumbnail
                        }
                    }
                }
            ";

            var variables = new { id };

            var ser = SerializeObject(new { query, variables });

            var request = new HttpRequestMessage(HttpMethod.Post, _anilistApi)
            {
                Content = new StringContent(ser, System.Text.Encoding.UTF8, "application/json")
            };
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenData.access_token);

            var response = await _client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = DeserializeObject<dynamic>(content).data.Media;

            var anime = new Meta
            {
                id = id,
                name = result.title.english,
                poster = result.coverImage.extraLarge,
                descriptionRich = result.description,
                genres = result.genres.ToObject<List<string>>(),
                background = result.coverImage.extraLarge,
                videos = result.streamingEpisodes.ToObject<List<Video>>()
            };

            if (result.trailer != null && result.trailer.site == "youtube")
            {
                anime.trailers.Add(new Trailer(result.trailer.id));
                anime.trailerStreams.Add(new TrailerStream(result.trailer.id, anime.name));
            }
            anime.videos.ForEach(v => v.id = $"{id}-{v.title}");

            return anime;
        }

        public async Task UpdateEntry(TokenData tokenData, string id, bool exists, string entryId = null)
        {

        }
    }
}

