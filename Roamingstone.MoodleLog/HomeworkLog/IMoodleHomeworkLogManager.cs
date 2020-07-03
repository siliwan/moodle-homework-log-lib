using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Roamingstone.MoodleLog.HomeworkLog
{
    public interface IMoodleHomeworkLogManager
    {
        Task<Cookie> Login(string username, string password);

        Task<string> GetSesskey(Cookie moodleSession);
        Task<string> GetSesskey(string moodleSession);

        Task<bool> SetLogContent(Cookie moodleSession, string sesskey, long logId, string htmlContent, bool append = false);
        Task<bool> SetLogContent(string moodleSession, string sesskey, long logId, string htmlContent, bool append = false);
        Task<string> GetLogContent(Cookie moodleSession, long logId);
        Task<string> GetLogContent(string moodleSession, long logId);
    }
}
