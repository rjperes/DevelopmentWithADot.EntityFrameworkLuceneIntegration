using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevelopmentWithADot.EntityFrameworkLuceneIntegrationTests
{
	public class OrderDetail
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public Int32 OrderDetailId
		{
			get;
			protected set;
		}

		public Int32 Count
		{
			get;
			set;
		}

		[Required]
		public virtual Order Order
		{
			get;
			set;
		}

		[Required]
		public virtual Product Product
		{
			get;
			set;
		}
	}
}
