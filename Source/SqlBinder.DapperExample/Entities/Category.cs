namespace SqlBinder.DapperExample.Entities
{
	public class Category
	{
		public int CategoryId { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public CategorySale SaleInfo { get; set; }
	}
}
