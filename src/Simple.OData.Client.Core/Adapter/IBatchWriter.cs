using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Simple.OData.Client;

public interface IBatchWriter
{
	Task<ODataRequest> CreateBatchRequestAsync(
		IODataClient client,
		IList<Func<IODataClient, Task>> actions,
		IList<int> responseIndexes,
		IDictionary<string, string>? headers = null);

	Task<object> CreateOperationMessageAsync(
		Uri uri,
		string method,
		string? collection,
		IDictionary<string, object>? entryData,
		bool resultRequired);

	bool HasOperations { get; }

	int LastOperationId { get; }

	string NextContentId();

	string GetContentId(IDictionary<string, object> entryData, object linkData);

	void MapContentId(IDictionary<string, object> entryData, string contentId);

	IDictionary<object, IDictionary<string, object>> BatchEntries { get; }
}
