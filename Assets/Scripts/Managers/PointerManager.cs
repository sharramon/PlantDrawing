// Copyright 2020 The Tilt Brush Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Random = UnityEngine.Random;

namespace TiltBrush
{
    public class PointerManager : MonoBehaviour
    {
        static public PointerManager m_Instance;
        const string PLAYER_PREFS_POINTER_ANGLE_OLD = "Pointer_Angle";
        const string PLAYER_PREFS_POINTER_ANGLE = "Pointer_Angle2";

        // Modifying this struct has implications for binary compatibility.
        // The layout should match the most commonly-seen layout in the binary file.
        // See SketchMemoryScript.ReadMemory.
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [System.Serializable]
        public struct ControlPoint
        {
            public Vector3 m_Pos;
            public Quaternion m_Orient;
            public float m_Pressure;
            public uint m_TimestampMs; // CurrentSketchTime of creation, in milliseconds
        }

        // TODO: all this should be stored in the PointerScript instead of kept alongside
        protected class PointerData
        {
            public PointerScript m_Script;
            public bool m_UiEnabled;
        }

        // ---- Private types

        private enum LineCreationState
        {
            // Not drawing a line.
            WaitingForInput,
            // Have first endpoint but not second endpoint.
            RecordingInput,
        }

        // ---- Private inspector data

        [SerializeField] private int m_MaxPointers = 4; // Need more pointers for left/right + playback
        [SerializeField] private GameObject m_MainPointerPrefab;
        [SerializeField] private GameObject m_AuxPointerPrefab;
        [SerializeField] private float m_DefaultPointerAngle = 25.0f;
        [SerializeField] private bool m_DebugViewControlPoints = false;
        

        
        [Header("Brush Settings")]
        [SerializeField] private float m_InitialBrushSize01 = 0.5f; // Initial brush size as percentage (0-1)
        [SerializeField] private float m_InitialBrushSizeAbsolute = 0.5f; // Initial brush size as absolute value
        [SerializeField] private bool m_UseAbsoluteBrushSize = false; // Use absolute size instead of percentage
        [SerializeField] private Color m_InitialBrushColor = Color.green;
        
        [Header("Color Toggle Settings")]
        [SerializeField] private Color m_AlternateBrushColor = Color.red; // Color to toggle to when pressing spacebar
        [SerializeField] private bool m_EnableColorToggle = true; // Enable spacebar color toggle
        private bool m_IsUsingAlternateColor = false; // Track which color is currently active
        
        [Header("Direct Brush Size Control")]
        [SerializeField] private float m_DirectBrushSize = 1.0f; // Direct control of m_CurrentBrushSize
        [SerializeField] private bool m_UseDirectBrushSize = false; // Use direct size control
        private float m_LastAppliedDirectBrushSize = 1.0f; // Track last applied size to avoid unnecessary updates

        // ---- Private member data

        private int m_NumActivePointers = 2; // Need at least 2 pointers for left/right hand

        private bool m_PointersRenderingRequested;
        private bool m_PointersRenderingActive;

        private float m_FreePaintPointerAngle;

        private LineCreationState m_CurrentLineCreationState;
        private bool m_LineEnabled = false;
        private int m_EatLineEnabledInputFrames;

        private PointerData[] m_Pointers;

        private PointerData m_MainPointerData;
        struct StoredBrushInfo
        {
            public BrushDescriptor brush;
            public float size01;
            public Color color;
        }
        private StoredBrushInfo? m_StoredBrushInfo;

        public Color m_lastChosenColor { get; private set; }
        
        // ---- events

        public event Action<TiltBrush.BrushDescriptor> OnMainPointerBrushChange = delegate { };
        public event Action OnPointerColorChange = delegate { };

        // ---- public properties

        public PointerScript MainPointer
        {
            get { return m_MainPointerData.m_Script; }
        }

