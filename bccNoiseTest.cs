
using System.Diagnostics.Contracts;
using System.Drawing;
using System.IO.Compression;
using System.Numerics;
using System.Reflection.Emit;
using System.Security.Cryptography;
using UnityEngine;


public class bccNoiseTest : MonoBehaviour
{
    public UnityEngine.Vector3[,,] latticePointsA;
    public UnityEngine.Vector3[,,] latticePointsB;
    
    [Range(1, 5)]
    public int sizeX;
    [Range(1, 5)]
    public int sizeY;
    [Range(1, 5)]
    public int sizeZ;

    public UnityEngine.Vector3 testPos;
    public float testNoise;
    public float speed;

    public int cellIndexX;
    public int cellIndexY;
    public int cellIndexZ;

    public UnityEngine.Vector3 pyramidOffset;

    public UnityEngine.Vector3 delta;
    public UnityEngine.Vector3 delta1;
    public UnityEngine.Vector3 delta2;
    public UnityEngine.Vector3 delta3;

    public UnityEngine.Vector3 latticePoint1;
    public UnityEngine.Vector3 latticePoint2;
    public UnityEngine.Vector3 latticePoint3;

    public uint[] p;
    public uint[] permutationTable;
    public UnityEngine.Vector3[] gradientTable3;

    public float maxTestNoise = -1000f;
    public float minTestNoise = 1000f;
    public float maxDeltaTestNoise = 0f;

    public float[,,] noiseSamples;
    public int noiseSampleResolution;

