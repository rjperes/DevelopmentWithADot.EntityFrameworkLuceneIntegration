using System;

namespace DevelopmentWithADot.EntityFrameworkLuceneIntegration
{
	[Serializable]
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public sealed class NumericFieldAttribute : LuceneAttribute
	{
		public NumericFieldAttribute()
		{
			this.Index = true;
			this.Store = Store.Yes;
			this.Key = false;
			this.Boost = 0;
			this.OmitNorms = false;
		}

		public Int32 PrecisionStep
		{
			get;
			set;
		}

		public Single Boost
		{
			get;
			set;
		}

		public Boolean Key
		{
			get;
			set;
		}

		public Boolean Index
		{
			get;
			set;
		}

		public Store Store
		{
			get;
			set;
		}

		public Boolean OmitNorms
		{
			get;
			set;
		}

		public Type TokenizerType
		{
			get;
			set;
		}
	}
}
