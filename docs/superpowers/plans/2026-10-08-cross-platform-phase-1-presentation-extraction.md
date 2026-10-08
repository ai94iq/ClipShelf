# Phase 1 — Presentation Extraction Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extract the portable half of the WinUI app into a new `ClipShelf.Presentation` project (net10.0) behind platform interfaces, with zero behavior change, plus a named-pipe single instance that also gives the app `--toggle` / `--history`.

**Architecture:** `ClipShelf.Presentation` (net10.0) holds view models, portable services, localization and the platform interfaces. `ClipShelf.App` (WinUI) keeps the views and provides the Windows implementations of the interfaces. The app keeps shipping unchanged; the extraction is the de-risking step before the Avalonia head (Phase 2).

**Tech Stack:** .NET 10 (SDK 10.0.401), CommunityToolkit.Mvvm 8.4.2, Microsoft.Extensions.Hosting 10.0.12, Serilog 4.3.0 (+ Sinks.File 7.0.0, Sinks.Async 2.1.0), xUnit v3 4.0.1 + NSubstitute 5.3.0 on Microsoft.Testing.Platform, Central Package Management, `.slnx`.

**Spec:** `docs/superpowers/specs/2026-10-08-cross-platform-migration-design.md`

## Global Constraints

- **Behavior-free:** no user-visible behavior changes except the single-instance mechanism and the new `--toggle` / `--history` flags. Data locations, database, settings, and UI behavior stay identical.
- **Target frameworks:** `ClipShelf.Presentation` and `ClipShelf.Presentation.Tests` are `net10.0`. `ClipShelf.App` stays `net10.0-windows10.0.19041.0`.
- **CPM:** all versions live in `Directory.Packages.props` only. Add `<PackageVersion Include="Serilog" Version="4.3.0" />` (the version the app already resolves transitively).
- **Build props are inherited** from `Directory.Build.props`: `Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors`, `AnalysisLevel=latest`; test projects get `OutputType=Exe` + MTP automatically (name ends with `.Tests`).
- **Test baseline:** 298 tests green before the work starts; the count must never drop (moved tests keep running), new tests are added on top.
- **Pre-commit hook runs the full suite** (`dotnet test --solution ClipShelf.slnx -c Release`). Never bypass with `--no-verify`. Stop `ClipShelf.exe` before building or committing (the exe lock fails the build).
- **Commit format:** `<project>: <type>: <short title>` (README convention). `presentation` is added as a project name in Task 1; use `presentation:` for Presentation-project changes, `app:` for App-side wiring, `tests:` for test-only moves, `docs:` for docs.
- **Namespace rule:** every moved file's namespace changes `ClipShelf.App.X` → `ClipShelf.Presentation.X`; `using` lines inside moved files follow. App files that referenced moved types keep compiling through new `global using ClipShelf.Presentation.X;` lines in App's `GlobalUsings.cs`.
- **Do not rename** public types beyond what a task names (e.g. `DatabaseKey` → `DpapiKeyStore` in Task 5 is deliberate; nothing else changes names).

## Review Focus

Input classes and failure modes the spec implies; each is pinned to a task below.

1. **Rapid double launch** — a second instance that starts before the first instance's pipe is listening must exit quietly and leave the first instance untouched. (Task 6: `SecondClaimFails`, `SendRetriesUntilServerAppears`.)
2. **`--toggle` / `--history` during the first instance's startup** — the command must not be lost and must not crash startup. (Task 6: pending-command stash + smoke step.)
3. **Restart after a language change** — the single-instance handle must be fully released before the new process starts. (Task 6: `ClaimSucceedsAgainAfterDispose` + smoke step.)
4. **Missing or corrupt `key.bin`** — the app must fail with the same logged behavior as today. (Task 5: `DatabaseKeyTests` kept green, renamed to `DpapiKeyStoreTests`.)
5. **`settings.json` from an older build** (unknown enum values, e.g. the retired `White` theme alias) — must load unchanged. (Task 2: `SettingsServiceTests` kept green.)

---

### Task 1: Scaffold `ClipShelf.Presentation`

