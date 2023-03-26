using FactoryGenerator.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
				var type = (INamedTypeSymbol)model.GetDeclaredSymbol(constructorGroup.Key);
				var typeName = type.Name;
				var constructors = constructorGroup.Select(c => model.GetDeclaredSymbol(c)!).ToArray();
				var predicate = GetIsParameterPredicate(model);
				var privateDependencyTypes = constructorGroup
					.SelectMany(c => c.ParameterList.Parameters)
					.Where(p => !predicate(p))
					.Select(p => model.GetTypeInfo(p.Type).Type)
					.Distinct((IEqualityComparer<ISymbol>)SymbolEqualityComparer.Default)
					.ToArray();
				var factoryName = $"{typeName}Factory";
				var source = $@"
namespace {GetNamespaceName(type.ContainingNamespace)}
{{
	public interface I{factoryName}
	{{
		{GenerateCreateMethods(type, constructors, model, privateDependencyTypes, true)}
	}}

	public class {factoryName}: I{factoryName}
	{{
		{string.Join("\n", privateDependencyTypes.Select((t, index) => $"\t\tprivate readonly {GetNamespaceName(t.ContainingNamespace)}.{t.Name} _dependency{index};"))}
		public {typeName}Factory({string.Join(", ", privateDependencyTypes.Select((t, index) => $"{GetNamespaceName(t.ContainingNamespace)}.{t.Name} dependency{index}"))})
		{{
			{string.Join("\n", privateDependencyTypes.Select((t, index) => $"_dependency{index} = dependency{index};"))}
		}}
		{GenerateCreateMethods(type, constructors, model, privateDependencyTypes, false)}

	}}
}}";
				context.AddSource($"{type.Name}Factory.g.cs", source);
				//var c = model.GetDeclaredSymbol(constructor);
				//context.AddSource(c.ContainingType.Name, "test");
			}
		}
		catch (Exception) { }
	}

	private static Func<ParameterSyntax, bool> GetIsParameterPredicate(SemanticModel model)
	{
		return p => p.AttributeLists.Any(l => l.Attributes.Any(a =>
		{
			var annotationType = model.GetTypeInfo(a).Type!;
			return annotationType.Name == nameof(ParameterAttribute) && annotationType.ContainingNamespace.Name == "Annotations";
		}));
	}

	private static Func<IParameterSymbol, bool> GetIsParameterPredicateForSymbol(SemanticModel model)
	{
		return p => p.GetAttributes().Any(a =>
		{
			var annotationType = a.AttributeClass!;
			return annotationType.Name == nameof(ParameterAttribute) && annotationType.ContainingNamespace.Name == "Annotations";
		});
	}

	private string GenerateCreateMethods(
		INamedTypeSymbol type,
		IEnumerable<IMethodSymbol> constructors,
		SemanticModel model,
		ISymbol?[] privateDependencyTypes,
		bool onlyHeader)
	{
		var predicate = GetIsParameterPredicateForSymbol(model);
		return string.Join(
			"\r\n",
			constructors.Select(constructor =>
			{
				var parameters = constructor.Parameters.Where(predicate).ToList();
				var header = $"public {type} Create({string.Join(
					",",
					parameters.Select((p, index) => $"{p.Type} {p.Name}")
					)})";
				if (onlyHeader)
					return header + ";";
				return $@"{header}
{{
	return new {type}({string.Join(",", constructor.Parameters.Select(p =>
				{
					var comparer = SymbolEqualityComparer.Default;
					if (predicate(p))
						return parameters.First(parameter => comparer.Equals(parameter.Type, p.Type)).Name;
					return $"_dependency{privateDependencyTypes.ToList().FindIndex(dependency => comparer.Equals(dependency, p.Type))}";
				}))});
}}";
			}));
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
