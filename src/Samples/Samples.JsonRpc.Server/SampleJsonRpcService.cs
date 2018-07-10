using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Community.AspNetCore.JsonRpc;

namespace Samples.JsonRpcServer
{
	public class SampleJsonRpcService : ISampleService, IJsonRpcService
	{		
		public Task<double> Method1(long p1,long p2) =>					
			Task.FromResult((double)p1 / ((p2 != 0) ? p2 : throw new JsonRpcServiceException(100L,"p2 can't be 0")));		
		
		public Task<long> Method2(long p1,long p2) =>	
			Task.FromResult(p1 + p2);		

		public Task<int> Sum(int p1,int p2) =>					
			Task.FromResult(p1 + p2);		

		public async Task<bool> Method3()
		{
			await Task.Delay(TimeSpan.FromSeconds(15)).ConfigureAwait(false);			
			return true;
		}

		public Task Method4() =>	
			Task.CompletedTask;		

		public Task Method5(string s) =>		
			Task.CompletedTask;		
	}
}
