using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class DeepAIStyleTransferClient : MonoBehaviour
{
    [SerializeField] private string apiKey = "your_deepai_api_key_here";
    [SerializeField] private Texture2D styleImage;

    public void Stylize(Texture2D contentImage, Action<Texture2D> onComplete)
    {
        StartCoroutine(UploadImages(contentImage, styleImage, onComplete));
    }

    private IEnumerator UploadImages(Texture2D content, Texture2D style, Action<Texture2D> onComplete)
    {
        if (content == null || style == null)
        {
            Debug.LogError("DeepAI: Content or style image is null.");
            yield break;
        }

        Debug.Log($"DeepAI: Content size = {content.width}x{content.height}, format = {content.format}, readable = {content.isReadable}");
        Debug.Log($"DeepAI: Style size = {style.width}x{style.height}, format = {style.format}, readable = {style.isReadable}");

        // Pixel sample debug
        try
        {
            Color sample = style.GetPixel(style.width / 2, style.height / 2);
            Debug.Log($"Style image sample color = {sample}");
        }
        catch
        {
            Debug.LogWarning("Style image is not readable — attempting to convert...");
        }

        Texture2D readableContent = ToReadableTexture(content);
        Texture2D readableStyle = ToReadableTexture(style);

        if (readableContent == null || readableStyle == null)
        {
            Debug.LogError("DeepAI: Could not make textures readable.");
            yield break;
        }

        byte[] contentBytes = readableContent.EncodeToPNG();
        byte[] styleBytes = readableStyle.EncodeToPNG();

        if (contentBytes == null || contentBytes.Length == 0 ||
            styleBytes == null || styleBytes.Length == 0)
        {
            Debug.LogError("DeepAI: Encoded PNGs are empty.");
            yield break;
        }

        // Save for inspection
        string path = Application.dataPath;
        File.WriteAllBytes(path + "/lastContent.png", contentBytes);
        File.WriteAllBytes(path + "/lastStyle.png", styleBytes);
        Debug.Log("DeepAI: Saved PNGs to disk for inspection.");

        // Upload to DeepAI
        WWWForm form = new WWWForm();
        form.AddBinaryData("content", contentBytes, "content.png", "image/png");
        form.AddBinaryData("style", styleBytes, "style.png", "image/png");

        UnityWebRequest request = UnityWebRequest.Post("https://api.deepai.org/api/neural-style", form);
        request.SetRequestHeader("api-key", apiKey);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Style transfer failed: {request.responseCode} — {request.error}");
            Debug.LogError("Server response: " + request.downloadHandler.text);
            yield break;
        }

        string resultUrl = JsonUtility.FromJson<ResultWrapper>(request.downloadHandler.text).output_url;
        Debug.Log("DeepAI: Stylized image URL: " + resultUrl);

        UnityWebRequest imageRequest = UnityWebRequestTexture.GetTexture(resultUrl);
        yield return imageRequest.SendWebRequest();

        if (imageRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to download stylized image: " + imageRequest.error);
            yield break;
        }

        Texture2D resultTexture = DownloadHandlerTexture.GetContent(imageRequest);
        onComplete?.Invoke(resultTexture);
    }

    private Texture2D ToReadableTexture(Texture2D source)
    {
        if (source == null) return null;

        RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0,
            RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        Graphics.Blit(source, rt);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        readable.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return readable;
    }

    [Serializable]
    private class ResultWrapper
    {
        public string id;
        public string output_url;
    }
}
