using Mm.Tools.Coroutine;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace SocketDemo
{
    class testI {
        public int i = 0;
    }
    class Program
    {
        static void Main(string[] args)
        {
            Mm.Tools.Coroutine.Coroutines SSocketList = new Mm.Tools.Coroutine.Coroutines();
            SSocketList.SetKeep();
            SSocketList.target = new testI();
            System.Threading.Tasks.Task.Factory.StartNew(SSocketList.Start);


            System.Net.Sockets.TcpListener ttt = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Parse("0.0.0.0"), 8899);
             
            ttt.Start(1000000);

            System.Threading.Tasks.Task.Factory.StartNew(()=> {
                while (true)
                {
                    Console.WriteLine(((testI)SSocketList.target).i);
                    System.Threading.Thread.Sleep(1000);
                }
            });
            while (true)
            {
                
                    SSocketList.SetCoroutineItem(SocketListener,ttt.AcceptTcpClient());
                //Console.WriteLine("link");

            }

            

            Console.Read();
        }

        private static IEnumerator<CoroutineControl> SocketListener(CoroutineItem ci,object tc)
        {
            TcpClient Tc = tc as TcpClient;
            testI ji = ci.target as testI;
            ji.i++;
             

            while (true)
            {
                try
                {
                    Tc.Client.Send(System.Text.ASCIIEncoding.ASCII.GetBytes("abc"));
                    
                }
                catch (Exception e)
                {
                    break;
                }
                //ci.Sleep(1000);
                yield return CoroutineControl.sched;
                
            }
            Tc.Dispose();
            ji.i--;
               
            yield return CoroutineControl.end;
        }



    }
}
