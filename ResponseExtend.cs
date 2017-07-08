using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SpiderEngineX
{
    /// <summary>
    /// HttpResponseMessage扩展
    /// </summary>
    static class ResponseExtend
    {
        /// <summary>
        /// 读取回复对象的文本内容
        /// </summary>
        /// <param name="response">回复</param>
        /// <returns></returns>
        public static async Task<string> ReadStringContentAsync(this HttpResponseMessage response)
        {
            string contentType = null;
            IEnumerable<string> contentTypeValues = null;
            if (response.Headers.TryGetValues("Content-Type", out contentTypeValues))
            {
                contentType = contentTypeValues.LastOrDefault();
            }

            var byteContent = await response.Content.ReadAsByteArrayAsync();
            var encoding = FindEncoding(contentType);
            if (encoding != null)
            {
                return encoding.GetString(byteContent);
            }

            var html = Encoding.UTF8.GetString(byteContent);
            encoding = FindEncoding(html);

            if (encoding == null || encoding == Encoding.UTF8)
            {
                return html;
            }
            else
            {
                return encoding.GetString(byteContent);
            }
        }

        /// <summary>
        /// 查找编码
        /// </summary>
        /// <param name="content">内容</param>
        /// <returns></returns>
        private static Encoding FindEncoding(string content)
        {
            if (content == null)
            {
                return null;
            }

            var match = Regex.Match(content, "(?<=charset=\"*)(\\w|-)+", RegexOptions.IgnoreCase);
            if (match.Success == false)
            {
                return null;
            }

            var chartSet = match.Value;
            var encoding = Encoding
                .GetEncodings()
                .FirstOrDefault(item => item.Name.Equals(chartSet, StringComparison.OrdinalIgnoreCase));

            if (encoding != null)
            {
                return encoding.GetEncoding();
            }
            else
            {
                return null;
            }
        }
    }
}
