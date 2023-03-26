using System;

namespace FactoryGenerator.Annotations
{
	[AttributeUsage(AttributeTargets.Constructor)]
	public sealed class FactoryAttribute : Attribute
	{

	}

	[AttributeUsage(AttributeTargets.Parameter)]
	public sealed class ParameterAttribute : Attribute { }
}
