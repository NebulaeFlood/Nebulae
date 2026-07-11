using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Nebulae.Runtime.Emit.Inline.Analyzers.Properties;
using System;
using System.Collections.Immutable;

namespace Nebulae.Runtime.Emit.Inline.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class PlaceholderAnalyzer : DiagnosticAnalyzer
    {
        private const string PlaceholderAssemblyName = "Nebulae.Runtime.Emit.Inline";
        private const string PlaceholderAttributeMetadataName = "Nebulae.Runtime.Emit.Inline.PlaceholderAttribute";
        private const string ReferenceAttributeMetadataName = "Nebulae.Runtime.Emit.Inline.ReferenceAttribute";
        private const string ExpressionMetadataName = "System.Linq.Expressions.Expression`1";
        private const string TypeMetadataName = "System.Type";


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get => ImmutableArray.Create(
                ReferenceEscapeRule,
                PlaceholderTypeRule,
                InvalidContextRule,
                NonConstantOperandRule,
                InvalidOperandValueRule,
                DuplicateLabelRule,
                UndefinedLabelRule);
        }


        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterCompilationStartAction(InitializeCompilation);
        }


        private static void InitializeCompilation(CompilationStartAnalysisContext context)
        {
            Compilation compilation = context.Compilation;

            if (string.Equals(compilation.AssemblyName, PlaceholderAssemblyName, StringComparison.Ordinal))
            {
                return;
            }

            INamedTypeSymbol? placeholderAttribute = compilation.GetTypeByMetadataName(PlaceholderAttributeMetadataName);
            if (placeholderAttribute is null)
            {
                return;
            }

            INamedTypeSymbol? referenceAttribute = compilation.GetTypeByMetadataName(ReferenceAttributeMetadataName);
            if (referenceAttribute is null)
            {
                return;
            }

            IAssemblySymbol placeholderAssembly = placeholderAttribute.ContainingAssembly;
            INamedTypeSymbol? expressionType = compilation.GetTypeByMetadataName(ExpressionMetadataName);
            INamedTypeSymbol? systemType = compilation.GetTypeByMetadataName(TypeMetadataName);

            var state = new PlaceholderAnalyzerState(
                placeholderAttribute,
                referenceAttribute,
                placeholderAssembly,
                expressionType,
                systemType);

            context.RegisterSymbolAction(
                state.AnalyzeDeclaredSymbol,
                SymbolKind.Event,
                SymbolKind.Field,
                SymbolKind.Method,
                SymbolKind.NamedType,
                SymbolKind.Property);

            context.RegisterOperationAction(state.AnalyzeInvocation, OperationKind.Invocation);
            context.RegisterOperationAction(state.AnalyzeMethodReference, OperationKind.MethodReference);
            context.RegisterOperationAction(state.AnalyzePropertyReference, OperationKind.PropertyReference);
            context.RegisterOperationAction(state.AnalyzeTypeOf, OperationKind.TypeOf);
            context.RegisterOperationAction(state.AnalyzeVariableDeclarator, OperationKind.VariableDeclarator);
            context.RegisterOperationBlockStartAction(state.AnalyzeOperationBlockStart);
        }


        //------------------------------------------------------
        //
        //  Internal Staitc Fields
        //
        //------------------------------------------------------

        #region Internal Staitc Fields

        internal static readonly DiagnosticDescriptor ReferenceEscapeRule = new(
            id: "NEBIL001",
            title: new LocalizableResourceString("ReferenceEscapeTitle", Resources.ResourceManager, typeof(Resources)),
            messageFormat: new LocalizableResourceString("ReferenceEscapeMessage", Resources.ResourceManager, typeof(Resources)),
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString("ReferenceEscapeDescription", Resources.ResourceManager, typeof(Resources)));

        internal static readonly DiagnosticDescriptor PlaceholderTypeRule = new(
            id: "NEBIL002",
            title: new LocalizableResourceString("PlaceholderTypeTitle", Resources.ResourceManager, typeof(Resources)),
            messageFormat: new LocalizableResourceString("PlaceholderTypeMessage", Resources.ResourceManager, typeof(Resources)),
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString("PlaceholderTypeDescription", Resources.ResourceManager, typeof(Resources)));

        internal static readonly DiagnosticDescriptor InvalidContextRule = new(
            id: "NEBIL003",
            title: new LocalizableResourceString("InvalidContextTitle", Resources.ResourceManager, typeof(Resources)),
            messageFormat: new LocalizableResourceString("InvalidContextMessage", Resources.ResourceManager, typeof(Resources)),
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString("InvalidContextDescription", Resources.ResourceManager, typeof(Resources)));

        internal static readonly DiagnosticDescriptor NonConstantOperandRule = new(
            id: "NEBIL004",
            title: new LocalizableResourceString("NonConstantOperandTitle", Resources.ResourceManager, typeof(Resources)),
            messageFormat: new LocalizableResourceString("NonConstantOperandMessage", Resources.ResourceManager, typeof(Resources)),
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString("NonConstantOperandDescription", Resources.ResourceManager, typeof(Resources)));

        internal static readonly DiagnosticDescriptor InvalidOperandValueRule = new(
            id: "NEBIL005",
            title: new LocalizableResourceString("InvalidOperandValueTitle", Resources.ResourceManager, typeof(Resources)),
            messageFormat: new LocalizableResourceString("InvalidOperandValueMessage", Resources.ResourceManager, typeof(Resources)),
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString("InvalidOperandValueDescription", Resources.ResourceManager, typeof(Resources)));

        internal static readonly DiagnosticDescriptor DuplicateLabelRule = new(
            id: "NEBIL006",
            title: new LocalizableResourceString("DuplicateLabelTitle", Resources.ResourceManager, typeof(Resources)),
            messageFormat: new LocalizableResourceString("DuplicateLabelMessage", Resources.ResourceManager, typeof(Resources)),
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString("DuplicateLabelDescription", Resources.ResourceManager, typeof(Resources)));

        internal static readonly DiagnosticDescriptor UndefinedLabelRule = new(
            id: "NEBIL007",
            title: new LocalizableResourceString("UndefinedLabelTitle", Resources.ResourceManager, typeof(Resources)),
            messageFormat: new LocalizableResourceString("UndefinedLabelMessage", Resources.ResourceManager, typeof(Resources)),
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString("UndefinedLabelDescription", Resources.ResourceManager, typeof(Resources)));

        #endregion
    }
}
