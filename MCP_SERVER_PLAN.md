# Implementation Plan — MCP Server for `dotnet-migrate-2019`

**Goal:** Add a new project to the `Project2015To2017.sln` solution that exposes the
migration functionality currently available through `dotnet-migrate-2019.exe` as a
**Model Context Protocol (MCP) server**, so MCP clients (Claude Desktop, IDE extensions,
etc.) can analyze, evaluate, and migrate legacy MSBuild projects/solutions.

## Decisions locked in (from the clarifying round)

| Topic | Decision | Consequence |
|-------|----------|-------------|
| **Transport** | **stdio** | stdout is the JSON-RPC channel. All logging/console output MUST be moved off stdout (→ stderr or an in-memory capture), otherwise the protocol stream is corrupted. This is the single biggest integration constraint. |
| **Operations** | **Analyze + Evaluate + Migrate** | Three MCP tools. `migrate` rewrites `.csproj`/`.sln` files on disk (backups ON by default). The interactive `wizard` flow is **not** exposed (it needs a human at a console). |
| **Integration** | **Reference the library code directly** | Add `ProjectReference`s and call `MigrationFacility` in-process. No subprocess, no output scraping, structured results. |
| **Unknown TFM** | **Caller passes target frameworks** | Each tool accepts an optional `targetFrameworks` parameter mapped to `ConversionOptions.TargetFrameworks`, used when a project's framework can't be resolved (e.g. `$(MDFrameworkVersion)`). |

---

## ⚠️ Blocking verification steps (do these BEFORE writing code)

These are assumptions that, if wrong, change the whole approach. Verify first.