**Files:**
- Create: `src/ClipShelf.Presentation/ClipShelf.Presentation.csproj`
- Create: `src/ClipShelf.Presentation/GlobalUsings.cs`
- Modify: `Directory.Packages.props` (add Serilog)
- Modify: `ClipShelf.slnx` (add the project)
- Modify: `README.md` (add `presentation` to the commit-project list)

**Interfaces:**
- Consumes: nothing.
- Produces: the project later tasks move files into. `ClipShelf.Presentation` with `RootNamespace` `ClipShelf.Presentation`, `net10.0`, references Core + Data.

- [ ] **Step 1: Create the project file**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>ClipShelf.Presentation</RootNamespace>
    <NeutralLanguage>en-US</NeutralLanguage>
    <SatelliteResourceLanguages>ar</SatelliteResourceLanguages>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="CommunityToolkit.Mvvm" />
    <PackageReference Include="Microsoft.Extensions.Hosting" />
    <PackageReference Include="Serilog" />
    <PackageReference Include="Serilog.Sinks.Async" />
    <PackageReference Include="Serilog.Sinks.File" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\ClipShelf.Core\ClipShelf.Core.csproj" />
    <ProjectReference Include="..\ClipShelf.Data\ClipShelf.Data.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create `GlobalUsings.cs`** (only namespaces that exist; later tasks append the project's own)

```csharp
global using System.Globalization;
global using CommunityToolkit.Mvvm.ComponentModel;
global using CommunityToolkit.Mvvm.Input;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using ClipShelf.Core.Abstractions;
global using ClipShelf.Core.Models;
global using ClipShelf.Core.Text;
global using ClipShelf.Data;
```

- [ ] **Step 3: Register Serilog core and the solution entry**

`Directory.Packages.props`: add `<PackageVersion Include="Serilog" Version="4.3.0" />` (the resolved version, verified with `dotnet list package --include-transitive`).

`ClipShelf.slnx`: add `<Project Path="src/ClipShelf.Presentation/ClipShelf.Presentation.csproj" />` under `/src/`.

`README.md` (Contributing section): append `presentation` to the projects list of the commit-title convention.

- [ ] **Step 4: Build and verify nothing changed**

