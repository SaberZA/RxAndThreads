using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace RxTut
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			// Passing outer threadId to inner thread

			new Thread(() =>
			{
				var outerThreadId = Thread.CurrentThread.ManagedThreadId;
				Console.WriteLine("[outer]outer thread: {0}", outerThreadId);

				ParameterizedThreadStart innerThread = async (obj) =>
				{
					var innerThreadId = Thread.CurrentThread.ManagedThreadId;
					await Task.Run(() =>
					{
						var inceptionThreadId = Thread.CurrentThread.ManagedThreadId;
						Console.WriteLine("[inception]outer thread: {0}", obj);
						Console.WriteLine("[inception]inner thread: {0}", innerThreadId);
						Console.WriteLine("[inception]inception thread: {0}", inceptionThreadId);
					});
				};

				new Thread(innerThread).Start(outerThreadId);
			}).Start();

			Console.WriteLine("Starting on threadId:{0}", Thread.CurrentThread.ManagedThreadId);
			var subject = new Subject<object>();
			subject.Subscribe(
				o => Console.WriteLine("Received {1} on threadId:{0}",
				Thread.CurrentThread.ManagedThreadId,
				o)
			);
			ParameterizedThreadStart notify = obj =>
			{
				Console.WriteLine("OnNext({1}) on threadId:{0}",
					Thread.CurrentThread.ManagedThreadId,
					obj);
				subject.OnNext(obj);
			};
			notify(1);
			new Thread(notify).Start(2);
			new Thread(notify).Start(3);
			Console.WriteLine("Starting on threadId:{0}", Thread.CurrentThread.ManagedThreadId);
			var source = Observable.Create<int>(
			o =>
			{
				Console.WriteLine("Invoked on threadId:{0}", Thread.CurrentThread.ManagedThreadId);
				o.OnNext(1);
				o.OnNext(2);
				o.OnNext(3);
				o.OnCompleted();
				Console.WriteLine("Finished on threadId:{0}",
					Thread.CurrentThread.ManagedThreadId);
				return Disposable.Empty;
			});
			source
			//.SubscribeOn(Scheduler.ThreadPool)
			.Subscribe(
			o => Console.WriteLine("Received {1} on threadId:{0}",
			Thread.CurrentThread.ManagedThreadId,
			o),
			() => Console.WriteLine("OnCompleted on threadId:{0}",
			Thread.CurrentThread.ManagedThreadId));
			Console.WriteLine("Subscribed on threadId:{0}", Thread.CurrentThread.ManagedThreadId);

			TransactionScopeTests();
			Console.ReadKey();
		}

		static void TransactionScopeTests()
		{
			using (var t1 = new TransactionScope(TransactionScopeOption.Required))
			{
				var thread1 = Thread.CurrentThread.ManagedThreadId;
				Console.WriteLine("[t1][{0}]", thread1);

				new Thread(() =>
				{
					var thread2 = Thread.CurrentThread.ManagedThreadId;
					Console.WriteLine("[t1][{0}]", thread2);

					using (var t2 = new TransactionScope(TransactionScopeOption.Required))
					{
						var thread3 = Thread.CurrentThread.ManagedThreadId;
						Console.WriteLine("[t2][{0}]", thread3);

					}
				}).Start();
			}

			using (var t1 = new TransactionScope(TransactionScopeOption.Required))
			{
				var thread1 = Thread.CurrentThread.ManagedThreadId;
				Console.WriteLine("[t1][{0}]", thread1);

				Parallel.ForEach(new[] { 1, 2, 3 }, (i) =>
				{
					var threadp = Thread.CurrentThread.ManagedThreadId;
					Console.WriteLine("2[t1][{0}][{1}]", thread1, threadp);
				});

				new Thread(() =>
				{
					var thread2 = Thread.CurrentThread.ManagedThreadId;
					Console.WriteLine("[t1][{0}]", thread2);

					using (var t2 = new TransactionScope(TransactionScopeOption.Required))
					{
						var thread3 = Thread.CurrentThread.ManagedThreadId;
						Console.WriteLine("[t2][{0}]", thread3);

					}
				}).Start();
			}
		}
}
}
