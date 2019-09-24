using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using UnityEngine;
using System.Linq;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

using Handle = System.Int64;
using I64 = System.Int64;

// This class is in dire need of a large refactoring. It was largely written in an explorative manner,
// with lots of duplication to avoid disruptive changes to old functioning code when new features were added.
// I have tried to do some cleanup by removing some unnecessary code and putting stuff into logical regions.
// However, more is obviously needed.
public class IfcConverter : MonoBehaviour
{
    // General variables used all around.
    #region GeneralVariables
    // List of all the IFCObjects that has been triangulated
    private List<GameObject> mGameObjects = new List<GameObject>();

    // Center of the global bounds
    private Vector3 mCenter = Vector3.zero;

    // Bounds large enough to contain the entire model (FIT layer objects fit within this)
    private Bounds mGlobalBounds;

    // All the voxels (bounds) located on various levels.
    private List<List<Bounds>> mVoxels = new List<List<Bounds>>();

    // Parent of all objects contained within each voxel in a grid.
    private List<List<GameObject>> mBoundsParents = new List<List<GameObject>>();

    // Parents of the grids located on various levels.
    private List<GameObject> mLevelParents = new List<GameObject>();

    [Header("Write layer sizes in descending order, leave 0 blank, reserved for global bounds")]
    public List<Vector3> LevelCellSizes = new List<Vector3>
    {
        Vector3.zero,
        new Vector3(12.0f, 12.0f, 12.0f),
        new Vector3(6.0f, 6.0f, 6.0f),
        new Vector3(3.0f, 3.0f, 3.0f),
    };
    #endregion

    #region EditorExposedAndControllingFunctions
    [ContextMenu("Convert Model")]
    private void ConvertModel()
    {
        mIFCFolderPath = @"Assets\IFCFiles\" + IFCFoldername;
        IFCSchemaName = @"Assets\" + IFCSchemaName + ".exp";

        foreach (var _ in LevelCellSizes)
            mBoundsParents.Add(new List<GameObject>());

        mLevelParents.Add(new GameObject("LevelFit"));
        for (int i = 1; i < LevelCellSizes.Count; i++)
            mLevelParents.Add(new GameObject($"Level {i}"));

        StartCoroutine(MainCoroutine());
    }

    [ContextMenu("Calculate grid")]
    private void CalculateGrid()
    {
        Debug.Log("Calculating Grid");
        LevelCellSizes[0] = mGlobalBounds.size;
        SplitModel();
        PutObjectsInBins();

        StartCoroutine(CalculateStatsForAllCells());
        StartCoroutine(ConvertMeshesToColliderBlobs());
    }

    private IEnumerator MainCoroutine()
    {
        var info = new DirectoryInfo(mIFCFolderPath);
        var foldernames = new List<string>();
        var files = info.GetFiles("*.ifc", SearchOption.AllDirectories);
        Array.Sort(files, (x, y) => (int)(y.Length - x.Length));

        var timer_start = Time.realtimeSinceStartup;
        for (int i = 0; i < files.Length; i++)
            yield return LoadFile(files[i].FullName);
        var timer_end = Time.realtimeSinceStartup;
        Debug.LogWarning($"Time spent triangulating: {timer_end - timer_start}");

        // Move objects to origin
        mGlobalBounds = EncapsulateObjectsInBounds(mGameObjects);
        mCenter = mGlobalBounds.center;
        var origin = GameObject.Find("Origin");
        var diff = mCenter - origin.transform.position;
        foreach (var go in mGameObjects)
            go.transform.position -= diff;

        mGlobalBounds.center -= diff;
        mCenter -= diff;

        CalculateGrid();
    }
    #endregion

    #region VoxelLogic
    // Inspired by: https://answers.unity.com/questions/511841/how-to-make-an-object-move-away-from-three-or-more.html
    private Bounds EncapsulateObjectsInBounds(List<GameObject> gos)
    {
        if (gos.Count == 0)
            return new Bounds();

        var b = gos[0].GetComponent<Renderer>().bounds;
        for (int i = 1; i < gos.Count; i++)
            b.Encapsulate(gos[i].GetComponent<Renderer>().bounds);

        return b;
    }

    private List<Bounds> CreateBoundsGrid(Vector3 min, Vector3 max, Vector3 cellSize)
    {
        var offset = cellSize / 2.0f;
        var grid = new List<Bounds>();
        for (float z = min.z; z < max.z; z += cellSize.z)
            for (float y = min.y; y < max.y; y += cellSize.y)
                for (float x = min.x; x < max.x; x += cellSize.x)
                    grid.Add(new Bounds(new Vector3(x + offset.x, y + offset.y, z + offset.z), cellSize));

        return grid;
    }

    private void SplitModel()
    {
        mVoxels.Clear();
        foreach (var _ in LevelCellSizes)
            mVoxels.Add(new List<Bounds>());

        // Specially adapting first two bounds to ensure good cover.
        mVoxels[0] = new List<Bounds>() { mGlobalBounds };
        mVoxels[1] = CreateBoundsGrid(mGlobalBounds.min - LevelCellSizes[1] / 2.0f, mGlobalBounds.max + LevelCellSizes[1] / 2.0f, LevelCellSizes[1]);
        for (int i = 2; i < LevelCellSizes.Count; i++)
            mVoxels[i] = CreateBoundsGrid(mVoxels[i - 1][0].min, mVoxels[i - 1][mVoxels[i - 1].Count - 1].max, LevelCellSizes[i]);

        // Need to kill all the children in previous mBoundsParents
        foreach (var set in mBoundsParents)
        {
            foreach (var go in set)
            {
                int childCount = go.transform.childCount;
                for (int i = 0; i < childCount; i++)
                    go.transform.GetChild(0).parent = null;
                GameObject.Destroy(go);
            }
            set.Clear();
        }

        // Assign all mBoundsParents to their respective levels.
        for (int i = 0; i < mVoxels.Count; i++)
        {
            for (int j = 0; j < mVoxels[i].Count; j++)
            {
                var go = new GameObject($"Layer: {i} ID: {j}");
                go.transform.position = mVoxels[i][j].center;
                go.transform.parent = mLevelParents[i].transform;
                mBoundsParents[i].Add(go);
            }
        }
    }

    /////////////////////////////////////////////////////////////////
    // Count objects pr box
    // What Hierarchy do we have:
    // - Levels
    //      - Boxes
    // Proposed Hierarchy:
    // - Level GameObject
    //      - Cells GameObject
    //          - All gameobjects inside cell is children.
    private void PutObjectsInBins()
    {
        foreach (var go in mGameObjects)
        {
            // Check which level bounds you can fit into.
            int layer = LevelCellSizes.Count - 1;
            var goBounds = go.GetComponent<Renderer>().bounds;
            while (layer > 0)
            {
                var b = new Bounds(goBounds.center, LevelCellSizes[layer]);
                if (b.Contains(goBounds.min) && b.Contains(goBounds.max))
                    break;

                layer--;
            }

            // Smarter approach here would be to start with top bounds and work my way down until I am at the layer I am after.
            // For this I need to be able to calculate in 3D coordinates where they are.
            // However, keep this approach for now, as it is good enough.
            for (int i = 0; i < mVoxels[layer].Count; i++)
            {
                if (mVoxels[layer][i].Contains(goBounds.center))
                {
                    go.transform.parent = mBoundsParents[layer][i].transform;
                    break;
                }
            }
        }
    }

