using System;
using System.Collections.Generic;
using Mm.Tools.Coroutine;
namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            //Create a new Coroutines Pool(Ones Thread);
            Coroutines cpool = new Coroutines();

            cpool.ExceptionOperator = ex =>
            {

            };
            Program p = new Program();
            Console.Title = "t1:" + System.Threading.Thread.CurrentThread.ManagedThreadId;
            cpool.SetCoroutineItem(p.MyTask);
            cpool.SetCoroutineItem(p.MyTask2);
             //cpool.Start(); 
            System.Threading.Tasks.Task.Factory.StartNew(cpool.Start);

            System.Threading.Thread.Sleep(1000);
             cpool.SetCoroutineItem(p.MyTask3);
         
            System.Threading.Thread.Sleep(3000);

           // cpool.Abort();

            Console.WriteLine(cpool.Status);
            System.Threading.Thread.Sleep(1000);
            Console.WriteLine(cpool.Status);

            Console.Read();
        }

        public IEnumerator<CoroutineControl> MyTask(CoroutineItem ci,object arg)
        {
            

            for (int i = 1; i < int.MaxValue; i++)
            {

                if (i%20000==1)
                {

                    Console.WriteLine("A:{0},thread{1}", i, System.Threading.Thread.CurrentThread.ManagedThreadId);
                    yield return CoroutineControl.sched;
                }
            }

            yield return CoroutineControl.end;

        }
        public IEnumerator<CoroutineControl> MyTask2(CoroutineItem ci,object arg)
        {
           
            for (int i = 1; i <  2000; i++)
            {
                if (i % 200 == 0)
                {
                    Console.WriteLine("B:{0},thread{1}", i/0, AppDomain.GetCurrentThreadId().ToString());
                   //ci.Sleep(10);
                    yield return CoroutineControl.sched;
                }
                

            }

            yield break;

        }
        public IEnumerator<CoroutineControl> MyTask3(CoroutineItem ci,object arg)
        {
            for (int i = 1; i < int.MaxValue; i++)
            {
                if (i % 30000 == 0)
                {

                    Console.WriteLine("C:{0},thread{1}", i, AppDomain.GetCurrentThreadId().ToString());
                    //ci.Sleep(10);
                    yield return CoroutineControl.sched;
                }

            }

            yield break;

        }
    }
}
