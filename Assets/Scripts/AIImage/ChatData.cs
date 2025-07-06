using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChatData", menuName = "ScriptableObjects/ChatData", order = 1)]
public class ChatData: ScriptableObject
{
    public List<ChatItemData> chatItemDatas = new List<ChatItemData>();
}


[System.Serializable]
public struct ChatItemData
{
    public enum RoleType
    {
        unknown,
        system,
        user,
        assistant
    }
    public RoleType roleType;
    public string message;
    public string time;
}
