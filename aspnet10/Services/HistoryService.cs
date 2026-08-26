using Microsoft.Data.Sqlite;
using YTDownloaderXPro.Models;

namespace YTDownloaderXPro.Services;

public class HistoryService
{
    private readonly DatabaseService _db;

    public HistoryService(DatabaseService db) { _db = db; }

    public HistoryEntry? Add(HistoryEntry entry)
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO history (title, thumbnail, uploader, date_completed, duration_seconds,
            format_id, quality_label, size_bytes, output_path, status, session_id)
            VALUES (@t, @th, @u, @dc, @ds, @fi, @ql, @sb, @op, @s, @si)";
        cmd.Parameters.AddWithValue("@t", entry.Title);
        cmd.Parameters.AddWithValue("@th", entry.Thumbnail);
        cmd.Parameters.AddWithValue("@u", entry.Uploader);
        cmd.Parameters.AddWithValue("@dc", entry.DateCompleted > 0 ? entry.DateCompleted : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0);
        cmd.Parameters.AddWithValue("@ds", entry.DurationSeconds);
        cmd.Parameters.AddWithValue("@fi", entry.FormatId);
        cmd.Parameters.AddWithValue("@ql", entry.QualityLabel);
        cmd.Parameters.AddWithValue("@sb", entry.SizeBytes);
        cmd.Parameters.AddWithValue("@op", entry.OutputPath);
        cmd.Parameters.AddWithValue("@s", entry.Status);
        cmd.Parameters.AddWithValue("@si", entry.SessionId);
        cmd.ExecuteNonQuery();
        cmd.CommandText = "SELECT last_insert_rowid()";
        cmd.Parameters.Clear();
        entry.Id = Convert.ToInt32(cmd.ExecuteScalar());
        return entry;
    }

    public List<HistoryEntry> List(string search = "")
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        if (!string.IsNullOrEmpty(search))
        {
            cmd.CommandText = "SELECT * FROM history WHERE title LIKE @s OR uploader LIKE @s ORDER BY date_completed DESC";
            cmd.Parameters.AddWithValue("@s", $"%{search}%");
        }
        else
        {
            cmd.CommandText = "SELECT * FROM history ORDER BY date_completed DESC";
        }
        var list = new List<HistoryEntry>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(ReadEntry(reader));
        }
        return list;
    }

    public HistoryEntry? FindById(int id)
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM history WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? ReadEntry(reader) : null;
    }

    public bool Remove(int id)
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM history WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    public int CountAll()
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM history";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public long SumSizeBytes()
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(SUM(size_bytes), 0) FROM history";
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    public int CountByDayRange(double start, double end)
    {
        using var conn = _db.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM history WHERE date_completed >= @s AND date_completed < @e";
        cmd.Parameters.AddWithValue("@s", start);
        cmd.Parameters.AddWithValue("@e", end);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private HistoryEntry ReadEntry(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt32(reader.GetOrdinal("id")),
        Title = reader.GetString(reader.GetOrdinal("title")),
        Thumbnail = reader.GetString(reader.GetOrdinal("thumbnail")),
        Uploader = reader.GetString(reader.GetOrdinal("uploader")),
        DateCompleted = reader.GetDouble(reader.GetOrdinal("date_completed")),
        DurationSeconds = reader.GetInt32(reader.GetOrdinal("duration_seconds")),
        FormatId = reader.GetString(reader.GetOrdinal("format_id")),
        QualityLabel = reader.GetString(reader.GetOrdinal("quality_label")),
        SizeBytes = reader.GetInt64(reader.GetOrdinal("size_bytes")),
        OutputPath = reader.GetString(reader.GetOrdinal("output_path")),
        Status = reader.GetString(reader.GetOrdinal("status")),
        SessionId = reader.GetString(reader.GetOrdinal("session_id"))
    };
}
