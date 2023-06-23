using System.Collections.Generic;
using UnityEngine;
using VRTX.ComputeGrass.Assets;

namespace VRTX.ComputeGrass
{
    [ExecuteInEditMode]
    public class BatchProcessor : MonoBehaviour
    {
        [SerializeField()]
        private BatchAsset _batch = null;

        private List<Grass> _grasses = new List<Grass>();


        private void OnEnable()
        {
            // clear & destroy previous references
            this.DestroyPreviousObjects();
            // check if batch is not null
            if (_batch != null && _batch.IsValid)
            {
                // create new grass components for each mesh in the batch
                foreach (Mesh m in _batch.Meshes)
                {
                    if (m == null)
                        continue;

                    Grass grass = Grass.Create(_batch.Definition, _batch.Parameters, m, this.transform);
                    _grasses.Add(grass);
                    grass.gameObject.SetActive(true);
                }
            }

            SetGrassesActive(true);
        }
        private void OnDisable()
        {
            SetGrassesActive(false);
        }

        private void SetGrassesActive(bool active)
        {
            foreach (Grass g in _grasses)
                g.gameObject.SetActive(active);
        }


        private void DestroyPreviousObjects()
        {
#if UNITY_EDITOR
            // check of application is playing (means either runtime or play mode in editor)
            if (Application.isPlaying)
            {
                // iterate over all references and delete them
                for (int i = _grasses.Count - 1; i >= 0; i--)
                    if (_grasses[i] != null)
                        GameObject.Destroy(_grasses[i].gameObject);
            }
            // or not (edit-mode)
            else
            {
                // iterate over all references and delete them
                for (int i = _grasses.Count - 1; i >= 0; i--)
                    if (_grasses[i] != null)
                        GameObject.DestroyImmediate(_grasses[i].gameObject);
            }

            // clear reference list from any remaining entries
            _grasses.Clear();
#endif
        }


#if UNITY_EDITOR
        // in-editor cached batch reference to detect change of batch and re-creation of ComputeGrass components
        private BatchAsset _prevBatch = null;
        // in-editor cached number of mesh of prev batch to detect change of batch and re-creation of ComputeGrass components
        private int _prevBatchMeshCount = 0;

        private void Update()
        {
            if (!Application.isPlaying)
            {
                if (_prevBatch != _batch ||
                    (_batch != null && _prevBatchMeshCount != _batch.Meshes.Count))
                {
                    this.DestroyPreviousObjects();
                    // changed batch info
                    _prevBatch = _batch;
                    _prevBatchMeshCount = _batch != null ? _batch.Meshes.Count : 0;

                    if (_batch != null && _batch.IsValid)
                    {
                        foreach (Mesh m in _batch.Meshes)
                        {
                            if (m == null)
                                continue;

                            Grass grass = Grass.Create(_batch.Definition, _batch.Parameters, m, this.transform);
                            _grasses.Add(grass);
                            grass.gameObject.SetActive(true);
                        }
                    }
                }
            }
        }

#endif
    }


}