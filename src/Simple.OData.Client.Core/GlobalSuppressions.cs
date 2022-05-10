// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
	"Style", "IDE0060:Remove unused parameter",
	Justification = "Required signature",
	Scope = "member",
	Target = "~M:Simple.OData.Client.ODataExpression.op_True(Simple.OData.Client.ODataExpression)~System.Boolean")
]

[assembly: SuppressMessage(
	"Style", "IDE0060:Remove unused parameter",
	Justification = "Required signature",
	Scope = "member",
	Target = "~M:Simple.OData.Client.ODataExpression.op_False(Simple.OData.Client.ODataExpression)~System.Boolean")
]
[assembly: SuppressMessage(
	"Globalization",
	"CA1308:Normalize strings to uppercase",
	Justification = "Case is important",
	Scope = "namespaceanddescendants",
	Target = "~N:Simple.OData.Client")
]
