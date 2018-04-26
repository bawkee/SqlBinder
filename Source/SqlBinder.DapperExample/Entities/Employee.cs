using System;

namespace SqlBinder.DapperExample.Entities
{
	public class Employee
	{
		public int EmployeeId { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Title { get; set; }
		public string FullName => $"{Title} {FirstName} {LastName}";
		public DateTime? HireDate { get; set; }
	}
}
