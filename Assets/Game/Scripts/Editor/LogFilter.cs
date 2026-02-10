using UnityEngine;

public class LogFilter : MonoBehaviour
{
    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (logString.Contains("m_GameObjects.find") ||
            logString.Contains("m_Hierarchies.find"))
        {
            return;
        }

        Debug.unityLogger.Log(type, logString);
    }
}
