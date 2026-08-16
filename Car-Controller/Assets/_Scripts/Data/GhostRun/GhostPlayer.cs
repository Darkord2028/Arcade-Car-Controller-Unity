using UnityEngine;

public class GhostPlayer : MonoBehaviour
{
    [Header("Ghost Asset")]
    public GhostRunData_SO ghostData;

    [Header("Visuals")]
    public TrailRenderer[] driftTrails;

    private float _playbackTime;
    private int _currentIndex = 0;
    public bool isPlaying { get; private set; }

    void Start()
    {
        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.OnRaceStart += StartPlayback;
        }
    }

    private void OnDestroy()
    {
        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.OnRaceStart -= StartPlayback;
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
        if (!isPlaying) return;

        _playbackTime += Time.deltaTime;

        if (_currentIndex < ghostData.frames.Count - 2)
        {
            if (_playbackTime >= ghostData.frames[_currentIndex + 1].time)
            {
                _currentIndex++;
            }
        }
        else
        {
            isPlaying = false;
            return;
        }

        GhostFrame frameA = ghostData.frames[_currentIndex];
        GhostFrame frameB = ghostData.frames[_currentIndex + 1];

        float interpolationFactor = (_playbackTime - frameA.time) / (frameB.time - frameA.time);

        transform.position = Vector3.Lerp(frameA.position, frameB.position, interpolationFactor);
        transform.rotation = Quaternion.Slerp(frameA.rotation, frameB.rotation, interpolationFactor);

        bool isDrifting = frameA.isDrifting;
        foreach (var trail in driftTrails)
        {
            if (trail != null) trail.emitting = isDrifting;
        }
    }
}