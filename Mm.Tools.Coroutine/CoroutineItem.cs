using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Mm.Tools.Coroutine
{
    /// <summary>
    /// 协程对象（子过程必须使用该对象封装）
    /// 
    /// </summary>
    public class CoroutineItem
    {
        private readonly Coroutines _coroutines;
        private readonly Func<CoroutineItem, object,IEnumerator<CoroutineControl>> _act;
        private CoroutineStatus _Status = CoroutineStatus.unset;
        private System.Diagnostics.Stopwatch _sleepWatch;
        private int sleepTime = 0;
        private IEnumerator<CoroutineControl> _actBuf;
        [Obsolete]
        public int RestartCount = 0; //默认重试，该功能谨慎使用，默认不该由框架处理异常重试。
        [Obsolete]
        public int RestartInterval = 1000;//同上

        
        public Action<Exception> ExceptionOperator;
        public Action AsyncCallBackAction; 
        public object result = null; //协程结果返回，此处主要用于Async await
        private object _args;
        public object target;
        /// <summary>
        /// 协程子过程专用Sleep调用，禁止直接使用Thread.Sleep()等其他原生线程休眠方法
        /// </summary>
        /// <param name="ms">休眠毫秒数</param>
        public void Sleep(int ms)
        { 

            sleepTime = ms;
            _Status = CoroutineStatus.sleeping;
            _sleepWatch = System.Diagnostics.Stopwatch.StartNew();
        }

        /// <summary>
        /// 内置唤醒
        /// </summary>
        private void Wakeup()
        {
            if (_Status == CoroutineStatus.sleeping)
            {
                if (_sleepWatch.ElapsedMilliseconds >= sleepTime)
                {
                    _Status = CoroutineStatus.runing;
                }
                else
                { 
                    System.Threading.Thread.Sleep((int)((long)sleepTime - _sleepWatch.ElapsedMilliseconds));
                }
            }
        }
        
        public CoroutineStatus Status { get => _Status; }
         
        /// <summary>
        /// 构造函数，要求传入
        /// </summary>
        /// <param name="coroutines"></param>
        /// <param name="func"></param>
        internal CoroutineItem(Coroutines coroutines, Func<CoroutineItem,object, IEnumerator<CoroutineControl>> func,  object args=null)
        {
            this._coroutines = coroutines;
            this._act = func;
            this.target = coroutines.target;
            this._args = args;
            this.RestartCount = coroutines.BreakReStartCount;
            this.RestartInterval = coroutines.BreakReStartInterval;
            if (coroutines.ExceptionOperator != null) this.ExceptionOperator = coroutines.ExceptionOperator;
        }

        public IEnumerator<CoroutineControl> start()
        {
            if(_Status<CoroutineStatus.starting) //第一次加载
                _Status = CoroutineStatus.starting;

            if (_Status == CoroutineStatus.starting) //第一次初始化 || 错误重新初始化
            {
                _actBuf = _act(this,_args);
                _Status = CoroutineStatus.runing;
            }

            if (_Status == CoroutineStatus.sleeping) //休眠状态
            {
                Wakeup();
                if (_Status == CoroutineStatus.sleeping)
                {
                    yield return CoroutineControl.sched;
                }
            }

            bool mnextb = false;
            CoroutineControl result = CoroutineControl.end;

            do
            {
                try
                {
                    //add function 
                    //此处增加了_actBuf为null的处理
                    //当_actBuf为空的时候，相当于没有用yeid返回，直接赋结束标记  CoroutineStatus.end;
                    if (_actBuf is null)
                    {
                        _Status = CoroutineStatus.end;
                    } 
                    else
                    {

                        mnextb = _actBuf.MoveNext();

                        ///Yu Fixd 2023.12.5 Support yeild break;
                        if (!mnextb)
                        {
                            result = CoroutineControl.end;
                            _Status = CoroutineStatus.end;
                            break;
                        } 
                        

                        if (_actBuf.Current == CoroutineControl.sched)
                        {

                            result = CoroutineControl.sched;
                            break;
                        }
                        //fix 2023.11.15 居然没处理end
                        if (_actBuf.Current == CoroutineControl.end)
                        {

                            result = CoroutineControl.end;
                            _Status = CoroutineStatus.end;
                            break;
                        }
                    }

                    
                    
                }
                catch (Exception ex)
                {
                    
                    //错误重试次数到达
                    if (RestartCount < 1)
                    {
                        //错误处理委托 fix 2023.11.8
                        //如果有错误重试机制，又设置了错误委托，则等待重试超过次数再触发，修正。
                        if (ExceptionOperator != null)
                        {
                            ExceptionOperator(ex);
                        }

                        _Status = CoroutineStatus.end;
                        result = CoroutineControl.end; 
                    }
                    else
                    {
                        RestartCount--;
                        Sleep(RestartInterval);
                        _Status = CoroutineStatus.starting;
                        result = CoroutineControl.sched; 

                    }
                    break;
                }
            } while (mnextb);


            /*2021.8.20
            while (_actBuf.MoveNext())
            {
                

                if (_actBuf.Current == CoroutineControl.sched)
                {
                    yield return CoroutineControl.sched;
                }
                
            }
            _Status = CoroutineStatus.end;
            */
            if (result == CoroutineControl.end)
            {
                if (this.AsyncCallBackAction!=null)
                {
                    AsyncCallBackAction.Invoke();
                }
                yield return result;
            }
            else
            {
                yield return result;
            }
        }
    }
    /// <summary>
    /// 协程子过程回发状态
    /// </summary>
    public enum CoroutineControl
    {
        /// <summary>
        /// 要求正常运行
        /// </summary>
        runing,
        /// <summary>
        /// 要求暂时交出控制权
        /// </summary>
        sched,
        /// <summary>
        /// 当前子过程结束
        /// </summary>
        end
    }
    /// <summary>
    /// 当前协程状态
    /// </summary>
    public enum CoroutineStatus
    {
        /// <summary>
        /// 未设置
        /// </summary>
        unset,
        /// <summary>
        /// 开始中
        /// </summary>
        starting,
        runing,
        sleeping,
        ending,
        end
    }
}
