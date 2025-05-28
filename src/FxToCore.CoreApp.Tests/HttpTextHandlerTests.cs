using FxToCore.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace FxToCore.CoreApp.Tests
{
    [TestFixture]
    class HttpTextHandlerTests() : HttpTextHandlerWithDynamicLoading(new NullLoggerFactory().CreateLogger("Null"))
    {
        [Test]
        [TestCase("abcdef", "fedcba")]
        [TestCase("1234", "4321")]
        public void TestStringReverse_BehavesAsExpected(string original, string expected)
        {
            Span<byte> buffer = stackalloc byte[1024];

            int chars = Encoding.UTF8.GetBytes(original, buffer);

            ReverseString(buffer[..chars]);

            string reversed = Encoding.UTF8.GetString(buffer[..chars]);

            Assert.That(expected, Is.EqualTo(reversed));
        }
    }
}