        /// Only call this if you don't want to update m_lastChosenColor
        /// Used by color jitter on new stroke
        private void ChangeAllPointerColorsDirectly(Color value)
        {
            for (int i = 0; i < m_NumActivePointers; ++i)
            {
                m_Pointers[i].m_Script.SetColor(value);
            }
        }

        public Color PointerColor
        {
            get { return m_MainPointerData.m_Script.GetCurrentColor(); }
            set
            {
                ChangeAllPointerColorsDirectly(value);
                m_lastChosenColor = value;
                OnPointerColorChange();
            }
        }
        public float PointerPressure
        {
            set
            {
                for (int i = 0; i < m_NumActivePointers; ++i)
                {
                    m_Pointers[i].m_Script.SetPressure(value);
                }
            }
        }

        public bool IndicateBrushSize
        {
            set
            {
                for (int i = 0; i < m_NumActivePointers; ++i)
                {
                    m_Pointers[i].m_Script.ShowSizeIndicator(value);
                }
            }
        }

        /// The number of pointers available with GetTransientPointer()
        public int NumTransientPointers { get { return m_Pointers.Length - NumUserPointers; } }

        private int NumUserPointers { get { return 2; } }

        public float FreePaintPointerAngle
        {
            get { return m_FreePaintPointerAngle; }
            set
            {
                m_FreePaintPointerAngle = value;
                PlayerPrefs.SetFloat(PLAYER_PREFS_POINTER_ANGLE, m_FreePaintPointerAngle);
            }
        }

        static public void ClearPlayerPrefs()
        {
            PlayerPrefs.DeleteKey(PLAYER_PREFS_POINTER_ANGLE_OLD);
            PlayerPrefs.DeleteKey(PLAYER_PREFS_POINTER_ANGLE);
        }

        // ---- accessors

        public PointerScript GetPointer(int index = 0)
        {
            if (index >= 0 && index < m_Pointers.Length)
            {
                return m_Pointers[index].m_Script;
            }
            return null;
        }

        // Return a pointer suitable for transient use (like for playback)
        // Guaranteed to be different from any non-null return value of GetPointer(ControllerName)
        // Raise exception if not enough pointers
        public PointerScript GetTransientPointer(int i)
        {
            return m_Pointers[NumUserPointers + i].m_Script;
        }

        /// The brush size, using "normalized" values in the range [0,1].
        /// Guaranteed to be in [0,1].
        public float GetPointerBrushSize01(int index = 0)
        {
            return Mathf.Clamp01(GetPointer(index).BrushSize01);
        }

        public bool IsMainPointerCreatingStroke()
        {
            return m_MainPointerData.m_Script.IsCreatingStroke();
        }

        public static bool MainPointerIsPainting()
        {
            if (
                m_Instance.IsMainPointerCreatingStroke()
                || m_Instance.IsLineEnabled()
            )
                return true;

            return false;
        }

        public void EatLineEnabledInput()
        {
            m_EatLineEnabledInputFrames = 2;
        }

        /// Causes pointer manager to begin or end a stroke; takes effect next frame.
        public void EnableLine(bool bEnable)
        {
            Debug.Log($"PointerManager: EnableLine({bEnable}) called, m_EatLineEnabledInputFrames={m_EatLineEnabledInputFrames}");
            
            // If we've been requested to eat input, discard any valid input until we've received
            //  some invalid input.
            if (m_EatLineEnabledInputFrames > 0)
            {
                if (!bEnable)
                {
                    --m_EatLineEnabledInputFrames;
                }
                m_LineEnabled = false;
                Debug.Log($"PointerManager: Eating input, m_LineEnabled set to false");
            }
            else
            {
                m_LineEnabled = bEnable;
                Debug.Log($"PointerManager: Setting m_LineEnabled to {bEnable}");
            }
        }

        public bool IsLineEnabled()
        {
            return m_LineEnabled;
        }

        // ---- Unity events

