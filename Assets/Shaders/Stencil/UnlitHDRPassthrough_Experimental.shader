Shader "Custom/UnlitHDRPassthrough_Experimental"
{
    Properties
    {
        _MainTex ("HDR Texture", 2D) = "white" {}
        _Exposure ("Exposure", Float) = 1.0
        _UseToneMap ("Use Tone Mapping", Float) = 0.0 // 0 = off, 1 = on
        _BaseColor ("Base Color (with Alpha)", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float _Exposure;
            float _UseToneMap;
            float4 _BaseColor;

            float3 ToneMapACES(float3 x)
            {
                return (x * (2.51 * x + 0.03)) / (x * (2.43 * x + 0.59) + 0.14);
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float4 frag (Varyings IN) : SV_Target
            {
                float3 hdrColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).rgb * _Exposure;
                if (_UseToneMap > 0.5)
                {
                    hdrColor = ToneMapACES(hdrColor);
                }

                float3 finalColor = hdrColor * _BaseColor.rgb;
                return float4(finalColor, _BaseColor.a);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/InternalErrorShader"
}
