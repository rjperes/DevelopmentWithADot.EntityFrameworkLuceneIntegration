using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevelopmentWithADot.EntityFrameworkLuceneIntegration;

namespace DevelopmentWithADot.EntityFrameworkLuceneIntegrationTests
{
	[Document]
	public class Product
	{
		public Product()
		{
			this.Details = new List<OrderDetail>();
			this.CustomerPreferences = new List<Customer>();
		}

		[Field]
		[Required]
		[StringLength(50)]
		public String Name
		{
			get;
			set;
		}

		[Key]
		[Field(Key = true)]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public Int32 ProductId
		{
			get;
			private set;
		}

		[Field]
		public Decimal Price
		{
			get;
			set;
		}

		public virtual ICollection<OrderDetail> Details
		{
			get;
			set;
		}

		public virtual ICollection<Customer> CustomerPreferences
		{
			get;
			set;
		}

		public override String ToString()
		{
			return (this.Name);
		}
	}
}
