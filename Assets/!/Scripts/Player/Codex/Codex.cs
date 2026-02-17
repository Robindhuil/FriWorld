using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class CodexEntry
{
    public string id;
    public string category;
    public string name;
    public string description;
    public string photo;
    public string video;

    public override bool Equals(object obj)
    {
        if (obj is CodexEntry other)
        {
            return id == other.id;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return id != null ? id.GetHashCode() : 0;
    }
}

public class Codex : MonoBehaviour
{
    public static Codex Instance { get; private set; }
    public List<CodexEntry> Entries { get; private set; }
    private HashSet<string> unlockedCategories = new HashSet<string>();
    private HashSet<CodexEntry> unlockedEntries = new HashSet<CodexEntry>();
    public event System.Action OnCodexUpdated;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadCodex();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadCodex()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Codex");
        if (jsonFile != null)
        {
            var loadedEntries = JsonUtility.FromJson<ListWrapper>(jsonFile.text).entries;
            Entries = loadedEntries.Distinct().ToList();
            Debug.Log($"[CodexEntry] Loaded {Entries.Count} codex entries from Resources");
        }
        else
        {
            Debug.LogError("[CodexEntry] Codex.json not found in Resources folder");
            Entries = new List<CodexEntry>();
        }
    }

    public IEnumerable<string> GetUnlockedCategories() => unlockedCategories;

    public IEnumerable<CodexEntry> GetUnlockedEntries(string category)
    {
        return unlockedEntries
            .Where(e => e.category == category)
            .GroupBy(e => e.name)
            .Select(g => g.First());
    }

    public void UnlockEntry(CodexEntry entry)
    {
        if (entry == null) return;

        if (!unlockedEntries.Any(e => e.id == entry.id))
        {
            unlockedEntries.Add(entry);
            unlockedCategories.Add(entry.category);
            OnCodexUpdated?.Invoke();
            Debug.Log($"[CodexEntry] Unlocked entry: {entry.name} (ID: {entry.id}, Category: {entry.category})");
        }
    }

    public bool IsEntryUnlocked(CodexEntry entry)
    {
        return unlockedEntries.Contains(entry);
    }

    [System.Serializable]
    private class ListWrapper
    {
        public List<CodexEntry> entries;
    }
}