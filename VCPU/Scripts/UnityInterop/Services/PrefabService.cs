using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// ScriptableObject container that stores index -> prefab mappings and is editable in the inspector.
/// Create an asset (recommended in a Resources folder named "PrefabServiceData" so it is auto-loaded at runtime),
/// or assign the asset to PrefabService.Data at runtime.
/// </summary>
[CreateAssetMenu(fileName = "PrefabServiceData", menuName = "VCPU/Prefab Service Data")]
public class PrefabServiceData : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        /// <summary>
        /// Human readable name used only for inspector readability.
        /// </summary>
        public string name;

        /// <summary>
        /// Integer key that identifies this entry.
        /// </summary>
        public int key;

        /// <summary>
        /// Prefab associated with this entry.
        /// </summary>
        public GameObject prefab;
    }

    /// <summary>
    /// Maximum number of entries supported by the PrefabService.
    /// </summary>
    public const int MaxEntries = 256;

    /// <summary>
    /// Fixed-size array that stores serialized entries for inspector editing.
    /// </summary>
    public Entry[] entries = new Entry[MaxEntries];
}

/// <summary>
/// Static runtime accessor for prefabs stored in a PrefabServiceData ScriptableObject.
/// Mirrors the behaviour of GameobjectService but only for prefabs and editable via inspector.
/// </summary>
public static class PrefabService
{
    /// <summary>
    /// Name of the Resources asset to load.
    /// </summary>
    private const string resourcesName = "PrefabServiceData";

    /// <summary>
    /// Cached runtime reference to the loaded PrefabServiceData asset.
    /// </summary>
    private static PrefabServiceData data;

    /// <summary>
    /// Runtime backing array indexed by key. Fixed size equal to PrefabServiceData.MaxEntries.
    /// </summary>
    private static GameObject[] runtimeArray = new GameObject[PrefabServiceData.MaxEntries];

    /// <summary>
    /// Reentrancy guard used while initializing Data to prevent recursive loads.
    /// </summary>
    private static bool isInitializing = false;

    /// <summary>
    /// Accessor for the PrefabServiceData asset. Loads the asset from Resources if necessary.
    /// </summary>
    public static PrefabServiceData Data
    {
        get
        {
            if (data == null)
            {
                if (isInitializing)
                    return data;
                isInitializing = true;
                try
                {
                    Debug.Log($"PrefabService: attempting Resources.Load('{resourcesName}')");
                    var assetLoaded = Resources.Load<PrefabServiceData>(resourcesName);
                    if (assetLoaded != null)
                    {
                        Debug.Log($"PrefabService: Resources.Load returned asset name='{assetLoaded.name}'");
                        data = assetLoaded;
#if UNITY_EDITOR
                        int prefabReferencesFound = 0;
                        if (data.entries != null)
                        {
                            for (int entryIndexLoaded = 0; entryIndexLoaded < data.entries.Length; entryIndexLoaded++)
                            {
                                var loadedEntry = data.entries[entryIndexLoaded];
                                if (loadedEntry != null && loadedEntry.prefab != null) prefabReferencesFound++;
                            }
                        }
                        if (prefabReferencesFound == 0)
                        {
                            var allAssets = Resources.LoadAll<PrefabServiceData>("");
                            if (allAssets != null && allAssets.Length > 0)
                            {
                                for (int assetIndex = 0; assetIndex < allAssets.Length; assetIndex++)
                                {
                                    var candidateAsset = allAssets[assetIndex];
                                    int candidateRefs = 0;
                                    if (candidateAsset.entries != null)
                                    {
                                        for (int candidateEntryIndex = 0; candidateEntryIndex < candidateAsset.entries.Length; candidateEntryIndex++)
                                        {
                                            var candidateEntry = candidateAsset.entries[candidateEntryIndex];
                                            if (candidateEntry != null && candidateEntry.prefab != null) candidateRefs++;
                                        }
                                    }
                                    if (candidateRefs > 0)
                                    {
                                        data = candidateAsset;
                                        Debug.LogWarning($"PrefabService: Resources.Load returned asset with no prefab refs; switching to candidate '{data.name}' with {candidateRefs} prefab refs.");
                                        break;
                                    }
                                }
                            }
                        }
#endif
                    }
                    else
                    {
                        Debug.Log("PrefabService: Resources.Load returned null; trying Resources.LoadAll<PrefabServiceData>(\"\")");
                        var allAssets = Resources.LoadAll<PrefabServiceData>("");
                        Debug.Log($"PrefabService: Resources.LoadAll returned { (allAssets == null ? 0 : allAssets.Length) } items");
                        if (allAssets != null && allAssets.Length > 0)
                        {
                            data = allAssets[0];
                            Debug.LogWarning($"PrefabService: using first PrefabServiceData found in Resources ('{data.name}'). Consider renaming the asset to '{resourcesName}'.");
                        }
                        else
                        {
#if UNITY_EDITOR
                            var instance = ScriptableObject.CreateInstance<PrefabServiceData>();
                            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                            {
                                AssetDatabase.CreateFolder("Assets", "Resources");
                            }
                            var path = Path.Combine("Assets/Resources", resourcesName + ".asset");
                            AssetDatabase.CreateAsset(instance, path);
                            AssetDatabase.SaveAssets();
                            EditorUtility.SetDirty(instance);
                            data = instance;
#else
                            data = ScriptableObject.CreateInstance<PrefabServiceData>();
#endif
                        }
                    }

                    EnsureEntriesInitialized();
#if UNITY_EDITOR
                    try
                    {
                        var assetPath = AssetDatabase.GetAssetPath(data);
                        Debug.Log($"PrefabService: asset path='{assetPath}' isPlaying={Application.isPlaying}");

                        int prefabRefs = 0;
                        if (data.entries != null)
                        {
                            for (int entryIndex = 0; entryIndex < data.entries.Length; entryIndex++)
                            {
                                var entryItem = data.entries[entryIndex];
                                if (entryItem != null && entryItem.prefab != null) prefabRefs++;
                            }
                        }
                        Debug.Log($"PrefabService: Data.entries length={(data.entries==null?0:data.entries.Length)} prefabRefs={prefabRefs}");
                    }
                    catch { }
#endif
                    BuildDictionary();
                }
                finally
                {
                    isInitializing = false;
                }
            }
            return data;
        }
        set
        {
            if (data == value) return;
            data = value;
            EnsureEntriesInitialized();
            BuildDictionary();
        }
    }

