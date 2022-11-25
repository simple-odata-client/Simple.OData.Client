using Microsoft.Data.OData;

namespace Simple.OData.Client.V3.Adapter;

internal static class ODataExtensions
{
	public static ODataMessageReaderSettings ToReaderSettings(this ISession session)
	{
		return session.Settings.ToReaderSettings();
	}

	public static ODataMessageReaderSettings ToReaderSettings(this ODataClientSettings settings)
	{
		var readerSettings = new ODataMessageReaderSettings();
		if (settings.IgnoreUnmappedProperties)
		{
			readerSettings.UndeclaredPropertyBehaviorKinds = ODataUndeclaredPropertyBehaviorKinds.IgnoreUndeclaredValueProperty;
		}

		readerSettings.MessageQuotas.MaxReceivedMessageSize = int.MaxValue;
		readerSettings.ShouldIncludeAnnotation = x => settings.IncludeAnnotationsInResults;
		return readerSettings;
	}
}