1. **MCP C# SDK availability & version.** The server uses the official
   [`ModelContextProtocol`](https://www.nuget.org/packages/ModelContextProtocol) NuGet
   package (currently *preview*). Confirm the exact latest version and its API shape
   (`.WithStdioServerTransport()`, `[McpServerToolType]`/`[McpServerTool]`,
   `.WithToolsFromAssembly()`). **The version pinned below is a placeholder — verify it
   resolves and that the builder API matches before relying on the code samples.**
   ```bash
   dotnet package search ModelContextProtocol --prerelease
   ```

2. **`net8.0` can consume the library projects.** `Project2015To2017` (MigrationFacility),
   `Project2015To2017.Core`, and `Project2015To2017.Migrate2019.Library` all target
   `netstandard2.0;net461` — **not** `net8.0`. `netstandard2.0` is consumable from `net8.0`
   (the existing `Migrate2019.Tool` already references them from its `net8.0` target), so
   this is expected to work, but confirm a trivial `net8.0` console app referencing
   `Project2015To2017.csproj` builds and runs `new MigrationFacility(...)` before building
   the full server.

3. **The MCP SDK requires `net8.0`+.** The new project must target `net8.0` (or `net9.0`).
   It cannot multi-target down to `net461` like the tool does. Confirm the build agents /
   `appveyor.yml` have the `net8.0` SDK (they already build the tool's `net8.0` target, so
   this should hold).

4. **Logging isolation on stdio.** Confirm that `MigrationFacility`'s `ILogger` output is
   the ONLY console output path we must intercept (there are no stray `Console.WriteLine`
   calls on the non-interactive code paths). The interactive prompts (`Console.ReadLine`,
   `AskBinaryChoice`) live only in the wizard flow, which we are **not** exposing —
   verified in `CommandLogic.*` and `CommandLogic.AskBinaryChoice.cs`. Grep to be sure:
   ```bash
   grep -rn "Console\." Project2015To2017/ Project2015To2017.Core/
   ```

---

## Architecture overview

```
MCP client (Claude Desktop / IDE)
        │  stdio (JSON-RPC)
        ▼
Project2015To2017.Mcp.Server (net8.0, NEW)
        │  in-process calls
        ▼
MigrationFacility  (Project2015To2017, netstandard2.0)
        ├── ParseProjects / DoAnalysis / ExecuteEvaluate / ExecuteMigrate
        └── ILogger  ──►  CapturingLogger (collects messages per request)
```

Key design point: we call **`MigrationFacility` directly rather than `CommandLogic`**,
because `CommandLogic` constructs its own `SerilogLoggerProvider` logger internally
(`CommandLogic.cs:34`) that we cannot intercept. Talking to `MigrationFacility` lets us
inject a **capturing `ILogger`** so each tool call returns the diagnostics/warnings/errors
as structured text instead of writing them to a console we don't control.

Reference for the exact transformation sets / options each operation uses today —
`Program.cs` in the Migrate2019 tool:
- `analyze` → `Program.cs:104` → `AnalysisOptions(Vs16DiagnosticSet.All)`
- `evaluate` → `Program.cs:101` → `Vs16TransformationSet.Instance` + `AnalysisOptions(Vs16DiagnosticSet.All)`
- `migrate` → `Program.cs:113` → `Vs16TransformationSet.Instance`, `no-backup` flag

---

## New project layout

```
Project2015To2017.Mcp.Server/
├── Project2015To2017.Mcp.Server.csproj
├── Program.cs                     # host builder + stdio transport wiring
├── Logging/
│   └── CapturingLoggerProvider.cs # ILogger that collects entries into a per-request buffer
├── MigrationServer.cs             # helpers: build ConversionOptions, run within a capture scope
└── Tools/
    └── MigrationTools.cs          # [McpServerToolType] with Analyze / Evaluate / Migrate tools
```

---

## File-by-file plan

### 1. `Project2015To2017.Mcp.Server.csproj` (new)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!-- MCP SDK needs modern runtime; cannot multi-target to net461 -->
    <TargetFramework>net8.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <AssemblyName>dotnet-migrate-2019-mcp</AssemblyName>
    <PackageId>Project2015To2017.Mcp.Server</PackageId>
    <Product>Project2015To2017.Mcp.Server</Product>
    <Nullable>enable</Nullable>
    <NoWarn>NU1900;NU1901;NU1902;NU1903</NoWarn>
    <!-- IMPORTANT: keep tool packaging OFF for this project; the shared props in
         Directory.Build.props set Version=4.2.0 which we inherit automatically. -->
  </PropertyGroup>

  <ItemGroup>
    <!-- Verify latest version in blocking step #1 before committing to this number -->
    <PackageReference Include="ModelContextProtocol" Version="0.3.0-preview.4" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Project2015To2017\Project2015To2017.csproj" />
    <ProjectReference Include="..\Project2015To2017.Core\Project2015To2017.Core.csproj" />
    <ProjectReference Include="..\Project2015To2017.Migrate2019.Library\Project2015To2017.Migrate2019.Library.csproj" />
  </ItemGroup>

</Project>
```

Notes:
- Do **not** import `..\Project2015To2017.MigrateXXXX.Tool\Project2015To2017.MigrateXXXX.Tool.proj`
  (that pulls in `CommandLogic` and the interactive command-line parser we're deliberately
  avoiding).
- `Directory.Build.props` at the repo root is auto-imported and adds `Microsoft.SourceLink.GitHub`
  plus `Version=4.2.0` — no action needed, just be aware the server inherits them.

### 2. `Logging/CapturingLoggerProvider.cs` (new)

An `ILoggerProvider` whose loggers append formatted entries (level + message +
exception) to a thread-safe buffer bound to the current tool call. Two responsibilities:
1. Provide the `ILogger` passed to `MigrationFacility`.
2. Let the tool method drain the buffer into the MCP tool result text.

```csharp
using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Project2015To2017.Mcp.Server.Logging;

/// <summary>
/// Captures all log output produced during a single migration operation so it can be
/// returned in the MCP tool result. Nothing is written to Console — on stdio transport
/// stdout is reserved for the JSON-RPC protocol.
/// </summary>
public sealed class CaptureScope : IDisposable
{
    private readonly ConcurrentQueue<string> lines = new();
    private volatile bool hasError;

    public bool HasError => hasError;

    internal void Append(LogLevel level, string message, Exception? ex)
    {
        if (level >= LogLevel.Error) hasError = true;
        var sb = new StringBuilder().Append('[').Append(level).Append("] ").Append(message);
        if (ex != null) sb.Append(" :: ").Append(ex.GetType().Name).Append(": ").Append(ex.Message);
        lines.Enqueue(sb.ToString());
    }

    public string Drain() => string.Join(Environment.NewLine, lines);

    public void Dispose() { /* buffer is GC'd with the scope */ }
}

public sealed class CapturingLoggerProvider : ILoggerProvider
{
    // One active scope per operation. Operations are serialized (see MigrationTools).
    private CaptureScope? current;

    public CaptureScope BeginScope() => current = new CaptureScope();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(() => current);

    public void Dispose() { }

    private sealed class CapturingLogger : ILogger
    {
        private readonly Func<CaptureScope?> scope;
        public CapturingLogger(Func<CaptureScope?> scope) => this.scope = scope;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            scope()?.Append(logLevel, formatter(state, exception), exception);
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
```

> **Concurrency note:** MCP tool calls can arrive concurrently. Because the capturing
> provider uses a single "current" scope, tool operations MUST be serialized. The plan
> uses a `SemaphoreSlim(1,1)` in `MigrationTools` (below). This matches the CLI's
> one-operation-at-a-time model and avoids interleaved log capture. If true concurrency is
> ever needed, switch to an `AsyncLocal<CaptureScope>` instead of a single field.

### 3. `MigrationServer.cs` (new) — shared option construction

Centralizes translation of tool parameters → `ConversionOptions` so the three tools stay
consistent with `Program.cs:62-76`.

```csharp
using Project2015To2017;
using Project2015To2017.Caching;

namespace Project2015To2017.Mcp.Server;

internal static class MigrationServer
{
    public static ConversionOptions BuildOptions(
        string[]? targetFrameworks, bool force, bool keepAssemblyInfo, bool appendTfmToOutputPath)
    {
        var options = new ConversionOptions
        {
            ProjectCache = new DefaultProjectCache(),
            ForceOnUnsupportedProjects = force,
            KeepAssemblyInfo = keepAssemblyInfo,
            AppendTargetFrameworkToOutputPath = appendTfmToOutputPath,
        };
        if (targetFrameworks is { Length: > 0 })
            options.TargetFrameworks = targetFrameworks;   // used when TFM can't be resolved
        return options;
    }
}
```

> **On "Caller passes target frameworks":** `ConversionOptions.TargetFrameworks`
> (`ConversionOptions.cs:23`) is an *override* the readers/transforms honour. We do **not**
> set `UnknownTargetFrameworkCallback` (that path prompts a human — `CommandLogic.cs:76`).
> With the callback null and `TargetFrameworks` supplied, projects whose framework can't be
> parsed (e.g. `$(MDFrameworkVersion)`) take the caller-provided frameworks. If the caller
> omits `targetFrameworks` AND a project's TFM is unresolvable, the reader logs an error and
> continues with no framework (post-fix behavior in `ProjectPropertiesReader.cs:100-105`) —
> the tool result will surface that error text from the capture buffer.

### 4. `Tools/MigrationTools.cs` (new) — the MCP tools

```csharp
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Project2015To2017;
using Project2015To2017.Analysis;
using Project2015To2017.Migrate2019.Library;
using Project2015To2017.Mcp.Server.Logging;
using Project2015To2017.Transforms;
using Project2015To2017.Writing;

namespace Project2015To2017.Mcp.Server.Tools;

[McpServerToolType]
public sealed class MigrationTools
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private readonly CapturingLoggerProvider loggerProvider;
    private readonly ILogger facilityLogger;

    public MigrationTools(CapturingLoggerProvider loggerProvider, ILoggerFactory loggerFactory)
    {
        this.loggerProvider = loggerProvider;
        this.facilityLogger = loggerFactory.CreateLogger("Migration");
    }

    [McpServerTool(Name = "analyze")]
    [Description("Analyze one or more legacy MSBuild project or solution files and report " +
                 "diagnostics without modifying anything.")]
    public async Task<string> Analyze(
        [Description("Absolute paths to .csproj/.vbproj/.fsproj/.sln/.slnx files or a directory.")]
        string[] paths,
        [Description("Optional target frameworks (e.g. [\"net48\"]) used when a project's " +
                     "framework cannot be determined.")]
        string[]? targetFrameworks = null,
        [Description("Force processing of otherwise-unsupported project types.")]
        bool force = false)
        => await Run(paths, () =>
        {
            var facility = new MigrationFacility(facilityLogger);
            var options = MigrationServer.BuildOptions(targetFrameworks, force, false, true);
            facility.ExecuteAnalyze(paths, options, new AnalysisOptions(Vs16DiagnosticSet.All));
        });

    [McpServerTool(Name = "evaluate")]
    [Description("Dry-run evaluation: reports what migration would do plus diagnostics, " +
                 "without writing files.")]
    public async Task<string> Evaluate(
        [Description("Absolute paths to project/solution files or a directory.")]
        string[] paths,
        string[]? targetFrameworks = null,
        bool force = false)
        => await Run(paths, () =>
        {
            var facility = new MigrationFacility(facilityLogger);
            var options = MigrationServer.BuildOptions(targetFrameworks, force, false, true);
            facility.ExecuteEvaluate(paths, options, Vs16TransformationSet.Instance,
                new AnalysisOptions(Vs16DiagnosticSet.All));
        });

    [McpServerTool(Name = "migrate")]
    [Description("Migrate legacy projects to SDK-style. WRITES to disk. Creates .backup " +
                 "files unless noBackup is true.")]
    public async Task<string> Migrate(
        [Description("Absolute paths to project/solution files or a directory.")]
        string[] paths,
        [Description("Target frameworks to write (e.g. [\"net48\",\"netstandard2.1\"]). " +
                     "Also used when a framework cannot be determined.")]
        string[]? targetFrameworks = null,
        [Description("Skip creating .backup copies of modified files. Default false (backups ON).")]
        bool noBackup = false,
        [Description("Keep AssemblyInfo.cs instead of folding attributes into the project file.")]
        bool keepAssemblyInfo = false,
        [Description("Write output to the legacy (non-TFM-suffixed) output path.")]
        bool oldOutputPath = false,
        bool force = false)
        => await Run(paths, () =>
        {
            var facility = new MigrationFacility(facilityLogger);
            var options = MigrationServer.BuildOptions(
                targetFrameworks, force, keepAssemblyInfo, appendTfmToOutputPath: !oldOutputPath);
            var writeOptions = new ProjectWriteOptions { MakeBackups = !noBackup };
            facility.ExecuteMigrate(paths, Vs16TransformationSet.Instance, options, writeOptions,
                new AnalysisOptions(Vs16DiagnosticSet.All));
        });

    // Serializes operations and captures all log output for the response.
    private async Task<string> Run(string[] paths, Action operation)
    {
        if (paths is not { Length: > 0 })
            return "[Error] No paths supplied.";

        await Gate.WaitAsync();
        var scope = loggerProvider.BeginScope();
        try
        {
            operation();
        }
        catch (Exception ex)
        {
            scope.Append(LogLevel.Error, "Operation failed", ex);
        }
        finally
        {
            Gate.Release();
        }

        var output = scope.Drain();
        return string.IsNullOrWhiteSpace(output)
            ? "Completed with no output."
            : output;
    }
}
```

> **Why `Vs16TransformationSet.Instance` and not `.TrueInstance`:** the non-wizard commands
> in `Program.cs` (`evaluate`/`migrate`) use `.Instance`; only the interactive wizard uses
> `.TrueInstance` chained with modernization sets (`Program.cs:83-98`). We mirror the
> non-interactive commands exactly.

### 5. `Program.cs` (new) — host + stdio wiring

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Project2015To2017.Mcp.Server.Logging;

var builder = Host.CreateApplicationBuilder(args);

// CRITICAL for stdio: remove console logging so stdout carries only JSON-RPC.
builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Register the capturing provider both as a provider (so MigrationFacility's ILogger is
// captured) and as a singleton (so the tool can drain the buffer).
var capturing = new CapturingLoggerProvider();
builder.Logging.AddProvider(capturing);
builder.Services.AddSingleton(capturing);

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();   // discovers [McpServerToolType] MigrationTools

await builder.Build().RunAsync();
```

> If any diagnostic logging to **stderr** is desired for debugging, add a console provider
> explicitly configured to `Console.Error` — never the default console provider, which
> writes to stdout and would corrupt the protocol.

---

## Solution integration

Add the project to `Project2015To2017.sln` (the project GUIDs use the CPS C# type GUID
`{9A19103F-16F7-4668-BE54-9A1E7A4F7556}`, consistent with existing entries):

```bash
dotnet sln Project2015To2017.sln add Project2015To2017.Mcp.Server/Project2015To2017.Mcp.Server.csproj
```

If `dotnet sln add` is unavailable, add manually mirroring the existing entries
(`Project2015To2017.sln:16-26`) plus matching `GlobalSection(ProjectConfigurationPlatforms)`
lines for Debug/Release AnyCPU.

---

## Verification steps

1. **Build the solution.**
   ```bash
   dotnet build Project2015To2017.sln
   ```
   Expect 0 errors. Confirm `Project2015To2017.Mcp.Server` produces
   `dotnet-migrate-2019-mcp.dll`.

2. **Smoke-test the server over stdio** with the MCP inspector (or a manual JSON-RPC
   `initialize` + `tools/list` handshake):
   ```bash
   npx @modelcontextprotocol/inspector dotnet run --project Project2015To2017.Mcp.Server
   ```
   Confirm `analyze`, `evaluate`, `migrate` appear in `tools/list` with the described
   parameters.

3. **Functional parity — analyze.** Point `analyze` at a known legacy project and compare
   its returned diagnostics against `dotnet-migrate-2019 analyze <same path>`. The set of
   diagnostic codes should match.

4. **Functional parity — migrate (in a throwaway copy).** Copy a small legacy project to a
   temp dir, run `migrate` with `targetFrameworks:["net48"]`, and confirm the `.csproj` is
   rewritten SDK-style and `.backup` files exist. Repeat with `noBackup:true` and confirm no
   backups.

5. **stdout cleanliness.** Capture raw stdio and confirm every non-JSON-RPC line (all the
   `[Information]/[Warning]/[Error]` migration output) is returned *inside* tool results, and
   nothing leaks onto stdout outside the JSON-RPC frames. This is the make-or-break check for
   stdio transport.

6. **Unknown-TFM path.** Run `evaluate` against the MonoDevelop `Main.sln`
   (`$(MDFrameworkVersion)` case) with `targetFrameworks:["net48"]` and confirm it no longer
   errors on that project and the result reflects `net48`.

7. **Client registration (manual).** Register the server in an MCP client config, e.g.:
   ```json
   {
     "mcpServers": {
       "migrate-2019": {
         "command": "dotnet",
         "args": ["run", "--project", "D:\\CommonLibrary\\CsprojToVs2017\\Project2015To2017.Mcp.Server"]
       }
     }
   }
   ```
   Confirm the client lists and can invoke the tools.

---

## Risks & open items

| Risk | Impact | Mitigation |
|------|--------|-----------|
| MCP C# SDK is preview; API may drift | Build/wiring breaks against the pinned version | Blocking step #1: verify version & builder API before coding. Pin an exact version; revisit on SDK updates. |
| Any stray `Console.Write*` on the non-wizard paths | Corrupts stdio JSON-RPC | Blocking step #4 grep. If found, wrap or redirect. The interactive prompts are wizard-only and not exposed. |
| Concurrent tool calls interleave captured logs | Garbled/incorrect result text | `SemaphoreSlim(1,1)` serializes operations (documented in `MigrationTools`). Upgrade to `AsyncLocal` scope if concurrency becomes a requirement. |
| `migrate` writes to disk on the user's machine | Data loss if misused | Backups ON by default; tool description flags it as destructive; leave the client's per-tool approval as the human gate. Consider adding a `dryRun` alias that just calls `evaluate`. |
| `MigrationFacility` relies on the current working directory for glob/directory search (`MigrationFacility.cs:97`) | Ambiguous results when relative paths are passed | Require/prefer absolute paths in tool descriptions; optionally set `Directory.SetCurrentDirectory` per call under the same `Gate`. Decide during implementation. |
| Results are unstructured text (drained log lines), not typed JSON | Clients can't machine-read diagnostics precisely | Acceptable for v1 (matches CLI output). A follow-up could add a custom `IReporter` to emit structured `DiagnosticResult` JSON — noted as future work, not in scope now. |
| Server inherits `Version=4.2.0` and SourceLink from `Directory.Build.props` | Unexpected package metadata if ever packed | Fine as-is; do not set `PackAsTool`. Only revisit if publishing the server as a package. |

---

## Out of scope (explicitly not doing)

- Exposing the interactive **wizard** flow (needs a console human; would require re-plumbing
  every `AskBinaryChoice`/`Console.ReadLine` into parameters).
- HTTP/SSE transport (stdio chosen).
- Structured/typed JSON diagnostic output (v1 returns captured log text).
- Multi-targeting the server to `net461` (MCP SDK requires modern runtime).
