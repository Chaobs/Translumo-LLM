using System.Collections.ObjectModel;
using System.Globalization;
using System.Speech.Synthesis;
using Translumo.TTS;

namespace Translumo.TTS.Engines;

public class WindowsTTSEngine : ITTSEngine
{
    private VoiceInfo _voiceInfo;
    private readonly SpeechSynthesizer _synthesizer;
    private readonly ReadOnlyDictionary<string, VoiceInfo> _voices;

    public WindowsTTSEngine(string languageCode, string voiceName = null)
    {
        _synthesizer = new SpeechSynthesizer();
        _synthesizer.SetOutputToDefaultAudioDevice();
        _synthesizer.Rate = 3;

        // By default SpeechSynthesizer does not expose all installed OneCore voices
        // (the modern voices installed via Windows Settings on Win10/11).
        WindowsTTSHelper.InjectOneCoreVoices(_synthesizer);

        _voices = _synthesizer
            .GetInstalledVoices(new CultureInfo(languageCode))
            .ToDictionary(v => v.VoiceInfo.Name, v => v.VoiceInfo)
            .AsReadOnly();

        _voiceInfo = !string.IsNullOrEmpty(voiceName)
            ? _voices.FirstOrDefault(x => x.Key.Equals(voiceName, StringComparison.OrdinalIgnoreCase)).Value
            : null;
        _voiceInfo ??= _voices.FirstOrDefault().Value;
    }

    public void SpeechText(string text)
    {
        // https://learn.microsoft.com/en-us/archive/msdn-magazine/2019/june/speech-text-to-speech-synthesis-in-net
        if (_voiceInfo == null)
        {
            return;
        }
        var builder = new PromptBuilder();
        builder.StartVoice(_voiceInfo);
        builder.AppendText(text);
        builder.EndVoice();
        _synthesizer.SpeakAsyncCancelAll();
        _synthesizer.SpeakAsync(builder);
    }

    public string[] GetVoices() => _voices.Keys.ToArray();

    public void SetVoice(string voice) =>
        _voiceInfo = _voices.FirstOrDefault(x => x.Key.Equals(voice, StringComparison.OrdinalIgnoreCase)).Value;

    public void Dispose()
    {
        _synthesizer.Dispose();
    }
}