Run: `dotnet build ClipShelf.slnx -c Release`
Expected: build succeeds, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add src/ClipShelf.Presentation Directory.Packages.props ClipShelf.slnx README.md
git commit -m "presentation: chore: scaffold the presentation project"
```

---

### Task 2: Move the foundation — Hosting, Localization, Common

**Files:**
- Move (git mv, namespace rewrite `ClipShelf.App.Hosting` → `ClipShelf.Presentation.Hosting`):
  `src/ClipShelf.App/Hosting/{AppLogging,AppPaths,AppSettings,AppTheme,Culture,FileSettingsStore,ISettingsStore,SettingsService,HotkeyGestureJsonConverter,LazyService}.cs` → `src/ClipShelf.Presentation/Hosting/`
- Move (namespace `ClipShelf.App.Localization` → `ClipShelf.Presentation.Localization`):
  `src/ClipShelf.App/Localization/Tr.cs` → `src/ClipShelf.Presentation/Localization/`
- Move (namespace `ClipShelf.App.Common` → `ClipShelf.Presentation.Common`):
  `src/ClipShelf.App/Common/{LoadState,Option,PageViewModel}.cs` → `src/ClipShelf.Presentation/Common/`
- Move: `src/ClipShelf.App/Resources/Strings.resx` and `Strings.ar.resx` → `src/ClipShelf.Presentation/Resources/`
- Create: `tests/ClipShelf.Presentation.Tests/ClipShelf.Presentation.Tests.csproj`, `tests/ClipShelf.Presentation.Tests/TestPaths.cs`
- Move tests (namespace + using updates): `LocalizationTests.cs`, `PageViewModelTests.cs`, `Settings/SettingsServiceTests.cs`, `Settings/FakeSettingsStore.cs` → `tests/ClipShelf.Presentation.Tests/`
- Modify: `src/ClipShelf.App/GlobalUsings.cs`, `src/ClipShelf.Presentation/GlobalUsings.cs`, `ClipShelf.slnx`

**Interfaces:**
- Consumes: Task 1's project.
- Produces: `ClipShelf.Presentation.Hosting.AppPaths/AppSettings/AppTheme/Culture/SettingsService/FileSettingsStore/ISettingsStore`, `ClipShelf.Presentation.Localization.Tr`, `ClipShelf.Presentation.Common.{LoadState,Option,PageViewModel}` — every later task uses these.

- [ ] **Step 1: Move the files and rewrite namespaces**

Use `git mv` for each file (keeps history), then change the `namespace` line and any `using ClipShelf.App.…` lines to the matching `ClipShelf.Presentation.…` form. Special cases:
- `Tr.cs`: the `ResourceManager` name becomes `"ClipShelf.Presentation.Resources.Strings"`.
- `AppFonts.cs` and `WindowExtensions.cs` **stay** in App (they use WinUI types).
- `BoolToVisibility.cs`, converters, `AppIcon*`, `IconData*`, `RtlAlign*`, `SettingsCard*`, `ThemeSurfaces.cs`, `WindowSurface.cs`, `LoadStateOverlay.*` **stay** in App.

- [ ] **Step 2: Update the global usings**

`src/ClipShelf.Presentation/GlobalUsings.cs` — append:

```csharp
global using ClipShelf.Presentation.Common;
global using ClipShelf.Presentation.Hosting;
global using ClipShelf.Presentation.Localization;
```

`src/ClipShelf.App/GlobalUsings.cs` — append the same three lines (App files now consume the moved types; the old `ClipShelf.App.Hosting` line stays because `AppFonts`/`WindowExtensions` remain).

- [ ] **Step 3: Create the test project**

`tests/ClipShelf.Presentation.Tests/ClipShelf.Presentation.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="NSubstitute" />
  </ItemGroup>
  <ItemGroup>
    <Using Include="Xunit" />
    <Using Include="System.Text.RegularExpressions" />
    <Using Include="System.Xml.Linq" />
    <Using Include="Microsoft.Extensions.Logging.Abstractions" />
    <Using Include="ClipShelf.Presentation.Common" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\ClipShelf.Presentation\ClipShelf.Presentation.csproj" />
  </ItemGroup>
</Project>
```

Add it to `ClipShelf.slnx` under `/tests/`.

- [ ] **Step 4: Move the first tests and their helper**

`git mv` the four test files listed above; update namespaces to `ClipShelf.Presentation.Tests` and usings to `ClipShelf.Presentation.*`. In `LocalizationTests.cs` the resx lookup must point at `TestPaths.PresentationProject` and the `Culture` type is `ClipShelf.Presentation.Hosting.Culture`.

Create `TestPaths.cs` (copy of the App one with `PresentationProject` instead of `AppProject`; drop members the moved tests do not use).

- [ ] **Step 5: Build and run the full suite**

Run: `test.bat --no-pause`
Expected: 298 tests total (moved tests now run from the new project), all green.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "presentation: refactor: move hosting, localization and common foundation out of the winui app"
```

---

### Task 3: Move the portable services and their interfaces

**Files:**
- Move (namespace `ClipShelf.App.Services` → `ClipShelf.Presentation.Services`):
  `src/ClipShelf.App/Services/{CategoryLockService,ICategoryLockService,ClipExportService,IClipExportService,DateFormatter,IDateFormatter,LockPasswordService,ILockPasswordService,UpdateChecker,IUpdateChecker,HotkeyRegistrationState,IClipboardWriter,IHotkeyRegistration,IStartupRegistration}.cs` → `src/ClipShelf.Presentation/Services/`
- Move tests: `ClipExportServiceTests.cs`, `DateFormatterTests.cs`, `HotkeyRegistrationStateTests.cs`, `UpdateCheckerTests.cs`, `Settings/CategoryLockServiceTests.cs`, `Settings/FakeStartupRegistration.cs` → `tests/ClipShelf.Presentation.Tests/`
- Modify: both `GlobalUsings.cs`

