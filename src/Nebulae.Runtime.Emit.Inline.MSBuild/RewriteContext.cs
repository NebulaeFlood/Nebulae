using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Nebulae.Runtime.Emit.Inline.MSBuild.Helpers;
using System;
using System.Collections.Generic;

namespace Nebulae.Runtime.Emit.Inline.MSBuild
{
    internal readonly struct RewriteContext
    {
        public readonly MethodBody MethodBody;
        public readonly Collection<Instruction> Instructions;
        public readonly ModuleDefinition Module;
        public readonly Collection<VariableDefinition> Variables;


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        public Span<ParameterDefinition> Parameters
        {
            get => _parameters.AsSpan();
        }

        public Span<Placeholder> Placeholers
        {
            get => _placeholders.AsSpan();
        }

        #endregion


        private RewriteContext(
            MethodDefinition method,
            MethodBody body,
            Collection<Instruction> instructions,
            Dictionary<string, Instruction> customLabels,
            Collector<ParameterDefinition> parameters,
            Collector<Placeholder> placeholders,
            Dictionary<Instruction, LabelMap> labelMaps)
        {
            MethodBody = body;
            Instructions = instructions;
            Module = method.Module;
            Variables = body.Variables;

            _customLabels = customLabels;
            _parameters = parameters;
            _placeholders = placeholders;

            _labelMaps = labelMaps;
        }


        public static RewriteContext Create(MethodDefinition definition)
        {
            var body = definition.Body;
            var instructions = body.Instructions;

            var customLabels = new Dictionary<string, Instruction>(StringComparer.Ordinal);
            var labelMaps = new Dictionary<Instruction, LabelMap>();

            var parameters = new Collector<ParameterDefinition>(definition.Parameters.Count + 1);

            if (definition.HasThis)
            {
                parameters.Collect(body.ThisParameter);
            }
            parameters.Collect(definition.Parameters);


            var placeholders = new Collector<Placeholder>(instructions.Count);

            for (int i = 0; i < instructions.Count; i++)
            {
                var instruction = instructions[i];

                if (!Placeholder.IsPlaceholder(instruction, out var placeholer))
                {
                    CollectLabelMapFromCode(labelMaps, instruction);
                    continue;
                }

                if (placeholer.Code is PlaceholderCode.Label)
                {
                    CollectCustomLabel(customLabels, instruction);
                    continue;
                }

                placeholders.Collect(placeholer);
            }


            var handlers = body.ExceptionHandlers;

            for (int i = 0; i < handlers.Count; i++)
            {
                CollectLabelMapFromHandler(labelMaps, handlers[i]);
            }


            return new RewriteContext(
                definition,
                body,
                instructions,
                customLabels,
                parameters,
                placeholders,
                labelMaps);


            static void CollectLabelMapFromCode(Dictionary<Instruction, LabelMap> collector, Instruction instruction)
            {
                var operand = instruction.Operand;

                if (operand is Instruction label)
                {
                    if (!collector.TryGetValue(label, out var map))
                    {
                        map = new();
                        collector[label] = map;
                    }

                    map.Sources.Add(instruction);
                }
                else if (operand is Instruction[] labels)
                {
                    for (int j = 0; j < labels.Length; j++)
                    {
                        label = labels[j];

                        if (!collector.TryGetValue(label, out var map))
                        {
                            map = new();
                            collector[label] = map;
                        }

                        map.Sources.Add(instruction);
                    }
                }
            }

            static void CollectCustomLabel(Dictionary<string, Instruction> collector, Instruction placeholder)
            {
                const string ArgumentName = "label name";

                var instruction = placeholder.AcquirePrevious(placeholder, ArgumentName);

                if (instruction.OpCode.Code is not Code.Ldstr)
                {
                    throw new InvalidProgramException($"Cannot resolve target {ArgumentName}, the instruction sequence is incompatible.")
                        .With(nameof(Instruction), placeholder);
                }

                var label = (string)instruction.Operand;
                instruction.Consume();

                if (label.Length is 0)
                {
                    throw new InvalidProgramException($"Label name cannot be empty.")
                        .With(nameof(Instruction), placeholder);
                }

                if (collector.ContainsKey(label))
                {
                    throw new InvalidProgramException($"Duplicate label '{label}' defined.")
                        .With(nameof(Instruction), placeholder);
                }

                placeholder.Consume();
                collector[label] = placeholder;
            }

            static void CollectLabelMapFromHandler(Dictionary<Instruction, LabelMap> collector, ExceptionHandler handler)
            {
                if (handler.TryStart is not null)
                {
                    if (!collector.TryGetValue(handler.TryStart, out var map))
                    {
                        map = new();
                        collector[handler.TryStart] = map;
                    }

                    map.Handlers.Add(handler);
                }

                if (handler.TryEnd is not null)
                {
                    if (!collector.TryGetValue(handler.TryEnd, out var map))
                    {
                        map = new();
                        collector[handler.TryEnd] = map;
                    }

                    map.Handlers.Add(handler);
                }

                if (handler.FilterStart is not null)
                {
                    if (!collector.TryGetValue(handler.FilterStart, out var map))
                    {
                        map = new();
                        collector[handler.FilterStart] = map;
                    }

                    map.Handlers.Add(handler);
                }

                if (handler.HandlerStart is not null)
                {
                    if (!collector.TryGetValue(handler.HandlerStart, out var map))
                    {
                        map = new();
                        collector[handler.HandlerStart] = map;
                    }

                    map.Handlers.Add(handler);
                }

                if (handler.HandlerEnd is not null)
                {
                    if (!collector.TryGetValue(handler.HandlerEnd, out var map))
                    {
                        map = new();
                        collector[handler.HandlerEnd] = map;
                    }

                    map.Handlers.Add(handler);
                }
            }
        }


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        public Instruction GetLabel(Instruction source, string label)
        {
            if (!_customLabels.TryGetValue(label, out var target))
            {
                throw new InvalidProgramException($"Label '{label}' is not defined.")
                    .With(nameof(Instruction), source);
            }

            if (!_labelMaps.TryGetValue(target, out var map))
            {
                map = new();
                _labelMaps[target] = map;
            }

            map.Sources.Add(source);
            return target;
        }

        public void Remove(Instruction instruction)
        {
            if (!_labelMaps.TryGetValue(instruction, out var map))
            {
                return;
            }

            foreach (var source in map.Sources)
            {
                if (source.Operand is Instruction)
                {
                    source.Operand = instruction.Next
                        ?? instruction.Previous
                        ?? throw new InvalidProgramException("Cannot operate on a empty method body.");
                }
                else if (source.Operand is Instruction[] labels)
                {
                    for (int i = 0; i < labels.Length; i++)
                    {
                        if (labels[i] == instruction)
                        {
                            labels[i] = instruction.Next
                                ?? instruction.Previous
                                ?? throw new InvalidProgramException("Cannot operate on a empty method body.");
                        }
                    }
                }
            }

            foreach (var handler in map.Handlers)
            {
                if (handler.TryStart == instruction)
                {
                    handler.TryStart = instruction.Next
                        ?? instruction.Previous
                        ?? throw new InvalidProgramException("Cannot operate on a empty method body.");
                }

                if (handler.TryEnd == instruction)
                {
                    handler.TryEnd = instruction.Next
                        ?? instruction.Previous
                        ?? throw new InvalidProgramException("Cannot operate on a empty method body.");
                }

                if (handler.FilterStart == instruction)
                {
                    handler.FilterStart = instruction.Next
                        ?? instruction.Previous
                        ?? throw new InvalidProgramException("Cannot operate on a empty method body.");
                }

                if (handler.HandlerStart == instruction)
                {
                    handler.HandlerStart = instruction.Next
                        ?? instruction.Previous
                        ?? throw new InvalidProgramException("Cannot operate on a empty method body.");
                }

                if (handler.HandlerEnd == instruction)
                {
                    handler.HandlerEnd = instruction.Next
                        ?? instruction.Previous
                        ?? throw new InvalidProgramException("Cannot operate on a empty method body.");
                }
            }
        }

        #endregion


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private readonly Dictionary<string, Instruction> _customLabels;
        private readonly Collector<ParameterDefinition> _parameters;
        private readonly Collector<Placeholder> _placeholders;

        private readonly Dictionary<Instruction, LabelMap> _labelMaps;

        #endregion


        private readonly struct LabelMap
        {
            public readonly HashSet<Instruction> Sources;
            public readonly HashSet<ExceptionHandler> Handlers;


            public LabelMap()
            {
                Sources = new();
                Handlers = new();
            }
        }
    }
}
