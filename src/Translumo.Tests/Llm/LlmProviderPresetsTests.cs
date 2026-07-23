using System;
using System.Linq;
using Translumo.Translation.Llm;
using Xunit;

namespace Translumo.Tests.Llm
{
    public class LlmProviderPresetsTests
    {
        [Fact]
        public void All_providers_except_custom_have_valid_https_endpoint_and_model()
        {
            foreach (LlmProvider provider in Enum.GetValues<LlmProvider>())
            {
                var preset = LlmProviderPresets.Presets[provider];
                if (provider == LlmProvider.Custom)
                {
                    Assert.Equal(string.Empty, preset.DefaultEndpoint);
                    Assert.Equal(string.Empty, preset.DefaultModel);
                    continue;
                }

                Assert.False(string.IsNullOrWhiteSpace(preset.DefaultEndpoint), $"{provider} endpoint is empty");
                Assert.True(Uri.TryCreate(preset.DefaultEndpoint, UriKind.Absolute, out var uri),
                    $"{provider} endpoint is not a valid absolute URI: {preset.DefaultEndpoint}");
                // Ollama runs locally over plain http; every cloud provider must use https.
                if (provider == LlmProvider.Ollama)
                {
                    Assert.True(uri.Scheme == Uri.UriSchemeHttp, $"{provider} endpoint should be http (local)");
                }
                else
                {
                    Assert.True(uri.Scheme == Uri.UriSchemeHttps, $"{provider} endpoint is not https");
                }
                Assert.False(string.IsNullOrWhiteSpace(preset.DefaultModel), $"{provider} model is empty");
            }
        }

        [Theory]
        [InlineData(LlmProvider.ChatGPT, LlmApiStyle.OpenAi)]
        [InlineData(LlmProvider.DeepSeek, LlmApiStyle.OpenAi)]
        [InlineData(LlmProvider.Qwen, LlmApiStyle.OpenAi)]
        [InlineData(LlmProvider.Kimi, LlmApiStyle.OpenAi)]
        [InlineData(LlmProvider.GLM, LlmApiStyle.OpenAi)]
        [InlineData(LlmProvider.MiniMax, LlmApiStyle.OpenAi)]
        [InlineData(LlmProvider.Grok, LlmApiStyle.OpenAi)]
        [InlineData(LlmProvider.Ollama, LlmApiStyle.OpenAi)]
        [InlineData(LlmProvider.Claude, LlmApiStyle.Anthropic)]
        [InlineData(LlmProvider.Gemini, LlmApiStyle.Gemini)]
        public void Api_style_matches_expectation(LlmProvider provider, LlmApiStyle expected)
        {
            Assert.Equal(expected, LlmProviderPresets.Presets[provider].ApiStyle);
        }

        [Fact]
        public void MiniMax_endpoint_is_openai_compatible_chat_completions()
        {
            var endpoint = LlmProviderPresets.Presets[LlmProvider.MiniMax].DefaultEndpoint;
            Assert.Contains("/v1/chat/completions", endpoint);
        }

        [Fact]
        public void Gemini_endpoint_contains_model_placeholder()
        {
            var endpoint = LlmProviderPresets.Presets[LlmProvider.Gemini].DefaultEndpoint;
            Assert.Contains("{0}", endpoint);
        }
    }
}
