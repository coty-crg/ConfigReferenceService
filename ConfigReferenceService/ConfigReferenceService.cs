namespace ConfigRefs
{
    using System.Collections.Generic;
    using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;

    [CustomEditor(typeof(ConfigReferenceService))]
    public class ConfigReferenceManagerEditor
        : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var self = target as ConfigReferenceService;

            if (GUILayout.Button("Scan"))
            {
                self.EditorRescanConfigs();
            }
        }
    }
#endif

    public class ConfigReferenceService : MonoBehaviour
    {
        public List<ConfigLookupEntry> entries = new List<ConfigLookupEntry>();
        [System.NonSerialized] private Dictionary<int, ScriptableObject> _lookupByGuid = new Dictionary<int, ScriptableObject>();

        public static ConfigReferenceService Instance;

        [System.Serializable]
        public class ConfigLookupEntry
        {
            public string guid;
            public ScriptableObject config;
        }

        private void OnEnable()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[ConfigReferenceService]: Multiple ConfigReferenceServices detected. Disabling this one.");
                this.enabled = false;
                return;
            }

            Instance = this;

            _lookupByGuid.Clear();
            foreach (var entry in entries)
            {
                var hashCode = entry.guid.GetHashCode();
                _lookupByGuid.Add(hashCode, entry.config);
            }
        }

        private void OnDisable()
        {
            if (Instance != null && Instance == this)
            {
                Instance = null;
            }
        }

        public System.Type[] ValidConfigTypes;

#if UNITY_EDITOR
        // [System.NonSerialized]
        // private static readonly System.Type[] ValidConfigTypes = new System.Type[]
        // {
        //         typeof(DataObject),
        //         typeof(AssetBundleData),
        //         typeof(InstancedSceneData),
        //         typeof(StreamingSceneMetadata),
        //         typeof(StreamingSceneData),
        // };

        public void EditorRescanConfigs()
        {
            // load all scriptable objects 
            var scriptableObjects = new List<ScriptableObject>();
            AssetDatabaseE.LoadAssetsOfType(scriptableObjects, ValidConfigTypes);

            entries.Clear();
            foreach (var scriptableObject in scriptableObjects)
            {
                var assetPath = AssetDatabase.GetAssetPath(scriptableObject);
                var guid = AssetDatabase.AssetPathToGUID(assetPath);

                entries.Add(new ConfigLookupEntry()
                {
                    config = scriptableObject,
                    guid = guid,
                });
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("ZR/Data/ConfigRefs/Refresh Base Configs")]
        public static void EditorAutoRescanConfigs()
        {
            var configService = AssetDatabaseE.FindSingletonAsset<ConfigReferenceService>();
            if (configService == null)
            {
                Debug.LogError("Couldn't find Bootstrap's config service!");
                return;
            }

            configService.EditorRescanConfigs();
        }

        public void EditorEnsureConfig<T>(ConfigReference<T> configReference) where T : ScriptableObject
        {
            for (var i = 0; i < entries.Count; ++i)
            {
                var entryLookup = entries[i];
                if (entryLookup.guid == configReference.Guid)
                {
                    return;
                }
            }

            UnityEditor.Undo.RecordObject(this, "ensure");

            entries.Add(new ConfigLookupEntry()
            {
                config = configReference.TryGetBaseConfig(),
                guid = configReference.Guid,
            });

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        public T TryFindBaseConfig<T>(ConfigReference<T> configRef) where T : ScriptableObject
        {
            return TryFindBaseConfig<T>(configRef.Guid);
        }

        public T TryFindBaseConfig<T>(string guid) where T : ScriptableObject
        {
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            if (_lookupByGuid.TryGetValue(guid.GetHashCode(), out ScriptableObject baseConfig))
            {
                return (T)baseConfig;
            }

            return null;
        }

        public T TryFindBaseConfig<T>(int guidHash) where T : ScriptableObject
        {
            if (guidHash == 0)
            {
                return null;
            }

            if (_lookupByGuid.TryGetValue(guidHash, out ScriptableObject baseConfig))
            {
                return (T)baseConfig;
            }

            return null;
        }

        public ConfigLookupEntry FindConfigLookupByName<T>(string name) where T : ScriptableObject
        {
            foreach (var entry in entries)
            {
                var entryAsT = entry.config as T;
                if (entryAsT == null)
                {
                    continue;
                }

                if (entryAsT.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }

            return null;
        }

        public T GetByName<T>(string name) where T : ScriptableObject
        {
            foreach (var entry in entries)
            {
                var entryAsT = entry.config as T;
                if (entryAsT == null)
                {
                    continue;
                }

                if (entryAsT.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return entryAsT;
                }
            }

            return null;
        }

        public List<T> TryFindAllConfigsOfType<T>() where T : ScriptableObject
        {
            var results = new List<T>();

            foreach (var entry in entries)
            {
                var entryAsT = entry.config as T;
                if (entryAsT != null)
                {
                    results.Add(entryAsT);
                }
            }

            return results;
        }

        public List<ConfigReference<T>> TryFindAllConfigsOfTypeRefs<T>() where T : ScriptableObject
        {
            var results = new List<ConfigReference<T>>();

            foreach (var entry in entries)
            {
                var entryAsT = entry.config as T;
                if (entryAsT != null)
                {
                    var configReferenceForEntry = new ConfigReference<T>(entry.guid, entry.config.name);
                    results.Add(configReferenceForEntry);
                }
            }

            return results;
        }
    }

}