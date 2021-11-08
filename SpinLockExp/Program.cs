using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpinLockExp
{
    class Program
    {
        public static void SpinLockSample1()
        {
            var sl = new SpinLock();
            var sb = new StringBuilder();
            Action action = () =>
              {
                bool judge=Thread.CurrentThread.IsThreadPoolThread;
                Console.WriteLine($"Is thread pool? {judge}\nThreadID is {Thread.CurrentThread.ManagedThreadId}");
                  bool gotLock = false;
                  for(int i=0;i<10000;i++)
                  {
                      //Set false
                      try
                      {
                          sl.Enter(ref gotLock);
                          sb.Append((i % 10).ToString());
                      }
                      finally
                      {
                          if (gotLock)
                          {
                              sl.Exit();
                              gotLock = false;
                          }
                          else { }
                      }
                  }
              };
            Parallel.Invoke(action, action, action);
            Console.WriteLine($"sb.Length=={sb.Length}");
            Console.WriteLine($"Number of occurrences of '5' in sb: {(sb.ToString().Where(c=> {return c == '5'; })).Count()} (should be 3000)");
        }
        public static void SpinLockSample2()
        {
            var sl = new SpinLock(true);

            //Inter Exclusive
            ManualResetEventSlim mre1 = new ManualResetEventSlim(false);
            ManualResetEventSlim mre2 = new ManualResetEventSlim(false);
            bool lockTaken = false;
            Task taskA = new Task(
                () =>
                {
                    try
                    {
                        sl.Enter(ref lockTaken);
                        Console.WriteLine($"taskA entered Spinlock");
                        //Active taskB
                        mre1.Set();

                        //Wait for taskB's signal
                        mre2.Wait();
                    }
                    finally
                    {
                        if(lockTaken)
                        {
                            sl.Exit();
                            lockTaken = false;
                        }
                    };
                }
                );
            taskA.Start();

            Task taskB = new Task(() =>
            {
                //Wait for taskA to signal me
                mre1.Wait();
                Console.WriteLine($"TaskB: sl.IsHeld={sl.IsHeld} (should be ture, it is held by taskA)");
                Console.WriteLine($"TaskB: sl.IsHeldByCurrentThread={sl.IsHeldByCurrentThread} (should be false because it is held by taskA)");

                //test for this
                Console.WriteLine($"TaskB: sl.IsThreadOwnerTrackingEnabled={sl.IsThreadOwnerTrackingEnabled} (should be true?)");
                try
                {
                    Console.WriteLine($"TaskB: Release sl...");
                    sl.Exit();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{e.Message}");
                }
                finally
                {
                    mre2.Set();
                }
            });
            taskB.Start();
            Task.WaitAll(taskA, taskB);
            mre1.Dispose();
            mre2.Dispose();
        }
        public static void SpinLockSample3()
        {
            SpinLock sl = new SpinLock(false);
            ref SpinLock slRef = ref sl;
            ManualResetEventSlim mres = new ManualResetEventSlim(false);
            Console.WriteLine($"Main thread ID is {Thread.CurrentThread.ManagedThreadId}");

            bool lockTaken = false;

            //Main thread gets the spinlock
            slRef.Enter(ref lockTaken);

            //JS-style name
            Task worker = new Task(()=>
            {
                bool judge=Thread.CurrentThread.IsThreadPoolThread;
                Console.WriteLine($"Is thread pool? {judge}");
                Console.WriteLine($"Work thread is {Thread.CurrentThread.ManagedThreadId}");
                try
                {
                    sl.Exit();
                    Console.WriteLine($"worker task: successfully exited spinlock, as expected");
                }catch(Exception e)
                {
                    Console.WriteLine($"worker task: unexpected failure in exiting spinlock: {e.Message}");
                }
                finally
                {

                }
                Thread.Sleep(300);
                Console.WriteLine($"worker task: Active mres...");
                mres.Set();
                Thread.Sleep(3500);
                Console.WriteLine($"worker task: Completed.");
            });
            //Start another thread.
            worker.Start();

            Thread.Sleep(300);
            //Block the main thread.
            Console.WriteLine($"Block the main thread...");
            mres.Wait();
            Console.WriteLine($"Block of main threa ends");
            Console.WriteLine($"Wait for worker to end...");
            worker.Wait();
            Console.WriteLine($"work waitting ends");
            mres.Dispose();
        }
        static void Main(string[] args)
        {
            //SpinLockSample1();
            //SpinLockSample2();
            SpinLockSample3();
        }
    }
}
