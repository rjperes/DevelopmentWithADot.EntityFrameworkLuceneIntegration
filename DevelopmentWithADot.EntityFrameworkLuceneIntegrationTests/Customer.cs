using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevelopmentWithADot.EntityFrameworkLuceneIntegrationTests
{
	public class Customer
	{
		public Customer()
		{
			this.PreferredProducts = new List<Product>();
			this.Orders = new List<Order>();
		}

		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public Int32 CustomerId
		{
			get;
			private set;
		}

		public virtual Address Address
		{
			get;
			set;
		}

		[Required]
		[StringLength(50)]
		public String Name
		{
			get;
			set;
		}

		[Required]
		[StringLength(50)]
		public String Email
		{
			get;
			set;
		}

		public void Test()
		{

		}

		public virtual ICollection<Product> PreferredProducts
		{
			get;
			set;
		}

		public virtual ICollection<Order> Orders
		{
			get;
			set;
		}
	}
}
