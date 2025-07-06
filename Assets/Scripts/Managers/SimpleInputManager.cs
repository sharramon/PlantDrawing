using UnityEngine;
using TiltBrush;

namespace TiltBrush
{
    /// <summary>
    /// Simple input manager that bridges Meta's XR anchors to OpenBrush's pointer system.
    /// Uses anchor transforms for both controllers and hands, converting them to pointer positions/actions.
    /// </summary>
    public class SimpleInputManager : MonoBehaviour
    {
        [Header("XR Anchors")]
        [SerializeField] private OVRCameraRig m_OVRCameraRig;
        [SerializeField] private Transform m_LeftAnchorTransform;
        [SerializeField] private Transform m_RightAnchorTransform;
        [SerializeField] private bool m_AutoFindOVRAnchors = true;
        
        [Header("Pointer Settings")]
        [SerializeField] private Transform m_LeftPointer;
        [SerializeField] private Transform m_RightPointer;
        [SerializeField] private float m_PointerDistance = 0.1f;
        [SerializeField] private LayerMask m_DrawingLayerMask = -1;
        
        [Header("Input Settings")]
        [SerializeField] private float m_TriggerThreshold = 0.5f;
        [SerializeField] private float m_GripThreshold = 0.5f;
        
        [Header("Brush Settings")]
        [SerializeField] private BrushDescriptor m_DefaultBrush;
        [SerializeField] private Color m_DefaultColor = Color.green;
        [SerializeField] private Color m_otherColor = Color.red;
        [SerializeField] private float m_DefaultBrushSize = 1.0f;
        
        // Input state
        private bool m_LeftTriggerPressed = false;
        private bool m_RightTriggerPressed = false;
        private bool m_LeftGripPressed = false;
        private bool m_RightGripPressed = false;
        
        // Pointer state
        private bool m_LeftPointerActive = false;
        private bool m_RightPointerActive = false;
        
        // Drawing state
        private bool m_IsDrawing = false;
        private int m_ActivePointerIndex = -1; // 0 = left, 1 = right
        
        void Start()
        {
            if (m_AutoFindOVRAnchors)
            {
                FindOVRAnchors();
            }
            InitializePointers();
            SetupBrushDefaults();
        }
        
        void Update()
        {
            UpdateInputState();
            UpdatePointerPositions();
            HandleDrawingInput();
            
            // Update PointerManager's line creation state machine
            if (PointerManager.m_Instance != null)
            {
                PointerManager.m_Instance.UpdateLine();
            }
        }
        
        private void FindOVRAnchors()
        {
            // Find OVR Camera Rig
            if (m_OVRCameraRig == null)
            {
                m_OVRCameraRig = FindObjectOfType<OVRCameraRig>();
            }

            if (m_OVRCameraRig == null)
            {
                Debug.LogWarning("SimpleInputManager: No OVR Camera Rig found in scene. Please assign anchors manually.");
                return;
            }
            
            // Get left and right hand anchors
            if (m_OVRCameraRig.leftHandAnchor != null)
            {
                m_LeftAnchorTransform = m_OVRCameraRig.leftHandAnchor;
                Debug.Log("SimpleInputManager: Found left hand anchor from OVR Camera Rig");
            }
            else
            {
                Debug.LogWarning("SimpleInputManager: Left hand anchor not found in OVR Camera Rig");
            }
            
            if (m_OVRCameraRig.rightHandAnchor != null)
            {
                m_RightAnchorTransform = m_OVRCameraRig.rightHandAnchor;
                Debug.Log("SimpleInputManager: Found right hand anchor from OVR Camera Rig");
            }
            else
            {
                Debug.LogWarning("SimpleInputManager: Right hand anchor not found in OVR Camera Rig");
            }
            
            // Fallback: try to find controller anchors if hand anchors aren't available
            if (m_LeftAnchorTransform == null && m_OVRCameraRig.leftControllerAnchor != null)
            {
                m_LeftAnchorTransform = m_OVRCameraRig.leftControllerAnchor;
                Debug.Log("SimpleInputManager: Using left controller anchor as fallback");
            }
            
            if (m_RightAnchorTransform == null && m_OVRCameraRig.rightControllerAnchor != null)
            {
                m_RightAnchorTransform = m_OVRCameraRig.rightControllerAnchor;
                Debug.Log("SimpleInputManager: Using right controller anchor as fallback");
            }
        }
        
        private void InitializePointers()
        {
            // Create pointer objects if they don't exist
            if (m_LeftPointer == null)
            {
                GameObject leftPointerObj = new GameObject("LeftPointer");
                m_LeftPointer = leftPointerObj.transform;
                m_LeftPointer.SetParent(transform);
            }
            
            if (m_RightPointer == null)
            {
                GameObject rightPointerObj = new GameObject("RightPointer");
                m_RightPointer = rightPointerObj.transform;
                m_RightPointer.SetParent(transform);
            }
        }
        
