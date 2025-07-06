using UnityEngine;
using System.Collections.Generic;

namespace TiltBrush
{
    /// <summary>
    /// Simplified quality controls for Quest 3 integration.
    /// Provides dynamic quality adjustment without full App dependencies.
    /// </summary>
    public class SimpleQualityControls : MonoBehaviour
    {
        public static SimpleQualityControls Instance { get; private set; }

        [Header("Performance Monitoring")]
        [SerializeField] private bool m_EnableAutoQuality = true;
        [SerializeField] private int m_TargetFPS = 72; // Quest 3 target
        [SerializeField] private float m_LowerQualityFPSThreshold = 60f;
        [SerializeField] private float m_HigherQualityFPSThreshold = 70f;
        [SerializeField] private int m_FramesForQualityChange = 30;

        [Header("Quality Settings")]
        [SerializeField] private int m_CurrentQualityLevel = 2;
        [SerializeField] private int m_MaxQualityLevel = 3;
        [SerializeField] private int m_MinQualityLevel = 0;

        [Header("VR Settings")]
        [SerializeField] private float m_ViewportScale = 1.0f;
        [SerializeField] private float m_EyeTextureScale = 1.0f;
        [SerializeField] private int m_MSAALevel = 1;
        [SerializeField] private bool m_EnableHDR = false; // Disabled for Quest 3 performance

        [Header("Stroke Simplification")]
        [SerializeField] private float m_StrokeSimplificationLevel = 0.0f;
        [SerializeField] private bool m_EnableAutoSimplification = true;
        [SerializeField] private int m_TargetMaxControlPoints = 300000; // Lower for Quest 3

        // Performance tracking
        private Queue<float> m_FrameTimeHistory = new Queue<float>();
        private int m_FramesInLastSecond = 0;
        private float m_TimeSinceStart = 0f;
        
        private int m_NumFramesFpsTooLow = 0;
        private int m_NumFramesFpsHighEnough = 0;

        // Events
        public System.Action<int> OnQualityLevelChanged;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            ApplyQualitySettings();
        }

        void Update()
        {
            if (!m_EnableAutoQuality) return;

            // Track frame times
            m_TimeSinceStart += Time.deltaTime;
            m_FrameTimeHistory.Enqueue(Time.deltaTime);
            m_FramesInLastSecond++;

            // Remove old frame times (keep last second)
            while (m_FrameTimeHistory.Count > 0 && m_TimeSinceStart - m_FrameTimeHistory.Peek() > 1.0f)
            {
                m_FrameTimeHistory.Dequeue();
                m_FramesInLastSecond--;
            }

            // Calculate current FPS
            float currentFPS = m_FramesInLastSecond;

            // Check if FPS is too low
            if (currentFPS <= m_LowerQualityFPSThreshold)
            {
                m_NumFramesFpsTooLow++;
                m_NumFramesFpsHighEnough = 0;
            }
            else if (currentFPS >= m_HigherQualityFPSThreshold)
            {
                m_NumFramesFpsHighEnough++;
                m_NumFramesFpsTooLow = 0;
            }
            else
            {
                m_NumFramesFpsTooLow = 0;
                m_NumFramesFpsHighEnough = 0;
            }

            // Adjust quality level
            if (m_NumFramesFpsTooLow >= m_FramesForQualityChange)
            {
                if (m_CurrentQualityLevel > m_MinQualityLevel)
                {
                    SetQualityLevel(m_CurrentQualityLevel - 1);
                    m_NumFramesFpsTooLow = 0;
                }
            }
            else if (m_NumFramesFpsHighEnough >= m_FramesForQualityChange)
            {
                if (m_CurrentQualityLevel < m_MaxQualityLevel)
                {
                    SetQualityLevel(m_CurrentQualityLevel + 1);
                    m_NumFramesFpsHighEnough = 0;
                }
            }
        }

        public void SetQualityLevel(int level)
        {
            if (level == m_CurrentQualityLevel) return;
            
            m_CurrentQualityLevel = Mathf.Clamp(level, m_MinQualityLevel, m_MaxQualityLevel);
            ApplyQualitySettings();
            
            OnQualityLevelChanged?.Invoke(m_CurrentQualityLevel);
            
            Debug.Log($"Quality level changed to: {m_CurrentQualityLevel}");
        }

        private void ApplyQualitySettings()
        {
            // Apply quality level settings
            switch (m_CurrentQualityLevel)
            {
                case 0: // Low quality
                    m_ViewportScale = 0.7f;
                    m_EyeTextureScale = 0.7f;
                    m_MSAALevel = 0;
                    m_EnableHDR = false;
                    m_StrokeSimplificationLevel = 2.0f;
                    break;
                    
                case 1: // Medium quality
                    m_ViewportScale = 0.85f;
                    m_EyeTextureScale = 0.85f;
                    m_MSAALevel = 1;
                    m_EnableHDR = false;
                    m_StrokeSimplificationLevel = 1.0f;
                    break;
                    
                case 2: // High quality
                    m_ViewportScale = 1.0f;
                    m_EyeTextureScale = 1.0f;
                    m_MSAALevel = 2;
                    m_EnableHDR = false;
                    m_StrokeSimplificationLevel = 0.5f;
                    break;
                    
                case 3: // Ultra quality
                    m_ViewportScale = 1.0f;
                    m_EyeTextureScale = 1.0f;
                    m_MSAALevel = 4;
                    m_EnableHDR = true;
                    m_StrokeSimplificationLevel = 0.0f;
                    break;
            }

            // Apply VR settings
            UnityEngine.XR.XRSettings.renderViewportScale = m_ViewportScale;
            UnityEngine.XR.XRSettings.eyeTextureResolutionScale = m_EyeTextureScale;

            // Apply Unity quality settings
            QualitySettings.antiAliasing = m_MSAALevel;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
            
            // Apply HDR settings to cameras
            Camera[] cameras = FindObjectsOfType<Camera>();
            foreach (var camera in cameras)
            {
                camera.allowHDR = m_EnableHDR;
            }
        }

        public float GetStrokeSimplificationLevel()
        {
            return m_EnableAutoSimplification ? m_StrokeSimplificationLevel : 0.0f;
        }

        public void SetStrokeSimplificationLevel(float level)
        {
            m_StrokeSimplificationLevel = Mathf.Clamp(level, 0.0f, 5.0f);
        }

        public int GetCurrentFPS()
        {
            return m_FramesInLastSecond;
        }

        public int GetQualityLevel()
        {
            return m_CurrentQualityLevel;
        }

        public void ResetQuality()
        {
            SetQualityLevel(2); // Default to high quality
        }
    }
} 