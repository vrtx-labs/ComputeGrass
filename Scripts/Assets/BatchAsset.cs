using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using VRTX.ComputeGrass.Extensions;

namespace VRTX.ComputeGrass.Assets
{
    public class BatchAsset : ScriptableObject
    {
        [SerializeField]
        private DefinitionAsset definition = null;
        [SerializeField]
        private ParametersAsset parameters = null;
        [SerializeField]
        private List<Mesh> meshes = new List<Mesh>();


        public DefinitionAsset Definition => definition;
        public ParametersAsset Parameters => parameters;
        public List<Mesh> Meshes => meshes;
        public bool IsValid => definition != null && parameters != null && meshes != null && meshes.Count > 0;


        public static Vector3 GetEstimatedMemoryConsumption(Mesh mesh, Parameters parameters, Shaders.ByteUnits unit = Shaders.ByteUnits.Base, bool useDecimal = false)
        {
            int numSourceVertices = mesh.vertexCount;
            // Each segment has two points
            int maxBladesPerVertex = Mathf.Max(1, parameters.BladesPerVertex);
            int maxSegmentsPerBlade = Mathf.Max(1, parameters.SegmentsPerBlade);
            int maxBladeTriangles = maxBladesPerVertex * ((maxSegmentsPerBlade - 1) * 2 + 1);
            if (parameters.BladeShape == Shapes.Rectangular)
                maxBladeTriangles = maxBladesPerVertex * ((maxSegmentsPerBlade) * 2);

            long sourceBufferSize = numSourceVertices * Grass.SOURCE_VERT_STRIDE * 2; // source size doubles as the source mesh is not unloaded at runtime
            long drawBufferSize = numSourceVertices * maxBladeTriangles * Grass.DRAW_STRIDE;
            long sumSize = sourceBufferSize + drawBufferSize;

            Vector3 memoryConsumption = new Vector3(sumSize.ToByteSize(unit, useDecimal), sourceBufferSize.ToByteSize(unit, useDecimal), drawBufferSize.ToByteSize(unit, useDecimal));
            return memoryConsumption;
        }
        public static Vector3 GetEstimatedMemoryConsumption(BatchAsset batch, Shaders.ByteUnits unit = Shaders.ByteUnits.Base, bool useDecimal = false)
        {
            Vector3 memoryConsumption = Vector3.zero;
            foreach (Mesh m in batch.Meshes)
                memoryConsumption += GetEstimatedMemoryConsumption(m, batch.Parameters, unit, useDecimal);
            return memoryConsumption;
        }
        public static Vector2 GetVertexCount(BatchAsset batch)
        {
            Vector2Int result = Vector2Int.zero;
            result.x = batch.Meshes.Count;
            foreach (Mesh m in batch.Meshes)
                result.y += m.vertexCount;
            return result;
        }


#if UNITY_EDITOR
        private const int TMPMenuOrder = 100; // used as reference for locating menu item in asset/create menu


        [MenuItem("Assets/Create/ComputeGrass/Batch", false, TMPMenuOrder)]
        public static void CreateBatch()
        {
            BatchAsset assetObject = ScriptableObject.CreateInstance<BatchAsset>();
            assetObject.name = nameof(BatchAsset);
            SaveBatch(assetObject, string.Empty, false);
        }

        public static bool SaveBatch(BatchAsset grassBatch, string basePath = "", bool replace = true)
        {
            if (grassBatch == null)
            {
                Debug.LogWarning($"The given {nameof(grassBatch)} object is not set and cannot be saved to assets.");
                return false;
            }

            if (string.IsNullOrEmpty(basePath))
            {
                UnityEngine.Object target = Selection.activeObject;
                if (target == null || !(target is DefaultAsset))
                {
                    Debug.LogWarning("A folder or asset must first be selected in order to create a definition asset.");
                    return false;
                }
                // Get the path to the selected asset.
                basePath = AssetDatabase.GetAssetPath(target);
                basePath = "Assets"; // use Assets root as default value
            }


            List<Mesh> meshesToDelete = new List<Mesh>();
            string meshFolderPath = basePath + Path.AltDirectorySeparatorChar + grassBatch.name;
            //Debug.Log("IsFolder: " + AssetDatabase.IsValidFolder(meshFolderPath));
            if (!AssetDatabase.IsValidFolder(meshFolderPath))
            {
                // generate mesh sub folder for the batch
                string meshFolderGuid = AssetDatabase.CreateFolder(basePath, grassBatch.name);

                // validate guid for created folder and retrieve the actual asset path afterwards (Unity applies unique naming to AssetDatabase.CreateFolder)
                if (!string.IsNullOrEmpty(meshFolderGuid))
                    meshFolderPath = AssetDatabase.GUIDToAssetPath(meshFolderGuid);
            }
            else
            {
                // get all meshes from asset folder to track all meshes for deletion
                string[] aMaterialFiles = Directory.GetFiles(meshFolderPath, "*.asset", SearchOption.TopDirectoryOnly);
                foreach (string assetFilePath in aMaterialFiles)
                {
                    Mesh exMesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetFilePath);
                    if (exMesh != null)
                        meshesToDelete.Add(exMesh);
                }

                //Debug.Log("Existing meshes in batch folder: " + meshesToDelete.Count);

            }

