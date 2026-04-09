
// ported from https://www.shadertoy.com/view/XsX3zB

const float F3 = 0.3333333;
const float G3 = 0.1666667;

int fastFloor(float x)
{
    int xi = (int)x;
    return x < xi ? xi - 1 : xi;
}


//float3 random3(float3 c)
//{
//    float j = 4096.0 * sin(dot(c, float3(17.0, 59.4, 15.0)));
//    float3 r;
//    r.z = frac(512.0 * j);
//    j *= 0.125;
//    r.x = frac(512.0 * j);
//    j *= 0.125;
//    r.y = frac(512.0 * j);
//    return r - 0.5;
//}


// for half precision
float3 random3(float3 c)
{
    float3 j = float3(
        dot(c, float3(127.1, 311.7, 74.7)),
        dot(c, float3(269.5, 183.3, 246.1)),
        dot(c, float3(113.5, 271.9, 124.6))
    );
    
    // 43758.5453 is the standard hash multiplier. 
    // It fits (just barely) inside the 65,504 limit of a half-float.
    return frac(sin(j) * 43758.5453) - 0.5;
}





void simplexNoise_float(float3 position, out float4 noise)
{
    float3 s = floor(position + dot(position, float3(F3, F3, F3)));
    float3 x = position - s + dot(s, float3(G3, G3, G3));

    float3 e = step(float3(0.0, 0.0, 0.0), x - x.yzx);
    float3 i1 = e * (1.0 - e.zxy);
    float3 i2 = 1.0 - e.zxy * (1.0 - e);

    float3 x1 = x - i1 + G3;
    float3 x2 = x - i2 + 2.0 * G3;
    float3 x3 = x - 1.0 + 3.0 * G3;

    float4 w;
    float4 d;

    w.x = dot(x, x);
    w.y = dot(x1, x1);
    w.z = dot(x2, x2);
    w.w = dot(x3, x3);

    w = max(0.6 - w, 0.0);

    d.x = dot(random3(s), x);
    d.y = dot(random3(s + i1), x1);
    d.z = dot(random3(s + i2), x2);
    d.w = dot(random3(s + 1.0), x3);

    w *= w;
    w *= w;
    d *= w;

    float n = dot(d, float4(52.0, 52.0, 52.0, 52.0));

    noise = float4(n * 0.5 + 0.5, n * 0.5 + 0.5, n * 0.5 + 0.5, 1);

}