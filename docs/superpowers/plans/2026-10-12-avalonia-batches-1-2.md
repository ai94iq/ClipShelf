# Avalonia restart - Batches 1-2 (scaffold, tokens, host) Implementation Plan

> Saved to `docs/superpowers/plans/2026-10-12-avalonia-batches-1-2.md` as the first execution
> step. Execution: Native (inline, owner choice 2026-10-12), superpowers:executing-plans with
> superpowers:test-driven-development on every task. Steps use `- [ ]` checkboxes.

**Status (2026-10-12):** all tasks done, commits 87bc367..3bd7245. Deviations: plain
`Avalonia.Headless` instead of `Avalonia.Headless.XUnit` (xUnit 4 incompatibility); an extra fix
commit d9cb613 for BI_BITFIELDS and row padding in clipboard image reads, found in the Task 7
live check. Batch 2 gate (owner check of the tray) is open.

**Goal:** an Avalonia 12 app that starts tray-first on Windows, owns the single instance,
captures text and image clips into the existing encrypted database, registers the hotkey and
exits cleanly, on a Fluent-only token rule guarded by tests. No visible windows yet.

**Architecture:** new `src/ClipShelf.Desktop` (Avalonia head, `net10.0`) and
`tests/ClipShelf.Desktop.Tests` (xUnit v3 + Avalonia.Headless.XUnit). Portable logic stays in
`ClipShelf.Presentation` (on main). Windows platform code is copied from main's
`src/ClipShelf.App` (kept until the swap); only the WinUI/WinRT-bound pieces are rewritten, TDD.

**Tech stack:** .NET 10.0.401 SDK, Avalonia 12.1.4 (Avalonia, Avalonia.Desktop,
Avalonia.Themes.Fluent, Avalonia.Headless.XUnit; NuGet checked 2026-10-12), SkiaSharp via
Avalonia.Skia, xUnit v3 4.0.1 on MTP, CommunityToolkit.Mvvm, Microsoft.Extensions.Hosting,
Serilog.

**Spec:** `docs/superpowers/specs/2026-10-12-avalonia-restart-design.md` (+ the 2026-10-08
migration design it builds on).

**Scope:** Batches 1-2 of the spec only. Batches 3-9 get their own plan each, written after the
owner approves the previous batch on the running app.

## Global constraints

- Version floor: Avalonia 12.1.4; versions live in `Directory.Packages.props` (central).
- `TreatWarningsAsErrors` is on: Windows-only APIs need `[SupportedOSPlatform("windows")]` on
  the class and an `OperatingSystem.IsWindows()` guard where they are registered (CA1416).
- Commit format `desktop: <type>: <title>` (tests: `tests: ...`, docs: `docs: docs: ...`), body
  with what was verified, no AI attribution. Pre-commit hook runs the full suite.
- Tests: `dotnet test --project tests/ClipShelf.Desktop.Tests/ClipShelf.Desktop.Tests.csproj -c Release`;
  full suite `test.bat --no-pause`. Never pass `--nologo`.
- Ask before any package restore of a new package (global rule): one approval at Task 1 covers
  the four Avalonia packages listed above.
- Stop any running `ClipShelf.exe` before building.
- UI tests: `Avalonia.Headless.XUnit` 12.1.4 fails discovery on xUnit v3 4.x (built against 3.2),
  so the test project references `Avalonia.Headless` and runs UI bodies through
  `Ui.Run(...)` (tests/ClipShelf.Desktop.Tests/TestApp.cs). Read every `[AvaloniaFact]` /
  `[AvaloniaTheory]` below as `[Fact]` / `[Theory]` whose body is wrapped in `Ui.Run`.

## Spec amendment taken in Task 2 (needs owner OK at plan approval)

The spec says `Colors.axaml` holds ClipShelf-named aliases onto Fluent keys. An alias written as
`<StaticResource x:Key="X" ResourceKey="FluentKey"/>` resolves once at load, so it can freeze on
the variant active at startup and not follow a later light/dark switch. Simpler and
theme-safe: **views use Fluent keys directly via `{DynamicResource}`; the only ClipShelf
resources are the exceptions** (`ClipShelfAccentSubtleBrush`, `ClipShelfCardShadow`), defined
per variant in `ThemeDictionaries`. Task 2's first test proves the exceptions flip with the
theme; the spec text is updated in the same commit.

