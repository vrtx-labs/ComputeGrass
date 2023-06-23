// VRTX Labs GmbH version by Tobias Pott
// credits to @Minionsart https://www.patreon.com/posts/compute-grass-in-63162723 & https://www.patreon.com/minionsart/posts
// credits  to  forkercat https://gist.github.com/junhaowww/fb6c030c17fe1e109a34f1c92571943f
// and  NedMakesGames https://gist.github.com/NedMakesGames/3e67fabe49e2e3363a657ef8a6a09838
// for the base setup for compute shaders
using UnityEngine;
using UnityEngine.Serialization;
using VRTX.ComputeGrass.Assets;
using VRTX.ComputeGrass.Extensions;

namespace VRTX.ComputeGrass
{
    [ExecuteInEditMode]
    public partial class Grass : MonoBehaviour
    {
        // defined number of maximum variants supported (four blade variants per batch)
        private const int MaximumVariants = 4;
        // static and constant members to build primitives for bounds
        private const float CullingDistance = 4;

        // The size of one entry in the various compute buffers
        public const int SOURCE_VERT_STRIDE = sizeof(float) * (3 + 3 + 2);
        public const int DRAW_STRIDE = sizeof(float) * (4 + (3 + 3) * 3);
        public const int INDIRECT_ARGS_STRIDE = sizeof(int) * 4;
        public const int BLADE_ARGS_STRIDE = sizeof(float) * 7; // see BladeArgs for number of members


        // defined empty material list used on ComputeGrass' culling MeshRenderer component to not render/hide the bounds but receive culling callbacks
        private static readonly Material[] EmptyMaterialList = new Material[0];
        private static readonly string[] ShaderKeywords_Variants = new string[] { "VARIANTS_2", "VARIANTS_3", "VARIANTS_4" };

        private static readonly Vector3[] BoundsPositionCache = new Vector3[8];
        private static readonly int[] BoundsIndices = new int[] { 3, 2, 1, 0, 4, 5, 6, 7 };

        // The data to reset the args buffer with every frame
        // 0: vertex count per draw instance. We will only use one instance
        // 1: instance count. One
        // 2: start vertex location if using a Graphics Buffer
        // 3: and start instance location if using a Graphics Buffer
        private static readonly int[] argsBufferReset = new int[] { 0, 1, 0, 0 };


        #region Fields

        [SerializeField]
        [FormerlySerializedAs("grassDefinition")]
        private DefinitionAsset m_definition = null;
        [SerializeField]
        [FormerlySerializedAs("grassParametersOverride")]
        private ParametersAsset m_parametersOverride = null; // ToDo: Add custom inspector which hides internal parameters when override is not-null
        [SerializeField]
        [FormerlySerializedAs("grassParameters")]
        private Parameters m_parameters = new Parameters();

        private Source m_source = default;


        // A state variable to help keep track of whether compute buffers have been set up
        private bool m_Initialized;
        // A state variable to help keep track of whether compute shader have run to generate the geometry
        private bool m_Computed;
        // A compute buffer to hold vertex data of the source mesh
        private ComputeBuffer m_SourceVertBuffer;
        // A compute buffer to hold vertex data of the generated mesh
        private ComputeBuffer m_DrawBuffer;
        // A compute buffer to hold blade definition argument
        private ComputeBuffer m_BladeArgsBuffer;
        // A compute buffer to hold indirect draw arguments
        private ComputeBuffer m_ArgsBuffer;
        // Instantiate the shaders so data belong to their unique compute buffers
        private ComputeShader m_InstantiatedComputeShader;
        // Reference to the shared material, unique/per-instance properties are achieved by the property block
        private Material m_SharedMaterial;
        // Set graphics shader properties for unique/instanced-like behaviour on different objects
        private MaterialPropertyBlock m_graphicsPropertyBlock;
        // The id of the kernel in the grass compute shader
        private int m_IdGrassKernel;
        // The x dispatch size for the grass compute shader
        private int m_DispatchSize;
        // The local bounds of the generated mesh
        private Bounds m_bounds;
        // The mesh representing the local bounds (only two faces of cube for lower memory footprint)
        private Mesh m_boundsMesh = null;
        #endregion

