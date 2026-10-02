-- 0006: clips can be images; Text stays the (empty) label and the pixels live in ImageBytes.
ALTER TABLE Clip ADD COLUMN ImageBytes BLOB NULL;
ALTER TABLE Clip ADD COLUMN ThumbnailBytes BLOB NULL;
