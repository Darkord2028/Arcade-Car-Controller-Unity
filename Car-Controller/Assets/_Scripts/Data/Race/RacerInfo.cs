using TMPro;
using UnityEngine;

public class RacerInfo : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI positionText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI timeText;

    public void SetUI(RacerResult result)
    {
        positionText.SetText(result.racerPosition.ToString());
        nameText.SetText(result.racerName);
        timeText.SetText(FormatTime(result.finalTime));
    }

    private string FormatTime(float time)
    {
        int minutes = (int)(time / 60f);
        int seconds = (int)(time % 60f);
        int fraction = (int)((time * 100f) % 100f);
        return string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, fraction);
    }
}
