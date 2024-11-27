using AnimeList.Models;

namespace AnimeList.Services.Interfaces
{
    public interface IAnimeService
    {
        Task<List<Meta>> GetAnimeListAsync(TokenData tokenData, ListType? list = null, string id = null, string skip = null);
        Task<Meta> GetAnimeByIdAsync(string id, TokenData tokenData, HttpContext context, string config);
        Task UpdateEntry(TokenData tokenData, string id, bool exists, string entryId = null);
    }
}

