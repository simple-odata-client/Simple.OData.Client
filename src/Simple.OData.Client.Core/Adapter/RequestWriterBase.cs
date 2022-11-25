﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Simple.OData.Client.Extensions;

namespace Simple.OData.Client;

public abstract class RequestWriterBase : IRequestWriter
{
	protected readonly ISession _session;
	protected readonly Lazy<IBatchWriter> _deferredBatchWriter;

	protected RequestWriterBase(ISession session, Lazy<IBatchWriter> deferredBatchWriter)
	{
		_session = session;
		_deferredBatchWriter = deferredBatchWriter;
	}

	protected bool IsBatch => _deferredBatchWriter != null;

	protected ITypeCache TypeCache => _session.TypeCache;

	protected ITypeConverter Converter => TypeCache.Converter;

	public async Task<ODataRequest> CreateGetRequestAsync(
		string commandText,
		bool scalarResult,
		IDictionary<string, string>? headers = null)
	{
		await WriteEntryContentAsync(
			RestVerbs.Get, Utils.ExtractCollectionName(commandText), commandText, null, true)
			.ConfigureAwait(false);

		var request = new ODataRequest(RestVerbs.Get, _session, commandText, headers)
		{
			ReturnsScalarResult = scalarResult,
			ResultRequired = true,
		};
		AssignHeaders(request);
		return request;
	}

	public async Task<ODataRequest> CreatePutRequestAsync(
		string commandText,
		Stream stream,
		string? mediaType = null,
		bool optimisticConcurrency = false,
		IDictionary<string, string>? headers = null)
	{
		var entryContent = await WriteStreamContentAsync(stream, IsTextMediaType(mediaType))
			.ConfigureAwait(false);

		var request = new ODataRequest(RestVerbs.Put, _session, commandText, null, entryContent, mediaType, headers)
		{
			CheckOptimisticConcurrency = optimisticConcurrency
		};
		AssignHeaders(request);
		return request;
	}

	public async Task<ODataRequest> CreateInsertRequestAsync(
		string collection,
		string commandText,
		IDictionary<string, object> entryData,
		bool resultRequired,
		IDictionary<string, string>? headers = null)
	{
		var segments = commandText.Split('/');
		if (segments.Length > 1 && segments.Last().Contains("."))
		{
			commandText = commandText.Substring(0, commandText.Length - segments.Last().Length - 1);
		}

		var entryContent = await WriteEntryContentAsync(
			RestVerbs.Post, collection, commandText, entryData, resultRequired)
			.ConfigureAwait(false);

		var request = new ODataRequest(RestVerbs.Post, _session, commandText, entryData, entryContent, headers: headers)
		{
			ResultRequired = resultRequired,
		};
		AssignHeaders(request);
		return request;
	}

	public async Task<ODataRequest> CreateUpdateRequestAsync(
		string collection,
		string entryIdent,
		IDictionary<string, object> entryKey,
		IDictionary<string, object> entryData,
		bool resultRequired,
		IDictionary<string, string>? headers = null)
	{
		var entityCollection = _session.Metadata.GetEntityCollection(collection);
		var entryDetails = _session.Metadata.ParseEntryDetails(entityCollection.Name, entryData);

		var hasPropertiesToUpdate = entryDetails.Properties.Count > 0;
		var usePatch = _session.Settings.PreferredUpdateMethod == ODataUpdateMethod.Patch || !hasPropertiesToUpdate;
		if (HasUpdatedKeyProperties(collection, entryKey, entryData))
		{
			usePatch = false;
		}

		var updateMethod = usePatch ? RestVerbs.Patch : RestVerbs.Put;

#pragma warning disable CS0618 // Type or member is obsolete - purpose of this test
		updateMethod = _session.Settings.PreferredUpdateMethod == ODataUpdateMethod.Merge ? RestVerbs.Merge : updateMethod;
#pragma warning restore CS0618 // Type or member is obsolete

		var entryContent = await WriteEntryContentAsync(
			updateMethod, collection, entryIdent, entryData, resultRequired)
			.ConfigureAwait(false);

		var checkOptimisticConcurrency = _session.Metadata.EntityCollectionRequiresOptimisticConcurrencyCheck(collection);
		var request = new ODataRequest(updateMethod, _session, entryIdent, entryData, entryContent, headers: headers)
		{
			ResultRequired = resultRequired,
			CheckOptimisticConcurrency = checkOptimisticConcurrency
		};
		AssignHeaders(request);
		return request;
	}

	public async Task<ODataRequest> CreateDeleteRequestAsync(
		string collection,
		string entryIdent,
		IDictionary<string, string>? headers = null)
	{
		await WriteEntryContentAsync(
			RestVerbs.Delete, collection, entryIdent, null, false)
			.ConfigureAwait(false);

		var request = new ODataRequest(RestVerbs.Delete, _session, entryIdent, headers)
		{
			CheckOptimisticConcurrency = _session.Metadata.EntityCollectionRequiresOptimisticConcurrencyCheck(collection)
		};
		AssignHeaders(request);
		return request;
	}

