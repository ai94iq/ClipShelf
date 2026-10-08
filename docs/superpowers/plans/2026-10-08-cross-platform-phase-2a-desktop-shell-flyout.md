# Phase 2a - Desktop Shell + The Flyout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stand up `ClipShelf.Desktop` (Avalonia 12.1.3) on the `cross-platform` branch: the app shell, the design foundation of the chosen world (The Construction Grid), the Windows platform layer moved behind the Phase 1 interfaces, the tray, the single-instance wiring, and the signature surface - the flyout - working end to end on Windows.

**Architecture:** `ClipShelf.Desktop` (net10.0) is the single UI head for all three platforms. It consumes `ClipShelf.Presentation` (view models + platform interfaces, from Phase 1) and provides the Windows implementations under `Platform/Windows`. The world's design tokens live in `Themes/`; the flyout is `Views/FlyoutWindow`.

**Tech Stack:** .NET 10 (SDK 10.0.401), Avalonia 12.1.3 (Avalonia, Avalonia.Desktop, Avalonia.Themes.Fluent; Avalonia.Diagnostics debug-only), SkiaSharp (ships with Avalonia) for the image codec, CommunityToolkit.Mvvm, Microsoft.Extensions.Hosting, Serilog, xUnit v3 + NSubstitute on Microsoft.Testing.Platform, Central Package Management.

**Spec:** `docs/superpowers/specs/2026-10-08-cross-platform-migration-design.md`
**Surface brief (binding direction contract):** `.impeccable/surfaces/src-clipshelf-desktop.md`
**Direction:** The Construction Grid (catalog challenger crouwel-grid-specimen; seed key d14438e0; code-led build).

## Global Constraints

