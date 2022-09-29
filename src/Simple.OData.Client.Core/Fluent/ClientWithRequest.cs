using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.OData.Client;

internal class ClientWithRequest<T> : IClientWithRequest<T>
	where T : class
{
	private readonly ODataRequest _request;
	private readonly ISession _session;
	private readonly RequestRunner _requestRunner;

	public ClientWithRequest(ODataRequest request, ISession session)
	{
		_request = request;
		_session = session;
		_requestRunner = new RequestRunner(_session);
	}

	public ODataRequest GetRequest()
	{
		return _request;
	}

	public IClientWithResponse<T> FromResponse(HttpResponseMessage responseMessage)
	{
		return new ClientWithResponse<T>(_session, _request, responseMessage);
	}

	public Task<IClientWithResponse<T>> RunAsync()
	{
		return RunAsync(CancellationToken.None);
	}

	public async Task<IClientWithResponse<T>> RunAsync(CancellationToken cancellationToken)
	{
		var response = await _requestRunner
			.ExecuteRequestAsync(_request, cancellationToken)
			.ConfigureAwait(false);
		if (cancellationToken.IsCancellationRequested)
		{
			cancellationToken.ThrowIfCancellationRequested();
		}

		return new ClientWithResponse<T>(_session, _request, response);
	}
}
