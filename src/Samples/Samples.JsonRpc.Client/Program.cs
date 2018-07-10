using System;
using System.Threading.Tasks;

namespace Samples.JsonRpc.Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
			try
			{
				await Examples.Run();
				Console.WriteLine("Done!");
			}
			catch (Exception x)
			{
				Console.WriteLine(x);
			}

			Console.ReadLine();
        }
    }
}
