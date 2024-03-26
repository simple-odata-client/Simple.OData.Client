﻿using Simple.OData.Client;
using Simple.OData.Tests.Client.Entities;
using Xunit;

namespace Simple.OData.Tests.Client.Core;

public class DynamicContainerTests
{
	private static ITypeCache TypeCache => TypeCaches.TypeCache("test", null);

	[Fact]
	public void ContainerName()
	{
		TypeCache.Register<Animal>();

		Assert.Equal("DynamicProperties", TypeCache.DynamicContainerName(typeof(Animal)));
	}

	[Fact]
	public void ExplicitContainerName()
	{
		TypeCache.Register<Animal>("Foo");

		Assert.Equal("Foo", TypeCache.DynamicContainerName(typeof(Animal)));
	}

	[Fact]
	public void SubTypeContainerName()
	{
		TypeCache.Register<Animal>();

		Assert.Equal("DynamicProperties", TypeCache.DynamicContainerName(typeof(Mammal)));
	}
}