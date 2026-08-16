using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GhostRunRecorder : MonoBehaviour
{
    [Header("Settings")]
    public string trackName = "Track1";
    public float recordInterval = 0.1f;

    private ArcadeController _carController;
    private RaceParticipant _raceParticipant;
    private GhostRunData_SO _currentRun;
    private float _timer;
    private float _recordingTime;

    public bool isRecording { get; private set; }

    void Start()
    {
        _carController = GetComponent<ArcadeController>();
        _raceParticipant = GetComponent<RaceParticipant>();

        if (_raceParticipant != null)
        {
            _raceParticipant.OnRaceFinished += StopAndSaveRecording;
        }

        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.OnRaceStart += StartRecording;
        }
    }

    private void OnDestroy()
    {
        if (_raceParticipant != null)
        {
            _raceParticipant.OnRaceFinished -= StopAndSaveRecording;
        }
        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.OnRaceStart -= StartRecording;
        }
    }

    public void StartRecording()
    {
        _currentRun = ScriptableObject.CreateInstance<GhostRunData_SO>();
        _currentRun.trackName = trackName;
        _currentRun.recordDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        _timer = 0f;
        _recordingTime = 0f;
        isRecording = true;

        Debug.Log("Ghost recording started!");
    }

    public void StopAndSaveRecording()
    {
        isRecording = false;
        _currentRun.totalTime = _recordingTime;

#if UNITY_EDITOR
        // Ensure the folder exists
        if (!AssetDatabase.IsValidFolder("Assets/GhostRuns"))
        {
            AssetDatabase.CreateFolder("Assets", "GhostRuns");
        }

        // Format: TrackName_yyyy-MM-dd_HH-mm-ss
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string assetPath = $"Assets/GhostRuns/{trackName}_{timestamp}.asset";

        // Save the ScriptableObject directly into the project hierarchy
        AssetDatabase.CreateAsset(_currentRun, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Ghost saved directly to project at: {assetPath}");
#else
        Debug.LogWarning("Ghost recording saving is only supported in the Unity Editor for prototyping.");
#endif
    }

    void Update()
    {
        if (!isRecording) return;

        _recordingTime += Time.deltaTime;
        _timer += Time.deltaTime;

        if (_timer >= recordInterval)
        {
            _timer = 0f;
            RecordFrame();
        }
    }

    private void RecordFrame()
    {
        GhostFrame newFrame = new GhostFrame
        {
            time = _recordingTime,
            position = transform.position,
            rotation = transform.rotation,
            isDrifting = _carController.IsDrifting
        };

        _currentRun.frames.Add(newFrame);
    }
}