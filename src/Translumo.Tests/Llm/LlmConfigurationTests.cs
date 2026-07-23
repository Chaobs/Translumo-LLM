using System.Text.Json;
using Translumo.Translation.Llm;
using Xunit;

namespace Translumo.Tests.Llm
{
    public class LlmConfigurationTests
    {
        [Fact]
        public void Default_constructor_sets_recommended_defaults()
        {
            var cfg = new LlmConfiguration();
            Assert.Equal(0.3, cfg.Temperature);
            Assert.Equal(4096, cfg.MaxTokens);
            Assert.Contains("{SourceLanguage}", cfg.SystemPrompt);
            Assert.Contains("{TargetLanguage}", cfg.SystemPrompt);
        }

        [Fact]
        public void Changing_provider_resets_endpoint_and_model_to_preset()
        {
            var cfg = new LlmConfiguration(); // default provider = DeepSeek
            cfg.Provider = LlmProvider.ChatGPT;
            Assert.Equal(LlmProviderPresets.Presets[LlmProvider.ChatGPT].DefaultEndpoint, cfg.Endpoint);
            Assert.Equal("gpt-4.1-mini", cfg.ModelName);
        }

        [Fact]
        public void Custom_provider_clears_endpoint_and_model()
        {
            var cfg = new LlmConfiguration();
            cfg.Provider = LlmProvider.Custom;
            Assert.Equal(string.Empty, cfg.Endpoint);
            Assert.Equal(string.Empty, cfg.ModelName);
        }

        [Fact]
        public void Resolved_endpoint_and_model_fall_back_to_preset_when_override_empty()
        {
            var cfg = new LlmConfiguration();
            cfg.Provider = LlmProvider.Claude;
            cfg.Endpoint = string.Empty;
            cfg.ModelName = string.Empty;
            Assert.Equal(LlmProviderPresets.Presets[LlmProvider.Claude].DefaultEndpoint, cfg.ResolvedEndpoint);
            Assert.Equal("claude-sonnet-4-6", cfg.ResolvedModel);
        }

        [Fact]
        public void User_override_takes_precedence_over_preset()
        {
            var cfg = new LlmConfiguration();
            cfg.Provider = LlmProvider.Gemini;
            cfg.Endpoint = "https://my.proxy/gemini";
            cfg.ModelName = "gemini-2.5-pro";
            Assert.Equal("https://my.proxy/gemini", cfg.ResolvedEndpoint);
            Assert.Equal("gemini-2.5-pro", cfg.ResolvedModel);
        }

        [Fact]
        public void Enabled_requires_endpoint_and_model_for_custom()
        {
            var cfg = new LlmConfiguration();
            cfg.Provider = LlmProvider.Custom;
            Assert.False(cfg.Enabled);
            cfg.Endpoint = "https://x";
            cfg.ModelName = "m";
            Assert.True(cfg.Enabled);
        }

        [Fact]
        public void Json_round_trip_preserves_explicit_overrides()
        {
            var cfg = new LlmConfiguration();
            cfg.Provider = LlmProvider.ChatGPT;
            cfg.Endpoint = "https://override";
            cfg.ModelName = "custom-model";
            cfg.Temperature = 0.7;

            var json = JsonSerializer.Serialize(cfg);
            var restored = JsonSerializer.Deserialize<LlmConfiguration>(json)!;

            // Deserialization must NOT re-apply provider defaults (OnDeserializing guard).
            Assert.Equal("https://override", restored.Endpoint);
            Assert.Equal("custom-model", restored.ModelName);
            Assert.Equal(0.7, restored.Temperature);
        }

        [Fact]
        public void Ollama_provider_does_not_require_api_key()
        {
            var cfg = new LlmConfiguration { Provider = LlmProvider.Ollama };
            Assert.False(cfg.RequiresApiKey);
        }

        [Fact]
        public void Cloud_providers_require_api_key()
        {
            var cfg = new LlmConfiguration { Provider = LlmProvider.DeepSeek };
            Assert.True(cfg.RequiresApiKey);
        }
    }
}
