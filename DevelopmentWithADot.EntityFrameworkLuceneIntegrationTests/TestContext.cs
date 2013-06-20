using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using DevelopmentWithADot.EntityFrameworkLuceneIntegration;

namespace DevelopmentWithADot.EntityFrameworkLuceneIntegrationTests
{
	public class TestContext: IndexedDbContext
	{
		static TestContext()
		{
			Database.SetInitializer<TestContext>(null);
		}

		public TestContext() : base("Name=Test")
		{
		}

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			modelBuilder.Conventions.Remove<PluralizingEntitySetNameConvention>();
			modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

			base.OnModelCreating(modelBuilder);
		}

		public DbSet<Order> Orders
		{
			get;
			set;
		}

		public DbSet<Customer> Customers
		{
			get;
			set;
		}

		public DbSet<Product> Products
		{
			get;
			set;
		}
	}
}
