# Net4x.Project2015To2017.Migrate2019.Library

VS2019 (VS16) transformation and diagnostic sets for
[CsprojToVs2017](https://github.com/hvanbakel/CsprojToVs2017) — the tool that converts legacy
(VS2015-era) MSBuild project files to the modern SDK-style format.

This is the **recommended** library package. It layers VS2019-era behaviour on top of
`Net4x.Project2015To2017.Migrate2017.Library`, most notably proper SDK selection for desktop and
mobile projects.

## Installation

```
dotnet add package Net4x.Project2015To2017.Migrate2019.Library
```

**Target frameworks:** `netstandard2.0`, `net461`

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

var logger = new SerilogLoggerProvider(serilog).CreateLogger(nameof(Serilog));

var facility = new MigrationFacility(logger);

facility.ExecuteMigrate(
    new[] { @"C:\full-path-to-solution-or-project-file.sln" },
    Vs16TransformationSet.Instance,   // the default set of project file transformations

    // The rest are optional and fall back to sane defaults
    new ConversionOptions(),          // target frameworks, AssemblyInfo treatment
    new ProjectWriteOptions(),        // backup creation and custom source control logic
    new AnalysisOptions(Vs16DiagnosticSet.All));
```

## Transformation sets

| Set | Contents |
|-----|----------|
| `Vs16TransformationSet.Instance` | The full migration pipeline: reading, generic simplification for VS 16.0, then the VS16 transformations. Pass this to `ExecuteMigrate`. |
| `Vs16TransformationSet.TrueInstance` | Only the VS16-specific transformations, for composing your own chain. |
| `Vs16ModernizationTransformationSet.Instance` | Cleanups for projects that are already SDK-style; currently the VS15 modernization transformations, chained. |

`Vs16TransformationSet.TargetVisualStudioVersion` is `16.0`.

### What VS16 changes over VS15

`Vs16FrameworkReferencesTransformation` replaces the VS15 `FrameworkReferencesTransformation`. It
picks the right project SDK instead of stuffing everything into `Microsoft.NET.Sdk`:

* WPF and Windows Forms projects get `Microsoft.NET.Sdk.WindowsDesktop` with `UseWPF` / `UseWindowsForms`
* Xamarin.Android, Xamarin.iOS and UAP projects (detected by project type GUID) get
  `MSBuild.Sdk.Extras` and the corresponding target framework

Everything else — the assembly reference filters, `FileTransformation`, `XamlPagesTransformation`,
`TestProjectPackageReferenceTransformation`, `BrokenHookTargetsTransformation` — is inherited from the
VS15 set.

## Diagnostics

`Vs16DiagnosticSet.All` is currently the whole of `Vs15DiagnosticSet.All`: the core diagnostics
(`W001`, `W002`, `W010`, `W011`) plus `W020`–`W021` and `W030`–`W034`. See the
`Net4x.Project2015To2017.Migrate2017.Library` readme for the table.

## Custom transformations

```csharp
var customTransforms = new BasicTransformationSet(
    // Implement ITransformationWithTargetMoment to run before or after
    // the majority of standard transforms; implement ITransformationWithDependencies
    // to always run after a specific other transformation.
    new MyCustomPreTransform(),
    new MyCustomPostTransform());

// The correct order is resolved from the dependency graph within each
// execution moment (early, normal, late).
var resultTransforms = new ChainTransformationSet(
    Vs16TransformationSet.Instance,
    customTransforms);

facility.ExecuteMigrate(new[] { @"C:\path\to\Solution.sln" }, resultTransforms);
```

## Related packages

* `Net4x.Project2015To2017.Core` — project model, readers, writer, transformation and analysis engine
* `Net4x.Project2015To2017.Base` — `MigrationFacility`, the high-level orchestration entry point
* `Net4x.Project2015To2017.Migrate2017.Library` — VS2017 (VS15) sets, which this package builds on
* `Net4x.Project2015To2017.Migrate2019.Tool` — `dotnet migrate-2019` global tool
* `Net4x.Project2015To2017.Mcp.Server` — MCP server exposing migration to AI agents

## License

MIT. Copyright Hans van Bakel. See [LICENSE](https://github.com/hvanbakel/CsprojToVs2017/blob/master/LICENSE).
