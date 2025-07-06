using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ReplicateImg2ImgClient : MonoBehaviour
{
    [TextArea(3, 5)]
    public string prompt = "A vibrant, emotionally charged oil painting in the style of Vincent van Gogh. Thick, swirling brushstrokes. Intense color contrast. Emphasis on motion and energy. Post-Impressionist style. Natural light. Deep texture and movement. Bright skies, expressive shapes.";
    [SerializeField] private string replicateToken = "r8_9qZyb3sgxuT4xdMWY4Ql9fWAgOhsuAY0St9tG";

    private const string apiUrl = "https://api.replicate.com/v1/predictions";
    private const string modelVersion = "15a3689ee13b0d2616e98820eca31d4c3abcd36672df6afce5cb6feb1d66087d";

    // Loading state management
    public bool isLoading = false;
    public event Action<bool> OnLoadingStateChanged;
    public event Action<Texture2D> OnStylizedImageComplete;
    public event Action OnStylizedImageFailed;

    public void StylizeImage(Texture2D tex, Action<Texture2D> onComplete)
    {
        if (isLoading)
        {
            Debug.LogWarning("Already processing an image. Please wait.");
            return;
        }
        
        SetLoading(true);
        StartCoroutine(SendToReplicate(tex, onComplete));
    }

    private void SetLoading(bool loading)
    {
        isLoading = loading;
        OnLoadingStateChanged?.Invoke(loading);
    }

    private IEnumerator SendToReplicate(Texture2D tex, Action<Texture2D> onComplete)
    {
        Texture2D readableTex = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
        RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        Graphics.Blit(tex, rt);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        readableTex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
        readableTex.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        byte[] pngBytes = readableTex.EncodeToPNG();
        string base64 = Convert.ToBase64String(pngBytes);
        string imageDataUrl = $"data:image/png;base64,{base64}";

        string json = $@"{{
            ""version"": ""{modelVersion}"",
            ""input"": {{
                ""image"": ""{imageDataUrl}"",
                ""prompt"": ""{prompt}"",
                ""num_inference_steps"": 25,
                ""guidance_scale"": 7.5,
                ""strength"": 0.75
            }}
        }}";

        UnityWebRequest req = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", "Token " + replicateToken);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Replicate request failed: {req.error}");
            SetLoading(false);
            OnStylizedImageFailed?.Invoke();
            yield break;
        }

        string predictionId = JsonUtility.FromJson<PredictionResponse>(req.downloadHandler.text).id;

        string statusUrl = $"{apiUrl}/{predictionId}";
        while (true)
        {
            UnityWebRequest check = UnityWebRequest.Get(statusUrl);
            check.SetRequestHeader("Authorization", "Token " + replicateToken);
            yield return check.SendWebRequest();

            if (check.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to poll for result.");
                SetLoading(false);
                OnStylizedImageFailed?.Invoke();
                yield break;
            }

            PredictionResponse result = JsonUtility.FromJson<PredictionResponse>(check.downloadHandler.text);
            if (result.status == "succeeded" && result.output.Length > 0)
            {
                UnityWebRequest imgReq = UnityWebRequestTexture.GetTexture(result.output[0]);
                yield return imgReq.SendWebRequest();

                if (imgReq.result == UnityWebRequest.Result.Success)
                {
                    Texture2D resultTex = DownloadHandlerTexture.GetContent(imgReq);
                    SetLoading(false);
                    onComplete?.Invoke(resultTex);
                    OnStylizedImageComplete?.Invoke(resultTex);
                    yield break;
                }
            }

            if (result.status == "failed")
            {
                Debug.LogError("Generation failed.");
                SetLoading(false);
                OnStylizedImageFailed?.Invoke();
                yield break;
            }

            yield return new WaitForSeconds(1f);
        }
    }

    [Serializable]
    private class PredictionResponse
    {
        public string id;
        public string status;
        public string[] output;
    }
}
