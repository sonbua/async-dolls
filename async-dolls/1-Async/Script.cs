﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows.Threading;
using NUnit.Framework;

namespace AsyncDolls
{
    /// <summary>
    /// Contains a lot of white space. Optimized for Consolas 14 pt 
    /// and Full HD resolution
    /// </summary>
    [TestFixture]
    public class AsyncScript
    {
        [Test]
        public void ThatsMe()
        {
            var daniel = new DanielMarbach();
            daniel
                .Is("CEO").Of("tracelight Gmbh").In("Switzerland")
                .and
                .WorkingFor("Particular Software").TheFolksBehind("NServiceBus")
                .Reach("@danielmarbach")
                .Reach("www.planetgeek.ch");
        }

        [Test]
        public async Task AsyncRecap()
        {
            var slide = new Slide(title: "Asynchronous vs. Parallel");
            await slide
                .Sample(async () =>
                {
                    // Parallel
                    Parallel.For(0, 1000, CpuBoundMethod); // or Parallel.ForEach
                    await Task.Run(() => CpuBoundMethod(10)); // or Task.Factory.StartNew(), if in doubt use Task.Run

                    // Asynchronous
                    await IoBoundMethod(".\\IoBoundMethod.txt"); // if true IOBound don't use Task.Run, StartNew
                });
        }

        static void CpuBoundMethod(int i)
        {
            Console.WriteLine(i);
        }

        static async Task IoBoundMethod(string path)
        {
            using (var stream = new FileStream(path, FileMode.OpenOrCreate))
            using (var writer = new StreamWriter(stream))
            {
                await writer.WriteLineAsync("Yehaa " + DateTime.Now);
                await writer.FlushAsync();
                writer.Close();
                stream.Close();
            }
        }

        [Test]
        public async Task AsyncVoid()
        {
            var slide = new Slide(title: "Best Practices: async Task over async void");
            await slide
                .Sample(async () =>
                {
                    try
                    {
                        AvoidAsyncVoid();
                    }
                    catch (InvalidOperationException e)
                    {
                        // where is the exception?
                        Console.WriteLine(e);
                    }
                    await Task.Delay(100);
                });
        }

        static async void AvoidAsyncVoid() // Fire & Forget, can't be awaited, exception: EventHandlers
        {
            Console.WriteLine("Going inside async void.");
            await Task.Delay(10);
            Console.WriteLine("Going to throw soon");
            throw new InvalidOperationException("Gotcha!");
        }

        [Test]
        public async Task ConfigureAwait()
        {
            var slide = new Slide(title: "Best Practices: ConfigureAwait(false)");
            // ReSharper disable once PossibleNullReferenceException
            await Process.Start(new ProcessStartInfo(@".\configureawait.exe") { UseShellExecute = false });
        }

        [Test]
        public async Task DontMixBlockingAndAsync()
        {
            var slide = new Slide(title: "Best Practices: Don't mix blocking code with async. Async all the way!");
            await slide
                .Sample(() =>
                {
                    SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext()); // Let's simulate wpf stuff

                    Delay(15); // what happens here? How can we fix this?
                });
        }

        static void Delay(int milliseconds)
        {
            DelayAsync(milliseconds).Wait(); // Similar evilness is Thread.Sleep, Semaphore.Wait..
        }

        static Task DelayAsync(int milliseconds)
        {
            return Task.Delay(milliseconds);
        }

        [Test]
        public async Task ACompleteExampleMixingAsynchronousAndParallelProcessing()
        {
            var runningTasks = new ConcurrentDictionary<Task, Task>();
            var semaphore = new SemaphoreSlim(1);
            var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var token = tokenSource.Token;
            var scheduler = new QueuedTaskScheduler(TaskScheduler.Default, 2);

            try
            {
                var pumpTask = Task.Factory.StartNew(async () =>
                {
                    int taskNumber = 0;
                    while (!token.IsCancellationRequested)
                    {
                        await semaphore.WaitAsync(token);

                        var task = Task.Factory.StartNew(async () =>
                        {
                            int nr = Interlocked.Increment(ref taskNumber);

                            Console.WriteLine("Kick off " + nr + " " + Thread.CurrentThread.ManagedThreadId);
                            await Task.Delay(1000).ConfigureAwait(false);
                            Console.WriteLine(" back " + nr + " " + Thread.CurrentThread.ManagedThreadId);

                            semaphore.Release();
                        }, CancellationToken.None, TaskCreationOptions.HideScheduler, scheduler)
                        .Unwrap();

                        task.ContinueWith(t =>
                        {
                            Task wayne;
                            runningTasks.TryRemove(t, out wayne);
                        }, TaskContinuationOptions.ExecuteSynchronously).Ignore();

                        runningTasks.AddOrUpdate(task, task, (k, v) => task).Ignore();
                    }
                }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default)
                .Unwrap();

                await pumpTask;
            }
            catch (OperationCanceledException)
            {
            }

            await Task.WhenAll(runningTasks.Values);
        }
    }

    static class TaskExtensions
    {
        public static void Ignore(this Task task)
        {
        }
    }

    public static class ProcessExtensions
    {
        public static TaskAwaiter<int> GetAwaiter(this Process process)
        {
            var tcs = new TaskCompletionSource<int>();
            process.EnableRaisingEvents = true;
            process.Exited += (s, e) => tcs.TrySetResult(process.ExitCode);
            if (process.HasExited) tcs.TrySetResult(process.ExitCode);
            return tcs.Task.GetAwaiter();
        }
    }
}