using System.Collections.Generic;

namespace Translumo.Translation.Llm
{
    public sealed class LlmProviderPreset
    {
        public string DefaultEndpoint { get; }

        public string DefaultModel { get; }

        public LlmApiStyle ApiStyle { get; }

        public LlmProviderPreset(string defaultEndpoint, string defaultModel, LlmApiStyle apiStyle)
        {
            DefaultEndpoint = defaultEndpoint;
            DefaultModel = defaultModel;
            ApiStyle = apiStyle;
        }
    }

    /// <summary>
    /// Default endpoints / models for each built-in provider. The user can always override both
    /// the endpoint and the model name from the UI (useful for self-hosted / proxy gateways).
    /// </summary>
    public static class LlmProviderPresets
    {
        public static IReadOnlyDictionary<LlmProvider, LlmProviderPreset> Presets { get; } =
            new Dictionary<LlmProvider, LlmProviderPreset>
            {
                [LlmProvider.DeepSeek] = new LlmProviderPreset(
                    "https://api.deepseek.com/chat/completions", "deepseek-chat", LlmApiStyle.OpenAi),

                [LlmProvider.Qwen] = new LlmProviderPreset(
                    "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions", "qwen-plus", LlmApiStyle.OpenAi),

                [LlmProvider.Kimi] = new LlmProviderPreset(
                    "https://api.moonshot.cn/v1/chat/completions", "moonshot-v1-8k", LlmApiStyle.OpenAi),

                [LlmProvider.GLM] = new LlmProviderPreset(
                    "https://open.bigmodel.cn/api/paas/v4/chat/completions", "glm-4-plus", LlmApiStyle.OpenAi),

                [LlmProvider.MiniMax] = new LlmProviderPreset(
                    "https://api.minimax.io/v1/chat/completions", "MiniMax-M2.7", LlmApiStyle.OpenAi),

                [LlmProvider.ChatGPT] = new LlmProviderPreset(
                    "https://api.openai.com/v1/chat/completions", "gpt-4o-mini", LlmApiStyle.OpenAi),

                [LlmProvider.Claude] = new LlmProviderPreset(
                    "https://api.anthropic.com/v1/messages", "claude-3-5-sonnet-latest", LlmApiStyle.Anthropic),

                [LlmProvider.Gemini] = new LlmProviderPreset(
                    "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent", "gemini-2.5-flash", LlmApiStyle.Gemini),

                [LlmProvider.Grok] = new LlmProviderPreset(
                    "https://api.x.ai/v1/chat/completions", "grok-2-latest", LlmApiStyle.OpenAi),

                [LlmProvider.Custom] = new LlmProviderPreset(
                    string.Empty, string.Empty, LlmApiStyle.OpenAi)
            };
    }
}
