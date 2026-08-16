using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public enum RaceState
{
    PreRace,
    Countdown,
    Racing,
    Finished
}

public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance;

    [Header("Race Settings")]
    [SerializeField] private int totalLaps = 3;
    [SerializeField] private int waitSecondsBeforeCountdown = 1;
    [SerializeField] private int countdownSeconds = 3;

    [Header("Track Data")]
    [SerializeField] private Transform checkpointParent;
    [SerializeField] private List<Checkpoint> trackCheckpoints = new List<Checkpoint>();

    public int TotalCheckPoints { get { return trackCheckpoints.Count; } set { value = trackCheckpoints.Count; } }
    public int TotalLaps { get { return totalLaps; } set { value = totalLaps; } }

    public RaceState CurrentState { get; private set; } = RaceState.PreRace;

    public event Action<int> OnCountdownTick;
    public event Action OnRaceStart;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        SetupCheckpoints();
    }

    private void Start()
    {
        StartCoroutine(RaceCountdownRoutine());
    }

    private void SetupCheckpoints()
    {
        int index = 0;
        foreach (Transform child in checkpointParent)
        {
            Checkpoint cp = child.GetComponent<Checkpoint>();
            if (cp != null)
            {
                cp.CheckpointIndex = index;
                trackCheckpoints.Add(cp);
                index++;
            }
        }
        Debug.Log($"Track loaded with {trackCheckpoints.Count} checkpoints.");
    }

    private IEnumerator RaceCountdownRoutine()
    {
        CurrentState = RaceState.Countdown;
        yield return new WaitForSeconds(waitSecondsBeforeCountdown);

        for (int i = countdownSeconds; i > 0; i--)
        {
            OnCountdownTick?.Invoke(i);
            Debug.Log($"Countdown: {i}...");
            yield return new WaitForSeconds(1f);
        }

        CurrentState = RaceState.Racing;
        OnRaceStart?.Invoke();
        Debug.Log("GO!");
    }
}