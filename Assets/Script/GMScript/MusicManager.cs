using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public AudioSource homeBGM;
    public AudioSource inGameBGM;
    public AudioSource gameOverBGM;

    void Start()
    {
        PlayHomeMusic();
    }

    public void PlayHomeMusic()
    {
        StopAllMusic();

        if (homeBGM != null)
        {
            homeBGM.Play();
        }
    }

    public void PlayInGameMusic()
    {
        StopAllMusic();

        if (inGameBGM != null)
        {
            inGameBGM.Play();
        }
    }

    public void PlayGameOverMusic()
    {
        StopAllMusic();

        if (gameOverBGM != null)
        {
            gameOverBGM.Play();
        }
    }

    public void StopAllMusic()
    {
        if (homeBGM != null)
        {
            homeBGM.Stop();
        }

        if (inGameBGM != null)
        {
            inGameBGM.Stop();
        }

        if (gameOverBGM != null)
        {
            gameOverBGM.Stop();
        }
    }
}