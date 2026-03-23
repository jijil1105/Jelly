Shader "Custom/Bullet"
{
    Properties
    {
        _MainColor("Main Color", Color) = (0.6,0.85,1,0.5)
        _EmissionColor("Emission Color", Color) = (0.15,0.3,0.6,0)
        _FresnelPower("Fresnel Power", Range(0.1,8)) = 2.0
        _Roughness("Roughness", Range(0.0, 1.0)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" } // 不透明より後に描画
        LOD 100
        Cull Off // 裏表描画

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha // シェーダーの色(RGB) * シェーダーのアルファ(A) + 既にあるバッファの色 * (1 - シェーダーのアルファ(A))
            ZWrite Off // 半透明なので深度バッファを無効にして奥の色を遮断しないようにする

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Common.hlsl"

            float4 _MainColor;
            float4 _EmissionColor;
            float _FresnelPower;
            float _Roughness;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float3 localPos : TEXCOORD3;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float4 posOS = v.positionOS;
                o.localPos = posOS.xyz;
                o.worldPos = TransformObjectToWorld(posOS).xyz; // ワールド行列をかける
                o.positionHCS = TransformObjectToHClip(posOS); // MVP行列をかける
                o.worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normalOS));// ワールド空間のNormalに変換
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normal = normalize(IN.worldNormal);
                float3 viewDir = normalize(_WorldSpaceCameraPos - IN.worldPos);
                float3 randNormal = normalize(cyclicNoise(normal + _Time.y, 0.5, 2.5));
                float fresnel = pow(1.0 - abs(dot(lerp(normal, randNormal, _Roughness), viewDir)), _FresnelPower);// 弾の端の光を歪ませる

                return (_MainColor + _EmissionColor) * fresnel;
            }

            ENDHLSL
        }
    }
}
