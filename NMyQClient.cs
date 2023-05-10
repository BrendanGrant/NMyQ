using NMyQ.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using RestSharp;
using System.Collections;

namespace NMyQ
{
    public class NMyQClient
    {
        ILogger logger;
        MyQConfiguration config;
        RestClient client;
        private Action<MyQConfiguration> savingAction;




        public NMyQClient(ILogger logger) => this.logger = logger;

        public async Task Login(string username, string password, string account = "")
        {
            client = new RestClient();
            if (await RefreshToken())
            {
                return;
            }

            var codeVerifier = GenerateCodeVerifier();

            var oAuthResponse = await OauthGetAuthPage(codeVerifier);

            var oauthLoginResponse = await OauthLogin(oAuthResponse, username, password);

            var redirectResponse = await OauthRedirect(oauthLoginResponse);

            await GetToken(redirectResponse, codeVerifier);

            await LoadAccounts();
        }

        #region Login Helpers
        public static HttpClient CreateFreshClient(bool allowAutoRedirect = true, string cookie = "")
        {
            var client = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = allowAutoRedirect });

            client.DefaultRequestHeaders.Add("User-Agent", RandomString(5));

            if (cookie != null)
            {
                client.DefaultRequestHeaders.Add("Cookie", cookie);
            }

            return client;
        }
        private static string RandomString(int length)
        {
            var sb = new StringBuilder(length);
            var random = new Random();
            for (int i = 0; i < length; i++)
            {
                sb.Append(Convert.ToChar(random.Next(0, 26) + 65));
            }
            return sb.ToString();
        }


        private static string GenerateCodeVerifier()
        {
            byte[] codeVerifier = new byte[32];
            RandomNumberGenerator.Fill(codeVerifier);
            return GetCleanCode(Convert.ToBase64String(codeVerifier));
        }

        private static string GenerateCodeChallange(string codeVerifier)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            var b64Hash = Convert.ToBase64String(hash);
            return GetCleanCode(b64Hash);
        }

        private static string GetCleanCode(string b64Hash)
        {
            var code = Regex.Replace(b64Hash, "\\+", "-");
            code = Regex.Replace(code, "\\/", "_");
            code = Regex.Replace(code, "=+$", "");
            return code;
        }

        async Task<HttpResponseMessage> OauthRedirect(HttpResponseMessage previousResponse)
        {
            logger.LogInformation("Doing OauthRedirect()...");
            var location = MyQURLs.BaseUri + previousResponse.Headers.Location;
            var cookie = TrimSetCookie(previousResponse.Headers.GetValues("set-cookie"));

            var manualClient = CreateFreshClient(false, cookie);
            var response = await manualClient.GetAsync(location);
            return response;
        }

        async Task<HttpResponseMessage> OauthGetAuthPage(string codeVerifier)
        {
            logger.LogInformation("Getting OauthGetAuthPage()...");
            var authDict = new Dictionary<string, string>() {
                    { "client_id", Secrets.ClientId },
                    { "code_challenge", GenerateCodeChallange(codeVerifier)},
                    { "code_challenge_method", "S256"},
                    { "redirect_uri", MyQURLs.RedirectUri},
                    { "response_type", "code"},
                    { "scope", HttpUtility.HtmlEncode("MyQ_Residential offline_access")},
                };

            var sb = new StringBuilder();
            foreach (var login in authDict)
            {
                if (sb.Length > 0)
                {
                    sb.Append("&");
                }
                sb.Append($"{login.Key}={Uri.EscapeUriString(login.Value)}");
            }

            var authUrlWithParams = MyQURLs.AuthorizeUri + "?" + sb.ToString();
            var response = await CreateFreshClient().GetAsync(authUrlWithParams);
            return response;
        }

        private static string TrimSetCookie(IEnumerable<string> setCookies)
        {
            var cookies = setCookies.Select(
                c => c.Split(";")[0]
                )
            .ToArray();
            Console.WriteLine($"Cookies: {cookies.Length}");

            var names = cookies.Select(c => c.Split("=")[0]).Distinct();
            if (names.Count() != cookies.Length)
            {
                Console.WriteLine("Problem!");
            }

            return string.Join("; ", cookies);
        }

        private async Task<HttpResponseMessage> OauthLogin(HttpResponseMessage previousResponse, string username, string password)
        {
            logger.LogInformation("Doing OauthLogin()...");
            var cookie = TrimSetCookie(previousResponse.Headers.GetValues("set-cookie"));
            var responseBody = await previousResponse.Content.ReadAsStringAsync();
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(responseBody);
            var formNode = doc.DocumentNode.SelectSingleNode("//form");

            var action = MyQURLs.BaseUri + formNode.Attributes["action"].Value;
            var returnUrl = formNode.ChildNodes.FirstOrDefault(n => n.Name == "input" && n.Id == "ReturnUrl")?.Attributes["value"].Value;
            var token = formNode.ChildNodes.FirstOrDefault(n => n.Name == "input" && n.GetAttributeValue("name", string.Empty) == "__RequestVerificationToken")?.Attributes["value"].Value;

            logger.LogInformation($"action:{action}");
            logger.LogInformation($"returnUrl:{returnUrl}");
            logger.LogInformation($"token:{token}");

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Email", username),
                new KeyValuePair<string, string>("Password", password),
                new KeyValuePair<string, string>("__RequestVerificationToken", token),
            });
            
            var manualClient = CreateFreshClient(false, cookie);
            var response = await manualClient.PostAsync(action, content);

            if (response.Headers.GetValues("set-cookie").Count() < 2)
            {
                logger.LogError("Unexpected number of cookies");
                throw new Exception("Error, check logs.");
            }
            return response;
        }

        public static Dictionary<string, string> GetParamMap(Uri uri)
        {
            if (uri.IsAbsoluteUri == false)
            {
                uri = new Uri(new Uri("http://example.com"), uri);
            }
            return uri.Query.Split("&amp;")[0].TrimStart('?').Split("&").Select(part => part.Split("=")).ToDictionary(p => p[0], p => p[1]);
        }


        async Task GetToken(HttpResponseMessage response, string codeVerifier)
        {
            logger.LogInformation("Doing GetToken()...");
            if (response.StatusCode == HttpStatusCode.Found)
            {
                var iosRedirect = response.Headers.Location;

                var map = GetParamMap(iosRedirect);
                var content2 = new Dictionary<string, string>()
                {
                    { "client_id", Secrets.ClientId },
                    { "client_secret", Encoding.UTF8.GetString(Convert.FromBase64String(Secrets.ClientSecret)) },
                    { "code", map["code"] },
                    { "code_verifier", codeVerifier },
                    { "grant_type", "authorization_code" },
                    { "redirect_uri", MyQURLs.RedirectUri },
                    { "scope", map["scope"] },
                };

                var newToken = await PostAsync<Token>(MyQURLs.TokenUri, content2);
                CreateConfigAndSave(newToken);
                client = new RestClient();
                client.AddDefaultHeader("Authorization", GetBearerTokenString());
            }
            else
            {
                logger.LogError($"GetToken encountered something other than previousResponse.StatusCode == HttpStatusCode.Found: {response.StatusCode}");
                throw new Exception("Error, check logs.");
            }
        }

        private string GetBearerTokenString()
        {
            return $"{this.config.TokenType} {this.config.AccessToken}";
        }
        #endregion

        #region Core API
        public async Task<bool> RefreshToken()
        {
            logger.LogInformation("RefreshToken()...");
            if (config == null)
            {
                logger.LogWarning("No config specified, refresh cannot proceed.");
                return false;
            }
            else if(string.IsNullOrEmpty(config.AccountId))
            {
                logger.LogWarning("Cannot refresh if an account is not specified.");
                return false;
            }
            else if(string.IsNullOrEmpty(config.RefreshToken))
            {
                logger.LogWarning("No refresh token found, cannot refresh.");
                return false;
            }

            var content = new Dictionary<string, string>()
                {
                    { "client_id", Secrets.ClientId },
                    { "client_secret", Encoding.UTF8.GetString(Convert.FromBase64String(Secrets.ClientSecret)) },
                    { "grant_type", "refresh_token" },
                    { "redirect_uri", MyQURLs.RedirectUri },
                    { "refresh_token", config.RefreshToken },
                    { "scope", HttpUtility.HtmlEncode("MyQ_Residential offline_access") },
                };

            var newToken = await PostAsync<Token>(MyQURLs.TokenUri, content);
            CreateConfigAndSave(newToken);
            client = new RestClient();
            client.AddDefaultHeader("Authorization", GetBearerTokenString());

            return true;
        }

        private void CreateConfigAndSave(Token token)
        {
            config = MyQConfiguration.FromToken(token, config?.AccountId);
            if( savingAction != null)
            {
                savingAction(config);
            }
        }

        private async Task<bool> LoadAccounts()
        {
            logger.LogInformation("Doing LoadAccounts()...");
            await CheckTokenState();
            var accountInfo = await GetAsync<AccountInfoResponse>(MyQURLs.AccountsUri);

            logger.LogInformation($"Got {accountInfo.accounts.Count} accounts");
            if (accountInfo.accounts.Count < 1)
            {
                logger.LogWarning("Multiple accounts found. You may need to specify which account you want to call.");
                foreach (var item in accountInfo.accounts)
                {
                    logger.LogWarning($"{item.name} - {item.id}");
                }
                logger.LogWarning($"Assuming first");
            }

            if (this.config.AccountId == null)
            {
                this.config.AccountId = accountInfo.accounts[0].id;
            }
            return true;
        }

        public Task<MyQDevices> GetDevicesAsync()
        {
            return GetDevicesAsync(this.config.AccountId);
        }

        public async Task<MyQDevices> GetDevicesAsync(string account)
        {
            logger.LogInformation("Doing GetDevicesAsync()...");
            await CheckTokenState();

            var devices = await GetAsync<MyQDevices>(MyQURLs.GetDevicesUri(account));

            var devicesString = string.Join(Environment.NewLine, devices.items.Select(d => $"{d.device_family} - {d.name} - {d.serial_number} - {d.state.door_state}").ToArray());
            logger.LogInformation($"Found the following devices: {Environment.NewLine}{devicesString}");

            return devices;
        }

        public async Task<string> SendDoorCommand(string doorSerial, string command)
        {
            logger.LogInformation($"Sending command '{command}' to {doorSerial}");
            await CheckTokenState();

            return await PutAsync<string>(MyQURLs.GarageDoorCommandUri(this.config.AccountId, doorSerial, command));
        }

        public async Task<string> GetDoorState(string doorSerial)
        {
            logger.LogInformation($"GetDoorState('{doorSerial}')");
            await CheckTokenState();

            var devices = await GetDevicesAsync();

            var device = devices.items.FirstOrDefault(d => d.serial_number == doorSerial);

            return device.state.door_state;
        }

        private async Task CheckTokenState()
        {
            if( config.TokenExpirationTime < DateTime.Now)
            {
                logger.LogInformation("Token expired, refreshing.");
                await RefreshToken();
            }
        }

        #endregion

        #region Call wrappers/helpers
        public async Task<T> ExecuteAsync<T>(RestRequest req)
        {
            logger.LogInformation($"Requesting {req.Method} at {req.Resource}");
            var response = await client.ExecuteAsync(req);
            if (response.IsSuccessful == false)
            {
                logger.LogError($"Failed in get: {response.ErrorMessage}");
            }
            else
            {
                //If much more logging is required
                logger.LogInformation($"Got {response.StatusCode}: {response.Content}");
            }

            if(string.IsNullOrEmpty(response.Content))
            {
                return default(T);
            }

            return JsonSerializer.Deserialize<T>(response.Content);
        }

        private async Task<T> GetAsync<T>(string url)
        {
            return await ExecuteAsync<T>(new RestRequest(url));
        }

        private async Task<T> PutAsync<T>(string url)
        {
            return await ExecuteAsync<T>(new RestRequest(url, Method.Put));
        }

        private async Task<T> PostAsync<T>(string url, Dictionary<string, string> content)
        {
            var req = new RestRequest(url);
            req.Method = Method.Post;
            if (content != null)
            {
                foreach (var item in content)
                {
                    req.AddParameter(item.Key, item.Value, ParameterType.GetOrPost);
                }
            }

            return await ExecuteAsync<T>(req);
        }
        #endregion

        #region Config loading/unloading
        public void RegisterConfigurationSaver(Action<MyQConfiguration> savingAction)
        {
            this.savingAction = savingAction;
        }

        public void LoadConfig(MyQConfiguration myQConfiguration)
        {
            this.config = myQConfiguration;
            client = new RestClient();
            client.AddDefaultHeader("Authorization", GetBearerTokenString());
        }
        #endregion
    }
}
