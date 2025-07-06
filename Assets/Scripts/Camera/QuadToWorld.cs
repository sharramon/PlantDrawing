using System;
using System.Collections;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Assertions;
using PassthroughCameraSamples.CameraToWorld;
using PassthroughCameraSamples;

public class QuadToWorld : MonoBehaviour
{
    [Header("Passthrough")]
    [SerializeField] private float m_quadDistance = 1f;

    [SerializeField] private Transform m_quadTransform;

    private bool m_snapshotTaken;

    private void OnEnable()
    {
        OVRManager.display.RecenteredPose += RecenterCallBack;
    }

    private IEnumerator Start()
    {
        if (CaptureManager.Instance._webCamTextureManager == null)
        {
            Debug.LogError($"{nameof(WebCamTextureManager)} is required.");
            enabled = false;
            yield break;
        }

        Assert.IsFalse(CaptureManager.Instance._webCamTextureManager.enabled);

        while (PassthroughCameraPermissions.HasCameraPermission != true)
        {
            yield return null;
        }

        CaptureManager.Instance._webCamTextureManager.RequestedResolution = PassthroughCameraUtils.GetCameraIntrinsics(CaptureManager.Instance._cameraEye).Resolution;
        CaptureManager.Instance._webCamTextureManager.enabled = true;

        ScaleCameraQuad();
    }

    private void Update()
    {
        if (CaptureManager.Instance._webCamTextureManager.WebCamTexture == null)
            return;

        if (!m_snapshotTaken)
        {
            UpdateQuadPose();
        }

        //just brute force for now
        UpdateQuadPose();
    }

    public void SnapshotTaken(bool isTaken)
    {
        m_snapshotTaken = isTaken;

        if(m_snapshotTaken)
        {
            CaptureManager.Instance._cameraToQuad.MakeCameraSnapshot();
            //CaptureManager.Instance._webCamTextureManager.WebCamTexture.Stop();
        }
        else
        {
            CaptureManager.Instance._webCamTextureManager.WebCamTexture.Play();
            CaptureManager.Instance._cameraToQuad.ResumeStreamingFromCamera();
        }
    }

    private void UpdateQuadPose()
    {
        var cameraPose = PassthroughCameraUtils.GetCameraPoseInWorld(CaptureManager.Instance._cameraEye);
        m_quadTransform.position = cameraPose.position + cameraPose.rotation * Vector3.forward * m_quadDistance;
        m_quadTransform.rotation = cameraPose.rotation;
    }

    private void ScaleCameraQuad()
{
    var resolution = CaptureManager.Instance._webCamTextureManager.RequestedResolution;

    var leftRay = PassthroughCameraUtils.ScreenPointToRayInCamera(CaptureManager.Instance._cameraEye, new Vector2Int(0, resolution.y / 2));
    var rightRay = PassthroughCameraUtils.ScreenPointToRayInCamera(CaptureManager.Instance._cameraEye, new Vector2Int(resolution.x - 1, resolution.y / 2));

    var fovDegrees = Vector3.Angle(leftRay.direction, rightRay.direction);
    var fovRadians = fovDegrees * Mathf.Deg2Rad;

    float quadWidth = 2f * m_quadDistance * Mathf.Tan(fovRadians / 2f);
    float aspect = (float)resolution.x / resolution.y;
    float quadHeight = quadWidth / aspect;

    var scale = m_quadTransform.localScale;
    scale.x = quadWidth;
    scale.y = quadHeight;
    m_quadTransform.localScale = scale;
}

    private void RecenterCallBack()
    {
        if (m_snapshotTaken)
        {
            m_snapshotTaken = false;
            CaptureManager.Instance._webCamTextureManager.WebCamTexture.Play();
            CaptureManager.Instance._cameraToQuad.ResumeStreamingFromCamera();
        }
    }
}