    [Range(0f, 1f)]
    public float noiseSampleGizmoRadius;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        latticePointsA = new UnityEngine.Vector3[sizeX + 1, sizeY + 1, sizeZ + 1];
        latticePointsB = new UnityEngine.Vector3[sizeX, sizeY, sizeZ];

        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    latticePointsA[x, y, z] = new UnityEngine.Vector3((float)x, (float)y, (float)z);
                }
            }
        }
        for (int x = 0; x < sizeX - 1; x++)
        {
            for (int y = 0; y < sizeY - 1; y++)
            {
                for (int z = 0; z < sizeZ - 1; z++)
                {
                    latticePointsB[x, y, z] = new UnityEngine.Vector3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f);
                }
            }
        }

        //https://cgvr.cs.uni-bremen.de/teaching/cg_literatur/simplexnoise.pdf
        p = new uint[] {151,160,137,91,90,15,
                131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
                190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
                88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
                77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
                102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
                135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
                5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
                223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
                129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
                251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
                49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
                138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180};

        permutationTable = new uint[512];
        for(int i=0; i<512; i++) 
        {
            permutationTable[i]=p[i & 255]; 
        }

        gradientTable3 = new UnityEngine.Vector3[] {
            new UnityEngine.Vector3(1,1,0), 
            new UnityEngine.Vector3(-1,1,0), 
            new UnityEngine.Vector3(1,-1,0), 

            new UnityEngine.Vector3(-1,-1,0), 
            new UnityEngine.Vector3(1,0,1), 
            new UnityEngine.Vector3(-1,0,1), 
            
            new UnityEngine.Vector3(1,0,-1), 
            new UnityEngine.Vector3(-1,0,-1),
            new UnityEngine.Vector3(0,1,1), 
            
            new UnityEngine.Vector3(0,-1,1), 
            new UnityEngine.Vector3(0,1,-1), 
            new UnityEngine.Vector3(0,-1,-1)};

        noiseSamples = new float[sizeX * noiseSampleResolution, sizeY * noiseSampleResolution, sizeZ * noiseSampleResolution];
        
        for (int x = 0; x < sizeX * noiseSampleResolution; x++)
        {
            for (int y = 0; y < sizeY * noiseSampleResolution; y++)
            {
                for (int z = 0; z < sizeZ * noiseSampleResolution; z++)
                {
                    float noise = sampleNoise(new UnityEngine.Vector3((float)x / noiseSampleResolution + 0.24f, (float)y / noiseSampleResolution + 0.84f, (float)z / noiseSampleResolution + 0.52f));

                    noiseSamples[x, y, z] = noise;
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        

        if (Input.GetKey(KeyCode.W))
        {
            testPos.z += speed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.S))
        {
            testPos.z -= speed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.A))
        {
            testPos.x -= speed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.D))
        {
            testPos.x += speed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            testPos.y -= speed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.E))
        {
            testPos.y += speed * Time.deltaTime;
        }

        testNoise = sampleNoise(testPos);

    }

    float sampleNoise(UnityEngine.Vector3 position)
    {
        cellIndexX = (int)position.x;
        cellIndexY = (int)position.y;
        cellIndexZ = (int)position.z;

        if (!(cellIndexX >= 0 && cellIndexX < sizeX && cellIndexY >= 0 && cellIndexY < sizeY && cellIndexZ >= 0 && cellIndexZ < sizeZ))
        {
            cellIndexX = 0;
            cellIndexY = 0;
            cellIndexZ = 0;    
        }

        delta = position - latticePointsA[cellIndexX, cellIndexY, cellIndexZ] - new UnityEngine.Vector3(0.5f, 0.5f, 0.5f);
        delta1 = new UnityEngine.Vector3(0f, 0f, 0f);
        delta2 = new UnityEngine.Vector3(0f, 0f, 0f);
        delta3 = new UnityEngine.Vector3(0f, 0f, 0f);
        int i1 = 0;
        int i2 = 0;
        int i3 = 0;
        int j1 = 0;
        int j2 = 0;
        int j3 = 0;
        int k1 = 0;
        int k2 = 0;
        int k3 = 0;

        if (Mathf.Abs(delta.x) > Mathf.Max(Mathf.Abs(delta.y), Mathf.Abs(delta.z)))
        {
            if (delta.x > 0)
            {
                // +x pyramid
                pyramidOffset = new UnityEngine.Vector3(0.25f, 0.0f, 0.0f);
                delta1 = position - latticePointsA[cellIndexX + 1, cellIndexY, cellIndexZ];
                delta3 = position - latticePointsA[cellIndexX + 1, cellIndexY + 1, cellIndexZ + 1];
                latticePoint1 = latticePointsA[cellIndexX + 1, cellIndexY, cellIndexZ];
                latticePoint3 = latticePointsA[cellIndexX + 1, cellIndexY + 1, cellIndexZ + 1];
                i1 = 1;
                j1 = 0;
                k1 = 0;
                i3 = 1;
                j3 = 1;
                k3 = 1;

                if (delta.y > delta.z)
                {
                    delta2 = position - latticePointsA[cellIndexX + 1, cellIndexY + 1, cellIndexZ];
                    latticePoint2 = latticePointsA[cellIndexX + 1, cellIndexY + 1, cellIndexZ];
                    i2 = 1;
                    j2 = 1;
                    k2 = 0;
                }
                else
                {
                    delta2 = position - latticePointsA[cellIndexX + 1, cellIndexY, cellIndexZ + 1];
                    latticePoint2 = latticePointsA[cellIndexX + 1, cellIndexY, cellIndexZ + 1];
                    i2 = 1;
                    j2 = 0;
                    k2 = 1;
                }
            }
            else
            {
                // -x pyramid
                pyramidOffset = new UnityEngine.Vector3(-0.25f, 0.0f, 0.0f);
                delta1 = position - latticePointsA[cellIndexX, cellIndexY, cellIndexZ];
                delta3 = position - latticePointsA[cellIndexX, cellIndexY + 1, cellIndexZ + 1];
                latticePoint1 = latticePointsA[cellIndexX, cellIndexY, cellIndexZ];
                latticePoint3 = latticePointsA[cellIndexX, cellIndexY + 1, cellIndexZ + 1];
                i1 = 0;
                j1 = 0;
                k1 = 0;
                i3 = 0;
                j3 = 1;
                k3 = 1;

                if (delta.y > delta.z)
                {
                    delta2 = position - latticePointsA[cellIndexX, cellIndexY + 1, cellIndexZ];
                    latticePoint2 = latticePointsA[cellIndexX, cellIndexY + 1, cellIndexZ];
                    i2 = 0;
                    j2 = 1;
                    k2 = 0;
                }
                else
                {
                    delta2 = position - latticePointsA[cellIndexX, cellIndexY, cellIndexZ + 1];
                    latticePoint2 = latticePointsA[cellIndexX, cellIndexY, cellIndexZ + 1];
                    i2 = 0;
                    j2 = 0;
                    k2 = 1;
                }
            }
        }
        else
        {
            if (Mathf.Abs(delta.y) > Mathf.Max(Mathf.Abs(delta.z), Mathf.Abs(delta.x)))
            {
                if (delta.y > 0)
                {
                    // +y pyramid
                    pyramidOffset = new UnityEngine.Vector3(0.0f, 0.25f, 0.0f);
                    delta1 = position - latticePointsA[cellIndexX, cellIndexY + 1, cellIndexZ];
                    delta3 = position - latticePointsA[cellIndexX + 1, cellIndexY + 1, cellIndexZ + 1];
                    latticePoint1 = latticePointsA[cellIndexX, cellIndexY + 1, cellIndexZ];
                    latticePoint3 = latticePointsA[cellIndexX + 1, cellIndexY + 1, cellIndexZ + 1];
                    i1 = 0;
                    j1 = 1;
                    k1 = 0;
                    i3 = 1;
                    j3 = 1;
                    k3 = 1;

                    if (delta.x > delta.z)
                    {
                        delta2 = position - latticePointsA[cellIndexX + 1, cellIndexY + 1, cellIndexZ];
                        latticePoint2 = latticePointsA[cellIndexX + 1, cellIndexY + 1, cellIndexZ];
                        i2 = 1;
                        j2 = 1;
                        k2 = 0;
                    }
                    else
                    {
                        delta2 = position - latticePointsA[cellIndexX, cellIndexY + 1, cellIndexZ + 1];
                        latticePoint2 = latticePointsA[cellIndexX, cellIndexY + 1, cellIndexZ + 1];
                        i2 = 0;
                        j2 = 1;
                        k2 = 1;
                    }
                }
                else
                {
                    // -y pyramid
                    pyramidOffset = new UnityEngine.Vector3(0.0f, -0.25f, 0.0f);
                    delta1 = position - latticePointsA[cellIndexX, cellIndexY, cellIndexZ];
                    delta3 = position - latticePointsA[cellIndexX + 1, cellIndexY, cellIndexZ + 1];
                    latticePoint1 = latticePointsA[cellIndexX, cellIndexY, cellIndexZ];
                    latticePoint3 = latticePointsA[cellIndexX + 1, cellIndexY, cellIndexZ + 1];
                    i1 = 0;
                    j1 = 0;
                    k1 = 0;
                    i3 = 1;
                    j3 = 0;
                    k3 = 1;

                    if (delta.x > delta.z)
                    {
                        delta2 = position - latticePointsA[cellIndexX + 1, cellIndexY, cellIndexZ];
                        latticePoint2 = latticePointsA[cellIndexX + 1, cellIndexY, cellIndexZ];
                        i2 = 1;
                        j2 = 0;
                        k2 = 0;
                    }
                    else
                    {
                        delta2 = position - latticePointsA[cellIndexX, cellIndexY, cellIndexZ + 1];
                        latticePoint2 = latticePointsA[cellIndexX, cellIndexY, cellIndexZ + 1];
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
                    pyramidOffset = new UnityEngine.Vector3(0.0f, 0.0f, 0.25f);
                    delta1 = position - latticePointsA[cellIndexX, cellIndexY, cellIndexZ + 1];
                    delta3 = position - latticePointsA[cellIndexX + 1, cellIndexY + 1, cellIndexZ + 1];
                    latticePoint1 = latticePointsA[cellIndexX, cellIndexY, cellIndexZ + 1];
                    latticePoint3 = latticePointsA[cellIndexX + 1, cellIndexY + 1, cellIndexZ + 1];
                    i1 = 0;
                    j1 = 0;
                    k1 = 1;
                    i3 = 1;
                    j3 = 1;
                    k3 = 1;

                    if (delta.x > delta.y)
                    {
                        delta2 = position - latticePointsA[cellIndexX + 1, cellIndexY, cellIndexZ + 1];
                        latticePoint2 = latticePointsA[cellIndexX + 1, cellIndexY, cellIndexZ + 1];
                        i2 = 1;
                        j2 = 0;
                        k2 = 1;
                    }
                    else
                    {
                        delta2 = position - latticePointsA[cellIndexX, cellIndexY + 1, cellIndexZ + 1];
                        latticePoint2 = latticePointsA[cellIndexX, cellIndexY + 1, cellIndexZ + 1];
                        i2 = 0;
                        j2 = 1;
                        k2 = 1;
                    }
                }
                else
                {
                    // -z pyramid
                    pyramidOffset = new UnityEngine.Vector3(0.0f, 0.0f, -0.25f);
                    delta1 = position - latticePointsA[cellIndexX, cellIndexY, cellIndexZ];
                    delta3 = position - latticePointsA[cellIndexX + 1, cellIndexY + 1, cellIndexZ];
                    latticePoint1 = latticePointsA[cellIndexX, cellIndexY, cellIndexZ];
                    latticePoint3 = latticePointsA[cellIndexX + 1, cellIndexY + 1, cellIndexZ];
                    i1 = 0;
                    j1 = 0;
                    k1 = 0;
                    i3 = 1;
                    j3 = 1;
                    k3 = 0;

                    if (delta.x > delta.y)
                    {
                        delta2 = position - latticePointsA[cellIndexX + 1, cellIndexY, cellIndexZ];
                        latticePoint2 = latticePointsA[cellIndexX + 1, cellIndexY, cellIndexZ];
                        i2 = 1;
                        j2 = 0;
                        k2 = 0;
                    }
                    else
                    {
                        delta2 = position - latticePointsA[cellIndexX, cellIndexY + 1, cellIndexZ];
                        latticePoint2 = latticePointsA[cellIndexX, cellIndexY + 1, cellIndexZ];
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

        uint gradientIndex0 = permutationTable[((ii + 127) & 255) + permutationTable[((jj + 127) & 255) + permutationTable[(kk + 127) & 255]]] % 12;
        
        uint gradientIndex1 = permutationTable[ii + i1 + permutationTable[jj + j1 + permutationTable[kk + k1]]] % 12;
        
        uint gradientIndex2 = permutationTable[ii + i2 + permutationTable[jj + j2 + permutationTable[kk + k2]]] % 12;
        
        uint gradientIndex3 = permutationTable[ii + i3 + permutationTable[jj + j3 + permutationTable[kk + k3]]] % 12;

        float t0 = 0.6f - UnityEngine.Vector3.Dot(delta, delta);
        float t1 = 0.6f - UnityEngine.Vector3.Dot(delta1, delta1);
        float t2 = 0.6f - UnityEngine.Vector3.Dot(delta2, delta2);
        float t3 = 0.6f - UnityEngine.Vector3.Dot(delta3, delta3);

        float n0 = 0f;
        float n1 = 0f;
        float n2 = 0f;
        float n3 = 0f;

        if (t0 > 0f)
        {
            t0 *= t0;
            UnityEngine.Vector3 a = gradientTable3[gradientIndex0];
            n0 = t0 * t0 * UnityEngine.Vector3.Dot(a, delta);
        }
        if (t1 > 0f)
        {
            t1 *= t1;
            UnityEngine.Vector3 b = gradientTable3[gradientIndex1];
            n1 = t1 * t1 * UnityEngine.Vector3.Dot(b, delta1);
        }
        if (t2 > 0f)
        {
            t2 *= t2;
            UnityEngine.Vector3 c = gradientTable3[gradientIndex2];
            n2 = t2 * t2 * UnityEngine.Vector3.Dot(c, delta2);
        }
        if (t3 > 0f)
        {
            t3 *= t3;
            UnityEngine.Vector3 d = gradientTable3[gradientIndex3];
            n3 = t3 * t3 * UnityEngine.Vector3.Dot(d, delta3);
        }

        return 32f * (n0 + n1 + n2 + n3);
    }

    void OnDrawGizmos()
    {
        if (latticePointsA != null && latticePointsB != null)
        {
            Gizmos.color = new UnityEngine.Color(1f, 0f, 0f);
            foreach (UnityEngine.Vector3 v in latticePointsA)
            {
                Gizmos.DrawSphere(v, 0.08f);
            }

            Gizmos.color = new UnityEngine.Color(0f, 1f, 0f);
            foreach (UnityEngine.Vector3 v in latticePointsB)
            {
                Gizmos.DrawSphere(v, 0.05f);
            }
        }

        
        if (latticePointsA != null && latticePointsB != null)
        {

            Gizmos.DrawLine(latticePointsA[cellIndexX, cellIndexY, cellIndexZ], testPos);
            Gizmos.color = new UnityEngine.Color(1f, 0f, 0f);
            if (cellIndexX < sizeX - 1 && cellIndexY < sizeY - 1 && cellIndexZ < sizeZ - 1)
            {
                Gizmos.DrawLine(latticePointsB[cellIndexX, cellIndexY, cellIndexZ], testPos);

                Gizmos.color = new UnityEngine.Color(0f, 0.8f, 0f);
                Gizmos.DrawSphere(latticePointsB[cellIndexX, cellIndexY, cellIndexZ] + pyramidOffset, 0.1f);

                Gizmos.DrawLine(latticePointsB[cellIndexX, cellIndexY, cellIndexZ], latticePointsB[cellIndexX, cellIndexY, cellIndexZ] + pyramidOffset);

                Gizmos.DrawLine(latticePoint1, testPos);
                Gizmos.DrawLine(latticePoint2, testPos);
                Gizmos.DrawLine(latticePoint3, testPos);

            }

            Gizmos.color = new UnityEngine.Color(testNoise + 0.5f, testNoise + 0.5f, testNoise + 0.5f);
            Gizmos.DrawSphere(testPos, 0.1f);

            for (int x = 0; x < sizeX * noiseSampleResolution; x++)
            {
                for (int y = 0; y < sizeY * noiseSampleResolution; y++)
                {
                    for (int z = 0; z < sizeZ * noiseSampleResolution; z++)
                    {
                        UnityEngine.Vector3 pos = new UnityEngine.Vector3(x / (float)noiseSampleResolution, y / (float)noiseSampleResolution, z / (float)noiseSampleResolution);

                        Gizmos.color = new UnityEngine.Color(noiseSamples[x, y, z] + 0.5f, noiseSamples[x, y, z] + 0.5f, noiseSamples[x, y, z] + 0.5f);
                        Gizmos.DrawSphere(pos, 0.07f * noiseSampleGizmoRadius);
                    }
                }
            }
        }

    }
}
