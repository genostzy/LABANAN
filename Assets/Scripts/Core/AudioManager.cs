using UnityEngine;

namespace LABANAN
{
    /// <summary>
    /// Manages all audio playback - music and sound effects.
    /// Uses classpath resources for portability.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Music")]
        public AudioClip menuMusic;
        public AudioClip level1Music;
        public AudioClip level2Music;

        [Header("Sound Effects")]
        public AudioClip attackSfx;
        public AudioClip attack2Sfx;
        public AudioClip blockSfx;
        public AudioClip hurtSfx;
        public AudioClip jumpSfx;

        [Header("Sources")]
        public AudioSource musicSource;
        public AudioSource sfxSource;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Create audio sources if not assigned
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }
        }

        public void PlayMenuMusic()
        {
            PlayMusic(menuMusic);
        }

        public void PlayLevel1()
        {
            PlayMusic(level1Music);
        }

        public void PlayLevel2()
        {
            PlayMusic(level2Music);
        }

        private void PlayMusic(AudioClip clip)
        {
            if (clip == null || musicSource == null) return;

            if (musicSource.clip == clip && musicSource.isPlaying) return;

            musicSource.clip = clip;
            musicSource.Play();
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }

        public void PlayAttackSFX() => PlaySFX(attackSfx);
        public void PlayBlockSFX() => PlaySFX(blockSfx);
        public void PlayHurtSFX() => PlaySFX(hurtSfx);
        public void PlayJumpSFX() => PlaySFX(jumpSfx);

        public void PauseMusic()
        {
            if (musicSource != null && musicSource.isPlaying)
                musicSource.Pause();
        }

        public void ResumeMusic()
        {
            if (musicSource != null && !musicSource.isPlaying)
                musicSource.UnPause();
        }

        public void StopMusic()
        {
            if (musicSource != null)
                musicSource.Stop();
        }
    }
}
