Shader "Custom/DotPaintingPostProcess"
{
    Properties
    {
        _MainTex("Source", 2D) = "white" {}
        _DotSize("Dot Size (px)", Float) = 8.0
        _PosterizeLevels("Posterize Levels", Range(2, 100)) = 6
    }
    SubShader
    {
        // ポスプロ用途なので最前面で描画する
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // 入力テクスチャ
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
             
            // パラメータ
            float _DotSize;
            float _PosterizeLevels;

            // 頂点→フラグメント間の構造体
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            // フルスクリーン三角形を SV_VertexID で生成する
            // id によって 4 頂点の UV を決め、クリップ空間位置を返す
            v2f Vert(uint id : SV_VertexID)
            {
                v2f o;
                /*
                id         (id<<1)       (id<<1)&2   id&2    uv
                00         000           0           0      (0,0)
                01         010           2           0      (2,0)
                10         100           0           2      (0,2)
                11         110           2           2      (2,2)
                */
                float2 uv = float2((id << 1) & 2, id & 2);
                o.uv = uv;
                o.pos = float4(uv * 2.0 - 1.0, 0.0, 1.0);
                return o;
            }

            // 色をlevelsに量子化する関数
            // levels が小さいほど粗いポスタライズになる
            float3 Posterize(float3 c, float levels)
            {
                return floor(c * levels) / levels;
            }

            half4 Frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 screen = float2(_ScreenParams.x, _ScreenParams.y);
                float2 texel = 1.0 / screen;
                // ドットにする
                float3 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, floor(uv / (_DotSize * texel)) * (_DotSize * texel)).rgb;
                float3 final = Posterize(col, max(2.0, _PosterizeLevels));
                return half4(final, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}

