
using UnityEngine;
using System.Collections;
using System.IO;

// Create a texture and fill it with Perlin noise.
// Try varying the xOrg, yOrg and scale values in the inspector
// while in Play mode to see the effect they have on the noise.

//[RequireComponent(typeof(MeshRenderer))]
public class noise
{
    // Width and height of the texture in pixels.
    public int pixWidth;
    public int pixHeight;

    // The origin of the sampled area in the plane.
    public float xOrg = 0f;
    public float yOrg = 0f;

    // The number of cycles of the basic noise pattern that are repeated
    // over the width and height of the texture.
    public float scale;

    public int octaves;

    private Texture2D noiseTex;
    private Texture2D quarterTex;
    private float[] pix;
    private Color[] oldpix;
    private Color[] newpix;
    private Color[] quarterpix;
    public Renderer rend;

    //public GameObject simplexnoise;
    public SimplexNoiseGenerator noiseGenerator;

    public string seed;
    public long seedl;

    public noise(int width, int height, float s, int o)
    {
        pixWidth = width;
        pixHeight = height;

        scale = s;
        octaves = o;
    }

    public void generateNoiseTexture()
    {
        noiseGenerator = new SimplexNoiseGenerator();


        // Set up the texture and a Color array to hold pixels during processing.
        noiseTex = new Texture2D(pixWidth, pixHeight, TextureFormat.RGB24, false);
        quarterTex = new Texture2D(pixWidth / 2, pixHeight / 2, TextureFormat.RGB24, false);
        pix = new float[noiseTex.width * noiseTex.height];
        newpix = new Color[noiseTex.width * noiseTex.height];
        oldpix = new Color[noiseTex.width * noiseTex.height];
        quarterpix = new Color[noiseTex.width * noiseTex.height / 4];

        rend.material.mainTexture = quarterTex;

        //CalcNoise();
        calcSimplexNoise();
        seed = noiseGenerator.GetSeed();

        Debug.Log("generated noise texture!");
    }

    void calcSimplexNoise()
    {
        // For each pixel in the texture...
        float yf = 0.0F;

        float xCoordMax = xOrg + scale;
        float yCoordMax = yOrg + scale;

        while (yf < noiseTex.height)
        {
            float xf = 0.0F;
            while (xf < noiseTex.width)
            {
                float xCoord = xOrg + xf / noiseTex.width * scale;
                float yCoord = yOrg + yf / noiseTex.height * scale;

                //single octave
                //float sample = noiseGenerator.coherentNoise(xCoord, yCoord, 0f, 1, 25, 1f, 2f, 0.9f);

                //lacunarity: increase in frequency of octaves (>1)
                //persistance: decrease in amplitude of octaves [0,1]

                //octaves
                float sample = 0f;
                for (int o = 0; o < octaves; o++)
                {
                    sample += Mathf.Pow(0.5f, o) * noiseGenerator.coherentNoise(xCoord * Mathf.Pow(2f, o), yCoord * Mathf.Pow(2f, o), 0f, 1, 25, 1f, 2f, 0.9f * Mathf.Pow(2f, o));
                }

                pix[(int)yf * noiseTex.width + (int)xf] = sample + 0.5f;
                xf++;
            }
            yf++;
        }

        for (int x = 0; x < pixWidth; x++)
        {
            for (int y = 0; y < pixHeight; y++)
            {
                // makes texture seamless
                float sampleShiftXY = pix[((y + pixHeight / 2) % pixHeight) * pixWidth + ((x + pixWidth / 2) % pixWidth)]; //shifted x and y with modulo

                float sampleShiftX = pix[y * pixWidth + (x + pixWidth / 2) % pixWidth]; //shifted x with modulo

                float sampleShiftY = pix[((y + pixHeight / 2) % pixHeight) * pixWidth + x]; //shifted y with modulo

                float g1 = (1 - Mathf.Sin(Mathf.PI * (float)x / (float)pixWidth)) * sampleShiftXY + (Mathf.Sin(Mathf.PI * (float)x / (float)pixWidth)) * sampleShiftY;

                float g2 = (1 - Mathf.Sin(Mathf.PI * (float)x / (float)pixWidth)) * sampleShiftX + (Mathf.Sin(Mathf.PI * (float)x / (float)pixWidth)) * pix[y * pixWidth + x];

                float sample = Mathf.Sin(Mathf.PI * (float)y / (float)pixHeight) * g2 + (1 - Mathf.Sin(Mathf.PI * (float)y / (float)pixHeight)) * g1;

                newpix[y * pixWidth + x] = new Color(sample, sample, sample);
            }
        }

        //quarter pixels to texture
        for (int x = 0; x < pixWidth / 2; x++)
        {
            for (int y = 0; y < pixHeight / 2; y++)
            {
                quarterpix[y * pixWidth / 2 + x] = newpix[y * pixWidth + x];
            }
        }

        // Copy the pixel data to the texture and load it into the GPU.
        //noiseTex.SetPixels(newpix);
        //noiseTex.Apply();

        quarterTex.SetPixels(quarterpix);
        quarterTex.Apply();

        ////export PNG image
        //byte[] bytes = noiseTex.EncodeToPNG();
        //var dirPath = Application.dataPath + "/../Assets/SaveImages/";
        //if (!Directory.Exists(dirPath))
        //{
        //    Directory.CreateDirectory(dirPath);
        //}
        //File.WriteAllBytes(dirPath + "Image" + ".png", bytes);
        //Debug.Log("image saved");

        //export PNG image
        byte[] quarterbytes = quarterTex.EncodeToPNG();
        var QdirPath = Application.dataPath + "/../Assets/SaveImages/";
        if (!Directory.Exists(QdirPath))
        {
            Directory.CreateDirectory(QdirPath);
        }
        File.WriteAllBytes(QdirPath + "ImageQ" + ".png", quarterbytes);
        Debug.Log("image saved");
    }

