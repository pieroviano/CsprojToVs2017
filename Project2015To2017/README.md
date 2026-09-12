# Net4x.Project2015To2017.Base

High-level entry point for [CsprojToVs2017](https://github.com/hvanbakel/CsprojToVs2017) — the tool
that converts legacy (VS2015-era) MSBuild project files to the modern SDK-style format.

This package provides `MigrationFacility`, the façade that ties together file discovery, project and
solution parsing, transformation and analysis. It is what the `dotnet migrate-2017` /
`dotnet migrate-2019` global tools and the MCP server are built on, and it is the package to use when
you want to drive migration from your own code.

## Installation

```
dotnet add package Net4x.Project2015To2017.Base
```

**Target frameworks:** `netstandard2.0`, `net461`

This package brings `Net4x.Project2015To2017.Core` with it and bundles the VS2017 (VS15) diagnostic
set, which it uses as the fallback when you do not supply your own `AnalysisOptions`. To migrate to
VS2019 conventions, also add `Net4x.Project2015To2017.Migrate2019.Library`.

## Usage

```csharp
using Project2015To2017;
using Project2015To2017.Analysis;
using Project2015To2017.Migrate2019.Library;
using Project2015To2017.Writing;
using Serilog;
using Serilog.Extensions.Logging;

var serilog = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

// MigrationFacility takes a Microsoft.Extensions.Logging.ILogger;
// any logging provider will do.
var logger = new SerilogLoggerProvider(serilog).CreateLogger(nameof(Serilog));

var facility = new MigrationFacility(logger);

facility.ExecuteMigrate(
    new[] { @"C:\full-path-to-solution-or-project-file.sln" },
    Vs16TransformationSet.Instance,   // the default set of project file transformations
    new ConversionOptions(),          // target frameworks, AssemblyInfo treatment, force flags
    new ProjectWriteOptions(),        // backup creation and custom source control logic
    new AnalysisOptions());           // diagnostics to run after migration
```

Only the first two arguments are required; the option objects fall back to sane defaults.

## API

### `MigrationFacility`

| Member | Purpose |
|--------|---------|
| `ExecuteMigrate(items, transformations, …)` | Parse, transform and **write** the projects. This is the destructive operation; `ProjectWriteOptions` controls backups. |
| `ExecuteEvaluate(items, …)` | Dry run — report what migration would produce, plus diagnostics, without writing anything. |
| `ExecuteAnalyze(items, conversionOptions, analysisOptions)` | Run diagnostics only, no conversion. |
| `ParseProjects(items, transformationSet, conversionOptions)` | Returns the parsed `Project` and `Solution` objects for your own processing. |
| `DoAnalysis(projects, options)` | Run the analyzer over already-parsed projects. |
| `DoProcessableFileSearch(force)` | Populate `Files` by globbing the current working directory for convertible files. |

`items` accepts project files, solution files (`.sln`, `.slnx`), directories, and glob patterns such
as `**\*`.

### Path resolution and `PatternProcessor`

Each item passed in is resolved by a chain of `PatternProcessor` delegates: a file processor, then a
directory processor (which auto-discovers the single convertible file in a directory and warns when
the choice is ambiguous), then anything you supply:

```csharp
var facility = new MigrationFacility(logger, myCustomPatternProcessor);
```

Directory and glob resolution is relative to the **current working directory**, so prefer absolute
paths when calling this from a host process.

### `ProjectConverterExtensions`

`ProjectConverter.Convert(string target, ILogger logger = default)` — a lower-level extension for
converting a single target when you do not need the facility's discovery and reporting.

## Related packages

* `Net4x.Project2015To2017.Core` — project model, readers, writer, transformation and analysis engine
* `Net4x.Project2015To2017.Migrate2017.Library` — VS2017 (VS15) transformation and diagnostic sets
* `Net4x.Project2015To2017.Migrate2019.Library` — VS2019 (VS16) transformation and diagnostic sets
* `Net4x.Project2015To2017.Migrate2019.Tool` — `dotnet migrate-2019` global tool
* `Net4x.Project2015To2017.Mcp.Server` — MCP server exposing migration to AI agents

## License

MIT. Copyright Hans van Bakel. See [LICENSE](https://github.com/hvanbakel/CsprojToVs2017/blob/master/LICENSE).
