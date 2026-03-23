Shader "Custom/ButterFlyTrail"
{
    Properties
    {
        _TrailWidth ("Trail Width", Float) = 0.2
        _FadePower ("Fade Power", Float) = 1.0
        _Alpha("Alpha", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" } // 不透明より後に描画
        LOD 200 // トレイルの見た目に問題なさそうなので一旦200

        Pass
        {
            Blend One OneMinusSrcAlpha // シェーダーの色(RGB) * シェーダーのアルファ(A) + 既にあるバッファの色 * (1 - シェーダーのアルファ(A))
            ZWrite Off // 半透明なので深度バッファを無効にして奥の色を遮断しないようにする
            Cull Off // 裏表描画

            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geometry
            #pragma fragment frag
            #pragma target 5.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Common.hlsl"

            float _TrailWidth;
            float _FadePower;
            float _Alpha;

            uint _TrailLength;
            uint _VertsPerSeg;// 一つの線分に配置する頂点数
            uint _MaxButterfly;

            StructuredBuffer<ButterflyData> _butterflyBuffer;
            StructuredBuffer<TrailPoint> _butterflyTrailBuffer;

            struct VSOut
            {
                float3 A : TEXCOORD0;
                float3 B : TEXCOORD1;
                float  ageA : TEXCOORD2;
                float  ageB : TEXCOORD3;
                uint   butterflyIndex : TEXCOORD4;
            };

            struct PSIn
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR0;
                float2 uv : TEXCOORD0;
            };

            VSOut vert(uint vid : SV_VertexID)
            {
                VSOut o;

                // トレイルを表現する点を隣り合う点で結んだ線分の数
                uint segCount = max(1u, _TrailLength - 1u);
                // 一匹の蝶のトレイルに存在する頂点数
                uint vertsPerButterfly = segCount * _VertsPerSeg;

                // どの蝶（インスタンス）に属する頂点かを求める
                uint butterflyIndex = vid / vertsPerButterfly;
                // その蝶内での相対インデックス
                uint localID = vid % vertsPerButterfly;
                // どの線分（セグメント）に属するかを求める
                uint segIndex = localID / _VertsPerSeg;

                butterflyIndex = min(butterflyIndex, _MaxButterfly - 1u);

                // バッファの現在書き込み位置を示すインデックス（次に書き込まれるスロット）
                uint writeSlot = _butterflyBuffer[butterflyIndex].trailWriteIndex;
                // 現在フレームで最新のデータが格納されているスロット
                uint newest = (writeSlot - 1u + _TrailLength) % _TrailLength;

                // segIndex分だけ古いスロットを参照して、線分の両端となる 2つのスロットを決定
                // slotA はセグメントの先端、slotB はその次の点
                uint slotA = (newest - segIndex + _TrailLength) % _TrailLength;
                uint slotB = (newest - (segIndex + 1u) + _TrailLength) % _TrailLength;

                // バッファは蝶ごとに連続して格納されている想定なので、インデックスを計算してアクセス
                uint idxA = butterflyIndex * _TrailLength + slotA;
                uint idxB = butterflyIndex * _TrailLength + slotB;
                
                // 実際のワールド空間位置を取得
                o.A = _butterflyTrailBuffer[idxA].pos;
                o.B = _butterflyTrailBuffer[idxB].pos;

                // segIndexを0..1 の範囲に正規化して経過率（age）を計算
                // segIndexが小さいほど最新のトレイルを表現し
                // segIndexが大きいほど古いトレイルのを表現し、トレイルの横幅が狭くなる
                float age = (float)segIndex / max(1.0, (float)segCount);
                o.ageA = saturate(1.0 - age);
                o.ageB = saturate(1.0 - (age + (1.0/segCount)));

                o.butterflyIndex = butterflyIndex;

                return o;
            }

            [maxvertexcount(6)]
            void geometry(point VSOut input[1], inout TriangleStream<PSIn> triStream)
            {
                VSOut v = input[0];

                float3 A = v.A;
                float3 B = v.B;

                // 蝶の進行方向からsegmentの右方向を求める
                float3 dir = normalize(B - A);
                float3 up = float3(0,1,0);
                if (abs(dot(dir, up)) > 0.99) up = float3(1,0,0);
                float3 right = normalize(cross(dir, up));
                
                // 蝶固有のサイズを取得
                float baseSize = _butterflyBuffer[v.butterflyIndex].size;
                // トレイルの横幅を計算
                float wA = baseSize * _TrailWidth * pow(saturate(v.ageA), _FadePower);
                float wB = baseSize * _TrailWidth * pow(saturate(v.ageB), _FadePower);
                
                // トレイル空間での左上
                float3 p0 = A - right * wA;
                // 右上
                float3 p1 = A + right * wA;
                // 左下
                float3 p2 = B - right * wB;
                // 右下
                float3 p3 = B + right * wB;
                
                // 色と経過率に応じた不透明度
                float3 col = _butterflyBuffer[v.butterflyIndex].butterflyColor;
                float alphaA = saturate(v.ageA);
                float alphaB = saturate(v.ageB);

                PSIn o;
                // クリップ空間へ変換して SV_POSITION に渡す
                o.pos = mul(UNITY_MATRIX_VP, float4(p0, 1.0));
                o.color = float4(col * alphaA, alphaA); // プリマルチ済みカラーを出力
                o.uv = float2(0,0);
                triStream.Append(o);

                o.pos = mul(UNITY_MATRIX_VP, float4(p2, 1.0));
                o.color = float4(col * alphaB, alphaB);
                o.uv = float2(0,1);
                triStream.Append(o);

                o.pos = mul(UNITY_MATRIX_VP, float4(p1, 1.0));
                o.color = float4(col * alphaA, alphaA);
                o.uv = float2(1,0);
                triStream.Append(o);
                // ストリップを区切って次の三角形を開始
                triStream.RestartStrip();

                o.pos = mul(UNITY_MATRIX_VP, float4(p1, 1.0));
                o.color = float4(col * alphaA, alphaA);
                o.uv = float2(1,0);
                triStream.Append(o);

                o.pos = mul(UNITY_MATRIX_VP, float4(p2, 1.0));
                o.color = float4(col * alphaB, alphaB);
                o.uv = float2(0,1);
                triStream.Append(o);

                o.pos = mul(UNITY_MATRIX_VP, float4(p3, 1.0));
                o.color = float4(col * alphaB, alphaB);
                o.uv = float2(1,1);
                triStream.Append(o);

                // ストリップを終了
                triStream.RestartStrip();
            }

            float4 frag(PSIn i) : SV_Target
            {
                float3 col = i.color.rgb;
                float a = i.color.a;
                return float4(col, a * _Alpha);
            }

            ENDHLSL
        }
    }

    FallBack Off
}
