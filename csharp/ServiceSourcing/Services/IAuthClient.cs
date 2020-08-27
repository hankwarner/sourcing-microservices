using System.Threading;
using System.Threading.Tasks;

namespace ServiceSourcing.Services
{
    public interface IAuthClient
    {
        Task<AccessTokenData> RefreshAccessTokenAsync(string refreshToken, CancellationToken token);
    }
    
    public class AccessTokenData
    {
        public string access_token { get; set; }
        public string expires_in { get; set; }
        public string scope { get; set; }
        public string token_type { get; set; }
        public string id_token { get; set; }
    }

}