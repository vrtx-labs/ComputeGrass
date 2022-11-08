using UnityEngine;

namespace VRTX.ComputeGrass
{
    public struct TexturedVertex
    {
        public static readonly TexturedVertex zero = new TexturedVertex(Vector3.zero, Vector2.zero);

        public Vector3 Position;
        public Vector2 UV;

        // direct access to Position properties (for partial API compatibility to Vector3)
        public float magnitude => this.Position.magnitude;
        public float sqrMagnitude => this.Position.sqrMagnitude;

        public TexturedVertex(Vector3 position, Vector2 uv)
        {
            this.Position = position;
            this.UV = uv;
        }

        public static TexturedVertex operator +(TexturedVertex lh, TexturedVertex rh) => new TexturedVertex(lh.Position + rh.Position, lh.UV + rh.UV);
        public static TexturedVertex operator -(TexturedVertex lh, TexturedVertex rh) => new TexturedVertex(lh.Position - rh.Position, lh.UV - rh.UV);
        public static TexturedVertex operator *(TexturedVertex lh, float rh) => new TexturedVertex(lh.Position * rh, lh.UV * rh);
        public static TexturedVertex operator /(TexturedVertex lh, float rh) => new TexturedVertex(lh.Position / rh, lh.UV / rh);

    }

}