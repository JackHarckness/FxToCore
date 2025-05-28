using FxToCore.Application;
using FxToCore.Http;
using System.Net;

namespace FxToCore
{
    class FxToCoreApp : App
    {
        public static int Main(string[] args)
        {
            using FxToCoreApp app = new FxToCoreApp();
            app.Run(args);
            return app.ExitCode;
        }

        protected override Task[] CreateTasks()
        {
            HttpTextHandler handler = new HttpTextHandlerWithDynamicLoading(Loggers.CreateLogger("Text handler"));
            HttpServer listener = new HttpServer(Loggers, new IPEndPoint(IPAddress.Any, 9999), handler);

            return [listener.Listen(StopToken)];
        }
    }
}
