// Copyright (c) DMO Consulting LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Community.JsonRpc.ServiceClient;
using System.Diagnostics;
using Samples.JsonRpcServer;
using System.Threading;
using Community.AspNetCore.JsonRpc;
using Dmo.Threading;
using System.Linq;

namespace Samples.JsonRpc.Client
{
	public class Examples
	{		
		public static async Task Run()
		{
			var x = new Examples();
			await x.Example1().ConfigureAwait(false);
			await x.Example2().ConfigureAwait(false);
			await x.Example3().ConfigureAwait(false);
			await x.Example4().ConfigureAwait(false);
		}
		
		public class KeyUsage
		{
			[JsonProperty("bitsLeft")]
			public long BitsLeft { get; set; }

			public long TotalBits { get; set; }
		}

		public class RandomResult
		{
			[JsonProperty("random")]
			public RandomData Random {get;set;}

			public struct RandomData
			{
				[JsonProperty("data")]
				public int[] Data { get; set; }

				[JsonProperty("completionTime")]
				public DateTime CompletionTime { get; set; }
			}

			[JsonProperty("bitsLeft")]
			public long BitsLeft { get; set; }

			[JsonProperty("totalBits")]
			public long TotalBits { get; set; }

			[JsonProperty("requestsLeft")]
			public long RequestsLeft { get; set; }

			[JsonProperty("advisoryDelay")]
			public int AdvisoryDelay { get; set; }
		}

		public interface IRandomApi : IDisposable
		{			
			[JsonRpcName("getUsage")]
			Task<KeyUsage> GetUsage(string apiKey);

			[JsonRpcName("generateIntegers")]
			Task<RandomResult> GenerateIntegers(string apiKey,int n,int min,int max,bool replacement = true,int @base = 10);
		}

		private const string RandomServerUri = "https://api.random.org/json-rpc/2/invoke";

		private async Task Example1()
		{
			var parameters = new Dictionary<string,object>
			{
				["apiKey"] = "00000000-0000-0000-0000-000000000000"
			};

			using (var client = new JsonRpcClient(RandomServerUri))
			{
				var result = await client.InvokeAsync<KeyUsage>("getUsage",parameters).ConfigureAwait(false);
				Console.WriteLine($"getUsage, BitsLeft = {result.BitsLeft}");
			}

			using (var svc = new JsonRpcClient(RandomServerUri).AsServiceContract<IRandomApi>())
			{
				var res = await svc.GetUsage("00000000-0000-0000-0000-000000000000").ConfigureAwait(false);
				Console.WriteLine($"GetUsage, BitsLeft = {res.BitsLeft}");

				var res2 = await svc.GenerateIntegers("00000000-0000-0000-0000-000000000000",3,-100,200).ConfigureAwait(false);
				Console.WriteLine($"Received random numbers: {String.Join(", ",res2?.Random.Data?.Take(10) ?? new int[0])}");	
			}
		}

		private const string SampleServerUri = "http://localhost:5025/api";

		private async Task Example2()
		{
			using (var client = new JsonRpcClient(SampleServerUri))
			{
				var parameters = new Dictionary<string,object>
				{
					["p1"] = 10,
					["p2"] = (long)4
				};

				var result = await client.InvokeAsync<double>("m1",parameters).ConfigureAwait(false);
				Console.WriteLine($"result = {result}");

				var result2 = await client.InvokeAsync<long>("m2",new object[] { 10,4 }).ConfigureAwait(false);
				Console.WriteLine($"result = {result2}");
			}
		}

		private async Task Example3(CancellationToken cancelToken = default)
		{
			using (var svc = new JsonRpcClient(SampleServerUri).AsServiceContract<ISampleService2>())
			{
				try
				{
					var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
					var res1 = await svc.Method3(cts.Token).CancelAfter(5000,cts).ConfigureAwait(false);
				}				
				catch (TimeoutException)
				{
					Console.WriteLine("Operation timed out...");
				}
				catch (JsonRpcRequestException x)
				{
					Console.WriteLine($"StatusCode = {x.StatusCode}, details = {x.ToString()}");
				}
				catch (Exception x)
				{
					Console.WriteLine(x.Message);
				}

				await svc.Method4().ConfigureAwait(false);
				await svc.Method5("Test123").ConfigureAwait(false);
			}
		}

		private async Task Example4()
		{
			Console.WriteLine("Sum, InvokeAsync:");

			using (var client = new JsonRpcClient(SampleServerUri))
			{
				var sw = new Stopwatch();
				var ran = new Random((int)DateTime.Now.Ticks);

				for (int i = 0; i < 10; ++i)
				{
					var x = ran.Next(0,100);
					var y = ran.Next(-100,100);
					sw.Restart();
					var res = await client.InvokeAsync<int>("sum",new object[] { x,y }).ConfigureAwait(false);
					Console.WriteLine($"x = {x}, y = {y}, res = {res}, duration = {sw.Elapsed.TotalMilliseconds}ms");
				}
			}

			Console.WriteLine("Sum, service method");

			using (var svc = new JsonRpcClient(SampleServerUri).AsServiceContract<ISampleServiceDisposable>())
			{				
				var sw = new Stopwatch();
				var ran = new Random((int)DateTime.Now.Ticks);

				for (int i = 0; i < 10; ++i)
				{
					var x = ran.Next(0,100);
					var y = ran.Next(-100,100);
					sw.Restart();
					var res = await svc.Sum(x,y).ConfigureAwait(false);
					Console.WriteLine($"x = {x}, y = {y}, res = {res}, duration = {sw.Elapsed.TotalMilliseconds}ms");
				}
			}
		}		

		public interface ISampleServiceDisposable : ISampleService, IDisposable
		{
		}

		//same contract defined locally with CancellationToken
		public interface ISampleService2 : IDisposable
		{
			[JsonRpcName("m1")]
			Task<double> Method1([JsonRpcName("p1")] long p1,[JsonRpcName("p2")] long p2);

			[JsonRpcName("m2")]
			Task<long> Method2(long p1,long p2);

			[JsonRpcName("sum")]
			Task<int> Sum(int p1,int p2);

			[JsonRpcName("m3")]
			Task<bool> Method3(CancellationToken cacncelToken = default);

			[JsonRpcName("m4")]
			Task Method4();

			[JsonRpcName("m5")]
			Task Method5(string s);
		}
	}	
}
