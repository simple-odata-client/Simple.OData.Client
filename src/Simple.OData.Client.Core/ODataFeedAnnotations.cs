using System;
using System.Collections.Generic;
using System.Linq;

namespace Simple.OData.Client;

/// <summary>
/// Contains additional information about OData feed
/// </summary>
public class ODataFeedAnnotations
{
	/// <summary>
	/// The ID of the corresponding entity set.
	/// </summary>
	public string? Id { get; internal set; }

	/// <summary>
	/// The result item count.
	/// </summary>
	public long? Count { get; internal set; }

	/// <summary>
	/// A URL that can be used to retrieve changes to the current set of results
	/// </summary>
	public Uri? DeltaLink { get; internal set; }

	/// <summary>
	/// A URL that can be used to retrieve the next subset of the requested collection.
	/// When set, indicates that the response is only a subset of the requested collection of entities or collection of entity references.
	/// </summary>
	public Uri? NextPageLink { get; internal set; }

	/// <summary>
	/// Custom feed annotations.
	/// </summary>
	public IEnumerable<object>? InstanceAnnotations { get; internal set; }

	/// <summary>
	/// Custom feed annotations returned as an adapter-specific annotation type
	/// </summary>
	/// <typeparam name="T">Custom type</typeparam>
	/// <returns></returns>
	public IEnumerable<T> GetInstanceAnnotations<T>()
	{
		return InstanceAnnotations.Select(x => (T)x);
	}

	internal void CopyFrom(ODataFeedAnnotations src)
	{
		if (src != null)
		{
			Id = src.Id;
			Count = src.Count;
			DeltaLink = src.DeltaLink;
			NextPageLink = src.NextPageLink;
			InstanceAnnotations = src.InstanceAnnotations;
		}
		else
		{
			Id = null;
			Count = null;
			DeltaLink = null;
			NextPageLink = null;
			InstanceAnnotations = null;
		}
	}

	internal void Merge(ODataFeedAnnotations src)
	{
		if (src != null)
		{
			Id ??= src.Id;
			Count ??= src.Count;
			DeltaLink ??= src.DeltaLink;
			NextPageLink ??= src.NextPageLink;
			InstanceAnnotations ??= src.InstanceAnnotations;
		}
	}
}
