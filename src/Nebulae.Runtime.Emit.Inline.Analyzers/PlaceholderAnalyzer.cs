using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Nebulae.Runtime.Emit.Inline.Analyzers.Properties;
using System.Collections.Immutable;

namespace Nebulae.Runtime.Emit.Inline.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class PlaceholderAnalyzer : DiagnosticAnalyzer
    {
        private const string PlaceholderAttributeMetadataName = "Nebulae.Runtime.Emit.Inline.PlaceholderAttribute";
        private const string ReferenceAttributeMetadataName = "Nebulae.Runtime.Emit.Inline.ReferenceAttribute";
        private const string ExpressionMetadataName = "System.Linq.Expressions.Expression`1";
        private const string TypeMetadataName = "System.Type";


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get => ImmutableArray.Create(
                PlaceholderTypeRule,
                InvalidPlaceholderUsageRule,
                InvalidInstructionUsageRule,
                InvalidExtendedInstructionUsageRule,
                InvalidPlaceholderReferenceExpressionUsageRule,
                RepeatedGenericMethodConstructionRule,
                NonConstantOperandRule,
                InvalidConstantValueRule,
                InvalidVariableOperandRule,
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

            INamedTypeSymbol? placeholderAttribute = compilation
                .GetTypeByMetadataName(PlaceholderAttributeMetadataName);

            if (placeholderAttribute is null)
            {
                return;
            }

            if (SymbolEqualityComparer.Default.Equals(compilation.Assembly, placeholderAttribute.ContainingAssembly))
            {
                return;
            }

            INamedTypeSymbol? referenceAttribute = compilation
                .GetTypeByMetadataName(ReferenceAttributeMetadataName);

            if (referenceAttribute is null)
            {
                return;
            }

            if (!SymbolEqualityComparer.Default.Equals(placeholderAttribute.ContainingAssembly, referenceAttribute.ContainingAssembly))
            {
                return;
            }

            INamedTypeSymbol? expressionType = compilation.GetTypeByMetadataName(ExpressionMetadataName);
            INamedTypeSymbol? systemType = compilation.GetTypeByMetadataName(TypeMetadataName);

            var state = new PlaceholderAnalyzerState(
                placeholderAttribute,
                referenceAttribute,
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
            context.RegisterOperationAction(state.AnalyzeInstructionMethodReference, OperationKind.MethodReference);
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

        internal static readonly DiagnosticDescriptor PlaceholderTypeRule = new(
            id: "NEBIL1001",
            title: new LocalizableResourceString("PlaceholderTypeTitle", Resources.ResourceManager, typeof(Resources)),
            messageFormat: new LocalizableResourceString("PlaceholderTypeMessage", Resources.ResourceManager, typeof(Resources)),
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString("PlaceholderTypeDescription", Resources.ResourceManager, typeof(Resources)));

        internal static readonly DiagnosticDescriptor InvalidPlaceholderUsageRule = new(
            id: "NEBIL1002",
            title: new LocalizableResourceString("InvalidPlaceholderUsageTitle", Resources.ResourceManager, typeof(Resources)),
            messageFormat: new LocalizableResourceString("InvalidPlaceholderUsageMessage", Resources.ResourceManager, typeof(Resources)),
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString("InvalidPlaceholderUsageDescription", Resources.ResourceManager, typeof(Resources)));

        internal static readonly DiagnosticDescriptor InvalidInstructionUsageRule = new(
            id: "NEBIL2001",
            title: new LocalizableResourceString("InvalidInstructionUsageTitle", Resources.ResourceManager, typeof(Resources)),
            messageFormat: new LocalizableResourceString("InvalidInstructionUsageMessage", Resources.ResourceManager, typeof(Resources)),
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString("InvalidInstructionUsageDescription", Resources.ResourceManager, typeof(Resources)));

        internal static readonly DiagnosticDescriptor InvalidExtendedInstructionUsageRule = new(
            id: "NEBIL2002",
            title: new LocalizableResourceString("InvalidExtendedInstructionUsageTitle", Resources.ResourceManager, typeof(Resources)),
            messageFormat: new LocalizableResourceString("InvalidExtendedInstructionUsageMessage", Resources.ResourceManager, typeof(Resources)),
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString("InvalidExtendedInstructionUsageDescription", Resources.ResourceManager, typeof(Resources)));

        internal static readonly DiagnosticDescriptor InvalidPlaceholderReferenceExpressionUsageRule = new(
            id: "NEBIL3001",
            title: new LocalizableResourceString("InvalidPlaceholderReferenceExpressionUsageTitle", Resources.ResourceManager, typeof(Resources)),
            messageFormat: new LocalizableResourceString("InvalidPlaceholderReferenceExpressionUsageMessage", Resources.ResourceManager, typeof(Resources)),
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString("InvalidPlaceholderReferenceExpressionUsageDescription", Resources.ResourceManager, typeof(Resources)));

        internal static readonly DiagnosticDescriptor RepeatedGenericMethodConstructionRule = new(
            id: "NEBIL3002",
            title: new LocalizableResourceString("RepeatedGenericMethodConstructionTitle", Resources.ResourceManager, typeof(Resources)),
            messageFormat: new LocalizableResourceString("RepeatedGenericMethodConstructionMessage", Resources.ResourceManager, typeof(Resources)),
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString("RepeatedGenericMethodConstructionDescription", Resources.ResourceManager, typeof(Resources)));

        internal static readonly DiagnosticDescriptor NonConstantOperandRule = new(
            id: "NEBIL4001",
            title: new LocalizableResourceString("NonConstantOperandTitle", Resources.ResourceManager, typeof(Resources)),
            messageFormat: new LocalizableResourceString("NonConstantOperandMessage", Resources.ResourceManager, typeof(Resources)),
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString("NonConstantOperandDescription", Resources.ResourceManager, typeof(Resources)));

        internal static readonly DiagnosticDescriptor InvalidConstantValueRule = new(
            id: "NEBIL4002",
            title: new LocalizableResourceString("InvalidConstantValueTitle", Resources.ResourceManager, typeof(Resources)),
            messageFormat: new LocalizableResourceString("InvalidConstantValueMessage", Resources.ResourceManager, typeof(Resources)),
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString("InvalidConstantValueDescription", Resources.ResourceManager, typeof(Resources)));

        internal static readonly DiagnosticDescriptor InvalidVariableOperandRule = new(
            id: "NEBIL4003",
            title: new LocalizableResourceString("InvalidVariableOperandTitle", Resources.ResourceManager, typeof(Resources)),
            messageFormat: new LocalizableResourceString("InvalidVariableOperandMessage", Resources.ResourceManager, typeof(Resources)),
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString("InvalidVariableOperandDescription", Resources.ResourceManager, typeof(Resources)));

        internal static readonly DiagnosticDescriptor DuplicateLabelRule = new(
            id: "NEBIL5001",
            title: new LocalizableResourceString("DuplicateLabelTitle", Resources.ResourceManager, typeof(Resources)),
            messageFormat: new LocalizableResourceString("DuplicateLabelMessage", Resources.ResourceManager, typeof(Resources)),
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString("DuplicateLabelDescription", Resources.ResourceManager, typeof(Resources)));

        internal static readonly DiagnosticDescriptor UndefinedLabelRule = new(
            id: "NEBIL5002",
            title: new LocalizableResourceString("UndefinedLabelTitle", Resources.ResourceManager, typeof(Resources)),
            messageFormat: new LocalizableResourceString("UndefinedLabelMessage", Resources.ResourceManager, typeof(Resources)),
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: new LocalizableResourceString("UndefinedLabelDescription", Resources.ResourceManager, typeof(Resources)));

        #endregion
    }
}
