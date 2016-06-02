using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SpiderEngineX
{
    /// <summary>
    /// 表示爬虫进度信息
    /// </summary>
    public class Progress
    {
        /// <summary>
        /// 已创建的任务数
        /// </summary>
        private long created = 0L;

        /// <summary>
        /// 已执行完成任务数
        /// </summary>
        private long completion = 0L;

        /// <summary>
        /// 待完成的任务数
        /// </summary>
        private long unComplete = 0L;

        /// <summary>
        /// 获取已创建的任务数
        /// </summary>         
        public long Created
        {
            get
            {
                return Interlocked.Read(ref this.created);
            }
        }

        /// <summary>
        /// 获取已执行完成任务数
        /// </summary>
        public long Completion
        {
            get
            {
                return Interlocked.Read(ref this.completion);
            }
        }

        /// <summary>
        /// 获取未完成任务数
        /// </summary>
        public long UnComplete
        {
            get
            {
                return Interlocked.Read(ref this.unComplete);
            }
        }

        /// <summary>
        /// 重置
        /// </summary>
        internal void Reset()
        {
            this.created = 0L;
            this.completion = 0L;
            this.unComplete = 0L;
        }

        /// <summary>
        /// 创建一个页面
        /// </summary>
        internal void RaiseCreateOne()
        {
            Interlocked.Increment(ref this.created);
            Interlocked.Increment(ref this.unComplete);
        }

        /// <summary>
        /// 完成一个页面
        /// </summary>
        internal void RaiseCompleteOne()
        {
            Interlocked.Increment(ref this.completion);
            Interlocked.Decrement(ref this.unComplete);
        }

        /// <summary>
        /// 转换为字符串
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("任务数：{0}  待完成：{1}  已完成：{2}", this.Created, this.UnComplete, this.Completion);
        }
    }
}
