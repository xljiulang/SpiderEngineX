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

            var children =
                from link in this.FindHrefs(page)
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
                using (var response = await this.httpClient.GetAsync(url, cancellationToken))
                {
                    response.EnsureSuccessStatusCode();
                    var html = await response.ReadStringContentAsync();
                    var page = new Page(html, url);

                    this.Progress.RaiseCompleteOne();
                    this.OnSpideCompleted(page);
                    return page;
                }
            }
            catch (Exception ex)
            {
                this.Progress.RaiseCompleteOne();
                this.OnException(url, ex);
                return null;
            }
        }


        /// <summary>
        /// 爬完一个页面
        /// </summary>
        /// <param name="page">页面</param>
        protected abstract void OnSpideCompleted(Page page);


        /// <summary>
        /// 查找页面内的链接
        /// 这些链接将用于继续爬虫
        /// </summary>
        /// <param name="page">页面</param>
        /// <returns></returns>
        protected abstract IEnumerable<Uri> FindHrefs(Page page);


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
