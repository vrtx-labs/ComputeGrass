using System.Collections.Generic;
using UnityEngine;using VRTX.ComputeGrass.Extensions;

namespace VRTX.ComputeGrass
{
    public struct Triangle
    {
        public enum SubdivisionMethod
        {
            CenterOfMass = 1, // subdivide by center of mass and tri vertices (corners)
            [System.Obsolete("Not yet implemented")]
            CenterOfMassToCenterOfEdges = 2, // subdivide by center of mass and center of edges and tri vertices (corners)
            CenterOfHypothenuseToOpposingCorner = 4, // subdivide by center of mess and center of tri hypothenuse
            [System.Obsolete("Not yet implemented")]
            CenterOfHypothenuseToCenterOfCathesus = 8, // subdivide by center of hypothenuse and center of cathesus and vertex opposing hypothenuse
        }
        [System.Flags()]
        public enum SubdivisionOptions
        {
            OffsetCenterOfMassOther = 1, // offset all subdivided triangles by a random amount except the last ( ! only applies when subdividing ! )
            OffsetCenterOfMassLast = 2, // offset last subdivided triangle by a random amount
            OffsetCenterOfMassEverything = 3, // offset all subdivided triangles by a random amount
        }


        private static float _randomOffsetRange = 0.5f;
        public static float RandomOffsetRange
        {
            get => _randomOffsetRange;
            set => _randomOffsetRange = Mathf.Clamp(value, 0.0f, 1.0f);
        }


        private TexturedVertex _v0, _v1, _v2;
        private TexturedVertex _com, _comOffsetted;
        private Vector3 _n;


        public Vector3 Normal
        { get => _n; }
        public float Area
        { get => Vector3.Cross((_v1 - _v0).Position, (_v2 - _v0).Position).magnitude * 0.5f; }


        public static Triangle GetTriangle(List<int> indices, int indexStart, List<Vector3> vertices, List<Vector2> texcoords)
        {
            int i0 = indices[indexStart];
            int i1 = indices[indexStart + 1];
            int i2 = indices[indexStart + 2];

            TexturedVertex v0 = new TexturedVertex(vertices[i0], texcoords[i0]);
            TexturedVertex v1 = new TexturedVertex(vertices[i1], texcoords[i1]);
            TexturedVertex v2 = new TexturedVertex(vertices[i2], texcoords[i2]);

            return new Triangle(v0, v1, v2);
        }


        private Triangle(TexturedVertex v0, TexturedVertex v1, TexturedVertex v2)
        {
            _v0 = v0;
            _v1 = v1;
            _v2 = v2;

            _com = (_v0 + _v1 + _v2) / 3;


            // calculate triangle normal
            Vector3 e0 = (_v1 - _v0).Position;
            Vector3 e1 = (_v2 - _v0).Position;

            _n.x = e0.y * e1.z - e0.z * e1.y;
            _n.y = e0.z * e1.x - e0.x * e1.z;
            _n.z = e0.x * e1.y - e0.y * e1.x;
            _n.Normalize();

            // Calculate center of mass offset and store it
            _comOffsetted = _com;
            _comOffsetted += GetCenterOfMassOffset(RandomVector3());
        }



        public TexturedVertex GetCenterOfMass(bool getOffsetted)
        {
            if (getOffsetted)
                return _comOffsetted;
            else
                return _com;
        }

        private TexturedVertex GetCenterOfMassOffset(Vector3 factors)
        {
            TexturedVertex offset = TexturedVertex.zero;
            // calculate center of mass to corner edges
            offset += (_v0 - _com) * (factors.x);
            offset += (_v1 - _com) * (factors.y);
            offset += (_v2 - _com) * (factors.z);
            return offset;
        }

        public void Subdivide(List<Triangle> triangles, int subDivLevel = 0, SubdivisionMethod method = SubdivisionMethod.CenterOfMass, SubdivisionOptions option = 0)
        {
            bool offsetAlways = option.HasFlag(SubdivisionOptions.OffsetCenterOfMassEverything);

            if (method.HasFlag(SubdivisionMethod.CenterOfMass))
            {
                SubdivideByCenterOfMass(triangles, subDivLevel, offsetAlways);
            }
            else if (method.HasFlag(SubdivisionMethod.CenterOfHypothenuseToOpposingCorner))
            {
                SubdivideByCenterOfHypothenuseToOpposingCorner(triangles, subDivLevel);
            }
        }

