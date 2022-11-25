using System;

namespace Simple.OData.Client.Tests;

public class Order
{
	public int OrderID { get; set; }
	public DateTime OrderDate { get; set; }
	public DateTime RequiredDate { get; set; }
	public DateTime ShippedDate { get; set; }
	public DateTimeOffset ShippedDateTimeOffset { get; set; }

	public OrderDetail[] OrderDetails { get; set; }
	
	public Employee Employee { get; set; }
}
