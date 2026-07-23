#nullable enable

using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Speech.Synthesis;

namespace Translumo.TTS
{
    public static class WindowsTTSHelper
    {
        public static async Task<bool> InstallTTSLanguageCapability(string languageCode)
        {
            var process = CreateTTSCapabilityInstallProcess(languageCode);
            if (process == null)
                return true;

            process.Start();

            // Wait asynchronously for the process to exit
            await Task.Run(() => process.WaitForExit());

            // Optional: check exit code (0 usually means success)
            return process.ExitCode == 0;
        }

        public static bool IsLanguageTTSCapabilityInstalled(string languageTag, bool exactMatching = false)
        {
            using var synth = new SpeechSynthesizer();
            InjectOneCoreVoices(synth);

            try
            {
                // Try exact match
                var voices = synth.GetInstalledVoices(new CultureInfo(languageTag));
                if (voices.Count != 0)
                    return true;
            }
            catch
            {
                // Culture not valid — ignore
            }

            if (!exactMatching)
            {
                try
                {
                    // Fallback: check only the two-letter language code
                    var shortTag = languageTag.Split('-')[0];
                    var voices = synth.GetInstalledVoices(new CultureInfo(shortTag));
                    if (voices.Count != 0)
                        return true;
                }
                catch
                {
                    // Still not valid — ignore
                }
            }

            return false;
        }

        private static Process CreateTTSCapabilityInstallProcess(string nameTag)
        {
            string psCommand = $@"
            $env:TERM = 'xterm';
            $Host.UI.RawUI.WindowTitle = 'Installing TTS Language...';
            Write-Host 'Installing {nameTag} TTS Language Capability. Please wait...' -ForegroundColor Yellow;
            Write-Host 'This can take up to 20 minutes. Please be patient.' -ForegroundColor Yellow;

            $cap = Get-WindowsCapability -Online | Where-Object {{ $_.Name -like 'Language.TextToSpeech~~~{nameTag}~*' }} | Select-Object -First 1;

            if ($cap) {{
                try {{
                    Add-WindowsCapability -Online -Name $cap.Name -ErrorAction Stop;
                    Write-Host 'Installation complete!' -ForegroundColor Green;
                    Start-Sleep -Seconds 2;
                }} catch {{
                    Write-Host 'Installation failed. Please try again or use the manual method.' -ForegroundColor Red;
                    Write-Host $_ -ForegroundColor Red;
                    Start-Sleep -Seconds 60;
                }}
            }} else {{
                Write-Host 'Capability not found. Try installing the language manually.' -ForegroundColor Red;
                Start-Sleep -Seconds 60;
            }}
            ";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"{psCommand}\"",
                    Verb = "runas", // enable elevation
                    UseShellExecute = true, // enable elevation prompt
                },
                EnableRaisingEvents = true, // enable process.Exited event
            };

