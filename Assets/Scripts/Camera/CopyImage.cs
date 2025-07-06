using UnityEngine;

public class CopyImage : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image m_image;

    public UnityEngine.UI.Image GetImage() {
        return m_image;
    }
}
