# Net4x.Project2015To2017.Core

Core object model and engine behind [CsprojToVs2017](https://github.com/hvanbakel/CsprojToVs2017) —
the tool that converts legacy (VS2015-era) MSBuild project files to the modern SDK-style format.

This package contains no migration policy of its own. It provides the project model, the readers,
the writer, the transformation pipeline and the analysis infrastructure. The actual sets of
transformations for a given Visual Studio generation live in
`Net4x.Project2015To2017.Migrate2017.Library` and `Net4x.Project2015To2017.Migrate2019.Library`.

## Installation

```
dotnet add package Net4x.Project2015To2017.Core
```

**Target frameworks:** `netstandard2.0`, `net461`, `net8.0`

## What's in it

| Area | Types |
|------|-------|
| Project model | `Project`, `Solution`, `AssemblyAttributes`, `PackageConfiguration`, `AssemblyReference`, `PackageReference`, `ProjectReference`, `ApplicationType` |
| Reading | `ProjectReader`, `SolutionReader`, `ProjectPropertiesReader`, `AssemblyInfoReader`, `NuSpecReader`, plus a conditional-expression evaluator for MSBuild `Condition` attributes |
| Writing | `ProjectWriter`, `ProjectWriteOptions` (backup creation, custom source-control hooks) |
| Transformation pipeline | `ITransformation`, `ITransformationSet`, `ITransformationWithDependencies`, `ITransformationWithTargetMoment`, `BasicTransformationSet`, `ChainTransformationSet`, `NoopTransformationSet` |
| Built-in transforms | `AssemblyAttributeTransformation`, `NuGetPackageTransformation`, `PropertySimplificationTransformation`, `PropertyDeduplicationTransformation`, `TargetFrameworkReplaceTransformation`, `ServiceFilterTransformation`, `EmptyGroupRemoveTransformation`, and the `BasicReadTransformationSet` / `BasicSimplifyTransformationSet` bundles |
| Analysis | `Analyzer<,>`, `AnalysisOptions`, `DiagnosticSet`, `DiagnosticBase`, `IReporter<>`, `LoggerReporter`, and the baseline diagnostics `W001` (illegal project type), `W002` (missing project file), `W010` (configurations mismatch), `W011` (unsupported conditional) |
| Conversion control | `ProjectConverter`, `ConversionOptions`, `UnsupportedProjectTypes`, `UnsupportedProjectReason` |
| Caching | `IProjectCache`, `DefaultProjectCache`, `NoProjectCache` |

## Transformation execution order

Transformations do not run in declaration order. Each one may declare:

* an **execution moment** (`TargetTransformationExecutionMoment`: `Early`, `Normal`, `Late`) via
  `ITransformationWithTargetMoment`, and
* **dependencies** on other transformations via `ITransformationWithDependencies`.

The engine performs a topological sort within each moment, so custom transformations can be mixed
freely with the standard ones and still land in the right place.

## Writing a custom transformation

```csharp
using System.Linq;
using Project2015To2017;
using Project2015To2017.Definition;
using Project2015To2017.Transforms;

public sealed class RemoveObsoletePropertyTransformation : ITransformation
{
    public void Transform(Project definition)
    {
        foreach (var property in definition.PropertyGroups.ElementsAnyNamespace("MyObsoleteProperty").ToList())
        {
            property.Remove();
        }
    }
}
```

Bundle it into a set and chain it with a standard set:

```csharp
var set = new ChainTransformationSet(
    Vs16TransformationSet.Instance,                       // from the Migrate2019 library package
    new BasicTransformationSet(new RemoveObsoletePropertyTransformation()));
```

## Logging

Every entry point takes a `Microsoft.Extensions.Logging.ILogger`. Pass `NoopLogger.Instance` if you
want the pipeline to stay silent.

## Dependencies

`Microsoft.CodeAnalysis.CSharp`, `NuGet.Configuration`, `Microsoft.Extensions.Logging.Abstractions`,
`System.Memory`.

## Related packages

* `Net4x.Project2015To2017.Base` — `MigrationFacility`, the high-level orchestration entry point
* `Net4x.Project2015To2017.Migrate2017.Library` — VS2017 (VS15) transformation and diagnostic sets
* `Net4x.Project2015To2017.Migrate2019.Library` — VS2019 (VS16) transformation and diagnostic sets
* `Net4x.Project2015To2017.Migrate2019.Tool` — `dotnet migrate-2019` global tool
* `Net4x.Project2015To2017.Mcp.Server` — MCP server exposing migration to AI agents

## License

MIT. Copyright Hans van Bakel. See [LICENSE](https://github.com/hvanbakel/CsprojToVs2017/blob/master/LICENSE).
