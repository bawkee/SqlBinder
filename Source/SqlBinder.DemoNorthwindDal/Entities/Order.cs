using System;

namespace SqlBinder.DemoNorthwindDal.Entities
{
	public class Order
	{
		public int OrderId { get; set; }
		public string CustomerId { get; set; }
		public int EmployeeId { get; set; }
		public int ShipperId { get; set; }
		public DateTime? OrderDate { get; set; }
		public DateTime? RequiredDate { get; set; }
		public DateTime? ShippedDate { get; set; }
		public decimal Freight { get; set; }
		public string CustomerName { get; set; }
		public string EmployeeName { get; set; }
		public string ShippedVia { get; set; }
		public string ShipName { get; set; }
		public string ShipAddress { get; set; }
		public string ShipCity { get; set; }
		public string ShipRegion { get; set; }
		public string ShipPostalCode { get; set; }
		public string ShipCountry { get; set; }
	}
}
