using TMPro;
using UnityEngine;
using System.Collections;

public class GameCountdownManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI textMesh;

    [Header("Header Config")]
    [SerializeField] private string headerMessage;
    [SerializeField] private float headerFontSize;
    [SerializeField] private Color headerColor;

    [Header("Timer Config")]
    [SerializeField] private float timerFontSize;
    [SerializeField] private Color timerColor;

    [Header("Start Config")]
    [SerializeField] private string startMessage;
    [SerializeField] private float startFontSize;
    [SerializeField] private Color startColor;

    private void Start()
    {
        if (textMesh != null)
        {
            textMesh.fontSize = headerFontSize;
            textMesh.color = headerColor;
            textMesh.SetText(headerMessage);
        }
        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.OnCountdownTick += ShowCountdownText;
            RaceManager.Instance.OnRaceStart += ShowGoText;
        }
    }

    private void OnDestroy()
    {
        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.OnCountdownTick -= ShowCountdownText;
            RaceManager.Instance.OnRaceStart -= ShowGoText;
        }
    }

    private void ShowCountdownText(int time)
    {
        textMesh.fontSize = timerFontSize;
        textMesh.color = timerColor;
        textMesh.SetText(time.ToString());
    }

    private void ShowGoText()
    {
        StartCoroutine(HideUIRoutine());
    }

    private IEnumerator HideUIRoutine()
    {
        textMesh.fontSize = startFontSize;
        textMesh.color = startColor;
        textMesh.SetText(startMessage);
        yield return new WaitForSeconds(1f);
        textMesh.transform.root.gameObject.SetActive(false);
    }
}