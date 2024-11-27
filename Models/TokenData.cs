
namespace AnimeList.Models
{
    public class TokenData
    {
        public string access_token { get; set; }

        public string refresh_token { get; set; }

        public long? expires_in { get; set; }

        public DateTime? expiration_date { get; set; }

        public string user_id { get; set; }

        public string username { get; set; }

        public string password { get; set; }

        public AnimeService anime_service { get; set; }
    }
}
