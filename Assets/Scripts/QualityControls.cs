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
using System.Collections.Generic;

namespace TiltBrush
{
    /// <summary>
    /// QualityControls manages dynamic quality adjustment for optimal performance.
    /// Adapted for Quest 3 integration with simplified dependencies.
    /// </summary>
    public class QualityControls : MonoBehaviour
    {
        [Header("Quality Settings")]
        [SerializeField] private int m_BaseQualityLevel = 2;
        [SerializeField] private float m_TargetFrameRate = 72f; // Quest 3 target
        [SerializeField] private float m_MinFrameRate = 60f;
        [SerializeField] private float m_QualityAdjustmentInterval = 2f;
        [SerializeField] private int m_MaxQualityLevel = 5;
        [SerializeField] private int m_MinQualityLevel = 0;

        [Header("VR-Specific Settings")]
        [SerializeField] private float m_ViewportScalingMin = 0.5f;
        [SerializeField] private float m_ViewportScalingMax = 1.0f;
        [SerializeField] private float m_EyeTextureScalingMin = 0.5f;
        [SerializeField] private float m_EyeTextureScalingMax = 1.0f;
        [SerializeField] private int m_MsaaLevelMin = 0;
        [SerializeField] private int m_MsaaLevelMax = 4;

        [Header("Stroke Simplification")]
        [SerializeField] private float m_StrokeSimplificationMin = 0.0f;
        [SerializeField] private float m_StrokeSimplificationMax = 0.5f;
        [SerializeField] private bool m_EnableStrokeSimplification = true;

        [Header("Performance Monitoring")]
        [SerializeField] private bool m_ShowPerformanceStats = false;
        [SerializeField] private int m_PerformanceHistorySize = 60;

        private float m_LastQualityAdjustmentTime;
        private float m_LastFrameTime;
        private float m_AverageFrameTime;
        private Queue<float> m_FrameTimeHistory;
        private int m_CurrentQualityLevel;
        private bool m_QualityControlsEnabled = true;

        // Performance tracking
        private float m_FrameRate;
        private float m_LastFrameRateUpdate;
        private int m_FrameCount;

        void Awake()
        {
            InitializeQualityControls();
        }

        void Start()
        {
            ApplyInitialQualitySettings();
        }

        void Update()
        {
            if (!m_QualityControlsEnabled) return;

            UpdatePerformanceMetrics();
            
            if (Time.time - m_LastQualityAdjustmentTime > m_QualityAdjustmentInterval)
            {
                AdjustQualityIfNeeded();
                m_LastQualityAdjustmentTime = Time.time;
            }
        }

        private void InitializeQualityControls()
        {
            m_FrameTimeHistory = new Queue<float>();
            m_CurrentQualityLevel = m_BaseQualityLevel;
            m_LastQualityAdjustmentTime = Time.time;
            m_LastFrameTime = Time.time;
            m_AverageFrameTime = 1f / m_TargetFrameRate;

            // Initialize frame time history
            for (int i = 0; i < m_PerformanceHistorySize; i++)
            {
                m_FrameTimeHistory.Enqueue(1f / m_TargetFrameRate);
            }
        }

        private void ApplyInitialQualitySettings()
        {
            // Apply quality level
            if (SimpleUserConfig.Instance != null && SimpleUserConfig.Instance.Profiling.QualityLevel >= 0)
            {
                m_CurrentQualityLevel = SimpleUserConfig.Instance.Profiling.QualityLevel;
            }
            QualitySettings.SetQualityLevel(m_CurrentQualityLevel, true);

            // Apply viewport scaling
            float viewportScaling = GetViewportScaling();
            if (viewportScaling > 0)
            {
                UnityEngine.XR.XRSettings.eyeTextureResolutionScale = viewportScaling;
            }

            // Apply MSAA
            int msaaLevel = GetMsaaLevel();
            if (msaaLevel > 0)
            {
                QualitySettings.antiAliasing = msaaLevel;
            }

            // Apply stroke simplification
            if (m_EnableStrokeSimplification)
            {
                float simplification = GetStrokeSimplification();
                if (simplification >= 0)
                {
                    // Apply stroke simplification to active strokes
                    ApplyStrokeSimplification(simplification);
                }
            }
        }

        private void UpdatePerformanceMetrics()
        {
            float currentTime = Time.time;
            float deltaTime = currentTime - m_LastFrameTime;
            m_LastFrameTime = currentTime;

            // Update frame time history
            m_FrameTimeHistory.Enqueue(deltaTime);
            if (m_FrameTimeHistory.Count > m_PerformanceHistorySize)
            {
                m_FrameTimeHistory.Dequeue();
            }

            // Calculate average frame time
            float totalFrameTime = 0f;
            foreach (float frameTime in m_FrameTimeHistory)
            {
                totalFrameTime += frameTime;
            }
            m_AverageFrameTime = totalFrameTime / m_FrameTimeHistory.Count;

            // Update frame rate
            m_FrameCount++;
            if (currentTime - m_LastFrameRateUpdate >= 1f)
            {
                m_FrameRate = m_FrameCount / (currentTime - m_LastFrameRateUpdate);
                m_FrameCount = 0;
                m_LastFrameRateUpdate = currentTime;
            }
        }

