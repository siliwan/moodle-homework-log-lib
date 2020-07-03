using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Roamingstone.MoodleLog.HomeworkLog
{
    public class MoodleHomeworkLogManager : IMoodleHomeworkLogManager
    {
        protected const string LOGIN_ENDPOINT = "{0}/login/index.php?authldap_skipntlmsso=1";
        protected const string BASE_ENDPOINT = "{0}";
        protected const string LOG_ENDPOINT = "{0}/mod/journal/edit.php";

        protected const string EDITOR_EL_ID = "id_text_editor"; //id_text_editoreditable

        protected string siteurl;

        private Cookie __temp_moodle_session;

        public bool DumpBodies { get; set; } = false;

        public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/81.0.4044.138 Safari/537.36 OPR/68.0.3618.125";

        /// <summary>
        /// Class for interacting with the log of a moodle site
        /// </summary>
        /// <param name="siteurl">Url of the moodle site. If no http schema is given, https:// will be prepended.</param>
        /// <example>https://moodle.kwb.ch</example>
        public MoodleHomeworkLogManager(string siteurl)
        {
            if (!siteurl.StartsWith("http"))
            {
                siteurl = "https://" + siteurl;
            }

            this.siteurl = siteurl;
        }

        /// <summary>
        /// Gets the necessary session key from the website.
        /// </summary>
        /// <param name="moodleSession">Moodle session obtained when logging in.</param>
        /// <returns>The session key needed for further interactions</returns>
        public async Task<string> GetSesskey(Cookie moodleSession)
        {
            Regex rgxSessKey = new Regex("\"sesskey\"\\s*:\\s*\"(?<sesskey>[a-zA-Z0-9]*)\"");

            using (var handler = new HttpClientHandler())
            using (var cli = new HttpClient(handler))
            {
                CookieContainer cookies = new CookieContainer();

                cookies.Add(moodleSession);

                Uri reqUri = new Uri(string.Format(BASE_ENDPOINT, siteurl));

                handler.CookieContainer = cookies;
                cli.DefaultRequestHeaders.Add("User-Agent", UserAgent);

                cli.BaseAddress = reqUri;

                HttpResponseMessage sesskeyResult = await cli.GetAsync("");

                string body = await sesskeyResult.Content.ReadAsStringAsync();

                if (DumpBodies)
                {
                    Console.WriteLine(body);
                }

                Match match = rgxSessKey.Match(body);

                if (match.Success)
                {
                    return match.Groups["sesskey"].Value;
                }
                else
                {
                    throw new Exception($"Unable to find session key in site. Url: {reqUri.AbsoluteUri}, StatusCode: {sesskeyResult.StatusCode}");
                }
            }
        }

        /// <summary>
        /// Logs a user in with his username and password to obtain a session.
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns>A cookie containing the moodle session</returns>
        public async Task<Cookie> Login(string username, string password)
        {
            using (var handler = new HttpClientHandler())
            using (var cli = new HttpClient(handler))
            {
                CookieContainer cookies = new CookieContainer();

                Uri reqUri = new Uri(string.Format(LOGIN_ENDPOINT, siteurl));

                handler.CookieContainer = cookies;
                cli.DefaultRequestHeaders.Add("User-Agent", UserAgent);

                cli.BaseAddress = reqUri;

                Dictionary<string, string> formParams = new Dictionary<string, string>();
                formParams.Add("username", username);
                formParams.Add("password", password);

                HttpResponseMessage loginResult = await cli.PostAsync("", new FormUrlEncodedContent(formParams));

                if (DumpBodies)
                {
                    string body = await loginResult.Content.ReadAsStringAsync();
                    Console.WriteLine(body);
                }

                return cookies.GetCookies(reqUri)["MoodleSession"];
            }
        }

        /// <summary>
        /// Sets the content of the log to the value given in <paramref name="htmlContent"/>. Currently not supporting appending it.
        /// </summary>
        /// <param name="moodleSession">Cookie containing session</param>
        /// <param name="sesskey">Session key. Stupid thing.</param>
        /// <param name="logId">Id of the log that you can lookup in the url.</param>
        /// <param name="htmlContent">Content that should be added/appended depending on <paramref name="append"/></param>
        /// <param name="append">If the content in <paramref name="htmlContent"/> should be appended or overwrite the current log.</param>
        /// <returns>If it was successful</returns>
        public async Task<bool> SetLogContent(Cookie moodleSession, string sesskey, long logId, string htmlContent, bool append = false)
        {
            if (append)
            {
                string precedingContent = await GetLogContent(moodleSession, logId);
                htmlContent = precedingContent + htmlContent;
            }

            using (var handler = new HttpClientHandler())
            using (var cli = new HttpClient(handler))
            {
                CookieContainer cookies = new CookieContainer();
                cookies.Add(moodleSession);

                Uri reqUri = new Uri(string.Format(LOG_ENDPOINT, siteurl));

                handler.CookieContainer = cookies;
                cli.DefaultRequestHeaders.Add("User-Agent", UserAgent);

                cli.BaseAddress = reqUri;

                Dictionary<string, string> formParams = new Dictionary<string, string>();
                formParams.Add("id", logId.ToString());
                formParams.Add("_qf__mod_journal_entry_form", "1");
                formParams.Add("text_editor[text]", htmlContent);
                formParams.Add("text_editor[format]", "1");
                formParams.Add("sesskey", sesskey);

                HttpResponseMessage res = await cli.PostAsync("", new FormUrlEncodedContent(formParams));

                if (DumpBodies)
                {
                    string body = await res.Content.ReadAsStringAsync();
                    Console.WriteLine(body);
                }

                return res.StatusCode == HttpStatusCode.OK && !res.RequestMessage.RequestUri.ToString().EndsWith("/login/index.php");
            }
        }

        /// <summary>
        /// Gets the necessary session key from the website.
        /// </summary>
        /// <param name="moodleSession">Moodle session obtained when logging in.</param>
        /// <returns>The session key needed for further interactions</returns>
        public Task<string> GetSesskey(string moodleSession)
        {
            return GetSesskey(new Cookie("MoodleSession", moodleSession, "/", siteurl));
        }

        /// <summary>
        /// Sets the content of the log to the value given in <paramref name="htmlContent"/>. Currently not supporting appending it.
        /// </summary>
        /// <param name="moodleSession">Cookie containing session</param>
        /// <param name="sesskey">Session key. Stupid thing.</param>
        /// <param name="logId">Id of the log that you can lookup in the url.</param>
        /// <param name="htmlContent">Content that should be added/appended depending on <paramref name="append"/></param>
        /// <param name="append">If the content in <paramref name="htmlContent"/> should be appended or overwrite the current log.</param>
        /// <returns>If it was successful</returns>
        public Task<bool> SetLogContent(string moodleSession, string sesskey, long logId, string htmlContent, bool append = false)
        {
            return SetLogContent(new Cookie("MoodleSession", moodleSession, "/", siteurl), sesskey, logId, htmlContent, append);
        }

        /// <summary>
        /// Gets the contents of the log
        /// </summary>
        /// <param name="moodleSession"></param>
        /// <param name="logId"></param>
        /// <returns>String with html that is contained in the editor.</returns>
        public async Task<string> GetLogContent(Cookie moodleSession, long logId)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(string.Format(LOG_ENDPOINT, siteurl) + $"?id={logId}");
            req.Method = "GET";

            CookieContainer cookies = new CookieContainer();
            cookies.Add(moodleSession);

            req.CookieContainer = cookies;
            req.AllowAutoRedirect = false;

            HttpWebResponse res = (HttpWebResponse)(await req.GetResponseAsync());

            using (var reader = new StreamReader(res.GetResponseStream()))
            {
                HtmlDocument html = new HtmlDocument();
                string raw = await reader.ReadToEndAsync();
                html.LoadHtml(raw);

                HtmlNode editorNode = html.GetElementbyId(EDITOR_EL_ID);

                return WebUtility.HtmlDecode(editorNode?.InnerHtml);
            }
        }

        private bool GetLogContent_OnPreRequest(HttpWebRequest request)
        {
            CookieContainer cookies = new CookieContainer();
            cookies.Add(__temp_moodle_session);

            request.CookieContainer = cookies;
            return true;
        }

        /// <summary>
        /// Gets the content of the log
        /// </summary>
        /// <param name="moodleSession"></param>
        /// <param name="logId"></param>
        /// <returns>String with html that is contained in the editor</returns>
        public Task<string> GetLogContent(string moodleSession, long logId)
        {
            return GetLogContent(new Cookie("MoodleSession", moodleSession, "/", siteurl), logId);
        }
    }
}