        void Awake()
        {
            m_Instance = this;

            Debug.Assert(m_MaxPointers > 0);
            m_Pointers = new PointerData[m_MaxPointers];

            for (int i = 0; i < m_Pointers.Length; ++i)
            {
                //set our main pointer as the zero index
                bool bMain = (i == 0);
                var data = new PointerData();
                GameObject obj = (GameObject)Instantiate(bMain ? m_MainPointerPrefab : m_AuxPointerPrefab);
                // Don't parent to PointerManager transform - will be parented to controllers later
                data.m_Script = obj.GetComponent<PointerScript>();
                data.m_UiEnabled = bMain;
                m_Pointers[i] = data;
                if (bMain)
                {
                    m_MainPointerData = data;
                }
            }

            m_CurrentLineCreationState = LineCreationState.WaitingForInput;

            //initialize rendering requests to default to hiding everything
            m_PointersRenderingRequested = false;
            m_PointersRenderingActive = true;
            m_EatLineEnabledInputFrames = 0; // Ensure this is explicitly set to 0

            m_FreePaintPointerAngle =
                PlayerPrefs.GetFloat(PLAYER_PREFS_POINTER_ANGLE, m_DefaultPointerAngle);
                
            // Apply initial brush settings to all pointers
            ApplyInitialBrushSettings();
        }

        void Start()
        {
            // Migrate setting, but only if it's non-zero
            if (PlayerPrefs.HasKey(PLAYER_PREFS_POINTER_ANGLE_OLD))
            {
                var prev = PlayerPrefs.GetFloat(PLAYER_PREFS_POINTER_ANGLE_OLD);
                PlayerPrefs.DeleteKey(PLAYER_PREFS_POINTER_ANGLE_OLD);
                if (prev != 0)
                {
                    PlayerPrefs.SetFloat(PLAYER_PREFS_POINTER_ANGLE, prev);
                }
            }

            RefreshFreePaintPointerAngle();
        }
        
        /// <summary>
        /// Applies initial brush settings (size and color) to all active pointers.
        /// Called during initialization to set up default brush properties.
        /// </summary>
        private void ApplyInitialBrushSettings()
        {
            for (int i = 0; i < m_NumActivePointers; ++i)
            {
                var pointer = m_Pointers[i].m_Script;
                
                // Set initial color
                pointer.SetColor(m_InitialBrushColor);
                
                // Set initial brush size (will be applied when brush is set)
                // Note: The actual size will be set when SetBrush is called on the pointer
                // This just ensures the color is set immediately
            }
            
            Debug.Log($"PointerManager: Applied initial brush settings - Color: {m_InitialBrushColor}, Size01: {m_InitialBrushSize01}");
        }

        void Update()
        {
            //update pointers
            for (int i = 0; i < m_NumActivePointers; ++i)
            {
                m_Pointers[i].m_Script.UpdatePointer();
            }

            //update pointer rendering according to state
            SetPointersRenderingEnabled(m_PointersRenderingRequested);
            
            // Apply direct brush size control if enabled
            if (m_UseDirectBrushSize)
            {
                ApplyDirectBrushSize();
            }
            
            // Handle color toggle with spacebar
            if (m_EnableColorToggle)
            {
                HandleColorToggleInput();
            }
        }
        
        // Note: Pointer positions are now handled by SimpleInputManager
        // This prevents conflicts between different systems trying to update pointer positions

        public void StoreBrushInfo()
        {
            m_StoredBrushInfo = new StoredBrushInfo
            {
                brush = MainPointer.CurrentBrush,
                size01 = MainPointer.BrushSize01,
                color = PointerColor,
            };
        }

        public void RestoreBrushInfo()
        {
            if (m_StoredBrushInfo == null) { return; }
            var info = m_StoredBrushInfo.Value;
            SetBrushForAllPointers(info.brush);
            SetAllPointersBrushSize01(info.size01);
            MarkAllBrushSizeUsed();
            PointerColor = info.color;
        }

        public void RefreshFreePaintPointerAngle()
        {
            // Simplified - removed InputManager dependency
        }

