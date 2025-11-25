using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class MenuVideoPlayer : MonoBehaviour
{
    [Header("Video Setup")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage displayImage; // The UI RawImage to display the video on
    
    [Header("Video Source (choose one)")]
    [SerializeField] private VideoClip videoClip;
    [SerializeField] private string videoURL; // Alternative: stream from URL
    
    [Header("Playback Options")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool loop = true;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    
    [Header("Optional Controls")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button stopButton;
    
    private RenderTexture renderTexture;

    void Start()
    {
        SetupVideoPlayer();
        
        if (playOnStart)
        {
            Play();
        }
        
        // Hook up button controls if assigned
        if (playButton) playButton.onClick.AddListener(Play);
        if (pauseButton) pauseButton.onClick.AddListener(Pause);
        if (stopButton) stopButton.onClick.AddListener(Stop);
    }

    void SetupVideoPlayer()
    {
        if (!videoClip && string.IsNullOrEmpty(videoURL))
        {
            Debug.LogError("[MenuVideoPlayer] No video source! Please assign either a VideoClip or a URL.");
            return;
        }

        if (!videoPlayer)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }

        // Configure video player
        videoPlayer.playOnAwake = false;
        
        // Choose source: URL takes priority if both are set
        if (!string.IsNullOrEmpty(videoURL))
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = videoURL;
            Debug.Log($"[MenuVideoPlayer] Loading video from URL: {videoURL}");
        }
        else
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = videoClip;
            Debug.Log($"[MenuVideoPlayer] Loading VideoClip: {videoClip.name}");
        }
        
        videoPlayer.isLooping = loop;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        
        // Setup audio
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.controlledAudioTrackCount = 1;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetDirectAudioVolume(0, volume);
        
        // Subscribe to error events
        videoPlayer.errorReceived += OnVideoError;
        videoPlayer.prepareCompleted += OnVideoPrepared;
        
        // Create render texture for the video
        if (displayImage)
        {
            renderTexture = new RenderTexture(1920, 1080, 0);
            videoPlayer.targetTexture = renderTexture;
            displayImage.texture = renderTexture;
        }
        else
        {
            Debug.LogError("[MenuVideoPlayer] No RawImage assigned to display the video!");
        }
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogError($"[MenuVideoPlayer] Video error: {message}\n" +
                       $"Video file may be corrupted or use an unsupported codec.\n" +
                       $"Try re-encoding the video to H.264 with AAC audio.");
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        Debug.Log("[MenuVideoPlayer] Video prepared and ready to play!");
    }

    public void Play()
    {
        if (videoPlayer && (videoClip || !string.IsNullOrEmpty(videoURL)))
        {
            videoPlayer.Play();
        }
    }

    public void Pause()
    {
        if (videoPlayer)
        {
            videoPlayer.Pause();
        }
    }

    public void Stop()
    {
        if (videoPlayer)
        {
            videoPlayer.Stop();
        }
    }

    public void SetVolume(float vol)
    {
        volume = Mathf.Clamp01(vol);
        if (videoPlayer)
        {
            videoPlayer.SetDirectAudioVolume(0, volume);
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from video events
        if (videoPlayer)
        {
            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.prepareCompleted -= OnVideoPrepared;
        }

        // Clean up render texture
        if (renderTexture)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
        
        // Unsubscribe from buttons
        if (playButton) playButton.onClick.RemoveListener(Play);
        if (pauseButton) pauseButton.onClick.RemoveListener(Pause);
        if (stopButton) stopButton.onClick.RemoveListener(Stop);
    }

    void OnDisable()
    {
        // Stop video when menu is hidden
        if (videoPlayer && videoPlayer.isPlaying)
        {
            Stop();
        }
    }

    void OnEnable()
    {
        // Resume video when menu is shown again (if playOnStart is true)
        if (playOnStart && videoPlayer && !videoPlayer.isPlaying)
        {
            Play();
        }
    }
}

