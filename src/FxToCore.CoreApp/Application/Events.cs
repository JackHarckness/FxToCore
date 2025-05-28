#if FEATURE_ANSI_ESCAPE_SEQ
#define COLOR_USE_ANSI_ESCAPE_SEQ
#endif

#if FEATURE_ZLOGGER
using System.Buffers;
using Utf8StringInterpolation;
using ZLogger;
#endif
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FxToCore.Application
{
#if FEATURE_ZLOGGER
    using u8writer = Utf8StringWriter<IBufferWriter<byte>>;
#endif

    partial class Events
    {
#if FEATURE_ZLOGGER
        [ZLoggerMessage(
#else
        [LoggerMessage(
#endif
            LogLevel.Critical,
            "Unhandled error",
            EventId = 10000,
            SkipEnabledCheck = true)]
        public static partial void AppUnhandledError(ILogger logger, Exception e);

#if FEATURE_ZLOGGER
        [ZLoggerMessage(
#else
        [LoggerMessage(
#endif
            LogLevel.Error,
            "HTTP Error",
            EventId = 10101,
            SkipEnabledCheck = true)]
        public static partial void HttpException(ILogger logger, Exception? e);

#if FEATURE_ZLOGGER
        [ZLoggerMessage(
#else
        [LoggerMessage(
#endif
            LogLevel.Information,
            "{method} {path}",
            EventId = 10102,
            SkipEnabledCheck = true)]
        public static partial void HandlingRequest(ILogger logger, string method, string path);

#if FEATURE_ZLOGGER
        [ZLoggerMessage(
#else
        [LoggerMessage(
#endif
            LogLevel.Information,
            "Server started",
            EventId = 10103,
            SkipEnabledCheck = true)]
        public static partial void ServerStarted(ILogger logger);

#if FEATURE_ZLOGGER
        [ZLoggerMessage(
#else
        [LoggerMessage(
#endif
            LogLevel.Information,
            "Stopping server",
            EventId = 10104,
            SkipEnabledCheck = true)]
        public static partial void ServerStopping(ILogger logger);

        public static ILoggerFactory AllocLoggerFactory()
        {
            return LoggerFactory.Create(static builder =>
                builder
                .ClearProviders()
#if FEATURE_ZLOGGER
                .AddZLoggerConsole(static options =>
                {
                    options.UseFormatter(static () => new LogFormatter());
#if COLOR_USE_ANSI_ESCAPE_SEQ
                    options.ConfigureEnableAnsiEscapeCode = true;
#endif // COLOR_USE_ANSI_ESCAPE_SEQ
                })
#else // FEATURE_ZLOGGER
                .AddConsole()
#endif // FEATURE_ZLOGGER
#if FEATURE_ASPNETCORE
                .AddFilter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.Information)
#endif // FEATURE_ASPNETCORE
                .SetMinimumLevel(LogLevel.Information)
            );
        }

#if FEATURE_ZLOGGER
        class LogFormatter : IZLoggerFormatter
        {
            public bool WithLineBreak => true;

            public void FormatLogEntry(IBufferWriter<byte> writer, IZLoggerEntry entry)
            {
                LogInfo info = entry.LogInfo;

                LfWritePrefix(writer, in info);
                LfWriteEntry(writer, entry);
                LfWriteSuffix(writer, in info);

                Exception? e = info.Exception;
                if (e != null)
                {
                    LfWriteException(writer, info.Exception);
                }
            }

            static void LfWriteEntry(IBufferWriter<byte> bufferWriter, IZLoggerEntry entry)
            {
                entry.ToString(bufferWriter);
            }

            static void LfWritePrefix(IBufferWriter<byte> bufferWriter, ref readonly LogInfo info)
            {
                u8writer writer = new u8writer(bufferWriter);
                LfWriteTs(ref writer, info.Timestamp);
                writer.AppendUtf8(" ["u8);
                LfWriteLogLevel(ref writer, info.LogLevel);
                writer.AppendUtf8("] "u8);
                writer.Flush();
            }

            static void LfWriteTs(ref u8writer writer, Timestamp ts)
            {
                const string PBSZ_UTCDTZ_FMT = "dd/MM/yyyy HH:mm:ss";

                Span<byte> buffer = stackalloc byte[32];
#if DEBUG
                bool res =
#endif // DEBUG
                ts.Utc.TryFormat(buffer, out int chars, PBSZ_UTCDTZ_FMT);
#if DEBUG
                Debug.Assert(res, "Insufficient buffer for UTC");
#endif // DEBUG
                writer.AppendUtf8(buffer[..chars]);
            }

            static void LfWriteLogLevel(ref u8writer writer, LogLevel logLevel)
            {
#if COLOR_USE_ANSI_ESCAPE_SEQ
                u8string color = LfSelectConsoleColor(logLevel);
                if (!color.IsEmpty)
                {
                    writer.AppendUtf8(color);
                }
#endif // COLOR_USE_ANSI_ESCAPE_SEQ
                writer.AppendUtf8(logLevel switch
                {
                    LogLevel.Trace       => "TRACE"U8,
                    LogLevel.Debug       => "DBG"U8,
                    LogLevel.Information => "INF"U8,
                    LogLevel.Warning     => "WARN"U8,
                    LogLevel.Error       => "ERR"U8,
                    LogLevel.Critical    => "CRIT"U8,
                    LogLevel.None or _   => "NON"U8
                });
#if COLOR_USE_ANSI_ESCAPE_SEQ
                if (!color.IsEmpty)
                {
                    writer.AppendUtf8("\x1b[1;0m"u8);
                }
#endif // COLOR_USE_ANSI_ESCAPE_SEQ
            }

            static void LfWriteSuffix(IBufferWriter<byte> bufferWriter, ref readonly LogInfo info)
            {
                using u8writer writer = new u8writer(bufferWriter);
                writer.AppendUtf8(" ("u8);
                writer.AppendUtf8(info.Category.Utf8Span);
                writer.AppendUtf8(") "u8);
            }

            static void LfWriteException(IBufferWriter<byte> bufferWriter, Exception? e)
            {
                using u8writer writer = new u8writer(bufferWriter);
                writer.AppendFormatted(e);
            }

#if COLOR_USE_ANSI_ESCAPE_SEQ
            static u8string LfSelectConsoleColor(LogLevel level)
            {
                switch (level)
                {
                case LogLevel.Trace:
                    return "\x1b[37;46m"u8;

                case LogLevel.Debug:
                case LogLevel.Information:
                    return "\x1b[32m"u8;

                case LogLevel.Warning:
                    return "\x1b[33m"u8;

                case LogLevel.Error:
                    return "\x1b[91m"u8;

                case LogLevel.Critical:
                    return "\x1b[37;41m"u8;

                case LogLevel.None:
                default:
                    return u8string.Empty;
                }
            }
#endif // COLOR_USE_ANSI_ESCAPE_SEQ
        }
#endif // FEATURE_ZLOGGER
    }
}
