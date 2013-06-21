﻿using System;

namespace DevelopmentWithADot.EntityFrameworkLuceneIntegration
{
	[Serializable]
	public enum Index
	{
		No = 0,
		Analyzed = 1,
		NotAnalyzed = 2,
		NotAnalyzedNoNorms = 3,
		AnalyzedNoNorms = 4,
	}
}
