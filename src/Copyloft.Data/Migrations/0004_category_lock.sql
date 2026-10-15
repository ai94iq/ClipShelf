-- 0004: categories can carry a password; their clips stay hidden until the category is unlocked.
ALTER TABLE Category ADD COLUMN PasswordHash TEXT NULL;
