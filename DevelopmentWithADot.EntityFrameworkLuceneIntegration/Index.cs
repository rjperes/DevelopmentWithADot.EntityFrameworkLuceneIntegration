using System;

namespace DevelopmentWithADot.EntityFrameworkLuceneIntegration
{
	[Serializable]
	public enum Index
	{
		Analyzed,
		AnalyzedNoNorms,
		No,
		NotAnalyzed,
		NotAnalyzedNoNorms,
	}
}
