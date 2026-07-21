namespace Translumo.Translation.Llm
{
    /// <summary>
    /// Supported LLM providers. Each provider maps to a preset (endpoint, default model, API style).
    /// </summary>
    public enum LlmProvider
    {
        DeepSeek = 0,
        Qwen = 1,
        Kimi = 2,
        GLM = 3,
        MiniMax = 4,
        ChatGPT = 5,
        Claude = 6,
        Gemini = 7,
        Grok = 8,
        Custom = 9
    }

    /// <summary>
    /// Wire format used to talk to the provider.
    /// </summary>
    public enum LlmApiStyle
    {
        /// <summary>OpenAI compatible chat/completions (Bearer auth).</summary>
        OpenAi,
        /// <summary>Anthropic messages API (x-api-key header).</summary>
        Anthropic,
        /// <summary>Google Gemini generateContent (key in query string).</summary>
        Gemini
    }
}
