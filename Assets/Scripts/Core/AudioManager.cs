using UnityEngine;

namespace LABANAN
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private AudioSource musicSource;
        private AudioSource sfxSource;

        [Header("Loaded Clips")]
        public AudioClip bgmClip;
        public AudioClip level1Clip;
        public AudioClip level2Clip;
        public AudioClip attack1Clip;
        public AudioClip attack2Clip;
        public AudioClip blockClip;
        public AudioClip hurtClip;
        public AudioClip jumpClip;
        public AudioClip labanClip;
        public AudioClip tiktikClip;
        public AudioClip pwestoClip;
        public AudioClip panaloPulaClip;
        public AudioClip walangPanaloClip;

        private int lastTimerSecond = -1;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.volume = 0.08f;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.volume = 1f;

            LoadAll();
        }

        private void LoadAll()
        {
            bgmClip = Resources.Load<AudioClip>("Audio/bgc");
            level1Clip = Resources.Load<AudioClip>("Audio/level1");
            level2Clip = Resources.Load<AudioClip>("Audio/level2");
            attack1Clip = Resources.Load<AudioClip>("Audio/attack1");
            attack2Clip = Resources.Load<AudioClip>("Audio/attack2");
            blockClip = Resources.Load<AudioClip>("Audio/block");
            hurtClip = Resources.Load<AudioClip>("Audio/hurt");
            jumpClip = Resources.Load<AudioClip>("Audio/jump");
            labanClip = Resources.Load<AudioClip>("Audio/laban");
            tiktikClip = Resources.Load<AudioClip>("Audio/tiktik");
            pwestoClip = Resources.Load<AudioClip>("Audio/pwesto");
            panaloPulaClip = Resources.Load<AudioClip>("Audio/panalo pula");
            walangPanaloClip = Resources.Load<AudioClip>("Audio/walangPanalo");
        }

        public void PlayBGM()
        {
            PlayMusic(bgmClip);
        }

        public void PlayLevel1()
        {
            PlayMusic(level1Clip);
        }

        public void PlayLevel2()
        {
            PlayMusic(level2Clip);
        }

        public void PlayMusic(AudioClip clip)
        {
            if (clip == null) return;
            if (musicSource.clip == clip && musicSource.isPlaying) return;
            musicSource.clip = clip;
            musicSource.Play();
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip != null && sfxSource != null)
                sfxSource.PlayOneShot(clip);
        }

        public void PlayAttack1() => PlaySFX(attack1Clip);
        public void PlayAttack2() => PlaySFX(attack2Clip);
        public void PlayBlock() => PlaySFX(blockClip);
        public void PlayHurt() => PlaySFX(hurtClip);
        public void PlayJump() => PlaySFX(jumpClip);
        public void PlayLaban() => PlaySFX(labanClip);
        public void PlayPwesto() => PlaySFX(pwestoClip);
        public void PlayRedWin() => PlaySFX(panaloPulaClip);
        public void PlayDraw() => PlaySFX(walangPanaloClip);

        public void PlayTimerTick(int currentSecond)
        {
            if (currentSecond != lastTimerSecond)
            {
                lastTimerSecond = currentSecond;
                PlaySFX(tiktikClip);
            }
        }

        public void StopMusic()
        {
            if (musicSource != null) musicSource.Stop();
        }

        public void PauseMusic()
        {
            if (musicSource != null && musicSource.isPlaying) musicSource.Pause();
        }

        public void ResumeMusic()
        {
            if (musicSource != null && !musicSource.isPlaying) musicSource.UnPause();
        }
    }
}
