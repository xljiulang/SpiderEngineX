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
    /// 表示网络爬虫
    /// </summary>
    public class Spider : WebRequestHandler
    {
        /// <summary>
        /// http客户端
        /// </summary>
        private readonly HttpClient httpClient;

        /// <summary>
        /// 同步锁
        /// </summary>
        private readonly object hashSync = new object();

        /// <summary>
        /// 记录已爬的页面
        /// </summary>
        private readonly HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


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
        }

        /// <summary>
        /// 启动爬虫任务
        /// </summary>
        /// <param name="address">站点地址</param>
        /// <exception cref="NotSupportedException"></exception>
        /// <returns></returns>
        public async Task RunAsync(Uri address)
        {
            await this.RunAsync(address, default(CancellationToken));
        }

        /// <summary>
        /// 启动爬虫任务
        /// </summary>
        /// <param name="address">站点地址</param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="NotSupportedException"></exception>
        /// <returns></returns>
        public virtual async Task RunAsync(Uri address, CancellationToken cancellationToken)
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

            this.hashSet.Clear();
            this.Progress.Reset();

            await this.SpiderAddress(address, cancellationToken);
        }


        /// <summary>
        /// 爬虫一个页面
        /// </summary>
        /// <param name="address">页面地址</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task SpiderAddress(Uri address, CancellationToken cancellationToken)
        {
            try
            {
                this.Progress.RaiseCreateOne();
                using (var response = await this.httpClient.GetAsync(address, cancellationToken))
                {
                    this.Progress.RaiseCompleteOne();
                    await this.ProcessResponseAsync(response, address, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                this.Progress.RaiseCompleteOne();
                this.OnException(address, ex);
            }
        }


        /// <summary>
        /// 处理回复内容
        /// </summary>     
        /// <param name="response">回复</param>
        /// <param name="address">地址</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task ProcessResponseAsync(HttpResponseMessage response, Uri address, CancellationToken cancellationToken)
        {
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var html = await this.GetStringContentAsync(response);
                var page = new Page(html, address);

                var tasks = this.GetPageLinks(page)
                    .Select(item => page.GetAbsoluteUri(item))
                    .Where(item => item != null && this.IsNotRepeat(item))
                    .Select(item => this.SpiderAddress(item, cancellationToken));

                await Task.WhenAll(tasks);
            }
        }

        /// <summary>
        /// 检测页面是否没被爬虫过
        /// </summary>
        /// <param name="address">地址</param>
        /// <returns></returns>
        private bool IsNotRepeat(Uri address)
        {
            lock (this.hashSync)
            {
                return this.hashSet.Add(address.ToString());
            }
        }


        /// <summary>
        /// 获取回复对象的文本内容
        /// </summary>
        /// <param name="response">回复</param>
        /// <returns></returns>
        private async Task<string> GetStringContentAsync(HttpResponseMessage response)
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
        /// 获取页面的链接
        /// </summary>
        /// <param name="page">页面</param>
        /// <returns></returns>
        protected virtual IEnumerable<Uri> GetPageLinks(Page page)
        {
            return page
                   .Find("a")
                   .Select(a => a.GetAttribute("href"))
                   .Select(href => page.GetAbsoluteUri(href))
                   .Where(item => item != null);
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
