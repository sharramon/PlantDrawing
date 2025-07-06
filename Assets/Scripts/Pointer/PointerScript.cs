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

using System;
using System.Collections.Generic;
using UnityEngine;

namespace TiltBrush
{
    /// <summary>
    /// Simplified pointer script focused on core brush stroke creation and geometry management.
    /// Designed for integration with Meta's VR/AR package.
    /// </summary>
    public class PointerScript : MonoBehaviour
    {
        /// <summary>
        /// Brush interpolation modes for smoothing brush strokes
        /// </summary>
        public enum BrushLerp
        {
            /// <summary>
            /// No interpolation - use raw input
            /// </summary>
            None,
            
            /// <summary>
            /// Default interpolation - balanced smoothing
            /// </summary>
            Default,
            
            /// <summary>
            /// Aggressive smoothing for very smooth strokes
            /// </summary>
            Smooth,
            
            /// <summary>
            /// Minimal smoothing - preserve detail
            /// </summary>
            Light
        }
        // ---- Core Data Structures

        [System.Serializable]
        public struct ControlPoint
        {
            public Vector3 m_Pos;
            public Quaternion m_Orient;
            public float m_Pressure;
            public uint m_TimestampMs;
        }

        public struct PreviewControlPoint
        {
            public float m_BirthTime;
            public TrTransform m_xf_LS;
        }

        // ---- Inspector Data

        [SerializeField] private Renderer m_Mesh;
        [SerializeField] private Transform m_BrushSizeIndicator;
        [SerializeField] private Transform m_BrushPressureIndicator;
        [SerializeField] private float m_PreviewLineControlPointLife = 1.0f;
        [SerializeField] private float m_PreviewLineIdealLength = 1.0f;
        [SerializeField] private bool m_PreviewLineEnabled = true;
        [SerializeField] private BrushLerp m_BrushLerpMode = BrushLerp.Default;
        [SerializeField] private float m_SmoothingStrength = 0.5f;

        // ---- Private Member Data

        private Color m_CurrentColor = Color.white;
        private BrushDescriptor m_CurrentBrush;
        private float m_CurrentBrushSize = 1.0f;
        private Vector2 m_BrushSizeRange = new Vector2(0.1f, 2.0f);
        private float m_CurrentPressure = 1.0f;
        private BaseBrushScript m_CurrentLine;

        private List<ControlPoint> m_ControlPoints;
        private bool m_LastControlPointIsKeeper;
        private Vector3 m_PreviousPosition;
        private Quaternion m_PreviousRotation;

        private float m_LineLength_CS;

        // ---- Preview Line System

        private bool m_AllowPreviewLine = true;
        private float m_AllowPreviewLineTimer;
        private BaseBrushScript m_PreviewLine;
        private List<PreviewControlPoint> m_PreviewControlPoints;

        // ---- Public Properties

        public BrushDescriptor CurrentBrush
        {
            get { return m_CurrentBrush; }
            set { SetBrush(value); }
        }

        public float BrushSize01
        {
            get
            {
                return Mathf.InverseLerp(m_BrushSizeRange.x, m_BrushSizeRange.y, m_CurrentBrushSize);
            }
            set
            {
                m_CurrentBrushSize = Mathf.Lerp(m_BrushSizeRange.x, m_BrushSizeRange.y, Mathf.Clamp01(value));
                UpdateBrushSizeIndicator();
            }
        }

        public float BrushSizeAbsolute
        {
            get { return m_CurrentBrushSize; }
            set { m_CurrentBrushSize = Mathf.Clamp(value, m_BrushSizeRange.x, m_BrushSizeRange.y); }
        }

        public BaseBrushScript CurrentBrushScript { get { return m_CurrentLine; } }

        public bool IsCreatingStroke() { return m_CurrentLine != null; }
        
        public BrushLerp BrushLerpMode
        {
            get { return m_BrushLerpMode; }
            set { m_BrushLerpMode = value; }
        }
        
        public float SmoothingStrength
        {
            get { return m_SmoothingStrength; }
            set { m_SmoothingStrength = Mathf.Clamp01(value); }
        }

        // ---- Unity Events

        void Awake()
        {
            m_ControlPoints = new List<ControlPoint>();
            m_PreviewControlPoints = new List<PreviewControlPoint>();
            m_PreviousPosition = transform.position;
            m_PreviousRotation = transform.rotation;
            m_AllowPreviewLine = true;
        }

