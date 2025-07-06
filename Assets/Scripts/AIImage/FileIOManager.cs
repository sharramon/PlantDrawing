using System.IO;
using UnityEngine;

public class FileIOManager : Singleton<FileIOManager>
{
#if UNITY_EDITOR
    private string savePath = "Assets/z_Dummy";
#else
    private string savePath = Application.persistentDataPath;
#endif
    private string chatDataPath = "chatData.json";
    public void SaveChatData(ChatData chatData)
    {
        string json = JsonUtility.ToJson(chatData);
        File.WriteAllText(Path.Combine(savePath, chatDataPath), json);

        this.PrintCustomLog($"SaveChatData: {json}");
    }

    public void LoadChatData()
    {
        string path = Path.Combine(savePath, chatDataPath);

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            //JsonUtility.FromJsonOverwrite(json, ChatManager.Instance.chatData);
        }
        else
        {
            this.PrintCustomLog($"LoadChatData: {path} not found");
        }
    }
}




