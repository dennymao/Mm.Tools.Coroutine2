using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mm.Tools.Coroutine
{
    public class CTask<T> : INotifyCompletion
    {
        private CoroutineItem  coroutineItem;
        public CTask(CoroutineItem citem)
        {
            coroutineItem = citem;
        }
        public CTask<T> GetAwaiter()
        {
            return this;
        }

        public bool IsCompleted
        {
            get
            {
                if (coroutineItem.Status == CoroutineStatus.end)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public T GetResult()
        {
            if (coroutineItem.result != null)
                return (T)coroutineItem.result;
            else
                return default(T);
        }
        public void OnCompleted(Action continuation)
        {
            if (coroutineItem.Status != CoroutineStatus.end)
            {
                coroutineItem.AsyncCallBackAction = () =>
                {
                    ThreadPool.QueueUserWorkItem(state =>
                    {
                        // Console.WriteLine("开始执行Await后面部分的代码");
                        //由于编译器特性这里无需错误处理！会转到正常try该处无法捕获
                        //try
                        //{

                            continuation();
                        //}
                        //catch (Exception ex){
                        //    //错误。
                        //    throw ex;
                        //}
                        // Console.WriteLine("后面部分的代码执行完毕");
                    });
                };
            }
            else
            {
                continuation();
            }
            
        } 
    }
 
}
