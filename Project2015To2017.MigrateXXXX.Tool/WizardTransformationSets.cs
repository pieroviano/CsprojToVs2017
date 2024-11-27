using System.Collections.Generic;
using Project2015To2017.Analysis;

namespace Project2015To2017.Migrate2017.Tool
{
	public sealed class WizardTransformationSets
	{
		public ITransformationSet MigrateSet { get; set; }
		public ITransformationSet ModernCleanUpSet { get; set; }
		public ITransformationSet ModernizeSet { get; set; }
		public HashSet<IDiagnostic> Diagnostics { get; set; }
	}
}