**Interfaces:**
- Consumes: Task 2's namespaces.
- Produces: `IClipboardWriter`, `IHotkeyRegistration`, `IStartupRegistration`, `ICategoryLockService`, `IClipExportService`, `IDateFormatter`, `ILockPasswordService`, `IUpdateChecker`, `HotkeyRegistrationState` in `ClipShelf.Presentation.Services` — consumed by view models (Task 4), the App's shell (still in App), and the platform wiring (Task 5).

- [ ] **Step 1: Move and rewrite**

`git mv` each file; rewrite `namespace ClipShelf.App.Services` → `namespace ClipShelf.Presentation.Services`. Files that **stay** in App/Services: `ClipboardService.cs`, `ClipboardWatcher.cs`, `DatabaseKey.cs`, `DataChangeNotifier.cs`, `GlobalHotkey.cs`, `SessionCleanup.cs` (moves in Task 5), `StartupRegistration.cs`, `ThemeService.cs`, `IThemeService.cs` (split in Task 4).

- [ ] **Step 2: Global usings**

Append `global using ClipShelf.Presentation.Services;` to both `GlobalUsings.cs` files.

- [ ] **Step 3: Move the tests**

`git mv` the six test files; update namespaces/usings as in Task 2.

- [ ] **Step 4: Build and run the full suite**

Run: `test.bat --no-pause`
Expected: all green, count unchanged from Task 2.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "presentation: refactor: move portable services and interfaces out of the winui app"
```

---

### Task 4: Move the view models, split `IThemeService`

**Files:**
- Move (namespace `ClipShelf.App.Features.ClipboardPanel` → `ClipShelf.Presentation.Features.ClipboardPanel`):
  `src/ClipShelf.App/Features/ClipboardPanel/{ClipboardPanelViewModel,ClipItemViewModel,ClipGroup}.cs` → `src/ClipShelf.Presentation/Features/ClipboardPanel/`
- Move (namespace `ClipShelf.App.Features.Settings` → `ClipShelf.Presentation.Features.Settings`):
  `src/ClipShelf.App/Features/Settings/{SettingsViewModel,AccentOption,CategoryRow}.cs` → `src/ClipShelf.Presentation/Features/Settings/`
- Move (namespace `ClipShelf.App.Platform` → `ClipShelf.Presentation.Platform`):
  `src/ClipShelf.App/Platform/{PanelPlacement,PanelDismiss}.cs` → `src/ClipShelf.Presentation/Platform/`
- Create: `src/ClipShelf.Presentation/Services/IThemeService.cs` (portable shape)
- Modify: `src/ClipShelf.App/Services/ThemeService.cs`, `src/ClipShelf.App/Services/IThemeService.cs` (delete), `src/ClipShelf.App/Shell/MainWindow.xaml.cs`, `src/ClipShelf.App/Shell/WelcomeWindow.xaml.cs`, `src/ClipShelf.App/Hosting/ServiceRegistration.cs`, both `GlobalUsings.cs`
- Move tests: `ClipboardPanelViewModelTests.cs`, `ClipItemViewModelTests.cs`, `PanelDismissTests.cs`, `PanelPlacementTests.cs`, `Settings/SettingsViewModelTests.cs` → `tests/ClipShelf.Presentation.Tests/`

**Interfaces:**
- Consumes: Task 3's services.
- Produces: `ClipShelf.Presentation.Features.*` view models; portable `IThemeService { void Apply(AppSettings settings); }`; `PanelPlacement`/`PanelDismiss` in `ClipShelf.Presentation.Platform` (consumed by App's `PanelFrame`).

- [ ] **Step 1: Split `IThemeService`**

Create `src/ClipShelf.Presentation/Services/IThemeService.cs`:

```csharp
public interface IThemeService
{
    void Apply(AppSettings settings);
}
```

Delete `src/ClipShelf.App/Services/IThemeService.cs`. In `ThemeService.cs` (App): implement the moved interface, keep the existing `public void Attach(Window window)` method as-is.

- [ ] **Step 2: Move the view models and layout logic**

`git mv` the files; rewrite namespaces. In `ClipboardPanelViewModel.cs` and `SettingsViewModel.cs` update usings (`ClipShelf.App.Services` → `ClipShelf.Presentation.Services`; `ClipShelf.App.Common` → `ClipShelf.Presentation.Common`). `PanelPlacement.cs`/`PanelDismiss.cs` move with namespace `ClipShelf.Presentation.Platform`; App's `Platform/PanelFrame.cs` picks them up through the new global using.

- [ ] **Step 3: Update the App side**

- `MainWindow.xaml.cs` (ctor param line 20) and `WelcomeWindow.xaml.cs` (line 11): change the `IThemeService theme` parameter to `ThemeService theme`; `theme.Attach(this)` calls stay.
- `ServiceRegistration.cs`: register the concrete and forward the interface —

```csharp
services.AddSingleton<ThemeService>();
services.AddSingleton<IThemeService>(sp => sp.GetRequiredService<ThemeService>());
```

- Global usings: append `global using ClipShelf.Presentation.Features.ClipboardPanel;`, `global using ClipShelf.Presentation.Features.Settings;`, `global using ClipShelf.Presentation.Platform;` to App's; add the Features lines to Presentation's.

- [ ] **Step 4: Move the tests**

`git mv` the five test files; update namespaces/usings. `SettingsViewModelTests` uses `FakeStartupRegistration` and `FakeSettingsStore` (both already in Presentation.Tests) — keep it compiling.

- [ ] **Step 5: Build and run the full suite**

Run: `test.bat --no-pause`
Expected: all green, count unchanged.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "presentation: refactor: move view models and panel layout logic out of the winui app"
```

