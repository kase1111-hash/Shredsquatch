using UnityEngine;

namespace Shredsquatch.Audio
{
    /// <summary>
    /// Generates placeholder audio clips for testing until real audio assets are available.
    /// Creates simple tones, noise, and synthesized sounds.
    /// </summary>
    public static class AudioPlaceholderGenerator
    {
        private const int SampleRate = 44100;

        #region Music Placeholders

        /// <summary>
        /// Generate a simple ambient music loop for menu.
        /// </summary>
        public static AudioClip GenerateMenuMusic(float duration = 30f)
        {
            int samples = (int)(SampleRate * duration);
            var clip = AudioClip.Create("MenuMusic_Placeholder", samples, 1, SampleRate, false);
            float[] data = new float[samples];

            // Low drone with subtle variation
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;

                // Base drone
                float drone = Mathf.Sin(2 * Mathf.PI * 55 * t) * 0.2f; // A1

                // Slow modulation
                float mod = Mathf.Sin(2 * Mathf.PI * 0.1f * t);
                drone *= 0.8f + mod * 0.2f;

                // Add subtle harmonics
                drone += Mathf.Sin(2 * Mathf.PI * 110 * t) * 0.1f;
                drone += Mathf.Sin(2 * Mathf.PI * 165 * t) * 0.05f;

                data[i] = drone * 0.3f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Generate gameplay music (more energetic).
        /// </summary>
        public static AudioClip GenerateGameplayMusic(float duration = 60f)
        {
            int samples = (int)(SampleRate * duration);
            var clip = AudioClip.Create("GameplayMusic_Placeholder", samples, 1, SampleRate, false);
            float[] data = new float[samples];

            // Tempo-based pulse with beat
            float bpm = 140f;
            float beatDuration = 60f / bpm;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float beatPhase = (t % beatDuration) / beatDuration;

                // Bass drum on beat
                float kick = 0f;
                if (beatPhase < 0.1f)
                {
                    float kickEnv = 1f - beatPhase / 0.1f;
                    float kickFreq = 60f + 100f * kickEnv;
                    kick = Mathf.Sin(2 * Mathf.PI * kickFreq * t) * kickEnv * 0.5f;
                }

                // Hi-hat on off-beat
                float hihat = 0f;
                float offbeatPhase = ((t + beatDuration / 2) % beatDuration) / beatDuration;
                if (offbeatPhase < 0.05f)
                {
                    hihat = (Random.value * 2 - 1) * (1f - offbeatPhase / 0.05f) * 0.15f;
                }

                // Bass line
                float bassNote = (int)(t / beatDuration) % 4;
                float bassFreq = GetNoteFrequency(bassNote % 4 == 0 ? 36 : bassNote % 4 == 2 ? 38 : 41);
                float bass = Mathf.Sin(2 * Mathf.PI * bassFreq * t) * 0.2f;

                data[i] = Mathf.Clamp(kick + hihat + bass, -1f, 1f) * 0.4f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Generate tense chase music.
        /// </summary>
        public static AudioClip GenerateChaseMusic(float duration = 60f)
        {
            int samples = (int)(SampleRate * duration);
            var clip = AudioClip.Create("ChaseMusic_Placeholder", samples, 1, SampleRate, false);
            float[] data = new float[samples];

            float bpm = 170f;
            float beatDuration = 60f / bpm;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float beatPhase = (t % beatDuration) / beatDuration;

                // Aggressive kick
                float kick = 0f;
                if (beatPhase < 0.08f)
                {
                    float kickEnv = 1f - beatPhase / 0.08f;
                    float kickFreq = 50f + 150f * kickEnv;
                    kick = Mathf.Sin(2 * Mathf.PI * kickFreq * t) * kickEnv * 0.6f;
                }

                // Snare on 2 and 4
                float snare = 0f;
                int beatNum = (int)(t / beatDuration) % 4;
                if ((beatNum == 1 || beatNum == 3) && beatPhase < 0.1f)
                {
                    float snareEnv = 1f - beatPhase / 0.1f;
                    snare = (Random.value * 2 - 1) * snareEnv * 0.3f;
                    snare += Mathf.Sin(2 * Mathf.PI * 200 * t) * snareEnv * 0.2f;
                }

                // Ominous low pulse
                float pulse = Mathf.Sin(2 * Mathf.PI * 40 * t) * 0.15f;
                pulse *= 0.7f + 0.3f * Mathf.Sin(2 * Mathf.PI * 0.5f * t);

                // Dissonant high string
                float stringNote = Mathf.Sin(2 * Mathf.PI * 440 * t) * 0.05f;
                stringNote += Mathf.Sin(2 * Mathf.PI * 466 * t) * 0.05f; // Minor 2nd - tension

                data[i] = Mathf.Clamp(kick + snare + pulse + stringNote, -1f, 1f) * 0.5f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        #endregion

        #region SFX Placeholders

        /// <summary>
        /// Generate a jump sound.
        /// </summary>
        public static AudioClip GenerateJumpSound()
        {
            float duration = 0.2f;
            int samples = (int)(SampleRate * duration);
            var clip = AudioClip.Create("Jump_Placeholder", samples, 1, SampleRate, false);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float env = 1f - t;

                // Rising tone
                float freq = 200f + 400f * t;
                data[i] = Mathf.Sin(2 * Mathf.PI * freq * t * duration) * env * 0.5f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Generate a landing sound.
        /// </summary>
        public static AudioClip GenerateLandSound()
        {
            float duration = 0.15f;
            int samples = (int)(SampleRate * duration);
            var clip = AudioClip.Create("Land_Placeholder", samples, 1, SampleRate, false);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float env = Mathf.Exp(-t * 10f);

                // Thump with noise
                float thump = Mathf.Sin(2 * Mathf.PI * 80 * t * duration) * env;
                float noise = (Random.value * 2 - 1) * env * 0.3f;

                data[i] = (thump + noise) * 0.6f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Generate a trick completion sound.
        /// </summary>
        public static AudioClip GenerateTrickSound()
        {
            float duration = 0.3f;
            int samples = (int)(SampleRate * duration);
            var clip = AudioClip.Create("Trick_Placeholder", samples, 1, SampleRate, false);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float env = Mathf.Exp(-t * 5f);

                // Ascending arpeggio
                float note1 = Mathf.Sin(2 * Mathf.PI * 523 * t * duration); // C5
                float note2 = Mathf.Sin(2 * Mathf.PI * 659 * t * duration) * (t > 0.1f ? 1 : 0); // E5
                float note3 = Mathf.Sin(2 * Mathf.PI * 784 * t * duration) * (t > 0.2f ? 1 : 0); // G5

                data[i] = (note1 + note2 + note3) * env * 0.3f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Generate a coin collect sound.
        /// </summary>
        public static AudioClip GenerateCoinSound()
        {
            float duration = 0.15f;
            int samples = (int)(SampleRate * duration);
            var clip = AudioClip.Create("Coin_Placeholder", samples, 1, SampleRate, false);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float env = Mathf.Exp(-t * 8f);

                // High ping
                float ping = Mathf.Sin(2 * Mathf.PI * 1200 * t * duration);
                ping += Mathf.Sin(2 * Mathf.PI * 1800 * t * duration) * 0.5f;

                data[i] = ping * env * 0.4f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Generate a crash/impact sound.
        /// </summary>
        public static AudioClip GenerateCrashSound()
        {
            float duration = 0.5f;
            int samples = (int)(SampleRate * duration);
            var clip = AudioClip.Create("Crash_Placeholder", samples, 1, SampleRate, false);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float env = Mathf.Exp(-t * 4f);

                // Heavy impact noise
                float noise = (Random.value * 2 - 1) * env;

                // Low thud
                float thud = Mathf.Sin(2 * Mathf.PI * 60 * t * duration) * env * 1.5f;

                // Crunch
                float crunch = (Random.value * 2 - 1) * Mathf.Exp(-t * 8f) * 0.5f;

                data[i] = Mathf.Clamp(noise * 0.4f + thud + crunch, -1f, 1f) * 0.6f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Generate a Sasquatch roar.
        /// </summary>
        public static AudioClip GenerateSasquatchRoar()
        {
            float duration = 2f;
            int samples = (int)(SampleRate * duration);
            var clip = AudioClip.Create("SasquatchRoar_Placeholder", samples, 1, SampleRate, false);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;

                // Envelope: attack, sustain, decay
                float env;
                if (t < 0.1f)
                    env = t / 0.1f; // Attack
                else if (t < 0.7f)
                    env = 1f; // Sustain
                else
                    env = 1f - (t - 0.7f) / 0.3f; // Decay

                // Low growl with formants
                float baseFreq = 80f + 20f * Mathf.Sin(2 * Mathf.PI * 5f * t);
                float growl = 0f;

                for (int h = 1; h <= 8; h++)
                {
                    float harmonic = Mathf.Sin(2 * Mathf.PI * baseFreq * h * t * duration);
                    growl += harmonic / h;
                }

                // Add noise for texture
                float noise = (Random.value * 2 - 1) * 0.2f;

                // Vibrato
                float vibrato = 1f + 0.1f * Mathf.Sin(2 * Mathf.PI * 8f * t);

                data[i] = (growl * vibrato + noise) * env * 0.4f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Generate powerup collection sound.
        /// </summary>
        public static AudioClip GeneratePowerupSound()
        {
            float duration = 0.4f;
            int samples = (int)(SampleRate * duration);
            var clip = AudioClip.Create("Powerup_Placeholder", samples, 1, SampleRate, false);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float env = Mathf.Exp(-t * 3f);

                // Ascending sweep
                float freq = 400f + 800f * t;
                float sweep = Mathf.Sin(2 * Mathf.PI * freq * t * duration);

                // Sparkle
                float sparkle = Mathf.Sin(2 * Mathf.PI * 2000 * t * duration) * Mathf.Exp(-t * 10f);

                data[i] = (sweep + sparkle * 0.3f) * env * 0.4f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Generate wind ambience.
        /// </summary>
        public static AudioClip GenerateWindAmbience(float duration = 10f)
        {
            int samples = (int)(SampleRate * duration);
            var clip = AudioClip.Create("Wind_Placeholder", samples, 1, SampleRate, false);
            float[] data = new float[samples];

            // Pre-generate some noise
            float[] noiseBuffer = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                noiseBuffer[i] = Random.value * 2 - 1;
            }

            // Apply low-pass filter and modulation
            float filtered = 0f;
            float alpha = 0.001f; // Very low pass

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;

                // Filtered noise
                filtered = filtered * (1f - alpha) + noiseBuffer[i] * alpha;

                // Modulate volume for gusts
                float gust = 0.5f + 0.5f * Mathf.Sin(2 * Mathf.PI * 0.1f * t);
                gust *= 0.7f + 0.3f * Mathf.Sin(2 * Mathf.PI * 0.03f * t);

                data[i] = filtered * gust * 10f * 0.3f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Generate carving/snow sound.
        /// </summary>
        public static AudioClip GenerateCarvingSound(float duration = 2f)
        {
            int samples = (int)(SampleRate * duration);
            var clip = AudioClip.Create("Carving_Placeholder", samples, 1, SampleRate, false);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;

                // Filtered noise for snow scraping
                float noise = Random.value * 2 - 1;

                // Modulate for scraping sound
                float mod = 0.5f + 0.5f * Mathf.Sin(2 * Mathf.PI * 20 * t);

                data[i] = noise * mod * 0.15f;
            }

            // Apply simple low-pass
            for (int i = 1; i < samples; i++)
            {
                data[i] = data[i] * 0.1f + data[i - 1] * 0.9f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Generate achievement unlock sound.
        /// </summary>
        public static AudioClip GenerateAchievementSound()
        {
            float duration = 0.6f;
            int samples = (int)(SampleRate * duration);
            var clip = AudioClip.Create("Achievement_Placeholder", samples, 1, SampleRate, false);
            float[] data = new float[samples];

            // Notes for fanfare
            float[] notes = { 523f, 659f, 784f, 1047f }; // C5, E5, G5, C6
            float noteLength = duration / notes.Length;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                int noteIndex = Mathf.Min((int)(t / noteLength), notes.Length - 1);
                float noteT = (t - noteIndex * noteLength) / noteLength;

                float env = Mathf.Exp(-noteT * 3f);
                float freq = notes[noteIndex];

                float tone = Mathf.Sin(2 * Mathf.PI * freq * t);
                tone += Mathf.Sin(2 * Mathf.PI * freq * 2 * t) * 0.3f; // Harmonic

                data[i] = tone * env * 0.4f;
            }

            clip.SetData(data, 0);
            return clip;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Get frequency for MIDI note number.
        /// </summary>
        private static float GetNoteFrequency(int midiNote)
        {
            return 440f * Mathf.Pow(2f, (midiNote - 69) / 12f);
        }

        #endregion
    }
}
