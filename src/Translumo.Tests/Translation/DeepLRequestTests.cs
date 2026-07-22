using System.Text.Json;
using Translumo.Translation.Deepl;
using Xunit;

namespace Translumo.Tests.Translation
{
    public class DeepLRequestTests
    {
        [Fact]
        public void Constructor_builds_request_with_target_language()
        {
            var req = new DeepLRequest.DeepLTranslatorRequest(1, "Hello world.", "EN", "ZH", null);
            Assert.Equal("ZH", req.Params.Lang.TargetLang);
            Assert.Equal("EN", req.Params.Lang.SourceLangComputed);
            Assert.NotEmpty(req.Params.Jobs);
        }

        [Fact]
        public void ToJsonString_produces_valid_json()
        {
            var req = new DeepLRequest.DeepLTranslatorRequest(2, "Test.", "EN", "DE", null);
            var json = req.ToJsonString();
            Assert.NotNull(json);

            using var doc = JsonDocument.Parse(json);
            Assert.Equal("2.0", doc.RootElement.GetProperty("jsonrpc").GetString());
            Assert.Equal("LMT_handle_jobs", doc.RootElement.GetProperty("method").GetString());
        }

        [Fact]
        public void Empty_sentence_still_builds()
        {
            var req = new DeepLRequest.DeepLTranslatorRequest(3, "", "EN", "ZH", null);
            Assert.NotNull(req.Params);
        }
    }
}
