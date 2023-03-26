using FactoryGenerator.Annotations;
using System;

namespace FactoryGenerator.Test;

public class Class1
{
	private readonly IDisposable _disposable;
	[Factory]
	public Class1([Parameter] string value, IDisposable disposable)
	{
		_disposable = disposable;
	}
}