    #endregion

    #region Visualization
    [Range(-1, 5)]
    public int RenderLevel = 1;
    public bool RenderGizmos = false;

    public int3 BoxCoords = int3.zero;

    public void OnDrawGizmos()
    {
        if (!RenderGizmos)
            return;

        var colors = new Color[4] { Color.red, Color.green, Color.magenta, Color.blue };

        if (RenderLevel != -1 && mVoxels.Count > 0 && mVoxels[0] != null)
        {
            if (RenderLevel == mVoxels.Count)
            {
                for (int i = 0; i < mVoxels.Count; i++)
                {
                    foreach (var b in mVoxels[i])
                        BoundsDrawer.Draw(b, colors[i % colors.Length]);
                }
            }
            else
            {
                for (int i = 0; i < mVoxels[RenderLevel].Count; i++)
                {
                    BoundsDrawer.Draw(mVoxels[RenderLevel][i], colors[RenderLevel % colors.Length]);
                }
            }

        }

        if (mCenter != Vector3.zero)
            Gizmos.DrawSphere(mCenter, 10.0f);

        DrawStreamBounds();
    }

    private List<List<int>> mSelectedBounds = new List<List<int>>();
    [ContextMenu("Visualize Stream Bounds")]
    private void VisualizeStreamBounds()
    {
        // Calculate which box we are working with:
        var cellSizes = LevelCellSizes[LevelCellSizes.Count - 1];
        var lastLevel = mVoxels.Count - 1;
        var firstCell = mVoxels[lastLevel][0];
        var lastCell = mVoxels[lastLevel][mVoxels[lastLevel].Count - 1];
        var size = lastCell.max - firstCell.min;
        var columns = (int)(size.x / cellSizes.x);
        var rows = (int)(size.y / cellSizes.y);
        var depth = (int)(size.z / cellSizes.z);

        var selectedBounds = new List<List<int>>();
        for (int i = 0; i < mVoxels.Count - 1; i++)
            selectedBounds.Add(new List<int>());

        var last = selectedBounds.Count - 1;

        int3 dimensions = new int3(columns, rows, depth);
        var middle = IndexUtility.CoordsToIdx(BoxCoords, dimensions);
        if (middle >= 0 && middle < mVoxels[lastLevel].Count)
        {
            for (int i = Math.Max(BoxCoords.z - 1, 0); i <= Math.Min(BoxCoords.z + 1, depth - 1); i++)
                for (int j = Math.Max(BoxCoords.y - 1, 0); j <= Math.Min(BoxCoords.y + 1, rows - 1); j++)
                    for (int k = Math.Max(BoxCoords.x - 1, 0); k <= Math.Min(BoxCoords.x + 1, columns - 1); k++)
                    {
                        var idx = IndexUtility.CoordsToIdx(new int3(k, j, i), dimensions);
                        if (idx >= 0 && idx < mVoxels[lastLevel].Count)
                            selectedBounds[last].Add(idx);
                    }
        }

        foreach (var b in selectedBounds[last])
            BoundsDrawer.Draw(mVoxels[mVoxels.Count - 1][b], new Color32(255, 0, 255, 255));

        BoundsDrawer.Draw(mVoxels[mVoxels.Count - 1][middle], Color.cyan);

        // Now need to find all boxes that contains any of the boxes I am in.
        // Just brute force this for now.
        // Start at the top,

        // Disregarding the first layer, as that technically contains everything
        for (int i = 1; i < mVoxels.Count - 1; i++)
            for (int j = 0; j < mVoxels[i].Count; j++)
                for (int k = 0; k < selectedBounds[last].Count; k++)
                    if (mVoxels[i][j].Contains(mVoxels[lastLevel][selectedBounds[last][k]].center))
                    {
                        selectedBounds[i - 1].Add(j);
                        break; // Break because we don't want duplicates.
                    }

        for (int i = 0; i < selectedBounds.Count - 1; i++)
            for (int j = 0; j < selectedBounds[i].Count; j++)
                BoundsDrawer.Draw(mVoxels[i + 1][selectedBounds[i][j]], Color.black);

        // Now have list, need to find all game objects belonging contained within the selected bounds.
        long bytes = 0;
        for (int i = 0; i < selectedBounds.Count; i++)
        {
            for (int j = 0; j < selectedBounds[i].Count; j++)
            {
                var layer = mBoundsParents[i + 1][selectedBounds[i][j]];
                var childCount = layer.transform.childCount;
                for (int k = 0; k < childCount; k++)
                {
                    var child = layer.transform.GetChild(k);
                    bytes += SingleMeshStats(child.GetComponent<MeshFilter>().mesh);
                }
            }
        }
        Debug.Log($"Bytes for all 27 cells: {bytes / Math.Pow(10, 6)}");

        mSelectedBounds = selectedBounds;
    }

    private void DrawStreamBounds()
    {
        if (mSelectedBounds.Count == 0)
            return;

        var cellSizes = LevelCellSizes[LevelCellSizes.Count - 1];
        var lastLevel = mVoxels.Count - 1;
        var firstCell = mVoxels[lastLevel][0];
        var lastCell = mVoxels[lastLevel][mVoxels[lastLevel].Count - 1];
        var size = lastCell.max - firstCell.min;
        var columns = (int)(size.x / cellSizes.x);
        var rows = (int)(size.y / cellSizes.y);
        var depth = (int)(size.z / cellSizes.z);

        var last = mSelectedBounds.Count - 1;
        var dimensions = new int3(columns, rows, depth);
        var middle = IndexUtility.CoordsToIdx(BoxCoords, dimensions);

        foreach (var b in mSelectedBounds[last])
            BoundsDrawer.Draw(mVoxels[mVoxels.Count - 1][b], new Color32(255, 0, 255, 255));

        BoundsDrawer.Draw(mVoxels[mVoxels.Count - 1][middle], Color.cyan);

        for (int i = 0; i < mSelectedBounds.Count - 1; i++)
            for (int j = 0; j < mSelectedBounds[i].Count; j++)
                BoundsDrawer.Draw(mVoxels[i + 1][mSelectedBounds[i][j]], Color.black);
    }
    #endregion

    #region StatCalculations
    [Header("Streaming budget in MB/s (Worst Case Read speed / Available Time)")]
    public int StreamBudget = 15; // How much are we reading based on the time we have available in MB/s. 30MB/s * 0.5s = 15.
    struct MemInfo
    {
        public int ID;
        public long TotalSize;
        public Vector3 Pos;
    }

