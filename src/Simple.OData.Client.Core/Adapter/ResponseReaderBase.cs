using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Simple.OData.Client;

public abstract class ResponseReaderBase : IResponseReader
{
	protected readonly ISession _session;

	protected ResponseReaderBase(ISession session)
	{
		_session = session;
	}

	protected ITypeCache TypeCache => _session.TypeCache;

	public abstract Task<ODataResponse> GetResponseAsync(HttpResponseMessage responseMessage);

	public async Task AssignBatchActionResultsAsync(IODataClient client,
		ODataResponse batchResponse, IList<Func<IODataClient, Task>> actions, IList<int> responseIndexes)
	{
		var exceptions = new List<Exception>();
		for (var actionIndex = 0; actionIndex < actions.Count && !exceptions.Any(); actionIndex++)
		{
			var responseIndex = responseIndexes[actionIndex];
			if (responseIndex >= 0 && responseIndex < batchResponse.Batch.Count)
			{
				var actionResponse = batchResponse.Batch[responseIndex];
				if (actionResponse.Exception != null)
				{
					if (actionResponse.StatusCode == (int)HttpStatusCode.NotFound && _session.Settings.IgnoreResourceNotFoundException)
					{
						await actions[actionIndex](new ODataClient(client as ODataClient, actionResponse))
							.ConfigureAwait(false);
					}
					else
					{
						exceptions.Add(actionResponse.Exception);
					}
				}
				else
				{
					await actions[actionIndex](new ODataClient(client as ODataClient, actionResponse))
						.ConfigureAwait(false);
				}
			}
		}

		if (exceptions.Any())
		{
			throw exceptions.First();
		}
	}

	protected abstract void ConvertEntry(ResponseNode entryNode, object entry);

	protected void StartFeed(Stack<ResponseNode> nodeStack, ODataFeedAnnotations feedAnnotations)
	{
		nodeStack.Push(new ResponseNode
		{
			Feed = new AnnotatedFeed(new List<AnnotatedEntry>()),
		});
	}

	protected void EndFeed(Stack<ResponseNode> nodeStack, ODataFeedAnnotations feedAnnotations, ref ResponseNode rootNode)
	{
		var feedNode = nodeStack.Pop();
		if (nodeStack.Any())
		{
			nodeStack.Peek().Feed = feedNode.Feed;
		}
		else
		{
			rootNode = feedNode;
		}

		feedNode.Feed.SetAnnotations(feedAnnotations);
	}

	protected void StartEntry(Stack<ResponseNode> nodeStack)
	{
		nodeStack.Push(new ResponseNode
		{
			Entry = new AnnotatedEntry(new Dictionary<string, object>())
		});
	}

	protected void EndEntry(Stack<ResponseNode> nodeStack, ref ResponseNode rootNode, object entry)
	{
		var entryNode = nodeStack.Pop();
		ConvertEntry(entryNode, entry);
		if (nodeStack.Any())
		{
			var node = nodeStack.Peek();
			if (node.Feed != null)
			{
				node.Feed.Entries.Add(entryNode.Entry);
			}
			else
			{
				node.Entry = entryNode.Entry;
			}
		}
		else
		{
			rootNode = entryNode;
		}
	}

	protected void StartNavigationLink(Stack<ResponseNode> nodeStack, string linkName)
	{
		nodeStack.Push(new ResponseNode
		{
			LinkName = linkName,
		});
	}

	protected void EndNavigationLink(Stack<ResponseNode> nodeStack)
	{
		var linkNode = nodeStack.Pop();
		var linkValue = linkNode.Value;
		if (linkValue != null)
		{
			if (linkValue is IDictionary<string, object> d)
			{
				if (!d.Any())
				{
					linkValue = null;
				}
				else if (_session.Settings.IncludeAnnotationsInResults)
				{
					d[FluentCommand.AnnotationsLiteral] = linkNode.Entry.Annotations;
				}
			}
			else if (linkValue is IEnumerable<AnnotatedEntry> annotatedEntries)
			{
				linkValue = annotatedEntries.Select(x =>
				{
					if (_session.Settings.IncludeAnnotationsInResults)
					{
						x.Data[FluentCommand.AnnotationsLiteral] = x.Annotations;
					}

					return x.Data;
				}).ToArray();
			}

			nodeStack.Peek().Entry.Data.Add(linkNode.LinkName, linkValue);
		}

		if (linkNode.Feed?.Annotations != null)
		{
			nodeStack.Peek().Entry.SetLinkAnnotations(linkNode.Feed.Annotations);
		}
	}
}