	public async Task<ODataRequest> CreateLinkRequestAsync(
		string collection,
		string linkName,
		string entryIdent,
		string linkIdent,
		IDictionary<string, string>? headers = null)
	{
		var associationName = _session.Metadata.GetNavigationPropertyExactName(collection, linkName);
		var linkMethod = _session.Metadata.IsNavigationPropertyCollection(collection, associationName) ?
			RestVerbs.Post :
			RestVerbs.Put;

		var commandText = FormatLinkPath(entryIdent, associationName);
		var linkContent = await WriteLinkContentAsync(linkMethod, commandText, linkIdent)
			.ConfigureAwait(false);
		var request = new ODataRequest(linkMethod, _session, commandText, null, linkContent, headers: headers)
		{
			IsLink = true,
		};
		AssignHeaders(request);
		return request;
	}

	public async Task<ODataRequest> CreateUnlinkRequestAsync(
		string collection,
		string linkName,
		string entryIdent,
		string linkIdent,
		IDictionary<string, string>? headers = null)
	{
		var associationName = _session.Metadata.GetNavigationPropertyExactName(collection, linkName);
		var commandText = FormatLinkPath(entryIdent, associationName, linkIdent);

		await WriteEntryContentAsync(RestVerbs.Delete, collection, commandText, null, false)
			.ConfigureAwait(false);

		var request = new ODataRequest(RestVerbs.Delete, _session, commandText, headers)
		{
			IsLink = true,
		};
		AssignHeaders(request);
		return request;
	}

	public async Task<ODataRequest> CreateFunctionRequestAsync(
		string commandText,
		string functionName,
		IDictionary<string, string>? headers = null)
	{
		var verb = _session.Metadata.GetFunctionVerb(functionName);

		await WriteFunctionContentAsync(verb, commandText)
			.ConfigureAwait(false);

		var request = new ODataRequest(verb, _session, commandText, headers)
		{
			ResultRequired = true,
		};
		AssignHeaders(request);
		return request;
	}

	public async Task<ODataRequest> CreateActionRequestAsync(
		string commandText,
		string actionName,
		string boundTypeName,
		IDictionary<string, object> parameters,
		bool resultRequired,
		IDictionary<string, string>? headers = null)
	{
		var verb = RestVerbs.Post;
		Stream? entryContent = null;
		var usePayloadFormat = ODataPayloadFormat.Unspecified;

		if (parameters != null && parameters.Any())
		{
			entryContent = await WriteActionContentAsync(RestVerbs.Post, commandText, actionName, boundTypeName, parameters)
				.ConfigureAwait(false);
			usePayloadFormat = ODataPayloadFormat.Json;
		}
		else
		{
			await WriteFunctionContentAsync(verb, commandText)
				.ConfigureAwait(false);
		}

		var request = new ODataRequest(verb, _session, commandText, parameters, entryContent, headers: headers)
		{
			ResultRequired = resultRequired,
			UsePayloadFormat = usePayloadFormat,
		};
		AssignHeaders(request);
		return request;
	}

	protected abstract Task<Stream> WriteEntryContentAsync(
		string method,
		string collection,
		string commandText,
		IDictionary<string, object>? entryData,
		bool resultRequired);

	protected abstract Task<Stream> WriteLinkContentAsync(
		string method,
		string commandText,
		string linkIdent);

	protected abstract Task<Stream> WriteFunctionContentAsync(
		string method,
		string commandText);

	protected abstract Task<Stream> WriteActionContentAsync(
		string method,
		string commandText,
		string actionName,
		string boundTypeName,
		IDictionary<string, object> parameters);

	protected abstract Task<Stream> WriteStreamContentAsync(
		Stream stream,
		bool writeAsText);

	protected abstract string FormatLinkPath(
		string entryIdent,
		string navigationPropertyName,
		string? linkIdent = null);

	protected abstract void AssignHeaders(ODataRequest request);

	protected string? GetContentId(ReferenceLink referenceLink)
	{
		string? contentId = null;
		var linkEntry = referenceLink.LinkData.ToDictionary(TypeCache);
		if (_deferredBatchWriter != null)
		{
			contentId = _deferredBatchWriter.Value.GetContentId(linkEntry, referenceLink.LinkData);
		}

		return contentId;
	}

	private bool HasUpdatedKeyProperties(
		string collection,
		IDictionary<string, object> entryKey,
		IDictionary<string, object> entryData)
	{
		var entityCollection = _session.Metadata.GetEntityCollection(collection);
		foreach (var propertyName in _session.Metadata.GetStructuralPropertyNames(entityCollection.Name))
		{
			if (entryKey.ContainsKey(propertyName) && entryData.ContainsKey(propertyName) &&
				!entryKey[propertyName].Equals(entryData[propertyName]))
			{
				return true;
			}
		}

		return false;
	}

	private static bool IsTextMediaType(string? mediaType)
	{
		if (mediaType == null)
		{
			return true;
		}

		var items = mediaType.Split('/');
		var type = items[0];
		var subtype = items.Length > 0 ? items[1] : string.Empty;

		if (type == "text")
		{
			return true;
		}

		if (subtype == "text" || subtype == "xml" || subtype == "json")
		{
			return true;
		}

		return false;
	}
}
