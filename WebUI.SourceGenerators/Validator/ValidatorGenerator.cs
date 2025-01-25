using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace WebUI.SourceGenerators.Validator;

[Generator]
public class ValidatorGenerator : IIncrementalGenerator
{
    const string _useValidatorAttribute = """
        using System;

        namespace WebUI.Validation
        {
            [AttributeUsage(AttributeTargets.Class)]
            internal sealed class UseValidatorAttribute : Attribute
            {
            }
        }
        """;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static postInitializationContext => postInitializationContext.AddSource(
            hintName: "WebUI.Validation.UseValidatorAttribute.g.cs", 
            sourceText: SourceText.From(_useValidatorAttribute, Encoding.UTF8)));

        var pipeline = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: "WebUI.Validation.UseValidatorAttribute",
            predicate: static (syntaxNode, cancellationToken) => syntaxNode is ClassDeclarationSyntax,
            transform: static (context, cancellationToken) =>
            {
                var containingClass = (INamedTypeSymbol)context.TargetSymbol;
                return new Model(
                    // Note: this is a simplified example. You will also need to handle the case where the type is in a global namespace, nested, etc.
                    Namespace: containingClass.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)) ?? "",
                    ClassName: containingClass.Name);
            }
        );

        context.RegisterSourceOutput(pipeline, static (context, model) =>
        {
            var sourceText = SourceText.From($$"""
                using FluentValidation;
                using FluentValidation.Results;
                using System.Threading.Tasks;

                namespace {{model.Namespace}};
                partial class {{model.ClassName}} : Filters.IValidator
                {
                    static partial void ConfigureValidator(AbstractValidator<{{model.ClassName}}> validator);

                    static readonly Validator _validator = new();
                    public async Task<ValidationResult> ValidateAsync() => await _validator.ValidateAsync(this);

                    class Validator : AbstractValidator<{{model.ClassName}}>
                    {
                        public Validator()
                        {
                            ConfigureValidator(this);
                        }
                    }
                }
                """, Encoding.UTF8);

            context.AddSource($"{model.ClassName}.g.cs", sourceText);
        });
    }

    private record Model(string Namespace, string ClassName);
}
