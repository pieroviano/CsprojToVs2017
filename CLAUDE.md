# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

CsprojToVs2017 converts legacy (VS2015-era) MSBuild project files to the modern SDK-style format.
It ships as a set of NuGet packages under the `Net4x.*` prefix, plus two .NET global tools
(`dotnet migrate-2017`, `dotnet migrate-2019`) and an MCP server.

## Commands

```bash
# Build everything
dotnet build Project2015To2017.sln

# Run the full test suite (MSTest, net8.0)
dotnet test Project2015To2017.Tests/Project2015To2017.Tests.csproj

# Run a single test or test class
dotnet test Project2015To2017.Tests/Project2015To2017.Tests.csproj --filter "FullyQualifiedName~XamlTransformationTest"
dotnet test Project2015To2017.Tests/Project2015To2017.Tests.csproj --filter "Name=TransformsPresentationPages"

# Run a CLI against a project without installing it
dotnet run --project Project2015To2017.Migrate2019.Tool -- wizard "C:\path\to\Some.sln"

# Run the MCP server (stdio) / inspect its tool surface
dotnet run --project Project2015To2017.Mcp.Server
npx @modelcontextprotocol/inspector dotnet run --project Project2015To2017.Mcp.Server
```

### Packing

The two CLI tools only become `dotnet tool` packages when `Pack=true` is set — that property flips
them from multi-targeting (`net461;net6.0` / `net461;net8.0`) to a single TFM with `PackAsTool`:

```bash
dotnet pack ./Project2015To2017.Migrate2019.Tool/Project2015To2017.Migrate2019.Tool.csproj -c Release --force /p:Pack=true
```

Library projects have `GeneratePackageOnBuild=true`, so a plain `dotnet build` already produces
nupkgs. They go to `$(SolutionDir)Packages/` — **build through the .sln**, because building a single
csproj directly leaves `SolutionDir` empty and drops a stray `Packages/` folder inside the project
directory.

CI (`appveyor.yml`) packs the library and both tools, publishes the tools for each TFM, and deploys
to NuGet from `master`.

## Architecture

Three layers plus front ends. The important separation is **engine vs. policy**: `Core` knows *how*
to read, transform and write a project; it holds no opinion about what a VS2017 or VS2019 project
should look like. That lives in the `Migrate20XX.Library` packages.

```
Project2015To2017.Core                 engine: model, readers, writer, pipeline, analysis
  ├─ Project2015To2017.Migrate2017.Library      VS15 policy: Vs15TransformationSet / Vs15DiagnosticSet
  │    └─ Project2015To2017.Migrate2019.Library VS16 policy, layered on VS15
  └─ Project2015To2017                   "Base" package: MigrationFacility façade

front ends (all built on MigrationFacility):
  Project2015To2017.Migrate2017.Tool     dotnet migrate-2017
  Project2015To2017.Migrate2019.Tool     dotnet migrate-2019
  Project2015To2017.Mcp.Server           MCP stdio server, dotnet-migrate-2019-mcp
```

`Project2015To2017.MigrateXXXX.Tool/` is **not a project** — it is a bare `.proj` file that both CLI
tools `<Import>` to share their entire command-line implementation (`ProgramBase.*`,
`CommandLogic.*`, a vendored `Microsoft.DotNet.Cli.CommandLine`). Each tool's own `Program.cs` only
wires the command names to a transformation set. **A change under `MigrateXXXX.Tool` affects both
CLIs.**

### The transformation pipeline

This is the part that requires reading several files to understand, and the part most changes touch.

A `Project` is an `XDocument` plus a parsed view of it (`PropertyGroups`, `ItemGroups`,
`PackageReferences`, `AssemblyAttributes`, …). Transformations mutate it in place:

```csharp
public interface ITransformation { void Transform(Project definition); }
```

Transformations are grouped into `ITransformationSet`s and combined with `ChainTransformationSet`.
`ProjectConverter.ProcessProjectFile` (`Project2015To2017.Core/ProjectConverter.cs:60`) runs
`conversionOptions.PreDefaultTransforms`, then the set, then `PostDefaultTransforms`.

**Declaration order is not execution order.** `Extensions.CollectAndOrderTransformations`
(`Project2015To2017.Core/ExtensionsGraph.cs:12`) does the ordering:

1. Partition by `ITransformationWithTargetMoment.ExecutionMoment` — `Early`, `Normal`, `Late`
   (a transformation that does not implement the interface is treated as `Normal`).
2. Topologically sort within each partition using `ITransformationWithDependencies`.

