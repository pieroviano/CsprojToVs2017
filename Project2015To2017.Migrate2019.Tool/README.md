# Net4x.Project2015To2017.Migrate2019.Tool

.NET global tool that converts legacy (VS2015-era) MSBuild project files to the modern SDK-style
format used by Visual Studio 2019 and later. Part of
[CsprojToVs2017](https://github.com/hvanbakel/CsprojToVs2017).

## Installation

```
dotnet tool install --global Net4x.Project2015To2017.Migrate2019.Tool
```

Requires the .NET 8 runtime. The tool is invoked as `dotnet migrate-2019`.

To migrate to VS2017 conventions instead, install `Net4x.Project2015To2017.Migrate2017.Tool`, which
provides `dotnet migrate-2017` with a few small behavioural differences for older Visual Studio
versions.

## Quick start

```
dotnet migrate-2019 wizard "D:\Path\To\My\TestProject.csproj"
dotnet migrate-2019 wizard "D:\Path\To\My\TestProject.sln"
dotnet migrate-2019 wizard .\MyProjectDirectory
dotnet migrate-2019 wizard **\*
```

The wizard is interactive and walks you through the conversion, offering to create backups before
every critical stage.

Paths are optional. With no arguments the tool searches the current directory; if there is exactly
one convertible project or solution it uses it, otherwise it tells you why it cannot decide safely.

## What it fixes

[VS2017 and later handle several things differently](http://www.natemcmaster.com/blog/2017/03/09/vs2015-to-vs2017-upgrade/).
The tool:

1. Replaces per-file `Compile` entries with wildcard includes
2. Rewrites project references in the succinct modern form
3. Converts `packages.config` to `PackageReference`
4. Folds `AssemblyInfo.cs` attributes into the project file
5. Moves the NuGet package definition into the project file
6. Selects the right project SDK — `Microsoft.NET.Sdk.WindowsDesktop` for WPF/WinForms,
   `MSBuild.Sdk.Extras` for Xamarin and UAP

## Commands

| Command | Description |
|---------|-------------|
| `wizard` | Interactive conversion wizard (recommended — it runs the others in the right order) |
| `migrate` | Non-interactive migration, for scripts and advanced users |
| `evaluate` | Report what migration would do, without changing anything |
| `analyze` | Run diagnostics against the project files, no conversion |

## Options

| Option | Applies to | Description |
|--------|-----------|-------------|
| `-t\|--target-frameworks <frameworks>` | `migrate`, `evaluate` | Override the target frameworks in the output project file |
| `-ft\|--force-transformations <names>` | `migrate` | Force specific transformations to run regardless of project conversion state |
| `-f\|--force` | `migrate`, `evaluate`, `wizard` | Convert project types otherwise considered unsupported (e.g. Entity Framework, ASP.NET) |
| `-a\|--keep-assembly-info` | `migrate`, `evaluate`, `wizard` | Keep `AssemblyInfo.cs` instead of folding attributes into the project file |
| `-o\|--old-output-path` | `migrate` | Do not append the target framework to the output path |
| `-n\|--no-backup` | `migrate` | Skip creating the `Backup` directory (useful when the solution is under source control) |
| `-v\|--verbosity <LEVEL>` | all | `q[uiet]`, `m[inimal]`, `n[ormal]`, `d[etailed]`, `diag[nostic]` |
| `-h\|--help` | all | Show help for the command |

Not every option applies to every command — check `dotnet migrate-2019 <command> --help`.

Repeat an option to pass multiple values:

```
dotnet migrate-2019 migrate -t net40 -t net45
```

## Migrating from your own code

For programmatic control, use `Net4x.Project2015To2017.Migrate2019.Library` and
`Net4x.Project2015To2017.Base` directly rather than shelling out to this tool.

## Related packages

* `Net4x.Project2015To2017.Migrate2017.Tool` — `dotnet migrate-2017`, the VS2017 equivalent
* `Net4x.Project2015To2017.Migrate2019.Library` — the VS2019 transformation and diagnostic sets
* `Net4x.Project2015To2017.Base` — `MigrationFacility`, the high-level orchestration entry point
* `Net4x.Project2015To2017.Core` — project model, readers, writer, transformation and analysis engine
* `Net4x.Project2015To2017.Mcp.Server` — the same operations exposed to AI agents over MCP

## License

MIT. Copyright Hans van Bakel. See [LICENSE](https://github.com/hvanbakel/CsprojToVs2017/blob/master/LICENSE).
