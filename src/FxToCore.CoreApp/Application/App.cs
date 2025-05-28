using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
namespace FxToCore.Application
{
    class App : IDisposable
    {
        const int
            E_SUCCESS         = 0,
            E_NOINIT          = 1,
            E_UNHANDLED_ERROR = short.MaxValue;

        CancellationTokenSource _stopSource = new CancellationTokenSource();
        protected int ExitCode = E_SUCCESS;
#nullable disable
        protected ILoggerFactory Loggers;
#nullable restore

        protected CancellationToken StopToken => _stopSource.Token;

#nullable disable
        protected ILogger AppLogger { get; private set; }
#nullable restore

        public void Dispose()
        {
            GC.WaitForPendingFinalizers();
        }

        public void Run(string[] args)
        {
            if (!AppInitialize())
            {
                ExitCode = E_NOINIT;
                return;
            }

            Task[] tasks = CreateTasks();

            try
            {
                Wait(tasks);
            }
            catch (Exception e)
            {
                Events.AppUnhandledError(AppLogger, e);
                ExitCode = E_UNHANDLED_ERROR;

                return;
            }

            Debug.Assert(StopToken.IsCancellationRequested, "App should have stopped at this point");
        }

        protected virtual bool AppInitialize()
        {
            AppInitializeLoggers();
            AppInitializeStopSource();
            return true;
        }

        protected virtual Task[] CreateTasks()
        {
            return [];
        }

        protected virtual void Wait(Task[] tasks)
        {
            Task.WaitAny(tasks);
        }

        void AppInitializeLoggers()
        {
            Debug.Assert(Loggers == null && AppLogger == null);

            Loggers = Events.AllocLoggerFactory();
            AppLogger = Loggers.CreateLogger("App");
        }

        void AppInitializeStopSource()
        {
            Console.CancelKeyPress += (sender, e) =>
            {
                _stopSource.Cancel();
                e.Cancel = true;
            };
        }
    }
}
