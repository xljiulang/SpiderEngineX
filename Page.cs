using CsQuery;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace SpiderEngineX
{
    /// <summary>
    /// 表示Html页面
    /// </summary>
    [DebuggerDisplay("{Address}")]
    public sealed class Page : CQ
    {
        /// <summary>
        /// 获取页面地址
        /// </summary>
        public Uri Address { get; private set; }

        /// <summary>
        /// 获取html内容
        /// </summary>
        public string Html { get; private set; }

        /// <summary>
        /// 获取标题
        /// </summary>
        /// <returns></returns>
        public string Title
        {
            get
            {
                var title = this.Find("title").Html();
                return HttpUtility.HtmlDecode(title);
            }
        }

        /// <summary>
        /// 页面
        /// </summary>
        /// <param name="address">地址</param>    
        /// <param name="option">爬虫选项</param>
        internal Page(string html, Uri address)
            : base(html)
        {
            this.Html = html;
            this.Address = address;
        }

        /// <summary>
        /// html解码
        /// </summary>
        /// <param name="html">html</param>
        /// <returns></returns>
        public static string HtmlDecode(string html)
        {
            return HttpUtility.HtmlDecode(html);
        }

        /// <summary>
        /// 链接地址转换为绝对地址
        /// 失败则返回null
        /// </summary>
        /// <param name="link">链接</param>
        /// <returns></returns>
        public Uri GetAbsoluteUri(string link)
        {
            if (link == null)
            {
                return null;
            }
            Uri uri = null;
            Uri.TryCreate(this.Address, link, out uri);
            return uri;
        }

        /// <summary>
        /// 链接地址转换为绝对地址
        /// 失败则返回null
        /// </summary>
        /// <param name="link">链接</param>
        /// <returns></returns>
        public Uri GetAbsoluteUri(Uri link)
        {
            if (link == null)
            {
                return null;
            }
            Uri uri = null;
            Uri.TryCreate(this.Address, link, out uri);
            return uri;
        }

        /// <summary>
        /// 转换为字符串
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Html;
        }
    }
}
