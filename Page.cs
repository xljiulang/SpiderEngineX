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
    [DebuggerDisplay("{Url}")]
    public sealed class Page : CQ
    {
        /// <summary>
        /// 获取页面地址
        /// </summary>
        public Uri Url { get; private set; }

        /// <summary>
        /// 获取html内容
        /// </summary>
        public string HtmlContent { get; private set; }

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
        /// <param name="url">地址</param>    
        /// <param name="html">html内容</param>
        internal Page(string html, Uri url)
            : base(html)
        {
            this.HtmlContent = html;
            this.Url = url;
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
        /// <param name="href">链接</param>
        /// <returns></returns>
        public Uri GetAbsoluteUri(string href)
        {
            if (href == null)
            {
                return null;
            }
            Uri uri = null;
            Uri.TryCreate(this.Url, href, out uri);
            return uri;
        }

        /// <summary>
        /// 链接地址转换为绝对地址
        /// 失败则返回null
        /// </summary>
        /// <param name="href">链接</param>
        /// <returns></returns>
        public Uri GetAbsoluteUri(Uri href)
        {
            if (href == null)
            {
                return null;
            }
            Uri uri = null;
            Uri.TryCreate(this.Url, href, out uri);
            return uri;
        }

        /// <summary>
        /// 查找页面内的a标签的链接        
        /// </summary>
        /// <param name="page">页面</param>
        /// <returns></returns>
        public IEnumerable<Uri> FindAllHrefs()
        {
            return from a in this.Find("a")
                   let href = a.GetAttribute("href")
                   let url = this.GetAbsoluteUri(href)
                   where url != null
                   select url;
        }

        /// <summary>
        /// 转换为字符串
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.HtmlContent;
        }
    }
}
