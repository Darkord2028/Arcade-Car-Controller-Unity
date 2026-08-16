using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Ghost Run", menuName = "Racing/Data/Ghost Run Data")]
public class GhostRunData_SO : ScriptableObject
{
    public string trackName = "Track_1";
    public string recordDate;
    public float totalTime;

    public List<GhostFrame> frames = new List<GhostFrame>();

    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }
}
