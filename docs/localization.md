# Localization

English is the only UI language for now: `Strings.resx` (neutral) and `Strings.en.resx` hold the same English text, so the app shows English on any culture. Both files always have identical keys, sorted alphabetically. `LocalizationTests` enforces this. Adding a language later means filling the satellite file and wiring it in `Culture`.

## Key naming

`Feature_Element_Purpose` in PascalCase segments: `People_Search_Placeholder`, `Common_Save`, `Error_Database_Locked`.
Plurals: `Base_zero`, `Base_one`, `Base_two`, `Base_few`, `Base_many`, `Base_other`, used through `Tr.Plural`.

## Glossary

| English | Arabic | Notes |
|---|---|---|
| Family tree | شجرة العائلة | |
| Person | شخص | |
| Spouse | الزوج / الزوجة | Use the gendered form when known |
| Marriage | زواج | |
| Children | الأبناء | |
| Ancestors | الأسلاف | |
| Descendants | الذرية | |
| Generation | جيل | |
| Deceased | متوفى / متوفاة | |
| Save | حفظ | |
| Cancel | إلغاء | |
| Retry | إعادة المحاولة | |
| Clipboard history | سجل الحافظة | The list of recently copied items |
| Clip | عنصر | One stored clipboard item |
| Pinned | مثبَّت | Kept when the history is trimmed or cleared |
| Tray icon | أيقونة شريط المهام | Notification-area icon next to the clock |
