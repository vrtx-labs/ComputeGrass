using UnityEngine;

namespace VRTX.ComputeGrass
{

    public class Source : MonoBehaviour
    {
        [SerializeField()]
        private Mesh _mesh = null;
        private Mesh _overrideMesh = null;

        public virtual Mesh Mesh
        {
            get
            {
                if (_overrideMesh != null)
                    return _overrideMesh;
                else
                    return _mesh;
            }
        }

        public virtual void SetMesh(Mesh originalMesh)
        {
            _mesh = originalMesh;
        }
        public virtual void SetOverrideMesh(Mesh overrideMesh)
        {
            _overrideMesh = overrideMesh;
        }

    }

}