        void Update()
        {
            // Update pressure indicator
            if (m_BrushPressureIndicator != null)
            {
                float scaledPressure = Remap(m_CurrentPressure, 0, 1, m_BrushSizeRange.x, m_CurrentBrushSize);
                m_BrushPressureIndicator.localScale = Vector3.one * scaledPressure;
            }
        }

        // ---- Core Methods

        public void SetColor(Color color)
        {
            m_CurrentColor = color;
            if (m_Mesh != null)
            {
                m_Mesh.material.color = color;
            }
            UpdateBrushSizeIndicator();
            ResetPreviewProperties();
        }

        public void SetBrush(BrushDescriptor brush)
        {
            if (brush != null && brush != m_CurrentBrush)
            {
                m_BrushSizeRange = brush.m_BrushSizeRange;
                BrushSize01 = 0.5f; // Default to middle size
                m_CurrentBrush = brush;
                DisablePreviewLine();
            }
        }

        public void SetPressure(float pressure)
        {
            m_CurrentPressure = Mathf.Clamp01(pressure);
        }

        public float GetPressure()
        {
            return m_CurrentPressure;
        }

        public Color GetCurrentColor()
        {
            return m_CurrentColor;
        }

        // ---- Stroke Creation and Management

        public void CreateNewLine(Transform parent, TrTransform xf_CS, BrushDescriptor brush = null)
        {
            BrushDescriptor desc = brush != null ? brush : m_CurrentBrush;
            if (desc == null)
            {
                Debug.LogWarning($"PointerScript: No brush descriptor available for CreateNewLine");
                return;
            }

            Debug.Log($"PointerScript: Creating new line with brush {desc.Description} at position {xf_CS.translation}");
            m_LineLength_CS = 0.0f;
            m_ControlPoints.Clear();
            
            // Reset position memory to prevent jumping from old position
            m_PreviousPosition = transform.position;
            m_PreviousRotation = transform.rotation;
            
            Debug.Log($"Previous position is {m_PreviousPosition}, Previous rotation is {m_PreviousRotation}");

            m_CurrentLine = BaseBrushScript.Create(
                parent, xf_CS, desc, m_CurrentColor, m_CurrentBrushSize);
            
            // Set m_LastSpawnXf so the first movement delta is correct
            if (m_CurrentLine != null)
            {
                m_CurrentLine.m_LastSpawnXf = xf_CS;
            }
                
            Debug.Log($"PointerScript: Line created: {(m_CurrentLine != null ? "SUCCESS" : "FAILED")}");
        }

        public void UpdateLineFromObject()
        {
            if (m_CurrentLine == null) return;

            // Apply brush smoothing based on lerp mode
            TrTransform smoothedTransform = ApplyBrushSmoothing(transform.position, transform.rotation);
            
            // Get smoothed transform in line's local space
            TrTransform xf_LS = GetTransformForLine(m_CurrentLine.transform, smoothedTransform);
            
            // Update the line with new position
            bool bQuadCreated = m_CurrentLine.UpdatePosition_LS(xf_LS, m_CurrentPressure);
            
            // Store control point
            SetControlPoint(xf_LS, isKeeper: bQuadCreated);
            
            // Update visuals
            m_CurrentLine.ApplyChangesToVisuals();
            
            // Update line length
            float movementDelta = Vector3.Distance(m_PreviousPosition, transform.position);
            m_LineLength_CS += movementDelta;
            
            m_PreviousPosition = transform.position;
        }

        public void DetachLine(bool discard)
        {
            if (m_CurrentLine == null) return;

            if (discard)
            {
                m_CurrentLine.DestroyMesh();
                Destroy(m_CurrentLine.gameObject);
            }
            else
            {
                // Finalize the brush stroke
                m_CurrentLine.FinalizeSolitaryBrush();
            }

            m_CurrentLine = null;
            m_ControlPoints.Clear();
        }

        public List<ControlPoint> GetControlPoints()
        {
            return new List<ControlPoint>(m_ControlPoints);
        }

        public bool ShouldCurrentLineEnd()
        {
            return m_CurrentLine != null && m_CurrentLine.ShouldCurrentLineEnd();
        }

        public bool ShouldDiscardCurrentLine()
        {
            return m_ControlPoints.Count <= 1 || m_CurrentLine == null || m_CurrentLine.ShouldDiscard();
        }

