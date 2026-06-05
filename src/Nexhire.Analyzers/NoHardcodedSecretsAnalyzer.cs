using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Nexhire.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NoHardcodedSecretsAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "NEX001";

        private static readonly LocalizableString Title = "Hardcoded secret detected";
        private static readonly LocalizableString MessageFormat = "Do not hardcode secrets or credentials in the codebase. Found potential secret assignment to '{0}'.";
        private static readonly LocalizableString Description = "Secrets such as passwords, API keys, and connection strings should not be hardcoded.";
        private const string Category = "Security";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Register for variable declarations and assignments
            context.RegisterSyntaxNodeAction(AnalyzeVariableDeclaration, SyntaxKind.VariableDeclarator);
            context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);
        }

        private void AnalyzeVariableDeclaration(SyntaxNodeAnalysisContext context)
        {
            var declarator = (VariableDeclaratorSyntax)context.Node;
            
            if (declarator.Initializer == null) return;
            
            var identifierName = declarator.Identifier.Text;
            if (IsSuspiciousName(identifierName) && IsHardcodedString(declarator.Initializer.Value))
            {
                var diagnostic = Diagnostic.Create(Rule, declarator.Identifier.GetLocation(), identifierName);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
        {
            var assignment = (AssignmentExpressionSyntax)context.Node;

            string identifierName = null;
            if (assignment.Left is IdentifierNameSyntax id)
            {
                identifierName = id.Identifier.Text;
            }
            else if (assignment.Left is MemberAccessExpressionSyntax memberAccess)
            {
                identifierName = memberAccess.Name.Identifier.Text;
            }

            if (identifierName != null && IsSuspiciousName(identifierName) && IsHardcodedString(assignment.Right))
            {
                var diagnostic = Diagnostic.Create(Rule, assignment.Left.GetLocation(), identifierName);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private bool IsSuspiciousName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            var lowerName = name.ToLowerInvariant();
            
            // Allow things like 'PasswordHasher' or 'IsPasswordValid' 
            // We're mainly looking for names that imply holding a literal secret
            string[] suspiciousKeywords = { "password", "secret", "apikey", "accesskey", "connectionstring", "token" };
            
            // Exceptions
            if (lowerName.Contains("hasher") || lowerName.Contains("validator") || lowerName.Contains("result") || lowerName.Contains("policy") || lowerName.Contains("type"))
                return false;

            foreach (var keyword in suspiciousKeywords)
            {
                if (lowerName.Contains(keyword))
                    return true;
            }

            return false;
        }

        private bool IsHardcodedString(ExpressionSyntax expression)
        {
            if (expression is LiteralExpressionSyntax literal)
            {
                if (literal.IsKind(SyntaxKind.StringLiteralExpression))
                {
                    var val = literal.Token.ValueText;
                    // Ignore empty strings or very short strings
                    return !string.IsNullOrWhiteSpace(val) && val.Length > 2;
                }
            }
            else if (expression is InterpolatedStringExpressionSyntax interpolated)
            {
                // Simple check for interpolated string containing mostly literals
                return interpolated.Contents.Any(c => c is InterpolatedStringTextSyntax text && !string.IsNullOrWhiteSpace(text.TextToken.ValueText));
            }

            return false;
        }
    }
}