Then `WhereSuitable` (`Project2015To2017.Core/ExtensionsGraph.cs:53`) filters by project kind:
`ILegacyOnlyProjectTransformation` is skipped on modern (CPS) projects and
`IModernOnlyProjectTransformation` on legacy ones — unless the transformation's **type name** appears
in `ConversionOptions.ForceDefaultTransforms`, which is what the CLI's `--force-transformations`
exposes.

Consequence: to place a new transformation you declare a moment and dependencies, you do not reorder
a list.

### Transformation set composition

Every `XxxTransformationSet` exposes two statics, and the distinction matters:

* `TrueInstance` — only that set's own transformations, for composing into a chain.
* `Instance` — the full ready-to-run pipeline: `BasicReadTransformationSet` +
  `BasicSimplifyTransformationSet(TargetVisualStudioVersion)` + `TrueInstance`.

Pass `Instance` to `MigrationFacility`. Use `TrueInstance` when building a chain by hand — the
wizard paths in both `Program.cs` files do exactly this.

`BasicSimplifyTransformationSet` is version-parameterised (`Version(15,0)` vs `Version(16,0)`), which
is how generic simplification differs between the two targets without duplicating transformations.

`Vs16TransformationSet` is `Vs15TransformationSet` with `FrameworkReferencesTransformation` swapped
for `Vs16FrameworkReferencesTransformation`, which selects a real project SDK
(`Microsoft.NET.Sdk.WindowsDesktop` for WPF/WinForms, `MSBuild.Sdk.Extras` for Xamarin/UAP, detected
by project type GUID) instead of forcing everything into `Microsoft.NET.Sdk`.

### Analysis

Diagnostics are a separate pass over the same `Project` objects: `DiagnosticBase` implementations
gathered into a `DiagnosticSet`, run by `Analyzer<TReporter, TReporterOptions>` and surfaced through
`LoggerReporter`. Codes are `W0xx`, numbered by concern — `W001`/`W002`/`W010`/`W011` in Core,
`W020`–`W021` (modern issues) and `W030`–`W034` (modernization tips) in the VS15 library.
`Vs16DiagnosticSet.All` currently just unions `Vs15DiagnosticSet.All`.

### Reading

`ProjectReader` / `SolutionReader` (`.sln` and `.slnx`) build the model. `Reading/Conditionals/`
contains a full MSBuild `Condition` expression scanner/parser/evaluator (adapted from MSBuild
itself), used to decide which conditional groups apply. Treat it as vendored code — change it only
for genuine evaluation bugs.

## Where things go

| Change | Location |
|--------|----------|
| New generic transformation (applies to any target) | `Project2015To2017.Core/Transforms/` |
| Transformation specific to VS2017 conventions | `Project2015To2017.Migrate2017.Library/Transforms/` + register in `Vs15TransformationSet` |
| Transformation specific to VS2019 conventions | `Project2015To2017.Migrate2019.Library/Transforms/` + register in `Vs16TransformationSet` |
| New diagnostic | `…/Diagnostics/W0xx….cs` + add to the matching `DiagnosticSet` |
| CLI command, flag, or wizard behaviour | `Project2015To2017.MigrateXXXX.Tool/` (shared by both tools) |
| MCP tool surface | `Project2015To2017.Mcp.Server/Tools/MigrationTools.cs` |

## Testing conventions

MSTest, one test class per transformation or reader. Fixtures live in
`Project2015To2017.Tests/TestFiles/` as `*.testcsproj` / `*.testsln` — the non-standard extensions
keep MSBuild from treating them as real projects, and each is copied to the output directory by an
explicit `<None Include>` entry in the test csproj. **A new fixture file needs that entry added or it
will not be there at run time.**

`Core` has `[assembly: InternalsVisibleTo("Project2015To2017.Tests")]`, so internals are testable.
The test project references `Core`, `Migrate2017.Library` and `Project2015To2017` — not the 2019
library.

## Conventions

* **Tabs**, 4 wide, UTF-8, trimmed trailing whitespace (`.editorconfig`).
* Logging is always `Microsoft.Extensions.Logging.ILogger` in the libraries; Serilog appears only in
  the CLI front ends. Use `NoopLogger.Instance` where a logger is required but unwanted.
* **Never write to stdout from library code.** The MCP server uses stdio for JSON-RPC and clears all
  logging providers at startup; a stray `Console.Write` corrupts the protocol. Interactive prompts
  are confined to the wizard paths in `MigrateXXXX.Tool`.
* Package versions come from `Directory.NuGet.props` (`Project2015To2017Version`); the build appends
  a date-derived `VersionSuffix` (`yy` + day-of-year) in `Directory.Build.props`.
* Each packable project carries its own `README.md`, packed as `PackageReadmeFile`. Keep it current
  when the package's public surface changes.
