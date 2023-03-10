using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace FactoryGenerator;

internal class ConstructorAggregatingSyntaxReciever : ISyntaxReceiver
{
	public List<ConstructorDeclarationSyntax> Constructors { get; } = new();

	public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
	{
		if (syntaxNode is ConstructorDeclarationSyntax constructor)
		{
			Constructors.Add(constructor);
		}
	}
}
