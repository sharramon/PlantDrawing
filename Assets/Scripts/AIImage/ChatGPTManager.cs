using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using UniRx;
using System;

public class ChatGPTManager : Singleton<ChatGPTManager>
{
    [Serializable]
    public class ChatGPTResponse
    {
        public Choice[] choices;
    }

    [Serializable]
    public class Choice
    {
        public Message message;
    }

    [Serializable]
    public class Message
    {
        public string role;
        public string content;
    }
    [Serializable]
    public class RequestBody
    {
        public string model = "gpt-3.5-turbo";
        public List<Message> messages = new List<Message>();
    }


    [SerializeField] private string apiKey = "";
    public string ApiKey => apiKey;

    public IObservable<ChatItemData> SendMessageToGPT(ChatItemData userInput)
    {
        return Observable.FromCoroutine<ChatItemData>((observer) => SendRequest(userInput, observer));
    }

    private IEnumerator<object> SendRequest(ChatItemData userInput, IObserver<ChatItemData> observer)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            observer.OnError(new Exception("API Key is not set"));
            yield break;
        }
        else
        {
            this.PrintCustomLog($"API Key: {apiKey}");
        }

        string endpoint = "https://api.openai.com/v1/chat/completions";

        // RequestBody 객체 생성
        RequestBody requestBody = new RequestBody();

        // 이전 대화 내용 추가
        // foreach (var chatItem in ChatManager.Instance.chatData.chatItemDatas)
        // {
        //     requestBody.messages.Add(new Message
        //     {
        //         role = chatItem.roleType.ToString(),
        //         content = chatItem.message
        //     });
        // }

        // 현재 입력 추가
        requestBody.messages.Add(new Message
        {
            role = userInput.roleType.ToString(),
            content = userInput.message
        });

        string jsonBody = JsonUtility.ToJson(requestBody);
        this.PrintCustomLog($"Request Body: {jsonBody}");

        var request = new UnityWebRequest(endpoint, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        this.PrintCustomLog($"Request: {request.url}");

        //ChatManager.Instance.chatData.chatItemDatas.Add(userInput);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            string errorResponse = request.downloadHandler.text;
            Debug.LogError("응답 본문: " + errorResponse);
            observer.OnError(new Exception($"ChatGPT Error: {request.responseCode} - {errorResponse}"));
        }
        else
        {
            string responseJson = request.downloadHandler.text;
            this.PrintCustomLog($"Response: {responseJson}");
            ChatGPTResponse chatGPTResponse = JsonUtility.FromJson<ChatGPTResponse>(responseJson);
            
            ChatItemData responseData = new ChatItemData
            {
                message = chatGPTResponse.choices[0].message.content,
                roleType = ChatItemData.RoleType.assistant,
                time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            // ChatManager.Instance.chatData.chatItemDatas.Add(responseData);
            // FileIOManager.Instance.SaveChatData(ChatManager.Instance.chatData);

            observer.OnNext(responseData);
            observer.OnCompleted();
        }

        request.Dispose();
    }
}