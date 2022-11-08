using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using VRTX.ComputeGrass.Assets;

namespace VRTX.ComputeGrass
{

    public class BatchGenerator : MonoBehaviour
    {
        private static readonly int[] EmptyIndexList = new int[] { };

        [Header("Target(s)")]
        [SerializeField()]
        private Transform _renderersRoot = null;
        [SerializeField()]
        private List<MeshRenderer> _renderers = new List<MeshRenderer>();

        [Header("Generation Assets")]
        [SerializeField]
        private DefinitionAsset _definition = null;
        [SerializeField]
        private ParametersAsset _parameters = null; // ToDo: Add custom inspector which hides internal parameters when override is not-null

        [Header("Name & Location")]
        [SerializeField]
        private string _name = nameof(BatchAsset);
        [SerializeField()]
        [HideInInspector()]
        // needs to point to subfolder in resources to allow loading the assets // may be replaced with reference to scriptable object
        private string _sourcePath = string.Empty;


        // mesh data lists
        private List<Vector3> _positionCache = new List<Vector3>();
        private List<Vector3> _normalCache = new List<Vector3>();
        private List<Vector2> _uvCache = new List<Vector2>();

        private List<Triangle> _sourceTriangles = new List<Triangle>();
        private List<int> _sourceIndices = new List<int>();
        private List<Vector3> _sourceVertices = new List<Vector3>();
        private List<Vector2> _sourceTexcoords = new List<Vector2>();


        private List<MeshRenderer> _sourceRenderers = new List<MeshRenderer>();
        private List<Mesh> _sourceMeshes = new List<Mesh>();



        internal string Name
        { get => _name; }

        internal string SourcePath
        {
            get => _sourcePath;
            set => _sourcePath = value;
        }

        internal ICollection<Mesh> SourceMeshes => _sourceMeshes;
        internal DefinitionAsset Definition => _definition;
        internal ParametersAsset Parameters => _parameters;
        internal Parameters ParametersData => _parameters;


        // Destroy batch generator at runtime (to avoid remainder of the instance)
        private void Awake()
        {
            Debug.LogWarning($"{nameof(BatchGenerator)} '{this.name}' is not supported at runtime and will be destroyed!");
            GameObject.Destroy(this.gameObject);
        }

        internal void PrepareRenderers()
        {
            _sourceMeshes.Clear();
            if (_renderersRoot != null)
            {
                _sourceRenderers.Clear();
                _renderersRoot.GetComponentsInChildren<MeshRenderer>(_sourceRenderers);
            }
            else if (_renderers.Count > 0)
            {
                _sourceRenderers.AddRange(_renderers);
                _sourceRenderers = _sourceRenderers.Distinct().ToList();
            }
            else
                Debug.Log("Root target object or list of target renderers are null or empty, renderers cannot be retrieved.");
        }
        internal void Clear()
        {
            this.ClearDataLists();
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                _sourceRenderers.Clear();
            }
#endif
        }
        private void ClearDataLists()
        {
            _positionCache.Clear();
            _normalCache.Clear();
            _uvCache.Clear();

            _sourceTriangles.Clear();
            _sourceIndices.Clear();
            _sourceVertices.Clear();
            _sourceTexcoords.Clear();
        }

