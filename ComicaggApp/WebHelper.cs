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
        static Uri BaseApiUri = new Uri("https://www.comicagg.com/api/");
        static Uri callbackUri = new Uri("http://oauth2/redirect");
        static string client_id = "28e6f56805988feb5547";
        static string client_secret = "1e3bf1c96ae2163a04b474ac1efa7cadb555ee32";

        public async static Task Login()
        {
            Uri requestUri = new Uri("https://www.comicagg.com/oauth2/authorize/?client_id=" + client_id + "&response_type=token&state=test_state&scope=write");

            WebAuthenticationResult result = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, requestUri, callbackUri);
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
            Uri requestUri = new Uri("https://www.comicagg.com/oauth2/access_token/");

            Dictionary<string, string> kv = new Dictionary<string, string>();
            kv["grant_type"] = "authorization_code";
            kv["client_id"] = client_id;
            kv["client_secret"] = client_secret;
            kv["code"] = code;
            kv["redirect_url"] = callbackUri.ToString();
            string ret = await DoRequest(requestUri, Methods.POST, kv, false);
            JsonObject token;
            JsonObject.TryParse(ret, out token);
            if (token.Count == 4)
            {
                Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
                roamingSettings.Values["access_token"] = token["access_token"].GetString();
                roamingSettings.Values["refresh_token"] = token["refresh_token"].GetString();
            }
            else
            {
                //return false;
            }
        }

        public static bool HaveAccessToken()
        {
            Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            return roamingSettings.Values.Keys.Contains("access_token");
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

        public enum Methods
        {
            GET,
            POST
        }

        public async static Task<string> DoRequest(string uriPath, Methods method, Dictionary<string,string> content, bool authenticated)
        {
            UriBuilder ub = new UriBuilder(BaseApiUri);
            ub.Path += uriPath.StartsWith("/") ? uriPath.Substring(1) : uriPath;
            Uri url = ub.Uri;
            return await DoRequest(url, method, content, authenticated);
        }

        public async static Task<string> DoRequest(Uri url, Methods method, Dictionary<string,string> content, bool authenticated)
        {
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
            return ret;
        }
    }
}
