using System;

namespace DevelopmentWithADot.EntityFrameworkLuceneIntegration
{
	[Serializable]
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class FieldAttribute : LuceneAttribute
	{
		public FieldAttribute()
		{
			this.Index = Index.Analyzed;
			this.Store = Store.Yes;
			this.Key = false;
			this.Boost = 1;
			this.OmitNorms = false;
			this.TermVector = TermVector.No;
			this.OmitTermFreqAndPositions = false;
		}

		public Boolean OmitTermFreqAndPositions
		{
			get;
			set;
		}

		public TermVector TermVector
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
	}
}
