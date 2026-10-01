# 0002. Tray icon library

Date: 2026-10-01
Status: Accepted

## Context
The app lives in the notification area and has no permanent window. WinUI 3 (Windows App SDK) provides no tray-icon control, and hand-writing `Shell_NotifyIcon` interop would add a large amount of unowned Win32 code for the same result.

## Decision
Use the `H.NotifyIcon.WinUI` package (MIT, `2.4.1`) for the tray icon, its context menu, tooltips and notifications.

## Consequences
One extra third-party dependency outside the locked stack, approved by the user. It is actively maintained, supports unpackaged apps and .NET 10, and its hidden message window is what keeps the process alive while no window is open. If it ever breaks, the fallback is a small `Shell_NotifyIcon` interop layer under `Platform/`.
