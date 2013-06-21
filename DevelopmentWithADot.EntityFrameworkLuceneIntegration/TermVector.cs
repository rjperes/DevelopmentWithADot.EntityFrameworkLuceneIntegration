using System;

namespace DevelopmentWithADot.EntityFrameworkLuceneIntegration
{
	[Serializable]
	public enum TermVector
	{
		No = 0,
		Yes = 1,
		WithPositions = 2,
		WithOffsets = 3,
		WithPositionsOffsets = 4,
	}
}
