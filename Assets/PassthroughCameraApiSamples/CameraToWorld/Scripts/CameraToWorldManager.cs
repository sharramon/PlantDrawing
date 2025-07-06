// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace PassthroughCameraSamples.CameraToWorld
{
    [MetaCodeSample("PassthroughCameraApiSamples-CameraToWorld")]
    public class CameraToWorldManager : MonoBehaviour
    {
        [SerializeField] private WebCamTextureManager m_webCamTextureManager;
        private PassthroughCameraEye CameraEye => m_webCamTextureManager.Eye;
        private Vector2Int CameraResolution => m_webCamTextureManager.RequestedResolution;
        [SerializeField] private GameObject m_centerEyeAnchor;

        [SerializeField] private CameraToWorldCameraCanvas m_cameraCanvas;
        [SerializeField] private float m_canvasDistance = 1f;

        //[SerializeField] private PlantNetAPI m_plantNetAPI;

        private bool m_snapshotTaken;

        private void Awake() => OVRManager.display.RecenteredPose += RecenterCallBack;

        private IEnumerator Start()
        {
            //m_cameraCanvas._snapshotTaken += m_plantNetAPI.IdentifyPlant;

            if (m_webCamTextureManager == null)
            {
                Debug.LogError($"PCA: {nameof(m_webCamTextureManager)} field is required "
                            + $"for the component {nameof(CameraToWorldManager)} to operate properly");
                enabled = false;
                yield break;
            }

            // Make sure the manager is disabled in scene and enable it only when the required permissions have been granted
            Assert.IsFalse(m_webCamTextureManager.enabled);
            while (PassthroughCameraPermissions.HasCameraPermission != true)
            {
                yield return null;
            }

            // Set the 'requestedResolution' and enable the manager
            m_webCamTextureManager.RequestedResolution = PassthroughCameraUtils.GetCameraIntrinsics(CameraEye).Resolution;
            m_webCamTextureManager.enabled = true;

            ScaleCameraCanvas();
        }

        private void Update()
        {
            if (m_webCamTextureManager.WebCamTexture == null)
                return;

            if (OVRInput.GetDown(OVRInput.Button.One))
            {
                m_snapshotTaken = !m_snapshotTaken;
                if (m_snapshotTaken)
                {
                    // Asking the canvas to make a snapshot before stopping WebCamTexture
                    m_cameraCanvas.MakeCameraSnapshot();
                    m_webCamTextureManager.WebCamTexture.Stop();
                }
                else
                {
                    m_webCamTextureManager.WebCamTexture.Play();
                    m_cameraCanvas.ResumeStreamingFromCamera();
                }
            }

            if (!m_snapshotTaken)
            {
                UpdateMarkerPoses();
            }
        }

        private void UpdateMarkerPoses()
        {
            var headPose = OVRPlugin.GetNodePoseStateImmediate(OVRPlugin.Node.Head).Pose.ToOVRPose();

            var cameraPose = PassthroughCameraUtils.GetCameraPoseInWorld(CameraEye);

            // Position the canvas in front of the camera
            m_cameraCanvas.transform.position = cameraPose.position + cameraPose.rotation * Vector3.forward * m_canvasDistance;
            m_cameraCanvas.transform.rotation = cameraPose.rotation;
        }

        /// <summary>
        /// Calculate the dimensions of the canvas based on the distance from the camera origin and the camera resolution
        /// </summary>
        private void ScaleCameraCanvas()
        {
            var cameraCanvasRectTransform = m_cameraCanvas.GetComponentInChildren<RectTransform>();
            var leftSidePointInCamera = PassthroughCameraUtils.ScreenPointToRayInCamera(CameraEye, new Vector2Int(0, CameraResolution.y / 2));
            var rightSidePointInCamera = PassthroughCameraUtils.ScreenPointToRayInCamera(CameraEye, new Vector2Int(CameraResolution.x, CameraResolution.y / 2));
            var horizontalFoVDegrees = Vector3.Angle(leftSidePointInCamera.direction, rightSidePointInCamera.direction);
            var horizontalFoVRadians = horizontalFoVDegrees / 180 * Math.PI;
            var newCanvasWidthInMeters = 2 * m_canvasDistance * Math.Tan(horizontalFoVRadians / 2);
            var localScale = (float)(newCanvasWidthInMeters / cameraCanvasRectTransform.sizeDelta.x);
            cameraCanvasRectTransform.localScale = new Vector3(localScale, localScale, localScale);
        }

        private void RecenterCallBack()
        {
            if (m_snapshotTaken)
            {
                m_snapshotTaken = false;
                m_webCamTextureManager.WebCamTexture.Play();
                m_cameraCanvas.ResumeStreamingFromCamera();
            }
        }
    }
}
