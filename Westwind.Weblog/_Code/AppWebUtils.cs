using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Westwind.Weblog.Business.Configuration;

namespace Westwind.Weblog
{
    public class AppWebUtils
    {

        /// <summary>
        /// Formats a date in friendly format
        /// </summary>
        /// <param name="date"></param>
        /// <param name="showTime"></param>
        /// <returns></returns>
        public static string WebLogDateString(DateTime date, bool showTime)
        {
            string FormattedDate = "";
            if (date.Date == DateTime.Today)
                FormattedDate = "Today"; //Resources.Resources.Today; 
            else if (date.Date == DateTime.Today.AddDays(-1))
                FormattedDate = "Yesterday"; //Resources.Resources.Yesterday;
            else if (date.Date > DateTime.Today.AddDays(-6))
                // *** Show the Day of the week
                FormattedDate = date.ToString("dddd");
            else
                FormattedDate = date.ToString("MMMM dd, yyyy");

            if (showTime)
                FormattedDate += " @ " + date.ToString("h:mmtt").ToLower();

            return FormattedDate;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="text"></param>
        /// <param name="newWindow"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public static string WebLogShortenedUrl(string url, string text, bool newWindow, int maxLength)
        {
            if (text.Length > maxLength)
            {
                text = text.Substring(0, maxLength / 2 - 3) + "..." + text.Substring(text.Length - maxLength / 2);
            }

            string Output = "<a href=\"" + url + "\"";

            if (newWindow)
                Output += " target=\"_top\"";

            Output += ">" + text + "</a>";

            return Output;
        }
    }
}
