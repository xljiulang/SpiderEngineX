using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SpiderEngineX
{
    /// <summary>
    /// 表示网络爬虫抽象类
    /// </summary>
    public abstract class Spider : WebRequestHandler
    {
        /// <summary>
        /// http客户端
        /// </summary>
        private readonly HttpClient httpClient;


        /// <summary>
        /// 获取或设置请求超时时间
        /// </summary>
        public TimeSpan Timeout { get; set; }

        /// <summary>
        /// 获取爬虫进度
        /// </summary>
        public Progress Progress { get; private set; }

        /// <summary>
        /// 获取或设置最大连接数
        /// </summary>
        public int DefaultConnectionLimit { get; set; }

        /// <summary>
        /// 获取请求头
        /// </summary>
        public IDictionary<string, string> DefaultRequestHeaders { get; private set; }

        /// <summary>
        /// 获取爬虫地址记录
        /// </summary>
        public UrlHistory History { get; private set; }

        /// <summary>
        /// 网络爬虫
        /// </summary>
        public Spider()
        {
            this.httpClient = new HttpClient(this, false);
            this.Progress = new Progress();
            this.Timeout = TimeSpan.FromSeconds(100);
            this.DefaultConnectionLimit = 1024;
            this.ServerCertificateValidationCallback = (a, b, c, d) => true;
            this.DefaultRequestHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            this.History = new UrlHistory();
        }

        /// <summary>
        /// 启动爬虫任务
        /// </summary>
        /// <param name="siteUrl">站点地址</param>
        /// <exception cref="NotSupportedException"></exception>
        /// <returns></returns>
        public async Task RunAsync(Uri siteUrl)
        {
            await this.RunAsync(siteUrl, default(CancellationToken));
        }

        /// <summary>
        /// 启动爬虫任务
        /// </summary>
        /// <param name="siteUrl">站点地址</param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="NotSupportedException"></exception>
        /// <returns></returns>
        public async Task RunAsync(Uri siteUrl, CancellationToken cancellationToken)
        {
            if (this.Progress.UnComplete > 0L)
            {
                throw new NotSupportedException("不支持多任务爬虫");
            }

            if (this.DefaultConnectionLimit > 0)
            {
                ServicePointManager.DefaultConnectionLimit = this.DefaultConnectionLimit;
            }

            this.httpClient.Timeout = this.Timeout;
            this.httpClient.DefaultRequestHeaders.Clear();
            foreach (var kv in this.DefaultRequestHeaders)
            {
                this.httpClient.DefaultRequestHeaders.Add(kv.Key, kv.Value);
            }

            this.History.Clear();
            this.Progress.Reset();

            await this.SpideAsync(siteUrl, cancellationToken);
        }


        /// <summary>
        /// 递归爬虫页面
        /// </summary>
        /// <param name="url">页面地址</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task SpideAsync(Uri url, CancellationToken cancellationToken)
        {
            var page = await this.TrySpideUrlAsync(url, cancellationToken);
            if (page == null)
            {
                return;
            }

            var children = from link in this.FindPageSubLinks(page)
                           let absUrl = page.GetAbsoluteUri(link)
                           select this.SpideAsync(absUrl, cancellationToken);

            await Task.WhenAll(children);
        }

        /// <summary>
        /// 尝试爬虫一个页面
        /// </summary>
        /// <param name="url">页面地址</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<Page> TrySpideUrlAsync(Uri url, CancellationToken cancellationToken)
        {
            if (url == null)
            {
                return null;
            }

            if (this.History.Add(url.ToString()) == false)
            {
                return null;
            }

            try
            {
                this.Progress.RaiseCreateOne();
                var page = await this.SpideUrlAsync(url, cancellationToken);
                this.Progress.RaiseCompleteOne();
                this.OnSpidedPage(page);
                return page;
            }
            catch (Exception ex)
            {
                this.Progress.RaiseCompleteOne();
                this.OnException(url, ex);
                return null;
            }
        }

        /// <summary>
        /// 尝试爬虫一个页面
        /// </summary>
        /// <param name="url">页面地址</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<Page> SpideUrlAsync(Uri url, CancellationToken cancellationToken)
        {
            using (var response = await this.httpClient.GetAsync(url, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                var html = await this.ReadStringContentAsync(response);
                return new Page(html, url);
            }
        }


        /// <summary>
        /// 读取回复对象的文本内容
        /// </summary>
        /// <param name="response">回复</param>
        /// <returns></returns>
        private async Task<string> ReadStringContentAsync(HttpResponseMessage response)
        {
            string contentType = null;
            IEnumerable<string> contentTypeValues = null;
            if (response.Headers.TryGetValues("Content-Type", out contentTypeValues))
            {
                contentType = contentTypeValues.LastOrDefault();
            }

            var byteContent = await response.Content.ReadAsByteArrayAsync();
            var encoding = this.FindEncoding(contentType);
            if (encoding != null)
            {
                return encoding.GetString(byteContent);
            }

            var html = Encoding.UTF8.GetString(byteContent);
            encoding = this.FindEncoding(html);

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
        private Encoding FindEncoding(string content)
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

        /// <summary>
        /// 爬完一个页面
        /// </summary>
        /// <param name="page">页面</param>
        protected abstract void OnSpidedPage(Page page);


        /// <summary>
        /// 查找页面内的链接
        /// 这些链接将用于继续爬虫
        /// </summary>
        /// <param name="page">页面</param>
        /// <returns></returns>
        protected virtual IEnumerable<Uri> FindPageSubLinks(Page page)
        {
            try
            {
                return page
                       .Find("a")
                       .Select(a => a.GetAttribute("href"))
                       .Select(href => page.GetAbsoluteUri(href))
                       .Where(item => item != null);
            }
            catch (Exception ex)
            {
                this.OnException(page.Address, ex);
                return Enumerable.Empty<Uri>();
            }
        }

        /// <summary>
        /// 异常时
        /// </summary>
        /// <param name="address">地址</param>
        /// <param name="ex">异常</param>
        protected virtual void OnException(Uri address, Exception ex)
        {
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            this.httpClient.Dispose();
        }
    }
}
