using System;
using UnityEngine;

public class RaceParticipant : MonoBehaviour
{
    [Header("Race Progress")]
    [SerializeField] private int currentLap = 1;
    [SerializeField] private int targetCheckpointIndex = 0;
    [SerializeField] private bool hasFinishedRace = false;

    private int _checkpointsPassed = 0;

    public event Action<int> OnCheckpointPassed;
    public event Action<int> OnLapCompleted;
    public event Action OnRaceFinished;
    public event Action OnWrongDirection;

    public int CurrentCheckpoint { get { return targetCheckpointIndex; } set { targetCheckpointIndex = value; } }
    public Vector3 LastCheckpointPosition { get; private set; }
    public Quaternion LastCheckpointRotation { get; private set; }

    public void UpdateCheckpoint(Checkpoint checkpoint)
    {
        if (hasFinishedRace) return;

        if (checkpoint != null)
        {
            CheckProgress(checkpoint);
        }
    }

    private void CheckProgress(Checkpoint hitCheckpoint)
    {
        int hitIndex = hitCheckpoint.CheckpointIndex;
        int totalCheckpoints = RaceManager.Instance.TotalCheckPoints;

        if (hitIndex == targetCheckpointIndex)
        {
            LastCheckpointPosition = hitCheckpoint.CheckpointSpawnPosition;
            LastCheckpointRotation = hitCheckpoint.CheckpointSpawnRotation;

            OnCheckpointPassed?.Invoke(hitIndex);
            _checkpointsPassed++;

            targetCheckpointIndex++;
            if (targetCheckpointIndex >= totalCheckpoints)
            {
                targetCheckpointIndex = 0;
            }

            if (hitIndex == 0 && _checkpointsPassed > 1)
            {
                CompleteLap();
            }
        }
        else
        {
            int expectedPrevious = (targetCheckpointIndex == 0) ? totalCheckpoints - 1 : targetCheckpointIndex - 1;

            if (hitIndex != expectedPrevious)
            {
                OnWrongDirection?.Invoke();
                Debug.LogWarning($"Wrong Way! You hit checkpoint {hitIndex}, but you need to head to checkpoint {targetCheckpointIndex}");
            }
        }
    }

    private void CompleteLap()
    {
        if (currentLap >= RaceManager.Instance.TotalLaps)
        {
            hasFinishedRace = true;
            OnRaceFinished?.Invoke();
            Debug.Log("Race Finished! You win!");
        }
        else
        {
            currentLap++;
            OnLapCompleted?.Invoke(currentLap);
            Debug.Log($"Lap {currentLap} started!");
        }
    }
}