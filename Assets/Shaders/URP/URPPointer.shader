Shader "Custom/URPPointer"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _MainTex("Main Texture", 2D) = "white" {}
        _OutlineWidth("Outline Width", Float) = 0.02
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }
        LOD 100

        // Pass 1: Outline
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "UniversalForward" }
            Cull Front
            ZWrite Off
            ZTest Less

            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            float _OutlineWidth;

            Varyings OutlineVert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));
                positionWS += normalWS * _OutlineWidth;
                OUT.positionHCS = TransformWorldToHClip(positionWS);
                return OUT;
            }

            half4 OutlineFrag(Varyings IN) : SV_Target
            {
                return half4(0, 0, 0, 1);
            }
            ENDHLSL
        }

        // Pass 2: Main Mesh
        Pass
        {
            Name "Main"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex MainVert
            #pragma fragment MainFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            float4 _BaseColor;

            Varyings MainVert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 MainFrag(Varyings IN) : SV_Target
            {
                return half4(_BaseColor.rgb, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