- **The direction contract binds every view.** The world lends only: type, palette, density, and ONE signature move (the focused clip rebuilt from its cells on state change). Layout, navigation and controls stay standard: a search box, a list, a footer, native buttons and dialogs. A view that reads as a design poster instead of a working product fails the contract.
- **The palette is committed:** paper ground, ink type, visible construction-grid hairlines at one weight, one stark saturated shell ground (the navigation rail), a single accent plane earned by the headline and the focused cell. The accent HUE is the user's accent setting (existing feature); the default is the world's blue `#0057FF`. Light, Dark and Black themes all express the world (dark inverts ground/type; black keeps the grid at low contrast).
- **Quiet and fast:** no effect may cost latency; the flyout must open instantly. No interaction may depend on motion (the cell rebuild is a 2-step, 90 ms step animation, disabled under reduced motion, and never blocks input).
- **Behavior parity:** the core loop (hotkey/tray -> flyout -> pick -> paste back) and all data paths behave exactly as the WinUI app does today. Existing `%LOCALAPPDATA%\ClipShelf` data must load untouched.
- **ASCII only** in code, comments, docs and commit messages: no em/en dashes, no arrows, no decorative symbols.
- **Commit format:** `<project>: <type>: <title>`; add `desktop` to the README project list in Task 1. Commits land on the `cross-platform` branch.
- **Build hygiene:** stop `ClipShelf.exe` before builds and commits (exe lock). The pre-commit hook runs the full suite; never bypass it.
- **Headless note for plan 2b:** `Avalonia.Headless.XUnit` does not support xUnit v3 (Avalonia issue #22072); headless tests will use the `Avalonia.Headless` base session API (`HeadlessUnitTestSession`) from plain xUnit v3 tests.
- **Asset duplication is deliberate until the swap (2b):** fonts and tray icons are copied into `ClipShelf.Desktop/Assets`; `ClipShelf.App` keeps its copies until it is removed.

## Review Focus

Input classes and failure modes; each pinned to a task below.

1. **Malformed clipboard images** - a truncated or lying DIB must yield null, never an exception or a crash (the WinUI app died on this class of input; the fix must survive the port). (Task 4: codec tests with malformed inputs.)
2. **Flyout dismissal races** - rapid open/close and foreground churn must not crash (the WinEvent hook must keep deferring `Hide` through the dispatcher, as the WinUI fix does). (Task 6: deferred-hide implementation + smoke steps.)
3. **Keyboard focus on open** - the search box must have focus the moment the flyout shows; arrows/Enter/Ctrl+1-9/Esc must work; RTL/IME input must not break. (Task 6: smoke steps; headless coverage in 2b.)
4. **Single instance during startup** - `--toggle` sent while the app is still starting must be stashed and applied, not lost. (Task 3: reuses the Phase 1 stash pattern; manual smoke step.)
5. **Existing data compatibility** - the Avalonia app must open the existing history and settings with no migration. (Task 3: manual smoke against the real `%LOCALAPPDATA%\ClipShelf`.)

---

### Task 1: Scaffold `ClipShelf.Desktop`

**Files:**
- Create: `src/ClipShelf.Desktop/ClipShelf.Desktop.csproj`, `src/ClipShelf.Desktop/Program.cs`, `src/ClipShelf.Desktop/App.axaml`, `src/ClipShelf.Desktop/App.axaml.cs`, `src/ClipShelf.Desktop/app.manifest`, `src/ClipShelf.Desktop/Views/MainWindow.axaml` + `.axaml.cs`
- Create: `src/ClipShelf.Desktop/Assets/` (copied fonts + tray icons)
- Modify: `Directory.Packages.props`, `ClipShelf.slnx`, `README.md`

**Interfaces:**
- Consumes: nothing.
- Produces: a launching Avalonia app; `App` (with `PendingCommand`/`SingleInstance` statics for Task 3); `BuildAvaloniaApp()` for tests later.

- [ ] **Step 1: Create the project file**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>ClipShelf.Desktop</RootNamespace>
    <AssemblyName>ClipShelf</AssemblyName>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
    <NeutralLanguage>en-US</NeutralLanguage>
    <SatelliteResourceLanguages>ar</SatelliteResourceLanguages>
    <InvariantGlobalization>false</InvariantGlobalization>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Avalonia" />
    <PackageReference Include="Avalonia.Desktop" />
    <PackageReference Include="Avalonia.Themes.Fluent" />
    <PackageReference Include="Avalonia.Diagnostics" Condition="'$(Configuration)' == 'Debug'" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\ClipShelf.Core\ClipShelf.Core.csproj" />
    <ProjectReference Include="..\ClipShelf.Data\ClipShelf.Data.csproj" />
    <ProjectReference Include="..\ClipShelf.Presentation\ClipShelf.Presentation.csproj" />
  </ItemGroup>
  <ItemGroup>
    <Content Include="Assets\Fonts\*.ttf" CopyToOutputDirectory="PreserveNewest" />
    <None Include="Assets\Fonts\OFL.txt" CopyToOutputDirectory="PreserveNewest" Condition="Exists('Assets\Fonts\OFL.txt')" />
    <Content Include="Assets\*.ico" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: CPM + solution + README**

`Directory.Packages.props`: add `Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`, `Avalonia.Diagnostics` all at `12.1.3`, and `Avalonia.Headless` at `12.1.3` (used by plan 2b).
`ClipShelf.slnx`: add the project under `/src/`.
`README.md`: add `desktop` to the commit-project list.

- [ ] **Step 3: Copy assets**

Copy `src/ClipShelf.App/Assets/Fonts/*` to `src/ClipShelf.Desktop/Assets/Fonts/` and `src/ClipShelf.App/Assets/tray-*.ico` plus `app.ico` to `src/ClipShelf.Desktop/Assets/`.

- [ ] **Step 4: Minimal entry point + blank window**

`Program.cs`: `[STAThread] static void Main(string[] args)` calling `BuildAvaloniaApp().StartWithClassicDesktopLifetime(args)`; `BuildAvaloniaApp()` = `AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace()`. `App.axaml`: `<FluentTheme />` + a placeholder `ResourceInclude`; `App.axaml.cs`: `OnFrameworkInitializationCompleted` sets `desktop.MainWindow = new MainWindow()`. `MainWindow`: empty window titled `ClipShelf`. `app.manifest`: DPI awareness per-monitor v2 (copy the shape from `src/ClipShelf.App/app.manifest`).

- [ ] **Step 5: Build and launch**

Run: `dotnet build ClipShelf.slnx -c Release`
Expected: 0 errors. Then launch `src/ClipShelf.Desktop/bin/Release/net10.0/ClipShelf.exe` and close it by hand.
Expected: a blank ClipShelf window appears.

- [ ] **Step 6: Commit**

```bash
git add src/ClipShelf.Desktop Directory.Packages.props ClipShelf.slnx README.md
git commit -m "desktop: chore: scaffold the avalonia desktop head"
```

---

### Task 2: Design foundation - The Construction Grid tokens

**Files:**
- Create: `src/ClipShelf.Desktop/Themes/Tokens.axaml`, `src/ClipShelf.Desktop/Themes/Controls.axaml`, `src/ClipShelf.Desktop/Controls/GridBackdrop.cs`
- Create: `src/ClipShelf.Desktop/Views/TokensPreviewWindow.axaml` + `.axaml.cs` (throwaway preview, deleted in Task 6)
- Modify: `src/ClipShelf.Desktop/App.axaml`

**Interfaces:**
- Consumes: Task 1's app.
- Produces: token keys every later view uses: `GridPaperBrush`, `GridInkBrush`, `GridLineBrush`, `GridRailBrush`, `GridAccentBrush`, `GridAccentPlaneBrush`, `GridLabelBrush`, `GridMutedBrush`; type sizes `GridDisplaySize` (44), `GridTitleSize` (20), `GridBodySize` (14), `GridLabelSize` (11); `GridBackdrop` (a `Control` drawing the construction grid hairlines).

- [ ] **Step 1: Write the token dictionaries**

`Tokens.axaml` (ResourceDictionary): light values - paper `#F7F7F4`, ink `#111111`, grid line `#E4E7EC`, rail `#0057FF` (the stark saturated shell ground; at full strength for the rail only), accent `#0057FF`, accent plane `#0057FF` at 12% for the focused cell, muted `#5A5F66`. `ThemeVariant` scopes for Light / Dark (ink ground `#0E0F11`, paper type `#F2F2EF`, grid line `#26282C`, rail `#0057FF`) / Black (ground `#000000`, grid line `#1A1B1E`). The accent keys resolve from the app's accent resources so the user's accent setting drives them; `#0057FF` is the default.

`Controls.axaml`: styles for the world's cell (hairline border at the grid weight, paper ground, 4 px radius, 8/10 px padding), the label style (small, tracked, muted), tabular-figure text (`FontFeatures="tnum"` where the stack supports it), and the focus frame (1 px accent border + accent plane).

- [ ] **Step 2: The grid backdrop control**

`GridBackdrop.cs`: a `Control` that renders vertical and horizontal hairlines on a fixed pitch (24 px) using the `GridLineBrush`, 1 px, no antialias shimmer; `IsHitTestVisible=false`. Keep it cheap: one `DrawingContext` pass, no bitmaps, no animation.

- [ ] **Step 3: Preview window**

`TokensPreviewWindow`: shows the grid backdrop, one display line ("128 CLIPS"), a cell sample with label, and the accent plane. Wire it as the temporary `MainWindow` in `App.OnFrameworkInitializationCompleted`.

- [ ] **Step 4: Build and look**

Run the app; confirm by eye: grid visible but quiet, ink on paper, one blue doing named jobs (rail, focus frame, accent plane), tabular figures aligned.

- [ ] **Step 5: Commit**

```bash
git add src/ClipShelf.Desktop
git commit -m "desktop: feat: the construction grid tokens and backdrop"
```

---

### Task 3: App shell, DI, single instance, existing data

**Files:**
- Modify: `src/ClipShelf.Desktop/Program.cs`, `App.axaml.cs`, `Views/MainWindow.axaml` + `.axaml.cs`
- Create: `src/ClipShelf.Desktop/Hosting/DesktopServiceRegistration.cs`, `src/ClipShelf.Desktop/Platform/Windows/AvaloniaUiDispatcher.cs`, `src/ClipShelf.Desktop/Platform/Windows/AvaloniaThemeService.cs`
- Move (from `src/ClipShelf.App/`, adapt): `Services/DataChangeNotifier.cs` -> `src/ClipShelf.Desktop/Platform/Windows/` (same class, Avalonia dispatcher underneath)

**Interfaces:**
- Consumes: Phase 1's `AddPresentationServices`, `SingleInstanceService`, `StartupCommand`, `IUiDispatcher`, `IThemeService`.
- Produces: the running shell with DI; `MainWindow` as the app shell (the committed rail + a content host; History/Settings pages arrive in 2b).

- [ ] **Step 1: Entry point with single instance**

`Program.Main`: parse `StartupCommand.Parse(args)`; create `SingleInstanceService(SingleInstanceService.DefaultName)`; on claim failure send the command and return; on success stash the command + service on `App` statics, subscribe `CommandReceived` (stash when the host is not up yet, exactly the Phase 1 pattern), then `StartWithClassicDesktopLifetime`.

- [ ] **Step 2: Host + DI**

`App.axaml.cs` builds the host like the WinUI app: `Host.CreateApplicationBuilder`, `AddSerilog`, `AddPresentationServices(settings)`, `DesktopServiceRegistration.AddPlatformServices()` + `.AddUiServices()` (this task registers: `IUiDispatcher` -> `AvaloniaUiDispatcher`, `IThemeService` -> `AvaloniaThemeService`, `IDataChangeNotifier` -> `DataChangeNotifier`; the remaining platform interfaces arrive in Task 5). Boot order: settings, culture, logging, `BackupAndMigrate`, warm-up, then the shell; `--toggle`/`--history` execute once the shell exists.

- [ ] **Step 3: The shell window**

`MainWindow`: the committed rail on the start edge (the saturated ground, 64 px, vertical: History, Settings, plus the app mark), a content host area (placeholder panels in this task), and the world's title treatment. Closing hides the window (same behavior as today). Keep navigation wired to placeholder `ContentControl`s.

- [ ] **Step 4: Verify with real data**

Run the app against the real `%LOCALAPPDATA%\ClipShelf` (no migration, no copies): the log shows startup and warm-up without errors; the window opens; `ClipShelf.exe --history` while running exits quietly and logs "Second instance blocked" (the command lands once pages exist in 2b; the stash path must not crash).

- [ ] **Step 5: Commit**

```bash
git add src/ClipShelf.Desktop
git commit -m "desktop: feat: app shell, di and single instance on avalonia"
```

---

### Task 4: Portable image codec (SkiaSharp), TDD

**Files:**
- Create: `src/ClipShelf.Desktop/Imaging/SkiaImageCodec.cs`
- Create: `tests/ClipShelf.Desktop.Tests/ClipShelf.Desktop.Tests.csproj`, `tests/ClipShelf.Desktop.Tests/SkiaImageCodecTests.cs`, `tests/ClipShelf.Desktop.Tests/DibFactory.cs`
- Modify: `ClipShelf.slnx`

**Interfaces:**
- Consumes: nothing from earlier tasks (pure bytes).
- Produces: `public static class SkiaImageCodec { byte[]? ToPng(byte[] dib); byte[]? Thumbnail(byte[] png, int maxEdge); byte[]? PngToDib(byte[] png); }` - replaces the WinRT `ImageClipCodec` (which cannot exist off the windows TFM). `DibFactory` builds valid test DIBs (32bpp BITMAPINFOHEADER + pixels).

- [ ] **Step 1: Write the failing tests**

```csharp
[Fact] public void ToPng_converts_a_valid_dib()
    // DibFactory.Create32bpp(4, 3) -> png; assert PNG magic (89 50 4E 47); decode with SKBitmap: 4x3.
[Fact] public void ToPng_returns_null_for_truncated_input()
    // a 20-byte buffer -> null (never throws).  [Review Focus 1]
[Fact] public void ToPng_returns_null_for_a_lying_header()
    // header claims 10000x10000 with 12 bytes of pixels -> null, no exception.  [Review Focus 1]
[Fact] public void Thumbnail_caps_the_long_edge()
    // 400x200 png -> thumbnail maxEdge 96 -> 96x48; aspect preserved.
[Fact] public void PngToDib_round_trips_dimensions()
    // png -> dib -> ToPng -> same width/height; biSize == 40, biBitCount == 32.
[Fact] public void PngToDib_returns_null_for_garbage()
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/ClipShelf.Desktop.Tests -c Release`
Expected: FAIL - the codec does not exist.

- [ ] **Step 3: Implement the codec**

DIB -> `SKBitmap` by reading `BITMAPINFOHEADER` (biSize, biWidth, biHeight - negative height means top-down, biBitCount 24/32) with the same bounds discipline as `Win32Clipboard.ReadBounded`: validate against the buffer length, reject impossible headers, never trust a size blindly. `SKBitmap.Encode(SKEncodedImageFormat.Png, 100)`. `Thumbnail`: decode, scale to fit maxEdge, encode. `PngToDib`: decode, write a 40-byte header (biHeight positive, bottom-up rows), 32bpp BGRA pixels, padded rows.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/ClipShelf.Desktop.Tests -c Release`
Expected: PASS (6/6).

- [ ] **Step 5: Commit**

```bash
git add src/ClipShelf.Desktop/Imaging tests/ClipShelf.Desktop.Tests ClipShelf.slnx
git commit -m "desktop: feat: skia image codec replacing the winrt one"
```

---

### Task 5: Windows platform layer, tray, the full core loop

**Files:**
- Move (from `src/ClipShelf.App/`, adapt to the Desktop head): `Platform/{NativeMethods,MessagePump,Win32Clipboard,PanelFrame,SessionWatcher,KeyboardPaste,ForegroundApp,SystemThemeWatcher}.cs`, `Services/{ClipboardService,ClipboardWatcher,GlobalHotkey,StartupRegistration,DpapiKeyStore,SystemThemeService}.cs` -> `src/ClipShelf.Desktop/Platform/Windows/`
- Create: `src/ClipShelf.Desktop/Shell/TrayController.cs` (Avalonia TrayIcon)
- Modify: `DesktopServiceRegistration`, `Program.cs` (nothing new), `App.axaml.cs` (wire the shell: tray + clipboard + hotkey)

**Interfaces:**
- Consumes: all Phase 1 interfaces; Task 4's codec.
- Produces: working capture (text and image), tray, hotkey, paste-back; `TrayController` with the menu (Open history, Settings, Exit), click toggles the flyout (Task 6).

- [ ] **Step 1: Move and adapt the framework-agnostic Win32 files**

`NativeMethods`, `MessagePump`, `SessionWatcher`, `KeyboardPaste`, `ForegroundApp`, `SystemThemeWatcher`, `DpapiKeyStore`, `StartupRegistration` move almost unchanged (no WinUI dependencies). `Win32Clipboard` keeps the Phase 1 bounded read; add `TryWriteImage(byte[] dib)`: OpenClipboard with retry, EmptyClipboard, GlobalAlloc + copy + SetClipboardData(CF_DIB), close. Keep the clipboard-write counting (self-writes must not be captured).

- [ ] **Step 2: ClipboardService for the Desktop head**

Same shape as the WinUI one: watcher events, capture via `Win32Clipboard` + `SkiaImageCodec` + `ClipCaptureService`, `WriteText` via Win32, `WriteImageAsync` via `PngToDib` + `TryWriteImage`. Register as `IClipboardMonitor` and `IClipboardWriter`.

- [ ] **Step 3: Tray + hotkey + paste-back wiring**

`TrayController`: Avalonia `TrayIcon` (icons from `Assets/tray-*.ico`, variant by `ISystemTheme.TaskbarIsLight`, `ShowTrayIcon` honored), menu (Open history, Settings, Exit), left click toggles the flyout, double-click honors `OpenHistoryOnDoubleClick`. `GlobalHotkey` registered via `MessagePump` exactly as today (keep-previous-on-failure behavior from the Phase 1 `HotkeyRegistrationState`).

- [ ] **Step 4: Verify the core loop by hand**

Run the app; copy a text clip in Notepad; copy an image in a browser; check the log lines and the database count via the app's own history later (2b); press the hotkey once Task 6 lands the flyout. Expected: capture happens, no crashes, self-writes ignored.

- [ ] **Step 5: Commit**

```bash
git add src/ClipShelf.Desktop
git commit -m "desktop: feat: windows platform layer and tray on avalonia"
```

---

### Task 6: The flyout - the signature surface

**Files:**
- Create: `src/ClipShelf.Desktop/Views/FlyoutWindow.axaml` + `.axaml.cs`, `src/ClipShelf.Desktop/Views/FlyoutCell.axaml` (or a DataTemplate in Controls.axaml), `src/ClipShelf.Desktop/Features/FlyoutActions.cs` (keyboard + dismissal, ported from `ClipboardPanelActions`), `src/ClipShelf.Desktop/Features/CategoryMenu.cs` (ported)
- Modify: `App.axaml.cs` (tray click + hotkey -> flyout toggle), delete `TokensPreviewWindow`

**Interfaces:**
- Consumes: `ClipboardPanelViewModel` (Presentation), `IPasteBack`, `IUiDispatcher`, the tokens.
- Produces: the FIRST VIEWPORT of the direction contract, working: search, the clip grid, focus, keyboard, pick-and-paste, dismissal, position near the cursor.

- [ ] **Step 1: The window**

Borderless (`SystemDecorations=None`), `Topmost=true`, `ShowInTaskbar=false`, `ShowActivated=false`, `CanResize=false`; sized 420 x 520; positioned near the cursor via `Screens` + the ported `PanelPlacement` math, with `PanelFrame`'s off-screen parking (Avalonia position + Win32 foreground handling where needed). Transparency: `TransparencyLevelHint` AcrylicBlur on Windows, solid paper elsewhere (the unresolved decision resolves to: acrylic only when it does not cost open latency; measure once, keep the result in the brief's Unresolved decisions note).

- [ ] **Step 2: The grid of cells (the contract's first viewport)**

Top row: the search box (standard TextBox, world-styled). Below: the cell list bound to `ClipboardPanelViewModel.Items` (grouped list arrives in 2b; flat list here): each cell shows the snippet (2 lines, ellipsized), the meta line (app name + relative time, tabular figures), the pin mark; the focused cell carries the accent plane and the cell frame. Footer: count + keyboard hints. The signature move: on a state change of the focused cell (pin toggle, re-copy moves it), run the 2-step 90 ms cell rebuild (a stepped opacity/mask pass, disabled under reduced motion).

- [ ] **Step 3: Keyboard + dismissal**

Search focused on show. Arrows move focus, Enter picks (paste-back into the previous window), Ctrl+1-9 pick directly, Esc leaves select mode then closes. Dismissal on foreground loss via the WinEvent hook, with the Phase 1 deferral: window work is enqueued to the dispatcher, never done inside the hook callback (Review Focus 2). Category menu: a Flyout anchored to the row with explicit position.

- [ ] **Step 4: Smoke the whole loop**

Run the app. Hotkey -> flyout near the cursor with search focused; type to filter; arrows + Enter pastes into the previous app; Ctrl+1-9 works; Esc closes; clicking elsewhere dismisses; rapid open/close 10 times with no crash (Review Focus 2); open with RTL (ar) culture shows mirrored alignment (Review Focus 3); pick an image clip and paste it into an image target.

- [ ] **Step 5: Commit**

```bash
git add src/ClipShelf.Desktop
git commit -m "desktop: feat: the flyout - the construction grid's first viewport"
```
