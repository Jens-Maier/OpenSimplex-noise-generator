using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace gridNamespace //                                    better 2D noise: https://gist.github.com/KdotJPG/b1270127455a94ac5d19
{
    // TODO: match edge pixels exactly (TODO: check done?)


// -> open game view in separate window
// -> press 'c' to center the cursor
// -> move mouse to see relation of skewed (orange) and unskewed (red) points
// -> noise texture is saved in /Assets/SaveImages

[RequireComponent(typeof(MeshFilter))]
public class grid : MonoBehaviour
{
    public ComputeShader gridPointsCompute;
    int kernel;
    public ComputeBuffer gradientTable3Buffer;
    public ComputeBuffer gridPointsBuffer;
    
    public ComputeBuffer gradientsBuffer;
    public ComputeBuffer noiseBuffer;

    //[HideInInspector]
    public float[][] noiseData;

    [HideInInspector]
    public Color[] colorData;

    public Vector3 unskewedTestPoint;
    public Vector3 skewedTestPoint;
    public int testPointCellIndexX;
    public int testPointCellIndexY;

    [Range(0f, 2f)]
    public float pointRadius;

    public float squishFactor;

    public int topRightIndexX;
    public int topLeftIndexX;
    
    public Vector2 point10;
    public Vector2 point11;
    public Vector2 point01;
    
    public static float cosPosPiOverTwelve;
    public static float cosNegPiOverTwelve;
    public static float sinPosPiOverTwelve;
    public static float sinNegPiOverTwelve;
    public static float negPiOverTwelve;
    public static float sqrt3;

    [HideInInspector]
    public float F2; // Skewing factor for 2D
    [HideInInspector]
    public float G2; // Unskewing factor for 2D

    [HideInInspector]
    public float imageSize; // do not change -> noise scaled with int scale!
    
    [Range(2, 35)]
    public int scale; // scale of lowest noise octave
    [Range(1, 5)]
    public int octaves;
    public int resolution; // texture size
    public uint seed;
    [Range(0f, 1f)]
    public float persistence;
    [Range(1.5f, 2.5f)]
    public float lacunarity;
    
    [HideInInspector]
    public float[] contrast; // contrast is calculated for each octave
    [Range(0f, 2f)]
    public float contrastAdjust;
    public Texture2D noiseTexture;
    public int gridSizeX;
    public int gridSizeY;
    public Vector2[] gridPoints;
    public Vector2[] gradients;
    public Vector2[] skewedGridPoints;
    int stretch;

    public uint[] p;
    public uint[] permutationTable;
    public int[] gradientTable3;
    bool dataGenerated;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (resolution == 0)
        {
            resolution = 4;
        }
        
        Cursor.lockState = CursorLockMode.Locked;

        cosPosPiOverTwelve = Mathf.Cos(Mathf.PI / 12f);
        cosNegPiOverTwelve = Mathf.Cos(-Mathf.PI / 12f);
        sinPosPiOverTwelve = Mathf.Sin(Mathf.PI / 12f);
        sinNegPiOverTwelve = Mathf.Sin(-Mathf.PI / 12f);
        negPiOverTwelve = -Mathf.PI / 12f;
        sqrt3 = Mathf.Sqrt(3f);

        F2 = 0.5f * (Mathf.Sqrt(3f) - 1f); // Skewing factor for 2D
        G2 = (3.0f - Mathf.Sqrt(3f)) / 6f; // Unskewing factor for 2D
        
        imageSize = 100f; // do not change -> noise is scaled with "int scale"!

        kernel = gridPointsCompute.FindKernel("gridPointSimplexNoise");

        contrast = new float[octaves];

        dataGenerated = false;
        
        //https://muugumuugu.github.io/bOOkshelF/generative%20art/simplexnoise.pdf simplex noise demystified        
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
        gradientTable3 = new int[] {1,1,0, -1,1,0, 1,-1,0, -1,-1,0,
                                    1,0,1, -1,0,1, 1,0,-1, -1,0,-1,
                                    0,1,1, 0,-1,1, 0,1,-1,  0,-1,-1};
        generateNoiseTexture(octaves);
    }
    void generateNoiseTextureTiles(int octaves, int tilesX, int tilesY)
    {
        for (int octave = 0; octave < octaves; octave++)
        {
            generateTileData(octave, tilesX, tilesY);
        }
    }
    void generateNoiseTexture(int octaves)
    {
        noiseData = new float[octaves][];
        for (int i = 0; i < octaves; i++)
        {
            noiseData[i] = new float[resolution * resolution];
        }

        for (int octave = 0; octave < octaves; octave++)
        {
            generateData(octave);

            gridPointsCompute.SetFloat("cosPosPiOverTwelve", cosPosPiOverTwelve);
            gridPointsCompute.SetFloat("cosNegPiOverTwelve", cosNegPiOverTwelve);
            gridPointsCompute.SetFloat("sinPosPiOverTwelve", sinPosPiOverTwelve);
            gridPointsCompute.SetFloat("sinNegPiOverTwelve", sinNegPiOverTwelve);
            gridPointsCompute.SetFloat("sqrt3", sqrt3);
            gridPointsCompute.SetFloat("imageSize", imageSize);

            int octaveScale = (int)(Mathf.RoundToInt(scale * Mathf.Pow(lacunarity, (float)octave)));
            gridPointsCompute.SetInt("scale", octaveScale);
            gridPointsCompute.SetInt("resolution", resolution);

            gridPointsCompute.SetFloat("gridPoint01x", gridPoints[1 * gridSizeX + 0].x);
            gridPointsCompute.SetFloat("gridPoint01y", gridPoints[1 * gridSizeX + 0].y);
            gridPointsCompute.SetFloat("gridPoint10x", gridPoints[0 * gridSizeX + 1].x);
            gridPointsCompute.SetFloat("gridPoint10y", gridPoints[0 * gridSizeX + 1].y);
            gridPointsCompute.SetFloat("gridPoint11x", gridPoints[1 * gridSizeX + 1].x);
            gridPointsCompute.SetFloat("gridPoint11y", gridPoints[1 * gridSizeX + 1].y);
            gridPointsCompute.SetFloat("skewedGridPoint11x", skewedGridPoints[1 * gridSizeX + 1].x);
            gridPointsCompute.SetFloat("skewedGridPoint11y", skewedGridPoints[1 * gridSizeX + 1].y);

            gridPointsCompute.SetInt("gridSizeX", gridSizeX);
            gridPointsCompute.SetInt("gridSizeY", gridSizeY);

            gridPointsCompute.SetFloat("F2", F2);
            gridPointsCompute.SetFloat("G2", G2);
            
            gridPointsCompute.SetFloat("squishFactor", squishFactor);

            gradientTable3Buffer = new ComputeBuffer(4*3*3, GetStride<int>());
            gradientTable3Buffer.SetData(gradientTable3);
            gridPointsCompute.SetBuffer(kernel, "gradientTable3", gradientTable3Buffer);

            gridPointsBuffer = new ComputeBuffer(gridSizeX * gridSizeY, GetStride<Vector2>());
            gridPointsBuffer.SetData(gridPoints);
            gridPointsCompute.SetBuffer(kernel, "gridPoints", gridPointsBuffer);

            gradientsBuffer = new ComputeBuffer(gridSizeX * gridSizeY, GetStride<Vector2>());
            gradientsBuffer.SetData(gradients);
            gridPointsCompute.SetBuffer(kernel, "gradients", gradientsBuffer);

            noiseBuffer = new ComputeBuffer(resolution * resolution, GetStride<float>());
            
            for (int i = 0; i < resolution * resolution; i++)
            {
                noiseData[octave][i] = -1f;
            }
            noiseBuffer.SetData(noiseData[octave]);
            gridPointsCompute.SetBuffer(kernel, "noise", noiseBuffer);

            gridPointsCompute.Dispatch(kernel, resolution, resolution, 1);
            noiseBuffer.GetData(noiseData[octave]);

            // float val = 0;
            // for (int n = 0; n < octaves; n++)
            // {
            //     val += noise(v3.x, v3.y, v3.z) * amplitude;
            //     v3 *= lacunarity;
            //     amplitude *= persistence;
            // }
            // return val;
        }

        //for (int i = 1; i < octaves; i++)
        //{
        //    for (int y = 0; y < resolution; y++)
        //    {
        //        for (int x = 0; x < resolution; x++)
        //        {
        //            noiseData[0][y * resolution + x] += noiseData[i][y * resolution + x] * amplitude;
        //            amplitude *= persistence;
        //        }
        //    }
        //} 

        for (int i = 0; i < octaves; i++)
        {
            float minNoise = float.MaxValue;
            float maxNoise = float.MinValue;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    if (noiseData[i][y * resolution + x] > maxNoise)
                    {
                        maxNoise = noiseData[i][y * resolution + x];
                    }
                    if (noiseData[i][y * resolution + x] < minNoise)
                    {
                        minNoise = noiseData[i][y * resolution + x];
                    }
                }
            }
            contrast[i] = 1f / (maxNoise - minNoise);
            Debug.Log("maxNoise[" + i + "]: " + maxNoise + ", minNoise[" + i + "]: " + minNoise + ", contrast[" + i + "]: " + contrast[i]);
        } 
        
        colorData = new Color[resolution * resolution];
        float totalMinNoise = float.MaxValue;
        float totalMaxNoise = float.MinValue;
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float amplitude = 1f;
                float noiseValue = 0f;
                for (int i = 0; i < octaves; i++)
                {
                    noiseValue += contrast[i] * contrastAdjust * amplitude * (noiseData[i][y * resolution + x]);
                    amplitude *= persistence;
                }
                if (noiseValue > totalMaxNoise)
                {
                    totalMaxNoise = noiseValue;
                }
                if (noiseValue < totalMinNoise)
                {
                    totalMinNoise = noiseValue;
                }
                noiseData[0][y * resolution + x] = noiseValue;
            }
        }
        
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float noiseValue = (1f / (totalMaxNoise - totalMinNoise)) * noiseData[0][y * resolution + x];
                colorData[y * resolution + x] = new Color(noiseValue + 0.5f, 
                                                          noiseValue + 0.5f, 
                                                          noiseValue + 0.5f);
            }
        }
            
        noiseTexture = new Texture2D(resolution, resolution);
        noiseTexture.SetPixels(colorData, 0);
        noiseTexture.Apply(false);

        //export PNG image
        byte[] bytes = noiseTexture.EncodeToPNG();
        var dirPath = Application.dataPath + "/../Assets/SaveImages/";
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }
        File.WriteAllBytes(dirPath + "gridCompute" + ".png", bytes);
        Debug.Log("image saved");
    }

    float sampleNoise(float point_x, float point_y)
    {
        float unsquished_x = point_x; //Vector2(p.x, p.y / squishFactor);
        float unsquished_y = point_y / squishFactor;

        Debug.Log("squishFactor: " + squishFactor);

        float rotated_x = cosNegPiOverTwelve * unsquished_x - sinNegPiOverTwelve * unsquished_y;
        float rotated_y = sinNegPiOverTwelve * unsquished_x + cosNegPiOverTwelve * unsquished_y;

        float skewed_x = rotated_x + F2 * (rotated_x + rotated_y);
        float skewed_y = rotated_y + F2 * (rotated_x + rotated_y);

        int cellIndexX = (int)(skewed_x / skewedGridPoints[1 * gridSizeX + 1].x);
        int cellIndexY = (int)(skewed_y / skewedGridPoints[1 * gridSizeX + 1].y);

        Debug.Log("skewed_x: " + skewed_x + ", skewedGridPoints[1 * gridSizeX + 1].x: " + skewedGridPoints[1 * gridSizeX + 1].x + 
                    ", skewed_y: " + skewed_y + ", skewedGridPoints[1 * gridSizeX + 1].y: " + skewedGridPoints[1 * gridSizeX + 1].y);

        int index = 1 * gridSizeX + 1;
        Debug.Log("gridPoints length: " + gridPoints.Length + ", cellIndexX: " + cellIndexX + ", cellIndexY: " + cellIndexY + 
                    "skewedGridPoints length: " + skewedGridPoints.Length + "1 * gridSizeX + 1: " + index);

        float t_value_0_cell_x = (point_x - gridPoints[cellIndexY * gridSizeX + cellIndexX].x) / skewedGridPoints[1 * gridSizeX + 1].x;
        float t_value_0_cell_y = (point_y - gridPoints[cellIndexY * gridSizeX + cellIndexX].y) / skewedGridPoints[1 * gridSizeX + 1].y; // scaling factor geometric to simplexnoise

        int i1;
        int j1; // Offsets for the second and third corners in the simplex.
        float t_value_1_corner_x;
        float t_value_1_corner_y;
        float t_value_2_diagonal_x;
        float t_value_2_diagonal_y;

        if (t_value_0_cell_x * squishFactor * sqrt3 > t_value_0_cell_y) // TEST: * squishFactor * sqrt3 // TODO: calculate t_values differently because triangles are squished !
        {
            // lower triangle
            i1 = 1; j1 = 0;
            //t_value_1_corner_x = point_x - gridPoints[cellIndexX + 1, cellIndexY].x;
            //t_value_1_corner_y = point_y - gridPoints[cellIndexX + 1, cellIndexY].y;
            t_value_1_corner_x = (point_x - gridPoints[cellIndexY * gridSizeX + cellIndexX + 1].x) / (skewedGridPoints[1 * gridSizeX + 1].x); // test: divide by cell size | funkt !!!
            t_value_1_corner_y = (point_y - gridPoints[cellIndexY * gridSizeX + cellIndexX + 1].y) / (skewedGridPoints[1 * gridSizeX + 1].y); // test / 2f -> visible seams at triangle cells
            
            //t_value_1_corner_x = t_value_0_cell_x - 1f + G2;          // second corner
            //t_value_1_corner_y = t_value_0_cell_y + G2;                 // second corner
        }
        else
        {
            // upper triangle
            i1 = 0; j1 = 1;
            t_value_1_corner_x = (point_x - gridPoints[(cellIndexY + 1) * gridSizeX + cellIndexX].x) / (skewedGridPoints[1 * gridSizeX + 1].x); // test / 0.5f -> visible seams at triangle cells
            t_value_1_corner_y = (point_y - gridPoints[(cellIndexY + 1) * gridSizeX + cellIndexX].y) / (skewedGridPoints[1 * gridSizeX + 1].y);

            //t_value_1_corner_x = t_value_0_cell_x + G2;                 // second corner
            //t_value_1_corner_y = t_value_0_cell_y - 1f + G2;          // second corner
        }

        // diagonal
        t_value_2_diagonal_x = (point_x - gridPoints[(cellIndexY + 1) * gridSizeX + cellIndexX + 1].x) / (skewedGridPoints[1 * gridSizeX + 1].x);
        t_value_2_diagonal_y = (point_y - gridPoints[(cellIndexY + 1) * gridSizeX + cellIndexX + 1].y) / (skewedGridPoints[1 * gridSizeX + 1].y);

        //t_value_2_diagonal_x = t_value_0_cell_x - 1f + 2f * G2; // third corner
        //t_value_2_diagonal_y = t_value_0_cell_y - 1f + 2f * G2; // third corner

        // Hash the corner coordinates to get the gradient indices.
        int ii = cellIndexX & 255;
        int jj = cellIndexY & 255;
         
        // cell gradient, permutationTable is set in generateData()
        Vector2 gradient0 = gradients[cellIndexY * gridSizeX + cellIndexX];            // [cellIndexX, cellIndexY]; 
        Vector2 gradient1 = gradients[(cellIndexY + j1) * gridSizeX + cellIndexX + i1];// [cellIndexX + i1, cellIndexY + j1];
        Vector2 gradient2 = gradients[(cellIndexY + 1) * gridSizeX + cellIndexX + 1];  // [cellIndexX + 1, cellIndexY + 1];
    
        // Compute the contribution from each corner.

        // The square of the distance to the first corner
        float t0 = 0.5f - t_value_0_cell_x * t_value_0_cell_x - t_value_0_cell_y * t_value_0_cell_y; // TEST 1  (was: 0.5)
        float n0 = 0.0f;

        if (t0 < 0f) 
        {
            n0 = 0.0f; // If it's negative, there is no contribution
        }
        else
        {
            t0 = t0 * t0; // Apply the fade function to the distance
            Vector2 t0_value_xy = new Vector2(t_value_0_cell_x, t_value_0_cell_y);
            n0 = t0 * t0 * Vector2.Dot(gradient0, t0_value_xy); // Calculate the gradient dot product
        }

        // The square of the distance to the second corner
        float t1 = 0.5f - t_value_1_corner_x * t_value_1_corner_x - t_value_1_corner_y * t_value_1_corner_y; 
        float n1 = 0.0f;

        if (t1 < 0f)
        {
            n1 = 0.0f;
        }
        else
        {
            t1 = t1 * t1; // Apply fade function
            Vector2 t1_value_xy = new Vector2(t_value_1_corner_x, t_value_1_corner_y);
            n1 = t1 * t1 * Vector2.Dot(gradient1, t1_value_xy); // Calculate the gradient dot product
        }

        // The square of the distance to the third corner
        float t2 = 0.5f - t_value_2_diagonal_x * t_value_2_diagonal_x - t_value_2_diagonal_y * t_value_2_diagonal_y;
        float n2 = 0.0f;

        if (t2 < 0f) 
        {
            n2 = 0.0f;
        }
        else
        {
            t2 = t2 * t2; // Apply fade function
            Vector2 t2_value_xy = new Vector2(t_value_2_diagonal_x, t_value_2_diagonal_y);
            n2 = t2 * t2 * Vector2.Dot(gradient2, t2_value_xy); // Calculate the gradient dot product
        }

        // Debug: distance from cell
        // return (Mathf.Sqrt(t_value_0_cell_x * t_value_0_cell_x + t_value_0_cell_y * t_value_0_cell_y) / 60f); // funkt!
        // return (Mathf.Sqrt(t_value_2_diagonal_x * t_value_2_diagonal_x + t_value_2_diagonal_y * t_value_2_diagonal_y) / 60f);  // funkt!
        // return (Mathf.Sqrt(t_value_1_corner_x * t_value_1_corner_x + t_value_1_corner_y * t_value_1_corner_y) / 60f); // funkt!

        // return ((float)(cellIndexX + cellIndexY)); // noiseData = sampleNoise // OK

        // Combine the results from each corner.
        return (n0 + n1 + n2);
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 mouseDelta = Input.mousePositionDelta;
        unskewedTestPoint += new Vector3(mouseDelta.x , 0f, mouseDelta.y) / 16f;
        if (Input.GetKeyDown(KeyCode.C))
        {
            unskewedTestPoint = new Vector3(0f, 0f, 0f);
        }
    
        skewedTestPoint = skewPoint(rotatePoint(unSquishPoints(new Vector3(unskewedTestPoint.x, unskewedTestPoint.z)), negPiOverTwelve));
        Vector2 unsquishedTestPoint = new Vector2(skewedTestPoint.x, skewedTestPoint.y / squishFactor);
        
    
        Vector2 rotatedTestPoint = new Vector2(cosNegPiOverTwelve * unsquishedTestPoint.x - sinNegPiOverTwelve * unsquishedTestPoint.y, 
                                               sinNegPiOverTwelve * unsquishedTestPoint.x + cosNegPiOverTwelve * unsquishedTestPoint.y);
        
        testPointCellIndexX = ((int)(skewedTestPoint.x / skewedGridPoints[1 * gridSizeX + 1].x) + gridSizeX) % gridSizeX;
        testPointCellIndexY = ((int)(skewedTestPoint.y / skewedGridPoints[1 * gridSizeX + 1].y) + gridSizeY) % gridSizeY;
    }

    void generateTileData(int octave, int tilesX, int tilesY)
    {
        for (int tileX = 0; tileX < tilesX; tileX++)
        {
            for (int tileY = 0; tileY < tilesY; tileY++)
            {
                // best with large grid size!

                gridSizeY = (int)(Mathf.Pow(2f, (float)octave) * scale * 2f / sqrt3 + 2f); 
                gridSizeX = (int)(Mathf.Pow(2f, (float)octave) * scale * (1f + sqrt3 / 3f) + 2f); 

                // TODO: match edge gradients left to right, and top to botton of all tile pairs
            }
        }
    }

    void generateData(int octave)
    {
        // triangular grid size best fit
        gridSizeY = (int)(Mathf.Pow(2f, (float)octave) * scale * 2f / sqrt3 + 2f); 
        gridSizeX = (int)(Mathf.Pow(2f, (float)octave) * scale * (1f + sqrt3 / 3f) + 2f); 
        
        int topIndexX = gridSizeY / 2 - 1; 

        Vector2 gridPointX1Y0 = (imageSize / (float)(Mathf.Pow(2f, (float)octave) * scale)) * (new Vector2(1f, 0f));

        Vector2 topRightGridPoint = (imageSize / (float)(Mathf.Pow(2f, (float)octave) * scale)) * (new Vector2(1f, 0f) * (float)(gridSizeX - 1) + 
                                       new Vector2(Mathf.Cos(2f * Mathf.PI / 3f), Mathf.Sin(2f * Mathf.PI / 3f)) * (float)(gridSizeY - 1));

        if (topRightGridPoint.x - imageSize > gridPointX1Y0.x / 4f)
        {
            // stretch
            stretch = 1;
            // remove one row
            Debug.Log("stretch");
            gridSizeY = gridSizeY - 1;
            topRightIndexX = gridSizeX - 2;
            //Debug.Log("octave: " + octave + ", stretch -> topRightIndexX: " + topRightIndexX); // octave: 0, stretch -> topRightIndexX: 9
            gridPoints = new Vector2[gridSizeX * gridSizeY];
            gradients = new Vector2[gridSizeX * gridSizeY];
            skewedGridPoints = new Vector2[gridSizeX * gridSizeY];

            for (int y = 0; y < gridSizeY; y++)
            {
                for (int x = 0; x < gridSizeX; x++)
                {
                    gridPoints[y * gridSizeX + x] = (imageSize / (float)(Mathf.Pow(2f, (float)octave) * scale)) * (new Vector2(1f, 0f) * (float)x + 
                                       new Vector2(Mathf.Cos(2f * Mathf.PI / 3f), Mathf.Sin(2f * Mathf.PI / 3f)) * (float)y);
                }
            }
            float octaveScale = Mathf.Pow(2f, (float)octave) * scale;
            Debug.Log("Scale: " + octaveScale);
            Debug.Log("imageSize " + imageSize + ", gridPoints[1, gridSizeY - 1].y: " + gridPoints[(gridSizeY - 1) * gridSizeX + 0].y);
            squishFactor = imageSize / gridPoints[(gridSizeY - 1) * gridSizeX + 0].y; 
            Debug.Log("squishFactor: " + squishFactor);
        }
        else
        {
            Debug.Log("squish");
            stretch = 0;
            topRightIndexX = gridSizeX - 1;
            //Debug.Log("octave: " + octave + ", squish -> topRightIndexX: " + topRightIndexX); // octave: 1, squish -> topRightIndexX: 19 OK 
            gridPoints = new Vector2[gridSizeX * gridSizeY];
            gradients = new Vector2[gridSizeX * gridSizeY];
            skewedGridPoints = new Vector2[gridSizeX * gridSizeY];

            for (int y = 0; y < gridSizeY; y++)
            {
                for (int x = 0; x < gridSizeX; x++)
                {
                    gridPoints[y * gridSizeX + x] = (imageSize / (float)(Mathf.Pow(2f, (float)octave) * scale)) * (new Vector2(1f, 0f) * (float)x + 
                                       new Vector2(Mathf.Cos(2f * Mathf.PI / 3f), Mathf.Sin(2f * Mathf.PI / 3f)) * (float)y);
                }
            }
            // squish
            float octaveScale = Mathf.Pow(2f, (float)octave) * scale;
            Debug.Log("Scale: " + octaveScale);
            Debug.Log("imageSize: " + imageSize + ", gridPoints[0, gridSizeY - 1].y: " + gridPoints[(gridSizeY - 1) * gridSizeX + 0].y);
            squishFactor = imageSize / gridPoints[(gridSizeY - 1) * gridSizeX + 0].y;
            Debug.Log("squishFactor: " + squishFactor);
        }

        // stretch / squish grid points into frame
        for (int y = 0; y < gridSizeY; y++)
        {
            for (int x = 0; x < gridSizeX; x++)
            {
                gridPoints[y * gridSizeX + x] = new Vector2(gridPoints[y * gridSizeX + x].x, gridPoints[y * gridSizeX + x].y * squishFactor);

                // unsquish
                skewedGridPoints[y * gridSizeX + x] = unSquishPoints(gridPoints[y * gridSizeX + x]);

                // rotate
                skewedGridPoints[y * gridSizeX + x] = rotatePoint(skewedGridPoints[y * gridSizeX + x], negPiOverTwelve);

                // skew point
                skewedGridPoints[y * gridSizeX + x] = skewPoint(skewedGridPoints[y * gridSizeX + x]);
            }
        }
        
        // set gradients
        for (int y = 0; y < gridSizeY; y++)
        {
            for (int x = 0; x < gridSizeX; x++)
            {
                uint hashValue1 = hash(seed);
                uint hashValue2 = hash(seed + 5236441);

                uint ii = ((uint)x + hashValue1) & 255; //x & 255;
                uint jj = ((uint)y + hashValue2) & 255; //y & 255;
                uint gradientIndex0 = permutationTable[ii + permutationTable[jj]] % 12; // cell gradient
                // uint gradientIndex0 = permutationTable[ii + permutationTable[jj]] % 12; // cell gradient
                
                gradients[y * gridSizeX + x] = new Vector2(gradientTable3[gradientIndex0 * 3 + 0], gradientTable3[gradientIndex0 * 3 + 1]); // cell gradient
            }   // [x, y] -> y * gridSizeX + x
        }

        // set repeating gradients // mirrored gradients
        topLeftIndexX = (gridSizeY - 1) / 2;

        // top row
        for (int x = 0; x < gridSizeX - topLeftIndexX - 1; x++)
        {
            gradients[(gridSizeY - 1) * gridSizeX + x + topLeftIndexX] = gradients[x];
        }

        // right row
        for (int y = 0; y < (gridSizeY - 1) / 2 + 1; y++)
        {
            gradients[2 * y * gridSizeX + gridSizeX - topLeftIndexX + y - 1 - stretch] = gradients[2 * y * gridSizeX + y]; 
        }

        for (int y = 0; y < (gridSizeY - 1) / 2; y++)
        {
            // right outside = left inside
            gradients[(2 * y + 1) * gridSizeX + gridSizeX - topLeftIndexX + y - 1 - stretch + 1] = gradients[(2 * y + 1) * gridSizeX + y + 1]; 
        }

        // left outside
        for (int y = 0; y < (gridSizeY - 1) / 2; y++)
        {
            // right inside = left outside
            gradients[(2 * y + 1) * gridSizeX + gridSizeX - topLeftIndexX + y - 1 - stretch] = gradients[(2 * y + 1) * gridSizeX + y];
        }

        dataGenerated = true;
    }

    uint hash(uint a)
    {
        // uint32_t hash( uint32_t a)
         a = (a ^ 61) ^ (a >> 16);
         a = a + (a << 3);
         a = a ^ (a >> 4);
         a = a * 0x27d4eb2d;
         a = a ^ (a >> 15);
         return a; // https://burtleburtle.net/bob/hash/integer.html
    }

    Vector2 unSquishPoints(Vector2 p)
    {
        return new Vector2(p.x, p.y / squishFactor);
    }

    Vector2 unSquishRotate(Vector2 p, float angle)
    {
        return new Vector2(Mathf.Cos(angle) * p.x - Mathf.Sin(angle) * p.y / squishFactor, 
                           Mathf.Sin(angle) * p.x + Mathf.Cos(angle) * p.y / squishFactor);
    }

    Vector2 rotatePoint(Vector2 p, float angle)
    {
        return new Vector2(Mathf.Cos(angle) * p.x - Mathf.Sin(angle) * p.y, 
                           Mathf.Sin(angle) * p.x + Mathf.Cos(angle) * p.y);
    }

    Vector2 skewPoint(Vector2 p)
    {
        return new Vector2(p.x + F2 * (p.x + p.y), 
                           p.y + F2 * (p.x + p.y));
    }

    public static int GetStride<T>()
	{
		return System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
	}

    void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawLine(new Vector3( 0f, 0f,  0f), new Vector3(imageSize, 0f,  0f));
        Gizmos.DrawLine(new Vector3(imageSize, 0f,  0f), new Vector3(imageSize, 0f, imageSize));
        Gizmos.DrawLine(new Vector3(imageSize, 0f, imageSize), new Vector3( 0f, 0f, imageSize));
        Gizmos.DrawLine(new Vector3( 0f, 0f, imageSize), new Vector3( 0f, 0f,  0f));

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(new Vector3(point01.x, 0f, point01.y), pointRadius);
        Gizmos.DrawSphere(new Vector3(point10.x, 0f, point10.y), pointRadius);
        Gizmos.DrawSphere(new Vector3(point11.x, 0f, point11.y), pointRadius);

        Gizmos.DrawSphere(unskewedTestPoint, pointRadius * 1.3f);


        if (gridPoints != null && dataGenerated == true)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                for (int x = 0; x < gridSizeX; x++)
                {
                    Gizmos.DrawSphere(new Vector3(gridPoints[y * gridSizeX + x].x, 0f, gridPoints[y * gridSizeX + x].y), pointRadius * 0.6f);
                    Gizmos.DrawRay(new Vector3(gridPoints[y * gridSizeX + x].x, 0f, gridPoints[y * gridSizeX + x].y), new Vector3(gradients[y * gridSizeX + x].x, 0f, gradients[y * gridSizeX + x].y) * 2f * pointRadius);
                }
            }
            Gizmos.color = Color.green;
            Gizmos.DrawLine(new Vector3(gridPoints[testPointCellIndexY * gridSizeX + testPointCellIndexX].x, 0f, gridPoints[testPointCellIndexY * gridSizeX + testPointCellIndexX].y), 
                            unskewedTestPoint);
        }

        Gizmos.color = new Color(0.8f, 0.4f, 0f); // orange
        Gizmos.DrawSphere(new Vector3(skewedTestPoint.x, 0f, skewedTestPoint.y), pointRadius * 1.3f);

        if (skewedGridPoints != null)
        {
            foreach (Vector2 v in skewedGridPoints)
            {
                Gizmos.DrawSphere(new Vector3(v.x, 0f, v.y), pointRadius * 0.6f);
            }
        }

        // mirrored gradients-------------------------------------------------------------------------
        Gizmos.color = Color.green;
        if (gridPoints != null && dataGenerated == true)
        {
            for (int y = 0; y < (gridSizeY - 1) / 2 + 1; y++)
            {
                Gizmos.DrawSphere(new Vector3(gridPoints[2 * y * gridSizeX + y].x, 0f, gridPoints[2 * y * gridSizeX + y].y), 1.0f * pointRadius); // left edge

                Gizmos.DrawSphere(new Vector3(gridPoints[2 * y * gridSizeX + gridSizeX - topLeftIndexX + y - 1 - stretch].x, 0f, 
                                              gridPoints[2 * y * gridSizeX + gridSizeX - topLeftIndexX + y - 1 - stretch].y), 1.0f * pointRadius); 
            }
            for (int y = 0; y < (gridSizeY - 1) / 2; y++)
            {
                Gizmos.DrawSphere(new Vector3(gridPoints[(2 * y + 1) * gridSizeX + gridSizeX - topLeftIndexX + y - 1 - stretch + 1].x, 0f, 
                                              gridPoints[(2 * y + 1) * gridSizeX + gridSizeX - topLeftIndexX + y - 1 - stretch + 1].y), 1.0f * pointRadius);

                Gizmos.DrawSphere(new Vector3(gridPoints[(2 * y + 1) * gridSizeX + y + 1].x, 0f, 
                                              gridPoints[(2 * y + 1) * gridSizeX + y + 1].y), 1.0f * pointRadius); // left indside
            }

            // left outside
            for (int y = 0; y < (gridSizeY - 1) / 2; y++)
            {
                Gizmos.DrawSphere(new Vector3(gridPoints[(2 * y + 1) * gridSizeX + y].x, 0f, 
                                              gridPoints[(2 * y + 1) * gridSizeX + y].y), 1.0f * pointRadius); // left outside
            }

            // right inside
            for (int y = 0; y < (gridSizeY - 1) / 2; y++)
            {
                Gizmos.DrawSphere(new Vector3(gridPoints[(2 * y + 1) * gridSizeX + gridSizeX - topLeftIndexX + y - 1 - stretch].x, 0f, 
                                              gridPoints[(2 * y + 1) * gridSizeX + gridSizeX - topLeftIndexX + y - 1 - stretch].y), 1.0f * pointRadius); // right inside
            }

            // top / botton
            // top row
            for (int x = 0; x < gridSizeX - topLeftIndexX - 1; x++)
            {
                Gizmos.DrawSphere(new Vector3(gridPoints[(gridSizeY - 1) * gridSizeX + x + topLeftIndexX].x, 0f, 
                                              gridPoints[(gridSizeY - 1) * gridSizeX + x + topLeftIndexX].y), 1.0f * pointRadius); // top edge
                
                Gizmos.DrawSphere(new Vector3(gridPoints[x].x, 0f, 
                                              gridPoints[x].y), 1.0f * pointRadius); // bottom edge
            }
        }
    }
}
}