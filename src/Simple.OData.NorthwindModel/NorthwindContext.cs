using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using ActionProviderImplementation;
using Simple.OData.NorthwindModel;
using Simple.OData.NorthwindModel.Entities;

namespace NorthwindModel;

public class NorthwindContext : DbContext
{
	static NorthwindContext()
	{
		Database.SetInitializer(new NorthwindInitializer());
	}

	protected override void OnModelCreating(DbModelBuilder modelBuilder)
	{
		modelBuilder.ComplexType<Address>();
		modelBuilder.Entity<Category>().Property(x => x.CategoryID).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
		modelBuilder.Entity<Customer>().Property(x => x.CustomerID).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
		modelBuilder.Entity<Employee>().Property(x => x.EmployeeID).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
		modelBuilder.Entity<Order>().Property(x => x.OrderID).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
		modelBuilder.Entity<Product>().Property(x => x.ProductID).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
		modelBuilder.Entity<Shipper>().Property(x => x.ShipperID).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
		modelBuilder.Entity<Transport>().Property(x => x.TransportID).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

		base.OnModelCreating(modelBuilder);
	}

	private static int NextId = 1000;

	private static void SetKey<T>(ObjectStateEntry entry, Action<T> setKey) where T : class
	{
		if (entry.Entity is T entity)
		{
			setKey(entity);
		}
	}

	public override int SaveChanges()
	{
		ChangeTracker.DetectChanges();
		var context = (this as System.Data.Entity.Infrastructure.IObjectContextAdapter).ObjectContext;

		foreach (var entry in context.ObjectStateManager.GetObjectStateEntries(EntityState.Added | EntityState.Modified))
		{
			SetKey<Category>(entry, x => { if (x.CategoryID == 0) { x.CategoryID = ++NextId; } });
			SetKey<Employee>(entry, x => { if (x.EmployeeID == 0) { x.EmployeeID = ++NextId; } });
			SetKey<Order>(entry, x => { if (x.OrderID == 0) { x.OrderID = ++NextId; } });
			SetKey<Product>(entry, x => { if (x.ProductID == 0) { x.ProductID = ++NextId; } });
			SetKey<Shipper>(entry, x => { if (x.ShipperID == 0) { x.ShipperID = ++NextId; } });
			SetKey<Transport>(entry, x => { if (x.TransportID == 0) { x.TransportID = ++NextId; } });
		}

		return base.SaveChanges();
	}

	public virtual DbSet<Category> Categories { get; set; }
	public virtual DbSet<Product> Products { get; set; }
	public virtual DbSet<Supplier> Suppliers { get; set; }
	public virtual DbSet<Customer> Customers { get; set; }
	public virtual DbSet<Employee> Employees { get; set; }
	public virtual DbSet<Shipper> Shippers { get; set; }
	public virtual DbSet<Order> Orders { get; set; }
	public virtual DbSet<OrderDetail> Order_Details { get; set; }
	public virtual DbSet<Transport> Transport { get; set; }

	[NonBindableAction]
	public static Address PassThroughAddress(Address address)
	{
		return address;
	}
}
