-- 0001: initial schema. Never edit after it ships; add 0002_... instead.
CREATE TABLE AppMeta (
    Key   TEXT PRIMARY KEY,
    Value TEXT NOT NULL
);
