using UnityEngine;

namespace IbArtMuseum
{
    public class IbAudioAmbience : MonoBehaviour
    {
        [Header("Audio Sources")]
        public AudioSource bgmSource;
        public AudioSource sfxSource;
        public AudioClip customBgmClip;

        private void Awake()
        {
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
                bgmSource.volume = 0.35f;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
                sfxSource.volume = 0.7f;
            }
        }

        private void Start()
        {
            PlayAtmosphericBGM();
        }

        private void PlayAtmosphericBGM()
        {
            // 플레이어에 이미 emotion.mp3가 재생 중이면 중복 BGM 방지
            if (IbPlayerController.LocalPlayer != null && IbPlayerController.LocalPlayer.bgmClip != null)
            {
                return;
            }

            AudioClip clipToPlay = (customBgmClip != null) ? customBgmClip : GenerateDroneClip();
            if (clipToPlay != null && bgmSource != null)
            {
                bgmSource.clip = clipToPlay;
                bgmSource.volume = 0.35f;
                bgmSource.Play();
            }
        }

        public void PlayCorrectSound()
        {
            AudioClip chime = GenerateChimeClip(523.25f); // C5 은은한 톤
            if (sfxSource != null && chime != null)
            {
                sfxSource.PlayOneShot(chime, 0.6f);
            }
        }

        public void PlayMistakeSound()
        {
            AudioClip dissonant = GenerateDissonantClip();
            if (sfxSource != null && dissonant != null)
            {
                sfxSource.PlayOneShot(dissonant, 0.8f);
            }
        }

        public void PlayEscapeSound()
        {
            AudioClip escapeChord = GenerateEscapeChordClip();
            if (sfxSource != null && escapeChord != null)
            {
                sfxSource.PlayOneShot(escapeChord, 0.8f);
            }
        }

        /// <summary>
        /// 스산한 미술관 저음 앰비언스 드론 생성
        /// </summary>
        private AudioClip GenerateDroneClip()
        {
            int sampleRate = 44100;
            float length = 4.0f;
            int totalSamples = (int)(sampleRate * length);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                // 55Hz (A1) + 82.4Hz (E2) 저음 합성 및 미세한 비트 주파수
                float wave1 = Mathf.Sin(2 * Mathf.PI * 55f * t) * 0.15f;
                float wave2 = Mathf.Sin(2 * Mathf.PI * 55.5f * t) * 0.15f;
                float sub = Mathf.Sin(2 * Mathf.PI * 27.5f * t) * 0.2f;
                float whiteNoise = (Random.value * 2f - 1f) * 0.015f;

                data[i] = (wave1 + wave2 + sub + whiteNoise) * 0.5f;
            }

            AudioClip clip = AudioClip.Create("IbMuseumDrone", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// 은은한 미술관 차임벨 단음 생성
        /// </summary>
        private AudioClip GenerateChimeClip(float freq)
        {
            int sampleRate = 44100;
            float length = 1.5f;
            int totalSamples = (int)(sampleRate * length);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-3f * t);
                float wave = Mathf.Sin(2 * Mathf.PI * freq * t) + 0.5f * Mathf.Sin(2 * Mathf.PI * freq * 2f * t);
                data[i] = wave * envelope * 0.3f;
            }

            AudioClip clip = AudioClip.Create("ChimeClip", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// 오답 리셋 불협화음 글리치 사운드 생성
        /// </summary>
        private AudioClip GenerateDissonantClip()
        {
            int sampleRate = 44100;
            float length = 1.0f;
            int totalSamples = (int)(sampleRate * length);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-4f * t);
                float wave = Mathf.Sin(2 * Mathf.PI * 110f * t) + Mathf.Sin(2 * Mathf.PI * 116.5f * t) + Mathf.Sin(2 * Mathf.PI * 155.5f * t);
                data[i] = wave * envelope * 0.35f;
            }

            AudioClip clip = AudioClip.Create("DissonantClip", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// 탈출 성공 화음 생성
        /// </summary>
        private AudioClip GenerateEscapeChordClip()
        {
            int sampleRate = 44100;
            float length = 3.0f;
            int totalSamples = (int)(sampleRate * length);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-1.5f * t);
                // C Major 7th 화음 (261.63, 329.63, 392.00, 493.88)
                float c = Mathf.Sin(2 * Mathf.PI * 261.63f * t);
                float e = Mathf.Sin(2 * Mathf.PI * 329.63f * t);
                float g = Mathf.Sin(2 * Mathf.PI * 392.00f * t);
                float b = Mathf.Sin(2 * Mathf.PI * 493.88f * t);
                data[i] = (c + e + g + b) * 0.2f * envelope;
            }

            AudioClip clip = AudioClip.Create("EscapeChordClip", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