            return process;  // Return the process after completion
        }

        public static List<VoiceInfo> GetAvailableVoicesForLanguage(string languageTag)
        {
            using var synth = new SpeechSynthesizer();
            InjectOneCoreVoices(synth);
            var result = new List<VoiceInfo>();

            try
            {
                var voices = synth.GetInstalledVoices(new CultureInfo(languageTag));
                if (voices.Count > 0)
                {
                    result.AddRange(voices.Select(v => v.VoiceInfo));
                    return result;
                }
            }
            catch
            {

            }

            try
            {
                var shortTag = languageTag.Split('-')[0];
                var voices = synth.GetInstalledVoices(new CultureInfo(shortTag));
                if (voices.Count > 0)
                {
                    result.AddRange(voices.Select(v => v.VoiceInfo));
                }
            }
            catch
            {

            }

            return result;
        }

        /// <summary>
        /// By default <see cref="SpeechSynthesizer"/> only exposes classic desktop voices and hides
        /// the OneCore voices installed via Windows Settings on Win10/11. This injects the OneCore
        /// voices into the synthesizer's internal voice list so they appear in GetInstalledVoices.
        /// Ported from upstream PR ramjke/Translumo#77. Based on https://stackoverflow.com/a/71198211
        /// </summary>
        public static void InjectOneCoreVoices(SpeechSynthesizer synthesizer)
        {
            var voiceSynthesizer = SpeechApiReflectionHelper.GetProperty(synthesizer, SpeechApiReflectionHelper.PropVoiceSynthesizer);
            if (voiceSynthesizer == null)
                throw new NotSupportedException($"Property not found: {SpeechApiReflectionHelper.PropVoiceSynthesizer}");

            var installedVoices = SpeechApiReflectionHelper.GetField(voiceSynthesizer, SpeechApiReflectionHelper.FieldInstalledVoices) as IList;
            if (installedVoices == null)
                throw new NotSupportedException($"Field not found or null: {SpeechApiReflectionHelper.FieldInstalledVoices}");

            if (SpeechApiReflectionHelper.ObjectTokenCategoryType
                    .GetMethod(SpeechApiReflectionHelper.MethodCreate, BindingFlags.Static | BindingFlags.NonPublic)?
                    .Invoke(null, new object?[] { SpeechApiReflectionHelper.OneCoreVoicesRegistry }) is not IDisposable otc)
                throw new NotSupportedException($"Failed to call Create on {SpeechApiReflectionHelper.ObjectTokenCategoryType} instance");

            using (otc)
            {
                if (SpeechApiReflectionHelper.ObjectTokenCategoryType
                        .GetMethod(SpeechApiReflectionHelper.MethodFindMatchingTokens, BindingFlags.Instance | BindingFlags.NonPublic)?
                        .Invoke(otc, new object?[] { null, null }) is not IList tokens)
                    throw new NotSupportedException("Failed to list matching tokens");

                foreach (var token in tokens)
                {
                    if (token == null || SpeechApiReflectionHelper.GetProperty(token, SpeechApiReflectionHelper.PropAttributes) == null)
                        continue;

                    var voiceInfo =
                        typeof(SpeechSynthesizer).Assembly
                            .CreateInstance(SpeechApiReflectionHelper.VoiceInfoType.FullName!, true,
                                BindingFlags.Instance | BindingFlags.NonPublic, null,
                                new object[] { token }, null, null);

                    if (voiceInfo == null)
                        throw new NotSupportedException($"Failed to instantiate {SpeechApiReflectionHelper.VoiceInfoType}");

                    var installedVoice =
                        typeof(SpeechSynthesizer).Assembly
                            .CreateInstance(SpeechApiReflectionHelper.InstalledVoiceType.FullName!, true,
                                BindingFlags.Instance | BindingFlags.NonPublic, null,
                                new object[] { voiceSynthesizer, voiceInfo }, null, null);

                    if (installedVoice == null)
                        throw new NotSupportedException($"Failed to instantiate {SpeechApiReflectionHelper.InstalledVoiceType}");

                    installedVoices.Add(installedVoice);
                }
            }
        }

        private static class SpeechApiReflectionHelper
        {
            internal const string PropVoiceSynthesizer = "VoiceSynthesizer";
            internal const string FieldInstalledVoices = "_installedVoices";
            internal const string PropAttributes = "Attributes";
            internal const string MethodCreate = "Create";
            internal const string MethodFindMatchingTokens = "FindMatchingTokens";
            internal const string OneCoreVoicesRegistry = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech_OneCore\Voices";

            internal static readonly Type ObjectTokenCategoryType = typeof(SpeechSynthesizer).Assembly
                .GetType("System.Speech.Internal.ObjectTokens.ObjectTokenCategory")!;

            internal static readonly Type VoiceInfoType = typeof(SpeechSynthesizer).Assembly
                .GetType("System.Speech.Synthesis.VoiceInfo")!;

            internal static readonly Type InstalledVoiceType = typeof(SpeechSynthesizer).Assembly
                .GetType("System.Speech.Synthesis.InstalledVoice")!;

            internal static object? GetProperty(object target, string propName)
            {
                return target.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target);
            }

            internal static object? GetField(object target, string propName)
            {
                return target.GetType().GetField(propName, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target);
            }
        }
    }
}