    /// <summary>
    /// Ensures the Data.entries array exists and has the expected fixed size. If the existing array
    /// has a different length, its contents are copied into a newly allocated array of size MaxEntries.
    /// </summary>
    private static void EnsureEntriesInitialized()
    {
        if (data == null) return;
        if (data.entries == null || data.entries.Length != PrefabServiceData.MaxEntries)
        {
            var newArr = new PrefabServiceData.Entry[PrefabServiceData.MaxEntries];
            if (data.entries != null)
            {
                int copy = Mathf.Min(newArr.Length, data.entries.Length);
                for (int copyIndex = 0; copyIndex < copy; copyIndex++) newArr[copyIndex] = data.entries[copyIndex];
            }
            data.entries = newArr;
#if UNITY_EDITOR
            EditorUtility.SetDirty(data);
#endif
        }
    }

    /// <summary>
    /// Returns the prefab associated with the given index, or null if not found.
    /// </summary>
    /// <param name="index">The index of the prefab to retrieve.</param>
    /// <returns>The prefab associated with the given index, or null if not found.</returns>
    public static GameObject GetPrefab(int index)
    {
        if (Data == null) return null;
        if (index < 0 || index >= PrefabServiceData.MaxEntries) return null;

        var go = runtimeArray[index];
        if (go != null) return go;

        if (Data.entries != null)
        {
            for (int entryIndex = 0; entryIndex < Data.entries.Length; entryIndex++)
            {
                var entryItem = Data.entries[entryIndex];
                if (entryItem == null) continue;
                if (entryItem.key == index)
                    return entryItem.prefab;
            }
        }

        return null;
    }