---

### Task 5: Platform interfaces, Windows implementations, DI split

**Files:**
- Create: `src/ClipShelf.Presentation/Platform/{IClipboardMonitor,IGlobalHotkey,IPasteBack,IForegroundApp,IKeyStore,IUiDispatcher,ISystemTheme,ISessionEnd}.cs`
- Create: `src/ClipShelf.App/Platform/DispatcherQueueUiDispatcher.cs`, `src/ClipShelf.App/Platform/SystemThemeService.cs`
- Modify: `src/ClipShelf.App/Services/{ClipboardService,GlobalHotkey,DataChangeNotifier}.cs`, `src/ClipShelf.App/Platform/{KeyboardPaste,ForegroundApp,SessionWatcher}.cs`
- Rename: `src/ClipShelf.App/Services/DatabaseKey.cs` → `DpapiKeyStore.cs`; `tests/ClipShelf.App.Tests/Settings/DatabaseKeyTests.cs` → `DpapiKeyStoreTests.cs`
- Move: `src/ClipShelf.App/Services/SessionCleanup.cs` → `src/ClipShelf.Presentation/Services/`
- Create: `src/ClipShelf.Presentation/Hosting/ServiceRegistration.cs`
- Modify: `src/ClipShelf.App/Hosting/ServiceRegistration.cs` (split), `src/ClipShelf.App/App.xaml.cs` (startup call), `src/ClipShelf.App/Shell/AppShellService.cs` (system theme + hotkey call)

**Interfaces:**
- Consumes: everything moved so far.
- Produces: the nine platform interfaces; `AddPresentationServices(settings)` (Presentation) and `AddPlatformServices()` / `AddUiServices()` (App). Consumed by Phase 2's Avalonia head.

- [ ] **Step 1: Define the interfaces** (exact shapes)

```csharp
// IClipboardMonitor.cs
public interface IClipboardMonitor : IClipboardWriter
{
    event EventHandler? Changed;
    string? ReadText();
    byte[]? ReadImage();   // platform-native image payload (a DIB block on Windows); the head decodes it
}

// IGlobalHotkey.cs
public interface IGlobalHotkey : IDisposable
{
    event EventHandler? Pressed;
    bool Register(HotkeyGesture gesture);
}

// IPasteBack.cs
public interface IPasteBack
{
    Task IntoAsync(IntPtr targetWindow);
}

// IForegroundApp.cs
public interface IForegroundApp
{
    string? Name();
}

// IKeyStore.cs
public interface IKeyStore
{
    string LoadOrCreate(string path);
}

// IUiDispatcher.cs
public interface IUiDispatcher
{
    void Post(Action action);
}

// ISystemTheme.cs
public interface ISystemTheme
{
    bool TaskbarIsLight { get; }
    event EventHandler? Changed;
}

// ISessionEnd.cs
public interface ISessionEnd
{
    event EventHandler? Ending;
}
```

