# ソースコードの解説
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
