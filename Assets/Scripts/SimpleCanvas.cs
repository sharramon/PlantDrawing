using UnityEngine;

namespace TiltBrush
{
    /// <summary>
    /// Simplified canvas system for Quest 3 integration.
    /// Provides coordinate system management without full App dependencies.
    /// </summary>
    public class SimpleCanvas : MonoBehaviour
    {
        public static SimpleCanvas ActiveCanvas { get; private set; }
        
        [SerializeField] private Transform m_CanvasTransform;
        [SerializeField] private BatchManager m_BatchManager;
        [SerializeField] private string[] m_BatchKeywords;
        
        private bool m_bInitialized;
        
        public TrTransform Pose
        {
            get { return TrTransform.FromTransform(transform); }
            set { value.ToTransform(transform); }
        }
        
        public TrTransform LocalPose
        {
            get { return TrTransform.FromLocalTransform(transform); }
            set { value.ToLocalTransform(transform); }
        }
        
        public BatchManager BatchManager
        {
            get { return m_BatchManager; }
            set { m_BatchManager = value; }
        }
        
        void Awake()
        {
            if (ActiveCanvas == null)
            {
                ActiveCanvas = this;
            }
            
            if (m_CanvasTransform == null)
            {
                m_CanvasTransform = transform;
            }
            
            Init();
            InitializeBatchManager();
        }
        
        void Init()
        {
            if (m_bInitialized)
            {
                return;
            }
            m_bInitialized = true;
        }
        
        void InitializeBatchManager()
        {
            if (m_BatchManager == null)
            {
                m_BatchManager = new BatchManager();
            }
            
            if (m_BatchKeywords != null)
            {
                m_BatchManager.MaterialKeywords.AddRange(m_BatchKeywords);
            }
            
            m_BatchManager.Init(this);
        }
        
        void Update()
        {
            if (m_BatchManager != null)
            {
                m_BatchManager.Update();
            }
        }
        
        void OnDestroy()
        {
            if (ActiveCanvas == this)
            {
                ActiveCanvas = null;
            }
        }
        
        /// <summary>
        /// Returns a bounds object that encompasses all strokes on the canvas.
        /// </summary>
        public Bounds GetCanvasBoundingBox(bool onlyActive = false)
        {
            return m_BatchManager?.GetBoundsOfAllStrokes(onlyActive) ?? new Bounds();
        }
        
        /// <summary>
        /// Register this canvas for highlight rendering.
        /// </summary>
        public void RegisterHighlight()
        {
            m_BatchManager?.RegisterHighlight();
        }
        
        /// <summary>
        /// Get transform relative to this canvas
        /// </summary>
        public TrTransform GetCanvasTransform(Transform target)
        {
            // For now, return the global transform since we don't have Coords system
            return TrTransform.FromTransform(target);
        }
        
        /// <summary>
        /// Set transform relative to this canvas
        /// </summary>
        public void SetCanvasTransform(Transform target, TrTransform canvasTransform)
        {
            // For now, set the global transform since we don't have Coords system
            canvasTransform.ToTransform(target);
        }
    }
} 