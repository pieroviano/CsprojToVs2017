using Project2015To2017;
using Project2015To2017.Caching;

namespace Project2015To2017.Mcp.Server;

/// <summary>
/// Shared translation of MCP tool parameters into <see cref="ConversionOptions"/>, kept in
/// one place so the analyze/evaluate/migrate tools stay consistent with the command-line
/// tool (see Project2015To2017.Migrate2019.Tool/Program.cs).
/// </summary>
internal static class MigrationServer
{
	public static ConversionOptions BuildOptions(
		string[]? targetFrameworks,
		bool force,
		bool keepAssemblyInfo,
		bool appendTfmToOutputPath)
	{
		var options = new ConversionOptions
		{
			ProjectCache = new DefaultProjectCache(),
			ForceOnUnsupportedProjects = force,
			KeepAssemblyInfo = keepAssemblyInfo,
			AppendTargetFrameworkToOutputPath = appendTfmToOutputPath,
		};

		// Used whenever a project's target framework cannot be determined (e.g. an
		// unresolved MSBuild property such as $(MDFrameworkVersion)). We deliberately do NOT
		// set ConversionOptions.UnknownTargetFrameworkCallback: that path prompts a human at
		// a console, which is meaningless in a non-interactive MCP server.
		if (targetFrameworks is { Length: > 0 })
		{
			options.TargetFrameworks = targetFrameworks;
		}

		return options;
	}
}