## Review focus

1. A view references a key Fluent 12 does not define: DynamicResource fails silently
   (transparent). Guard: Task 2 test resolves every referenced key in Light and Dark.
2. Theme switch at runtime (settings or System following the OS): exceptions and Fluent keys
   must flip without restart. Task 2 + Task 3 tests.
3. A corrupt or truncated clipboard DIB: capture must skip it, never crash the app. Task 5 test.
4. Second launch while the app runs (`ClipShelf.exe --toggle`): must hand the command over and
   exit, not start a second tray icon. Task 7 manual check (pipe logic is already tested in
   Presentation).
5. Exit from the tray with `LockOnExit` on: locks reset before the host is disposed. Task 7 test.

---

## Batch 1 - scaffold and token rule

### Task 1: scaffold the Desktop head and its test project

**Files:**
- Delete (ignored build output left from the old branch): `src/ClipShelf.Desktop/`,
  `tests/ClipShelf.Desktop.Tests/` (only `bin/` and `obj/` exist there; confirm with
  `git status --ignored` first)
- Modify: `Directory.Packages.props`, `ClipShelf.slnx`
- Create: `src/ClipShelf.Desktop/ClipShelf.Desktop.csproj`, `Program.cs`, `App.axaml`,
  `App.axaml.cs`, `app.manifest` (copy from `src/ClipShelf.App/app.manifest`),
  `Assets/` (copy `app.ico`, `tray-*.ico` from `src/ClipShelf.App/Assets`)
- Create: `tests/ClipShelf.Desktop.Tests/ClipShelf.Desktop.Tests.csproj`, `TestApp.cs`,
  `AppBootTests.cs`

**Interfaces produced:** `ClipShelf.Desktop.App` (Avalonia `Application`),
`ClipShelf.Desktop.Program.BuildAvaloniaApp()`, test `[assembly: AvaloniaTestApplication]`.

- [x] Step 1: packages in `Directory.Packages.props`
```xml
<!-- Avalonia head (spec 2026-10-12). One version for all four. -->
<PackageVersion Include="Avalonia" Version="12.1.4" />
<PackageVersion Include="Avalonia.Desktop" Version="12.1.4" />
<PackageVersion Include="Avalonia.Themes.Fluent" Version="12.1.4" />
<PackageVersion Include="Avalonia.Headless.XUnit" Version="12.1.4" />
```
- [x] Step 2: test project + failing test
```xml
<!-- tests/ClipShelf.Desktop.Tests/ClipShelf.Desktop.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework><IsPackable>false</IsPackable></PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="Avalonia.Headless.XUnit" />
    <PackageReference Include="NSubstitute" />
  </ItemGroup>
  <ItemGroup><Using Include="Xunit" /></ItemGroup>
  <ItemGroup><ProjectReference Include="..\..\src\ClipShelf.Desktop\ClipShelf.Desktop.csproj" /></ItemGroup>
</Project>
```
```csharp
// TestApp.cs
using Avalonia;
using Avalonia.Headless;
[assembly: AvaloniaTestApplication(typeof(ClipShelf.Desktop.Tests.TestApp))]
namespace ClipShelf.Desktop.Tests;
public static class TestApp
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

// AppBootTests.cs
using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Themes.Fluent;
namespace ClipShelf.Desktop.Tests;
public sealed class AppBootTests
{
    [AvaloniaFact]
    public void The_app_runs_on_the_fluent_theme_only() =>
        Assert.IsType<FluentTheme>(Assert.Single(Application.Current!.Styles));
}
```
- [x] Step 3: run tests, expect FAIL (compile error: `ClipShelf.Desktop` / `App` missing).
- [x] Step 4: minimal head
```xml
<!-- src/ClipShelf.Desktop/ClipShelf.Desktop.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <AssemblyName>ClipShelf</AssemblyName>
    <RootNamespace>ClipShelf.Desktop</RootNamespace>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <ApplicationIcon>Assets\app.ico</ApplicationIcon>
    <NeutralLanguage>en-US</NeutralLanguage>
    <SatelliteResourceLanguages>ar</SatelliteResourceLanguages>
    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Avalonia" />
    <PackageReference Include="Avalonia.Desktop" />
    <PackageReference Include="Avalonia.Themes.Fluent" />
    <PackageReference Include="CommunityToolkit.Mvvm" />
    <PackageReference Include="Microsoft.Extensions.Hosting" />
    <PackageReference Include="Serilog.Extensions.Hosting" />
    <PackageReference Include="SQLitePCLRaw.bundle_e_sqlcipher" />
    <PackageReference Include="System.Security.Cryptography.ProtectedData" />
  </ItemGroup>
  <ItemGroup>
    <AvaloniaResource Include="Assets\**" />
    <Content Include="Assets\*.ico" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\ClipShelf.Core\ClipShelf.Core.csproj" />
    <ProjectReference Include="..\ClipShelf.Data\ClipShelf.Data.csproj" />
    <ProjectReference Include="..\ClipShelf.Presentation\ClipShelf.Presentation.csproj" />
  </ItemGroup>
</Project>
```
```xml
<!-- App.axaml -->
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="ClipShelf.Desktop.App"
             RequestedThemeVariant="Default">
  <Application.Styles>
    <FluentTheme />
  </Application.Styles>
</Application>
```
```csharp
// App.axaml.cs
using Avalonia;
using Avalonia.Markup.Xaml;
namespace ClipShelf.Desktop;
public sealed partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);
}

// Program.cs
using Avalonia;
namespace ClipShelf.Desktop;
internal static class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnExplicitShutdown);

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
}
```
  Add both projects to `ClipShelf.slnx` (`/src/` and `/tests/` folders).