        #region Properties
        protected Parameters ActiveParameters
        {
            get
            {
                if (m_parametersOverride != null)
                    return m_parametersOverride;
                return m_parameters;
            }
        }
        #endregion


        internal static Grass Create(DefinitionAsset definition, ParametersAsset parameters, Mesh sourceMesh, Transform parent = null)
        {
            // create a new GameObject with source and grass components (configure it to not be saved in scene or build)
            GameObject goComputeGrass = new GameObject(sourceMesh.name, typeof(Source), typeof(Grass));
            goComputeGrass.SetActive(false);
            goComputeGrass.gameObject.hideFlags = HideFlags.HideAndDontSave; // avoid saving the instances with the scene
            goComputeGrass.transform.SetParent(parent);
            // get the source component and set the given mesh as default source mesh
            Source source = goComputeGrass.GetComponent<Source>();
            source.SetMesh(sourceMesh);
            // get the grass component and configure it using the source, given definition and parameters
            Grass grass = goComputeGrass.GetComponent<Grass>();
            grass.Configure(source, definition, parameters);
            return grass;
        }
        private void Configure(Source source, DefinitionAsset definition, ParametersAsset parameters)
        {
            this.m_source = source;
            this.m_definition = definition;
            this.m_parametersOverride = parameters;
        }

        public void OnValidate()
        {
            // update shader properties on validate to allow runtime changes to apply
            this.SetGrassDataBase();
        }


