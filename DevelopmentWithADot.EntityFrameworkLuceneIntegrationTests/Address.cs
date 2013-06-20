using System;
using System.ComponentModel.DataAnnotations;

namespace DevelopmentWithADot.EntityFrameworkLuceneIntegrationTests
{
	[Serializable]
	public class Address
	{
		[Key]
		public virtual Int32 CustomerId
		{
			get;
			set;
		}

		[Required]
		public virtual Customer Customer
		{
			get;
			set;
		}

		[Required]
		[StringLength(50)]
		public virtual String City
		{
			get;
			set;
		}

		[Required]
		[StringLength(50)]
		public virtual String Street
		{
			get;
			set;
		}

		[Required]
		[StringLength(10)]
		public virtual String ZipCode
		{
			get;
			set;
		}

		[Required]
		[StringLength(50)]
		public virtual String Country
		{
			get;
			set;
		}
	}
}
