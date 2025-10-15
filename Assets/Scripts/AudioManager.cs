using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("---------Audio Source----------")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource SFXSource;

    [Header("---------Audio Clip----------")]
    public AudioClip backGround;
    public AudioClip Jump;

    private void Start()
    {
        musicSource.clip = backGround;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        SFXSource.clip = clip;
        SFXSource.PlayOneShot(clip); 
    }
}