        // ---- Preview Line System

        public void UpdatePointer()
        {
            if (m_CurrentLine != null)
            {
                // Non-preview mode: Update line with new pointer position
                UpdateLineFromObject();
            }
            else if (m_PreviewLineEnabled && m_CurrentBrush != null)
            {
                // Preview mode: Create a preview line if we need one but don't have one
                if (m_AllowPreviewLine && m_PreviewLine == null)
                {
                    m_AllowPreviewLineTimer -= Time.deltaTime;
                    if (m_AllowPreviewLineTimer <= 0.0f)
                    {
                        CreatePreviewLine();
                    }
                }

                if (m_PreviewLine != null)
                {
                    // For most brushes, we control the rebuilding of the preview brush,
                    // since we have the necessary timing information and the brush doesn't.
                    if (m_PreviewLine.AlwaysRebuildPreviewBrush())
                    {
                        RebuildPreviewLine();
                    }
                    else
                    {
                        m_PreviewLine.DecayBrush();
                        m_PreviewLine.UpdatePosition_LS(GetTransformForLine(m_PreviewLine.transform), 1f);
                    }

                    // Always update preview brush after each frame
                    m_PreviewLine.ApplyChangesToVisuals();
                }
            }

            m_PreviousPosition = transform.position;
            m_PreviousRotation = transform.rotation;
        }

        private void CreatePreviewLine()
        {
            if (m_PreviewLine == null && m_CurrentBrush != null && m_CurrentBrush.m_BrushPrefab != null)
            {
                // Create preview line at current position
                TrTransform xf_LS = TrTransform.TRS(transform.position, transform.rotation, 1f);
                BaseBrushScript line = BaseBrushScript.Create(
                    transform.parent, xf_LS,
                    m_CurrentBrush, m_CurrentColor, m_CurrentBrushSize);

                line.gameObject.name = string.Format("Preview {0}", m_CurrentBrush.Description);
                line.SetPreviewMode();

                m_PreviewLine = line;
                ResetPreviewProperties();

                m_PreviewControlPoints.Clear();
            }
        }

        private void RebuildPreviewLine()
        {
            if (m_PreviewLine == null) return;

            // Update head preview control point.
            {
                PreviewControlPoint point = new PreviewControlPoint();
                point.m_BirthTime = Time.realtimeSinceStartup;
                point.m_xf_LS = GetTransformForLine(m_PreviewLine.transform);
                m_PreviewControlPoints.Add(point);
            }

            // Trim old points from the start.
            {
                int i;
                // "-2" because we can't generate geometry without at least 2 points
                for (i = 0; i < m_PreviewControlPoints.Count - 2; ++i)
                {
                    float now = Time.realtimeSinceStartup;
                    if (now - m_PreviewControlPoints[i].m_BirthTime < m_PreviewLineControlPointLife)
                    {
                        break;
                    }
                }
                m_PreviewControlPoints.RemoveRange(0, i);
            }

            // Calculate length in world space.
            float previewLineLength_WS = 0.0f;
            {
                for (int i = 1; i < m_PreviewControlPoints.Count; ++i)
                {
                    previewLineLength_WS += Vector3.Distance(
                        m_PreviewControlPoints[i - 1].m_xf_LS.translation, 
                        m_PreviewControlPoints[i].m_xf_LS.translation);
                }
            }

            if (m_PreviewControlPoints.Count > 0)
            {
                m_PreviewLine.ResetBrushForPreview(m_PreviewControlPoints[0].m_xf_LS);

                // Walk control points and draw preview brush.
                // We use the num segments and length to determine width.
                {
                    float lengthScale01 = Mathf.Min(1f, previewLineLength_WS / m_PreviewLineIdealLength);
                    // Adjust for emphasis on the front.
                    int iFullWidthSegment = Mathf.Max(1, m_PreviewControlPoints.Count - 1 - 2);
                    for (int i = 1; i < m_PreviewControlPoints.Count; ++i)
                    {
                        int iSegment = i - 1;
                        float segmentScale01 = Mathf.Min(1f, (float)iSegment / iFullWidthSegment);
                        // Brush size is: original size * "distance" to front * ratio to ideal length.
                        // "distance" is approximated here by "segment index".
                        m_PreviewLine.UpdatePosition_LS(
                            m_PreviewControlPoints[i].m_xf_LS, segmentScale01 * lengthScale01);
                    }
                }
            }
        }