- [x] Step 5: run Desktop tests, expect 1/1 PASS; run `test.bat --no-pause`, expect all green
  (310 + 1). If an Avalonia 12 API name differs, fix from the compiler error, not by guessing.
- [x] Step 6: commit `desktop: chore: scaffold the avalonia 12 head and its headless tests`.

### Task 2: the Fluent-only token rule and its guards

**Files:**
- Create: `src/ClipShelf.Desktop/Themes/Exceptions.axaml`, `tests/ClipShelf.Desktop.Tests/TokenRuleTests.cs`
- Modify: `App.axaml` (merge Exceptions), `DESIGN.md` (rewrite), spec section "What changes" item 3

**Interfaces produced:** resource keys `ClipShelfAccentSubtleBrush`, `ClipShelfCardShadow`
(`BoxShadows`); helper `TokenRuleTests.ReferencedKeys()` reused by later batches.

- [x] Step 1: failing tests
```csharp
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
namespace ClipShelf.Desktop.Tests;
public sealed class TokenRuleTests
{
    private static readonly string Src = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
        "..", "..", "..", "..", "..", "src", "ClipShelf.Desktop"));

    // Every {DynamicResource}/{StaticResource} key used by any Desktop .axaml.
    public static IEnumerable<string> ReferencedKeys() =>
        Directory.EnumerateFiles(Src, "*.axaml", SearchOption.AllDirectories)
            .SelectMany(f => Regex.Matches(File.ReadAllText(f), @"\{(?:Dynamic|Static)Resource\s+(\w+)\}"))
            .Select(m => m.Groups[1].Value).Distinct();

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Every_referenced_key_resolves(string variant)
    {
        var theme = variant == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;
        foreach (var key in ReferencedKeys().Append("ClipShelfAccentSubtleBrush").Append("ClipShelfCardShadow"))
            Assert.True(Application.Current!.TryGetResource(key, theme, out _), $"{key} missing in {variant}");
    }

    [AvaloniaFact]
    public void The_accent_tint_flips_with_the_theme()
    {
        Application.Current!.TryGetResource("ClipShelfAccentSubtleBrush", ThemeVariant.Light, out var light);
        Application.Current!.TryGetResource("ClipShelfAccentSubtleBrush", ThemeVariant.Dark, out var dark);
        Assert.NotEqual(((ISolidColorBrush)light!).Color, ((ISolidColorBrush)dark!).Color);
    }

    [Fact]
    public void Literal_colors_live_only_in_the_exceptions_file()
    {
        var offenders = Directory.EnumerateFiles(Src, "*.axaml", SearchOption.AllDirectories)
            .Where(f => Path.GetFileName(f) != "Exceptions.axaml")
            .Where(f => Regex.IsMatch(File.ReadAllText(f), "\"#[0-9A-Fa-f]{3,8}\""));
        Assert.Empty(offenders);
    }
}
```
- [x] Step 2: run, expect FAIL on the two exception keys (missing).
- [x] Step 3: implement
```xml
<!-- Themes/Exceptions.axaml: the only literal colors in the app. Each names the Fluent gap. -->
<ResourceDictionary xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <ResourceDictionary.ThemeDictionaries>
    <ResourceDictionary x:Key="Light">
      <!-- Fluent has no translucent accent tint for a selected rail item or row. -->
      <SolidColorBrush x:Key="ClipShelfAccentSubtleBrush" Color="#DBEAFE" />
      <!-- Fluent has no card elevation shadow token. -->
      <BoxShadows x:Key="ClipShelfCardShadow">0 4 16 -4 #1A000000</BoxShadows>
    </ResourceDictionary>
    <ResourceDictionary x:Key="Dark">
      <SolidColorBrush x:Key="ClipShelfAccentSubtleBrush" Color="#1E3A5F" />
      <BoxShadows x:Key="ClipShelfCardShadow">0 4 16 -4 #66000000</BoxShadows>
    </ResourceDictionary>
  </ResourceDictionary.ThemeDictionaries>
</ResourceDictionary>
```
  `App.axaml`: `<Application.Resources><ResourceDictionary><ResourceDictionary.MergedDictionaries><ResourceInclude Source="/Themes/Exceptions.axaml"/></ResourceDictionary.MergedDictionaries></ResourceDictionary></Application.Resources>`.
  (`#DBEAFE` light tint carries over from DESIGN.md; dark value is a first pick, tuned in Batch 8.)
