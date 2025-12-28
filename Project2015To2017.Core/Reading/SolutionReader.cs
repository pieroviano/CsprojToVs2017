using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Project2015To2017.Definition;

namespace Project2015To2017.Reading;

public sealed class SolutionReader
{
	private const string vbProjectGuid = "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}";
	private const string csProjectGuid = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
	private const string cpsProjectGuid = "{13B669BE-BB05-4DDF-9536-439F39A36129}";
	private const string cpsCsProjectGuid = "{9A19103F-16F7-4668-BE54-9A1E7A4F7556}";
	private const string cpsVbProjectGuid = "{778DAE3C-4631-46EA-AA77-85C1314464D9}";
	private const string fsProjectGuid = "{F2A71F9B-5D33-465A-A702-920D77279786}";
	private const string solutionFolderGuid = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";
	public static readonly SolutionReader Instance = new();

	private static readonly Lazy<Regex> crackProjectLine = new(() =>
		new Regex(
			"^Project\\(\"(?<PROJECTTYPEGUID>.*)\"\\)\\s*=\\s*\"(?<PROJECTNAME>.*)\"\\s*,\\s*\"(?<RELATIVEPATH>.*)\"\\s*,\\s*\"(?<PROJECTGUID>.*)\"$",
			RegexOptions.Compiled));

	private SolutionReader()
	{
	}

	public Solution Read(string filePath, ILogger logger = null)
	{
		if (string.IsNullOrWhiteSpace(filePath))
		{
			throw new ArgumentException(nameof(filePath));
		}

		return Read(new FileInfo(filePath), logger ?? NoopLogger.Instance);
	}

	public Solution Read(FileInfo fileInfo, ILogger logger)
	{
		if (fileInfo == null)
		{
			throw new ArgumentNullException(nameof(fileInfo));
		}

		logger ??= NoopLogger.Instance;

		var projects = new List<ProjectReference>();
		var unsupported = new List<string>();

		if (fileInfo.Extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase))
		{
			ReadSlnx(fileInfo, logger, projects, unsupported);
		}
		else
		{
			ReadClassicSln(fileInfo, logger, projects, unsupported);
		}

		return new Solution
		{
			FilePath = fileInfo,
			ProjectPaths = projects,
			UnsupportedProjectPaths = unsupported
		};
	}

	private static bool IsSolutionFolder(string guid)
	{
		return string.Equals(guid, solutionFolderGuid, StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsSupportedProjectType(string guid)
	{
		return string.Equals(guid, vbProjectGuid, StringComparison.OrdinalIgnoreCase) ||
		       string.Equals(guid, csProjectGuid, StringComparison.OrdinalIgnoreCase) ||
		       string.Equals(guid, cpsProjectGuid, StringComparison.OrdinalIgnoreCase) ||
		       string.Equals(guid, cpsCsProjectGuid, StringComparison.OrdinalIgnoreCase) ||
		       string.Equals(guid, cpsVbProjectGuid, StringComparison.OrdinalIgnoreCase) ||
		       string.Equals(guid, fsProjectGuid, StringComparison.OrdinalIgnoreCase);
	}

	private bool ParseFirstProjectLine(
		string firstLine,
		out string projectTypeGuid,
		out string projectName,
		out string path,
		out string projectGuid)
	{
		var match = crackProjectLine.Value.Match(firstLine);
		if (!match.Success)
		{
			throw new ArgumentException("Invalid Project line", nameof(firstLine));
		}

		projectTypeGuid = match.Groups["PROJECTTYPEGUID"].Value.Trim();
		projectName = match.Groups["PROJECTNAME"].Value.Trim();
		path = match.Groups["RELATIVEPATH"].Value.Trim();
		projectGuid = match.Groups["PROJECTGUID"].Value.Trim();

		if (string.IsNullOrWhiteSpace(projectName))
		{
			projectName = $"EmptyProjectName.{Guid.NewGuid()}";
		}

		return IsSupportedProjectType(projectTypeGuid);
	}

	#region Classic .sln support

	private void ReadClassicSln(
		FileInfo solutionFile,
		ILogger logger,
		ICollection<ProjectReference> projects,
		ICollection<string> unsupported)
	{
		using var reader = new StreamReader(solutionFile.OpenRead());

		string line;
		while ((line = reader.ReadLine()) != null)
		{
			line = line.Trim();
			if (!line.StartsWith("Project(", StringComparison.Ordinal))
			{
				continue;
			}

			if (!ParseFirstProjectLine(
				    line,
				    out var typeGuid,
				    out var name,
				    out var path,
				    out var projectGuid))
			{
				if (!IsSolutionFolder(typeGuid))
				{
					logger.LogWarning(
						"Unsupported project[{Name}] type {Type}",
						name, typeGuid);
					unsupported.Add(path);
				}

				continue;
			}

			var adjustedPath = Extensions.MaybeAdjustFilePath(
				path, solutionFile.DirectoryName);

			projects.Add(new ProjectReference
			{
				Include = path,
				ProjectName = name,
				ProjectTypeGuid = typeGuid,
				ProjectGuid = Guid.Parse(projectGuid),
				ProjectFile = new FileInfo(
					Path.Combine(solutionFile.DirectoryName!, adjustedPath))
			});
		}
	}

	#endregion

	#region .slnx (VS 2026+) support

	private void ReadSlnx(
		FileInfo solutionFile,
		ILogger logger,
		ICollection<ProjectReference> projects,
		ICollection<string> unsupported)
	{
		XDocument doc;
		try
		{
			doc = XDocument.Load(solutionFile.FullName);
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException(
				$"Failed to parse '{solutionFile.FullName}' as .slnx XML.", ex);
		}

		var projectElements = doc
			.Descendants()
			.Where(e => e.Name.LocalName.Equals("Project", StringComparison.OrdinalIgnoreCase));

		foreach (var element in projectElements)
		{
			var path = GetAttributeValue(element,
				"Path", "Include", "File", "RelativePath");

			if (string.IsNullOrWhiteSpace(path))
			{
				continue;
			}

			var projectTypeGuid = GetAttributeValue(element,
				"TypeGuid", "ProjectTypeGuid", "Type");

			var name = GetAttributeValue(element,
				           "Name", "ProjectName", "Title")
			           ?? Path.GetFileNameWithoutExtension(path);

			var guidText = GetAttributeValue(element,
				"ProjectGuid", "Guid", "Id", "ProjectId");

			if (!IsSupportedProjectType(projectTypeGuid))
			{
				if (!IsSolutionFolder(projectTypeGuid))
				{
					logger.LogWarning(
						"Unsupported project[{Name}] type {Type}",
						name, projectTypeGuid);
					unsupported.Add(path);
				}

				continue;
			}

			var adjustedPath = Extensions.MaybeAdjustFilePath(
				path, solutionFile.DirectoryName);

			projects.Add(new ProjectReference
			{
				Include = path,
				ProjectName = string.IsNullOrWhiteSpace(name)
					? $"EmptyProjectName.{Guid.NewGuid()}"
					: name,
				ProjectTypeGuid = projectTypeGuid,
				ProjectGuid = Guid.TryParse(guidText, out var g) ? g : Guid.Empty,
				ProjectFile = new FileInfo(
					Path.Combine(solutionFile.DirectoryName!, adjustedPath))
			});
		}
	}

	private static string GetAttributeValue(XElement element, params string[] names)
	{
		foreach (var name in names)
		{
			var attr = element.Attribute(name);
			if (attr != null)
			{
				return attr.Value.Trim();
			}
		}

		return null;
	}

	#endregion
}