    /// <summary>
    /// Adds or updates the prefab associated with the given index. If the index already exists, its prefab will be updated.
    /// </summary>
    /// <param name="index">The index of the prefab to add or update.</param>
    /// <param name="prefab">The prefab to associate with the given index.</param>
    public static void AddPrefab(int index, GameObject prefab)
    {
        if (Data == null) return;

        int existingKey = GetIndex(prefab);
        if (existingKey != -1 && existingKey != index)
        {
            for (int entryIndex = 0; entryIndex < Data.entries.Length; entryIndex++)
            {
                var existingEntry = Data.entries[entryIndex];
                if (existingEntry != null && existingEntry.key == existingKey)
                    Data.entries[entryIndex] = null;
            }
        }

        // find existing entry with target index
        int foundIndex = -1;
        for (int entryIndex = 0; entryIndex < Data.entries.Length; entryIndex++)
        {
            var existingEntry = Data.entries[entryIndex];
            if (existingEntry != null && existingEntry.key == index)
            {
                foundIndex = entryIndex;
                break;
            }
        }

        if (foundIndex != -1)
        {
            Data.entries[foundIndex].prefab = prefab;
        }
        else
        {
            // find first empty slot
            int slot = -1;
            for (int slotSearchIndex = 0; slotSearchIndex < Data.entries.Length; slotSearchIndex++)
            {
                if (Data.entries[slotSearchIndex] == null)
                {
                    slot = slotSearchIndex;
                    break;
                }
            }
            if (slot != -1)
            {
                Data.entries[slot] = new PrefabServiceData.Entry { key = index, prefab = prefab };
            }
            else
            {
                Debug.LogWarning($"PrefabService: cannot add prefab for key {index} - entries array is full (max {PrefabServiceData.MaxEntries}).");
            }
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
            EditorUtility.SetDirty(Data);
#endif

        BuildDictionary();
    }

    /// <summary>
    /// Adds the given prefab to the service and returns the assigned index. If the prefab already exists, returns the existing index.
    /// </summary>
    /// <param name="prefab">The prefab to add to the service.</param>
    /// <returns>The index assigned to the prefab, or the existing index if the prefab already exists.</returns>
    public static int AddPrefab(GameObject prefab)
    {
        if (prefab == null) return -1;
        if (Data == null) return -1;

        int existing = GetIndex(prefab);
        if (existing != -1)
            return existing;

        int key = FindNextAvailableKey();
        AddPrefab(key, prefab);
        return key;
    }

    /// <summary>
    /// Gets the index associated with the given prefab, or -1 if not found. Uses reference equality to match prefabs.
    /// </summary>
    /// <param name="prefab">The prefab to find the index for.</param>
    /// <returns>The index associated with the given prefab, or -1 if not found.</returns>
    public static int GetIndex(GameObject prefab)
    {
        if (prefab == null) return -1;
        if (Data == null) return -1;

        BuildDictionary();
        for (int i = 0; i < runtimeArray.Length; i++)
        {
            var val = runtimeArray[i];
            if (val != null && ReferenceEquals(val, prefab))
                return i;
        }

        foreach (var entryItem in Data.entries)
        {
            if (entryItem == null) continue;
            if (entryItem.prefab != null && ReferenceEquals(entryItem.prefab, prefab))
                return entryItem.key;
        }

        return -1;
    }

    /// <summary>
    /// Removes the prefab associated with the given index from the service. Does nothing if the index is not found.
    /// </summary>
    /// <param name="index">The index of the prefab to remove.</param>
    public static void RemovePrefab(int index)
    {
        if (Data == null) return;

        int found = -1;
        for (int i = 0; i < Data.entries.Length; i++)
        {
            var entryItem = Data.entries[i];
            if (entryItem != null && entryItem.key == index)
            {
                found = i;
                break;
            }
        }
        if (found != -1)
        {
            Data.entries[found] = null;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                EditorUtility.SetDirty(Data);
#endif
            BuildDictionary();
        }
    }

    /// <summary>
    /// Returns the next available index that can be used to add a new prefab. 
    /// This is the smallest non-negative integer that is not currently used as a key in the service.
    /// </summary>
    /// <returns>The next available index that can be used to add a new prefab, or -1 if the data is null.</returns>
    public static int GetNextAvailableIndex()
    {
        if (Data == null) return -1;
        return FindNextAvailableKey();
    }

    /// <summary>
    /// Finds the next available key by checking existing keys in the data entries.
    /// </summary>
    /// <returns>The next available key that can be used to add a new prefab, or 0 if the data is null.</returns>
    private static int FindNextAvailableKey()
    {
        if (Data == null) return 0;
        var used = new HashSet<int>();
        foreach (var entryItem in Data.entries)
        {
            if (entryItem == null) continue;
            used.Add(entryItem.key);
        }
        int k = 0;
        while (used.Contains(k)) k++;
        return k;
    }

    public static int Count
    {
        get
        {
            int c = 0;
            for (int arrayIndex = 0; arrayIndex < runtimeArray.Length; arrayIndex++) 
                if (runtimeArray[arrayIndex] != null) 
                    c++;
            return c;
        }
    }

    public static IEnumerable<int> Keys
    {
        get
        {
            var list = new List<int>();
            for (int arrayIndex = 0; arrayIndex < runtimeArray.Length; arrayIndex++) if (runtimeArray[arrayIndex] != null) list.Add(arrayIndex);
            return list;
        }
    }

    public static IEnumerable<GameObject> Values
    {
        get
        {
            var list = new List<GameObject>();
            for (int arrayIndex = 0; arrayIndex < runtimeArray.Length; arrayIndex++) if (runtimeArray[arrayIndex] != null) list.Add(runtimeArray[arrayIndex]);
            return list;
        }
    }
    /// <summary>
    /// Instantiates the prefab associated with the given index.
    /// Returns the instantiated GameObject or null if the prefab/index is not found.
    /// Adds the created instance to the GameobjectService runtime bindings.
    /// </summary>
    /// <param name="index">Index of the prefab to spawn.</param>
    /// <param name="position">World-space position for the spawned object.</param>
    /// <param name="rotation">Rotation for the spawned object.</param>
    /// <param name="parent">Optional parent transform for the spawned object.</param>
    /// <returns>The instantiated GameObject or null on failure.</returns>
    public static GameObject SpawnObjectPrefab(int index, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        var prefab = GetPrefab(index);
        if (prefab == null)
            return null;

        GameObject instance;
        if (parent != null)
            instance = Object.Instantiate(prefab, position, rotation, parent);
        else
            instance = Object.Instantiate(prefab, position, rotation);

        GameobjectService.AddObject(index, instance);

        return instance;
    }

    /// <summary>
    /// Convenience overload that instantiates the prefab at the origin with no rotation.
    /// </summary>
    public static GameObject SpawnObjectPrefab(int index) => SpawnObjectPrefab(index, Vector3.zero, Quaternion.identity, null);

    public static void Clear()
    {
        if (Data == null) return;
        for (int i = 0; i < Data.entries.Length; i++)
            Data.entries[i] = null;
#if UNITY_EDITOR
        if (!Application.isPlaying)
            EditorUtility.SetDirty(Data);
#endif
        // Clear runtime array as well
        for (int clearIndex = 0; clearIndex < runtimeArray.Length; clearIndex++) runtimeArray[clearIndex] = null;
        BuildDictionary();
    }

    /// <summary>
    /// Rebuilds the runtime array from the serialized data entries.
    /// Split into smaller helpers for readability and testability.
    /// </summary>
    private static void BuildDictionary()
    {
        ClearRuntimeArray();
        if (Data == null) return;

        int total = Data.entries == null ? 0 : Data.entries.Length;
        int nonNull = ProcessEntries(total);

        int runtimeCount = CountRuntimeNonNull();
        Debug.Log($"PrefabService: BuildDictionary completed. entries total={total} nonNull={nonNull} runtimeArray.count={runtimeCount}");
    }

    /// <summary>
    /// Clears the runtime backing array by setting all slots to null.
    /// </summary>
    private static void ClearRuntimeArray()
    {
        for (int i = 0; i < runtimeArray.Length; i++) runtimeArray[i] = null;
    }

    /// <summary>
    /// Processes the serialized entries array and populates the runtimeArray accordingly.
    /// Returns the number of non-null serialized entries encountered.
    /// </summary>
    /// <param name="total">Number of serialized entries to process (typically Data.entries.Length).</param>
    /// <returns>Count of non-null serialized entries encountered.</returns>
    private static int ProcessEntries(int total)
    {
        int nonNull = 0;
        for (int entryIndex = 0; entryIndex < total; entryIndex++)
        {
            var dataEntry = Data.entries[entryIndex];
            if (dataEntry == null) continue;
            nonNull++;

            if (dataEntry.key < 0 || dataEntry.key >= PrefabServiceData.MaxEntries)
            {
                Debug.LogWarning($"PrefabService: BuildDictionary - entry[{entryIndex}] has out-of-range key {dataEntry.key}");
                continue;
            }

            if (dataEntry.prefab != null)
            {
                runtimeArray[dataEntry.key] = dataEntry.prefab;
                if (nonNull <= 10)
                    Debug.Log($"PrefabService: BuildDictionary - entry[{entryIndex}] key={dataEntry.key} prefab={dataEntry.prefab.name}");
            }
            else
            {
                if (nonNull <= 10)
                    Debug.Log($"PrefabService: BuildDictionary - entry[{entryIndex}] key={dataEntry.key} prefab=null (skipping overwrite)");
            }
        }
        return nonNull;
    }

    /// <summary>
    /// Counts non-null entries in the runtime array.
    /// </summary>
    /// <returns>The number of non-null slots in runtimeArray.</returns>
    private static int CountRuntimeNonNull()
    {
        int runtimeCount = 0;
        for (int i = 0; i < runtimeArray.Length; i++) if (runtimeArray[i] != null) runtimeCount++;
        return runtimeCount;
    }
}
