using System;
using System.IO;
using System.Text;
using UnityEngine;

public class LocalCSVLogger : MonoBehaviour
{
    public static LocalCSVLogger Instance { get; private set; }
    private const string FilePrefix = "PERFORMANCE_";
    private const string FileExt = ".csv";
    private const string DefaultHeader = "\uFEFFPlayerID,Level,Stage,CueType,Timestamp,Score,AggregatedStageScore,OverallPFF,StartTime(H:M:S),StopTime(H:M:S),PlayedFor(H:M:S)";
    private string sessionFilePath = "";
    private string sessionPlayerId = "";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            string baseDir = Application.persistentDataPath;
            if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);
        }
        else Destroy(gameObject);
    }

    public void CreateSessionFileForPlayer(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            if (!string.IsNullOrEmpty(PlayerDataLogger.CurrentPlayerID))
                playerId = PlayerDataLogger.CurrentPlayerID;
            else
                playerId = "Player";
        }

        if (!string.IsNullOrEmpty(sessionFilePath) && string.Equals(sessionPlayerId, playerId, StringComparison.OrdinalIgnoreCase))
            return;

        sessionPlayerId = playerId;
        string sanitized = SanitizeFileName(playerId);
        string fname = $"{FilePrefix}{sanitized}{FileExt}";
        string path = Path.Combine(Application.persistentDataPath, fname);
        sessionFilePath = path;
        EnsureCsvExists(sessionFilePath);
    }

    public void SaveStageTimestamps(string playerId, int level, int stage, string cueTypeKey, float scoreT1_cum, float scoreT2_cum, float scoreT3_cum, int denom, DateTime startUtc, DateTime stopUtc)
    {
        if (string.IsNullOrEmpty(sessionFilePath)) CreateSessionFileForPlayer(playerId);

        float t1_inc = scoreT1_cum;
        float t2_inc = scoreT2_cum - scoreT1_cum;
        float t3_inc = scoreT3_cum - scoreT2_cum;
        if (float.IsNaN(t2_inc) || float.IsInfinity(t2_inc)) t2_inc = 0f;
        if (float.IsNaN(t3_inc) || float.IsInfinity(t3_inc)) t3_inc = 0f;
        float aggregated = t1_inc + t2_inc + t3_inc;
        string aggregatedText = $"{Mathf.Round(aggregated*10f)/10f}/{denom}";
        string aggregatedPct = denom > 0 ? $"{((aggregated / denom) * 100f):F1}%" : "0%";

        DateTime sTime = startUtc;
        DateTime eTime = stopUtc;
        TimeSpan diff = eTime - sTime;
        string startStr = sTime.ToString("HH:mm:ss");
        string stopStr = eTime.ToString("HH:mm:ss");
        string playedStr = $"{(int)diff.TotalHours:D2}:{diff.Minutes:D2}:{diff.Seconds:D2}";

        string cueType = FormatCueType(cueTypeKey);

        using (var fs = new FileStream(sessionFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
        using (var sw = new StreamWriter(fs, Encoding.UTF8))
        {
            sw.WriteLine(Line(playerId, level, stage, cueType, "T1", "'" + $"{FormatFloat(t1_inc)}/{denom}", "", "", "", "", ""));
            sw.WriteLine(Line(playerId, level, stage, cueType, "T2", "'" + $"{FormatFloat(t2_inc)}/{denom}", "'" + $"{aggregatedText} ({aggregatedPct})", "", "", "", ""));
            sw.WriteLine(Line(playerId, level, stage, cueType, "T3", "'" + $"{FormatFloat(t3_inc)}/{denom}", "", "", startStr, stopStr, playedStr));
            sw.WriteLine("●,●,●,●,●,●,●,●,●,●,●");
        }
    }

public void SaveAttemptPFF(string playerId, int level, int attemptNumber, string cueTypeKey, float pffValue, DateTime attemptStartUtc, DateTime attemptStopUtc)
{
    if (string.IsNullOrEmpty(sessionFilePath))
        CreateSessionFileForPlayer(playerId);

    string pffText = $"{pffValue:F1}%";

    // We intentionally leave every column blank except OverallPFF (column index 7)
    // Build a safe CSV line: 11 columns total -> 10 commas, value in column 7
    string line = $",,,,,,,{Esc(pffText)},,,";

    try
    {
        using (var fs = new FileStream(sessionFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
        using (var sw = new StreamWriter(fs, Encoding.UTF8))
        {
            sw.WriteLine(line);
            sw.WriteLine("●,●,●,●,●,●,●,●,●,●,●");
        }
    }
    catch (Exception ex)
    {
        Debug.LogWarning("[CSV] SaveAttemptPFF write failed: " + ex.Message);
    }

    try { PlayerDataLogger.Instance?.AddOrUpdateLevelPFF(playerId, level, pffValue); } catch { }
}




    private string Line(string pid, int level, int stage, string cue, string ts, string score, string agg, string pff, string st, string et, string play)
    {
        return $"{Esc(pid)},{level},{stage},{Esc(cue)},{Esc(ts)},{Esc(score)},{Esc(agg)},{Esc(pff)},{Esc(st)},{Esc(et)},{Esc(play)}";
    }

    private void EnsureCsvExists(string path)
    {
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (!File.Exists(path)) File.WriteAllText(path, DefaultHeader + Environment.NewLine, Encoding.UTF8);
        }
        catch { }
    }

    private string Esc(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.Contains(",") || s.Contains("\"")) s = "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }

    private string SanitizeFileName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Player";
        char[] invalid = Path.GetInvalidFileNameChars();
        var sb = new System.Text.StringBuilder();
        foreach (var ch in name)
        {
            bool ok = true;
            foreach (var inv in invalid) if (ch == inv) { ok = false; break; }
            if (ok) sb.Append(ch);
        }
        string cleaned = sb.ToString().Trim();
        if (string.IsNullOrEmpty(cleaned)) cleaned = "Player";
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", "_");
        if (cleaned.Length > 40) cleaned = cleaned.Substring(0, 40);
        return cleaned;
    }

    private string FormatCueType(string cueTypeKey)
    {
        if (string.IsNullOrEmpty(cueTypeKey)) return "No Cue";
        cueTypeKey = cueTypeKey.Trim().ToLower();
        if (cueTypeKey.Contains("nocue") || cueTypeKey == "none") return "No Cue";
        if (cueTypeKey.Contains("glow") && !cueTypeKey.Contains("arrow")) return "Glow Cue";
        if (cueTypeKey.Contains("arrow")) return "Glow + Arrow";
        return cueTypeKey;
    }

    private string FormatFloat(float v)
    {
        float r = Mathf.Round(v * 10f) / 10f;
        return r.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
    }
}
