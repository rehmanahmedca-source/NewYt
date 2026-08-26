using YTDownloaderXPro.Models;

namespace YTDownloaderXPro.Services;

public class HistoryService
{
    private readonly DatabaseService _db;

    public HistoryService(DatabaseService db) { _db = db; }

    public void Add(HistoryEntry entry) => _db.GetConnection().Insert(entry);
    public List<HistoryEntry> List(string search = "")
    {
        var db = _db.GetConnection();
        if (!string.IsNullOrEmpty(search))
            return db.Table<HistoryEntry>().Where(h => h.Title.Contains(search) || h.Uploader.Contains(search)).OrderByDescending(h => h.DateCompleted).ToList();
        return db.Table<HistoryEntry>().OrderByDescending(h => h.DateCompleted).ToList();
    }
    public HistoryEntry? FindById(int id) => _db.GetConnection().Find<HistoryEntry>(id);
    public bool Remove(int id) => _db.GetConnection().Delete<HistoryEntry>(id) > 0;
    public int CountAll() => _db.GetConnection().Table<HistoryEntry>().Count();
    public long SumSizeBytes() => _db.GetConnection().Table<HistoryEntry>().Sum(h => h.SizeBytes);
}
