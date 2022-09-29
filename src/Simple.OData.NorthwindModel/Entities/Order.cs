using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Simple.OData.NorthwindModel.Entities;

public class Order
{
	[Key]
	public int OrderID { get; set; }
	public string CustomerID { get; set; }
	public int EmployeeID { get; set; }
	public string ShipName { get; set; }
	public string ShipAddress { get; set; }
	public string ShipCity { get; set; }
	public string? ShipRegion { get; set; }
	public string ShipPostalCode { get; set; }
	public string ShipCountry { get; set; }
	public int? ShipVia { get; set; }
	public DateTime OrderDate { get; set; }
	public DateTime RequiredDate { get; set; }
	public DateTime ShippedDate { get; set; }
	public decimal? Freight { get; set; }

	public virtual Employee Employee { get; set; }
	public virtual Customer Customer { get; set; }
	public virtual ICollection<OrderDetail> Order_Details { get; set; }
	[ForeignKey("ShipVia")]
	public virtual Shipper Shipper { get; set; }
}
