using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Simple.OData.NorthwindModel.Entities;

public class Employee
{
	[Key]
	public int EmployeeID { get; set; }
	[Required]
	public string FirstName { get; set; }
	[Required]
	public string LastName { get; set; }
	public string Title { get; set; }
	public DateTime? BirthDate { get; set; }
	public DateTime? HireDate { get; set; }
	public string Address { get; set; }
	public string City { get; set; }
	public string? Region { get; set; }
	public string PostalCode { get; set; }
	public string Country { get; set; }
	public string HomePhone { get; set; }
	public string Extension { get; set; }
	public byte[] Photo { get; set; }
	public string? Notes { get; set; }
	public int? ReportsTo { get; set; }

	[ForeignKey("ReportsTo")]
	public virtual Employee Superior { get; set; }
	public virtual ICollection<Employee> Subordinates { get; set; }
	public virtual ICollection<Order> Orders { get; set; }
}
