using System.IO;
using UnityEditor;
using UnityEngine;


namespace VRTX.ComputeGrass.Assets
{
    public class ParametersAsset : ScriptableObject
    {
        [SerializeField()]
        private ComputeGrass.Parameters _parameters = new ComputeGrass.Parameters();

        public static implicit operator ComputeGrass.Parameters(ParametersAsset scriptableObject) => scriptableObject._parameters;


#if UNITY_EDITOR
        private const int TMPMenuOrder = 100; // used as reference for locating menu item in asset/create menu

        [MenuItem("Assets/Create/ComputeGrass/Parameters", false, TMPMenuOrder)]
        public static void CreateParameter()
        {
            UnityEngine.Object target = Selection.activeObject;

            if (target == null || !(target is DefaultAsset))
            {
                Debug.LogWarning("A folder or asset must first be selected in order to create a definition asset.");
                return;
            }

            // Get the path to the selected asset.
            string filePath = AssetDatabase.GetAssetPath(target);
            filePath = filePath + Path.DirectorySeparatorChar + nameof(ParametersAsset) + ".asset";
            // make path unique (after ensuring it starts at Assets root folder path
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(filePath);

            // Create new definition asset
            ParametersAsset assetObject = ScriptableObject.CreateInstance<ParametersAsset>();
            AssetDatabase.CreateAsset(assetObject, uniquePath);

            // Get the Sprites contained in the Sprite Sheet
            EditorUtility.SetDirty(assetObject);
            AssetDatabase.SaveAssetIfDirty(assetObject);
        }

#endif
    }

}