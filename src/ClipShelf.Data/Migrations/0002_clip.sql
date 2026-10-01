-- 0002: clipboard history. TextHash is unique so re-copying an existing clip
-- moves it to the top instead of inserting a duplicate.
CREATE TABLE Clip (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    Text         TEXT    NOT NULL,
    TextHash     TEXT    NOT NULL,
    AppName      TEXT    NULL,
    IsPinned     INTEGER NOT NULL DEFAULT 0,
    CreatedAtUtc TEXT    NOT NULL
);

CREATE UNIQUE INDEX UX_Clip_TextHash ON Clip (TextHash);

-- Serves newest-first paging, search ordering and prune scans.
CREATE INDEX IX_Clip_CreatedAtUtc ON Clip (CreatedAtUtc DESC, Id DESC);