        void SetPointersRenderingEnabled(bool bEnable)
        {
            if (m_PointersRenderingActive != bEnable)
            {
                foreach (PointerData rData in m_Pointers)
                {
                    rData.m_Script.EnableRendering(bEnable && rData.m_UiEnabled);
                }
                m_PointersRenderingActive = bEnable;
            }
        }

        public void EnablePointerStrokeGeneration(bool bActivate)
        {
            foreach (PointerData rData in m_Pointers)
            {
                // Note that pointers with m_UiEnabled=false may still be employed during scene playback.
                rData.m_Script.gameObject.SetActive(bActivate);
            }
        }

        public void RequestPointerRendering(bool bEnable)
        {
            m_PointersRenderingRequested = bEnable;
        }

        private PointerData GetPointerData(int index = 0)
        {
            if (index >= 0 && index < m_Pointers.Length)
            {
                return m_Pointers[index];
            }
            Debug.AssertFormat(false, "No pointer for index {0}", index);
            return null;
        }

        public void AllowPointerPreviewLine(bool bAllow)
        {
            for (int i = 0; i < m_NumActivePointers; ++i)
            {
                m_Pointers[i].m_Script.AllowPreviewLine(bAllow);
            }
        }

        public void DisablePointerPreviewLine()
        {
            for (int i = 0; i < m_NumActivePointers; ++i)
            {
                m_Pointers[i].m_Script.DisablePreviewLine();
            }
        }

        public void SetPointerPreviewLineDelayTimer()
        {
            for (int i = 0; i < m_NumActivePointers; ++i)
            {
                m_Pointers[i].m_Script.SetPreviewLineDelayTimer();
            }
        }

        public void ExplicitlySetAllPointersBrushSize(float fSize)
        {
            for (int i = 0; i < m_NumActivePointers; ++i)
            {
                m_Pointers[i].m_Script.BrushSizeAbsolute = fSize;
            }
        }

        public void MarkAllBrushSizeUsed()
        {
            for (int i = 0; i < m_NumActivePointers; ++i)
            {
                // Simplified - removed MarkBrushSizeUsed dependency
            }
        }

        public void SetAllPointersBrushSize01(float t)
        {
            for (int i = 0; i < m_NumActivePointers; ++i)
            {
                m_Pointers[i].m_Script.BrushSize01 = t;
            }
        }

        public void AdjustAllPointersBrushSize01(float dt)
        {
            for (int i = 0; i < m_NumActivePointers; ++i)
            {
                m_Pointers[i].m_Script.BrushSize01 += dt;
            }
        }

        public void SetBrushForAllPointers(BrushDescriptor desc)
        {
            for (int i = 0; i < m_NumActivePointers; ++i)
            {
                m_Pointers[i].m_Script.SetBrush(desc);
                
                // Apply the brush size after setting the brush
                if (m_UseDirectBrushSize)
                {
                    // Use direct brush size control (sets m_CurrentBrushSize directly)
                    m_Pointers[i].m_Script.BrushSizeAbsolute = m_DirectBrushSize;
                }
                else if (m_UseAbsoluteBrushSize)
                {
                    // Use absolute brush size directly
                    m_Pointers[i].m_Script.BrushSizeAbsolute = m_InitialBrushSizeAbsolute;
                }
                else
                {
                    // Use percentage of brush size range
                    m_Pointers[i].m_Script.BrushSize01 = m_InitialBrushSize01;
                }
            }
        }
        
        /// <summary>
        /// Sets the initial brush size for all pointers (0-1 range).
        /// This will be applied when brushes are set on the pointers.
        /// </summary>
        /// <param name="size01">Brush size as percentage (0-1)</param>
        public void SetInitialBrushSize01(float size01)
        {
            m_InitialBrushSize01 = Mathf.Clamp01(size01);
            Debug.Log($"PointerManager: Initial brush size percentage set to {m_InitialBrushSize01}");
        }
        
