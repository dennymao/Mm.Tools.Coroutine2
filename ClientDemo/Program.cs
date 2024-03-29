﻿using Mm.Tools.Coroutine;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace ClientDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Mm.Tools.Coroutine.Coroutines SSocketList = new Mm.Tools.Coroutine.Coroutines();
            SSocketList.SetKeep();
            System.Threading.Tasks.Task.Factory.StartNew(SSocketList.Start);


            //System.Net.Sockets.TcpListener ttt = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Parse("0.0.0.0"), 8899);

            // ttt.Start(100);

            for(int i=0;i<500 ;i++)
            {

                SSocketList.SetCoroutineItem(SocketLink, i);
                if(i%1000==0)
                    Console.WriteLine("link{0}",i);

            }

            Console.Read();
        }

      
        private static IEnumerator<CoroutineControl> SocketLink(CoroutineItem ci, object I)
        { 
            TcpClient Tc = new TcpClient();
            byte[] buf = new byte[1024];
            
            Tc.Connect("127.0.0.1", 8899);

            var sa = new SocketAsyncEventArgs();
            sa.SetBuffer(buf, 0, 1024);
            sa.Completed += (object sender, SocketAsyncEventArgs e) =>
            {

            };
           
                yield return CoroutineControl.sched;
            while (!Tc.Connected)
            {
                Console.WriteLine($"wait{I}");
                yield return CoroutineControl.sched;
            }
                if (Tc.Connected)
                {
                    for (int i = 0; i < 1024;)
                    {

                        bool j =   Tc.Client.ReceiveAsync(sa);
                        if (j)
                        { 
                            i += sa.Buffer.Length;

                            Console.WriteLine("link{0} -》{1}", I, System.Text.ASCIIEncoding.ASCII.GetString(buf, 0, sa.Buffer.Length));
                        }
                        
                        yield return CoroutineControl.sched;
                    }
                }
                 
            yield return CoroutineControl.sched;
            Console.WriteLine("END {0} -》{1}", I, System.Text.ASCIIEncoding.ASCII.GetString(buf));
            Tc.Close();

            yield return CoroutineControl.end;
        }
    }
}
