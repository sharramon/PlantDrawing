using System.Collections.Generic;
using UnityEngine;

namespace TiltBrush
{
    /// <summary>
    /// Simplified brush catalog for Quest 3 integration.
    /// Manages brush descriptors without complex dependencies.
    /// </summary>
    public class SimpleBrushCatalog : MonoBehaviour
    {
        public static SimpleBrushCatalog Instance { get; private set; }

        [Header("Brush Management")]
        [SerializeField] private List<BrushDescriptor> m_Brushes = new List<BrushDescriptor>();

        private Dictionary<System.Guid, BrushDescriptor> m_BrushLookup = new Dictionary<System.Guid, BrushDescriptor>();

        public List<BrushDescriptor> Brushes => m_Brushes;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeBrushLookup();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeBrushLookup()
        {
            m_BrushLookup.Clear();
            foreach (BrushDescriptor brush in m_Brushes)
            {
                if (brush != null && brush.m_Guid != System.Guid.Empty)
                {
                    m_BrushLookup[brush.m_Guid] = brush;
                }
            }
        }

        /// <summary>
        /// Gets a brush by its GUID
        /// </summary>
        public BrushDescriptor GetBrush(System.Guid brushGuid)
        {
            if (m_BrushLookup.TryGetValue(brushGuid, out BrushDescriptor brush))
            {
                return brush;
            }
            
            Debug.LogWarning($"Brush with GUID {brushGuid} not found in catalog");
            return null;
        }

        /// <summary>
        /// Adds a brush to the catalog
        /// </summary>
        public void AddBrush(BrushDescriptor brush)
        {
            if (brush != null && brush.m_Guid != System.Guid.Empty)
            {
                if (!m_Brushes.Contains(brush))
                {
                    m_Brushes.Add(brush);
                    m_BrushLookup[brush.m_Guid] = brush;
                }
            }
        }

        /// <summary>
        /// Removes a brush from the catalog
        /// </summary>
        public void RemoveBrush(BrushDescriptor brush)
        {
            if (brush != null)
            {
                m_Brushes.Remove(brush);
                m_BrushLookup.Remove(brush.m_Guid);
            }
        }

        /// <summary>
        /// Gets the default brush (first in the list)
        /// </summary>
        public BrushDescriptor GetDefaultBrush()
        {
            return m_Brushes.Count > 0 ? m_Brushes[0] : null;
        }
    }
} 