        /// <summary>
        /// Sets the initial absolute brush size for all pointers.
        /// This will be applied when brushes are set on the pointers.
        /// </summary>
        /// <param name="absoluteSize">Absolute brush size value</param>
        public void SetInitialBrushSizeAbsolute(float absoluteSize)
        {
            m_InitialBrushSizeAbsolute = Mathf.Max(0f, absoluteSize);
            Debug.Log($"PointerManager: Initial absolute brush size set to {m_InitialBrushSizeAbsolute}");
        }
        
        /// <summary>
        /// Sets whether to use absolute brush size instead of percentage.
        /// </summary>
        /// <param name="useAbsolute">True to use absolute size, false to use percentage</param>
        public void SetUseAbsoluteBrushSize(bool useAbsolute)
        {
            m_UseAbsoluteBrushSize = useAbsolute;
            Debug.Log($"PointerManager: Use absolute brush size set to {m_UseAbsoluteBrushSize}");
        }
        
        /// <summary>
        /// Sets the direct brush size that controls both indicator and stroke thickness.
        /// This directly sets m_CurrentBrushSize on all pointers.
        /// </summary>
        /// <param name="size">Direct brush size value</param>
        public void SetDirectBrushSize(float size)
        {
            m_DirectBrushSize = Mathf.Max(0f, size);
            Debug.Log($"PointerManager: Direct brush size set to {m_DirectBrushSize}");
        }
        
        /// <summary>
        /// Sets whether to use direct brush size control.
        /// </summary>
        /// <param name="useDirect">True to use direct size control</param>
        public void SetUseDirectBrushSize(bool useDirect)
        {
            m_UseDirectBrushSize = useDirect;
            Debug.Log($"PointerManager: Use direct brush size set to {m_UseDirectBrushSize}");
        }
        
        /// <summary>
        /// Applies the direct brush size to all active pointers.
        /// Called every frame when direct brush size control is enabled.
        /// </summary>
        private void ApplyDirectBrushSize()
        {
            // Only update if the size has actually changed
            if (Mathf.Abs(m_DirectBrushSize - m_LastAppliedDirectBrushSize) > 0.001f)
            {
                for (int i = 0; i < m_NumActivePointers; ++i)
                {
                    m_Pointers[i].m_Script.BrushSizeAbsolute = m_DirectBrushSize;
                    // Recreate preview line to update its size
                    m_Pointers[i].m_Script.RecreatePreviewLine();
                }
                m_LastAppliedDirectBrushSize = m_DirectBrushSize;
            }
        }
        
        /// <summary>
        /// Sets the initial brush color for all pointers.
        /// This will be applied immediately to all active pointers.
        /// </summary>
        /// <param name="color">Brush color</param>
        public void SetInitialBrushColor(Color color)
        {
            m_InitialBrushColor = color;
            // Apply to all active pointers immediately
            for (int i = 0; i < m_NumActivePointers; ++i)
            {
                m_Pointers[i].m_Script.SetColor(color);
            }
            Debug.Log($"PointerManager: Initial brush color set to {color}");
        }
        
        /// <summary>
        /// Handles spacebar input to toggle between default and alternate brush colors.
        /// </summary>
        private void HandleColorToggleInput()
        {
            // Check for spacebar press using the new Input System
            if (OVRInput.GetDown(OVRInput.Button.One) || UnityEngine.InputSystem.Keyboard.current != null && 
                UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                ToggleBrushColor();
            }
        }
        
        /// <summary>
        /// Toggles between the default and alternate brush colors.
        /// </summary>
        public void ToggleBrushColor()
        {
            m_IsUsingAlternateColor = !m_IsUsingAlternateColor;
            Color newColor = m_IsUsingAlternateColor ? m_AlternateBrushColor : m_InitialBrushColor;
            
            // Apply the new color to all pointers
            PointerColor = newColor;
            
            Debug.Log($"PointerManager: Toggled brush color to {(m_IsUsingAlternateColor ? "alternate" : "default")}: {newColor}");
        }
        
