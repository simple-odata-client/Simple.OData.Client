using System;
using System.Collections.Generic;
using System.Linq;

namespace Simple.OData.Client;

/// <summary>
/// Contains additional information about OData entry
/// </summary>
public class ODataEntryAnnotations
{
	/// <summary>
	/// Contains additional information about OData association links
	/// </summary>
	public class AssociationLink
	{
		/// <summary>
		/// The association name.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// The association URI.
		/// </summary>
		public Uri Uri { get; set; }
	}

	/// <summary>
	/// The entry ID.
	/// </summary>
	public string? Id { get; set; }

	/// <summary>
	/// The type name of the entry.
	/// </summary>
	public string? TypeName { get; set; }

	/// <summary>
	/// The link that can be used to read the entry.
	/// </summary>
	public Uri? ReadLink { get; set; }

	/// <summary>
	/// The link can be used to edit the entry.
	/// </summary>
	public Uri? EditLink { get; set; }

	/// <summary>
	/// The entry ETag.
	/// </summary>
	public string? ETag { get; set; }

	/// <summary>
	/// The collection of entry association links.
	/// </summary>
	public IEnumerable<AssociationLink>? AssociationLinks { get; set; }

	/// <summary>
	/// The media resource annotations.
	/// </summary>
	public ODataMediaAnnotations? MediaResource { get; set; }

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

	internal void CopyFrom(ODataEntryAnnotations src)
	{
		if (src != null)
		{
			Id = src.Id;
			TypeName = src.TypeName;
			ReadLink = src.ReadLink;
			EditLink = src.EditLink;
			ETag = src.ETag;
			AssociationLinks = src.AssociationLinks;
			MediaResource = src.MediaResource;
			InstanceAnnotations = src.InstanceAnnotations;
		}
		else
		{
			Id = null;
			TypeName = null;
			ReadLink = null;
			EditLink = null;
			ETag = null;
			AssociationLinks = null;
			MediaResource = null;
			InstanceAnnotations = null;
		}
	}

	internal void Merge(ODataEntryAnnotations src)
	{
		if (src != null)
		{
			Id ??= src.Id;
			TypeName ??= src.TypeName;
			ReadLink ??= src.ReadLink;
			EditLink ??= src.EditLink;
			ETag ??= src.ETag;
			AssociationLinks ??= src.AssociationLinks;
			MediaResource ??= src.MediaResource;
			InstanceAnnotations ??= src.InstanceAnnotations;
		}
	}
}
