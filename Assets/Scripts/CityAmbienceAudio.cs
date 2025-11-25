using UnityEngine;

public class CityAmbienceAudio : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    private bool _hasPlayed;

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // Make sure audio doesn't play on awake
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    void Update()
    {
        // Only play once the game has started
        if (!_hasPlayed && StartMenuController.Instance != null && StartMenuController.Instance.HasStarted)
        {
            PlayAmbience();
            _hasPlayed = true;
        }
    }

    void PlayAmbience()
    {
        if (audioSource != null)
        {
            audioSource.Play();
        }
    }
}