        internal void Generate()
        {
            Random.State prevState = Random.state;

            if (this.ParametersData.Seed != -1)
                Random.InitState(this.ParametersData.Seed);
            else
                Random.InitState((int)System.DateTime.Now.Ticks);

            _sourceMeshes.Clear();
            for (int i = 0; i < _sourceRenderers.Count; i++)
            {
                Mesh sourceMesh = new Mesh();
                sourceMesh.name = _sourceRenderers[i].name;
                _sourceMeshes.Add(sourceMesh);
                GenerateFor(_sourceRenderers[i], sourceMesh);
            }

            // restore random state
            Random.state = prevState;
        }
        internal bool GenerateFor(MeshRenderer meshRenderer, Mesh sourceMesh)
        {
            if (meshRenderer != null && meshRenderer.TryGetComponent<MeshFilter>(out MeshFilter meshFilter))
            {
                Mesh mesh = meshFilter.sharedMesh;
                if (mesh == null)
                    return false;

                Matrix4x4 transformation = meshRenderer.transform.localToWorldMatrix;
                this.ClearDataLists();


                mesh.GetVertices(_sourceVertices);
                mesh.GetUVs(0, _sourceTexcoords);
                // pretransform vertices to world space (to take scaled objects into account
                for (int i = 0; i < _sourceVertices.Count; i++)
                    _sourceVertices[i] = transformation.MultiplyPoint(_sourceVertices[i]);


                float areaCutoff = this.ParametersData.TriangleAreaCutoff;
                int subdivLevel = this.ParametersData.SubdivisionLevel;
                //Debug.Log("Subdiv Method: " + this.ParametersData.SubdivisionMethod);

                // assume mesh topology is triangles and triangles only
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    SubMeshDescriptor sDescr = mesh.GetSubMesh(s);
                    if (sDescr.topology == MeshTopology.Triangles)
                    {
                        mesh.GetIndices(_sourceIndices, s, false);
                        for (int t = 0; t < sDescr.indexCount; t += 3)
                        {
                            Triangle tri = Triangle.GetTriangle(_sourceIndices, t, _sourceVertices, _sourceTexcoords);
                            // check triangle for area size and skip subdividing it
                            if (tri.Area > areaCutoff)
                                tri.Subdivide(_sourceTriangles, subdivLevel, this.ParametersData.SubdivisionMethod);
                        }
                    }

                }

                // iterate over all triangles and check for area cutoff
                int skipped = 0;
                //Debug.Log($"Triangles in mesh {mesh.name} are {sourceTriangles.Count} in total. Skipped {skipped}.");
                for (int i = 0; i < _sourceTriangles.Count; i++)
                {
                    Triangle tri = _sourceTriangles[i];
                    if (tri.Area <= areaCutoff)
                    {
                        skipped++;
                        continue;
                    }

                    TexturedVertex centerOfMass = tri.GetCenterOfMass(this.ParametersData.SubdivisionOptions.HasFlag(Triangle.SubdivisionOptions.OffsetCenterOfMassLast));
                    Vector3 normal = tri.Normal;
                    _positionCache.Add(centerOfMass.Position);
                    _normalCache.Add(normal);
                    _uvCache.Add(centerOfMass.UV);
                }

                // output warning about high skip triangle ratio
                if (skipped > 0)
                {
                    float skipRatio = (float)skipped / (float)_sourceTriangles.Count;
                    Debug.Log("Triangles: " + _sourceTriangles.Count + " (" + skipped + " skipped)");
                    if (skipRatio > 0.10f)
                        Debug.LogWarning($"Skipped {skipRatio * 100:##0.#}% of triangles of '{meshRenderer.name}'. Consider checking the resulting grass batch.");
                }

                this.SetMeshData(sourceMesh);
                return true;
            }
            return false;
        }
        internal void SetMeshData(Mesh mesh)
        {
            if (mesh != null)
            {
                mesh.Clear();
                mesh.SetVertices(_positionCache);
                mesh.SetIndices(EmptyIndexList, MeshTopology.Points, 0);
                mesh.SetUVs(0, _uvCache);
                mesh.SetNormals(_normalCache);
                mesh.RecalculateBounds();
            }
        }

    }

#if UNITY_EDITOR

    [CustomEditor(typeof(BatchGenerator))]
    public class GrassBatchGeneratorInspector : Editor
    {
        private DefaultAsset _folder = null;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            BatchGenerator generator = this.target as BatchGenerator;
            _folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(generator.SourcePath);
            _folder = (DefaultAsset)EditorGUILayout.ObjectField("Asset Location", _folder, typeof(DefaultAsset), false);


            SerializedProperty spSourcePath = this.serializedObject.FindProperty("_sourcePath");
            SerializedProperty spName = this.serializedObject.FindProperty("_name");

            using (new EditorGUI.DisabledGroupScope(Application.isPlaying || string.IsNullOrEmpty(spName.stringValue)))
            {
                string sourcePath = _folder != null ? AssetDatabase.GetAssetPath(_folder) : "Assets";

                if (!generator.SourcePath.Equals(sourcePath))
                {
                    spSourcePath.stringValue = sourcePath;
                    this.serializedObject.ApplyModifiedProperties();
                }

                EditorGUILayout.Space();
                if (GUILayout.Button("Generate"))
                {
                    generator.Clear();
                    generator.PrepareRenderers();
                    generator.Generate();

                    //Debug.Log("Store Batch: " + generator.SourceMeshes.Count);
                    BatchAsset batch = BatchAsset.CreateWith(generator.Definition, generator.Parameters, generator.SourceMeshes);
                    // update grass batch name if a name is set in the generator
                    if (!string.IsNullOrEmpty(generator.Name))
                        batch.name = generator.Name;

                    Vector2 sourceVertexCount = BatchAsset.GetVertexCount(batch);
                    if (!BatchAsset.SaveBatch(batch, generator.SourcePath, true))
                    {
                        if (!Application.isPlaying)
                            GameObject.DestroyImmediate(batch);
                        else
                            GameObject.Destroy(batch);
                    }

                    // clear data lists after saving batch assets
                    generator.Clear();

                    Debug.Log($"Generated '{generator.Name}' at: {generator.SourcePath}. Total of {sourceVertexCount.y} for {sourceVertexCount.x} target meshes.");
                }
            }

        }

    }
#endif 
}