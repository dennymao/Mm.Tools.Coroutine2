using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace Mm.Tools.Coroutine
{
    public class AsyncArgs<T>
    {
        public T Result;
        public object args;
        public AsyncArgs(T result, object args)
        {
            this.Result = result;
            this.args = args;
        }   
    }
    public enum CoroutinesStatus
    {
        UnStart,
        Runing,
        Keeping,
        Aborting,
        Aborted,
        Canceling,
        Canceled,
    }
    public class Coroutines
    { 

        private bool _isRuning = false; 
        private object mutexObj = new object();
        //处理完当前内存队列退出，不处理二级队列。
        public CancellationToken cancellationToken = new CancellationToken();
        public Action<Exception> ExceptionOperator;
        /// <summary>
        /// 错误重试数，如果不重新启动子进程，设为0
        /// 错误重试将重新开始子委托，而不是从错误处继续
        /// </summary>
        public int BreakReStartCount = 0;
        /// <summary>
        /// ms单位
        /// </summary>
        public int BreakReStartInterval = 1000;
        /// <summary>
        /// 协程内部运行情况
        /// </summary>
        public bool IsRuning { get => _isRuning;} 
        public CoroutinesStatus Status { get => _runStatus; }

        /// <summary>
        /// 协程间交换对象。
        /// </summary>
        public object target = null;

        //设置协程方法组
        private List<CoroutineItem> _LCIs = new List<CoroutineItem>();
        private List<CoroutineItem> _LCIs2 = new List<CoroutineItem>();

        public int CoroutinesCount
        {
            get
            {
                return _LCIs.Count;
            }
        }
        public int CoroutinesCount2
        {
            get
            {
                return _LCIs2.Count;
            }
        }

        //设置表明该对象是否持续运行。
        private CoroutinesStatus _runStatus = CoroutinesStatus.UnStart;


        //System.Collections.Concurrent.ConcurrentQueue<CoroutineItem> _LCIs = new System.Collections.Concurrent.ConcurrentQueue<CoroutineItem>();
        /// <summary>
        /// 异步获取，返回await由协程调用线程池触发
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public CTask<T> RunCoroutineItemAsync<T>(Func<CoroutineItem,object, IEnumerator<CoroutineControl>> func, object args = null)
        { 
            var citem = new CoroutineItem(this, func, args);
            var v = new CTask<T>(citem);
            SetCoroutineItem(citem);
            return v;
        }
        /// <summary>
        /// 内置类型，仅为了CTask<T> RunCoroutineItemAsync<T1,T>(Func<T1,T> func, T1 args = default, Action<Exception> exAct =null)
        /// 方法使用。
        /// </summary>
        /// <typeparam name="T1">输入类型</typeparam>
        /// <typeparam name="T">返回类型</typeparam>
        private class ciaCallParmas<T1,T>
        {
            internal Func<T1, T> func;
            internal T1 args; 
        }
        /// <summary>
        /// 单次将非异步程序强行封装异步处理
        /// 这可能会导致线程被持续占用的情况，请注意！
        /// </summary>
        /// <typeparam name="T1">输入参数类型</typeparam>
        /// <typeparam name="T">返回参数类型</typeparam>
        /// <param name="func">函数</param>
        /// <param name="args"></param>
        /// <param name="exAct"></param>
        /// <returns></returns>
        public CTask<T> RunCoroutineItemAsync<T1,T>(Func<T1,T> func, T1 args = default, Action<Exception> exAct =null)
        { 
            var citem = new CoroutineItem(this, (ci, _a)=>{
                ciaCallParmas<T1, T> arg = _a as ciaCallParmas<T1, T>;
                ci.result = arg.func(arg.args);
                return default;
            }, new ciaCallParmas<T1,T>() { func = func, args = args });
            //Err Hook
            if (exAct != null)
            {
                citem.ExceptionOperator = exAct;
            } 
            var v = new CTask<T>(citem);
            SetCoroutineItem(citem);
            return v;
        }
        //private IEnumerator<CoroutineControl> defInvork(CoroutineItem ci,object args)
        //{
        //   // ciaCallParmas<T1, T> arg = args as ciaCallParmas<T1, T>;
        //    //ci.result = arg.func(arg.args); 
        //    //yield return CoroutineControl.end;
        //}

        private void SetCoroutineItems(IEnumerable<CoroutineItem> citems)
        {
            if (IsRuning)
            {
                lock (_LCIs2)
                {

                    _LCIs2.AddRange(citems);

                }
            }
            else
            {
                lock (_LCIs)
                {
                    _LCIs.AddRange(citems);
                }
            }
        }

        private void SetCoroutineItem(CoroutineItem citem)
        {
            if (IsRuning)
            {
                lock (_LCIs2)
                {
                   
                    _LCIs2.Add(citem);

                }
            }
            else
            {
                lock (_LCIs)
                {
                    _LCIs.Add(citem);
                }
            }
        }
        /// <summary>
        /// thread safe
        /// 该方法线程安全。用于将子过程加入协程处理。
        /// </summary>
        /// <param name="func"></param>
        public void SetCoroutineItems(List<Func<CoroutineItem, object,IEnumerator<CoroutineControl>>> funcs,object args=null)
        {
            List<CoroutineItem> list = new List<CoroutineItem>(funcs.Count);
            foreach (var func in funcs)
            {
                list.Add(new CoroutineItem(this, func, args));
            }
            SetCoroutineItems(list);
        }

        public void SetCoroutineItem(Func<CoroutineItem, object, IEnumerator<CoroutineControl>> func, object args = null)
        {
            SetCoroutineItem(new CoroutineItem(this, func, args));
        }
        /// <summary>
        /// 同步器
        /// </summary>
        private void CoroutineItemsSync()
        {
            //因private，内部调用为单线程，故没有lock _LCIs对象。
            if (_LCIs2.Count == 0)
            {
                return;
            }

            lock (_LCIs2)
            {
                _LCIs.AddRange(_LCIs2);
                _LCIs2.Clear();
            }
        }

        /// <summary>
        /// 清除器
        /// </summary>
        private void CoroutineItemsClear()
        {
            for (int i = 0; i < _LCIs.Count; i++)
            {
                if (_LCIs[i].Status == CoroutineStatus.end)
                {
                    _LCIs.Remove(_LCIs[i]);
                    i--;
                }
            }
            
        }

        public void SetKeep()
        {
            if (_runStatus == CoroutinesStatus.UnStart)
                _runStatus = CoroutinesStatus.Keeping;
        }
        public void SetUnKeep()
        {
            if (_runStatus == CoroutinesStatus.Keeping)
                _runStatus = CoroutinesStatus.Runing;
        }
        /// <summary>
        /// 要求容器得到控制权后立刻退出
        /// </summary>
        public void Abort()
        {
            if(_runStatus== CoroutinesStatus.Keeping || _runStatus== CoroutinesStatus.Runing)
                _runStatus = CoroutinesStatus.Aborting;
        }
        public void BackgroundStart()
        {
            Task.Factory.StartNew(() => { this.Start(); },this.cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
        public void Start()
        {
            Start(this.cancellationToken);
        }
        public void Start(CancellationToken ctoken)
        {
            //入口互斥，防止并发start。一旦一个start开始了，余下均不起作用。
            //这里单独使用了isruning，没有使用status的状态。为了单独计算
            lock (mutexObj)
            {
                if (IsRuning)
                {
                    return;
                }
                else
                {
                    _isRuning = true;
                    if (_runStatus == CoroutinesStatus.UnStart)
                    {
                        _runStatus = CoroutinesStatus.Runing;
                    }
                }
            } 


            //bool TaskOver = true;
             
            while (!ctoken.IsCancellationRequested)
            {
                if (_LCIs.Count == 0)
                {
                    if (_runStatus != CoroutinesStatus.Keeping )
                    {
                        _isRuning = false;

                        _runStatus = CoroutinesStatus.UnStart;
                        break;
                    }
                    System.Threading.Thread.Sleep(1); //防止cpu资源耗费太多，提高性能可设置为0
                }
               // _isRuning = true;


                foreach (var v in _LCIs)
                { 

                    var v2 = v.start();
                    while (v2.MoveNext())
                    {
                        if(_runStatus== CoroutinesStatus.Aborting)
                        {
                            break;
                        }
                        if (v2.Current == CoroutineControl.sched)
                        {
                            break;
                        }
                        if (v2.Current == CoroutineControl.end)
                        {
                            break;
                        }
                    }
                    if (_runStatus == CoroutinesStatus.Aborting)
                    {
                        break;
                    }

                }
                if (_runStatus == CoroutinesStatus.Aborting)
                {
                    _runStatus = CoroutinesStatus.Aborted;
                    _isRuning = false;
                    break;
                }
                CoroutineItemsClear();
                CoroutineItemsSync(); 
            }
            if (this.cancellationToken.IsCancellationRequested)
            {
                _isRuning= false;
                _runStatus = CoroutinesStatus.Canceled;
            }

            
        }
         
    }
}
