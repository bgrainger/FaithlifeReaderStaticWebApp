using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FaithlifeReader.Functions
{
	public static class SignIn
	{
		[Function("SignIn")]
		public static HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
			FunctionContext executionContext)
		{
			var logger = executionContext.GetLogger("SignIn");
			logger.LogInformation("C# HTTP trigger function processed a request.");

			var response = req.CreateResponse(HttpStatusCode.OK);
			response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

			response.WriteString("Welcome to Azure Functions!");

			return response;
		}
	}
}
