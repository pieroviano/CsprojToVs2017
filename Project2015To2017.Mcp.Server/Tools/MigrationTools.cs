using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Project2015To2017;
using Project2015To2017.Analysis;
using Project2015To2017.Migrate2019.Library;
using Project2015To2017.Mcp.Server.Logging;
using Project2015To2017.Writing;

namespace Project2015To2017.Mcp.Server.Tools;

/// <summary>
/// MCP tools that expose the non-interactive <c>dotnet-migrate-2019</c> operations.
/// Each tool mirrors the corresponding command in
/// Project2015To2017.Migrate2019.Tool/Program.cs, but drives <see cref="MigrationFacility"/>
/// directly so all log output can be captured and returned in the tool result.
/// </summary>
[McpServerToolType]
public sealed class MigrationTools
{
	// Operations are serialized: the log-capture scope is a single shared slot, and the CLI
	// itself processes one migration at a time.
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
	public Task<string> Analyze(
		[Description("Absolute paths to .csproj/.vbproj/.fsproj/.sln/.slnx files or a directory.")]
		string[] paths,
		[Description("Optional target frameworks (e.g. [\"net48\"]) used when a project's " +
		             "framework cannot be determined.")]
		string[]? targetFrameworks = null,
		[Description("Force processing of otherwise-unsupported project types.")]
		bool force = false)
		=> Run(paths, () =>
		{
			var facility = new MigrationFacility(facilityLogger);
			var options = MigrationServer.BuildOptions(targetFrameworks, force,
				keepAssemblyInfo: false, appendTfmToOutputPath: true);
			facility.ExecuteAnalyze(paths, options, new AnalysisOptions(Vs16DiagnosticSet.All));
		});

	[McpServerTool(Name = "evaluate")]
	[Description("Dry-run evaluation: reports what migration would do plus diagnostics, " +
	             "without writing any files.")]
	public Task<string> Evaluate(
		[Description("Absolute paths to project/solution files or a directory.")]
		string[] paths,
		[Description("Optional target frameworks used when a project's framework cannot be determined.")]
		string[]? targetFrameworks = null,
		[Description("Force processing of otherwise-unsupported project types.")]
		bool force = false)
		=> Run(paths, () =>
		{
			var facility = new MigrationFacility(facilityLogger);
			var options = MigrationServer.BuildOptions(targetFrameworks, force,
				keepAssemblyInfo: false, appendTfmToOutputPath: true);
			facility.ExecuteEvaluate(paths, options, Vs16TransformationSet.Instance,
				new AnalysisOptions(Vs16DiagnosticSet.All));
		});

	[McpServerTool(Name = "migrate")]
	[Description("Migrate legacy projects to SDK-style. WRITES to disk. Creates .backup " +
	             "copies of modified files unless noBackup is true.")]
	public Task<string> Migrate(
		[Description("Absolute paths to project/solution files or a directory.")]
		string[] paths,
		[Description("Target frameworks to write (e.g. [\"net48\",\"netstandard2.1\"]). Also " +
		             "used when a framework cannot be determined.")]
		string[]? targetFrameworks = null,
		[Description("Skip creating .backup copies of modified files. Default false (backups ON).")]
		bool noBackup = false,
		[Description("Keep AssemblyInfo.cs instead of folding attributes into the project file.")]
		bool keepAssemblyInfo = false,
		[Description("Write output to the legacy (non-TFM-suffixed) output path.")]
		bool oldOutputPath = false,
		[Description("Force processing of otherwise-unsupported project types.")]
		bool force = false)
		=> Run(paths, () =>
		{
			var facility = new MigrationFacility(facilityLogger);
			var options = MigrationServer.BuildOptions(targetFrameworks, force,
				keepAssemblyInfo, appendTfmToOutputPath: !oldOutputPath);
			var writeOptions = new ProjectWriteOptions { MakeBackups = !noBackup };
			facility.ExecuteMigrate(paths, Vs16TransformationSet.Instance, options, writeOptions,
				new AnalysisOptions(Vs16DiagnosticSet.All));
		});

	/// <summary>
	/// Serializes execution, runs the operation inside a fresh log-capture scope, and returns
	/// the captured output as the tool result.
	/// </summary>
	private async Task<string> Run(string[] paths, Action operation)
	{
		if (paths is not { Length: > 0 })
		{
			return "[Error] No paths supplied.";
		}

		await Gate.WaitAsync().ConfigureAwait(false);
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
