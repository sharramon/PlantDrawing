Shader "Custom/URPSingleSided"
{
    Properties
    {
        _MainTex ("Base Map", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" }
        LOD 200
        Cull Back

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 tangentWS : TEXCOORD2;
                float3 bitangentWS : TEXCOORD3;
                float4 color : COLOR;
                float3 viewDirWS : TEXCOORD4;
                float fogCoord : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _MainTex_ST;
            float4 _Color;
            float _Cutoff;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                float3 positionWS = TransformObjectToWorld(IN.positionOS);
                OUT.positionHCS = TransformWorldToHClip(float4(positionWS, 1.0));
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);

                float3 tangentWS = TransformObjectToWorldDir(IN.tangentOS.xyz);
                float tangentSign = IN.tangentOS.w;
                OUT.tangentWS = tangentWS;
                OUT.bitangentWS = cross(OUT.normalWS, tangentWS) * tangentSign;

                OUT.color = IN.color;
                OUT.viewDirWS = normalize(_WorldSpaceCameraPos - positionWS);
                OUT.fogCoord = ComputeFogFactor(OUT.positionHCS.z);
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv));
                float3x3 TBN = float3x3(IN.tangentWS, IN.bitangentWS, IN.normalWS);
                float3 normalWS = normalize(mul(normalTS, TBN));

                float4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _Color * IN.color;
                clip(baseColor.a - _Cutoff);

                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float3 lighting = mainLight.color * NdotL;

                float3 finalColor = baseColor.rgb * lighting;
                finalColor = MixFog(finalColor, IN.fogCoord);

                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/InternalErrorShader"
}
