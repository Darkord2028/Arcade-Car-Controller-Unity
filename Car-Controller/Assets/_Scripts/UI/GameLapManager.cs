using System.Collections;
using TMPro;
using UnityEngine;

public class GameLapManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI lapCountText;
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private RaceParticipant playerParticipant;

    [Header("Text")]
    [SerializeField] private string wrongDirectionText = "Missed a Checkpoint!";
    [SerializeField] private string checkpointReachedText = "Checkpoint Completed!";
    [SerializeField] private string lapCompleteText = "Lap Completed!";
    [SerializeField] private float infoShowTime = 1.5f;

    private RaceManager _raceManager;

    private int _totalLaps;
    private int _currentLap;
    private Coroutine _infoCoroutine;

    private void Start()
    {
        _raceManager = RaceManager.Instance;
        _totalLaps = _raceManager.TotalLaps;

        infoText.transform.root.gameObject.SetActive(false);

        _currentLap = playerParticipant.CurrentLap;
        UpdateLapText();
    }

    private void OnEnable()
    {
        playerParticipant.OnCheckpointPassed += HandleCheckpointReached;
        playerParticipant.OnLapCompleted += HandleLapCompleted;
        playerParticipant.OnWrongDirection += HandleWrongDirection;
    }

    private void OnDisable()
    {
        playerParticipant.OnCheckpointPassed -= HandleCheckpointReached;
        playerParticipant.OnLapCompleted -= HandleLapCompleted;
        playerParticipant.OnWrongDirection -= HandleWrongDirection;
    }

    private void HandleCheckpointReached(int checkpointIndex)
    {
        if (checkpointIndex != 0)
        {
            ShowMessage(checkpointReachedText);
        }
    }

    private void HandleLapCompleted(int lapCount)
    {
        _currentLap = lapCount;
        UpdateLapText();

        if (_currentLap > 1)
        {
            ShowMessage(lapCompleteText);
        }
    }

    private void HandleWrongDirection()
    {
        ShowMessage(wrongDirectionText);
    }

    private void UpdateLapText()
    {
        lapCountText.text = $"{_currentLap}/{_totalLaps}";
    }

    private void ShowMessage(string message)
    {
        infoText.text = message;

        if (_infoCoroutine != null)
        {
            StopCoroutine(_infoCoroutine);
        }

        _infoCoroutine = StartCoroutine(ShowInfoUI(infoShowTime));
    }

    private IEnumerator ShowInfoUI(float waitSec)
    {
        infoText.transform.root.gameObject.SetActive(true);
        yield return new WaitForSeconds(waitSec);
        infoText.transform.root.gameObject.SetActive(false);
    }
}