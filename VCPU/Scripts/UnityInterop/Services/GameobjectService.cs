using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Provides a service for managing GameObjects in a scene, allowing them to be referenced by integer keys.
/// </summary>
public static class GameobjectService
{
    #region Types

    /// <summary>
    /// Represents a single entry in the GameobjectService, mapping an integer key to either a prefab or a scene object.
    /// </summary>
    [System.Serializable]
    public class Entry
    {
        public int key;
        public GameObject prefab;
        public string scenePath = "";
        public string objectPath = "";
    }

    /// <summary>
    /// Represents the bindings for a specific scene, including the scene index and a list of entries mapping integer keys to GameObjects.
    /// </summary>
    [System.Serializable]
    public class SceneBindings
    {
        public int sceneIndex = -1;
        public List<Entry> entries = new List<Entry>();
    }
    #endregion

    #region Fields

    // A list of scene bindings kept in-memory (not serialized when static)
    private static List<SceneBindings> sceneBindings = new List<SceneBindings>();

    /// <summary>
    /// A runtime dictionary that maps scene indices to another dictionary, which maps integer keys to GameObjects.
    /// </summary>
    private static Dictionary<int, Dictionary<int, GameObject>> sceneObjects = new Dictionary<int, Dictionary<int, GameObject>>();
    #endregion

    #region Indexer
    #endregion

    #region API
    /// <summary>
    /// Add or update a mapping from an integer key to the provided GameObject for the active scene.
    /// If an entry with the same key exists it will be updated; otherwise a new entry is created.
    /// </summary>
    public static void AddObject(int index, GameObject value)
    {
        //If value is null, remove existing entry for this key (matches previous indexer behavior).
        if (value == null)
        {
            RemoveObject(index);
            return;
        }
        var binding = FindOrCreateBindingsForActiveScene();
        if (binding == null)
            return;

        //If this GameObject is already registered under a different key, remove that registration
        int existingKeyForObject = GetIndex(value);
        if (existingKeyForObject != -1 && existingKeyForObject != index)
        {
            //Log and remove previous entry so the object is only registered once
            Debug.LogWarning($"GameObject '{value.name}' is already registered with key {existingKeyForObject}. Removing previous registration to assign new key {index}.");
            binding.entries.RemoveAll(e => e != null && e.key == existingKeyForObject);
        }

        var existing = binding.entries.FirstOrDefault(e => e.key == index);
        if (existing != null)
        {
            //update existing
            if (value.scene.IsValid())
            {
                existing.prefab = null;
                existing.scenePath = value.scene.path;
                var parts = new List<string>();
                var t = value.transform;
                while (t != null)
                {
                    parts.Insert(0, t.name);
                    t = t.parent;
                }
                existing.objectPath = string.Join("/", parts.ToArray());
            }
            else
            {
                existing.prefab = value;
                existing.scenePath = "";
                existing.objectPath = "";
            }
        }
        else //If the object is not already registered under this key, create a new entry
        {
            var entry = new Entry { key = index };
            if (value.scene.IsValid())
            {
                entry.prefab = null;
                entry.scenePath = value.scene.path;
                var parts = new List<string>();
                var t = value.transform;
                while (t != null)
                {
                    parts.Insert(0, t.name);
                    t = t.parent;
                }
                entry.objectPath = string.Join("/", parts.ToArray());
            }
            else
            {
                entry.prefab = value;
                entry.scenePath = "";
                entry.objectPath = "";
            }

            binding.entries.Add(entry);
        }
        BuildDictionary();
    }

    /// <summary>
    /// Add the provided GameObject to the active scene bindings and automatically choose an unused integer key.
    /// Returns the assigned key, or -1 on failure.
    /// </summary>
    public static int AddObject(GameObject value)
    {
        if (value == null)
            return -1;

        var binding = FindOrCreateBindingsForActiveScene();
        if (binding == null)
            return -1;

        //Do not add if already registered; return existing key instead.
        int existing = GetIndex(value);
        if (existing != -1)
        {
            Debug.LogWarning($"GameObject '{(value == null ? "<null>" : value.name)}' is already registered with key {existing}. AddObject(GameObject) returning existing key.");
            return existing;
        }

        int key = FindNextAvailableKey(binding);
        AddObject(key, value);
        return key;
    }

