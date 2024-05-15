using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class quad : MonoBehaviour
{
    public Mesh mesh;

    public List<Vector3> vertices;
    public List<Vector2> uvs;
    public List<int> triangles;
    public List<Vector3> normals;

    public MeshRenderer meshRenderer;

    Material noiseMaterial;

    public noise noiseTextureGenerator;

    public int width = 1024 * 2;
    public int height = 1024 * 2;
    public float noiseScale = 50f; // TEST scale = 50
    public int octaves = 5;

    // Start is called before the first frame update
    void Start()
    {
        vertices.Add(new Vector3(-1f, 0f, -1f));
        vertices.Add(new Vector3(-1f, 0f,  1f));
        vertices.Add(new Vector3( 1f, 0f,  1f));
        vertices.Add(new Vector3( 1f, 0f, -1f));
        vertices.Add(new Vector3( 0f, 0f,  0f));

        for (int i = 0; i < 5; i++)
        {
            Vector3 v = vertices[i];
            float x = v.x;
            float z = v.z;
            uvs.Add(new Vector2((x + 1f)/2f, (z + 1f)/2f));
            normals.Add(new Vector3(0f, 1f, 0f));
        }

        triangles.Add(0);
        triangles.Add(1);
        triangles.Add(4);
        triangles.Add(1);
        triangles.Add(2);
        triangles.Add(4);
        triangles.Add(2);
        triangles.Add(3);
        triangles.Add(4);
        triangles.Add(3);
        triangles.Add(0);
        triangles.Add(4);

        
        noiseTextureGenerator = new noise(width, height, noiseScale, octaves);

        meshRenderer = GetComponent<MeshRenderer>();

        noiseMaterial = new Material(Shader.Find("Standard"));

        //material.SetTexture("_MainTex", texture);

        meshRenderer.material = noiseMaterial;

        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uvs.ToArray();

        noiseTextureGenerator.rend = GetComponent<Renderer>();

        noiseTextureGenerator.generateNoiseTexture();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
