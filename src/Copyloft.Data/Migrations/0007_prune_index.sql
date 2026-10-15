-- 0007: the history limit only counts uncategorized unpinned clips, so pruning must
-- touch only those rows. A partial index restricted to exactly that set keeps the
-- per-copy prune proportional to the limit instead of the whole history, and a large
-- archive of pinned or categorized clips no longer slows every copy down.
CREATE INDEX IX_Clip_Prune ON Clip (CreatedAtUtc DESC, Id DESC)
WHERE IsPinned = 0 AND CategoryId IS NULL;
