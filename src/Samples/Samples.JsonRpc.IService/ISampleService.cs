using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Community.AspNetCore.JsonRpc;

namespace Samples.JsonRpcServer
{
    public interface ISampleService
	{
		[JsonRpcName("m1")]
		Task<double> Method1([JsonRpcName("p1")] long p1,[JsonRpcName("p2")] long p2);

		[JsonRpcName("m2")]
		Task<long> Method2(long p1,long p2);

		[JsonRpcName("sum")]
		Task<int> Sum(int p1,int p2);

		[JsonRpcName("m3")]
		Task<bool> Method3();

		[JsonRpcName("m4")]
		Task Method4();

		[JsonRpcName("m5")]
		Task Method5(string s);
	}
}