    private IEnumerator CalculateStatsForAllCells()
    {
        // Find all adjacent 26 smallest layer boxes,
        // Find all boxes at the higher levels that contain that box.
        // Go through all the bounds parents (mBoundsParents) for all those boxes.
        // The parents are the ones who reside at the same index as the boxes.

        // First thing first: Formula for mapping 3D coordinate to 1D index.
        // Calculate which box we are working with:

        var cellSizes = LevelCellSizes[LevelCellSizes.Count - 1];
        var lastLevel = mVoxels.Count - 1;
        var firstCell = mVoxels[lastLevel][0];
        var lastCell = mVoxels[lastLevel][mVoxels[lastLevel].Count - 1];
        var size = lastCell.max - firstCell.min;
        var columns = (int)(size.x / cellSizes.x);
        var rows = (int)(size.y / cellSizes.y);
        var depth = (int)(size.z / cellSizes.z);

        long budget = StreamBudget * (long)Math.Pow(10, 6);

        var largestCells = new List<MemInfo>();

        int count = 0;
        for (int d = 0; d < depth; d++)
        {
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    var coords = new int3(c, r, d);
                    var res = CalculateStatsForStreamingCell(depth, rows, columns, coords);
                    if (res > budget)
                    {
                        Debug.LogWarning($"Cell id {IndexUtility.CoordsToIdx(coords, new int3(columns, rows, depth))} exceeds streaming budget {StreamBudget} MB, {res / Math.Pow(10, 6)} MB, excess: {(res - budget) / Math.Pow(10, 6)} MB, pos: {mVoxels[lastLevel][IndexUtility.CoordsToIdx(coords, new int3(columns, rows, depth))].center}");
                        largestCells.Add(new MemInfo { ID = IndexUtility.CoordsToIdx(coords, new int3(columns, rows, depth)), TotalSize = res, Pos = mVoxels[lastLevel][IndexUtility.CoordsToIdx(coords, new int3(columns, rows, depth))].center });
                    }

                    count++;
                    if (count % 100 == 0)
                    {
                        if ((count % 100) == 0)
                        {
                            Debug.Log($"100: At cell: {count}");
                        }
                        yield return null;
                    }
                }
            }
        }

        if (largestCells.Count > 0)
        {
            largestCells.Sort((x, y) =>
            {
                return (int)(x.TotalSize - y.TotalSize);
            });

            var avg = (float)largestCells.Average(x => x.TotalSize);

            var largest = largestCells[largestCells.Count - 1];
            var closest = largestCells[0];
            var avgIdx = 0;
            var currValue = 0.0f;

            for (int i = 0; i < largestCells.Count; i++)
            {
                if (Math.Abs(largestCells[i].TotalSize - avg) < Math.Abs(currValue - avg))
                {
                    avgIdx = i;
                    currValue = largestCells[i].TotalSize;
                }
            }

            Debug.Log($"Closest: {{{closest.ID}, {closest.TotalSize / Math.Pow(10, 6)} MB, {closest.Pos}}}, " +
                      $"Largest: {{{largest.ID}, {largest.TotalSize / Math.Pow(10, 6)} MB, {largest.Pos}}}, " +
                      $"ClosestToAvg: {{{largestCells[avgIdx].ID}, {largestCells[avgIdx].TotalSize / Math.Pow(10, 6)} MB, {largestCells[avgIdx].Pos}}}, avg: {avg / Math.Pow(10, 6)} MB");

        }

        Debug.Log("Finished Calculating All Boxes");
    }

    private struct Empty { };

    private long CalculateStatsForStreamingCell(int depth, int rows, int columns, int3 cellCoord)
    {
        var cellSizes = LevelCellSizes[LevelCellSizes.Count - 1];
        var lastLevel = mVoxels.Count - 1;

        var selectedBounds = new NativeHashMap<Voxel, Empty>(512, Allocator.Temp); // Random number that should be big enough.

        var middle = IndexUtility.CoordsToIdx(cellCoord, new int3(columns, rows, depth));
        if (middle >= 0 && middle < mVoxels[lastLevel].Count)
        {
            for (int z = Math.Max(cellCoord.z - 1, 0); z <= Math.Min(cellCoord.z + 1, depth - 1); z++)
                for (int y = Math.Max(cellCoord.y - 1, 0); y <= Math.Min(cellCoord.y + 1, rows - 1); y++)
                    for (int x = Math.Max(cellCoord.x - 1, 0); x <= Math.Min(cellCoord.x + 1, columns - 1); x++)
                    {
                        var idx = IndexUtility.CoordsToIdx(new int3(x, y, z), new int3(columns, rows, depth));
                        if (idx >= 0 && idx < mVoxels[lastLevel].Count)
                            selectedBounds.TryAdd(new Voxel { Index = idx, Layer = lastLevel }, new Empty());


                        for (int i = mVoxels.Count - 2; i > 0; i--)
                        {
                            var firstCell = mVoxels[i][0];
                            var lastCell = mVoxels[i][mVoxels[i].Count - 1];
                            var size = lastCell.max - firstCell.min;
                            var outercolumns = (int)(size.x / LevelCellSizes[i].x);
                            var outerrows = (int)(size.y / LevelCellSizes[i].y);
                            var outerdepth = (int)(size.z / LevelCellSizes[i].z);
                            var outerDimensions = new int3(outercolumns, outerrows, outerdepth);

                            selectedBounds.TryAdd(new Voxel { Layer = i, Index = IndexUtility.CoordsToIdx(IndexUtility.CoordsToOuterGrid(new int3(x, y, z), new int3(columns, rows, depth), outerDimensions), outerDimensions) }, new Empty());
                        }
                    }
        }

        // Now have list, need to find all game objects belonging contained within the selected bounds.
        long bytes = 0;
        var keys = selectedBounds.GetKeyArray(Allocator.Temp);
        for (int i = 0; i < keys.Length; i++)
        {
            var layerIDX = keys[i].Layer;
            var index = keys[i].Index;
            try
            {
                var layer = mBoundsParents[keys[i].Layer][keys[i].Index];
                var childCount = layer.transform.childCount;
                for (int k = 0; k < childCount; k++)
                {
                    var child = layer.transform.GetChild(k);
                    bytes += SingleMeshStats(child.GetComponent<MeshFilter>().mesh);
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                Debug.Log($"Values out of range: {layerIDX} vs {mBoundsParents.Count}, {index} vs {mBoundsParents[keys[i].Layer].Count} vs {index}");
            }
        }

        keys.Dispose();
        selectedBounds.Dispose();

        return bytes;
    }

    // Returns the total estimate space it would take to store the mesh in bytes.
    private long SingleMeshStats(Mesh mesh)
    {
        var positions = 3;
        var colors = 4;
        var normals = 3;
        var uvcoords = 0; // Not supplying UV coords

        var vertexCount = mesh.vertexCount;
        var indexCount = mesh.GetIndexCount(0); // assuming we are only using 1 material, see submesh
        return vertexCount * (colors * sizeof(byte) + (positions + normals + uvcoords) * sizeof(float)) + indexCount * sizeof(int);
    }
    #endregion

    #region ColliderCreation
    JobHandle mCreatorJobHandle;
    NativeArray<BlobAssetReference<Unity.Physics.Collider>> mColliders;
    NativeArray<ulong> mVerticesGCHandles;
    NativeArray<ulong> mIndicesGCHandles;

    unsafe struct CreateColliderJob : IJob
    {
        [ReadOnly, NativeDisableUnsafePtrRestriction]
        public float3* Vertices;

        [ReadOnly, NativeDisableUnsafePtrRestriction]
        public int* Indices;

        [WriteOnly, NativeDisableContainerSafetyRestriction]
        public NativeArray<BlobAssetReference<Unity.Physics.Collider>> Colliders;

        public int VertexCount;
        public int IndexCount;

        public int Idx;

        public void Execute()
        {
            var coll = MeshColliderHelper.Create(Vertices, Indices, VertexCount, IndexCount);
            Colliders[Idx] = coll;
        }
    }

    private IEnumerator ConvertMeshesToColliderBlobs()
    {
        int count = 0;
        StartColliderJobs();

        while (!mCreatorJobHandle.IsCompleted)
        {
            if (count++ % 100 == 0)
            {
                Debug.Log("Waiting");
            }
            yield return new WaitForSeconds(0.5f);
        }

        mCreatorJobHandle.Complete();
        for (int i = 0; i < mIndicesGCHandles.Length; i++)
        {
            UnsafeUtility.ReleaseGCObject(mIndicesGCHandles[i]);
            UnsafeUtility.ReleaseGCObject(mVerticesGCHandles[i]);
        }
        mIndicesGCHandles.Dispose();
        mVerticesGCHandles.Dispose();
        Debug.Log("Meshes Converted, Can now write to file");

        var debugDisplayData = World.Active.EntityManager.CreateEntity(ComponentType.ReadOnly<Unity.Physics.Authoring.PhysicsDebugDisplayData>());
        var debugDisplayConfig = new Unity.Physics.Authoring.PhysicsDebugDisplayData
        {
            DrawColliders = 1, //1,
            DrawBroadphase = 0,
            DrawColliderAabbs = 0,
            DrawColliderEdges = 1,
            DrawCollisionEvents = 0,
            DrawContacts = 0,
            DrawJoints = 0,
            DrawMassProperties = 0,
            DrawTriggerEvents = 0,
        };
        //World.Active.GetOrCreateSystem<Unity.Physics.Authoring.DisplayBodyColliders>().SetSingleton(debugDisplayConfig);

        for (int i = 0; i < mColliders.Length; i++)
        {
            var e = World.Active.EntityManager.CreateEntity();
            World.Active.EntityManager.AddComponentData(e, new Unity.Transforms.Translation { Value = float3.zero });
            World.Active.EntityManager.AddComponentData(e, new Unity.Transforms.Rotation { Value = quaternion.identity });
            World.Active.EntityManager.AddComponentData(e, new Unity.Physics.PhysicsCollider { Value = mColliders[i] });
            World.Active.EntityManager.AddComponentData(e, new Unity.Transforms.Frozen { });

        }

        yield return null;
    }

    private unsafe void StartColliderJobs()
    {
        var goCount = mGameObjects.Count;
        mColliders = new NativeArray<BlobAssetReference<Unity.Physics.Collider>>(goCount, Allocator.Persistent);
        mIndicesGCHandles = new NativeArray<ulong>(goCount, Allocator.Persistent);
        mVerticesGCHandles = new NativeArray<ulong>(goCount, Allocator.Persistent);
        var verticesList = new List<List<Vector3>>();
        var indicesList = new List<List<int>>();
        for (int i = 0; i < goCount; i++)
        {
            verticesList.Add(new List<Vector3>());
            indicesList.Add(new List<int>());
        }

        var handles = new NativeArray<JobHandle>(goCount, Allocator.Temp);

        int goNumber = 0;
        for (int i = 0; i < mLevelParents.Count; i++)
        {
            var voxTrans = mLevelParents[i].transform;
            var voxCount = voxTrans.childCount;
            for (int j = 0; j < voxCount; j++)
            {
                var voxel = voxTrans.GetChild(j);
                var voxChildren = voxel.childCount;
                for (int k = 0; k < voxChildren; k++, goNumber++)
                {
                    var child = voxel.GetChild(k);
                    var mesh = child.GetComponent<MeshFilter>().mesh;

                    mesh.GetVertices(verticesList[goNumber]);
                    mesh.GetTriangles(indicesList[goNumber], 0);

                    for (int x = 0; x < verticesList[goNumber].Count; x++)
                    {
                        verticesList[goNumber][x] = child.TransformPoint(verticesList[goNumber][x]);
                    }

                    var verticesArr = NoAllocHelpers.ExtractArrayFromListT(verticesList[goNumber]);
                    var indicesArr = NoAllocHelpers.ExtractArrayFromListT(indicesList[goNumber]);
                    var verticesPtr = (float3*)UnsafeUtility.PinGCArrayAndGetDataAddress(verticesArr, out ulong verticesGC);
                    var indicesPtr = (int*)UnsafeUtility.PinGCArrayAndGetDataAddress(indicesArr, out ulong indicesGC);
                    mVerticesGCHandles[goNumber] = verticesGC;
                    mIndicesGCHandles[goNumber] = indicesGC;

                    var job = new CreateColliderJob
                    {
                        Vertices = verticesPtr,
                        Indices = indicesPtr,
                        VertexCount = verticesList[goNumber].Count,
                        IndexCount = indicesList[goNumber].Count,
                        Idx = goNumber,
                        Colliders = mColliders,
                    };

                    handles[goNumber] = job.Schedule();
                }
            }
        }

        mCreatorJobHandle = JobHandle.CombineDependencies(handles);
        //mCreatorJobHandle.Complete();
        handles.Dispose();
        Debug.Log("Jobs started");
    }
    #endregion

    #region ObjectTriangulation

    [SerializeField]
    private string IFCFoldername = "Westside2";
    private string mIFCFolderPath;

    [SerializeField]
    private string IFCSchemaName = @"IFC2X3_TC1";

    public Handle mModel = 0;

    public Material TMPMaterial;
    // Int is to be used for index in mGameObjects array
    public Dictionary<string, int> mExistingNames = new Dictionary<string, int>();



    // Massively ugly hack.
    // Unity crashes when I try to close the model using IfcEngine.x64.sdaiCloseModel
    // When the model is opened, it seems like the file is locked or something.
    // If we don't close the model and start a new application right away IfcEngine cannot open the file.
    // However, it seems like windows unlocks the file by itself at some point if the application holding it has quit.
    // Therefore, the "solution" to this problem, is simply to give it a couple of attempts.
    // The wait on i % 10 is there to give windows more time.
    //
    // -- Update
    // Originally I thought it crashed because I couldn't close the file. Turns out it probably crashed because
    // the DLL is x64, but this script thought it was x86 (the #if _WIN64 didn't work).
    // Therefore, I have now added closing back to when the thing is destroyed.
    // But I still got the issues of models not loading on startup, so decided to keep this "hack".
    //
    // Usually it only fails 1-2 times before succeeding.
    private IEnumerator LoadFile(string filepath)
    {
        for (int i = 0; i < 10000; i++)
        {
            if (i % 10 == 0)
            {
                Debug.Log("Attempt failed, waiting to retry");
                yield return new WaitForSeconds(0.5f);
            }

            mModel = IfcEngine.x64.sdaiOpenModelBNUnicode(0, System.Text.Encoding.Unicode.GetBytes(filepath),
                                                             System.Text.Encoding.Unicode.GetBytes(IFCSchemaName));

            if (mModel != 0)
            {
                Debug.Log("Model loaded successfully!");
                break;
            }
        }

        Debug.AssertFormat(mModel != 0, "Couldn't load file");
        SetFormat(mModel);

        yield return CreateObjectsFromModel(filepath);
    }

    private IEnumerator CreateObjectsFromModel(string filepath)
    {

#if false // ALL IFC
        var ifcType = new string[]
        {
            "IfcProduct",
            "IfcAnnotation",
            "IfcElement",
            "IfcBuildingElement",
            "IfcBeam",
            "IfcBeamStandardCase",
            "IfcBuildingElementProxy",
            "IfcChimney",
            "IfcColumn",
            "IfcColumnStandardCase",
            "IfcCovering",
            "IfcCurtainWall",
            "IfcDoor",
            "IfcDoorStandardCase",
            "IfcFooting",
            "IfcMember",
            "IfcMemberStandardCase",
            "IfcPile",
            "IfcPlate",
            "IfcPlateStandardCase",
            "IfcRailing",
            "IfcRamp",
            "IfcRampFlight",
            "IfcRoof",
            "IfcShadingDevice",
            "IfcSlab",
            "IfcSlabElementedCase",
            "IfcSlabStandardCase",
            "IfcStair",
            "IfcStairFlight",
            "IfcWall",
            "IfcWallElementedCase",
            "IfcWallStandardCase",
            "IfcWindow",
            "IfcWindowStandardCase",
            "IfcCivilElement",
            "IfcDistributionElement",
            "IfcDistributionControlElement",
            "IfcActuator",
            "IfcAlarm",
            "IfcController",
            "IfcFlowInstrument",
            "IfcProtectiveDeviceTrippingUnit",
            "IfcSensor",
            "IfcUnitaryControlElement",
            "IfcDistributionFlowElement",
            "IfcDistributionChamberElement",
            "IfcEnergyConversionDevice",
            "IfcAirToAirHeatRecovery",
            "IfcBoiler",
            "IfcBurner",
            "IfcChiller",
            "IfcCoil",
            "IfcCondenser",
            "IfcCooledBeam",
            "IfcCoolingTower",
            "IfcElectricGenerator",
            "IfcElectricMotor",
            "IfcEngine",
            "IfcEvaporativeCooler",
            "IfcEvaporator",
            "IfcHeatExchanger",
            "IfcHumidifier",
            "IfcMotorConnection",
            "IfcSolarDevice",
            "IfcTransformer",
            "IfcTubeBundle",
            "IfcUnitaryEquipment",
            "IfcFlowController",
            "IfcAirTerminalBox",
            "IfcDamper",
            "IfcElectricDistributionBoard",
            "IfcElectricTimeControl",
            "IfcFlowMeter",
            "IfcProtectiveDevice",
            "IfcSwitchingDevice",
            "IfcValve",
            "IfcFlowFitting",
            "IfcCableCarrierFitting",
            "IfcCableFitting",
            "IfcDuctFitting",
            "IfcJunctionBox",
            "IfcPipeFitting",
            "IfcFlowMovingDevice",
            "IfcCompressor",
            "IfcFan",
            "IfcPump",
            "IfcFlowSegment",
            "IfcCableCarrierSegment",
            "IfcCableSegment",
            "IfcDuctSegment",
            "IfcPipeSegment",
            "IfcFlowStorageDevice",
            "IfcElectricFlowStorageDevice",
            "IfcTank",
            "IfcFlowTerminal",
            "IfcAirTerminal",
            "IfcAudioVisualAppliance",
            "IfcCommunicationsAppliance",
            "IfcElectricAppliance",
            "IfcFireSuppressionTerminal",
            "IfcLamp",
            "IfcLightFixture",
            "IfcMedicalDevice",
            "IfcOutlet",
            "IfcSanitaryTerminal",
            "IfcSpaceHeater",
            "IfcStackTerminal",
            "IfcWasteTerminal",
            "IfcFlowTreatmentDevice",
            "IfcDuctSilencer",
            "IfcFilter",
            "IfcInterceptor",
            "IfcElementAssembly",
            "IfcElementComponent",
            "IfcBuildingElementPart",
            "IfcDiscreteAccessory",
            "IfcFastener",
            "IfcMechanicalFastener",
            "IfcReinforcingElement",
            "IfcReinforcingBar",
            "IfcReinforcingMesh",
            "IfcTendon",
            "IfcTendonAnchor",
            "IfcVibrationIsolator",
            "IfcFeatureElement",
            "IfcFeatureElementAddition",
            "IfcProjectionElement",
            "IfcFeatureElementSubtraction",
            "IfcOpeningElement",
            "IfcOpeningStandardCase",
            "IfcVoidingFeature",
            "IfcSurfaceFeature",
            "IfcFurnishingElement",
            "IfcFurniture",
            "IfcSystemFurnitureElement",
            "IfcGeographicElement",
            "IfcTransportElement",
            "IfcVirtualElement",
            "IfcPort",
            "IfcDistributionPort",
            "IfcPositioningElement",
            "IfcGrid",
            "IfcLinearPositioningElement",
            "IfcAlignment",
            "IfcProxy",
            "IfcSpatialElement",
            "IfcExternalSpatialStructureElement",
            "IfcExternalSpatialElement",
            "IfcSpatialStructureElement",
            "IfcBuilding",
            "IfcBuildingStorey",
            "IfcSite",
            "IfcSpace",
            "IfcSpatialZone",
            "IfcStructuralActivity",
            "IfcStructuralAction",
            "IfcStructuralCurveAction",
            "IfcStructuralLinearAction",
            "IfcStructuralPointAction",
            "IfcStructuralSurfaceAction",
            "IfcStructuralPlanarAction",
            "IfcStructuralReaction",
            "IfcStructuralCurveReaction",
            "IfcStructuralPointReaction",
            "IfcStructuralSurfaceReaction",
            "IfcStructuralItem",
            "IfcStructuralConnection",
            "IfcStructuralCurveConnection",
            "IfcStructuralPointConnection",
            "IfcStructuralSurfaceConnection",
            "IfcStructuralMember",
            "IfcStructuralCurveMember",
            "IfcStructuralCurveMemberVarying",
            "IfcStructuralSurfaceMember",
            "IfcStructuralSurfaceMemberVarying",

        };
#else // Selected IFC
        var ifcType = new string[]
        {
            "IfcProduct",
            "IfcAnnotation",
            "IfcElement",
            "IfcBuildingElement",
            "IfcBeam",
            "IfcBeamStandardCase",
            "IfcBuildingElementProxy",
            "IfcChimney",
            "IfcColumn",
            "IfcColumnStandardCase",
            //"IfcCovering", // UNsure
            "IfcCurtainWall",
            "IfcDoor",
            "IfcDoorStandardCase",
            "IfcFooting",
            "IfcMember",
            "IfcMemberStandardCase",
            "IfcPile",
            "IfcPlate",
            "IfcPlateStandardCase",
            "IfcRailing",
            "IfcRamp",
            "IfcRampFlight",
            "IfcRoof",
            "IfcShadingDevice",
            "IfcSlab",
            "IfcSlabElementedCase",
            "IfcSlabStandardCase",
            "IfcStair",
            "IfcStairFlight",
            "IfcWall",
            "IfcWallElementedCase",
            "IfcWallStandardCase",
            "IfcWindow",
            "IfcWindowStandardCase",
            "IfcCivilElement",
            "IfcDistributionElement",
            "IfcDistributionControlElement",
            "IfcActuator",
            "IfcAlarm",
            "IfcController",
            "IfcFlowInstrument",
            "IfcProtectiveDeviceTrippingUnit",
            "IfcSensor",
            "IfcUnitaryControlElement",
            "IfcDistributionFlowElement",
            "IfcDistributionChamberElement",
            "IfcEnergyConversionDevice",
            "IfcAirToAirHeatRecovery",
            "IfcBoiler",
            "IfcBurner",
            "IfcChiller",
            "IfcCoil",
            "IfcCondenser",
            "IfcCooledBeam",
            "IfcCoolingTower",
            "IfcElectricGenerator",
            "IfcElectricMotor",
            "IfcEngine",
            "IfcEvaporativeCooler",
            "IfcEvaporator",
            "IfcHeatExchanger",
            "IfcHumidifier",
            "IfcMotorConnection",
            "IfcSolarDevice",
            "IfcTransformer",
            "IfcTubeBundle",
            "IfcUnitaryEquipment",
            "IfcFlowController",
            "IfcAirTerminalBox",
            "IfcDamper",
            "IfcElectricDistributionBoard",
            "IfcElectricTimeControl",
            "IfcFlowMeter",
            "IfcProtectiveDevice",
            "IfcSwitchingDevice",
            "IfcValve",
            "IfcFlowFitting",
            "IfcCableCarrierFitting",
            "IfcCableFitting",
            "IfcDuctFitting",
            "IfcJunctionBox",
            "IfcPipeFitting",
            "IfcFlowMovingDevice",
            "IfcCompressor",
            "IfcFan",
            "IfcPump",
            "IfcFlowSegment",
            "IfcCableCarrierSegment",
            "IfcCableSegment",
            "IfcDuctSegment",
            "IfcPipeSegment",
            "IfcFlowStorageDevice",
            "IfcElectricFlowStorageDevice",
            "IfcTank",
            "IfcFlowTerminal",
            "IfcAirTerminal",
            "IfcAudioVisualAppliance",
            "IfcCommunicationsAppliance",
            "IfcElectricAppliance",
            "IfcFireSuppressionTerminal",
            "IfcLamp",
            "IfcLightFixture",
            "IfcMedicalDevice",
            "IfcOutlet",
            "IfcSanitaryTerminal",
            "IfcSpaceHeater",
            "IfcStackTerminal",
            "IfcWasteTerminal",
            "IfcFlowTreatmentDevice",
            "IfcDuctSilencer",
            "IfcFilter",
            "IfcInterceptor",
            "IfcElementAssembly",
            "IfcElementComponent",
            "IfcBuildingElementPart",
            "IfcDiscreteAccessory",
            "IfcFastener",
            "IfcMechanicalFastener",
            "IfcReinforcingElement",
            "IfcReinforcingBar",
            "IfcReinforcingMesh",
            "IfcTendon",
            "IfcTendonAnchor",
            "IfcVibrationIsolator",
            "IfcFeatureElement",
            "IfcFeatureElementAddition",
            "IfcProjectionElement",
            "IfcFeatureElementSubtraction",
            //"IfcOpeningElement",
            //"IfcOpeningStandardCase",
            "IfcVoidingFeature",
            "IfcSurfaceFeature",
            "IfcFurnishingElement",
            "IfcFurniture",
            "IfcSystemFurnitureElement",
            "IfcGeographicElement",
            "IfcTransportElement",
            "IfcVirtualElement",
            "IfcPort",
            "IfcDistributionPort",
            "IfcPositioningElement",
            "IfcGrid",
            "IfcLinearPositioningElement",
            "IfcAlignment",
            "IfcProxy",
            "IfcSpatialElement",
            "IfcExternalSpatialStructureElement",
            "IfcExternalSpatialElement",
            "IfcSpatialStructureElement",
            "IfcBuilding",
            "IfcBuildingStorey",
            "IfcSite",
            //"IfcSpace",
            "IfcSpatialZone",
            "IfcStructuralActivity",
            "IfcStructuralAction",
            "IfcStructuralCurveAction",
            "IfcStructuralLinearAction",
            "IfcStructuralPointAction",
            "IfcStructuralSurfaceAction",
            "IfcStructuralPlanarAction",
            "IfcStructuralReaction",
            "IfcStructuralCurveReaction",
            "IfcStructuralPointReaction",
            "IfcStructuralSurfaceReaction",
            "IfcStructuralItem",
            "IfcStructuralConnection",
            "IfcStructuralCurveConnection",
            "IfcStructuralPointConnection",
            "IfcStructuralSurfaceConnection",
            "IfcStructuralMember",
            "IfcStructuralCurveMember",
            "IfcStructuralCurveMemberVarying",
            "IfcStructuralSurfaceMember",
            "IfcStructuralSurfaceMemberVarying",

        };
#endif

        IfcEngine.x64.setVertexOffset(mModel, -10021.29, -125633.9, -166011.2);

        int count = 0;
        int pauser = 0;
        for (int t = 0; t < ifcType.Length; t++)
        {
            var allObjects = IfcEngine.x64.sdaiGetEntityExtentBN(mModel, ifcType[t]);
            if (allObjects == 0)
                continue;

            var allObjectsCount = IfcEngine.x64.sdaiGetMemberCount(allObjects);
            Debug.Log($"{filepath}: {ifcType[t]} ({t} of  {ifcType.Length}): Number of objects {allObjectsCount}");

            for (int i = 0; i < allObjectsCount; i++)
            {
                IfcEngine.x64.engiGetAggrElement(allObjects, i, IfcEngine.x64.sdaiINSTANCE, out Handle instance);
                Debug.Assert(instance != 0, $"engiGetAggrElement res: {instance}");

                var res = IfcEngine.x64.sdaiGetAttrBN(instance, "GlobalId", IfcEngine.x64.sdaiUNICODE, out IntPtr name);
                var strname = System.Runtime.InteropServices.Marshal.PtrToStringUni(name);

                if (mExistingNames.ContainsKey(strname))
                {
                    continue;
                }

                mExistingNames.Add(strname, mGameObjects.Count);

                I64 verticesCount = 0;
                I64 indicesCount = 0;
                IfcEngine.x64.initializeModellingInstance(mModel, ref verticesCount, ref indicesCount, 0, instance);

                if (verticesCount <= 0 || indicesCount <= 0)
                    continue;

                var valuesPerVertex = 7;
                var vertices = new float[verticesCount * valuesPerVertex];
                var indices = new Int32[indicesCount];
                IfcEngine.x64.finalizeModelling(mModel, vertices, indices, 0);

                I64 startVS = 0;
                I64 startIDX = 0;
                I64 primitiveCount = 0;
                IfcEngine.x64.getInstanceInModelling(mModel, instance, 0, ref startVS, ref startIDX, ref primitiveCount);

                var positions = new Vector3[verticesCount];
                var normals = new Vector3[verticesCount];
                var colors = new Color32[verticesCount];

                Mesh mesh = new Mesh();
                for (int j = 0; j < verticesCount; j++)
                {
                    // Scaling down to Unity unitys. (This does not work for IFC Files with specified scale)
                    positions[j].x = -vertices[j * valuesPerVertex + 0] * 0.001f;
                    positions[j].y = vertices[j * valuesPerVertex + 1] * 0.001f;
                    positions[j].z = vertices[j * valuesPerVertex + 2] * 0.001f;
                    normals[j].x = -vertices[j * valuesPerVertex + 3];
                    normals[j].y = vertices[j * valuesPerVertex + 4];
                    normals[j].z = vertices[j * valuesPerVertex + 5];

                    var byteArr = BitConverter.GetBytes(vertices[j * valuesPerVertex + 6]);
                    colors[j] = new Color32(byteArr[3], byteArr[2], byteArr[1], byteArr[0]);
                }

                mesh.vertices = positions;
                mesh.normals = normals;
                mesh.colors32 = colors;
                mesh.SetTriangles(indices, 0);


                var go = new GameObject(strname);

                var filter = go.AddComponent<MeshFilter>();
                filter.mesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.material = TMPMaterial;

                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                // Rotating -90 deg to get into Unity Coordinate system.
                go.transform.localRotation = Quaternion.Euler(-90.0f, 0.0f, 0.0f);

                mGameObjects.Add(go);

                if (pauser++ % 5000 == 0)
                {
                    pauser = 0;

                    // Used to end up with weird crash if I weren't cleaning up my memory.
                    yield return null;
                }
            }
            IfcEngine.x64.cleanMemory(mModel, 0);
        }

        if (count != 0)
            Debug.Log($"Success, {count} walls had geometry");

        IfcEngine.x64.sdaiCloseModel(mModel);
        mModel = 0;

        yield return null;
    }

    private void SetFormat(Handle model)
    {
        Int64 setting = 0;
        Int64 mask = 0;

        mask += IfcEngine.x64.flagbit2; //    PRECISION (32/64 bit)
        mask += IfcEngine.x64.flagbit3; //	INDEX ARRAY (32/64 bit)
        mask += IfcEngine.x64.flagbit5; //    NORMALS
        mask += IfcEngine.x64.flagbit8; //    TRIANGLES
        mask += IfcEngine.x64.flagbit9; //    LINES
        mask += IfcEngine.x64.flagbit10; //    POINTS
        mask += IfcEngine.x64.flagbit12; //    WIREFRAME
        mask += IfcEngine.x64.flagbit24; //	AMBIENT
        mask += IfcEngine.x64.flagbit25; //	DIFFUSE
        mask += IfcEngine.x64.flagbit26; //	EMISSIVE
        mask += IfcEngine.x64.flagbit27; //	SPECULAR

        setting += 0; //    SINGLE PRECISION (float)
        setting += 0; //    32 BIT INDEX ARRAY (Int32)
        setting += IfcEngine.x64.flagbit5; //    NORMALS ON
        setting += IfcEngine.x64.flagbit8; //    TRIANGLES ON
        setting += 0; //    LINES ON
        setting += 0; //    POINTS ON
        setting += 0; //    WIREFRAME ON
        setting += IfcEngine.x64.flagbit24; //	AMBIENT
        setting += 0; //IfcEngine.x64.flagbit25; //	DIFFUSE
        setting += 0; //IfcEngine.x64.flagbit26; //	EMISSIVE
        setting += 0; //	SPECULAR

        IfcEngine.x64.setFormat(model, setting, mask);
    }

    private void OnDestroy()
    {
        if (mModel != 0)
            IfcEngine.x64.sdaiCloseModel(mModel);
    }
    #endregion

    public string Filename = "tmp.str";
    [ContextMenu("Write to file")]
    private unsafe void WriteToFile()
    {
        // We aren't able to resize a memory map, so I just allocate a non-persistant memory map with lots of memory
        // ensuring that we have at least enough, then write to that and copy over to a persistant memory map.
        // A better solution would probably be to keep track of how much memory we would need while converting the model.
        var maxSize = (long)16 * (1 << 30); // (1 << 30) = 1 GB.
        //var maxSize = (long)120 * (1 << 30); // (1 << 30) = 1 GB.

        var tmpFilepath = @"Assets\..\StreamingFiles\scratchpad.tmp";

        using (var tmpFile = MemoryMappedFile.CreateFromFile(tmpFilepath, FileMode.Create, "tmp", maxSize))
        {
            long ptr = 0; // Technically index, but will be used to iterate over memory positions, so pointer.

            using (var map = tmpFile.CreateViewAccessor(0, maxSize))
            {
                long headerSize = 256;
                ptr = headerSize;



                // Write dimensions for all layers
                long sizesSection = ptr;
                for (int i = 0; i < LevelCellSizes.Count; i++)
                {
                    var cellSize = LevelCellSizes[i];
                    map.Write(ptr, ref cellSize);
                    ptr += sizeof(Vector3);
                }

                long dimensionsSection = ptr;
                for (int i = 0; i < LevelCellSizes.Count; i++)
                {
                    var cellSize = LevelCellSizes[i];
                    var firstCell = mVoxels[i][0];
                    var lastCell = mVoxels[i][Math.Max(mVoxels[i].Count - 1, 0)];
                    var size = lastCell.max - firstCell.min;
                    var columns = (int)(size.x / cellSize.x);
                    var rows = (int)(size.y / cellSize.y);
                    var depth = (int)(size.z / cellSize.z);

                    var dimensions = new Vector3Int(columns, rows, depth);
                    map.Write(ptr, ref dimensions);

                    ptr += sizeof(Vector3Int);
                }

                // Write all Bounds for all layers.
                long boundsSection = ptr;
                for (int i = 0; i < mVoxels.Count; i++)
                {
                    for (int j = 0; j < mVoxels[i].Count; j++)
                    {
                        var bounds = mVoxels[i][j].center;
                        map.Write(ptr, ref bounds);
                        ptr += sizeof(Vector3);
                    }
                }

                // Write start index of objects within bounds
                long objectsInBoundsSection = ptr;
                int gameObjectCount = 0;
                for (int i = 0; i < mLevelParents.Count; i++)
                {
                    var voxTrans = mLevelParents[i].transform;
                    var voxCount = voxTrans.childCount;
                    for (int j = 0; j < voxCount; j++)
                    {
                        var voxel = voxTrans.GetChild(j);
                        var voxChildren = voxel.childCount;
                        map.Write(ptr, gameObjectCount);
                        ptr += sizeof(int);
                        map.Write(ptr, gameObjectCount + voxChildren);
                        ptr += sizeof(int);
                        gameObjectCount += voxChildren;
                    }
                }

                int vertexCount = 0;
                int indexCount = 0;
                var vertices = new List<Vector3>();
                var normals = new List<Vector3>();
                var colors = new List<Color32>();
                var indices = new List<int>();

                Debug.Assert(gameObjectCount == mGameObjects.Count);

                long verticesInfoSection = ptr;
                long verticesSection = ptr + (long)gameObjectCount * sizeof(int) * 4;

                for (int i = 0; i < mLevelParents.Count; i++)
                {
                    var voxTrans = mLevelParents[i].transform;
                    var voxCount = voxTrans.childCount;
                    for (int j = 0; j < voxCount; j++)
                    {
                        var voxel = voxTrans.GetChild(j);
                        var voxChildren = voxel.childCount;
                        for (int k = 0; k < voxChildren; k++)
                        {
                            var child = voxel.GetChild(k);
                            var mesh = child.GetComponent<MeshFilter>().mesh;

                            var vertexStart = vertexCount;
                            var indexStart = indexCount;

                            // Write Vertices
                            var v = mesh.vertices;
                            var n = mesh.normals;
                            for (int l = 0; l < v.Length; l++)
                            {
                                v[l] = child.TransformPoint(v[l]);
                                n[l] = child.TransformDirection(n[l]);
                            }

                            vertices.AddRange(v);
                            normals.AddRange(n);
                            Debug.Assert(mesh.colors32.Length == mesh.vertexCount);
                            colors.AddRange(mesh.colors32);
                            indices.AddRange(mesh.triangles);

                            var triangles = mesh.triangles;

                            // Write VerticesInfo
                            map.Write(ptr, vertexStart);
                            ptr += sizeof(int);
                            map.Write(ptr, vertexStart + mesh.vertexCount);
                            ptr += sizeof(int);
                            map.Write(ptr, indexStart);
                            ptr += sizeof(int);
                            map.Write(ptr, indexStart + triangles.Length);
                            ptr += sizeof(int);

                            vertexCount += mesh.vertexCount;
                            indexCount += triangles.Length;
                        }
                    }
                }

                map.WriteArray(verticesSection, vertices.ToArray(), 0, vertices.Count);
                long normalsSection = verticesSection + vertices.Count * sizeof(Vector3);
                map.WriteArray(normalsSection, normals.ToArray(), 0, normals.Count);
                long colorSection = normalsSection + normals.Count * sizeof(Vector3);
                map.WriteArray(colorSection, colors.ToArray(), 0, colors.Count);
                long indicesSection = colorSection + colors.Count * sizeof(Color32);
                map.WriteArray(indicesSection, indices.ToArray(), 0, indices.Count);

                // Write out names
                ptr = indicesSection + indices.Count * sizeof(int);
                long namesSection = ptr;
                for (int i = 0; i < mLevelParents.Count; i++)
                {
                    var voxTrans = mLevelParents[i].transform;
                    var voxCount = voxTrans.childCount;
                    for (int j = 0; j < voxCount; j++)
                    {
                        var voxel = voxTrans.GetChild(j);
                        var voxChildren = voxel.childCount;
                        for (int k = 0; k < voxChildren; k++)
                        {
                            var name = voxel.GetChild(k).name;
                            byte[] str = System.Text.Encoding.ASCII.GetBytes(name.ToCharArray());
                            Debug.Assert(str.Length == 22);
                            map.WriteArray(ptr, str, 0, str.Length);
                            ptr += sizeof(byte) * 22;
                        }
                    }
                }

                // Write out BlobInfo
                var blobInfoSection = ptr;
                var blobInfoPtr = ptr;
                var blobSection = ptr + (long)gameObjectCount * sizeof(int) * 2;
                var blobPtr = blobSection;

                //int blobByteCount = 0;
                int blobCount = 0;
                Debug.Log($"BlobInfosBegin {blobInfoSection}");
                Debug.Log($"BlobSection {blobSection}");

                int byteCount = 0;
                for (int i = 0; i < mLevelParents.Count; i++)
                {
                    var voxTrans = mLevelParents[i].transform;
                    var voxCount = voxTrans.childCount;
                    for (int j = 0; j < voxCount; j++)
                    {
                        var voxel = voxTrans.GetChild(j);
                        var voxChildren = voxel.childCount;
                        for (int k = 0; k < voxChildren; k++, blobCount++)
                        {
                            var writer = new Unity.Entities.Serialization.MemoryBinaryWriter();

                            var begin = byteCount;
                            map.Write(blobInfoPtr, begin);
                            blobInfoPtr += sizeof(int);

                            writer.Write(mColliders[blobCount]);
                            var end = byteCount + writer.Length;
                            map.Write(blobInfoPtr, end);
                            blobInfoPtr += sizeof(int);

                            var basePtr = (byte*)0;
                            map.SafeMemoryMappedViewHandle.AcquirePointer(ref basePtr);
                            UnsafeUtility.MemCpy(basePtr + blobSection + byteCount, writer.Data, writer.Length);
                            map.SafeMemoryMappedViewHandle.ReleasePointer();
                            byteCount += writer.Length;
                            writer.Dispose();
                        }
                    }
                }


                ptr = blobSection + byteCount;

                Debug.Log($"First BlobInfoSection, begin: {map.ReadInt32(blobInfoSection)}, end {map.ReadInt32(blobInfoSection + sizeof(int))}");

                // Write out header info.
                long headerPtr = 128;
                map.Write(headerPtr + sizeof(int) * 0, LevelCellSizes.Count);
                headerPtr += sizeof(int);
                map.Write(headerPtr + sizeof(long) * 0, sizesSection);
                map.Write(headerPtr + sizeof(long) * 1, dimensionsSection);
                map.Write(headerPtr + sizeof(long) * 2, boundsSection);
                map.Write(headerPtr + sizeof(long) * 3, objectsInBoundsSection);
                map.Write(headerPtr + sizeof(long) * 4, verticesInfoSection);
                map.Write(headerPtr + sizeof(long) * 5, verticesSection);
                map.Write(headerPtr + sizeof(long) * 6, normalsSection);
                map.Write(headerPtr + sizeof(long) * 7, colorSection);
                map.Write(headerPtr + sizeof(long) * 8, indicesSection);
                map.Write(headerPtr + sizeof(long) * 9, namesSection);
                map.Write(headerPtr + sizeof(long) * 10, blobInfoSection);
                map.Write(headerPtr + sizeof(long) * 11, blobSection);
            }

            // Copy to new file.
            using (var file = MemoryMappedFile.CreateFromFile(@"Assets\..\StreamingFiles\" + Filename, FileMode.Create, "file", ptr))
            {
                using (var tmp = tmpFile.CreateViewStream(0, ptr))
                {
                    using (var stream = file.CreateViewStream(0, ptr))
                    {
                        tmp.CopyTo(stream);
                    }
                }
            }
        }

        System.IO.File.Delete(tmpFilepath);

        mColliders.Dispose();
        Debug.Log("Finished writing to file");
    }
}
