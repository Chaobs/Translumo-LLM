using Translumo.Infrastructure.Constants;
using Xunit;

namespace Translumo.Tests.Infrastructure
{
    public class RegexStorageTests
    {
        [Fact]
        public void YandexSidRegex_extracts_lowercase_sid()
        {
            var body = "var params = {sid: 'AbC.123.XyZ', expire: 123};";
            var m = RegexStorage.YandexSidRegex.Match(body);
            Assert.True(m.Success);
            Assert.Equal("AbC.123.XyZ", m.Value);
        }

        [Fact]
        public void YandexSidRegex_is_case_insensitive_for_uppercase_SID()
        {
            var body = "var params = {SID: 'QwE.987.Zz'};";
            var m = RegexStorage.YandexSidRegex.Match(body);
            Assert.True(m.Success);
            Assert.Equal("QwE.987.Zz", m.Value);
        }

        [Fact]
        public void YandexSidRegex_returns_no_match_when_absent()
        {
            var m = RegexStorage.YandexSidRegex.Match("no sid here");
            Assert.False(m.Success);
        }

        [Fact]
        public void YandexSidRegex_handles_sid_with_separators()
        {
            var body = "sid: 'a.b.c.d'";
            var m = RegexStorage.YandexSidRegex.Match(body);
            Assert.True(m.Success);
            Assert.Equal("a.b.c.d", m.Value);
        }
    }
}
