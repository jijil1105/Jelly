#define CyclicScale 50.0

struct JellyFishData
{
    float3 jellyFishPos;
    float3 jellyFishDir;
    float3 jellyFishColor;
    float size;
    uint dead;
};

struct ButterflyData
{
    float3 butterflyPos;
    float3 butterflyDir;
    float3 butterflyColor;
    float size;
    uint trailWriteIndex;
    float trailAccum;
};

struct TrailPoint
{
    float3 pos;
    float time;
};

struct BulletData
{
    float3 position;
    uint isHit;
};

float3x3 CreateRotationMatrix(float3 dir)
{
    float3 z = normalize(dir);
    float3 up = abs(z.y) < 0.999 ? float3(0, 1, 0) : float3(0, 0, 1);
    float3 x = normalize(cross(up, z));
    float3 y = cross(z, x);
    return float3x3(x, y, z);
}

float3x3 CreateRotationMatrixY(float3 dir)
{
    float3 y = normalize(dir);
    float3 forward = abs(y.z) < 0.999 ? float3(0, 0, 1) : float3(1, 0, 0);
    float3 x = normalize(cross(forward, y));
    float3 z = cross(x, y);
    return float3x3(x, y, z);
}

float3 cyclicNoise(float3 p, float pers, float lacu)
{
    float3 pos = p;
    float4 sum = float4(0.0, 0.0, 0.0, 0.0);
    float3x3 rot = CreateRotationMatrix(float3(2, 1, -1));

    for (int i = 0; i < 5; i++)
    {
        p = mul(rot, p);
        p += sin(p.zxy);
        sum += float4(cross(cos(p), sin(p.yzx)), 1.0);
        sum /= pers;
        p *= lacu;
    }
    return ((sum.xyz / sum.w) * CyclicScale) - CyclicScale / 2.0;
}

float hash(float t)
{
    uint p = asuint(t);
    p = ((p >> 8u) * 1238645289u);
    p = ((p >> 8u) * 1238645289u);
    p = ((p >> 8u) * 1238645289u);
    return float(p) / float(0xFFFFFFFFu);
}

float rnd(inout float seed)
{
    seed += 1.0;
    return hash(seed);
}