# Avalonia restart - Batch 5 (History page) Implementation Plan

> Execution: inline, superpowers:test-driven-development per task; UI tests through `Ui.Run`.

**Goal:** the main window's History section shows main's History page: title and description,
search and category filter, the clips grouped Pinned then Recent on a card, click or Enter copies
with a visible "Copied" notice, select mode with copy, clear all with a confirm, undo after a
delete, the true total in the footer, and more rows load when scrolled to the end.

**Reference:** `src/ClipShelf.App/Features/History/HistoryPage.xaml(.cs)` on `main`. The view
model is `ClipboardPanelViewModel` (its own instance, not the flyout's).

**Critique P1s kept:** P1-1 footer shows `TotalCount` and the list loads more at the end; P1-2
the "Copied" notice in the footer; P1-3 rows take keyboard focus, Enter copies, Esc leaves select
mode or cancels the clear confirm.

**Deferred:** the row category menu and unlocking a locked category in the filter both need the
dialogs (batch 7); until then a locked filter choice falls back to the previous one.

## Review focus

1. Group headers must not be selectable or take keyboard focus.
2. The footer count is the stored total, not the number of loaded rows.
3. Clicking a row copies it and does not leave it looking selected.
4. A refresh that regroups (pin, delete, new copy) keeps the list consistent with the groups.
5. Esc on the page never closes the window; it only steps out of select mode or the confirm.

### Task 15: grouped rows
- `Controls/GroupedRows` flattens `Groups` into headers and clips and follows every change.
- Tests: Pinned header, pinned clip, Recent header, recent clips in order; a pin moves the clip.

### Task 16: HistoryPage
- `Features/HistoryPage.axaml(.cs)` as in the goal; header rows disabled; click and Enter copy;
  Esc to `DismissTransient`; load more near the end of the scroll; locked filter falls back.
- Tests: header rows disabled; Enter on a row copies and the footer shows "Copied"; footer
  shows the stored total (532 of 2 loaded); Esc leaves select mode then cancels the confirm;
  near-end scroll asks for more only when more exist.

### Task 17: the main window hosts it
- `MainWindow` shows the page on History, marks its view model visible and loads it; hiding the
  window or switching to Settings marks it hidden. Register page and its view model.
- Tests: ShowHistory puts the page in the section and loads; Settings hides it.
- Live: tray History shows real clips; screenshots light.
