using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlBinder.DapperExample.Entities
{
	public class OrderDetail
	{
		public int OrderId { get; set; }
		//public int ProductId { get; set; }
		public decimal UnitPrice { get; set; }
		//public Product Product { get; set; }
	}
}
