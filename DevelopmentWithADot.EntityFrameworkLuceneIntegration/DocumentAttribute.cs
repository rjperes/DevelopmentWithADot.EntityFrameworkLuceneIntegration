namespace DevelopmentWithADot.EntityFrameworkLuceneIntegration
{
	using System;
	using global::Lucene.Net.Analysis.Standard;

	[Serializable]
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public sealed class DocumentAttribute : LuceneAttribute
	{
		public Type AnalyzerType
		{
			get;
			set;
		}

		public DocumentAttribute()
		{
			this.AnalyzerType = typeof(StandardAnalyzer);
		}
	}
}