    float risingSlope(float x, float overlap)
    {
        return ((x * 0.5f / overlap) + 0.5f);
    }

    float fallingSlope(float x, float overlap, float width)
    {
        return (-0.5f * (x / overlap - (1 + width / overlap)));
    }
}


public class SimplexNoiseGenerator
{
    private int[] A = new int[3];
    private float s, u, v, w;
    private int i, j, k;
    private float onethird = 0.333333333f;
    private float onesixth = 0.166666667f;
    private int[] T;

    public SimplexNoiseGenerator()
    {
        if (T == null)
        {
            System.Random rand = new System.Random();
            T = new int[8];
            for (int q = 0; q < 8; q++)
                T[q] = rand.Next();
        }
    }

    public SimplexNoiseGenerator(string seed)
    {
        T = new int[8];
        string[] seed_parts = seed.Split(new char[] { ' ' });

        for (int q = 0; q < 8; q++)
        {
            int b;
            try
            {
                b = int.Parse(seed_parts[q]);
            }
            catch
            {
                b = 0x0;
            }
            T[q] = b;
        }
    }

    public SimplexNoiseGenerator(int[] seed)
    { // {0x16, 0x38, 0x32, 0x2c, 0x0d, 0x13, 0x07, 0x2a}
        T = seed;
    }

    public string GetSeed()
    {
        string seed = "";

        for (int q = 0; q < 8; q++)
        {
            seed += T[q].ToString();
            if (q < 7)
                seed += " ";
        }

        return seed;
    }

    public float coherentNoise(float x, float y, float z, int octaves = 5, int multiplier = 25, float amplitude = 0.5f, float lacunarity = 2, float persistence = 0.9f)
    {
        Vector3 v3 = new Vector3(x, y, z) / multiplier;
        float val = 0;
        for (int n = 0; n < octaves; n++)
        {
            val += noise(v3.x, v3.y, v3.z) * amplitude;
            v3 *= lacunarity;
            amplitude *= persistence;
        }
        return val;
    }

    // Simplex Noise Generator
    public float noise(float x, float y, float z)
    {
        s = (x + y + z) * onethird;
        i = fastfloor(x + s);
        j = fastfloor(y + s);
        k = fastfloor(z + s);

        s = (i + j + k) * onesixth;
        u = x - i + s;
        v = y - j + s;
        w = z - k + s;

        A[0] = 0; A[1] = 0; A[2] = 0;

        int hi = u >= w ? u >= v ? 0 : 1 : v >= w ? 1 : 2;
        int lo = u < w ? u < v ? 0 : 1 : v < w ? 1 : 2;

        return kay(hi) + kay(3 - hi - lo) + kay(lo) + kay(0);
    }

    float kay(int a)
    {
        s = (A[0] + A[1] + A[2]) * onesixth;
        float x = u - A[0] + s;
        float y = v - A[1] + s;
        float z = w - A[2] + s;
        float t = 0.6f - x * x - y * y - z * z;
        int h = shuffle(i + A[0], j + A[1], k + A[2]);
        A[a]++;
        if (t < 0) return 0;
        int b5 = h >> 5 & 1;
        int b4 = h >> 4 & 1;
        int b3 = h >> 3 & 1;
        int b2 = h >> 2 & 1;
        int b1 = h & 3;

        float p = b1 == 1 ? x : b1 == 2 ? y : z;
        float q = b1 == 1 ? y : b1 == 2 ? z : x;
        float r = b1 == 1 ? z : b1 == 2 ? x : y;

        p = b5 == b3 ? -p : p;
        q = b5 == b4 ? -q : q;
        r = b5 != (b4 ^ b3) ? -r : r;
        t *= t;
        return 8 * t * t * (p + (b1 == 0 ? q + r : b2 == 0 ? q : r));
    }

    int shuffle(int i, int j, int k)
    {
        return b(i, j, k, 0) + b(j, k, i, 1) + b(k, i, j, 2) + b(i, j, k, 3) + b(j, k, i, 4) + b(k, i, j, 5) + b(i, j, k, 6) + b(j, k, i, 7);
    }

    int b(int i, int j, int k, int B)
    {
        return T[b(i, B) << 2 | b(j, B) << 1 | b(k, B)];
    }

    int b(int N, int B)
    {
        return N >> B & 1;
    }

    int fastfloor(float n)
    {
        return n > 0 ? (int)n : (int)n - 1;
    }
}