        public void SubdivideByCenterOfMass(List<Triangle> triangles, int subDivLevel = 0, bool applyCoMOffset = false)
        {
            if (subDivLevel == 0)
            {
                // add only the current triangle instance
                triangles.Add(this);
            }
            else if (subDivLevel > 0)
            {
                TexturedVertex centerOfMass = this.GetCenterOfMass(applyCoMOffset);

                Triangle sub0 = new Triangle(_v0, _v1, centerOfMass);
                Triangle sub1 = new Triangle(_v1, _v2, centerOfMass);
                Triangle sub2 = new Triangle(_v2, _v0, centerOfMass);

                int nextSubDivLevel = subDivLevel - 1;
                if (nextSubDivLevel == 0)
                {
                    triangles.Add(sub0);
                    triangles.Add(sub1);
                    triangles.Add(sub2);
                }
                else
                {
                    sub0.SubdivideByCenterOfMass(triangles, nextSubDivLevel, applyCoMOffset);
                    sub1.SubdivideByCenterOfMass(triangles, nextSubDivLevel, applyCoMOffset);
                    sub2.SubdivideByCenterOfMass(triangles, nextSubDivLevel, applyCoMOffset);
                }
            }

        }
        public void SubdivideByCenterOfHypothenuseToOpposingCorner(List<Triangle> triangles, int subDivLevel = 0)
        {
            if (subDivLevel == 0)
            {
                // add only the current triangle instance
                triangles.Add(this);
            }
            else if (subDivLevel > 0)
            {
                GetHypothenuse(out TexturedVertex centerOfHypothenuse, out TexturedVertex oppositeCorner, out TexturedVertex nextCorner, out TexturedVertex lastCorner);

                Triangle sub0 = new Triangle(lastCorner, oppositeCorner, centerOfHypothenuse);
                Triangle sub1 = new Triangle(oppositeCorner, nextCorner, centerOfHypothenuse);

                int nextSubDivLevel = subDivLevel - 1;
                if (nextSubDivLevel == 0)
                {
                    triangles.Add(sub0);
                    triangles.Add(sub1);
                }
                else
                {
                    sub0.SubdivideByCenterOfHypothenuseToOpposingCorner(triangles, nextSubDivLevel);
                    sub1.SubdivideByCenterOfHypothenuseToOpposingCorner(triangles, nextSubDivLevel);
                }
            }

        }


        private void GetHypothenuse(out TexturedVertex centerOfHypothenuse, out TexturedVertex oppositeCorner, out TexturedVertex nextCorner, out TexturedVertex lastCorner)
        {
            // calculate triangle normal
            TexturedVertex e0 = _v1 - _v0;
            TexturedVertex e1 = _v2 - _v1;
            TexturedVertex e2 = _v0 - _v2;

            if (e0.sqrMagnitude > e1.sqrMagnitude
                && e0.sqrMagnitude > e2.sqrMagnitude)
            {
                centerOfHypothenuse = _v0 + e0 * 0.5f;
                oppositeCorner = _v2;
                nextCorner = _v0;
                lastCorner = _v1;
            }
            else if (e1.sqrMagnitude >= e2.sqrMagnitude)
            {
                centerOfHypothenuse = _v1 + e1 * 0.5f;
                oppositeCorner = _v0;
                nextCorner = _v1;
                lastCorner = _v2;
            }
            else
            {
                centerOfHypothenuse = _v2 + e2 * 0.5f;
                oppositeCorner = _v1;
                nextCorner = _v2;
                lastCorner = _v0;
            }
        }

        private static float[] rFactors = new float[3];
        private static Vector3 RandomVector3()
        {
            rFactors[0] = Random.Range(0.0f, RandomOffsetRange);
            rFactors[1] = Random.Range(0.0f, RandomOffsetRange - rFactors[0]);
            rFactors[2] = Random.Range(0.0f, RandomOffsetRange - rFactors[0] - rFactors[1]);
            rFactors.Shuffle();
            return new Vector3(rFactors[0], rFactors[1], rFactors[2]);
        }

    }
}