        private void OnEnable()
        {
            // If initialized, call on disable to clean things up
            if (m_Initialized)
            {
                OnDisable();
            }

            // Setup compute shader and material manually

            // Don't do anything if resources are not found
            if (m_source == null || m_source.Mesh == null || !this.ActiveParameters.IsValid || m_definition == null)
                return;

            // Don't do anything if mesh has no vertices (vertices are relevant not indices)
            Mesh sourceMesh = m_source.Mesh;
            if (sourceMesh.vertexCount == 0)
                return;

            // check for null bounds mesh and create new empty if necessary
            if (m_boundsMesh == null)
            {
                m_boundsMesh = new Mesh();
                m_boundsMesh.name = nameof(Bounds);
            }

            m_Initialized = true;

            // Instantiate the shaders so they can point to their own buffers
            m_InstantiatedComputeShader = Instantiate(this.ActiveParameters.ComputeShader);
            m_SharedMaterial = this.ActiveParameters.Material; // material is shared and differences are passed as property block
            m_graphicsPropertyBlock = new MaterialPropertyBlock();

            // Grab data from the source mesh
            Vector3[] positions = sourceMesh.vertices;
            Vector3[] normals = sourceMesh.normals;
            Vector2[] uvs = sourceMesh.uv;

            // Create the data to upload to the source vert buffer
            SourceVertex[] vertices = new SourceVertex[positions.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new SourceVertex()
                {
                    position = positions[i],
                    normal = normals[i],
                    uv = uvs[i],
                };
            }

            int numSourceVertices = vertices.Length;

            // Each segment has two points
            int maxBladesPerVertex = Mathf.Max(1, this.ActiveParameters.BladesPerVertex);
            int maxSegmentsPerBlade = Mathf.Max(1, this.ActiveParameters.SegmentsPerBlade);
            int maxBladeTriangles = maxBladesPerVertex * ((maxSegmentsPerBlade - 1) * 2 + 1);
            if (this.ActiveParameters.BladeShape == Shapes.Rectangular)
                maxBladeTriangles = maxBladesPerVertex * ((maxSegmentsPerBlade) * 2);

            // Create compute buffers
            // The stride is the size, in bytes, each object in the buffer takes up
            m_SourceVertBuffer = new ComputeBuffer(vertices.Length, SOURCE_VERT_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
            m_SourceVertBuffer.SetData(vertices);

            m_DrawBuffer = new ComputeBuffer(numSourceVertices * maxBladeTriangles, DRAW_STRIDE, ComputeBufferType.Append);
            m_DrawBuffer.SetCounterValue(0);

            m_BladeArgsBuffer = new ComputeBuffer(m_definition.NumberOfVariants, BLADE_ARGS_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable); // somehow not working when flagged "Constant" buffer
            m_BladeArgsBuffer.SetData(m_definition.Arguments);

            m_ArgsBuffer = new ComputeBuffer(1, INDIRECT_ARGS_STRIDE, ComputeBufferType.IndirectArguments);


            // Cache the kernel IDs we will be dispatching
            m_IdGrassKernel = m_InstantiatedComputeShader.FindKernel("Main");

            // Set buffer data
            m_InstantiatedComputeShader.SetBuffer(m_IdGrassKernel, "_SourceVertices", m_SourceVertBuffer);
            m_InstantiatedComputeShader.SetBuffer(m_IdGrassKernel, "_DrawTriangles", m_DrawBuffer);
            m_InstantiatedComputeShader.SetBuffer(m_IdGrassKernel, "_BladeVariants", m_BladeArgsBuffer);
            m_InstantiatedComputeShader.SetBuffer(m_IdGrassKernel, "_IndirectArgsBuffer", m_ArgsBuffer);

            // Set vertex data
            m_InstantiatedComputeShader.SetInt("_NumSourceVertices", numSourceVertices);
            m_InstantiatedComputeShader.SetInt("_MaxBladesPerVertex", maxBladesPerVertex);
            m_InstantiatedComputeShader.SetInt("_MaxSegmentsPerBlade", maxSegmentsPerBlade);

            m_graphicsPropertyBlock.SetBuffer("_DrawTriangles", m_DrawBuffer);

            // Calculate the number of threads to use. Get the thread size from the kernel
            // Then, divide the number of triangles by that size
            m_InstantiatedComputeShader.GetKernelThreadGroupSizes(m_IdGrassKernel, out uint threadGroupSize, out _, out _);
            m_DispatchSize = Mathf.CeilToInt((float)numSourceVertices / threadGroupSize);


            // Get the bounds of the source mesh and then expand by the maximum blade width and height
            m_bounds = sourceMesh.bounds;
            m_bounds.Expand(CullingDistance);
            // Transform the bounds to world space
            m_bounds = TransformBounds(m_bounds);

            this.SetGrassDataBase();
            this.UpdateRendererBounds();

            // unload mesh from grass source (reduce memory footprint as it was read into the source... lists
            if (sourceMesh != null)
                Resources.UnloadAsset(sourceMesh);
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            // Dispose of buffers and copied shaders here
            if (m_Initialized)
            {
                // If the application is not in play mode, we have to call DestroyImmediate
                if (Application.isPlaying)
                    GameObject.Destroy(m_InstantiatedComputeShader);
                else
                    GameObject.DestroyImmediate(m_InstantiatedComputeShader);

                // Release each buffer
                if (m_SourceVertBuffer.TryReleaseAndDispose())
                    m_SourceVertBuffer = null;

                if (m_DrawBuffer.TryReleaseAndDispose())
                    m_DrawBuffer = null;

                if (m_BladeArgsBuffer.TryReleaseAndDispose())
                    m_BladeArgsBuffer = null;

                if (m_ArgsBuffer.TryReleaseAndDispose())
                    m_ArgsBuffer = null;
            }

            m_Initialized = false;
            m_Computed = false;
#endif
        }

        private void OnDestroy()
        {
            // destroy native mesh object (is recreated in Enable)
            if (m_boundsMesh != null)
                if (Application.isPlaying)
                    GameObject.Destroy(m_boundsMesh);
                else
                    GameObject.DestroyImmediate(m_boundsMesh);
        }

        // LateUpdate is called after all Update calls
        private void LateUpdate()
        {
            // If in edit mode, we need to update the shaders each Update to make sure settings changes are applied
            // Don't worry, in edit mode, Update isn't called each frame
            if (Application.isPlaying == false)
            {
                OnDisable();
                OnEnable();
            }

            // If not initialized, do nothing (creating zero-length buffer will crash)
            if (!m_Initialized)
            {
                // Initialization is not done, please check if there are null components
                // or just because there is not vertex being painted.
                return;
            }


            // Update the shader with frame specific data
            SetGrassDataDynamic();

            if (Application.isPlaying)
            {
                if (!m_Computed || this.ActiveParameters.UpdateOn == RecomputeEvents.Update)
                {
                    // Clear the draw and indirect args buffers of last frame's data
                    m_DrawBuffer.SetCounterValue(0);
                    m_ArgsBuffer.SetData(argsBufferReset);

                    // Dispatch the grass shader. It will run on the GPU
                    m_InstantiatedComputeShader.Dispatch(m_IdGrassKernel, m_DispatchSize, 1, 1);
                    // mark as computed and data ready to be used for Graphics.DrawProceduralIndirect call
                    m_Computed = true;
                }
                if (m_Computed || this.ActiveParameters.UpdateOn == RecomputeEvents.Update)
                {
                    // DrawProceduralIndirect queues a draw call up for our generated mesh
                    Graphics.DrawProceduralIndirect(m_SharedMaterial, m_bounds, MeshTopology.Triangles, m_ArgsBuffer, 0, null, m_graphicsPropertyBlock, this.ActiveParameters.CastShadow, true, gameObject.layer);
                }

            }
            else
            {
#if UNITY_EDITOR
                // Clear the draw and indirect args buffers of last frame's data
                m_DrawBuffer.SetCounterValue(0);
                m_ArgsBuffer.SetData(argsBufferReset);

                // Dispatch the grass shader. It will run on the GPU
                m_InstantiatedComputeShader.Dispatch(m_IdGrassKernel, m_DispatchSize, 1, 1);

                // DrawProceduralIndirect queues a draw call up for our generated mesh
                Graphics.DrawProceduralIndirect(m_SharedMaterial, m_bounds, MeshTopology.Triangles, m_ArgsBuffer, 0, null, m_graphicsPropertyBlock, this.ActiveParameters.CastShadow, true, gameObject.layer);
#endif
            }
        }

        private void UpdateRendererBounds()
        {
            if (m_source == null || m_source.Mesh == null)
            {
                Debug.LogWarning($"No source data is available and no grass will be generated");
                return;
            }

            // get mesh filter or add if none exists
            MeshFilter meshFilter = this.GetComponent<MeshFilter>();
            if (meshFilter == null)
                meshFilter = this.gameObject.AddComponent<MeshFilter>();


            // get min and max vectors of this instance source vertex data
            Vector3 min = this.m_bounds.min;
            Vector3 max = this.m_bounds.max;
            // update each cube corner with the respective min/max component value
            BoundsPositionCache[0] = new Vector3(min.x, min.y, min.z); // back, left, bottom
            BoundsPositionCache[1] = new Vector3(max.x, min.y, min.z);
            BoundsPositionCache[2] = new Vector3(max.x, max.y, min.z);
            BoundsPositionCache[3] = new Vector3(min.x, max.y, min.z);
            BoundsPositionCache[4] = new Vector3(min.x, min.y, max.z); // front, left, bottom
            BoundsPositionCache[5] = new Vector3(max.x, min.y, max.z);
            BoundsPositionCache[6] = new Vector3(max.x, max.y, max.z);
            BoundsPositionCache[7] = new Vector3(min.x, max.y, max.z);

            // update bounds mesh with updated vertices and indices (which is a static sequence)
            m_boundsMesh.SetVertices(BoundsPositionCache);
            m_boundsMesh.SetIndices(BoundsIndices, MeshTopology.Quads, 0);
            meshFilter.sharedMesh = m_boundsMesh;

            // Update mesh renderer material list to empty list to prevent actual rendering
            MeshRenderer meshRenderer = this.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                meshRenderer = this.gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterials = Grass.EmptyMaterialList;

        }

        private void SetGrassDataBase()
        {
            if (m_InstantiatedComputeShader == null)
                return;

            // Send things to compute shader that dont need to be set every frame
            m_InstantiatedComputeShader.SetKeyword("LEVELOFDETAIL", this.ActiveParameters.EnableLevelOfDetail);
            m_InstantiatedComputeShader.SetKeyword("TAPERED", this.ActiveParameters.BladeShape == Shapes.Tapered);
            m_InstantiatedComputeShader.SetKeyword(ShaderKeywords_Variants, this.m_definition.NumberOfVariants - 2);
            // set local to world matrix from code (no used in shader)
            m_InstantiatedComputeShader.SetMatrix("_LocalToWorld", Matrix4x4.identity);

            m_InstantiatedComputeShader.SetFloat("_WindSpeed", this.ActiveParameters.WindSpeed);
            m_InstantiatedComputeShader.SetFloat("_WindStrength", this.ActiveParameters.WindStrength);

            if (this.ActiveParameters.EnableLevelOfDetail)
            {
                if (Application.isPlaying || !this.ActiveParameters.IgnoreInEditor)
                {
                    m_InstantiatedComputeShader.SetFloat("_MinFadeDist", this.ActiveParameters.MinFadeDistance);
                    m_InstantiatedComputeShader.SetFloat("_MaxFadeDist", this.ActiveParameters.MaxFadeDistance);
                }
            }
            else
            {
                m_InstantiatedComputeShader.SetFloat("_MinFadeDist", 0);
                m_InstantiatedComputeShader.SetFloat("_MaxFadeDist", 999);
            }
            // set density map to compute shader
            if (this.m_definition.DensityMap != null)
                m_InstantiatedComputeShader.SetTexture(m_IdGrassKernel, "_DensityMap", this.m_definition.DensityMap);
            else
                m_InstantiatedComputeShader.SetTexture(m_IdGrassKernel, "_DensityMap", Texture2D.whiteTexture);



            // update instantiated material data
            if (m_SharedMaterial == null || m_graphicsPropertyBlock == null)
                return;

            // call to set variant textures
            this.SetGrassDataDynamic();
        }

        private void SetGrassDataDynamic()
        {
            // update instantiated material data if it is available
            if (m_SharedMaterial == null || m_graphicsPropertyBlock == null)
                return;

            // set keywords on graphics shader
            m_graphicsPropertyBlock.SetInt("_NumOfVariants", this.m_definition.NumberOfVariants);
            int maxSegmentsPerBlade = Mathf.Max(1, this.ActiveParameters.SegmentsPerBlade);
            m_graphicsPropertyBlock.SetInt("_SegmentsPerBlade", maxSegmentsPerBlade);

            m_BladeArgsBuffer.SetData(m_definition.Arguments);
            for (int i = 0; i < MaximumVariants; i++)
            {
                if (m_definition.Textures.Length > i && m_definition.Textures[i] != null)
                    m_graphicsPropertyBlock.SetTexture("_Variant" + i, m_definition.Textures[i]);
                else
                    m_graphicsPropertyBlock.SetTexture("_Variant" + i, Texture2D.whiteTexture);
            }
        }


        // This applies the game object's transform to the local bounds
        // Code by benblo from https://answers.unity.com/questions/361275/cant-convert-bounds-from-world-coordinates-to-loca.html
        private Bounds TransformBounds(Bounds localBounds)
        {
            var center = transform.TransformPoint(localBounds.center);

            // transform the local extents' axes
            var extents = localBounds.extents;
            var axisX = transform.TransformVector(extents.x, 0, 0);
            var axisY = transform.TransformVector(0, extents.y, 0);
            var axisZ = transform.TransformVector(0, 0, extents.z);

            // sum their absolute value to get the world extents
            extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
            extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
            extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

            return new Bounds { center = center, extents = extents };
        }

    }

}