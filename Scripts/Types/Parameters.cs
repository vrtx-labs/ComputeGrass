using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace VRTX.ComputeGrass
{
    [Serializable()]
    public class Parameters
    {
        [Header("Material & Shader")]
        [SerializeField]
        private Material material = default;
        [SerializeField]
        private ComputeShader computeShader = default;

        [Header("Subdivision")]
        [Tooltip("Seed used for randomised elements during generation. Use a set seed to reproduce results, use -1 for no fixed seed.")]
        [SerializeField]
        private int seed = -1;

        [Range(0, 4)]
        [SerializeField()]
        private int subdivisionLevel = 0;
        [SerializeField]
        private Triangle.SubdivisionMethod subdivisionMethod = Triangle.SubdivisionMethod.CenterOfMass;
        [SerializeField]
        private Triangle.SubdivisionOptions subdivisionOptions = Triangle.SubdivisionOptions.OffsetCenterOfMassLast;
        [SerializeField()]
        private float triangleAreaCutoff = 0.00625f;

        [Header("Geometry")]
        [SerializeField]
        private Shapes bladeShape = Shapes.Rectangular;
        [Range(1, 5)]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("allowedBladesPerVertex")]
        private int bladesPerVertex = 2;
        [Range(1, 5)]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("allowedSegmentsPerBlade")]
        private int segmentsPerBlade = 3;

        // LOD
        [Header("LOD")]
        [SerializeField]
        private bool enableLevelOfDetail = true;
        [SerializeField]
        private bool ignoreInEditor = true;
        [SerializeField]
        private float minFadeDistance = 40;
        [SerializeField]
        private float maxFadeDistance = 60;

        // Wind
        [Header("Wind")]
        [SerializeField]
        private float windSpeed = 10;
        [SerializeField]
        private float windStrength = 0.05f;

        // Other
        [Header("Other")]
        [SerializeField]
        private ShadowCastingMode castShadow = ShadowCastingMode.On;
        [SerializeField]
        private RecomputeEvents updateOn = RecomputeEvents.Update;


        #region Properties
        public int Seed => seed;
        public int SubdivisionLevel => subdivisionLevel;
        public Triangle.SubdivisionMethod SubdivisionMethod => subdivisionMethod;
        public Triangle.SubdivisionOptions SubdivisionOptions => subdivisionOptions;
        public float TriangleAreaCutoff => triangleAreaCutoff;


        public bool IsValid => material != null && computeShader != null;

        public Material Material => material;
        public ComputeShader ComputeShader => computeShader;

        public int BladesPerVertex => bladesPerVertex;
        public int SegmentsPerBlade => segmentsPerBlade;
        public Shapes BladeShape => bladeShape;

        public float WindSpeed => windSpeed;
        public float WindStrength => windStrength;

        public bool EnableLevelOfDetail => enableLevelOfDetail;
        public bool IgnoreInEditor => ignoreInEditor;
        public float MinFadeDistance => minFadeDistance;
        public float MaxFadeDistance => maxFadeDistance;

        public ShadowCastingMode CastShadow => castShadow;
        public RecomputeEvents UpdateOn => updateOn;

        #endregion

    }


}