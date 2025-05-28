using FxToCore.Application;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Net.HttpStatusCode;

namespace FxToCore.Http
{
    using unsafe ReverseStringCallback = delegate* unmanaged[Cdecl]<byte*, int>;

    unsafe class HttpTextHandlerWithDynamicLoading : HttpTextHandler
    {
        nint _module;
        ReverseStringCallback _fp;

        public HttpTextHandlerWithDynamicLoading(ILogger logger)
            : base(logger)
        {
            _module = NativeLibrary.Load("libcpp.so");
            _fp = (ReverseStringCallback)NativeLibrary.GetExport(_module, "reverse_str");
        }

        ~HttpTextHandlerWithDynamicLoading()
        {
            NativeLibrary.Free(_module);
        }

        [SkipLocalsInit]
        protected override void ReverseString(Span<byte> buffer)
        {
            const int TEXT_BUFFER_SIZE = 1024;

            Debug.Assert(_fp != null);
            Debug.Assert(buffer.Length < TEXT_BUFFER_SIZE);

            byte* str = stackalloc byte[TEXT_BUFFER_SIZE];

            // Why copy instead of taking an address of buffer directly? Because buffer MAY be stored
            // in a GC heap and, in current implementation, actually is. Copying buffer contents back
            // and forth raises the restriction of a 'fixed' context and may be faster sometimes since
            // buffer is expected to be small (no more than TEXT_BUFFER_SIZE).
            MoveMemory(buffer, str);
            NullTerminate(str, buffer.Length);

            ReverseStr(str);

            MoveMemory(str, buffer);
        }

        void ReverseStr(byte* pbszStr)
        {
            if (_fp(pbszStr) != 0)
            {
                throw new InvalidOperationException();
            }
        }

        static void NullTerminate(byte* str, int len)
        {
            str[len] = (byte)'\0';
        }

        static void MoveMemory(u8string src, byte* dst)
        {
            src.CopyTo(WrapStrNoAlloc(dst, src.Length));
        }

        static void MoveMemory(byte* src, Span<byte> dst)
        {
            WrapStrNoAlloc(src, dst.Length).CopyTo(dst);
        }

        static Span<byte> WrapStrNoAlloc(byte* str, int len)
        {
            return MemoryMarshal.CreateSpan(ref Unsafe.AsRef<byte>(str), len);
        }
    }

    class HttpTextHandler(ILogger logger) : HttpHandler
    {
        ILogger _logger = logger;

        public override async Task HandleAsync(HttpContext context, CancellationToken token)
        {
            byte[] buffer;
            int chars;

            if (context.Request.Path != "/")
            {
                SetStatusCode(context, NotFound);
                return;
            }

            if (context.Request.Method != HttpMethod.Get.Method)
            {
                SetStatusCode(context, MethodNotAllowed);
                return;
            }

            buffer = ArrayPool<byte>.Shared.Rent(1024);

            try
            {
                chars = await ReadRequestBody(context, buffer, token);

                if (chars == buffer.Length)
                {
                    SetStatusCode(context, RequestEntityTooLarge);
                    return;
                }

                ReverseString(buffer.AsSpan(0, chars));

                SetStatusCode(context, OK);
                SetContentType(context, "text/plain");

                await context.Response.Body.WriteAsync(buffer.AsMemory(0, chars), token);
            }
            catch (Exception e)
            {
                SetStatusCode(context, InternalServerError);
                Events.HttpException(_logger, e);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        protected virtual void ReverseString(Span<byte> buffer)
        {
            buffer.Reverse();
        }
    }
}
