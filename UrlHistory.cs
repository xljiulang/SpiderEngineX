using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpiderEngineX
{
    /// <summary>
    /// 爬虫地址历史记录
    /// </summary>
    public class UrlHistory
    {
        /// <summary>
        /// 同步锁
        /// </summary>
        private readonly object syncRoot = new object();

        /// <summary>
        /// 记录已爬的页面
        /// </summary>
        private readonly HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 获取当前元素数量
        /// </summary>
        public int Count
        {
            get
            {
                return this.hashSet.Count;
            }
        }

        /// <summary>
        /// 添加新项
        /// </summary>
        /// <param name="url">Url地址</param>
        /// <returns></returns>
        public bool Add(string url)
        {
            lock (this.syncRoot)
            {
                return this.hashSet.Add(url);
            }
        }

        /// <summary>
        /// 清除所有项
        /// </summary>
        public void Clear()
        {
            lock (this.syncRoot)
            {
                this.hashSet.Clear();
            }
        }


        /// <summary>
        /// 转换为数组
        /// </summary>
        /// <returns></returns>
        public string[] ToArray()
        {
            lock (this.syncRoot)
            {
                return this.hashSet.ToArray();
            }
        }
    }
}