- [x] Step 4: run, expect PASS; full suite green.
- [x] Step 5: rewrite `DESIGN.md` (short): Fluent 12 is the token source, views use Fluent keys
  via DynamicResource, the exceptions list with reasons, type sizes, components kept from WinUI
  `main`; drop all Memry hexes and "Retired" history. Update spec item 3 to the amendment above.
- [x] Step 6: commit `desktop: feat: fluent-only token rule with an exceptions block`.

### Task 3: theme and accent service

**Files:** Create `src/ClipShelf.Desktop/Services/ThemeService.cs`,
`tests/ClipShelf.Desktop.Tests/ThemeServiceTests.cs`.

**Interfaces:** consumes `IThemeService.Apply(AppSettings)` (Presentation),
`AccentPalette.Shades(string)` (Core). Produces `ThemeService : IThemeService`.

- [x] Step 1: failing tests
```csharp
public sealed class ThemeServiceTests
{
    [AvaloniaTheory]
    [InlineData(AppTheme.System, "Default")]
    [InlineData(AppTheme.Light, "Light")]
    [InlineData(AppTheme.White, "Light")]
    [InlineData(AppTheme.Dark, "Dark")]
    [InlineData(AppTheme.Black, "Dark")]
    public void The_app_theme_maps_to_a_variant(AppTheme theme, string expected)
    {
        new ThemeService().Apply(new AppSettings { Theme = theme });
        Assert.Equal(expected, Application.Current!.RequestedThemeVariant!.Key.ToString());
    }

    [AvaloniaFact]
    public void A_preset_accent_sets_the_fluent_accent_colors()
    {
        new ThemeService().Apply(new AppSettings { Accent = "#0F766E" });
        Assert.Equal(Color.Parse("#0F766E"), (Color)Application.Current!.Resources["SystemAccentColor"]!);
    }

    [AvaloniaFact]
    public void No_accent_restores_the_system_accent()
    {
        var service = new ThemeService();
        service.Apply(new AppSettings { Accent = "#0F766E" });
        service.Apply(new AppSettings { Accent = null });
        Assert.False(Application.Current!.Resources.ContainsKey("SystemAccentColor"));
    }
}
```
- [x] Step 2: run, expect FAIL (ThemeService missing).
- [x] Step 3: implement
```csharp
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using ClipShelf.Core.Theming;
namespace ClipShelf.Desktop.Services;

// Maps the saved theme and accent onto Fluent. Black renders as Dark until Batch 8 adds its
// surfaces. ponytail: no Black variant yet, add a custom ThemeVariant in the dark pass.
public sealed class ThemeService : IThemeService
{
    private static readonly string[] AccentKeys =
        ["SystemAccentColor", "SystemAccentColorLight1", "SystemAccentColorLight2", "SystemAccentColorLight3",
         "SystemAccentColorDark1", "SystemAccentColorDark2", "SystemAccentColorDark3"];

    public void Apply(AppSettings settings)
    {
        var app = Application.Current!;
        app.RequestedThemeVariant = settings.Theme switch
        {
            AppTheme.Light or AppTheme.White => ThemeVariant.Light,
            AppTheme.Dark or AppTheme.Black => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };

        foreach (var key in AccentKeys) app.Resources.Remove(key);
        if (settings.Accent is null) return;

        var s = AccentPalette.Shades(settings.Accent);
        string[] values = [s.Base, s.Light1, s.Light2, s.Light3, s.Dark1, s.Dark2, s.Dark3];
        for (var i = 0; i < AccentKeys.Length; i++) app.Resources[AccentKeys[i]] = Color.Parse(values[i]);
    }
}
```
- [x] Step 4: run, expect PASS; full suite green.
- [x] Step 5: commit `desktop: feat: theme and accent onto fluent`.

