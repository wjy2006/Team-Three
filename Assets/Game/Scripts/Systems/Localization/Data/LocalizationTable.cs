using UnityEngine;

[CreateAssetMenu(menuName = "Game/Localization/Localization Table")]
public class LocalizationTable : ScriptableObject
{
    public TextAsset csv;      // zh-CN.csv 或 en-US.csv
    public string locale = "zh-CN";
}
