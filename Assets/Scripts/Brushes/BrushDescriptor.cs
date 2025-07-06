// Copyright 2020 The Tilt Brush Authors
// Modified for minimal runtime use

using System;
using System.Collections.Generic;
using UnityEngine;

namespace TiltBrush
{
    /// Minimal data shared by all brushes.
    public class BrushDescriptor : ScriptableObject
    {
        [Header("Identity")]
        public SerializableGuid m_Guid;
        public string m_DurableName;
        public GameObject m_BrushPrefab;

        [Tooltip("A category that can be used to determine whether a brush will be included in the brush panel")]
        public List<string> m_Tags = new List<string> { "default" };

        [Tooltip("When upgrading a brush, populate this field with the prior version")]
        public BrushDescriptor m_Supersedes;
        [NonSerialized]
        public BrushDescriptor m_SupersededBy;

        [Tooltip("True if this brush looks identical to the version it supersedes")]
        public bool m_LooksIdentical = false;

        [Header("GUI")]
        public Texture2D m_ButtonTexture;
        public string m_DescriptionExtra;
        [NonSerialized] public bool m_HiddenInGui = false;

        public string Description
        {
            get
            {
                try
                {
                    return m_DurableName;
                }
                catch
                {
                    return m_DurableName;
                }
            }
        }

        [Header("Audio")]
        public AudioClip[] m_BrushAudioLayers;
        public float m_BrushAudioBasePitch;
        public float m_BrushAudioMaxPitchShift = 0.05f;
        public float m_BrushAudioMaxVolume;
        public float m_BrushVolumeUpSpeed = 4f;
        public float m_BrushVolumeDownSpeed = 4f;
        public float m_VolumeVelocityRangeMultiplier = 1f;
        public bool m_AudioReactive;
        public AudioClip m_ButtonAudio;

        [Header("Material")]
        [SerializeField] private Material m_Material;
        public int m_TextureAtlasV;
        public float m_TileRate;
        public bool m_UseBloomSwatchOnColorPicker;

        [Header("Size")]
        public Vector2 m_BrushSizeRange;
        [SerializeField]
        private Vector2 m_PressureSizeRange = new Vector2(.1f, 1f);
        public float m_SizeVariance;
        [Range(.001f, 1)]
        public float m_PreviewPressureSizeMin = .001f;

        [Header("Color")]
        public float m_Opacity;
        public Vector2 m_PressureOpacityRange;
        [Range(0, 1)] public float m_ColorLuminanceMin;
        [Range(0, 1)] public float m_ColorSaturationMax;

        [Header("Particle")]
        public float m_ParticleSpeed;
        public float m_ParticleRate;
        public float m_ParticleInitialRotationRange;
        public bool m_RandomizeAlpha;

        [Header("QuadBatch")]
        public float m_SprayRateMultiplier;
        public float m_RotationVariance;
        public float m_PositionVariance;
        public Vector2 m_SizeRatio;

        [Header("Geometry Brush")]
        public bool m_M11Compatibility;

        [Header("Tube")]
        public float m_SolidMinLengthMeters_PS = 0.002f;
        public bool m_TubeStoreRadiusInTexcoord0Z;

        [Header("Misc")]
        public bool m_RenderBackfaces;
        public bool m_BackIsInvisible;
        public float m_BackfaceHueShift;
        public float m_BoundsPadding;
        public bool m_PlayBackAtStrokeGranularity;

        public Material Material => m_Material;
        [Header("Simplification Settings")]
        public bool m_SupportsSimplification = true;
        public int m_HeadMinPoints = 1;
        public int m_HeadPointStep = 1;
        public int m_TailMinPoints = 1;
        public int m_TailPointStep = 1;
        public int m_MiddlePointStep = 0;

        public float PressureSizeMin(bool previewMode)
        {
            return previewMode ? m_PreviewPressureSizeMin : m_PressureSizeRange.x;
        }

        public override string ToString()
        {
            return $"BrushDescriptor<{name} {Description} {m_Guid}>";
        }
    }
}