        private void AdjustQualityIfNeeded()
        {
            float targetFrameTime = 1f / m_TargetFrameRate;
            float minFrameTime = 1f / m_MinFrameRate;

            // If performance is poor, reduce quality
            if (m_AverageFrameTime > targetFrameTime && m_CurrentQualityLevel > m_MinQualityLevel)
            {
                m_CurrentQualityLevel--;
                ApplyQualitySettings();
                Debug.Log($"Quality reduced to level {m_CurrentQualityLevel} due to poor performance");
            }
            // If performance is good, increase quality
            else if (m_AverageFrameTime < minFrameTime && m_CurrentQualityLevel < m_MaxQualityLevel)
            {
                m_CurrentQualityLevel++;
                ApplyQualitySettings();
                Debug.Log($"Quality increased to level {m_CurrentQualityLevel} due to good performance");
            }
        }

        private void ApplyQualitySettings()
        {
            QualitySettings.SetQualityLevel(m_CurrentQualityLevel, true);

            // Adjust viewport scaling based on quality level
            float viewportScaling = Mathf.Lerp(m_ViewportScalingMin, m_ViewportScalingMax, 
                (float)m_CurrentQualityLevel / m_MaxQualityLevel);
            UnityEngine.XR.XRSettings.eyeTextureResolutionScale = viewportScaling;

            // Adjust MSAA based on quality level
            int msaaLevel = Mathf.RoundToInt(Mathf.Lerp(m_MsaaLevelMin, m_MsaaLevelMax, 
                (float)m_CurrentQualityLevel / m_MaxQualityLevel));
            QualitySettings.antiAliasing = msaaLevel;

            // Adjust stroke simplification based on quality level
            if (m_EnableStrokeSimplification)
            {
                float simplification = Mathf.Lerp(m_StrokeSimplificationMax, m_StrokeSimplificationMin, 
                    (float)m_CurrentQualityLevel / m_MaxQualityLevel);
                ApplyStrokeSimplification(simplification);
            }
        }

        private float GetViewportScaling()
        {
            if (SimpleUserConfig.Instance != null && SimpleUserConfig.Instance.Profiling.ViewportScaling > 0)
            {
                return SimpleUserConfig.Instance.Profiling.ViewportScaling;
            }
            return 1.0f; // Default to full resolution
        }

        private int GetMsaaLevel()
        {
            if (SimpleUserConfig.Instance != null && SimpleUserConfig.Instance.Profiling.MsaaLevel > 0)
            {
                return SimpleUserConfig.Instance.Profiling.MsaaLevel;
            }
            return 0; // Default to no MSAA
        }

        private float GetStrokeSimplification()
        {
            if (SimpleUserConfig.Instance != null && SimpleUserConfig.Instance.Profiling.HasStrokeSimplification)
            {
                return SimpleUserConfig.Instance.Profiling.StrokeSimplification;
            }
            return -1f; // Default to no simplification
        }

        private void ApplyStrokeSimplification(float simplificationFactor)
        {
            // Find all active strokes and apply simplification
            BaseBrushScript[] brushes = FindObjectsOfType<BaseBrushScript>();
            foreach (BaseBrushScript brush in brushes)
            {
                if (brush != null && brush.enabled)
                {
                    // Apply simplification to the brush's stroke geometry
                    brush.ApplyStrokeSimplification(simplificationFactor);
                }
            }
        }

        public void SetQualityLevel(int level)
        {
            m_CurrentQualityLevel = Mathf.Clamp(level, m_MinQualityLevel, m_MaxQualityLevel);
            ApplyQualitySettings();
        }

        public void EnableQualityControls(bool enable)
        {
            m_QualityControlsEnabled = enable;
        }

        public float GetCurrentFrameRate()
        {
            return m_FrameRate;
        }

        public float GetAverageFrameTime()
        {
            return m_AverageFrameTime;
        }

        public int GetCurrentQualityLevel()
        {
            return m_CurrentQualityLevel;
        }

        void OnGUI()
        {
            if (m_ShowPerformanceStats)
            {
                GUILayout.BeginArea(new Rect(10, 10, 300, 200));
                GUILayout.Label($"FPS: {m_FrameRate:F1}");
                GUILayout.Label($"Avg Frame Time: {m_AverageFrameTime * 1000:F1}ms");
                GUILayout.Label($"Quality Level: {m_CurrentQualityLevel}");
                GUILayout.Label($"Viewport Scale: {UnityEngine.XR.XRSettings.eyeTextureResolutionScale:F2}");
                GUILayout.Label($"MSAA: {QualitySettings.antiAliasing}");
                GUILayout.EndArea();
            }
        }
    }
}
