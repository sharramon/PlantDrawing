using UnityEngine;

namespace TiltBrush
{
    /// <summary>
    /// Simplified App singleton for Quest 3 integration.
    /// Provides basic functionality without full App dependencies.
    /// </summary>
    public class SimpleApp : MonoBehaviour
    {
        public static SimpleApp Instance { get; private set; }
        
        [SerializeField] private SimpleCanvas m_ActiveCanvas;
        [SerializeField] private SimpleAppConfig m_Config;
        
        public SimpleCanvas ActiveCanvas 
        { 
            get { return m_ActiveCanvas; }
            set { m_ActiveCanvas = value; }
        }
        
        public SimpleAppConfig Config
        {
            get { return m_Config; }
            set { m_Config = value; }
        }

         // ------------------------------------------------------------
        // Constants and types
        // ------------------------------------------------------------

        public const float METERS_TO_UNITS = 10f;
        public const float UNITS_TO_METERS = .1f;
        
        public float CurrentSketchTime { get { return Time.time; } }
        
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
            // Auto-find canvas if not assigned
            if (m_ActiveCanvas == null)
            {
                m_ActiveCanvas = FindObjectOfType<SimpleCanvas>();
            }
        }
        
        /// <summary>
        /// Set the active canvas for drawing
        /// </summary>
        public void SetActiveCanvas(SimpleCanvas canvas)
        {
            m_ActiveCanvas = canvas;
        }
    }
} 