**Batch 1 gate:** owner runs `dotnet run --project src/ClipShelf.Desktop -c Release` (no window
is expected yet; process stays alive, `Get-Process ClipShelf`; stop it) and approves the token
rule and DESIGN.md.

---

## Batch 2 - host, platform, tray

### Task 4: UI dispatcher and data-change notifier

**Files:** Create `src/ClipShelf.Desktop/Platform/AvaloniaUiDispatcher.cs`,
`Services/DataChangeNotifier.cs` (copy from `src/ClipShelf.App/Services/DataChangeNotifier.cs`,
namespace `ClipShelf.Desktop.Services`), test `UiDispatcherTests.cs`.

- [x] Step 1: failing test
```csharp
public sealed class UiDispatcherTests
{
    [AvaloniaFact]
    public async Task Work_posted_from_a_pool_thread_runs_on_the_ui_thread()
    {
        var dispatcher = new AvaloniaUiDispatcher();
        var ran = new TaskCompletionSource<bool>();
        await Task.Run(() => dispatcher.Post(() => ran.SetResult(Dispatcher.UIThread.CheckAccess())));
        Dispatcher.UIThread.RunJobs();
        Assert.True(await ran.Task.WaitAsync(TimeSpan.FromSeconds(5)));
    }
}
```
- [x] Step 2: run, expect FAIL (type missing).
- [x] Step 3: implement
```csharp
using Avalonia.Threading;
namespace ClipShelf.Desktop.Platform;
public sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    public void Post(Action action) => Dispatcher.UIThread.Post(action);
}
```
- [x] Step 4: PASS; suite green. Step 5: commit `desktop: feat: ui dispatcher on avalonia`.

### Task 5: Skia image codec

**Files:** Create `src/ClipShelf.Desktop/Platform/ImageClipCodec.cs`, test
`ImageClipCodecTests.cs` (ported from `tests/ClipShelf.App.Tests/ImageClipCodecTests.cs`,
WinRT decode swapped for `SKBitmap.Decode`).

**Interfaces produced:** `static byte[]? ToPng(byte[] dib)`, `static byte[]? Thumbnail(byte[] png)`,
`static byte[]? ToDib(byte[] png)` (for clipboard image writes in Task 6). Synchronous: callers
already run them on `Task.Run`.

