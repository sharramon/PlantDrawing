using Meta.XR.ImmersiveDebugger.UserInterface.Generic;
using PassthroughCameraSamples;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SmallCamera : MonoBehaviour
{
    public bool isSimulated = false;

    [Header("Snap Components")]
    [SerializeField] private GameObject m_passthroughQuad;
    [SerializeField] private Transform quadTransform;
    [SerializeField] private float rayLength = 5f;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private UnityEngine.UI.Image m_snappedWindow;

    [Header("Small Camera")]
    [SerializeField] private Transform m_handCameraTransform;
    [SerializeField] private UnityEngine.UI.Image m_cameraImage;
    [SerializeField] private Color m_onColor = Color.green;
    [SerializeField] private Color m_offColor = Color.red;
    // [Header("Sound")]
    // [SerializeField] private AudioSource m_audioSource;
    // [SerializeField] private AudioClip m_cameraSound;
    [SerializeField] private CopyImage m_copyImagePrefab;

    [Header("AI Processing")]
    [SerializeField] private VanGoghUIImageTransfer_Replicate m_replicate;

    [Header("Debug")]
    public bool isDebug = false;
    [SerializeField] private GameObject m_debugSphere;
    [SerializeField] private GameObject m_debugCube;

    private List<GameObject> debugObjects = new List<GameObject>();
    public System.Action<Texture2D> OnPictureReady;

    private void Update()
    {
        UpdateCameraPose();
        UpdateRaycastStatus();
    }

    private void UpdateCameraPose()
    {
        var cameraPose = new Pose(Vector3.zero, Quaternion.identity);

        if(!isSimulated) {
            cameraPose = PassthroughCameraUtils.GetCameraPoseInWorld(CaptureManager.Instance._cameraEye);
        }

        m_handCameraTransform.rotation = cameraPose.rotation;
    }

    public void GetPicture()
    {
        CaptureManager.Instance._cameraToQuad.UpdateDebugText("Getting Picture");
        TryCropFromQuadView();
    }

    private bool UpdateRaycastStatus()
    {
        Vector3 center = quadTransform.position;
        Vector3 right = quadTransform.right * 0.5f * quadTransform.localScale.x;
        Vector3 up = quadTransform.up * 0.5f * quadTransform.localScale.y;

        Vector3[] corners = new Vector3[4];
        corners[0] = center + right + up;
        corners[1] = center - right + up;
        corners[2] = center - right - up;
        corners[3] = center + right - up;

        Vector3 cameraPosition = Camera.main.transform.position;
        bool allHit = true;

        foreach (var corner in corners)
        {
            Vector3 rayDir = (corner - cameraPosition).normalized;

            if (!Physics.Raycast(cameraPosition, rayDir, rayLength, targetLayer))
            {
                allHit = false;
                break;
            }
        }

        if(allHit != CaptureManager.Instance._isShootable) {
            CaptureManager.Instance._isShootable = allHit;
            ChangeCameraColor();
        }
        return allHit;
    }

    private void ChangeCameraColor() {
        m_cameraImage.color = CaptureManager.Instance._isShootable ? m_onColor : m_offColor;
    }

    private void CreateDebugVisualization(Vector3[] corners, Vector3 cameraPosition)
    {
        if(!isDebug) return;

        // Clear previous debug objects
        foreach (var obj in debugObjects)
        {
            Destroy(obj);
        }
        debugObjects.Clear();

        // Create spheres at corners
        foreach (var corner in corners)
        {
            GameObject sphere = Instantiate(m_debugSphere);
            sphere.transform.position = corner;
            debugObjects.Add(sphere);
        }

        // Create visualization along actual camera rays
        foreach (var corner in corners)
        {
            // Calculate direction from camera to corner
            Vector3 rayDir = (corner - cameraPosition).normalized;
            
            // Create cubes along the ray path
            float step = rayLength / 4f;
            for (float distance = 0; distance < rayLength; distance += step)
            {
                GameObject cube = Instantiate(m_debugCube);
                cube.transform.position = cameraPosition + rayDir * distance;
                cube.transform.rotation = Quaternion.LookRotation(rayDir);
                debugObjects.Add(cube);
            }
        }
    }

    private void TryCropFromQuadView()
    {
        if (CaptureManager.Instance._webCamTextureManager.WebCamTexture == null)
        {
            CaptureManager.Instance._cameraToQuad.UpdateDebugText("WebCamTexture not ready.");
            return;
        }

        // Get the texture from the quad's material
        Texture quadTex = m_passthroughQuad.gameObject.GetComponent<MeshRenderer>().material.mainTexture;
        if (quadTex == null)
        {
            CaptureManager.Instance._cameraToQuad.UpdateDebugText("Quad texture not ready.");
            return;
        }

        Vector3 center = quadTransform.position;
        Vector3 right = quadTransform.right * 0.5f * quadTransform.localScale.x;
        Vector3 up = quadTransform.up * 0.5f * quadTransform.localScale.y;

        Vector3[] corners = new Vector3[4];
        corners[0] = center + right + up;
        corners[1] = center - right + up;
        corners[2] = center - right - up;
        corners[3] = center + right - up;

        // Get the camera position (assuming it's the main camera for now)
        Vector3 cameraPosition = Camera.main.transform.position;

        // Create debug visualization
        CreateDebugVisualization(corners, cameraPosition);

        List<Vector2> uvs = new();
        bool allHit = true;

        foreach (var corner in corners)
        {
            // Calculate direction from camera to corner
            Vector3 rayDir = (corner - cameraPosition).normalized;
            
            if (Physics.Raycast(cameraPosition, rayDir, out RaycastHit hit, rayLength, targetLayer))
            {
                Debug.DrawRay(cameraPosition, rayDir * hit.distance, Color.green);
                if(isDebug) {
                    GameObject sphere = Instantiate(m_debugSphere);
                    sphere.transform.position = hit.point;
                    debugObjects.Add(sphere);
                }
                uvs.Add(hit.textureCoord);
            }
            else
            {
                Debug.DrawRay(cameraPosition, rayDir * rayLength, Color.red);
                CaptureManager.Instance._cameraToQuad.UpdateDebugText("Raycast missed from one of the corners.");
                allHit = false;
                break;
            }
        }

        if (!allHit || uvs.Count != 4)
        {
            CaptureManager.Instance._cameraToQuad.UpdateDebugText("Crop failed — not all rays hit the target.");
            return;
        }

        Debug.Log("Cropping Texture");
        Texture2D cropped = CropFromQuadTexture((Texture2D)quadTex, uvs.ToArray(), true);

        if (cropped == null)
        {
            CaptureManager.Instance._cameraToQuad.UpdateDebugText("CropFromQuadTexture returned null.");
            return;
        }

        OnPictureReady?.Invoke(cropped);
    }

    private Texture2D CropFromQuadTexture(Texture2D quadTex, Vector2[] uvs, bool isCopy = false)
    {
        int texWidth = quadTex.width;
        int texHeight = quadTex.height;

        Vector2[] pixels = new Vector2[4];
        for (int i = 0; i < 4; i++)
        {
            pixels[i] = new Vector2(uvs[i].x * texWidth, uvs[i].y * texHeight);
        }

        float minX = Mathf.Min(pixels[0].x, pixels[1].x, pixels[2].x, pixels[3].x);
        float maxX = Mathf.Max(pixels[0].x, pixels[1].x, pixels[2].x, pixels[3].x);
        float minY = Mathf.Min(pixels[0].y, pixels[1].y, pixels[2].y, pixels[3].y);
        float maxY = Mathf.Max(pixels[0].y, pixels[1].y, pixels[2].y, pixels[3].y);

        int width = Mathf.Clamp((int)(maxX - minX), 1, texWidth);
        int height = Mathf.Clamp((int)(maxY - minY), 1, texHeight);
        int x = Mathf.Clamp((int)minX, 0, texWidth - width);
        int y = Mathf.Clamp((int)minY, 0, texHeight - height);

        Color[] pixelData = quadTex.GetPixels(x, y, width, height);
        Texture2D cropped = new Texture2D(width, height, TextureFormat.RGBA32, false);
        cropped.SetPixels(pixelData);
        cropped.Apply();

        UnityEngine.UI.Image image = null;

        if(isCopy) {
            CopyImage copyImage = Instantiate(m_copyImagePrefab);
            copyImage.GetImage().sprite = UnityEngine.Sprite.Create(cropped, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
            copyImage.GetImage().preserveAspect = true;

            copyImage.transform.position = m_handCameraTransform.position;
            copyImage.transform.rotation = m_handCameraTransform.rotation;

            m_replicate.SetImages(copyImage.GetImage(), copyImage.GetImage());
            m_replicate.StartTransfer();

        }else {
            // Set the snapped image directly to the Image component
            m_snappedWindow.sprite = UnityEngine.Sprite.Create(cropped, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
            m_snappedWindow.preserveAspect = true;
            image = m_snappedWindow;
        }

        image.gameObject.SetActive(true);

        return cropped;
    }

    public void OnSnap(bool isSnapped)
    {
        if(isSnapped)
        {
            GetPicture();
            //m_audioSource.PlayOneShot(m_cameraSound);
            quadTransform.gameObject.SetActive(false);
            quadTransform.gameObject.GetComponent<MeshRenderer>().enabled = false;
        }
        else
        {
            // Clear debug objects when unsnapping
            foreach (var obj in debugObjects)
            {
                Destroy(obj);
            }
            debugObjects.Clear();
            quadTransform.gameObject.SetActive(true);
            quadTransform.gameObject.GetComponent<MeshRenderer>().enabled = true;
        }
    }
}

