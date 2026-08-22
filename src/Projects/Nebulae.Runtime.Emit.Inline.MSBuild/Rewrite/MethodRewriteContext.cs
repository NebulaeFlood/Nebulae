using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Nebulae.Collections;
using Nebulae.Runtime.Emit.Inline.MSBuild.Helpers;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Nebulae.Runtime.Emit.Inline.MSBuild.Rewrite
{
    internal readonly ref struct MethodRewriteContext
    {
        //------------------------------------------------------
        //
        //  Public Fields
        //
        //------------------------------------------------------

        #region Public Fields

        public readonly MethodBody MethodBody;
        public readonly Collection<Instruction> Instructions;
        public readonly ModuleDefinition Module;
        public readonly Collection<VariableDefinition> Variables;

        #endregion


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

        public Span<Placeholder> Placeholders
        {
            get => _placeholders.AsSpan();
        }

        #endregion


        private MethodRewriteContext(
            MethodDefinition method,
            MethodBody body,
            Collection<Instruction> instructions,
            Dictionary<string, Instruction> customLabels,
            ValueCollector<ParameterDefinition> parameters,
            ValueCollector<Placeholder> placeholders,
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


        public static MethodRewriteContext Create(MethodDefinition definition)
        {
            var body = definition.Body;
            var instructions = body.Instructions;

            var customLabels = new Dictionary<string, Instruction>(StringComparer.Ordinal);
            var labelMaps = new Dictionary<Instruction, LabelMap>();


            // Collect parameters,
            // including the implicit 'this' parameter.
            var parameters = new ValueCollector<ParameterDefinition>((uint)definition.Parameters.Count + 1);

            if (definition.HasThis)
            {
                parameters.Collect(body.ThisParameter);
            }

            parameters.CollectRange(definition.Parameters);


            var placeholders = new ValueCollector<Placeholder>((uint)instructions.Count);

            for (int i = 0; i < instructions.Count; i++)
            {
                var instruction = instructions[i];

                if (!Placeholder.IsPlaceholder(instruction, out var placeholder))
                {
                    CollectLabelMapFromCode(labelMaps, instruction);
                    continue;
                }

                if (placeholder.Code is PlaceholderCode.Label)
                {
                    CollectCustomLabel(customLabels, instruction);
                    continue;
                }

                placeholders.Collect(placeholder);
            }


            var handlers = body.ExceptionHandlers;

            for (int i = 0; i < handlers.Count; i++)
            {
                CollectLabelMapFromHandler(labelMaps, handlers[i]);
            }


            return new MethodRewriteContext(
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

            static void CollectCustomLabel(Dictionary<string, Instruction> collector, Instruction placeholder)
            {
                const string ArgumentName = "label name";

                var instruction = placeholder.AcquirePrevious(ArgumentName);

                if (instruction.OpCode.Code is not Code.Ldstr)
                {
                    throw new InvalidProgramException($"Cannot resolve target {ArgumentName}, the instruction sequence is incompatible.")
                        .With(placeholder);
                }

                var label = (string)instruction.Operand;
                instruction.Elide();

                if (label.Length is 0)
                {
                    throw new InvalidProgramException($"Label name cannot be empty.")
                        .With(placeholder);
                }

                if (collector.ContainsKey(label))
                {
                    throw new InvalidProgramException($"Duplicate label '{label}' defined.")
                        .With(placeholder);
                }

                placeholder.Elide();
                collector[label] = placeholder;
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
                    .With(source);
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

            // We remove instructions from back to front,
            // so the next instruction is always not elided
            // and does not need to update _labelMaps.
            Instruction? replacement = instruction.Next;
            HashSet <Instruction> sources = map.Sources;

            if (sources.Count is not 0)
            {
                if (replacement is null)
                {
                    throw new InvalidProgramException(
                        $"Cannot retarget references to the instruction " +
                        $"at offset '{instruction.Offset:X4}' " +
                        $"because it is the end of method.")
                        .With(instruction);
                }

                foreach (var source in sources)
                {
                    if (source.Operand is Instruction)
                    {
                        source.Operand = replacement;
                    }
                    else if (source.Operand is Instruction[] labels)
                    {
                        for (int i = 0; i < labels.Length; i++)
                        {
                            if (labels[i] == instruction)
                            {
                                labels[i] = replacement;
                            }
                        }
                    }
                }
            }


            foreach (var handler in map.Handlers)
            {
                if (handler.TryStart == instruction)
                {
                    if (handler.TryEnd == replacement)
                    {
                        throw new InvalidProgramException(
                            $"Cannot remove the only instruction " +
                            $"at offset '{instruction.Offset:X4}' " +
                            $"in a try region of exception handler.")
                            .With(instruction);
                    }

                    if (replacement is null)
                    {
                        throw new InvalidProgramException(
                            $"Cannot retarget references to the instruction " +
                            $"at offset '{instruction.Offset:X4}' " +
                            $"because it is the end of method.")
                            .With(instruction);
                    }

                    handler.TryStart = replacement;
                }
                else if (handler.TryEnd == instruction)
                {
                    handler.TryEnd = replacement;
                }

                if (handler.FilterStart == instruction)
                {
                    if (handler.HandlerStart == replacement)
                    {
                        throw new InvalidProgramException(
                            $"Cannot remove the only instruction " +
                            $"at offset '{instruction.Offset:X4}' " +
                            $"in a filter region of exception handler.")
                            .With(instruction);
                    }

                    if (replacement is null)
                    {
                        throw new InvalidProgramException(
                            $"Cannot retarget references to the instruction " +
                            $"at offset '{instruction.Offset:X4}' " +
                            $"because it is the end of method.")
                            .With(instruction);
                    }

                    handler.FilterStart = replacement;
                }

                if (handler.HandlerStart == instruction)
                {
                    if (handler.HandlerEnd == replacement)
                    {
                        throw new InvalidProgramException(
                            $"Cannot remove the only instruction " +
                            $"at offset '{instruction.Offset:X4}' " +
                            $"in a handler region of exception handler.")
                            .With(instruction);
                    }

                    if (replacement is null)
                    {
                        throw new InvalidProgramException(
                            $"Cannot retarget references to the instruction " +
                            $"at offset '{instruction.Offset:X4}' " +
                            $"because it is the end of method.")
                            .With(instruction);
                    }

                    handler.HandlerStart = replacement;
                }
                else if (handler.HandlerEnd == instruction)
                {
                    handler.HandlerEnd = replacement;
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

        private readonly ValueCollector<ParameterDefinition> _parameters;

        private readonly ValueCollector<Placeholder> _placeholders;

        private readonly Dictionary<Instruction, LabelMap> _labelMaps;

        #endregion


        private readonly struct LabelMap
        {
            public readonly HashSet<Instruction> Sources;
            public readonly HashSet<ExceptionHandler> Handlers;


            public LabelMap()
            {
                Sources = [];
                Handlers = [];
            }
        }
    }
}
