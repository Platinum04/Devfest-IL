using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class LeaderboardEntry
{
    public string name;
    public int score;
    public string timestamp;
}

[Serializable]
public class LeaderboardData
{
    public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
}

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance;

    public int maxEntriesToShow = 5;
    private const string PrefsKey = "DevFestLeaderboard_v1";
    private LeaderboardData data;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        Load();
    }

    void Load()
    {
        string json = PlayerPrefs.GetString(PrefsKey, "");
        if (string.IsNullOrEmpty(json))
            data = new LeaderboardData();
        else
            data = JsonUtility.FromJson<LeaderboardData>(json);
    }

    void Save()
    {
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(PrefsKey, json);
        PlayerPrefs.Save();
    }

    public void RecordScore(string playerName, int score)
    {
        if (string.IsNullOrWhiteSpace(playerName)) playerName = "Anon";
        var entry = new LeaderboardEntry
        {
            name = playerName,
            score = score,
            timestamp = DateTime.UtcNow.ToString("s")
        };

        data.entries.Add(entry);
        // sort descending and keep maybe top 50
        data.entries = data.entries.OrderByDescending(e => e.score).ThenBy(e => e.timestamp).ToList();
        if (data.entries.Count > 50) data.entries = data.entries.Take(50).ToList();

        Save();
    }

    public List<LeaderboardEntry> GetTopEntries(int count)
    {
        return data.entries.Take(count).ToList();
    }

    // Helper to produce leaderboard text (call from UI)
    public string GetLeaderboardText(int showCount = 5)
    {
        var top = GetTopEntries(showCount);
        if (top.Count == 0) return "No scores yet. Be the first!";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < top.Count; i++)
        {
            sb.AppendLine($"{i + 1}. {top[i].name} — {top[i].score}");
        }
        return sb.ToString();
    }
}
