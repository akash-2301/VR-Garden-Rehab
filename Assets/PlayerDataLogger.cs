using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Threading;

public class PlayerDataLogger : MonoBehaviour
{
    public static PlayerDataLogger Instance { get; private set; }
    public static string CurrentPlayerID { get; private set; } = "Player";
    private string csvFilePath;
    private const string fileName = "PlayerData.csv";
    private const string PrefsKey_CurrentPlayerID = "CurrentPlayerID";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (PlayerPrefs.HasKey(PrefsKey_CurrentPlayerID)) CurrentPlayerID = PlayerPrefs.GetString(PrefsKey_CurrentPlayerID, CurrentPlayerID);
            csvFilePath = Path.Combine(Application.persistentDataPath, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(csvFilePath));
            if (!File.Exists(csvFilePath))
            {
                var header = "\uFEFFPlayerID,Age,Gender,PostStrokePeriod,StrokeDeficit,StrokeAffectedSide,CreatedAt(Date/Time),Performance";
                TryWriteAllTextExclusive(csvFilePath, header + Environment.NewLine, Encoding.UTF8);
            }
        }
        else Destroy(gameObject);
    }

    public void SavePlayerData(string playerId, string age, string gender, string postStrokePeriod, string strokeDeficit, string strokeAffectedSide)
    {
        if (string.IsNullOrWhiteSpace(playerId)) playerId = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
        string createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string line = CsvLine(playerId, age, gender, postStrokePeriod, strokeDeficit, strokeAffectedSide, createdAt, "");
        bool ok = TryAppendLineWithShare(csvFilePath, line, Encoding.UTF8);
        if (!ok)
        {
            string backup = Path.Combine(Path.GetDirectoryName(csvFilePath), $"PlayerData_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.backup.csv");
            try { File.WriteAllText(backup, "\uFEFF" + File.ReadAllText(csvFilePath, Encoding.UTF8) + Environment.NewLine + line + Environment.NewLine, Encoding.UTF8); } catch { try { File.WriteAllText(backup, "\uFEFF" + line + Environment.NewLine, Encoding.UTF8); } catch { } }
        }
        CurrentPlayerID = playerId;
        PlayerPrefs.SetString(PrefsKey_CurrentPlayerID, CurrentPlayerID);
        PlayerPrefs.Save();
    }

    private string CsvLine(string playerId, string age, string gender, string postStrokePeriod, string strokeDeficit, string strokeAffectedSide, string createdAt, string performance)
    {
        return $"{Escape(playerId)},{Escape(age)},{Escape(gender)},{Escape(postStrokePeriod)},{Escape(strokeDeficit)},{Escape(strokeAffectedSide)},{Escape(createdAt)},{Escape(performance)}";
    }

    private string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.Contains(",") || s.Contains("\"")) s = "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }

    [ContextMenu("Open Player Data Folder")]
    public void OpenDataFolder() { Application.OpenURL("file://" + Application.persistentDataPath); }
    public string GetCSVPath() => csvFilePath;

    public void AddOrUpdateLevelPFF(string playerId, int level, float pff)
    {
        if (string.IsNullOrEmpty(playerId)) playerId = CurrentPlayerID ?? "Player";
        if (level < 1 || level > 6) return;
        string key = PlayerPffKey(playerId, level);
        PlayerPrefs.SetFloat(key, pff);
        PlayerPrefs.Save();
        float avg = RecalculateAveragePerformance(playerId);
        WritePerformanceToPlayerDataCsv(playerId, avg);
    }

    private string PlayerPffKey(string playerId, int level) => $"PFF_{playerId}_L{level}";
    private float RecalculateAveragePerformance(string playerId)
    {
        float sum = 0f;
        for (int l = 1; l <= 6; l++) sum += PlayerPrefs.GetFloat(PlayerPffKey(playerId, l), 0f);
        return sum / 6f;
    }

    private void WritePerformanceToPlayerDataCsv(string playerId, float performancePercent)
    {
        try
        {
            if (!File.Exists(csvFilePath)) return;
            string[] lines = File.ReadAllLines(csvFilePath, Encoding.UTF8);
            if (lines == null || lines.Length == 0) return;
            bool updated = false;
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] parts = SplitCsvLinePreserveQuotes(line);
                if (parts.Length == 0) continue;
                string currentId = parts[0].Trim('"');
                if (string.Equals(currentId, playerId, StringComparison.OrdinalIgnoreCase))
                {
                    int needed = 8;
                    if (parts.Length < needed) Array.Resize(ref parts, needed);
                    parts[7] = $"{performancePercent:F1}%";
                    for (int j = 0; j < parts.Length; j++) parts[j] = EscapeForCsvWhenNeeded(parts[j]);
                    lines[i] = string.Join(",", parts);
                    updated = true;
                    break;
                }
            }
            if (updated) TryWriteAllTextExclusive(csvFilePath, string.Join(Environment.NewLine, lines) + Environment.NewLine, Encoding.UTF8);
        }
        catch { }
    }

    private string[] SplitCsvLinePreserveQuotes(string line)
    {
        var parts = new System.Collections.Generic.List<string>();
        bool inQuotes = false;
        var sb = new StringBuilder();
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"') { inQuotes = !inQuotes; sb.Append(c); }
            else if (c == ',' && !inQuotes) { parts.Add(sb.ToString()); sb.Length = 0; }
            else sb.Append(c);
        }
        parts.Add(sb.ToString());
        return parts.ToArray();
    }

    private string EscapeForCsvWhenNeeded(string s)
    {
        if (s == null) return "";
        if (s.Contains(",") || s.Contains("\"")) return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }

    private bool TryAppendLineWithShare(string path, string line, Encoding encoding)
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                using (var fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                using (var sw = new StreamWriter(fs, encoding)) { sw.WriteLine(line); sw.Flush(); }
                return true;
            }
            catch (IOException) { Thread.Sleep(60); continue; }
            catch { return false; }
        }
        return false;
    }

    private bool TryWriteAllTextExclusive(string path, string contents, Encoding encoding)
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                using (var sw = new StreamWriter(fs, encoding)) { sw.Write(contents); sw.Flush(); }
                return true;
            }
            catch (IOException) { Thread.Sleep(60); continue; }
            catch { return false; }
        }
        return false;
    }
}
