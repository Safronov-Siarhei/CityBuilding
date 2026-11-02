// The sky's clouds, one field for both what is seen and what it casts (Maps/CloudLayer, the user
// 2026-11-02: a shadow with no cloud is unconvincing -- at the farthest zoom the clouds themselves
// show). The field is the fog's seamless noise (CloudNoise.png: red broad billows, green finer
// puffs) laid flat at _CloudPlace.z metres up, drifting with the wind. Everything is set globally by
// CloudLayer; with no layer (a dungeon, the menu) _CloudParams.w is 0 and nothing is shaded.
#ifndef CITYBUILDER_CLOUD_FIELD
#define CITYBUILDER_CLOUD_FIELD

TEXTURE2D(_CloudNoise); SAMPLER(sampler_CloudNoise);
// x: 1 / the noise tile's size in metres, y: cover (higher = fewer clouds), z: edge softness, w: shadow strength
float4 _CloudParams;
// xy: the drift so far (metres), z: the layer's height
float4 _CloudPlace;

// 0..1: how much cloud there is over this point of the layer
float CloudDensity(float2 xz)
{
    float2 uv = (xz - _CloudPlace.xy) * _CloudParams.x;
    float broad = SAMPLE_TEXTURE2D_LOD(_CloudNoise, sampler_CloudNoise, uv, 0).r;
    float fine = SAMPLE_TEXTURE2D_LOD(_CloudNoise, sampler_CloudNoise, uv * 2.7 + 0.37, 0).g;
    float n = broad * 0.75 + fine * 0.25;
    return smoothstep(_CloudParams.y, _CloudParams.y + _CloudParams.z, n);
}

// 0..1: how much a point on the ground is darkened -- the cloud straight up the sun's ray from it
float CloudShadow(float3 positionWS, float3 toSun)
{
    if (_CloudParams.w <= 0.0 || toSun.y <= 0.05) return 0.0;
    float up = max(_CloudPlace.z - positionWS.y, 0.0);
    float2 at = positionWS.xz + toSun.xz * (up / toSun.y);
    return CloudDensity(at) * _CloudParams.w;
}

#endif
