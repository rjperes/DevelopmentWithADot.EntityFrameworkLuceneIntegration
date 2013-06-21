using System;

namespace DevelopmentWithADot.EntityFrameworkLuceneIntegration
{
	[Serializable]
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public sealed class NumericFieldAttribute : FieldAttribute
	{
		public NumericFieldAttribute()
		{
			this.PrecisionStep = Lucene.Net.Util.NumericUtils.PRECISION_STEP_DEFAULT;
			this.Index = true;
		}

		public Int32 PrecisionStep
		{
			get;
			set;
		}

		public new Boolean Index
		{
			get;
			set;
		}
	}
}
