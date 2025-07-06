using UnityEngine;

public static class Extention
{
    public static void PrintCustomLog(this MonoBehaviour _mono, string _log)
    {
        Debug.Log($"[CustomLog] :: {_log}");
    }

    public static void PrintCustomLog(this MonoBehaviour _mono, string _log, Color _color)
    {
        Debug.Log($"<color={_color}>[CustomLog] :: {_log}</color>");
    }
}