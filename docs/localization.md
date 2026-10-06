# Localization

English is the neutral language in `Strings.resx`; Arabic ships as the satellite in `Strings.ar.resx`. Pick the language in Settings → Appearance → Language (English / العربية); the change applies after a restart (`Culture.Configure` reads it once) and the layout flips to RTL in Arabic. Both files always have identical keys, sorted alphabetically. `LocalizationTests` enforces this.

## Key naming

`Feature_Element_Purpose` in PascalCase segments: `History_Filter`, `Common_Save`, `Error_LoadFailed`.
Plurals: `Base_zero`, `Base_one`, `Base_two`, `Base_few`, `Base_many`, `Base_other`, used through `Tr.Plural`, which picks English rules (zero/one/other) or Arabic CLDR rules (0, 1, 2, 3-10, 11-99, 100+) by the current UI culture.

## Glossary

| English | Arabic | Notes |
|---|---|---|
| Save | حفظ | |
| Cancel | إلغاء | |
| Retry | إعادة المحاولة | |
| Undo | تراجع | Restores a clip deleted by mistake |
| Clipboard history | سجل الحافظة | The list of recently copied items |
| Clip | عنصر | One stored clipboard item |
| Pinned | مثبَّت | Kept when the history is trimmed or cleared |
| Tray icon | أيقونة شريط المهام | Notification-area icon next to the clock |
| Hotkey | اختصار لوحة المفاتيح | Global shortcut; Win+Shift+V by default |
| History window | نافذة السجل | The full app window that lists every clip |
