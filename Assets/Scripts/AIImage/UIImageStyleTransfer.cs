using UnityEngine;
using UnityEngine.UI;

public class UIImageStyleTransfer : MonoBehaviour
{
    public Image sourceImage;
    public Image targetImage;
    public DeepAIStyleTransferClient transferClient;

    public void StartTransfer()
    {
        if (sourceImage.sprite == null)
        {
            Debug.LogWarning("Source sprite is missing.");
            return;
        }

        Texture2D tex = ToReadableTexture(sourceImage.sprite.texture);
        transferClient.Stylize(tex, styled =>
        {
            Sprite styledSprite = Sprite.Create(styled, new Rect(0, 0, styled.width, styled.height), Vector2.one * 0.5f);
            targetImage.sprite = styledSprite;
        });
    }

    private Texture2D ToReadableTexture(Texture2D source)
    {
        RenderTexture rt = RenderTexture.GetTemporary(
            source.width,
            source.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear);

        Graphics.Blit(source, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        readable.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return readable;
    }

    
}
