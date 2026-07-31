Shader "Custom/URPGlass"
{
    Properties
    {
        _MainTex      ("Texture", 2D)            = "white" {}
        _Color        ("Tint Color", Color)      = (1, 1, 1, 0.5)
        _Smoothness   ("Smoothness", Range(0,1)) = 0.9
        _Metallic     ("Metallic",   Range(0,1)) = 0.0
        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 4.0
    }
    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off                 // double-sided (seen from inside and outside)

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            // CHEAP: no _MAIN_LIGHT_SHADOWS, no _ADDITIONAL_LIGHTS — glass doesn't need them.
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Color;
                half   _Smoothness;
                half   _Metallic;
                half   _FresnelPower;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float  fogFactor   : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs posInputs  = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   normInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = normInputs.normalWS;
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.fogFactor   = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN, FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                half4 texCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 col    = texCol * _Color;

                // Double-sided: flip the normal on back faces so lighting is correct from inside.
                float3 N = normalize(IN.normalWS) * IS_FRONT_VFACE(cullFace, 1.0, -1.0);
                float3 V = GetWorldSpaceNormalizeViewDir(IN.positionWS);

                // Cheap lighting: main directional light only (NO shadows, NO additional lights) + ambient SH.
                Light mainLight = GetMainLight();
                half  NdotL   = saturate(dot(N, mainLight.direction));
                half3 ambient = SampleSH(N);
                half3 diffuse = col.rgb * (ambient + mainLight.color * NdotL);

                // Cheap glassy edge highlight via Fresnel (replaces full PBR specular/reflections).
                half  fresnel = pow(1.0h - saturate(dot(N, V)), _FresnelPower);
                half3 rim     = fresnel * _Smoothness * mainLight.color;

                half3 rgb   = MixFog(diffuse + rim, IN.fogFactor);
                half  alpha = saturate(col.a + fresnel * _Smoothness); // glass reads more solid at grazing angles
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