    /// <summary>
    /// Get the integer key currently associated with the provided GameObject in the active scene.
    /// Returns -1 if not found.
    /// </summary>
    public static int GetIndex(GameObject value)
    {
        if (value == null)
            return -1;

        var binding = FindOrCreateBindingsForActiveScene();
        if (binding == null)
            return -1;

        //Ensure runtime dictionary is up to date and check direct runtime references first.
        BuildDictionary();
        var sceneIndex = GetActiveSceneIndex();
        if (sceneIndex >= 0 && sceneObjects.TryGetValue(sceneIndex, out var runtimeDict))
        {
            foreach (var kvp in runtimeDict)
            {
                if (kvp.Value != null && ReferenceEquals(kvp.Value, value))
                    return kvp.Key;
            }
        }

        //First try to match prefabs directly
        for (int i = 0; i < binding.entries.Count; i++)
        {
            var entry = binding.entries[i];
            if (entry == null) continue;
            if (entry.prefab != null && ReferenceEquals(entry.prefab, value))
                return entry.key;
        }

        //Then try to match scene objects by resolving stored path
        for (int i = 0; i < binding.entries.Count; i++)
        {
            var e = binding.entries[i];
            if (e == null) continue;
            if (string.IsNullOrEmpty(e.scenePath) || string.IsNullOrEmpty(e.objectPath)) continue;
            var scene = SceneManager.GetSceneByPath(e.scenePath);
            if (!scene.IsValid() || !scene.isLoaded) continue;
            var go = FindInScene(scene, e.objectPath);
            if (go != null && ReferenceEquals(go, value))
                return e.key;
        }

        return -1;
    }

    /// <summary>
    /// Returns the next available key for the active scene without creating an entry. Returns -1 on failure.
    /// </summary>
    public static int GetNextAvailableIndex()
    {
        var binding = FindOrCreateBindingsForActiveScene();
        if (binding == null)
            return -1;
        return FindNextAvailableKey(binding);
    }

    /// <summary>
    /// Find the smallest non-negative integer key not used by the binding
    /// </summary>
    /// <param name="binding">The scene bindings to check for used keys.</param>
    /// <returns>The smallest non-negative integer key not used by the binding.</returns>
    private static int FindNextAvailableKey(SceneBindings binding)
    {
        if (binding == null)
            return 0;
        var used = new HashSet<int>(binding.entries.Select(e => e != null ? e.key : -1));
        int k = 0;
        while (used.Contains(k)) k++;
        return k;
    }

    /// <summary>
    /// Remove an entry for the given key from the active scene bindings.
    /// </summary>
    public static void RemoveObject(int index)
    {
        var binding = FindOrCreateBindingsForActiveScene();
        if (binding == null)
            return;

        var existing = binding.entries.FirstOrDefault(e => e.key == index);
        if (existing != null)
        {
            binding.entries.Remove(existing);
            BuildDictionary();
        }
    }
    #endregion

    #region Properties
    public static int Count
    {
        get
        {
            var sceneIndex = GetActiveSceneIndex();
            if (sceneIndex < 0)
                return 0;
            if (sceneObjects.TryGetValue(sceneIndex, out var dict))
                return dict.Count;
            return 0;
        }
    }

    public static IEnumerable<int> Keys
    {
        get
        {
            var sceneIndex = GetActiveSceneIndex();
            if (sceneIndex < 0)
                return Enumerable.Empty<int>();
            if (sceneObjects.TryGetValue(sceneIndex, out var dict))
                return dict.Keys;
            return Enumerable.Empty<int>();
        }
    }

