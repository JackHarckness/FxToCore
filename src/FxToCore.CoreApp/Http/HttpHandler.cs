using Microsoft.AspNetCore.Http;
using System.Net;

namespace FxToCore.Http
{
    class HttpHandler
    {
        public virtual Task HandleAsync(HttpContext context, CancellationToken token)
        {
            return Task.CompletedTask;
        }

        protected static async Task<int> ReadRequestBody(HttpContext context, Memory<byte> buffer, CancellationToken token)
        {
            int bytes, offset = 0;

            while (offset < buffer.Length && (bytes = await context.Request.Body.ReadAsync(buffer[offset..], token)) != 0)
                offset += bytes;

            return offset;
        }

        protected static void SetStatusCode(HttpContext context, HttpStatusCode statusCode)
        {
            context.Response.StatusCode = (int)statusCode;
        }

        protected static void SetContentType(HttpContext context, string contentType)
        {
            context.Response.ContentType = contentType;
        }
    }
}
