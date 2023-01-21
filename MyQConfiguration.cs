using NMyQ.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMyQ
{
    public class MyQConfiguration
    {
        public DateTime TokenExpirationTime { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string TokenType { get; set; }
        public string AccountId { get; set; }

        public static MyQConfiguration FromToken(Token token, string accountId)
        {
            return new MyQConfiguration() { 
                AccountId = accountId,
                AccessToken = token.access_token,
                RefreshToken = token.refresh_token,
                TokenType = token.token_type,
                TokenExpirationTime = DateTime.Now.AddSeconds(token.expires_in)
            };
        }
    }
}
