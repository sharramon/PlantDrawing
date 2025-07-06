using UnityEngine;

namespace TiltBrush
{
    /// <summary>
    /// Simplified App configuration for Quest 3 integration.
    /// Provides essential settings without full App dependencies.
    /// </summary>
    public class SimpleAppConfig : MonoBehaviour
    {
        public static SimpleAppConfig Instance { get; private set; }

        [Header("Graphics Settings")]
        [SerializeField] private bool m_GeometryShaderSupported = true;
        [SerializeField] private float m_MetersToUnits = 1.0f;

        [Header("Brush Settings")]
        [SerializeField] private bool m_EnableBrushCatalog = true;

        [Header("Performance Settings")]
        [SerializeField] private bool m_EnableBatchMemoryOptimization = true;
        [Tooltip("When enabled, deletes Batch's GeometryPool after about a second to save memory")]
        [SerializeField] private bool m_LargeMeshSupport = false;
        [Tooltip("When enabled, uses 32-bit indices for larger meshes (more memory, higher vertex limit)")]

        public bool GeometryShaderSuppported => m_GeometryShaderSupported;
        public float MetersToUnits => m_MetersToUnits;
        public bool EnableBrushCatalog => m_EnableBrushCatalog;
        public bool EnableBatchMemoryOptimization => m_EnableBatchMemoryOptimization;
        public bool LargeMeshSupport => m_LargeMeshSupport;

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
    }
} 