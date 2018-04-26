using System;
using System.Collections.Generic;

namespace SqlBinder.DapperExample.Entities
{
	public class Order
	{
		public int OrderId { get; set; }
		//public string CustomerId { get; set; }
		public List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
	}
}
