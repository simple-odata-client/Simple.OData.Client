using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Simple.OData.NorthwindModel.Entities;

public class Supplier
{
	[Key]
	public int SupplierID { get; set; }
	[Required]
	public string CompanyName { get; set; }
	public string ContactName { get; set; }
	public string ContactTitle { get; set; }
	public string Address { get; set; }
	public string City { get; set; }
	public string? Region { get; set; }
	public string PostalCode { get; set; }
	public string Country { get; set; }
	public string Phone { get; set; }
	public string? Fax { get; set; }

	public virtual ICollection<Product> Products { get; set; }
}
