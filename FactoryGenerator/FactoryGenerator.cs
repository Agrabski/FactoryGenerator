using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FactoryGenerator;

[Generator]
public class FactoryGenerator : ISourceGenerator
{
	public void Execute(GeneratorExecutionContext context)
	{
		if (context.SyntaxReceiver is not ConstructorAggregatingSyntaxReciever reciever)
			throw new InvalidOperationException();
		try
		{
			foreach (var constructorGroup in reciever.Constructors.GroupBy(s => s.Parent))
			{
				var model = context.Compilation.GetSemanticModel(constructorGroup.Key!.SyntaxTree);
				var type = model.GetDeclaredSymbol(constructorGroup.Key);
				var typeName = type.Name;
				context.AddSource($"{type.Name}Factory.g.cs", $@"
namespace {GetNamespaceName(type.ContainingNamespace)}
{{
	public class X{{}}
	public interface I{typeName}Factory
	{{
		{GenerateCreateMethods(constructorGroup, model)}
	}}
}}");
				//var c = model.GetDeclaredSymbol(constructor);
				//context.AddSource(c.ContainingType.Name, "test");
			}
		}
		catch (Exception) { }
	}

	private string GenerateCreateMethods(IEnumerable<ConstructorDeclarationSyntax> constructorGroup, SemanticModel model)
	{
		return string.Join(
			"\r\n",
			constructorGroup.Select(constructor => "")
			);
	}

	private static string GetNamespaceName(INamespaceSymbol symbol)
	{
		if (symbol.IsGlobalNamespace)
			return string.Empty;
		var parentName = GetNamespaceName(symbol.ContainingNamespace);
		if (parentName == string.Empty)
			return symbol.Name;
		else return $"{parentName}.{symbol.Name}";
	}

	public void Initialize(GeneratorInitializationContext context)
	{
		if (!Debugger.IsAttached)
			_ = Debugger.Launch();
		context.RegisterForSyntaxNotifications(() => new ConstructorAggregatingSyntaxReciever());
	}
}
