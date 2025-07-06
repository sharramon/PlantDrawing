using System;
using System.Collections;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.UI;
using PassthroughCameraSamples.CameraToWorld;
using PassthroughCameraSamples;

public class CameraToQuad : MonoBehaviour
{
    //[Header("UI Components")]
    private Text m_debugText;

    [Header("Quad Components")]
    [SerializeField] private Renderer m_quadRenderer;
    [SerializeField] private Transform m_quadTransform;

    private IEnumerator Start()
    {
        while (CaptureManager.Instance._webCamTextureManager.WebCamTexture == null)
        {
            UpdateDebugText("Waiting for WebCamTexture...");
            yield return null;
        }

        UpdateDebugText("Webcam texture set!");
        AssignWebCamTexture();
    }

    private void AssignWebCamTexture()
    {
        var tex = CaptureManager.Instance._webCamTextureManager.WebCamTexture;
        m_quadRenderer.material.mainTexture = tex;
    }

    public void ResumeStreamingFromCamera()
    {
        AssignWebCamTexture();
    }

    public void MakeCameraSnapshot()
    {
        var tex = CaptureManager.Instance._webCamTextureManager.WebCamTexture;
        if (tex == null || !tex.isPlaying)
            return;

        Texture2D snapshot = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
        snapshot.SetPixels32(tex.GetPixels32());
        snapshot.Apply();

        m_quadRenderer.material.mainTexture = snapshot;

        CaptureManager.Instance._onSnapshotTaken?.Invoke(snapshot);
    }

    public void UpdateDebugText(string text)
    {
        if (m_debugText != null)
            m_debugText.text = text;
    }

    private void Update()
    {
        if (PassthroughCameraPermissions.HasCameraPermission != true)
        {
            UpdateDebugText("No camera permission.");
        }
    }
}