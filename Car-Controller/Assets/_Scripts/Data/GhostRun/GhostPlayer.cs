using UnityEngine;

public class GhostPlayer : MonoBehaviour
{
    [Header("Config")]
    public string playerName = "Jack";

    [Header("Ghost Asset")]
    public GhostRunData_SO ghostData;

    [Header("Visuals")]
    public TrailRenderer[] driftTrails;

    private float _playbackTime;
    private int _currentIndex = 0;
    public bool isPlaying { get; private set; }
    public float TotalTime { get { return ghostData.totalTime; } }

    private RaceManager _raceManager;

    private void Awake()
    {
        _raceManager = RaceManager.Instance;
    }

    void Start()
    {
        if (_raceManager != null)
        {
            _raceManager.OnRaceStart += StartPlayback;
        }
    }

    private void OnDestroy()
    {
        if (_raceManager != null)
        {
            _raceManager.OnRaceStart -= StartPlayback;
        }
    }

    public void StartPlayback()
    {
        if (ghostData == null || ghostData.frames.Count < 2)
        {
            Debug.LogWarning("No valid ghost data assigned to " + gameObject.name);
            return;
        }

        _playbackTime = 0f;
        _currentIndex = 0;
        isPlaying = true;
    }

    void Update()
    {
        if (!isPlaying || ghostData == null || ghostData.frames.Count < 2) return;

        _playbackTime += Time.deltaTime;

        while (_currentIndex < ghostData.frames.Count - 1 && _playbackTime >= ghostData.frames[_currentIndex + 1].time)
        {
            _currentIndex++;
        }

        if (_currentIndex >= ghostData.frames.Count - 1)
        {
            transform.position = ghostData.frames[ghostData.frames.Count - 1].position;
            transform.rotation = ghostData.frames[ghostData.frames.Count - 1].rotation;

            foreach (var trail in driftTrails) { if (trail != null) trail.emitting = false; }
            isPlaying = false;
            return;
        }

        GhostFrame frameA = ghostData.frames[_currentIndex];
        GhostFrame frameB = ghostData.frames[_currentIndex + 1];

        float timeDiff = frameB.time - frameA.time;
        float interpolationFactor = (timeDiff > 0f) ? (_playbackTime - frameA.time) / timeDiff : 1f;

        transform.position = Vector3.Lerp(frameA.position, frameB.position, interpolationFactor);
        transform.rotation = Quaternion.Slerp(frameA.rotation, frameB.rotation, interpolationFactor);

        bool isDrifting = frameA.isDrifting;
        foreach (var trail in driftTrails)
        {
            if (trail != null) trail.emitting = isDrifting;
        }
    }
}