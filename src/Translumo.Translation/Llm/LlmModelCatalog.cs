using System.Collections.Generic;

namespace Translumo.Translation.Llm
{
    /// <summary>
    /// Curated, provider-specific model catalogs used to populate the model selector dropdown.
    /// The first entry of each list mirrors that provider's default preset model. The lists
    /// reflect officially available models as of mid-2026 (verified against each vendor's docs).
    /// The dropdown is editable, so users can still type a custom model name (proxies, preview
    /// builds or models not yet listed here).
    /// </summary>
    public static class LlmModelCatalog
    {
        public static IReadOnlyDictionary<LlmProvider, IReadOnlyList<string>> Models { get; } =
            new Dictionary<LlmProvider, IReadOnlyList<string>>
            {
                [LlmProvider.ChatGPT] = new[]
                {
                    "gpt-4.1-mini",
                    "gpt-4.1-nano",
                    "gpt-4.1",
                    "gpt-5-mini",
                    "gpt-5-nano",
                    "gpt-5.1",
                    "gpt-5.4",
                    "gpt-5.5",
                },

                [LlmProvider.Claude] = new[]
                {
                    "claude-sonnet-4-6",
                    "claude-opus-4-8",
                    "claude-opus-4-7",
                    "claude-sonnet-4-5",
                    "claude-haiku-4-5",
                },

                [LlmProvider.Gemini] = new[]
                {
                    "gemini-2.5-flash",
                    "gemini-2.5-pro",
                    "gemini-2.5-flash-lite",
                    "gemini-3-flash",
                    "gemini-3.5-flash",
                    "gemini-3.1-pro-preview",
                },

                [LlmProvider.DeepSeek] = new[]
                {
                    "deepseek-v4-flash",
                    "deepseek-v4-pro",
                },

                [LlmProvider.Qwen] = new[]
                {
                    "qwen-plus",
                    "qwen-plus-latest",
                    "qwen-max",
                    "qwen-max-latest",
                    "qwen-turbo",
                    "qwen-flash",
                },

                [LlmProvider.Kimi] = new[]
                {
                    "kimi-k3",
                    "kimi-k2.7-code",
                    "kimi-k2.7-code-highspeed",
                    "kimi-k2.6",
                    "kimi-k2.5",
                    "moonshot-v1-8k",
                    "moonshot-v1-32k",
                    "moonshot-v1-128k",
                },

                [LlmProvider.GLM] = new[]
                {
                    "glm-4-plus",
                    "glm-4-air-250414",
                    "glm-4-airx",
                    "glm-4-flash-250414",
                    "glm-4-flashx-250414",
                    "glm-4-long",
                },

                [LlmProvider.MiniMax] = new[]
                {
                    "MiniMax-M3",
                    "MiniMax-M2.7",
                    "MiniMax-M2.7-highspeed",
                    "MiniMax-M2.5",
                    "MiniMax-M2.5-highspeed",
                    "MiniMax-M2.1",
                },

                [LlmProvider.Grok] = new[]
                {
                    "grok-3-mini",
                    "grok-3",
                    "grok-4",
                    "grok-4-fast-non-reasoning",
                    "grok-4-fast-reasoning",
                    "grok-4.1-fast-non-reasoning",
                    "grok-4.1-fast-reasoning",
                },

                [LlmProvider.Ollama] = new[]
                {
                    "llama3",
                    "llama3.1",
                    "llama3.2",
                    "qwen2.5",
                    "qwen2.5-coder",
                    "mistral",
                    "mistral-nemo",
                    "gemma2",
                    "phi3",
                    "deepseek-r1",
                },

                // Custom providers have no fixed model list — the user types their own.
                [LlmProvider.Custom] = new string[0],
            };
    }
}