(`IHotkeyRegistration` and `IStartupRegistration` already moved in Task 3.)

- [ ] **Step 2: Adapt the Windows implementations**

These implementations stay in place for Phase 1 (`App/Services/` + `App/Platform/`); Phase 2 moves them into `ClipShelf.Desktop/Platform/Windows/`.

- `ClipboardService : IClipboardMonitor` — add `ReadText()` → `Win32Clipboard.TryReadText()`, `ReadImage()` → `Win32Clipboard.TryReadImage()`; raise `Changed` from `OnClipboardChanged`. The internal capture path stays as it is (behavior-free).
- `GlobalHotkey : IGlobalHotkey` — `Register(HotkeyGesture gesture)` converts inside (`(uint)gesture.Modifiers | ModNoRepeat`, `(uint)gesture.VirtualKey`). `AppShellService.ApplyHotkey` call becomes `gesture => _hotkey.Register(gesture)`.
- `KeyboardPaste` — becomes `internal sealed class KeyboardPaste : IPasteBack` with the existing body as `public Task IntoAsync(IntPtr targetWindow)`. Inject `IPasteBack` at the call site (`ClipboardPanelActions`).
- `ForegroundApp` — becomes `internal sealed class ForegroundApp : IForegroundApp`. Inject `IForegroundApp` into `ClipboardService`.
- `DatabaseKey` → `DpapiKeyStore` (`internal sealed class DpapiKeyStore : IKeyStore`, same body).
- `StartupRegistration` — add `: IStartupRegistration` (interface already moved).
- `SystemTheme.cs` + `SystemThemeWatcher.cs` → merge into `Platform/SystemThemeService.cs` (`internal sealed class SystemThemeService : ISystemTheme, IDisposable`) that reads the registry and raises `Changed` from both the registry watcher and `MessagePump.SystemThemeChanged` (the current fallback). `AppShellService` injects `ISystemTheme`, subscribes `Changed`, and reads `TaskbarIsLight`.
- `SessionWatcher : ISessionEnd` (add the interface).
- `SessionCleanup` — move to `Presentation/Services/`, ctor takes `ISessionEnd` instead of `SessionWatcher`.
- `DataChangeNotifier` — ctor takes `IUiDispatcher`; `Notify` becomes `_dispatcher.Post(() => WeakReferenceMessenger.Default.Send(change))`. New `DispatcherQueueUiDispatcher : IUiDispatcher` captures `DispatcherQueue.GetForCurrentThread()` in its ctor (created on the UI thread at startup, same as today).

- [ ] **Step 3: Split DI registration**

`src/ClipShelf.Presentation/Hosting/ServiceRegistration.cs`:

```csharp
public static IServiceCollection AddPresentationServices(this IServiceCollection services, AppSettings settings)
```

Registers: `settings`, `ISettingsStore → FileSettingsStore`, `SettingsService`, `Lazy<> → LazyService<>`, `DataOptions` (via `IKeyStore`), `SqliteConnectionFactory`, `DatabaseInitializer`, repositories, `ClipCaptureService`, `SessionCleanup`, `ICategoryLockService`, `ILockPasswordService`, `IClipExportService`, `IDateFormatter`, `IUpdateChecker`, `SettingsViewModel`.

`src/ClipShelf.App/Hosting/ServiceRegistration.cs` keeps `AddPlatformServices()` (Windows implementations: `IKeyStore → DpapiKeyStore`, `IClipboardMonitor`/`IClipboardWriter → ClipboardService`, `IGlobalHotkey → GlobalHotkey`, `IPasteBack → KeyboardPaste`, `IForegroundApp → ForegroundApp`, `IStartupRegistration → StartupRegistration`, `ISystemTheme → SystemThemeService`, `IUiDispatcher → DispatcherQueueUiDispatcher`, `ISessionEnd → SessionWatcher`, `IDataChangeNotifier → DataChangeNotifier`, `IThemeService → ThemeService`) and `AddUiServices()` (windows, pages, shell, `AppLifetime`, `AppShellService`, `ClipboardPanelWindow`, `HistoryPage`, `SettingsPage`, `MainWindow`, `WelcomeWindow`, `ClipboardPanelViewModel`, `ClipboardPanelActions`).

