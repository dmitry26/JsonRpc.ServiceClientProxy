using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Community.JsonRpc.ServiceClient.Benchmarks.Resources;
using Newtonsoft.Json.Linq;

namespace Community.JsonRpc.ServiceClient.Benchmarks.Internal
{
    internal sealed class JsonRpcClientBenchmarkHandler2 : HttpMessageHandler
    {
        private static readonly MediaTypeHeaderValue _mediaTypeHeaderValue = new MediaTypeHeaderValue("application/json");

        private readonly JObject _content;

        public JsonRpcClientBenchmarkHandler2(string resourceName)
        {
            _content = JObject.Parse(EmbeddedResourceManager.GetString(resourceName));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var httpResponseMessage = default(HttpResponseMessage);

            if (_content == null)
            {
                httpResponseMessage = new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NoContent
                };
            }
            else
            {
				var requestString = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
				var requestObject = JObject.Parse(requestString);
				_content["id"] = requestObject["id"];
				var contentBytes = Encoding.UTF8.GetBytes(_content.ToString());

				var httpContent = new ByteArrayContent(contentBytes);

                httpContent.Headers.ContentType = _mediaTypeHeaderValue;

                httpResponseMessage = new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = httpContent
                };
            }

            return httpResponseMessage;
        }
    }
}