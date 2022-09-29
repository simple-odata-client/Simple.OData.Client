namespace Simple.OData.Client;

internal class ExpressionContext
{
	public ISession Session { get; set; }
	public EntityCollection EntityCollection { get; set; }
	public string ScopeQualifier { get; set; }
	public string DynamicPropertiesContainerName { get; set; }
	public bool IsQueryOption { get; set; }

	public ExpressionContext(ISession session)
	{
		Session = session;
	}

	public ExpressionContext(
		ISession session,
		EntityCollection? entityCollection,
		string? scopeQualifier,
		string dynamicPropertiesContainerName)
	{
		Session = session;
		EntityCollection = entityCollection;
		ScopeQualifier = scopeQualifier;
		DynamicPropertiesContainerName = dynamicPropertiesContainerName;
	}

	public ExpressionContext(ISession session, bool isQueryOption)
	{
		Session = session;
		IsQueryOption = isQueryOption;
	}
}