        public void DisablePreviewLine()
        {
            if (m_PreviewLine)
            {
                m_PreviewLine.DestroyMesh();
                Destroy(m_PreviewLine.gameObject);
                m_PreviewLine = null;
            }
        }

        private void ResetPreviewProperties()
        {
            if (m_PreviewLine)
            {
                m_PreviewLine.SetPreviewProperties(m_CurrentColor, m_CurrentBrushSize);
            }
        }
        
        /// <summary>
        /// Recreates the preview line with current brush size settings.
        /// Call this when brush size changes to update the preview.
        /// </summary>
        public void RecreatePreviewLine()
        {
            if (m_PreviewLine != null)
            {
                DisablePreviewLine();
                CreatePreviewLine();
            }
        }

        public void AllowPreviewLine(bool allow)
        {
            if (m_AllowPreviewLine != allow)
            {
                SetPreviewLineDelayTimer();
                if (!allow)
                {
                    DisablePreviewLine();
                }
            }
            m_AllowPreviewLine = allow;
        }

        public void SetPreviewLineDelayTimer()
        {
            m_AllowPreviewLineTimer = 0.25f;
        }

        // ---- Utility Methods

        private TrTransform GetTransformForLine(Transform line, TrTransform? smoothedTransform = null)
        {
            // Get the line's transform in world space
            TrTransform xfLine_WS = TrTransform.FromTransform(line);
            
            // Get our transform in world space (use smoothed if provided)
            TrTransform xfPointer_WS = smoothedTransform ?? TrTransform.FromTransform(transform);
            
            // Calculate our transform relative to the line
            return TrTransform.InvMul(xfLine_WS, xfPointer_WS);
        }
        
        private TrTransform ApplyBrushSmoothing(Vector3 position, Quaternion rotation)
        {
            switch (m_BrushLerpMode)
            {
                case BrushLerp.None:
                    // No smoothing - return raw input
                    return TrTransform.TRS(position, rotation, 1.0f);
                    
                case BrushLerp.Light:
                    // Light smoothing
                    return TrTransform.TRS(
                        Vector3.Lerp(m_PreviousPosition, position, 0.8f),
                        Quaternion.Slerp(m_PreviousRotation, rotation, 0.8f),
                        1.0f
                    );
                    
                case BrushLerp.Smooth:
                    // Aggressive smoothing
                    return TrTransform.TRS(
                        Vector3.Lerp(m_PreviousPosition, position, 0.6f),
                        Quaternion.Slerp(m_PreviousRotation, rotation, 0.6f),
                        1.0f
                    );
                    
                case BrushLerp.Default:
                default:
                    // Default smoothing - balanced
                    float strength = Mathf.Clamp01(m_SmoothingStrength);
                    return TrTransform.TRS(
                        Vector3.Lerp(m_PreviousPosition, position, 1.0f - strength),
                        Quaternion.Slerp(m_PreviousRotation, rotation, 1.0f - strength),
                        1.0f
                    );
            }
        }

        private void SetControlPoint(TrTransform xf_LS, bool isKeeper)
        {
            ControlPoint controlPoint = new ControlPoint
            {
                m_Pos = xf_LS.translation,
                m_Orient = xf_LS.rotation,
                m_Pressure = m_CurrentPressure,
                m_TimestampMs = (uint)(Time.time * 1000)
            };

            if (m_ControlPoints.Count == 0 || m_LastControlPointIsKeeper)
            {
                m_ControlPoints.Add(controlPoint);
            }
            else
            {
                m_ControlPoints[m_ControlPoints.Count - 1] = controlPoint;
            }

            m_LastControlPointIsKeeper = isKeeper;
        }

        private void UpdateBrushSizeIndicator()
        {
            if (m_BrushSizeIndicator != null)
            {
                m_BrushSizeIndicator.localScale = Vector3.one * m_CurrentBrushSize;
                var renderer = m_BrushSizeIndicator.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = m_CurrentColor;
                }
            }
        }

        private float Remap(float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        public void ShowSizeIndicator(bool show)
        {
            if (m_BrushSizeIndicator != null)
            {
                m_BrushSizeIndicator.gameObject.SetActive(show);
            }
        }

        public void EnableRendering(bool enable)
        {
            if (m_Mesh != null)
            {
                m_Mesh.enabled = enable;
            }
        }
    }
}
