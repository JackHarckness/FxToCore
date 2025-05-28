using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FxToCore.Application;
using System.Net;
using System.Runtime.CompilerServices;

namespace FxToCore.Http
{
    class HttpServer
    {
        KestrelServer _server;
        ILogger _logger;
        HttpHandler _handler;

        public HttpServer(ILoggerFactory loggers, IPEndPoint endpoint, HttpHandler handler)
        {
            _logger = loggers.CreateLogger("Http");
            _handler = handler;
            _server = HttpAllocKestrelServer(endpoint, loggers);
        }

        public async Task Listen(CancellationToken token)
        {
            await _server.StartAsync(new ServerApplication(this), token);

            Events.ServerStarted(_logger);

            TaskCompletionSource promise = new TaskCompletionSource();
            token.Register(
                static (promise, token) => Unsafe.As<TaskCompletionSource>(promise)!.SetCanceled(token),
                promise
            );

            try
            {
                await promise.Task;
            }
            catch (OperationCanceledException)
            {
                Events.ServerStopping(_logger);
            }
            finally
            {
                await _server.StopAsync(CancellationToken.None);
            }
        }

        protected virtual HttpContext AllocContext(IFeatureCollection features)
        {
            return new DefaultHttpContext(features);
        }

        protected virtual Task HandleRequest(HttpContext context, CancellationToken token)
        {
            Events.HandlingRequest(_logger, context.Request.Method, context.Request.Path.Value ?? string.Empty);
            return _handler.HandleAsync(context, token);
        }

        protected virtual void FreeContext(HttpContext context, Exception? e)
        {
            if (e != null)
            {
                Events.HttpException(_logger, e);
            }
        }

        static KestrelServer HttpAllocKestrelServer(IPEndPoint endpoint, ILoggerFactory loggers)
        {
            KestrelServerOptions options = new KestrelServerOptions()
            {
                AddServerHeader = false,
                AllowAlternateSchemes = true,
                AllowResponseHeaderCompression = false,
                AllowSynchronousIO = true
            };

            options.Listen(endpoint);

            options.Limits.MaxRequestBodySize = 4096; // No more than 4.0 kB can be send within the request
            options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(60); // Do not send FIN before at least one minute

            SocketTransportFactory factory = HttpAllocTransportFactory(loggers);

            return new KestrelServer(Options(options), factory, loggers);
        }

        static SocketTransportFactory HttpAllocTransportFactory(ILoggerFactory loggers)
        {
            SocketTransportOptions options = new SocketTransportOptions()
            {
                NoDelay = true,
                MaxWriteBufferSize = 1024,
                UnsafePreferInlineScheduling = true,
                WaitForDataBeforeAllocatingBuffer = true,
                Backlog = 16
            };

            return new SocketTransportFactory(Options(options), loggers);
        }

        static OptionsWrapper<TOptions> Options<TOptions>(TOptions options) where TOptions : class, new()
        {
            return new OptionsWrapper<TOptions>(options);
        }

        class ServerApplication : IHttpApplication<HttpContext>
        {
            HttpServer _server;

            public ServerApplication(HttpServer server)
            {
                _server = server;
            }

            public HttpContext CreateContext(IFeatureCollection features)
            {
                return _server.AllocContext(features);
            }

            public void DisposeContext(HttpContext context, Exception? exception)
            {
                _server.FreeContext(context, exception);
            }

            public Task ProcessRequestAsync(HttpContext context)
            {
                return _server.HandleRequest(context, CancellationToken.None);
            }
        }
    }
}
