using System.Linq;
using Translumo.Translation.Llm;
using Xunit;

namespace Translumo.Tests.Llm
{
    public class LlmModelCatalogTests
    {
        [Fact]
        public void Every_non_custom_provider_has_non_empty_model_list()
        {
            foreach (LlmProvider provider in System.Enum.GetValues<LlmProvider>())
            {
                if (provider == LlmProvider.Custom) continue;
                Assert.True(LlmModelCatalog.Models.TryGetValue(provider, out var models));
                Assert.NotNull(models);
                Assert.NotEmpty(models);
            }
        }

        [Fact]
        public void Custom_provider_has_empty_model_list()
        {
            Assert.Empty(LlmModelCatalog.Models[LlmProvider.Custom]);
        }

        [Theory]
        [InlineData(LlmProvider.ChatGPT, "gpt-4.1-mini")]
        [InlineData(LlmProvider.Claude, "claude-sonnet-4-6")]
        [InlineData(LlmProvider.Gemini, "gemini-2.5-flash")]
        [InlineData(LlmProvider.DeepSeek, "deepseek-v4-flash")]
        [InlineData(LlmProvider.Kimi, "kimi-k3")]
        [InlineData(LlmProvider.Grok, "grok-3-mini")]
        [InlineData(LlmProvider.Ollama, "llama3")]
        public void First_model_matches_provider_default(LlmProvider provider, string expectedDefault)
        {
            var models = LlmModelCatalog.Models[provider];
            Assert.Equal(expectedDefault, models[0]);
        }

        [Fact]
        public void No_model_entry_is_null_or_whitespace()
        {
            foreach (var kvp in LlmModelCatalog.Models)
            {
                foreach (var model in kvp.Value)
                {
                    Assert.False(string.IsNullOrWhiteSpace(model), $"{kvp.Key} has an empty model entry");
                }
            }
        }

        [Fact]
        public void No_duplicate_models_within_a_provider()
        {
            foreach (var kvp in LlmModelCatalog.Models)
            {
                var distinct = kvp.Value.Distinct().Count();
                Assert.Equal(kvp.Value.Count, distinct);
            }
        }
    }
}
