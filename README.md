# コンピュートシェーダーの解説
# ComputePerticle.compute
・蝶の計算

    float3 butterflyOffsetPos = float3(rnd(seed), rnd(seed), rnd(seed));
    float3 prebutterflyPos = cyclicNoise(butterflyTime - 0.01 + butterflyOffsetPos, Pers, Lacu);
    float3 butterflyPos = cyclicNoise(butterflyTime + butterflyOffsetPos, Pers, Lacu);
    float3 butterflyDir = normalize(butterflyPos - prebutterflyPos);
    
    _butterflyBuffer[index].butterflyPos = butterflyPos;
    _butterflyBuffer[index].butterflyDir = lerp(_butterflyBuffer[index].butterflyDir, butterflyDir, _SmoothFactor);
    _butterflyBuffer[index].butterflyColor = float3(rnd(seed), rnd(seed), rnd(seed));
    _butterflyBuffer[index].size = 0.1 + smoothstep(0.0, 1.0, rnd(seed)) * 0.7;
この処理ではハッシュ値を使い各蝶毎のワールド座標、進行方向、色、蝶の大きさを計算しています。座標計算(蝶のアニメーション)に使用しているノイズ関数はOb5vrさんの記事で紹介されているノイズ関数を改造して作りました。

https://scrapbox.io/0b5vr/Cyclic_Noise

・蝶の座標をトレイルバッファに記録

    _butterflyBuffer[index].trailAccum += _DeltaTime;
    uint maxSamples = 8;
    uint loopCount = 0;
    float sampleInterval = max(0.001, _ButterflyTrailSampleInterval);
    while (_butterflyBuffer[index].trailAccum >= sampleInterval && loopCount < maxSamples)
    {
        _butterflyBuffer[index].trailAccum -= sampleInterval;
        uint butterflyIndex = index;
        uint writeSlot = _butterflyBuffer[butterflyIndex].trailWriteIndex;
        uint trailLen = max(1u, _TrailLength);
        uint trailIndex = butterflyIndex * trailLen + writeSlot;
    
        TrailPoint tp;
        tp.pos = butterflyPos;
        tp.time = _Time;
        _butterflyTrailBuffer[trailIndex] = tp;
        writeSlot = (writeSlot + 1) % trailLen;
        _butterflyBuffer[butterflyIndex].trailWriteIndex = writeSlot;
        
        loopCount++;
    }
この処理ではsampleIntervalをインターバルとして蝶の座標をトレイルバッファに記録しています。書き込み後に"writeSlot = (writeSlot + 1) % trailLen"でバッファへの書き込み位置をずらし、次のフレームで新しい座標を書き込みます。

・クラゲの当たり判定

    float hitRadius = 1;
    bool hit = false;
    float sizeOffset = 0.5;
    float jellyFishSize = rnd(seed) + sizeOffset;
    jellyFishSize = pow(smoothstep(0.0, 1.0 + sizeOffset, jellyFishSize), 2.0) + sizeOffset;
    for (uint i = 0; i < _BulletCount; i++)
    {
        float d = distance(_jellyFishBuffer[index].jellyFishPos, _bulletBuffers[i].position);
        if (d < hitRadius * jellyFishSize)
        {
            _jellyFishBuffer[index].dead = 1;
            _jellyFishBuffer[index].jellyFishPos = float3(100, 100, 100);
            _bulletBuffers[i].isHit = 1;
            return;
        }
    }
この処理ではユーザーが発射した弾にクラゲが当たったかどうかを判定しています。弾にクラゲが当たった場合、クラゲの死亡フラグと弾のバッファのヒットフラグを立てています。

・クラゲの計算

    seed = rnd(seed);
    float jellyFishTime = (_Time / CyclicScale) * _TimeScale * 5.0 + rnd(seed);
    
    float3 jellyFishOffsetPos = cyclicNoise(float3(rnd(seed), rnd(seed), rnd(seed)), Pers, Lacu);
    float3 preJellyFishPos = cyclicNoise(jellyFishTime - 0.01 + jellyFishOffsetPos, Pers, Lacu);
    float3 jellyFishPos = cyclicNoise(jellyFishTime + jellyFishOffsetPos, Pers, Lacu);
    float3 jellyFishDir = normalize(jellyFishPos - preJellyFishPos);
    
    _jellyFishBuffer[index].jellyFishPos = jellyFishPos;
    _jellyFishBuffer[index].jellyFishDir = lerp(_jellyFishBuffer[index].jellyFishDir, jellyFishDir, _SmoothFactor);
    _jellyFishBuffer[index].jellyFishColor = float3(rnd(seed), rnd(seed), rnd(seed));
    _jellyFishBuffer[index].size = jellyFishSize;

この処理ではクラゲのワールド座標、ワールド座標、進行方向、色、蝶の大きさを計算しています。蝶の座標計算とほぼ同じアルゴリズムを使用しています。

# 自前で実装したGPUInstancing用トレイルの説明
# ButterFlyTrail.shader
・頂点シェーダー

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

蝶のトレイルを表現したかった為、GPUInstancing用のトレイルを実装しました。まず頂点シェーダーの頂点IDからその頂点が属している蝶、トレイル区間を計算してセグメント両端のワールド座標、経過度を計算しています。

・ジオメトリシェーダー

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
このジオメトリシェーダーは、頂点シェーダーから渡されたセグメント両端のワールド座標（A, B）、経過度（ageA, ageB）、蝶インデックスを受け取り、各セグメントを四角として展開してトレイルを描画します。各セグメントは幅を持つ帯として生成され、経過度に応じて幅や不透明度が変化します。
Unity環境のコンピュートシェーダー、ジオメトリシェーダー、GPUInstancing周りはShitakamiさんのHatenaBlogやQiitaを読み漁って勉強していました。

https://shitakami.hateblo.jp/about

https://qiita.com/genkitoyama/items/262c5b9c489130eb877d?utm_source=copilot.com
