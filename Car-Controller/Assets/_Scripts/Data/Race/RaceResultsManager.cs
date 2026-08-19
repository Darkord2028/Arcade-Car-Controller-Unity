using System.Collections.Generic;
using TMPro;
using UnityEngine;

public struct RacerResult
{
    public int racerPosition;
    public string racerName;
    public float finalTime;
    public bool isPlayer;

    public RacerResult(
        int position,
        string name,
        float time,
        bool player)
    {
        racerPosition = position;
        racerName = name;
        finalTime = time;
        isPlayer = player;
    }
}

public class RaceResultsManager : MonoBehaviour
{
    [Header("Participants")]
    [SerializeField] private GhostPlayer goldGhost;
    [SerializeField] private GhostPlayer silverGhost;

    [Header("Results UI")]
    [SerializeField] private Transform resultsContent;
    [SerializeField] private RacerInfo playerResultPrefab;
    [SerializeField] private TextMeshProUGUI winnerDecText;

    private RaceManager _raceManager;

    private float _goldTime;
    private float _silverTime;
    private float _playerTime;

    private void Awake()
    {
        _raceManager = RaceManager.Instance;
    }

    private void Start()
    {
        if (_raceManager != null)
        {
            _raceManager.OnRaceFinished += EndRaceAndCalculateWinner;
        }

        if (resultsContent != null) resultsContent.root.gameObject.SetActive(false);
        if (winnerDecText != null) winnerDecText.transform.root.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_raceManager != null)
        {
            _raceManager.OnRaceFinished -= EndRaceAndCalculateWinner;
        }
    }

    private void EndRaceAndCalculateWinner()
    {
        _playerTime = _raceManager.TotalTime;

        _goldTime = goldGhost != null ? goldGhost.TotalTime : float.MaxValue;
        _silverTime = silverGhost != null ? silverGhost.TotalTime : float.MaxValue;

        List<RacerResult> results = new List<RacerResult>
        {
            new RacerResult(0, "Player", _playerTime, true),
            new RacerResult(0, goldGhost.playerName, _goldTime, false),
            new RacerResult(0, silverGhost.playerName, _silverTime, false)
        };

        results.Sort((a, b) => a.finalTime.CompareTo(b.finalTime));

        for (int i = 0; i < results.Count; i++)
        {
            RacerResult result = results[i];

            result.racerPosition = i + 1;

            results[i] = result;
        }

        UpdateResultsUI(results);

        DeterminePlayerResult(results);
    }

    private void DeterminePlayerResult(List<RacerResult> results)
    {
        RacerResult playerResult = results.Find(result => result.isPlayer);

        if (playerResult.racerPosition <= 2)
        {
            _raceManager.HandlePlayerWin(playerResult.racerPosition);
            winnerDecText.SetText("You Won!");
            winnerDecText.transform.root.gameObject.SetActive(true);
        }
        else
        {
            _raceManager.HandlePlayerLost(playerResult.racerPosition);
            winnerDecText.SetText("You Lost!");
            winnerDecText.transform.root.gameObject.SetActive(true);
        }
    }

    private void UpdateResultsUI(List<RacerResult> results)
    {
        foreach (Transform child in resultsContent)
        {
            Destroy(child.gameObject);
        }

        foreach (RacerResult result in results)
        {
            RacerInfo racerInfo = Instantiate(
                playerResultPrefab,
                resultsContent
            );

            racerInfo.SetUI(result);
        }

        resultsContent.root.gameObject.SetActive(true);
    }
}