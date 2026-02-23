using UnityEngine;

namespace PrismPulse.Gameplay.Audio
{
    /// <summary>
    /// Generates and plays procedural sound effects.
    /// All audio clips are synthesized at runtime — no audio assets needed.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("Volume")]
        [SerializeField] private float _masterVolume = 0.7f;

        private AudioSource _source;

        private AudioClip _rotateClip;
        private AudioClip _beamConnectClip;
        private AudioClip _solveClip;
        private AudioClip _levelStartClip;
        private AudioClip _buttonClickClip;

        private const int SampleRate = 44100;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;

            GenerateClips();
        }

        public bool IsMuted
        {
            get => _source != null && _source.mute;
            set { if (_source != null) _source.mute = value; }
        }

        public void PlayRotate() => PlayClip(_rotateClip, 0.5f);
        public void PlayBeamConnect() => PlayClip(_beamConnectClip, 0.6f);
        public void PlaySolve() => PlayClip(_solveClip, 0.8f);
        public void PlayLevelStart() => PlayClip(_levelStartClip, 0.3f);
        public void PlayButtonClick() => PlayClip(_buttonClickClip, 0.4f);

        private void PlayClip(AudioClip clip, float volume)
        {
            if (clip == null || _source == null) return;
            _source.PlayOneShot(clip, volume * _masterVolume);
        }

        private void GenerateClips()
        {
            _rotateClip = GenerateSineBlip("Rotate", 800f, 0.05f);
            _beamConnectClip = GenerateSineSweep("BeamConnect", 400f, 800f, 0.15f);
            _solveClip = GenerateChord("Solve", 0.4f);
            _levelStartClip = GenerateNoise("LevelStart", 0.2f);
            _buttonClickClip = GenerateSineBlip("ButtonClick", 1000f, 0.03f);
        }

        /// <summary>
        /// Short sine wave blip with exponential decay.
        /// </summary>
        private static AudioClip GenerateSineBlip(string name, float freq, float duration)
        {
            int samples = (int)(SampleRate * duration);
            var data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float envelope = Mathf.Exp(-t * 40f); // fast decay
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope;
            }

            return CreateClip(name, data);
        }

        /// <summary>
        /// Sine wave with linearly sweeping frequency. Rising tone for beam connect.
        /// </summary>
        private static AudioClip GenerateSineSweep(string name, float freqStart, float freqEnd, float duration)
        {
            int samples = (int)(SampleRate * duration);
            var data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float norm = (float)i / samples;
                float freq = Mathf.Lerp(freqStart, freqEnd, norm);
                float envelope = 1f - norm; // linear fade out
                envelope *= Mathf.Clamp01(t * 100f); // quick fade in
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.8f;
            }

            return CreateClip(name, data);
        }

        /// <summary>
        /// Major chord (C5 + E5 + G5) with staggered attacks. Celebratory sound.
        /// </summary>
        private static AudioClip GenerateChord(string name, float duration)
        {
            int samples = (int)(SampleRate * duration);
            var data = new float[samples];

            float[] freqs = { 523.25f, 659.25f, 783.99f }; // C5, E5, G5
            float[] delays = { 0f, 0.04f, 0.08f }; // staggered entry

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float sum = 0f;

                for (int n = 0; n < freqs.Length; n++)
                {
                    float noteT = t - delays[n];
                    if (noteT < 0f) continue;

                    float envelope = Mathf.Exp(-noteT * 5f); // gentle decay
                    envelope *= Mathf.Clamp01(noteT * 200f); // quick attack
                    sum += Mathf.Sin(2f * Mathf.PI * freqs[n] * noteT) * envelope;
                }

                data[i] = sum / freqs.Length * 0.9f;
            }

            return CreateClip(name, data);
        }

        /// <summary>
        /// Filtered noise burst. Soft whoosh for level transitions.
        /// </summary>
        private static AudioClip GenerateNoise(string name, float duration)
        {
            int samples = (int)(SampleRate * duration);
            var data = new float[samples];

            // Use seeded random for consistency
            var rng = new System.Random(42);
            float prev = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float norm = (float)i / samples;

                // Envelope: fade in then fade out
                float envelope = Mathf.Sin(norm * Mathf.PI);

                // Simple low-pass: average with previous sample
                float raw = (float)(rng.NextDouble() * 2.0 - 1.0);
                float filtered = prev * 0.7f + raw * 0.3f;
                prev = filtered;

                data[i] = filtered * envelope * 0.4f;
            }

            return CreateClip(name, data);
        }

        private static AudioClip CreateClip(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