- [x] Step 1: failing tests (keep the `SampleDib` helper from the old test, made
  `internal static` so Task 6's clipboard test reuses it)
```csharp
[Fact] public void A_dib_becomes_a_png() =>
    Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, ImageClipCodec.ToPng(SampleDib(2, 2))!.Take(4));

[Fact] public void The_thumbnail_fits_the_row()
{
    using var thumb = SKBitmap.Decode(ImageClipCodec.Thumbnail(ImageClipCodec.ToPng(SampleDib(400, 200))!)!);
    Assert.True(thumb.Width <= 240 && thumb.Height <= 160);
}

[Fact] public void A_png_round_trips_through_a_dib()
{
    var png = ImageClipCodec.ToPng(SampleDib(3, 2))!;
    using var back = SKBitmap.Decode(ImageClipCodec.ToPng(ImageClipCodec.ToDib(png)!)!);
    Assert.Equal((3, 2), (back.Width, back.Height));
}

[Theory]
[InlineData(new byte[0])]
[InlineData(new byte[] { 40, 0, 0, 0, 1 })]          // header claims 40 bytes, truncated
public void A_corrupt_dib_is_skipped_not_thrown(byte[] dib) => Assert.Null(ImageClipCodec.ToPng(dib));
```
- [x] Step 2: run, expect FAIL (type missing).
- [x] Step 3: implement: wrap DIB in a 14-byte BMP header (logic copied from main's
  `WrapDibAsBmp`/`HeaderSize`), `SKBitmap.Decode`, return null on null bitmap or any exception;
  PNG encode via `SKImage.FromBitmap(b).Encode(SKEncodedImageFormat.Png, 100).ToArray()`;
  thumbnail scales by `min(1, 240/w, 160/h)` with `SKSamplingOptions(SKCubicResampler.Mitchell)`;
  `ToDib` decodes PNG, writes a 40-byte BITMAPINFOHEADER (32 bpp BI_RGB, bottom-up rows) plus
  BGRA pixels from `bitmap.Copy(SKColorType.Bgra8888)`.
- [x] Step 4: PASS; suite green. Step 5: commit `desktop: feat: skia image codec replacing winrt`.

### Task 6: Windows platform layer

**Files:** copy into `src/ClipShelf.Desktop/Platform/Windows/` (namespace
`ClipShelf.Desktop.Platform.Windows`, add `[SupportedOSPlatform("windows")]`), unchanged
otherwise: from `src/ClipShelf.App/Platform/` `NativeMethods`, `MessagePump`, `Win32Clipboard`,
`ForegroundApp`, `KeyboardPaste`, `SessionWatcher`, `SystemThemeService`, `SystemThemeWatcher`,
`TrayIconFiles`; from `src/ClipShelf.App/Services/` `ClipboardWatcher`, `DpapiKeyStore`,
`GlobalHotkey`, `StartupRegistration`, `ClipboardService`. Port tests
`tests/ClipShelf.App.Tests/TrayIconFileTests.cs` (+ `TestPaths` pointing at the Desktop project).
Create `Hosting/ServiceRegistration.cs` with `AddPlatformServices()` mirroring main's, minus
`ThemeService` WinUI (use Task 3's) and with `AvaloniaUiDispatcher`.

- [x] Step 1: port `TrayIconFileTests` and add a failing test for the one rewritten method,
  `ClipboardService.WriteImageAsync`, which must now go through `Win32Clipboard.TryWriteImage(byte[] dib)`
  (new, CF_DIB) using `ImageClipCodec.ToDib`:
```csharp
[Fact]
public void A_png_written_to_the_clipboard_reads_back_as_an_image()
{
    if (!OperatingSystem.IsWindows()) return;
    var png = ImageClipCodec.ToPng(ImageClipCodecTests.SampleDib(4, 4))!;
    Assert.True(Win32Clipboard.TryWriteImage(ImageClipCodec.ToDib(png)!));
    Assert.NotNull(Win32Clipboard.TryReadImage());
}
```
  (Touches the real clipboard of the test machine; that is project work, not a system change.)
- [x] Step 2: run, expect FAIL (`TryWriteImage` missing; ported tests compile-fail until copy).
- [x] Step 3: copy the files; add `TryWriteImage` to `Win32Clipboard` (OpenClipboard with the
  existing retry loop, EmptyClipboard, GlobalAlloc(GMEM_MOVEABLE) + copy, SetClipboardData(CF_DIB=8));
  rewrite `ClipboardService.WriteImageAsync` to `Task.Run(() => Win32Clipboard.TryWriteImage(dib))`
  and drop the WinRT stream field; `CaptureImageAsync` uses `ImageClipCodec.ToPng/Thumbnail`.
- [x] Step 4: PASS; `dotnet build -c Release` has zero warnings; suite green.
- [x] Step 5: commit `desktop: feat: windows platform layer on the avalonia head`.

### Task 7: host, single instance and tray

**Files:** Create `src/ClipShelf.Desktop/Shell/AppLifetime.cs` (copy), `Shell/AppShellService.cs`
(port), modify `App.axaml.cs` (startup sequence), `Program.cs` (SQLCipher init, settings,
culture, logging before Avalonia starts); test `AppShellServiceTests.cs`.

**Interfaces:** consumes everything above plus Presentation's `SingleInstanceService`,
`StartupCommand`, `AddPresentationServices`. Produces `AppShellService` with `ExitRequested`,
`Exit()`, `TogglePanel()`, `ShowHistory()`, `ShowSettings()` (the last three log-only until
Batches 3-4 add windows).

- [x] Step 1: failing test
```csharp
public sealed class AppShellServiceTests
{
    [AvaloniaFact]
    public void Exit_resets_locks_first_when_lock_on_exit_is_on()
    {
        var locks = Substitute.For<ICategoryLockService>();
        var order = new List<string>();
        locks.When(l => l.Reset()).Do(_ => order.Add("reset"));
        var shell = AppShellServiceFactory.Create(locks, new AppSettings { LockOnExit = true });
        shell.ExitRequested += (_, _) => order.Add("exit");
        shell.Exit();
        Assert.Equal(["reset", "exit"], order);
    }
}
```
  (`AppShellServiceFactory` is a test helper building the service with NSubstitute fakes for
  `IGlobalHotkey`, `ISystemTheme` and a real `SettingsService` over a temp `ISettingsStore`.)
- [x] Step 2: run, expect FAIL.
- [x] Step 3: port `AppShellService` from main: `TrayIcon` built in code, added with
  `TrayIcon.SetIcons(Application.Current, new TrayIcons { _tray })` (12.1.3+ needs the
  collection attachment), `NativeMenu` with Open history, Settings, separator, Exit; tray
  variant via `TrayIconFiles` + `ISystemTheme`; hotkey via `HotkeyRegistrationState` exactly as
  main; depends on `IGlobalHotkey` (not the concrete class) so the test can fake it. Port
  main's `App.OnLaunched` sequence into `App.OnFrameworkInitializationCompleted` (single
  instance claim and command hand-off, host build with `AddPresentationServices`,
  `AddPlatformServices`, `AddUiServices`, DB `BackupAndMigrate`, warm-up, startup registration,
  `ClipboardService`, `SessionCleanup`, retention prune); `ExitRequested` disposes the host and
  calls `desktop.Shutdown()`. Welcome window and real History/Settings come in later batches.
- [x] Step 4: PASS; suite green.
- [x] Step 5: manual (Windows): stop any ClipShelf, run the Release build; check: tray icon
  appears with the right light/dark variant; copy text and an image (Snipping Tool); second
  launch `ClipShelf.exe --toggle` exits and the log shows the command received; Exit from the
  tray ends the process (`Get-Process ClipShelf` empty). Then launch main's WinUI build and
  confirm the captured clips appear in its History (same database).
- [x] Step 6: commit `desktop: feat: tray-first host with single instance on avalonia`.

**Batch 2 gate:** owner repeats the Step 5 checks and approves.

## Verification (end of plan)

- `test.bat --no-pause`: all suites green, Desktop suite included.
- `dotnet build ClipShelf.slnx -c Release`: zero warnings.
- Manual checks of Task 7 Step 5 done by the owner.