`App.xaml.cs` startup call becomes:

```csharp
builder.Services.AddPresentationServices(_settings);
builder.Services.AddPlatformServices();
builder.Services.AddUiServices();
```

- [ ] **Step 4: Update the renamed test**

`git mv` `DatabaseKeyTests.cs` → `DpapiKeyStoreTests.cs`; update the type reference (`DpapiKeyStore.LoadOrCreate`). It stays in `ClipShelf.App.Tests` (DPAPI is Windows-only).

- [ ] **Step 5: Build, full suite, manual smoke**

Run: `test.bat --no-pause` — expected all green.

Manual smoke on Windows (the app must behave exactly as before): tray icon appears; hotkey opens the flyout; copying text captures; copying an image captures; picking a clip pastes into the previous app; Settings theme change applies; no log errors in `%LOCALAPPDATA%\ClipShelf\logs`.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "app: refactor: platform interfaces with windows implementations behind them"
```

---

### Task 6: Named-pipe single instance + `--toggle` / `--history`

**Files:**
- Create: `src/ClipShelf.Presentation/Hosting/SingleInstanceService.cs`, `src/ClipShelf.Presentation/Hosting/StartupCommand.cs`
- Create: `tests/ClipShelf.Presentation.Tests/SingleInstanceServiceTests.cs`, `tests/ClipShelf.Presentation.Tests/StartupCommandTests.cs`
- Modify: `src/ClipShelf.App/App.xaml.cs`, `src/ClipShelf.App/Shell/AppShellService.cs`

**Interfaces:**
- Consumes: Task 5's DI split.
- Produces: `SingleInstanceService` (pipe-based single instance + command channel), `StartupCommand.Parse(string[] args)`.

- [ ] **Step 1: Write the failing tests**

`SingleInstanceServiceTests.cs` — each test uses a unique pipe name (`"ClipShelf.Test." + Guid.NewGuid().ToString("N")`). Keep the synchronous tests non-`async` (warnings are errors):

```csharp
[Fact] public void FirstClaimSucceeds()
    // new SingleInstanceService(name).TryClaim() => true

[Fact] public void SecondClaimFails()
    // s1.TryClaim() => true; s2.TryClaim() => false

[Fact] public async Task SentCommandIsReceived()
    // s1.TryClaim(); await SingleInstanceService.TrySendAsync(name, "--toggle") => true;
    // the CommandReceived event delivers "--toggle" within 5 s

[Fact] public async Task SendRetriesUntilServerAppears()
    // start TrySendAsync; claim 200 ms later; send task => true; command received

[Fact] public void ClaimSucceedsAgainAfterDispose()
    // s1.TryClaim(); s1.Dispose(); s2.TryClaim() => true

[Fact] public async Task SendWithoutServerFails()
    // TrySendAsync on an unused name => false (within the retry window)