        /// <summary>
        /// Sets the alternate brush color for spacebar toggle.
        /// </summary>
        /// <param name="color">Alternate brush color</param>
        public void SetAlternateBrushColor(Color color)
        {
            m_AlternateBrushColor = color;
            Debug.Log($"PointerManager: Alternate brush color set to {color}");
        }
        
        /// <summary>
        /// Enables or disables the spacebar color toggle feature.
        /// </summary>
        /// <param name="enable">True to enable color toggle, false to disable</param>
        public void SetColorToggleEnabled(bool enable)
        {
            m_EnableColorToggle = enable;
            Debug.Log($"PointerManager: Color toggle {(enable ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// Gets whether the alternate color is currently active.
        /// </summary>
        /// <returns>True if using alternate color, false if using default color</returns>
        public bool IsUsingAlternateColor()
        {
            return m_IsUsingAlternateColor;
        }
        
        /// <summary>
        /// Gets the currently active brush color (either default or alternate).
        /// </summary>
        /// <returns>The currently active brush color</returns>
        public Color GetCurrentBrushColor()
        {
            return m_IsUsingAlternateColor ? m_AlternateBrushColor : m_InitialBrushColor;
        }
        


        public void SetPointerTransform(int index, Vector3 v, Quaternion q)
        {
            Transform pointer = GetPointer(index).transform;
            pointer.position = v;
            pointer.rotation = q;
        }

        public void SetMainPointerPosition(Vector3 vPos)
        {
            m_MainPointerData.m_Script.transform.position = vPos;
        }

        public void SetMainPointerRotation(Quaternion qRot)
        {
            m_MainPointerData.m_Script.transform.rotation = qRot;
        }

        public void SetMainPointerForward(Vector3 vForward)
        {
            m_MainPointerData.m_Script.transform.forward = vForward;
        }

        private void ChangeNumActivePointers(int num)
        {
            if (num > m_Pointers.Length)
            {
                Debug.LogWarning($"Not enough pointers for mode. {num} requested, {m_Pointers.Length} available");
                num = m_Pointers.Length;
            }
            m_NumActivePointers = num;
            for (int i = 1; i < m_Pointers.Length; ++i)
            {
                var pointer = m_Pointers[i];
                bool enabled = i < m_NumActivePointers;
                pointer.m_UiEnabled = enabled;
                pointer.m_Script.gameObject.SetActive(enabled);
                pointer.m_Script.EnableRendering(m_PointersRenderingActive && enabled);
                if (enabled)
                {
                    // Simplified - removed CopyInternals dependency
                }
            }
        }

        int NumFreePlaybackPointers()
        {
            // TODO: Plumb this info from ScenePlayback so it can emulate pointer usage e.g. while
            // keeping all strokes visible.
            int count = 0;
            for (int i = NumUserPointers; i < m_Pointers.Length; ++i)
            {
                if (!m_Pointers[i].m_Script.IsCreatingStroke())
                {
                    ++count;
                }
            }
            Debug.Log($"PointerManager: NumFreePlaybackPointers={count}, m_NumActivePointers={m_NumActivePointers}, NumUserPointers={NumUserPointers}");
            return count;
        }

        /// State-machine update function; always called once per frame.
        public void UpdateLine()
        {
            // For simple drawing, we don't need playback pointers - just check if we have active pointers
            bool pointersAvailable = m_NumActivePointers > 0;
            
            //Debug.Log($"PointerManager: UpdateLine - m_LineEnabled={m_LineEnabled}, pointersAvailable={pointersAvailable}, state={m_CurrentLineCreationState}");

            switch (m_CurrentLineCreationState)
            {
                case LineCreationState.WaitingForInput:
                    if (m_LineEnabled)
                    {
                        if (pointersAvailable)
                        {
                            Transition_WaitingForInput_RecordingInput();
                        }
                        else
                        {
                            Debug.LogWarning("PointerManager: Line enabled but no pointers available");
                        }
                    }
                    else
                    {
                        //Debug.LogWarning("PointerManager: Line not enabled");
                    }
                    break;

                case LineCreationState.RecordingInput:
                    if (m_LineEnabled)
                    {
                        if (pointersAvailable)
                        {
                            // check to see if any pointer's line needs to end
                            bool bStartNewLine = false;
                            for (int i = 0; i < m_NumActivePointers; ++i)
                            {
                                bStartNewLine = bStartNewLine || m_Pointers[i].m_Script.ShouldCurrentLineEnd();
                            }
                            if (bStartNewLine)
                            {
                                //if it has, stop this line and start anew
                                FinalizeLine(isContinue: true);
                                InitiateLine(isContinue: true);
                            }
                        }
                        else
                        {
                            Transition_RecordingInput_WaitingForInput();
                        }
                    }
                    else
                    {
                        Transition_RecordingInput_WaitingForInput();
                    }
                    break;
            }
        }

        private void Transition_WaitingForInput_RecordingInput()
        {
            // Can't check for null as Color is a struct
            // But it's harmless to call this if the color really has been set to black
            if (m_lastChosenColor == Color.black)
            {
                m_lastChosenColor = PointerColor;
            }

            Debug.Log("PointerManager: Starting line creation");
            InitiateLine();
            m_CurrentLineCreationState = LineCreationState.RecordingInput;
        }

        private void Transition_RecordingInput_WaitingForInput()
        {
            // standard mode, just finalize our line and get ready for the next one
            FinalizeLine();
            m_CurrentLineCreationState = LineCreationState.WaitingForInput;
        }

        // Only called during interactive creation.
        // isContinue is true if the line is the logical (if not physical) continuation
        // of a previous line -- ie, previous line ran out of verts and we transparently
        // stopped and started a new one.
        void InitiateLine(bool isContinue = false)
        {
            // Turn off the preview when we start drawing
            for (int i = 0; i < m_NumActivePointers; ++i)
            {
                m_Pointers[i].m_Script.DisablePreviewLine();
                m_Pointers[i].m_Script.AllowPreviewLine(false);
            }

            // Get the active canvas for brush stroke creation
            Transform canvasTransform = SimpleCanvas.ActiveCanvas?.transform ?? transform;
            Debug.Log($"PointerManager: Using canvas transform: {(SimpleCanvas.ActiveCanvas != null ? SimpleCanvas.ActiveCanvas.name : "null")}");
            
            for (int i = 0; i < m_NumActivePointers; ++i)
            {
                PointerScript script = m_Pointers[i].m_Script;
                
                // Convert pointer position from world space to canvas local space
                var localPos = canvasTransform.InverseTransformPoint(script.transform.position);
                var localRot = Quaternion.Inverse(canvasTransform.rotation) * script.transform.rotation;
                var localTf = TrTransform.TRS(localPos, localRot, 1f);
                Debug.Log($"PointerManager: Creating new line at {localTf}, localPos is {localPos}, Canvas transform is {canvasTransform}");
                script.CreateNewLine(canvasTransform, localTf, null);
            }
        }

        // Detach and record lines for all active pointers.
        void FinalizeLine(bool isContinue = false, bool discard = false)
        {
            //discard or solidify every pointer's active line
            for (int i = 0; i < m_NumActivePointers; ++i)
            {
                var pointer = m_Pointers[i].m_Script;
                // XXX: when would an active pointer not be creating a line?
                if (pointer.IsCreatingStroke())
                {
                    bool bDiscardLine = discard || pointer.ShouldDiscardCurrentLine();
                    pointer.DetachLine(bDiscardLine);
                }
            }
            
            // Re-enable preview lines after drawing is complete
            for (int i = 0; i < m_NumActivePointers; ++i)
            {
                m_Pointers[i].m_Script.AllowPreviewLine(true);
            }
        }
    }
} // namespace TiltBrush
