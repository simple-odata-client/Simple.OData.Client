namespace Simple.OData.Client;

public class EntityCollection
{
	internal EntityCollection(
		string name,
		EntityCollection? baseEntityCollection = null)
	{
		Name = name;
		BaseEntityCollection = baseEntityCollection;
	}

	public override string ToString()
	{
		return Name;
	}

	public string Name { get; private set; }

	public EntityCollection? BaseEntityCollection { get; private set; }
}
