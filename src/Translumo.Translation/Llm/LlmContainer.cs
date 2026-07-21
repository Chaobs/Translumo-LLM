using Translumo.Translation.Configuration;
using Translumo.Utils.Http;

namespace Translumo.Translation.Llm
{
    public sealed class LlmContainer : TranslationContainer
    {
        public HttpReader Reader { get; }

        public LlmContainer(Proxy proxy = null, bool isPrimary = false) : base(proxy, isPrimary)
        {
            Reader = CreateReader(proxy);
        }

        private HttpReader CreateReader(Proxy proxy)
        {
            var reader = new HttpReader();
            reader.Proxy = proxy?.ToWebProxy();
            reader.ContentType = "application/json";
            reader.Accept = "application/json";
            reader.UserAgent = "Translumo-LLM/1.0";
            reader.OptionalHeaders.Add("Accept-Language", "en-US,en;q=0.9");

            return reader;
        }
    }
}