        private void SetupBrushDefaults()
        {
            if (PointerManager.m_Instance != null)
            {
                if (m_DefaultBrush != null)
                {
                    PointerManager.m_Instance.SetBrushForAllPointers(m_DefaultBrush);
                }
                PointerManager.m_Instance.PointerColor = m_DefaultColor;
                PointerManager.m_Instance.SetAllPointersBrushSize01(0.5f); // Normalized size
            }
        }
        
        private void UpdateInputState()
        {
            // Update trigger states using OVR input system - only track right controller for drawing
            bool rightTrigger = OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger) || 
                               OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger) > m_TriggerThreshold;
            
            // Update grip states - only track right controller
            bool rightGrip = OVRInput.Get(OVRInput.Button.SecondaryHandTrigger) || 
                            OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger) > m_GripThreshold;
            
            // Handle trigger press/release events - only right controller
            if (rightTrigger && !m_RightTriggerPressed)
            {
                OnRightTriggerPressed();
            }
            else if (!rightTrigger && m_RightTriggerPressed)
            {
                OnRightTriggerReleased();
            }
            
            // Update grip states - only right controller
            m_RightGripPressed = rightGrip;
            
            // Update trigger states - only right controller
            m_RightTriggerPressed = rightTrigger;
        }
        
        private void UpdatePointerPositions()
        {
            // Update right pointer only - position in front of controller for better drawing
            if (m_RightAnchorTransform != null)
            {
                Vector3 rightPos = m_RightAnchorTransform.position;
                Quaternion rightRot = m_RightAnchorTransform.rotation;
                Vector3 rightForward = m_RightAnchorTransform.forward;
                
                // Position pointer in front of controller for better drawing experience
                Vector3 rightPointerPos = rightPos + rightForward * m_PointerDistance;
                m_RightPointer.position = rightPointerPos;
                m_RightPointer.rotation = rightRot;
                
                // Update PointerManager - use index 0 for the main drawing pointer
                if (PointerManager.m_Instance != null)
                {
                    PointerManager.m_Instance.SetPointerTransform(0, m_RightPointer.position, m_RightPointer.rotation);
                }
            }
        }
        

        
        private void HandleDrawingInput()
        {
            if (PointerManager.m_Instance == null) return;
            
            // Handle drawing start/stop - only use right controller for drawing
            if (m_RightTriggerPressed && !m_IsDrawing)
            {
                StartDrawing(1); // Right pointer only
            }
            else if (!m_RightTriggerPressed && m_IsDrawing)
            {
                StopDrawing();
            }
            
            // Handle pressure input from right controller only
            if (m_IsDrawing)
            {
                float pressure = Mathf.Clamp01(OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger));
                PointerManager.m_Instance.PointerPressure = pressure;
            }
        }
        
        private void StartDrawing(int pointerIndex)
        {
            m_IsDrawing = true;
            m_ActivePointerIndex = 0; // Always use index 0 (right pointer) for drawing
            
            if (PointerManager.m_Instance != null)
            {
                PointerManager.m_Instance.EnableLine(true);
            }
            
            Debug.Log("Started drawing with right controller");
        }
        
        private void StopDrawing()
        {
            m_IsDrawing = false;
            m_ActivePointerIndex = -1;
            
            if (PointerManager.m_Instance != null)
            {
                PointerManager.m_Instance.EnableLine(false);
            }
            
            Debug.Log("Stopped drawing");
        }
        
        private void OnRightTriggerPressed()
        {
            Debug.Log("Right trigger pressed");
        }
        
        private void OnRightTriggerReleased()
        {
            Debug.Log("Right trigger released");
        }
        
        // Public methods for external control
        public void SetBrush(BrushDescriptor brush)
        {
            if (PointerManager.m_Instance != null)
            {
                PointerManager.m_Instance.SetBrushForAllPointers(brush);
            }
        }
        
        public void SetColor(Color color)
        {
            if (PointerManager.m_Instance != null)
            {
                PointerManager.m_Instance.PointerColor = color;
            }
        }
        
        public void SetBrushSize(float size01)
        {
            if (PointerManager.m_Instance != null)
            {
                PointerManager.m_Instance.SetAllPointersBrushSize01(size01);
            }
        }
        
        public bool IsDrawing()
        {
            return m_IsDrawing;
        }
        
        public Transform GetActivePointer()
        {
            // Only return right pointer since we only use right controller for drawing
            return m_RightPointer;
        }
        
        /// <summary>
        /// Sets the distance that pointers appear in front of controllers.
        /// </summary>
        /// <param name="distance">Distance in front of controller (positive = forward, negative = backward)</param>
        public void SetPointerDistance(float distance)
        {
            m_PointerDistance = distance;
            Debug.Log($"SimpleInputManager: Updated pointer distance to {m_PointerDistance}");
        }
        
        /// <summary>
        /// Gets the current pointer distance from controllers.
        /// </summary>
        /// <returns>Current pointer distance</returns>
        public float GetPointerDistance()
        {
            return m_PointerDistance;
        }
    }
} 