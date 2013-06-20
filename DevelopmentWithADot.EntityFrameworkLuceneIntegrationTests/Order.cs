using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevelopmentWithADot.EntityFrameworkLuceneIntegration;

namespace DevelopmentWithADot.EntityFrameworkLuceneIntegrationTests
{
	[Document]
	public class Order
	{
		[Field]
		public DateTime Date
		{
			get;
			set;
		}

		[Key]
		[Field(Key = true)]
		[Column("ORDER_ID")]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public Int32 OrderId
		{
			get;
			private set;
		}

		public virtual ICollection<OrderDetail> Details
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

		[Field]
		public Double TotalCost
		{
			get
			{
				return ((Double)this.Details.Select(d => d.Product.Price * d.Count).Sum());
			}
		}
	}
}