```

`StartupCommandTests.cs`:

```csharp
[Theory]
[InlineData(new[] { "app.exe" }, null)]
[InlineData(new[] { "app.exe", "--toggle" }, "--toggle")]
[InlineData(new[] { "app.exe", "--history" }, "--history")]
[InlineData(new[] { "app.exe", "--unknown" }, null)]
public void Parse_reads_only_known_commands(string[] args, string? expected)
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/ClipShelf.Presentation.Tests -c Release` (filter to the two new classes if the runner needs it)
Expected: FAIL — the types do not exist.

- [ ] **Step 3: Implement**

`SingleInstanceService` (net10.0, `NamedPipeServerStream` maps to a Unix domain socket on Linux/macOS):

```csharp
public sealed class SingleInstanceService : IDisposable
{
    public SingleInstanceService(string name);
    public bool TryClaim();            // false when another instance already owns the name
    public event EventHandler<string>? CommandReceived;
    public static Task<bool> TrySendAsync(string name, string command, CancellationToken cancellationToken = default);
    public void Dispose();
}
```

Behavior: `TryClaim` creates the server stream (`PipeDirection.In`, 1 max instance, asynchronous); on `IOException` returns false. The accept loop (`WaitForConnectionAsync` → read one UTF-8 line → raise `CommandReceived` → `Disconnect()` → repeat) runs on a background task and stops on dispose. `TrySendAsync` connects with a retry loop (50 ms steps, up to ~2 s) so a just-launched first instance is found; returns false when no server answers. `Dispose` cancels the loop and disposes the stream. Command strings: `"--toggle"`, `"--history"`.

`StartupCommand.Parse(string[] args)`: returns `"--toggle"` or `"--history"` when present, otherwise `null`; ignore `--unknown` and anything else.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/ClipShelf.Presentation.Tests -c Release`
Expected: PASS (all new tests).

- [ ] **Step 5: Wire the App**

- `App.xaml.cs`: replace the `Mutex` with `SingleInstanceService` (name `"ClipShelf.SingleInstance"`). In `OnLaunched`:
  - Claim first. If the claim fails: parse the command; if one exists, `await SingleInstanceService.TrySendAsync(...)`; log "Second instance blocked"; `Exit()`; return.
  - If the claim succeeds and a command was given, stash it and execute after startup (after `AppShellService` exists): `--history` → `shell.ShowHistory()`, `--toggle` → `shell.TogglePanel()`.
  - Subscribe `CommandReceived` right after the host starts; route through `IUiDispatcher` to the same two actions. Commands arriving before the shell exists go into the stash (Review Focus 2).
  - `RestartApplication`: dispose the single instance before `Process.Start(executable)` (replaces the mutex release; update the comment).
  - `Shutdown`: dispose the single instance (replaces the mutex lines).
- `AppShellService`: add `public void TogglePanel() => _ = TogglePanelAsync();`.

- [ ] **Step 6: Full suite + smoke**

Run: `test.bat --no-pause` — expected all green.

Manual smoke: launch the app; launch a second copy (exits quietly, first unaffected); `ClipShelf.exe --history` (the first instance opens History); `--toggle` (the flyout toggles); `--toggle` while the app is still starting (command stashed, applied after startup); change the language in Settings (restart works, new process owns the pipe).

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "app: feat: named-pipe single instance with --toggle and --history"
```

---

### Task 7: Docs, changelog, release verification

**Files:**
- Modify: `docs/architecture.md`, `CHANGELOG.md`, `README.md`

**Interfaces:**
- Consumes: everything above.
- Produces: documentation matching the new project layout; the MSI check for the release.

- [ ] **Step 1: Update the docs**

- `docs/architecture.md`: projects table gains `ClipShelf.Presentation` (net10.0: view models, portable services, platform interfaces) and the dependency graph becomes `App → Presentation → Core, Data`; startup flow mentions the named-pipe single instance and `--toggle` / `--history`.
- `README.md`: project structure table gains `ClipShelf.Presentation`; Scripts/Usage note for `--toggle` / `--history`.
- `CHANGELOG.md`: under Unreleased → Changed: "View models and services now live in a shared `ClipShelf.Presentation` project (no behavior change); a second launch can drive the running app with `--toggle` / `--history`."

- [ ] **Step 2: Build the installer as the acceptance check**

Run: `package.bat --no-pause`
Expected: `[test] OK`, `[build] OK`, `[package] OK: artifacts\installer`.

- [ ] **Step 3: Manual release checklist (owner)**

Fresh install of the MSI behaves as v0.3.x: welcome window on first run, history and settings load existing data, tray and hotkey work, uninstall keeps `%LOCALAPPDATA%\ClipShelf`.

- [ ] **Step 4: Commit**

```bash
git add docs/architecture.md README.md CHANGELOG.md
git commit -m "docs: docs: describe the presentation project and the new launch flags"
```
