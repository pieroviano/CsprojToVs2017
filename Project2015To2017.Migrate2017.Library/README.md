# Net4x.Project2015To2017.Migrate2017.Library

VS2017 (VS15) transformation and diagnostic sets for
[CsprojToVs2017](https://github.com/hvanbakel/CsprojToVs2017) — the tool that converts legacy
(VS2015-era) MSBuild project files to the modern SDK-style format.

`Net4x.Project2015To2017.Core` provides the engine; this package provides the *policy* — the concrete
set of transformations and diagnostics that produce a project file a Visual Studio 2017 toolchain is
happy with.

Use this package when you must stay on VS2017 conventions. For current work prefer
`Net4x.Project2015To2017.Migrate2019.Library`, which builds on this one.

## Installation

```
dotnet add package Net4x.Project2015To2017.Migrate2017.Library
```

**Target frameworks:** `netstandard2.0`, `net461`

## Usage

```csharp
using Project2015To2017;
using Project2015To2017.Analysis;
using Project2015To2017.Migrate2017;

var facility = new MigrationFacility(logger);

facility.ExecuteMigrate(
    new[] { @"C:\path\to\Solution.sln" },
    Vs15TransformationSet.Instance,
    analysisOptions: new AnalysisOptions(Vs15DiagnosticSet.All));
```

## Transformation sets

| Set | Contents |
|-----|----------|
| `Vs15TransformationSet.Instance` | The full migration pipeline: reading, generic simplification for VS 15.0, then the VS15 transformations below. This is what you normally pass to `ExecuteMigrate`. |
| `Vs15TransformationSet.TrueInstance` | Only the VS15-specific transformations, for composing your own chain. |
| `Vs15ModernizationTransformationSet.Instance` | Opt-in cleanups applied to projects that are *already* SDK-style: `UpgradeDebugTypeTransformation`, `UpgradeUseDefaultOutputPathTransformation`, `UpgradeUseComVisibleDefaultTransformation`, `UpgradeTestServiceTransformation`, `UpgradeFrameworkAssembliesToNuGetTransformation`. |

`Vs15TransformationSet` applies `TargetFrameworkReplaceTransformation`,
`FrameworkReferencesTransformation`, `TestProjectPackageReferenceTransformation`, the assembly
reference filters (`AssemblyFilterPackageReferencesTransformation`,
`AssemblyFilterHintedPackageReferencesTransformation`, `AssemblyFilterDefaultTransformation`),
`ImportsTargetsFilterPackageReferencesTransformation`, `FileTransformation`,
`XamlPagesTransformation` and `BrokenHookTargetsTransformation`.

`Vs15TransformationSet.TargetVisualStudioVersion` is `15.0` and drives the version-dependent
behaviour of `BasicSimplifyTransformationSet`.

## Diagnostics

`Vs15DiagnosticSet.All` is the core diagnostics plus:

| Code | Diagnostic |
|------|-----------|
| `W020` | `Microsoft.CSharp` reference is redundant on modern projects |
| `W021` | `System.*` NuGet packages that a modern project gets from the framework |
| `W030` | Legacy `DebugType` values |
| `W031` | MSBuild SDK version specification issues |
| `W032` | Outdated `LangVersion` |
| `W033` | Obsolete Portable Class Library targets |
| `W034` | Reference aliases that need attention |

`W020`/`W021` are grouped as `Vs15DiagnosticSet.ModernIssues`; `W030`–`W034` as
`Vs15DiagnosticSet.ModernizationTips`.

## Composing with your own transformations

```csharp
var set = new ChainTransformationSet(
    Vs15TransformationSet.Instance,
    new BasicTransformationSet(new MyPreTransform(), new MyPostTransform()));
```

Ordering is resolved by the engine from each transformation's execution moment and declared
dependencies — see the `Net4x.Project2015To2017.Core` readme.

## Related packages

* `Net4x.Project2015To2017.Core` — project model, readers, writer, transformation and analysis engine
* `Net4x.Project2015To2017.Base` — `MigrationFacility`, the high-level orchestration entry point
* `Net4x.Project2015To2017.Migrate2019.Library` — VS2019 (VS16) sets, built on top of these
* `Net4x.Project2015To2017.Migrate2017.Tool` — `dotnet migrate-2017` global tool

## License

MIT. Copyright Hans van Bakel. See [LICENSE](https://github.com/hvanbakel/CsprojToVs2017/blob/master/LICENSE).
