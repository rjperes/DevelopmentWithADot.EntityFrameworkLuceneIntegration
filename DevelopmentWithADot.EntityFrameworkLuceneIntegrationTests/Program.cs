using System.Linq;
using DevelopmentWithADot.EntityFrameworkLuceneIntegration;

namespace DevelopmentWithADot.EntityFrameworkLuceneIntegrationTests
{
	class Program
	{
		static void Main(string[] args)
		{
			using (TestContext ctx = new TestContext())
			{
				/*ctx.Orders.DeleteIndex();
				ctx.Products.DeleteIndex();
				ctx.Orders.Index();
				ctx.Products.Index();*/


				var allProducts = ctx.Products.ToList();
				var allOrders = ctx.Orders.ToList();

				var productsWithNameEqualTo1 = ctx.Search<Product>("Name:1").ToList();
				var productsWithNameDifferentThan1 = ctx.Search<Product>(x => x.Name != "1").ToList();
				var productsWithNameContainingProduct = ctx.Search<Product>("Name", "Product").ToList();
				var productsWithName = ctx.Search<Product>(x => x.Name != null).ToList();
				var productsStartingWithSome = ctx.Search<Product>(x => x.Name.StartsWith("Some")).ToList();
				var productsContainsOne = ctx.Search<Product>(x => x.Name.Contains("One")).ToList();

				var ordersWithTotalCostEqualTo200 = ctx.Search<Order>(x => x.TotalCost == 200).ToList();
			}
		}
	}
}
