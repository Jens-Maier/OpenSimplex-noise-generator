
int fastFloor(float x)
{
    int xi = (int)x;
    return x < xi ? xi - 1 : xi;
}

float3 round(float3 x)
{
    return float3(fastFloor(x.x), fastFloor(x.y), fastFloor(x.z));
}


static const float3 gradientTable3[12] = 
{
    float3(1,1,0), 
    float3(-1,1,0), 
    float3(1,-1,0), 

    float3(-1,-1,0), 
    float3(1,0,1), 
    float3(-1,0,1), 

    float3(1,0,-1), 
    float3(-1,0,-1),
    float3(0,1,1), 

    float3(0,-1,1), 
    float3(0,1,-1), 
    float3(0,-1,-1)
};


void bccNoise_float(float3 position, UnityTexture2D permutationLUT, out float4 noise)
{
    // If you need to sample it using the built-in macros:
    // float4 val = SAMPLE_TEXTURE2D(permutationLUT.tex, permutationLUT.samplerstate, uv);
    
    // If you want to use .Load() as we discussed for the LUT:
    // float rawValue = permutationLUT.tex.Load(int3(position.x, 0, 0)).r;


    float4 val = SAMPLE_TEXTURE2D(permutationLUT.tex, permutationLUT.samplerstate, float2(position.x, position.y));

    //noise = val;

    int index = position.x * 256; //...
    float4 permutationTableEntry = permutationLUT.tex.Load(int3(index, 0, 0));
    //noise = permutationTableEntry;


    float scale = 3;

    float3 pos = position / scale;

    int cellIndexX = fastFloor(pos.x);
    int cellIndexY = fastFloor(pos.y);
    int cellIndexZ = fastFloor(pos.z);

    float3 delta = position - float3(cellIndexX + 0.5, cellIndexY + 0.5, cellIndexZ + 0.5);

    float3 delta1;
    float3 delta2;
    float3 delta3;
    int i1 = 0;
    int i2 = 0;
    int i3 = 0;
    int j1 = 0;
    int j2 = 0;
    int j3 = 0;
    int k1 = 0;
    int k2 = 0;
    int k3 = 0;

    if (abs(delta.x) > max(abs(delta.y), abs(delta.z)))
    {
        if (delta.x > 0)
        {
            // +x pyrmaid
            delta1 = pos - float3(cellIndexX + 1.0, cellIndexY, cellIndexZ);
            delta3 = pos - float3(cellIndexX + 1.0, cellIndexY + 1.0, cellIndexZ + 1.0);
            i1 = 1;
            j1 = 0;
            k1 = 0;
            i3 = 1;
            j3 = 1;
            k3 = 1;

            if (delta.y > delta.z)
            {
                delta2 = pos - float3(cellIndexX + 1.0, cellIndexY + 1.0, cellIndexZ);
                i2 = 1;
                j2 = 1;
                k2 = 0;
            }
            else
            {
                delta2 = pos - float3(cellIndexX + 1.0, cellIndexY, cellIndexZ + 1.0);
                i2 = 1;
                j2 = 0;
                k2 = 1;
            }
        }
        else
        {
            // -x pyramid
            delta1 = pos - float3(cellIndexX, cellIndexY, cellIndexZ);
            delta3 = pos - float3(cellIndexX, cellIndexY + 1.0, cellIndexZ + 1.0);
            i1 = 0;
            j1 = 0;
            k1 = 0;
            i3 = 0;
            j3 = 1;
            k3 = 1;

            if (delta.y > delta.z)
            {
                delta2 = pos - float3(cellIndexX, cellIndexY + 1.0, cellIndexZ);
                i2 = 0;
                j2 = 1;
                k2 = 0;
            }
            else
            {
                delta2 = pos - float3(cellIndexX, cellIndexY, cellIndexZ + 1.0);
                i2 = 0;
                j2 = 0;
                k2 = 1;
            }
        }
    }
    else
    {
        if (abs(delta.y) > max(abs(delta.z), abs(delta.x)))
        {
            if (delta.y > 0)
            {
                // +y pyramid
                delta1 = pos - float3(cellIndexX, cellIndexY + 1.0, cellIndexZ);
                delta3 = pos - float3(cellIndexX + 1.0, cellIndexY + 1.0, cellIndexZ + 1.0);
                i1 = 0;
                j1 = 1;
                k1 = 0;
                i3 = 1;
                j3 = 1;
                k3 = 1;

                if (delta.x > delta.z)
                {
                    delta2 = pos - float3(cellIndexX + 1, cellIndexY + 1.0, cellIndexZ);
                    i2 = 1;
                    j2 = 1;
                    k2 = 0;
                }
                else
                {
                    delta2 = pos - float3(cellIndexX, cellIndexY + 1.0, cellIndexZ + 1.0);
                    i2 = 0;
                    j2 = 1;
                    k2 = 1;       
                }
            }
            else
            {
                // -y pyramid
                delta1 = pos - float3(cellIndexX, cellIndexY, cellIndexZ);
                delta3 = pos - float3(cellIndexX + 1.0, cellIndexY, cellIndexZ + 1.0);
                i1 = 0;
                j1 = 0;
                k1 = 0;
                i3 = 1;
                j3 = 0;
                k3 = 1;

                if (delta.x > delta.z)
                {
                    delta2 = pos - float3(cellIndexX + 1.0, cellIndexY, cellIndexZ);
                    i2 = 1;
                    j2 = 0;
                    k2 = 0;
                }
                else
                {
                    delta2 = pos - float3(cellIndexX, cellIndexY, cellIndexZ + 1.0);
                    i2 = 0;
                    j2 = 0;
                    k2 = 1;
                }
            }
        }
        else
        {
            if (delta.z > 0)
            {
                // +z pyramid
                delta1 = pos - float3(cellIndexX, cellIndexY, cellIndexZ + 1.0);
                delta3 = pos - float3(cellIndexX + 1.0, cellIndexY + 1.0, cellIndexZ + 1.0);
                i1 = 0;
                j1 = 0;
                k1 = 1;
                i3 = 1;
                j3 = 1;
                k3 = 1;

                if (delta.x > delta.y)
                {
                    delta2 = pos - float3(cellIndexX + 1.0, cellIndexY, cellIndexZ + 1.0);
                    i2 = 1;
                    j2 = 0;
                    k2 = 1;
                }
                else
                {
                    delta2 = pos - float3(cellIndexX, cellIndexY + 1.0, cellIndexZ + 1.0);
                    i2 = 0;
                    j2 = 1;
                    k2 = 1;
                }
            }
            else
            {
                // -z pyramid
                delta1 = pos - float3(cellIndexX, cellIndexY, cellIndexZ);
                delta3 = pos - float3(cellIndexX + 1.0, cellIndexY + 1.0, cellIndexZ);
                i1 = 0;
                j1 = 0;
                k1 = 0;
                i3 = 1;
                j3 = 1;
                k3 = 0;

                if (delta.x > delta.y)
                {
                    delta2 = pos - float3(cellIndexX + 1.0, cellIndexY, cellIndexZ);
                    i2 = 1;
                    j2 = 0;
                    k2 = 0;
                }
                else
                {
                    delta2 = pos - float3(cellIndexX, cellIndexY + 1.0, cellIndexZ);
                    i2 = 0;
                    j2 = 1;
                    k2 = 0;
                }
            }
        }
    }

    // Hash the corner coordinates to get the gradient indices.
    int ii = cellIndexX & 255;
    int jj = cellIndexY & 255;
    int kk = cellIndexZ & 255;

    // int index = position.x * 256; //...
    // float4 permutationTableEntry = permutationLUT.tex.Load(int3(index, 0, 0));
    // noise = permutationTableEntry;

    uint gradientIndex0 = (uint)(permutationLUT.tex.Load(int3(((ii + 127) & 255) + 
                          (uint)(permutationLUT.tex.Load(int3(((jj + 127) & 255) + 
                          (uint)(permutationLUT.tex.Load(int3(((kk + 127) & 255), 
                          0, 0)).r * 255.0), 0, 0)).r * 255.0), 0, 0)).r * 255.0) % 12;

    uint gradientIndex1 = (uint)(permutationLUT.tex.Load(int3(ii + i1 + 
                          (uint)(permutationLUT.tex.Load(int3(jj + j1 + 
                          (uint)(permutationLUT.tex.Load(int3(kk + k1, 
                          0, 0)).r * 255.0), 0, 0)).r * 255.0), 0, 0)).r * 255.0) % 12;

    uint gradientIndex2 = (uint)(permutationLUT.tex.Load(int3(ii + i2 + 
                          (uint)(permutationLUT.tex.Load(int3(jj + j2 + 
                          (uint)(permutationLUT.tex.Load(int3(kk + k2, 
                          0, 0)).r * 255.0), 0, 0)).r * 255.0), 0, 0)).r * 255.0) % 12;

    uint gradientIndex3 = (uint)(permutationLUT.tex.Load(int3(ii + i3 + 
                          (uint)(permutationLUT.tex.Load(int3(jj + j3 + 
                          (uint)(permutationLUT.tex.Load(int3(kk + k3, 
                          0, 0)).r * 255.0), 0, 0)).r * 255.0), 0, 0)).r * 255.0) % 12;

    float t0 = 0.6 - dot(delta, delta);
    float t1 = 0.6 - dot(delta1, delta1);
    float t2 = 0.6 - dot(delta2, delta2);
    float t3 = 0.6 - dot(delta3, delta3);

    float n0 = 0.0;
    float n1 = 0.0;
    float n2 = 0.0;
    float n3 = 0.0;


    if (t0 > 0)
    {
        t0 = t0 * t0;
        n0 = t0 * t0 * dot(gradientTable3[gradientIndex0], delta);
    }
    if (t1 > 0)
    {
        t1 = t1 * t1;
        n1 = t1 * t1 * dot(gradientTable3[gradientIndex1], delta1);
    }
    if (t2 > 0)
    {
        t2 = t2 * t2;
        n2 = t2 * t2 * dot(gradientTable3[gradientIndex2], delta2);
    }
    if (t3 > 0)
    {
        t3 = t3 * t3;
        n3 = t3 * t3 * dot(gradientTable3[gradientIndex3], delta3);
    }

    float n = 32.0 * (n0 + n1 + n2 + n3);

    noise = float4(n, n, n, 1);

    
}
