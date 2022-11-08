using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VRTX.ComputeGrass
{
    // The structure to send to the compute shader
    // This layout kind assures that the data is laid out sequentially
    [Serializable()]
    [StructLayout(LayoutKind.Sequential)]
    public struct BladeArgs
    {
        public static readonly BladeArgs DefaultSquare = new BladeArgs(1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 3.0f, 0.0f);
        public static readonly BladeArgs DefaultRectThin = new BladeArgs(1.0f, 0.0f, 0.25f, 0.0f, 0.0f, 3.0f, 0.0f);


        // ToDo: using range attributes and default values make the grass compute shader go wonky
        [SerializeField]
        [Range(0, 5)]
        private float height;
        [SerializeField]
        [Range(0, 1)]
        private float randomHeight;
        [SerializeField]
        [Range(0, 5)]
        private float aspectRatio;
        [SerializeField]
        [Range(0, 1)]
        private float radius;
        [SerializeField]
        [Range(0, 1)]
        private float forward;
        [SerializeField]
        [Range(0, 5)]
        private float curvature;
        [SerializeField]
        [Range(0, 1)]
        private float rootWidth;

        public BladeArgs(float height, float randomHeight, float aspectRatio, float radius, float forward, float curvature, float rootWidth)
        {
            this.radius = radius;
            this.forward = forward;
            this.curvature = curvature;
            this.rootWidth = rootWidth;
            this.height = height;
            this.randomHeight = randomHeight;
            this.aspectRatio = aspectRatio;
        }

    }

}