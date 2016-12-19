using CsvHelper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ItemImportMultiThreaded
{
    class Program
    {
        const int WorkerCount = 16;
        const int BatchSize = 200;
        const int ProcessLimit = 5000;
        const string Url = "http://ec2-54-209-21-238.compute-1.amazonaws.com";

        static void Main(string[] args)
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 100;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            Console.WriteLine("Loading CSV to queue...");
            var queue = LoadItemsToQueue();
            int count = queue.Count;
            Console.WriteLine("Loading completed. Queue contains {0} items.", count);

            Console.WriteLine("Starting {0} worker threads", WorkerCount);

            //Note: i was previously using the Task Parallel Library but switched to pure threads to make sure the tasks are not simply pushed to the thread-pool.
            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < WorkerCount; i++)
            {
                var t = new Thread(() => ProcessQueue(queue, Url));
                t.Start();
                threads.Add(t);
            }

            foreach (var t in threads)
            {
                t.Join();
            }

            sw.Stop();

            //Initialization takes about 20 seconds which we substract from the time to get a more accurate measurement of orders per hour
            Console.WriteLine("All threads have completed. Total time elapsed: {0} seconds. Total processed: {1}. Items per hour: {2}", sw.Elapsed.TotalMinutes * 60, count, count / sw.Elapsed.TotalMinutes * 60);

            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }

        private static void ProcessQueue(ConcurrentQueue<Item> queue, string url)
        {
            Item item;
            var itemsToImport = new List<Item>();

            var importer = new ItemImporter();
            importer.Login(url, "admin", "admin", null);

            while (true)
            {
                if (queue.TryDequeue(out item))
                {
                    itemsToImport.Add(item);
                    if (itemsToImport.Count == BatchSize)
                    {
                        importer.Import(itemsToImport);
                        itemsToImport.Clear();
                    }
                }
                else
                {
                    if (itemsToImport.Count > 0)
                    {
                        importer.Import(itemsToImport);
                    }

                    Console.WriteLine("[{0}] Queue is now empty - we can stop.", System.Threading.Thread.CurrentThread.ManagedThreadId);
                    break;
                }
            }

            importer.Logout();
        }

        private static ConcurrentQueue<Item> LoadItemsToQueue()
        {
            var queue = new ConcurrentQueue<Item>();

            using (StreamReader reader = File.OpenText(Environment.GetCommandLineArgs()[1]))
            {
                var csv = new CsvReader(reader);
                csv.Configuration.IgnoreHeaderWhiteSpace = true;

                while (csv.Read())
                {
                    var item = csv.GetRecord<Item>();
                    queue.Enqueue(item);

                    if (ProcessLimit > 0 && queue.Count >= ProcessLimit) break;
                }
            }

            return queue;
        }
    }
}
