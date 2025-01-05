#if UNITY_EDITOR
namespace ConfigRefs
{
    using UnityEngine;
    using UnityEditor;
    using System.Collections.Generic;

    public static class AssetDatabaseE
    {
        public static T FindSingletonAsset<T>() where T : UnityEngine.Object
        {
            var filter = string.Format("t:{0}", typeof(T).Name);
            var assetGuids = UnityEditor.AssetDatabase.FindAssets(filter);

            foreach (var assetGuid in assetGuids)
            {
                var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuid);
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
                return asset;
            }

            return null;
        }

        public static void LoadAssetsOfType<T>(List<T> results)
            where T : UnityEngine.Object
        {
            var filter = string.Format("t:{0}", typeof(T).Name);
            var assetGuids = UnityEditor.AssetDatabase.FindAssets(filter);

            foreach (var assetGuid in assetGuids)
            {
                var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuid);
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);

                if (!results.Contains(asset))
                {
                    results.Add(asset);
                }
            }
        }


        public static void LoadAssetsOfType<T>(List<T> results, params System.Type[] validTypes)
            where T : UnityEngine.Object
        {
            var filter = string.Format("t:{0}", typeof(T).Name);
            var assetGuids = UnityEditor.AssetDatabase.FindAssets(filter);

            foreach (var assetGuid in assetGuids)
            {
                var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuid);
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);

                var validType = false;
                var assetType = asset.GetType();

                foreach (var checkType in validTypes)
                {
                    if (checkType == assetType || assetType.IsSubclassOf(checkType))
                    {
                        validType = true;
                        break;
                    }
                }

                if (!validType)
                {
                    continue;
                }

                if (!results.Contains(asset))
                {
                    results.Add(asset);
                }
            }
        }
        public static T EditorFindByNameHash<T>(int nameHash) where T : ScriptableObject
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (var guid in guids)
            {
                if (string.IsNullOrEmpty(guid)) continue;
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null && asset.GetHashCode() == nameHash)
                {
                    return asset;
                }
            }

            return null;
        }

        public static void EditorFindByNameHash(int nameHash, System.Type t, out object result)
        {
            var guids = AssetDatabase.FindAssets($"t:{t.Name}");
            foreach (var guid in guids)
            {
                if (string.IsNullOrEmpty(guid)) continue;
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;
                var asset = AssetDatabase.LoadAssetAtPath(path, t);
                if (asset != null && asset is ScriptableObject && ((ScriptableObject)asset).GetHashCode() == nameHash)
                {
                    result = asset;
                    return;
                }
            }

            result = null;
            return;
        }
    }
}
#endif