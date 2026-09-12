# Net4x.Project2015To2017.Mcp.Server

Model Context Protocol (MCP) server that exposes the
[CsprojToVs2017](https://github.com/hvanbakel/CsprojToVs2017) migration engine to AI agents, so an
assistant can analyze, evaluate and migrate legacy MSBuild projects to the modern SDK-style format.

It is the same engine as the `dotnet migrate-2019` global tool — `MigrationFacility` driven by
`Vs16TransformationSet` and `Vs16DiagnosticSet` — reachable over JSON-RPC instead of a command line.

## Installation

```
dotnet add package Net4x.Project2015To2017.Mcp.Server
```

**Target framework:** `net8.0`. The executable is named `dotnet-migrate-2019-mcp`.

## Registering with an MCP client

The server speaks **stdio**. Register it in your client's MCP configuration:

```json
{
  "mcpServers": {
    "migrate-2019": {
      "command": "dotnet-migrate-2019-mcp"
    }
  }
}
```

Or run it straight from a checkout:

```json
{
  "mcpServers": {
    "migrate-2019": {
      "command": "dotnet",
      "args": ["run", "--project", "path/to/Project2015To2017.Mcp.Server"]
    }
  }
}
```

To inspect the surface manually:

```
npx @modelcontextprotocol/inspector dotnet run --project Project2015To2017.Mcp.Server
```

## Tools

### `analyze`

Report diagnostics for legacy projects. Reads only; writes nothing.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `paths` | `string[]` | *required* | Absolute paths to `.csproj` / `.vbproj` / `.fsproj` / `.sln` / `.slnx` files, or a directory |
| `targetFrameworks` | `string[]` | `null` | Frameworks to assume when a project's own framework cannot be determined |
| `force` | `bool` | `false` | Process project types otherwise considered unsupported |

### `evaluate`

Dry run. Reports what migration would do, plus diagnostics, without writing any files. Same
parameters as `analyze`.

### `migrate`

**Writes to disk.** Migrates the projects to SDK-style.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `paths` | `string[]` | *required* | Absolute paths to project/solution files or a directory |
| `targetFrameworks` | `string[]` | `null` | Target frameworks to write, e.g. `["net48","netstandard2.1"]` |
| `noBackup` | `bool` | `false` | Skip `.backup` copies of modified files — backups are **on** by default |
| `keepAssemblyInfo` | `bool` | `false` | Keep `AssemblyInfo.cs` instead of folding attributes into the project file |
| `oldOutputPath` | `bool` | `false` | Do not append the target framework to the output path |
| `force` | `bool` | `false` | Process project types otherwise considered unsupported |

The interactive `wizard` flow of the CLI is deliberately **not** exposed — it requires a human at a
console.

## Behaviour worth knowing

* **`migrate` is destructive.** It rewrites project files in place. Backups are on unless you pass
  `noBackup`. Leave your client's per-tool approval prompt enabled.
* **Use absolute paths.** Directory and glob resolution happens relative to the server process's
  current working directory, which is not necessarily the agent's.
* **Results are the migration log.** Each call returns the captured `[Information]` / `[Warning]` /
  `[Error]` output as text, matching what the CLI prints — not structured JSON.
* **Calls are serialized.** A semaphore ensures one operation at a time so captured log output cannot
  interleave between concurrent calls.
* **stdout is protocol-only.** All default logging providers are cleared at startup and migration
  output is captured in memory, because stdout carries JSON-RPC frames.

## Related packages

* `Net4x.Project2015To2017.Migrate2019.Tool` — `dotnet migrate-2019`, the same operations as a CLI
* `Net4x.Project2015To2017.Migrate2019.Library` — the VS2019 transformation and diagnostic sets
* `Net4x.Project2015To2017.Base` — `MigrationFacility`, the high-level orchestration entry point
* `Net4x.Project2015To2017.Core` — project model, readers, writer, transformation and analysis engine

## License

MIT. Copyright Hans van Bakel. See [LICENSE](https://github.com/hvanbakel/CsprojToVs2017/blob/master/LICENSE).
