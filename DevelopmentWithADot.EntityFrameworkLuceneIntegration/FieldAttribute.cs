using System;

namespace DevelopmentWithADot.EntityFrameworkLuceneIntegration
{
	[Serializable]
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public sealed class FieldAttribute : LuceneAttribute
	{
		public FieldAttribute()
		{
			this.Index = Index.Analyzed;
			this.Store = Store.Yes;
			this.Key = false;
			this.Boost = 0;
			this.OmitNorms = false;
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

		public Index Index
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