            // generate mesh sub folder for the batch
            if (!string.IsNullOrEmpty(meshFolderPath))
            {
                //Debug.Log(meshFolderPath);
                // save all mesh assets into the created folder
                for (int i = 0; i < grassBatch.meshes.Count; i++)
                {
                    Mesh mesh = grassBatch.meshes[i];
                    string meshAssetPath = meshFolderPath + Path.AltDirectorySeparatorChar + mesh.name + ".asset";

                    // Create new definition asset
                    Mesh existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshAssetPath);
                    if (existingMesh != null)
                    {
                        // remove from 
                        meshesToDelete.Remove(existingMesh);
                        existingMesh.Clear();
                        existingMesh.name = mesh.name;
                        existingMesh.SetVertices(mesh.vertices);
                        existingMesh.SetNormals(mesh.normals);
                        existingMesh.SetUVs(0, mesh.uv);
                        existingMesh.SetIndices(mesh.GetIndices(0), mesh.GetTopology(0), 0);
                        grassBatch.meshes[i] = existingMesh;
                        //AssetDatabase.SaveAssetIfDirty(existingMesh);
                    }
                    else
                    {
                        AssetDatabase.CreateAsset(mesh, meshAssetPath);
                    }

                }

                // delete any mesh assets that reside in the grass batch subfolder and are not used (remove unused previous meshes)
                for (int i = meshesToDelete.Count - 1; i >= 0; i--)
                {
                    if (AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(meshesToDelete[i])))
                        meshesToDelete.RemoveAt(i);
                }

                //Debug.Log("Remaining meshes to delete: " + meshesToDelete.Count);
                AssetDatabase.SaveAssets();
            }

            string uniquePath = basePath + Path.AltDirectorySeparatorChar + grassBatch.name + ".asset";
            if (!replace)
                // make path unique (after ensuring it starts at Assets root folder path
                uniquePath = AssetDatabase.GenerateUniqueAssetPath(uniquePath);


            BatchAsset existingBatch = AssetDatabase.LoadAssetAtPath<BatchAsset>(uniquePath);
            if (existingBatch != null)
            {
                existingBatch.definition = grassBatch.definition;
                existingBatch.parameters = grassBatch.parameters;
                existingBatch.meshes.Clear();
                existingBatch.meshes.AddRange(grassBatch.meshes);

                // delete 'new' as the existing is written to disk again
                GameObject.DestroyImmediate(grassBatch);

                EditorUtility.SetDirty(existingBatch);
                AssetDatabase.SaveAssetIfDirty(existingBatch);
            }
            else
            {
                // Create new definition asset
                AssetDatabase.CreateAsset(grassBatch, uniquePath);

                // mark asset dirty and save it
                EditorUtility.SetDirty(grassBatch);
                AssetDatabase.SaveAssetIfDirty(grassBatch);
            }
            return true;
        }
        public static BatchAsset CreateWith(DefinitionAsset definition, ParametersAsset parameters, ICollection<Mesh> meshes)
        {
            if (definition == null || parameters == null || meshes == null || meshes.Count == 0)
            {
                Debug.LogWarning($"Given arguments are not sufficient to create a {nameof(BatchAsset)}.");
                return null;
            }

            BatchAsset assetObject = ScriptableObject.CreateInstance<BatchAsset>();
            assetObject.name = nameof(BatchAsset);
            assetObject.definition = definition;
            assetObject.parameters = parameters;
            assetObject.meshes.Clear();
            assetObject.meshes.AddRange(meshes);
            return assetObject;
        }

#endif



    }

#if UNITY_EDITOR

    [CanEditMultipleObjects()]
    [CustomEditor(typeof(BatchAsset))]
    public class GrassBatchInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            Vector3 memoryConsumptionMiB = Vector3.zero;
            Vector3 memoryConsumptionMB = Vector3.zero;
            int numOfBatches = 0;
            for (int i = 0; i < targets.Length; i++)
            {
                BatchAsset batch = targets[i] as BatchAsset;
                if (batch != null)
                {
                    numOfBatches++;
                    memoryConsumptionMiB += BatchAsset.GetEstimatedMemoryConsumption(batch, Shaders.ByteUnits.Mega);
                    memoryConsumptionMB += BatchAsset.GetEstimatedMemoryConsumption(batch, Shaders.ByteUnits.Mega, true);
                }
            }

            EditorGUILayout.LabelField($"Memory Consumption ({numOfBatches} Selected)", EditorStyles.largeLabel);
            EditorGUILayout.LabelField("Total Data", $"{memoryConsumptionMiB.x:0.000} MiB / {memoryConsumptionMB.x:0.000} MB");
            EditorGUILayout.LabelField("Source Data", $"{memoryConsumptionMiB.y:0.000} MiB / {memoryConsumptionMB.y:0.000} MB");
            EditorGUILayout.LabelField("GPU Buffers", $"{memoryConsumptionMiB.z:0.000} MiB / {memoryConsumptionMB.z:0.000} MB");
            EditorGUILayout.Space();

            // only display base inspector in case only one batch is selected
            if (numOfBatches <= 1)
                base.OnInspectorGUI();
        }

    }
#endif 
}