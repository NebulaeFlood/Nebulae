using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System;
using System.Collections;
using System.IO;

namespace Nebulae.Runtime.Emit.Inline.MSBuild.Helpers
{
    internal static class DiagnosticHelpers
    {
        const string InstructionOffsetKey = "InstructionOffset";


        public static bool TryGetInstructionOffset(this Exception exception, out int offset)
        {
            if (exception.Data.Contains(InstructionOffsetKey) && exception.Data[InstructionOffsetKey] is int instructionOffset)
            {
                offset = instructionOffset;
                return true;
            }

            offset = -1;
            return false;
        }

        public static bool TryGetSequencePoint(
            this MethodDefinition definition,
            int offset,
            out SequencePoint point)
        {
            Collection<SequencePoint> points = definition.DebugInformation.SequencePoints;
            int last = points.Count - 1;

            for (int i = 0; i < last; i++)
            {
                var lo = points[i];
                var hi = points[i + 1];

                if (lo.Offset <= offset && offset < hi.Offset)
                {
                    point = lo;
                    return true;
                }
            }

            if (points.Count > 0)
            {
                point = points[0];
                return true;
            }

            point = null!;
            return false;
        }

        public static bool TryGetFileInfo<T>(
            this T exception,
            out string file,
            out int startLine,
            out int startColumn,
            out int endLine,
            out int endColumn) where T : Exception
        {
            IDictionary data = exception.Data;

            if (data.Contains(nameof(SequencePoint.Document.Url)) &&
                data.Contains(nameof(SequencePoint.StartLine)) &&
                data.Contains(nameof(SequencePoint.StartColumn)) &&
                data.Contains(nameof(SequencePoint.EndLine)) &&
                data.Contains(nameof(SequencePoint.EndColumn)))
            {
                file = (string)data[nameof(SequencePoint.Document.Url)]!;
                startLine = (int)data[nameof(SequencePoint.StartLine)]!;
                startColumn = (int)data[nameof(SequencePoint.StartColumn)]!;
                endLine = (int)data[nameof(SequencePoint.EndLine)]!;
                endColumn = (int)data[nameof(SequencePoint.EndColumn)]!;
                return true;
            }

            file = null!;
            startLine = -1;
            startColumn = -1;
            endLine = -1;
            endColumn = -1;
            return false;
        }

        public static T With<T>(this T exception, Instruction instruction) where T : Exception
        {
            exception.Data[InstructionOffsetKey] = instruction.Offset;
            return exception;
        }

        public static T With<T>(this T exception, SequencePoint point) where T : Exception
        {
            IDictionary data = exception.Data;

            data[nameof(SequencePoint.Document.Url)] = point.Document.Url;
            data[nameof(SequencePoint.StartLine)] = point.StartLine;
            data[nameof(SequencePoint.StartColumn)] = point.StartColumn;
            data[nameof(SequencePoint.EndLine)] = point.EndLine;
            data[nameof(SequencePoint.EndColumn)] = point.EndColumn;

            return exception;
        }
    }
}
