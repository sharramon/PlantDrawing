using UnityEngine;
using UnityEngine.UI;

public class VanGoghUIImageTransfer_Replicate : MonoBehaviour
{
    public Image sourceImage;
    public Image targetImage;
    public ReplicateImg2ImgClient replicateClient;

    public void StartTransfer()
    {
        if (sourceImage.sprite == null)
        {
            Debug.LogWarning("Source sprite is null.");
            return;
        }

        Texture2D tex = sourceImage.sprite.texture;
        replicateClient.StylizeImage(tex, 
            result => {
                if (result != null)
                {
                    OnStylizedImageComplete(result);
                }
                else
                {
                    OnStylizedImageFailed();
                }
            }
        );
    }

    public void SetImages(Image source, Image target) {
        sourceImage = source;
        targetImage = target;
    }

    private void OnStylizedImageFailed()
    {
        // Re-enable camera for next photo even if AI processing failed
        CaptureManager.Instance.MakeCameraSnapshot(false);
        
        Debug.Log("AI processing failed, camera re-enabled");
    }

    private void OnStylizedImageComplete(Texture2D stylizedImage)
    {
        // Set the target image to the stylized result
        if (targetImage != null && stylizedImage != null)
        {
            targetImage.sprite = Sprite.Create(stylizedImage, new Rect(0, 0, stylizedImage.width, stylizedImage.height), new Vector2(0.5f, 0.5f));
        }

        // Re-enable camera for next photo
        CaptureManager.Instance.MakeCameraSnapshot(false);

        Debug.Log("Stylized image created and displayed");
    }

}
