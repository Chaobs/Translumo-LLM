using System.Linq;
using System.Reflection;
using System.Text.Json;
using Translumo.Translation.Llm;
using Xunit;

namespace Translumo.Tests.Llm
{
    /// <summary>
    /// White-box tests for the (private) request builders in LlmTranslator. They validate that the
    /// JSON payload structure matches each vendor's official API (OpenAI chat/completions,
    /// Anthropic messages, Gemini generateContent).
    /// </summary>
    public class LlmTranslatorRequestTests
    {
        private static string InvokeBuild(string method, LlmConfiguration cfg, string system, string user)
        {
            var m = typeof(LlmTranslator).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(m);
            return (string)m!.Invoke(null, new object[] { cfg, system, user })!;
        }

        [Fact]
        public void OpenAi_request_has_messages_with_system_role()
        {
            var cfg = new LlmConfiguration { Provider = LlmProvider.ChatGPT };
            var json = InvokeBuild("BuildOpenAiRequest", cfg, "SYS", "USER");

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            Assert.Equal("gpt-4.1-mini", root.GetProperty("model").GetString());

            var messages = root.GetProperty("messages").EnumerateArray().ToArray();
            Assert.Equal(2, messages.Length);
            Assert.Equal("system", messages[0].GetProperty("role").GetString());
            Assert.Equal("SYS", messages[0].GetProperty("content").GetString());
            Assert.Equal("user", messages[1].GetProperty("role").GetString());
            Assert.Equal("USER", messages[1].GetProperty("content").GetString());
            Assert.True(root.TryGetProperty("temperature", out _));
            Assert.True(root.TryGetProperty("max_tokens", out _));
        }

        [Fact]
        public void Anthropic_request_has_top_level_system_and_content_blocks()
        {
            var cfg = new LlmConfiguration { Provider = LlmProvider.Claude };
            var json = InvokeBuild("BuildAnthropicRequest", cfg, "SYS", "USER");

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            Assert.Equal("claude-sonnet-4-6", root.GetProperty("model").GetString());
            Assert.Equal("SYS", root.GetProperty("system").GetString());

            var messages = root.GetProperty("messages").EnumerateArray().ToArray();
            var content = messages[0].GetProperty("content").EnumerateArray().ToArray();
            Assert.Equal("text", content[0].GetProperty("type").GetString());
            Assert.Equal("USER", content[0].GetProperty("text").GetString());
        }

        [Fact]
        public void Gemini_request_has_contents_systemInstruction_generationConfig()
        {
            var cfg = new LlmConfiguration { Provider = LlmProvider.Gemini };
            var json = InvokeBuild("BuildGeminiRequest", cfg, "SYS", "USER");

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            Assert.True(root.TryGetProperty("contents", out _));
            Assert.Equal("SYS",
                root.GetProperty("systemInstruction").GetProperty("parts").EnumerateArray().First()
                    .GetProperty("text").GetString());

            var gen = root.GetProperty("generationConfig");
            Assert.True(gen.TryGetProperty("maxOutputTokens", out _));
        }
    }
}
