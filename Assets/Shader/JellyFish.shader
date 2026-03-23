Shader "Custom/JellyFish"
{
    Properties
    {
        _MainTex("Main Tex", 2D) = "white" {}
        _MainColor("Main Color", Color) = (1,1,1,1)
        _FresnelColor("Fresnel Color", Color) = (1,1,1,1)
        _FresnelPow("Fresnel Pow", Float) = 1.0
        _VertexWaveIntensity("Vertex Wave Intensity", Float) = 0.0
        _VertexWaveFrequency("Vertex Wave Frequency", Float) = 8.0
        _VertexWavePow("Vertex Wave Pow", Float) = 1.0
        _EmissionColor("Emission Color", Color) = (0,0,0,0)
        _EmissionPow("Emission Pow", Float) = 1.0
        _Spectrum("Spectrum", Float) = 0.0
        _Mix("Mix", Float) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        LOD 100
        Cull Off


        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Common.hlsl"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainColor;
            float4 _FresnelColor;
            float _FresnelPow;
            float _VertexWaveIntensity;
            float _VertexWaveFrequency;
            float _VertexWavePow;
            float4 _EmissionColor;
            float _EmissionPow;
            float _Spectrum;
            float _Mix;

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            StructuredBuffer<JellyFishData> _jellyFishBuffer;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float4 color : COLOR;
            };

            Varyings vert(Attributes v, uint instanceID : SV_InstanceID)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                Varyings o;

                float time = _Time.y;
                float wave = 0;

                if (_VertexWaveIntensity > 0.0001)
                {
                    float phase = (time - v.uv.x) * _VertexWaveFrequency;
                    wave = pow(abs(sin(phase)), _VertexWavePow) * _VertexWaveIntensity * 0.1;
                }

                float3 pos = v.positionOS.xyz + v.normalOS * wave;
                pos.y += wave;
                JellyFishData data = _jellyFishBuffer[instanceID];
                pos *= data.size;
                float3x3 rot = CreateRotationMatrixY(data.jellyFishDir);
                float3 worldPos = mul(rot, pos) + data.jellyFishPos;
                o.worldPos = worldPos;
                o.worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normalOS));
                o.positionHCS = TransformObjectToHClip(worldPos);
                o.uv = v.uv;
                o.color = float4(data.jellyFishColor, 1);
                return o;
            }

            half4 frag(Varyings IN, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                float3 normal = normalize(IN.worldNormal);
                if (!isFrontFace)
                {
                    normal = -normal;
                }

                float2 uv = IN.uv;
                float4 src = tex2D(_MainTex, uv) * _MainColor;

                float3 viewDir = normalize(_WorldSpaceCameraPos - IN.worldPos);
                float ndotv = abs(saturate(dot(normal, viewDir)));
                float fresnel = pow(1.0 - ndotv, _FresnelPow);
                float3 fresnelCol = _FresnelColor.rgb * fresnel;

                float waveT = frac(_Time.y - IN.uv.x);
                float emissionFactor = pow(saturate(abs(sin(waveT * 3.14159))), _EmissionPow);
                float3 emission = _EmissionColor.rgb * emissionFactor;
                emission = lerp(emission, IN.color.rgb, _Mix);
                float3 colorOut = src.rgb + fresnelCol + emission;
                colorOut = lerp(colorOut, IN.color.rgb, _Mix);
                return half4(colorOut, src.a * _Spectrum);
            }

            ENDHLSL
        }
    }
}
