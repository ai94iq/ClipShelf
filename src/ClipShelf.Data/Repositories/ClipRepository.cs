namespace ClipShelf.Data.Repositories;

// SQLite + Dapper. No cache: the history is small and every write already notifies open views.
public sealed class ClipRepository(SqliteConnectionFactory factory, IDataChangeNotifier changes) : IClipRepository
{
    public Task AddOrBumpAsync(string text, string? appName, DateTimeOffset copiedAtUtc, CancellationToken ct) =>
        Task.Run(() =>
        {
            using var connection = factory.Open();
            connection.Execute(
                """
                INSERT INTO Clip (Text, TextHash, AppName, CreatedAtUtc)
                VALUES (@text, @hash, @appName, @createdAt)
                ON CONFLICT(TextHash) DO UPDATE SET
                    CreatedAtUtc = excluded.CreatedAtUtc,
                    AppName      = excluded.AppName
                """,
                new { text, hash = ClipText.Hash(text), appName, createdAt = Iso(copiedAtUtc) });
            changes.Notify(new DataChanged("clip"));
        }, ct);

    public Task<IReadOnlyList<ClipListItem>> GetRecentAsync(
        PageCursor? after, int pageSize, CancellationToken ct, long? categoryId = null) =>
        Task.Run<IReadOnlyList<ClipListItem>>(() =>
        {
            using var connection = factory.Open();
            return Read(connection,
                """
                SELECT c.Id, c.Text, c.AppName, c.IsPinned, c.CreatedAtUtc, c.CategoryId,
                       cat.Name AS CategoryName
                FROM Clip c
                LEFT JOIN Category cat ON cat.Id = c.CategoryId
                WHERE (@key IS NULL
                   OR c.CreatedAtUtc < @key
                   OR (c.CreatedAtUtc = @key AND c.Id < @id))
                  AND (@categoryId IS NULL OR c.CategoryId = @categoryId)
                ORDER BY c.CreatedAtUtc DESC, c.Id DESC
                LIMIT @pageSize
                """,
                Paging(after, pageSize, categoryId));
        }, ct);

    public Task<IReadOnlyList<ClipListItem>> SearchAsync(
        string query, PageCursor? after, int pageSize, CancellationToken ct, long? categoryId = null) =>
        Task.Run<IReadOnlyList<ClipListItem>>(() =>
        {
            using var connection = factory.Open();
            return Read(connection,
                """
                SELECT c.Id, c.Text, c.AppName, c.IsPinned, c.CreatedAtUtc, c.CategoryId,
                       cat.Name AS CategoryName
                FROM Clip c
                LEFT JOIN Category cat ON cat.Id = c.CategoryId
                WHERE c.Text LIKE @like ESCAPE '\'
                  AND (@key IS NULL OR c.CreatedAtUtc < @key OR (c.CreatedAtUtc = @key AND c.Id < @id))
                  AND (@categoryId IS NULL OR c.CategoryId = @categoryId)
                ORDER BY c.CreatedAtUtc DESC, c.Id DESC
                LIMIT @pageSize
                """,
                Paging(after, pageSize, categoryId, like: $"%{EscapeLike(query)}%"));
        }, ct);

    public Task SetPinnedAsync(long id, bool pinned, CancellationToken ct) =>
        Task.Run(() =>
        {
            using var connection = factory.Open();
            connection.Execute("UPDATE Clip SET IsPinned = @pinned WHERE Id = @id;", new { id, pinned = pinned ? 1 : 0 });
            changes.Notify(new DataChanged("clip", id));
        }, ct);

    public Task AssignCategoryAsync(long clipId, long? categoryId, CancellationToken ct) =>
        Task.Run(() =>
        {
            using var connection = factory.Open();
            connection.Execute(
                "UPDATE Clip SET CategoryId = @categoryId WHERE Id = @id;", new { id = clipId, categoryId });
            changes.Notify(new DataChanged("clip", clipId));
        }, ct);

    public Task DeleteAsync(long id, CancellationToken ct) =>
        Task.Run(() =>
        {
            using var connection = factory.Open();
            connection.Execute("DELETE FROM Clip WHERE Id = @id;", new { id });
            changes.Notify(new DataChanged("clip", id));
        }, ct);

    public Task ClearUnpinnedAsync(CancellationToken ct) =>
        Task.Run(() =>
        {
            using var connection = factory.Open();
            connection.Execute("DELETE FROM Clip WHERE IsPinned = 0 AND CategoryId IS NULL;");
            changes.Notify(new DataChanged("clip"));
        }, ct);

    public Task<int> PruneAsync(int keepUnpinned, CancellationToken ct) =>
        Task.Run(() =>
        {
            using var connection = factory.Open();
            var removed = connection.Execute(
                """
                DELETE FROM Clip
                WHERE IsPinned = 0
                  AND CategoryId IS NULL
                  AND Id NOT IN (
                      SELECT Id FROM Clip
                      WHERE IsPinned = 0
                        AND CategoryId IS NULL
                      ORDER BY CreatedAtUtc DESC, Id DESC
                      LIMIT @keepUnpinned
                  );
                """,
                new { keepUnpinned });
            if (removed > 0) changes.Notify(new DataChanged("clip"));
            return removed;
        }, ct);

    public Task<int> PruneOlderThanAsync(DateTimeOffset cutoffUtc, CancellationToken ct) =>
        Task.Run(() =>
        {
            using var connection = factory.Open();
            var removed = connection.Execute(
                "DELETE FROM Clip WHERE IsPinned = 0 AND CategoryId IS NULL AND CreatedAtUtc < @cutoff;",
                new { cutoff = Iso(cutoffUtc) });
            if (removed > 0) changes.Notify(new DataChanged("clip"));
            return removed;
        }, ct);

    public int ClearUnpinned()
    {
        using var connection = factory.Open();
        var removed = connection.Execute("DELETE FROM Clip WHERE IsPinned = 0 AND CategoryId IS NULL;");
        if (removed > 0) changes.Notify(new DataChanged("clip"));
        return removed;
    }

    private static IReadOnlyList<ClipListItem> Read(SqliteConnection connection, string sql, object parameters) =>
        connection.Query<ClipRow>(sql, parameters)
            .Select(r => new ClipListItem(
                r.Id,
                r.Text,
                r.AppName,
                r.IsPinned != 0,
                DateTimeOffset.Parse(r.CreatedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                new PageCursor(r.CreatedAtUtc, r.Id),
                r.CategoryId,
                r.CategoryName))
            .ToList();

    private static object Paging(PageCursor? after, int pageSize, long? categoryId, string? like = null) =>
        new { key = after?.Key, id = after?.Id ?? 0L, pageSize, like, categoryId };

    private static string Iso(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    // Row records use only long/string so Dapper's constructor mapping matches SQLite types.
    private sealed record ClipRow(
        long Id, string Text, string? AppName, long IsPinned, string CreatedAtUtc, long? CategoryId, string? CategoryName);
}
