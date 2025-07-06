using System;
using UnityEngine;

namespace TiltBrush
{
    /// <summary>
    /// Simplified UserConfig for Quest 3 integration.
    /// Provides essential profiling settings without full App dependencies.
    /// </summary>
    public class SimpleUserConfig : MonoBehaviour
    {
        public static SimpleUserConfig Instance { get; private set; }

        [Header("Profiling Settings")]
        [SerializeField] private int m_QualityLevel = -1; // -1 = auto
        [SerializeField] private float m_ViewportScaling = 0f; // 0 = auto
        [SerializeField] private float m_EyeTextureScaling = 0f; // 0 = auto
        [SerializeField] private int m_GlobalMaximumLOD = 0; // 0 = auto
        [SerializeField] private int m_MsaaLevel = 0; // 0 = auto
        [SerializeField] private float m_StrokeSimplification = -1f; // -1 = auto
        [SerializeField] private bool m_AutoProfile = false;

        [Header("Flags")]
        [SerializeField] private bool m_DisableAudio = false;
        [SerializeField] private bool m_LargeMeshSupport = false;

        public ProfilingConfig Profiling { get; private set; }
        public FlagsConfig Flags { get; private set; }

        [Serializable]
        public struct ProfilingConfig
        {
            public int QualityLevel;
            public float ViewportScaling;
            public float EyeTextureScaling;
            public int GlobalMaximumLOD;
            public int MsaaLevel;
            public float StrokeSimplification;
            public bool HasStrokeSimplification;
            public bool AutoProfile;
        }

        [Serializable]
        public struct FlagsConfig
        {
            public bool DisableAudio;
            public bool LargeMeshSupport;
        }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeConfigs();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeConfigs()
        {
            Profiling = new ProfilingConfig
            {
                QualityLevel = m_QualityLevel,
                ViewportScaling = m_ViewportScaling,
                EyeTextureScaling = m_EyeTextureScaling,
                GlobalMaximumLOD = m_GlobalMaximumLOD,
                MsaaLevel = m_MsaaLevel,
                StrokeSimplification = m_StrokeSimplification,
                HasStrokeSimplification = m_StrokeSimplification >= 0f,
                AutoProfile = m_AutoProfile
            };

            Flags = new FlagsConfig
            {
                DisableAudio = m_DisableAudio,
                LargeMeshSupport = m_LargeMeshSupport
            };
        }

        public void UpdateProfilingConfig()
        {
            Profiling = new ProfilingConfig
            {
                QualityLevel = m_QualityLevel,
                ViewportScaling = m_ViewportScaling,
                EyeTextureScaling = m_EyeTextureScaling,
                GlobalMaximumLOD = m_GlobalMaximumLOD,
                MsaaLevel = m_MsaaLevel,
                StrokeSimplification = m_StrokeSimplification,
                HasStrokeSimplification = m_StrokeSimplification >= 0f,
                AutoProfile = m_AutoProfile
            };
        }
    }
} 