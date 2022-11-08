using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace VRTX.ComputeGrass.Assets
{
    public class DefinitionAsset : ScriptableObject
    {
        [SerializeField()]
        [Range(1, 4)]
        private int _numberOfVariants = 4;
        [SerializeField()]
        private Texture2D _densityMap = null;
        [FormerlySerializedAs("_bladeParameters")]
        [SerializeField()]
        private BladeArgs[] _bladeArguments = new BladeArgs[4];
        [SerializeField()]
        private Texture2D[] _bladeTextures = new Texture2D[4];

        #region Properties
        public int NumberOfVariants => _numberOfVariants;
        public Texture2D DensityMap => _densityMap;
        public BladeArgs[] Arguments => _bladeArguments;
        public Texture2D[] Textures => _bladeTextures;
        #endregion


#if UNITY_EDITOR
        private const int TMPMenuOrder = 100; // used as reference for locating menu item in asset/create menu


        [MenuItem("Assets/Create/ComputeGrass/Definition", false, TMPMenuOrder)]
        public static void CreateDefinitionThin()
        {
            UnityEngine.Object target = Selection.activeObject;

            if (target == null || !(target is DefaultAsset))
            {
                Debug.LogWarning("A folder or asset must first be selected in order to create a definition asset.");
                return;
            }

            // Get the path to the selected asset.
            string filePath = AssetDatabase.GetAssetPath(target);
            filePath = filePath + Path.DirectorySeparatorChar + nameof(DefinitionAsset) + "_Variants.asset";
            // make path unique (after ensuring it starts at Assets root folder path
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(filePath);

            // Create new definition asset
            DefinitionAsset assetObject = ScriptableObject.CreateInstance<DefinitionAsset>();
            AssetDatabase.CreateAsset(assetObject, uniquePath);

            int variants = 4;
            assetObject._numberOfVariants = variants;
            BladeArgs[] parameters = new BladeArgs[variants];
            // set the blade parameters to the default rect thin template
            for (int i = 0; i < variants; i++)
                parameters[i] = BladeArgs.DefaultRectThin;
            assetObject._bladeArguments = parameters;
            assetObject._bladeTextures = new Texture2D[variants];


            // Get the Sprites contained in the Sprite Sheet
            EditorUtility.SetDirty(assetObject);
            AssetDatabase.SaveAssetIfDirty(assetObject);
        }

        private void OnValidate()
        {
            if (_numberOfVariants > 0 && _numberOfVariants < 8)
            {
                int prevSize = _bladeArguments.Length;
                int prevLastIndex = prevSize - 1;
                Array.Resize(ref _bladeArguments, _numberOfVariants);
                Array.Resize(ref _bladeTextures, _numberOfVariants);


                for (; prevSize < _numberOfVariants; prevSize++)
                {
                    _bladeArguments[prevSize] = _bladeArguments[prevLastIndex];
                    _bladeTextures[prevSize] = _bladeTextures[prevLastIndex];
                }

            }
        }
#endif



    }

}