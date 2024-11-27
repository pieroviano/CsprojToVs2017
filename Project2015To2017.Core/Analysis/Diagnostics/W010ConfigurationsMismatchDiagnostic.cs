using Project2015To2017.Definition;
using Project2015To2017.Reading;
using System;
using System.Collections.Generic;

namespace Project2015To2017.Analysis.Diagnostics
{
	public sealed class W010ConfigurationsMismatchDiagnostic : DiagnosticBase
	{
		/// <inheritdoc />
		public override IReadOnlyList<IDiagnosticResult> Analyze(Project project)
		{
			var configurationSet = new HashSet<string>();
			var platformSet = new HashSet<string>();

			var configurationsFromProperty = ParseFromProperty("Configurations") ?? new[] { "Debug", "Release" };
			var platformsFromProperty = ParseFromProperty("Platforms") ?? new[] { "AnyCPU" };

			foreach (var configuration in configurationsFromProperty)
			{
				configurationSet.Add(configuration);
			}

			foreach (var platform in platformsFromProperty)
			{
				platformSet.Add(platform);
			}

			var list = new List<IDiagnosticResult>();
			foreach (var x in project.ProjectDocument.Descendants())
			{
				var condition = x.Attribute("Condition");
				if (condition == null)
				{
					continue;
				}

				var conditionValue = condition.Value;
				if (!conditionValue.Contains("$(Configuration)") && !conditionValue.Contains("$(Platform)"))
				{
					continue;
				}

				var conditionEvaluated = ConditionEvaluator.GetConditionValues(conditionValue);

				if (conditionEvaluated.TryGetValue("Configuration", out var configurations))
				{
					foreach (var configuration in configurations)
					{
						if (!configurationSet.Contains(configuration))
						{
							list.Add(
								CreateDiagnosticResult(project,
										$"Configuration '{configuration}' is used in project file but not mentioned in $(Configurations).",
										project.FilePath)
									.LoadLocationFromElement(x));
						}
					}
				}

				if (conditionEvaluated.TryGetValue("Platform", out var platforms))
				{
					foreach (var platform in platforms)
					{
						if (!platformSet.Contains(platform))
						{
							list.Add(
								CreateDiagnosticResult(project,
										$"Platform '{platform}' is used in project file but not mentioned in $(Platforms).",
										project.FilePath)
									.LoadLocationFromElement(x));
						}
					}
				}
			}

			return list;

			string[] ParseFromProperty(string name) => project.Property(name)
				?.Value
				.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
		}

		public W010ConfigurationsMismatchDiagnostic() : base(10)
		{
		}
	}
}