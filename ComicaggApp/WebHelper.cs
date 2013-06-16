using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Security.Authentication.Web;

namespace ComicaggApp
{
    class WebHelper
    {
        static string BaseUri = "https://dev.comicagg.com";

        #region OAuth2 stuff

        static string OauthCallbackUri = "";
        static string OauthClientId = "";
        static string OauthClientSecret = "";

        public async static Task Login()
        {
            Uri requestUri = new Uri(BaseUri + "/oauth2/authorize/?client_id=" + OauthClientId + "&response_type=token&state=test_state&scope=write");

            WebAuthenticationResult result = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, requestUri, new Uri(OauthCallbackUri));
            if (result.ResponseStatus == WebAuthenticationStatus.Success)
            {
                Uri resultUri = new Uri(result.ResponseData);
                Dictionary<string, string> qsd = Regex.Matches(resultUri.Query, "([^?=&]+)(=([^&]*))?").Cast<Match>().ToDictionary(x => x.Groups[1].Value, x => x.Groups[3].Value);
                if (qsd["state"].Equals("test_state") && !qsd.Keys.Contains("error"))
                {
                    string grant_code = qsd["code"];
                    await GetAccessToken(grant_code);
                }
                else
                {
                    //return false;
                }
            }
            else
            {
                //return false;
            }
            //return true;
        }

        private async static Task GetAccessToken(string code)
        {
            Dictionary<string, string> kv = new Dictionary<string, string>();
            kv["grant_type"] = "authorization_code";
            kv["client_id"] = OauthClientId;
            kv["client_secret"] = OauthClientSecret;
            kv["code"] = code;
            kv["redirect_url"] = OauthCallbackUri;
            string ret = await DoRequest("/oauth2/access_token/", Methods.POST, kv, false);
            JsonObject token;
            JsonObject.TryParse(ret, out token);
            //Instead of checking how many items were returned, better check if an error was sent or not
            if (token.Count == 4)
            {
                Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
                roamingSettings.Values["access_token"] = token["access_token"].GetString();
                roamingSettings.Values["refresh_token"] = token["refresh_token"].GetString();
            }
            else
            {
                //TODO
                //return false;
            }
        }

        public static bool HaveAccessToken()
        {
            Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            return roamingSettings.Values.Keys.Contains("access_token") && ((string) roamingSettings.Values["access_token"]).Length > 0;
        }

        public static string GetAccessToken()
        {
            Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            return (string) roamingSettings.Values["access_token"];
        }

        public static void Logout()
        {
            Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            roamingSettings.Values["access_token"] = "";
            roamingSettings.Values["refresh_token"] = "";
        }

        #endregion

        public enum Methods
        {
            GET,
            POST
        }

        /// <summary>
        ///  Does an HTTP request
        ///  <param name="uriPath">Absolute HTTP path. Must start with a slash (/).</param>
        ///  <param name="authenticated">Bool to indicate wether the request will be OAuth authenticated</param>
        /// </summary>
        public async static Task<string> DoRequest(string uriPath, Methods method, Dictionary<string,string> content, bool authenticated)
        {
            Uri url = new Uri(BaseUri + uriPath);
            return await DoRequest(url, method, content, authenticated);
        }

        private async static Task<string> DoRequest(Uri url, Methods method, Dictionary<string,string> content, bool authenticated)
        {
            //Asked for an authenticated request but we don't have an Access Token. Do nothing.
            if (authenticated && !HaveAccessToken()) return null;

            HttpClient hc = new HttpClient();
            HttpContent httpContent = null;
            if (content != null)
                httpContent = new FormUrlEncodedContent(content);

            if (authenticated)
            {
                hc.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(GetAccessToken());
            }
            string ret = null;
            try
            {
                switch (method)
                {
                    case Methods.GET:
                        ret = await hc.GetStringAsync(url);
                        break;
                    case Methods.POST:
                        HttpResponseMessage response = await hc.PostAsync(url, httpContent);
                        if (response.IsSuccessStatusCode)
                        {
                            ret = await response.Content.ReadAsStringAsync();
                        }
                        break;
                }
            }
            catch (Exception e)
            {
            }
            return ret;
        }
    }
}
