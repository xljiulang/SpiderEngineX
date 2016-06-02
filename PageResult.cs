using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SpiderEngineX
{
    /// <summary>
    /// 表示请求页面结果
    /// </summary>
    public class PageResult
    {
        /// <summary>
        /// 获取页面地址
        /// </summary>
        public Uri Address { get; private set; }
        /// <summary>
        /// 获取页面html文本
        /// </summary>
        public string Html { get; private set; }

        /// <summary>
        /// 状态
        /// </summary>
        public HttpStatusCode Status { get; private set; }

        /// <summary>
        /// 请求页面结果
        /// </summary>
        /// <param name="address">页面地址</param>
        /// <param name="html">html</param>
        /// <param name="status">状态</param>
        internal PageResult(Uri address, string html, HttpStatusCode status)
        {
            this.Address = address;
            this.Html = html;
            this.Status = status;
        }

        /// <summary>
        /// 转换为page对象
        /// </summary>
        /// <returns></returns>
        public Page ToPage()
        {
            return new Page(this.Html, this.Address);
        }

        /// <summary>
        /// 字符串显示
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Html;
        }
    }
}
