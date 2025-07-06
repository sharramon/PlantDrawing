using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;

public class OpenAIImageGenerator : MonoBehaviour
{
    [SerializeField] private string picSize = "512x512";

    [TextArea(3, 10)]
    [SerializeField] private string prompt = "이 식물의 식생읠 정보들을 토대로 캐릭터화를 해줬으면 좋겠어. 그리고나서 그 특징을 가지고 지브리 스타일로 캐릭터를 그림으로 그려줘. DALL-E 를 써서 그림을 그려줘";

    [TextArea(3, 20)]
    [SerializeField] private string systemPrompt = "You are a character designer. You are given a prompt and you need to generate a character design. You need to generate a character design that is unique and creative. You need to generate a character design that is a mix of the prompt and the character design.";
    private void OnEnable()
    {
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        //PlantDataManager.Instance._onPlantScientificName += GenerateImage;
    }

    private void UnsubscribeEvents()
    {
        //PlantDataManager.Instance._onPlantScientificName -= GenerateImage;
    }

    public void GenerateImage(string scientificName)
    {
        string fullPrompt = $"Plant Name : {scientificName} {prompt}"; 
        //PlantDataManager.Instance.SetLoading("openai_image", true);
        CoroutineManager.Instance.Run("generate_image", GenerateCharacterImage(fullPrompt));
    }

    IEnumerator GenerateCharacterImage(string conceptPrompt)
    {
        // Step 1: GPT-4에게 프롬프트 생성 요청
        Task<string> gptTask = RequestPromptFromGPT(conceptPrompt);
        yield return new WaitUntil(() => gptTask.IsCompleted);

        string generatedPrompt = gptTask.Result;
        Debug.Log("🎯 GPT-Generated Prompt:\n" + generatedPrompt);

        // Step 2: DALL·E API로 이미지 생성
        yield return StartCoroutine(GenerateImageWithDalle(generatedPrompt));
    }

    async Task<string> RequestPromptFromGPT(string input)
    {
        string gptUrl = "https://api.openai.com/v1/chat/completions";

        var gptRequest = new
        {
            model = "gpt-4.1",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = input }
            },
            temperature = 0.9
        };

        string json = JsonConvert.SerializeObject(gptRequest);

        using (UnityWebRequest www = UnityWebRequest.PostWwwForm(gptUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", "Bearer " + ChatGPTManager.Instance.ApiKey);

            var operation = www.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("GPT 요청 실패: " + www.error);
                return "";
            }

            var responseJson = www.downloadHandler.text;
            var result = JsonConvert.DeserializeObject<GPTResponse>(responseJson);
            return result.choices[0].message.content.Trim();
        }
    }

    private IEnumerator GenerateImageWithDalle(string prompt)
    {
        string url = "https://api.openai.com/v1/images/generations";

        // Create JSON body
        string jsonBody = JsonUtility.ToJson(new ImageGenerationRequest
        {
            model = "dall-e-3",
            quality = "standard",
            prompt = prompt,
            n = 1,
            size = picSize
        });

        // Create request
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + ChatGPTManager.Instance.ApiKey);

        // Send request
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("OpenAI request failed: " + request.error);
            //PlantDataManager.Instance.ReportError("Failed to generate image: " + request.error);
        }
        else
        {
            Debug.Log("OpenAI response: " + request.downloadHandler.text);
            // Parse image URL and download
            ImageGenerationResponse response = JsonUtility.FromJson<ImageGenerationResponse>(request.downloadHandler.text);
            yield return CoroutineManager.Instance.Run("download_image", DownloadImageCoroutine(response.data[0].url));
        }

        //PlantDataManager.Instance.SetLoading("openai_image", false);
    }

    private IEnumerator DownloadImageCoroutine(string imageUrl)
    {
        UnityWebRequest imageRequest = UnityWebRequestTexture.GetTexture(imageUrl);
        yield return imageRequest.SendWebRequest();

        if (imageRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Image download failed: " + imageRequest.error);
            //PlantDataManager.Instance.ReportError("Failed to download image: " + imageRequest.error);
        }
        else
        {
            Texture2D texture = ((DownloadHandlerTexture)imageRequest.downloadHandler).texture;
            //PlantDataManager.Instance._onPlantImageGenerated?.Invoke(texture);
        }
    }

    // Request body class
    [System.Serializable]
    public class ImageGenerationRequest
    {
        public string model;
        public string prompt;
        public string quality;
        public int n;
        public string size;
    }

    // Response class
    [System.Serializable]
    public class ImageGenerationResponse
    {
        public ImageData[] data;
    }

    [System.Serializable]
    public class ImageData
    {
        public string url;
    }

    
}

[System.Serializable]
public class GPTResponse
{
    public List<Choice> choices;
}

[System.Serializable]
public class Choice
{
    public Message message;
}

[System.Serializable]
public class Message
{
    public string role;
    public string content;
}