    public static IEnumerable<GameObject> Values
    {
        get
        {
            var sceneIndex = GetActiveSceneIndex();
            if (sceneIndex < 0)
                return Enumerable.Empty<GameObject>();
            if (sceneObjects.TryGetValue(sceneIndex, out var dict))
                return dict.Values;
            return Enumerable.Empty<GameObject>();
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Retrieves a GameObject associated with the given integer key for the currently active scene.
    /// </summary>
    /// <param name="index">The integer key associated with the desired GameObject.</param>
    /// <returns>The GameObject if found; otherwise, null.</returns>
    public static GameObject GetObject(int index)
    {
        var sceneIndex = GetActiveSceneIndex();
        if (sceneIndex < 0)
            return null;

        if (sceneObjects.TryGetValue(sceneIndex, out var dict))
        {
            if (dict.TryGetValue(index, out var go))
                return go;
        }

        return null;
    }

    /// <summary>
    /// Destroys the GameObject associated with the given integer key for the currently active scene, if it exists.
    /// </summary>
    /// <param name="index">The integer key associated with the GameObject to be destroyed.</param>
    /// <returns>True if the GameObject was found and destroyed; otherwise, false.</returns>
    public static bool DestroyGameobject(int index)
    {
        var sceneIndex = GetActiveSceneIndex();
        if (sceneIndex < 0)
            return false;

        if (sceneObjects.TryGetValue(sceneIndex, out var dict))
        {
            if (dict.TryGetValue(index, out var go))
            {
                if (go != null)
                    Object.Destroy(go);
                dict.Remove(index);
                return true;
            }
        }

        return false;
    }
    
    /// <summary>
    /// Clears all GameObject bindings for the currently active scene.
    /// </summary>
    public static void Clear()
    {
        var binding = FindOrCreateBindingsForActiveScene();
        if (binding != null)
        {
            binding.entries.Clear();
            BuildDictionary();
        }
    }
    #endregion

    #region Helpers
    /// <summary>
    /// Builds the runtime dictionary of GameObjects for each scene based on the serialized scene bindings.
    /// </summary>
    private static void BuildDictionary()
    {
        sceneObjects.Clear();

        foreach (var sb in sceneBindings)
        {
            if (sb == null)
                continue;

            int key = sb.sceneIndex;
            if (key < 0)
                continue;

            if (!sceneObjects.TryGetValue(key, out var dict))
            {
                dict = new Dictionary<int, GameObject>();
                sceneObjects[key] = dict;
            }

            foreach (var entry in sb.entries)
            {
                if (entry == null)
                    continue;

                if (entry.prefab != null)
                {
                    dict[entry.key] = entry.prefab;
                    continue;
                }

                if (!string.IsNullOrEmpty(entry.scenePath) && !string.IsNullOrEmpty(entry.objectPath))
                {
                    var scene = SceneManager.GetSceneByPath(entry.scenePath);
                    if (scene.IsValid() && scene.isLoaded)
                    {
                        var go = FindInScene(scene, entry.objectPath);
                        if (go != null)
                        {
                            dict[entry.key] = go;
                            continue;
                        }
                    }
                }

                dict[entry.key] = null;
            }
        }
    }

    /// <summary>
    /// Finds a GameObject in the specified scene based on a hierarchical path (entry.g., "Root/Child/Grandchild").
    /// </summary>
    /// <param name="scene">The scene to search in.</param>
    /// <param name="path">The hierarchical path to the GameObject.</param>
    /// <returns>The GameObject if found; otherwise, null.</returns>
    private static GameObject FindInScene(Scene scene, string path)
    {
        if (!scene.IsValid())
            return null;

        var parts = path.Split('/');
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            if (root.name != parts[0])
                continue;

            GameObject cur = root;
            bool failed = false;
            for (int i = 1; i < parts.Length; i++)
            {
                var next = cur.transform.Find(parts[i]);
                if (next == null)
                {
                    failed = true;
                    break;
                }
                cur = next.gameObject;
            }

            if (!failed)
                return cur;
        }

        return null;
    }

    /// <summary>
    /// Gets the index of the currently active scene, using the build index if available, or a hash of the scene path as a fallback.
    /// </summary>
    /// <returns>The index of the currently active scene, or -1 if the scene is invalid.</returns>
    private static int GetActiveSceneIndex()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return -1;
        if (scene.buildIndex >= 0)
            return scene.buildIndex;
        return scene.path.GetHashCode();
    }

    /// <summary>
    /// Finds the SceneBindings for the currently active scene, or creates a new one if it doesn't exist. 
    /// The scene is identified by its build index if available, or by a hash of its path as a fallback.
    /// </summary>
    /// <returns>The SceneBindings for the active scene, or null if the scene is invalid.</returns>
    private static SceneBindings FindOrCreateBindingsForActiveScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return null;

        int key = scene.buildIndex >= 0 ? scene.buildIndex : scene.path.GetHashCode();

        var existing = sceneBindings.FirstOrDefault(sb => sb.sceneIndex == key);
        if (existing != null)
            return existing;

        var sbNew = new SceneBindings { sceneIndex = key };
        sceneBindings.Add(sbNew);
        return sbNew;
    }
    #endregion
}
