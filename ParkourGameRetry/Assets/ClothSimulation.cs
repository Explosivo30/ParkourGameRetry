using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine;
using Unity.Mathematics;

public class ClothSimulation : MonoBehaviour
{
    public int clothSize = 10; // Cloth grid size (10x10)
    public float damping = 0.99f; // Velocity damping
    public float stiffness = 0.5f; // Constraint strength

    private NativeArray<Vector3> positions;
    private NativeArray<Vector3> velocities;
    private NativeArray<Vector3> forces;
    private NativeArray<int2> constraints;

    private Mesh mesh;
    private Vector3[] meshVertices;

    void Start()
    {
        int totalVerts = clothSize * clothSize;
        positions = new NativeArray<Vector3>(totalVerts, Allocator.Persistent);
        velocities = new NativeArray<Vector3>(totalVerts, Allocator.Persistent);
        forces = new NativeArray<Vector3>(totalVerts, Allocator.Persistent);

        GenerateMesh();
        GenerateConstraints();
    }

    void Update()
    {
        // Apply physics simulation
        ClothSimulationJob simulationJob = new ClothSimulationJob
        {
            positions = positions,
            velocities = velocities,
            forces = forces,
            deltaTime = Time.deltaTime,
            damping = damping
        };

        JobHandle simHandle = simulationJob.Schedule(positions.Length, 32);

        // Apply structural constraints
        ClothConstraintJob constraintJob = new ClothConstraintJob
        {
            positions = positions,
            constraints = constraints,
            stiffness = stiffness
        };

        JobHandle constraintHandle = constraintJob.Schedule(constraints.Length, 16, simHandle);

        constraintHandle.Complete();

        // Update mesh vertices
        for (int i = 0; i < positions.Length; i++)
        {
            meshVertices[i] = positions[i];
        }
        mesh.vertices = meshVertices;
        mesh.RecalculateNormals();
    }

    void OnDestroy()
    {
        positions.Dispose();
        velocities.Dispose();
        forces.Dispose();
        constraints.Dispose();
    }

    void GenerateMesh()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        int totalVerts = clothSize * clothSize;
        Vector3[] verts = new Vector3[totalVerts];
        int[] tris = new int[(clothSize - 1) * (clothSize - 1) * 6];

        for (int y = 0; y < clothSize; y++)
        {
            for (int x = 0; x < clothSize; x++)
            {
                int index = y * clothSize + x;
                verts[index] = new Vector3(x, -y, 0);
                positions[index] = verts[index];
                velocities[index] = Vector3.zero;
                forces[index] = new Vector3(0, -9.81f, 0); // Gravity
            }
        }

        int triIndex = 0;
        for (int y = 0; y < clothSize - 1; y++)
        {
            for (int x = 0; x < clothSize - 1; x++)
            {
                int i = y * clothSize + x;
                tris[triIndex++] = i;
                tris[triIndex++] = i + clothSize;
                tris[triIndex++] = i + 1;

                tris[triIndex++] = i + 1;
                tris[triIndex++] = i + clothSize;
                tris[triIndex++] = i + clothSize + 1;
            }
        }

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        meshVertices = mesh.vertices;
    }

    void GenerateConstraints()
    {
        int constraintCount = (clothSize - 1) * clothSize * 2;
        constraints = new NativeArray<int2>(constraintCount, Allocator.Persistent);

        int index = 0;
        for (int y = 0; y < clothSize; y++)
        {
            for (int x = 0; x < clothSize - 1; x++)
            {
                constraints[index++] = new int2(y * clothSize + x, y * clothSize + x + 1);
            }
        }

        for (int y = 0; y < clothSize - 1; y++)
        {
            for (int x = 0; x < clothSize; x++)
            {
                constraints[index++] = new int2(y * clothSize + x, (y + 1) * clothSize + x);
            }
        }
    }
}

[BurstCompile]
struct ClothSimulationJob : IJobParallelFor
{
    public NativeArray<Vector3> positions;
    public NativeArray<Vector3> velocities;
    public NativeArray<Vector3> forces;
    public float deltaTime;
    public float damping;

    public void Execute(int index)
    {
        velocities[index] += forces[index] * deltaTime;
        velocities[index] *= damping;
        positions[index] += velocities[index] * deltaTime;
    }
}

[BurstCompile]
struct ClothConstraintJob : IJobParallelFor
{
    public NativeArray<Vector3> positions;
    [ReadOnly] public NativeArray<int2> constraints;
    public float stiffness;

    public void Execute(int index)
    {
        int2 edge = constraints[index];
        Vector3 p1 = positions[edge.x];
        Vector3 p2 = positions[edge.y];

        Vector3 delta = p2 - p1;
        float distance = delta.magnitude;
        float correction = (distance - 1.0f) * stiffness;

        positions[edge.x] += delta.normalized * correction * 0.5f;
        positions[edge.y] -= delta.normalized * correction * 0.5f;
    }
}
