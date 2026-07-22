using Translumo.Translation.Yandex;
using Xunit;

namespace Translumo.Tests.Translation
{
    public class YandexReaderProxyTests
    {
        [Fact]
        public void ExtractSid_returns_sid_from_body()
        {
            var proxy = new YandexReaderProxy();
            var body = "var params = {sid: 'AbC.123.XyZ', expire: 123};";
            Assert.Equal("AbC.123.XyZ", proxy.ExtractSid(body));
        }

        [Fact]
        public void ExtractSid_returns_empty_for_null_or_whitespace()
        {
            var proxy = new YandexReaderProxy();
            Assert.Equal(string.Empty, proxy.ExtractSid(null));
            Assert.Equal(string.Empty, proxy.ExtractSid(string.Empty));
            Assert.Equal(string.Empty, proxy.ExtractSid("   "));
        }

        [Fact]
        public void ExtractSid_returns_empty_when_no_sid()
        {
            var proxy = new YandexReaderProxy();
            Assert.Equal(string.Empty, proxy.ExtractSid("plain html without sid"));
        }
    }
}
