using System;
using UnityEngine;
using PassthroughCameraSamples;

public class CaptureManager : Singleton<CaptureManager>
{
    [SerializeField] private SmallCamera m_smallCamera;
    public SmallCamera _smallCamera => m_smallCamera;
    [SerializeField] private CameraToQuad m_cameraToQuad;
    public CameraToQuad _cameraToQuad => m_cameraToQuad;
    [SerializeField] private QuadToWorld m_quadToWorld;
    public QuadToWorld _quadToWorld => m_quadToWorld;
    [SerializeField] private WebCamTextureManager m_webCamTextureManager;
    public WebCamTextureManager _webCamTextureManager => m_webCamTextureManager;
    public PassthroughCameraEye _cameraEye => m_webCamTextureManager.Eye;
    public bool _isShootable = false;
    private bool m_isSnapshotTaken = false;
    public Action _onSnapshot;
    public Action<Texture2D> _onSnapshotTaken;
    
    [Header("AI Processing")]
    [SerializeField] private ReplicateImg2ImgClient m_replicateClient;

    private void Update()
    {
        if(OVRInput.GetDown(OVRInput.Button.Three))
        {
            if(m_replicateClient != null && m_replicateClient.isLoading)
            {
                m_cameraToQuad.UpdateDebugText("AI is processing image. Please wait.");
                return;
            }

            if(m_isSnapshotTaken == false && _isShootable == false)
            {
                return;
            }

            m_isSnapshotTaken = !m_isSnapshotTaken;
            m_cameraToQuad.UpdateDebugText($"isSnapshotTaken: {m_isSnapshotTaken}");
            MakeCameraSnapshot(m_isSnapshotTaken);
        }
    }

    public void MakeCameraSnapshot(bool isTaken)
    {
        //if still loading, don't take snapshot
        if(m_replicateClient != null && m_replicateClient.isLoading)
        {
            m_cameraToQuad.UpdateDebugText("AI is processing image. Please wait.");
            return;
        }

        m_quadToWorld.SnapshotTaken(isTaken);
        m_smallCamera.OnSnap(isTaken);
        if(isTaken)
        {
            _onSnapshot?.Invoke();
            m_cameraToQuad.UpdateDebugText("snapshot taken");
        }
        else
        {
            m_cameraToQuad.UpdateDebugText("snapshot released");
        }
    }
}
