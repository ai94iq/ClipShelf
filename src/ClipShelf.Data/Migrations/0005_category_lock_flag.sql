-- 0005: category locks share one password; a category only stores whether it is locked.
-- Locks from the per-category password model start fresh, so nothing can be left unopenable.
ALTER TABLE Category ADD COLUMN IsLocked INTEGER NOT NULL DEFAULT 0;
ALTER TABLE Category DROP COLUMN PasswordHash;
