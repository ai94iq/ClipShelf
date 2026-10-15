-- 0003: categories group clips and keep them out of Clear, the history limit and retention.
CREATE TABLE Category (
    Id   INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT    NOT NULL
);

-- One category per name, ignoring case.
CREATE UNIQUE INDEX UX_Category_Name ON Category (Name COLLATE NOCASE);

ALTER TABLE Clip ADD COLUMN CategoryId INTEGER NULL REFERENCES Category (Id) ON DELETE SET NULL;
