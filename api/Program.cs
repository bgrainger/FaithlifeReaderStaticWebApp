using System;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FaithlifeReader.Functions
{
	public class Program
	{
		public static void Main()
		{
			var host = new HostBuilder()
				.ConfigureFunctionsWorkerDefaults()
				.ConfigureServices(services =>
				{
					services.AddSingleton(new CosmosClient(Environment.GetEnvironmentVariable("CosmosConnectionString"),
						new CosmosClientOptions()
						{
							SerializerOptions = new CosmosSerializationOptions()
							{
								PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
							}
						}));
				})
				.Build();

			host.Run();
		}
	}
}