using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

public static class DynamicMethodIL
{
    private const BindingFlags InstanceFlags =
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.NonPublic;

    private static readonly Dictionary<ushort, OpCode> OpCodeMap =
        BuildOpCodeMap();

    /// <summary>
    /// 将 .NET 10 CoreCLR DynamicMethod 反汇编为可读 IL。
    /// </summary>
    public static string Disassemble(DynamicMethod method)
    {
        ArgumentNullException.ThrowIfNull(method);

        RuntimeData data = GetRuntimeData(method);

        byte[] il = data.Code;
        IList tokens = data.Tokens;

        var sb = new StringBuilder();

        sb.AppendLine(
            $"// DynamicMethod: {FormatMethod(method)}");
        sb.AppendLine($"// IL size: {il.Length} bytes");
        sb.AppendLine();

        int p = 0;

        while (p < il.Length)
        {
            int offset = p;

            OpCode opcode = ReadOpCode(il, ref p);

            string operand;

            try
            {
                operand = ReadOperand(
                    opcode,
                    il,
                    ref p,
                    tokens);
            }
            catch (Exception ex)
            {
                operand =
                    $"/* decode error: {ex.GetType().Name}: {ex.Message} */";

                // 避免 malformed IL 导致死循环
                if (p <= offset)
                    p = offset + 1;
            }

            sb.Append("IL_")
              .Append(offset.ToString("X4"))
              .Append(":  ")
              .Append(opcode.Name);

            if (!string.IsNullOrEmpty(operand))
            {
                int padding = Math.Max(
                    1,
                    14 - (opcode.Name?.Length ?? 0));

                sb.Append(' ', padding);
                sb.Append(operand);
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    // ============================================================
    // DynamicMethod internals
    // ============================================================

    private static RuntimeData GetRuntimeData(DynamicMethod method)
    {
        object? resolver =
            GetFieldValue(method, "_resolver");

        if (resolver == null)
        {
            //
            // 强制 DynamicILGenerator.BakeByteArray()
            // 并创建 DynamicResolver。
            //
            MethodInfo? getDescriptor =
                typeof(DynamicMethod).GetMethod(
                    "GetMethodDescriptor",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            if (getDescriptor == null)
            {
                throw new PlatformNotSupportedException(
                    "找不到 DynamicMethod.GetMethodDescriptor(). " +
                    "当前实现只针对 .NET 10 CoreCLR。");
            }

            try
            {
                _ = getDescriptor.Invoke(method, null);
            }
            catch (TargetInvocationException e)
                when (e.InnerException != null)
            {
                throw new InvalidOperationException(
                    "无法 materialize DynamicMethod。",
                    e.InnerException);
            }

            resolver =
                GetFieldValue(method, "_resolver");
        }

        if (resolver == null)
        {
            throw new InvalidOperationException(
                "DynamicMethod 没有创建 DynamicResolver。");
        }

        object? codeObject =
            GetFieldValue(resolver, "m_code");

        object? scope =
            GetFieldValue(resolver, "m_scope");

        if (codeObject is not byte[] code)
        {
            throw new PlatformNotSupportedException(
                "DynamicResolver.m_code 不存在或类型发生变化。");
        }

        if (scope == null)
        {
            throw new PlatformNotSupportedException(
                "DynamicResolver.m_scope 不存在。");
        }

        object? tokenList =
            GetFieldValue(scope, "m_tokens");

        if (tokenList is not IList tokens)
        {
            throw new PlatformNotSupportedException(
                "DynamicScope.m_tokens 不存在或类型发生变化。");
        }

        return new RuntimeData(
            (byte[])code.Clone(),
            tokens);
    }

    private sealed record RuntimeData(
        byte[] Code,
        IList Tokens);

    // ============================================================
    // Opcode decoding
    // ============================================================

    private static Dictionary<ushort, OpCode> BuildOpCodeMap()
    {
        var result = new Dictionary<ushort, OpCode>();

        foreach (FieldInfo field in typeof(OpCodes).GetFields(
                     BindingFlags.Public |
                     BindingFlags.Static))
        {
            if (field.FieldType != typeof(OpCode))
                continue;

            var opcode = (OpCode)field.GetValue(null)!;

            ushort value =
                unchecked((ushort)opcode.Value);

            result[value] = opcode;
        }

        return result;
    }

    private static OpCode ReadOpCode(
        byte[] il,
        ref int p)
    {
        byte first = ReadByte(il, ref p);

        ushort value;

        if (first == 0xFE)
        {
            byte second = ReadByte(il, ref p);

            value = (ushort)(0xFE00 | second);
        }
        else
        {
            value = first;
        }

        if (!OpCodeMap.TryGetValue(value, out OpCode opcode))
        {
            throw new BadImageFormatException(
                $"未知 IL opcode: 0x{value:X4}");
        }

        return opcode;
    }

    private static string ReadOperand(
        OpCode opcode,
        byte[] il,
        ref int p,
        IList tokens)
    {
        switch (opcode.OperandType)
        {
            case OperandType.InlineNone:
                return "";

            // ----------------------------------------------------
            // Integer
            // ----------------------------------------------------

            case OperandType.ShortInlineI:
                {
                    sbyte value =
                        unchecked((sbyte)ReadByte(il, ref p));

                    return value.ToString(
                        CultureInfo.InvariantCulture);
                }

            case OperandType.InlineI:
                {
                    int value = ReadInt32(il, ref p);

                    return value.ToString(
                        CultureInfo.InvariantCulture);
                }

            case OperandType.InlineI8:
                {
                    long value = ReadInt64(il, ref p);

                    return value.ToString(
                        CultureInfo.InvariantCulture);
                }

            // ----------------------------------------------------
            // Float
            // ----------------------------------------------------

            case OperandType.ShortInlineR:
                {
                    int bits = ReadInt32(il, ref p);

                    float value =
                        BitConverter.Int32BitsToSingle(bits);

                    return value.ToString(
                        "R",
                        CultureInfo.InvariantCulture);
                }

            case OperandType.InlineR:
                {
                    long bits = ReadInt64(il, ref p);

                    double value =
                        BitConverter.Int64BitsToDouble(bits);

                    return value.ToString(
                        "R",
                        CultureInfo.InvariantCulture);
                }

            // ----------------------------------------------------
            // Local / argument index
            // ----------------------------------------------------

            case OperandType.ShortInlineVar:
                {
                    byte index = ReadByte(il, ref p);

                    return index.ToString(
                        CultureInfo.InvariantCulture);
                }

            case OperandType.InlineVar:
                {
                    ushort index = ReadUInt16(il, ref p);

                    return index.ToString(
                        CultureInfo.InvariantCulture);
                }

            // ----------------------------------------------------
            // Branch
            // ----------------------------------------------------

            case OperandType.ShortInlineBrTarget:
                {
                    sbyte delta =
                        unchecked((sbyte)ReadByte(il, ref p));

                    int target = p + delta;

                    return FormatLabel(target);
                }

            case OperandType.InlineBrTarget:
                {
                    int delta = ReadInt32(il, ref p);

                    int target = p + delta;

                    return FormatLabel(target);
                }

            // ----------------------------------------------------
            // switch
            // ----------------------------------------------------

            case OperandType.InlineSwitch:
                {
                    int count = ReadInt32(il, ref p);

                    if (count < 0)
                    {
                        throw new BadImageFormatException(
                            "switch target count < 0");
                    }

                    if (count >
                        (il.Length - p) / sizeof(int))
                    {
                        throw new BadImageFormatException(
                            "switch operand 超出 IL 流。");
                    }

                    var deltas = new int[count];

                    for (int i = 0; i < count; i++)
                        deltas[i] = ReadInt32(il, ref p);

                    //
                    // switch 的 branch offset 全部相对于
                    // 整个 switch operand 结束位置。
                    //
                    int baseOffset = p;

                    return "(" +
                        string.Join(
                            ", ",
                            deltas.Select(
                                x => FormatLabel(
                                    baseOffset + x))) +
                        ")";
                }

            // ----------------------------------------------------
            // DynamicScope tokens
            // ----------------------------------------------------

            case OperandType.InlineString:
                {
                    int token = ReadInt32(il, ref p);

                    object? obj =
                        GetTokenObject(tokens, token);

                    if (obj is string str)
                        return Quote(str);

                    return FormatUnknownToken(
                        token,
                        obj);
                }

            case OperandType.InlineType:
                {
                    int token = ReadInt32(il, ref p);

                    return FormatDynamicToken(
                        tokens,
                        token);
                }

            case OperandType.InlineField:
                {
                    int token = ReadInt32(il, ref p);

                    return FormatDynamicToken(
                        tokens,
                        token);
                }

            case OperandType.InlineMethod:
                {
                    int token = ReadInt32(il, ref p);

                    return FormatDynamicToken(
                        tokens,
                        token);
                }

            case OperandType.InlineTok:
                {
                    int token = ReadInt32(il, ref p);

                    return FormatDynamicToken(
                        tokens,
                        token);
                }

            case OperandType.InlineSig:
                {
                    int token = ReadInt32(il, ref p);

                    object? obj =
                        GetTokenObject(tokens, token);

                    if (obj is byte[] signature)
                    {
                        return "signature [" +
                               Convert.ToHexString(signature) +
                               "]";
                    }

                    //
                    // EmitCall vararg 时这里也可能涉及
                    // VarArgMethod。
                    //
                    return FormatDynamicToken(
                        tokens,
                        token);
                }

            default:
                throw new NotSupportedException(
                    $"不支持 OperandType: {opcode.OperandType}");
        }
    }

    // ============================================================
    // DynamicScope token resolution
    // ============================================================

    private static object? GetTokenObject(
        IList tokens,
        int token)
    {
        //
        // DynamicScope 在 .NET 10 中也是这么取 index：
        //
        // token &= 0x00FFFFFF
        //
        int index =
            token & 0x00FFFFFF;

        if ((uint)index >= (uint)tokens.Count)
            return null;

        return tokens[index];
    }

    private static string FormatDynamicToken(
        IList tokens,
        int token)
    {
        object? obj =
            GetTokenObject(tokens, token);

        if (obj == null)
            return $"0x{token:X8} /* unresolved */";

        try
        {
            switch (obj)
            {
                case string str:
                    return Quote(str);

                case Type type:
                    return FormatType(type);

                case DynamicMethod dm:
                    return FormatMethod(dm);

                case MethodBase method:
                    return FormatMethod(method);

                case FieldInfo field:
                    return FormatField(field);

                case RuntimeTypeHandle typeHandle:
                    return FormatType(
                        Type.GetTypeFromHandle(typeHandle)!);

                case RuntimeMethodHandle methodHandle:
                    {
                        MethodBase? method =
                            MethodBase.GetMethodFromHandle(
                                methodHandle);

                        return method != null
                            ? FormatMethod(method)
                            : $"0x{token:X8}";
                    }

                case RuntimeFieldHandle fieldHandle:
                    {
                        FieldInfo? field =
                            FieldInfo.GetFieldFromHandle(
                                fieldHandle);

                        return field != null
                            ? FormatField(field)
                            : $"0x{token:X8}";
                    }

                case byte[] signature:
                    return "signature [" +
                           Convert.ToHexString(signature) +
                           "]";
            }

            //
            // 以下类型是 System.Private.CoreLib 内部类型，
            // C# 无法直接引用，所以按 runtime 类型名处理。
            //
            string typeName =
                obj.GetType().Name;

            return typeName switch
            {
                "GenericMethodInfo" =>
                    FormatGenericMethodInfo(obj, token),

                "GenericFieldInfo" =>
                    FormatGenericFieldInfo(obj, token),

                "VarArgMethod" =>
                    FormatVarArgMethod(obj, token),

                _ =>
                    FormatUnknownToken(token, obj)
            };
        }
        catch
        {
            return FormatUnknownToken(
                token,
                obj);
        }
    }

    private static string FormatGenericMethodInfo(
        object obj,
        int token)
    {
        object? mhObject =
            GetFieldValue(obj, "m_methodHandle");

        object? contextObject =
            GetFieldValue(obj, "m_context");

        if (mhObject is not RuntimeMethodHandle methodHandle ||
            contextObject is not RuntimeTypeHandle typeHandle)
        {
            return FormatUnknownToken(token, obj);
        }

        MethodBase? method =
            MethodBase.GetMethodFromHandle(
                methodHandle,
                typeHandle);

        return method != null
            ? FormatMethod(method)
            : FormatUnknownToken(token, obj);
    }

    private static string FormatGenericFieldInfo(
        object obj,
        int token)
    {
        object? fhObject =
            GetFieldValue(obj, "m_fieldHandle");

        object? contextObject =
            GetFieldValue(obj, "m_context");

        if (fhObject is not RuntimeFieldHandle fieldHandle ||
            contextObject is not RuntimeTypeHandle typeHandle)
        {
            return FormatUnknownToken(token, obj);
        }

        FieldInfo? field =
            FieldInfo.GetFieldFromHandle(
                fieldHandle,
                typeHandle);

        return field != null
            ? FormatField(field)
            : FormatUnknownToken(token, obj);
    }

    private static string FormatVarArgMethod(
        object obj,
        int token)
    {
        object? dm =
            GetFieldValue(obj, "m_dynamicMethod");

        if (dm is DynamicMethod dynamicMethod)
        {
            return FormatMethod(dynamicMethod)
                   + " /* vararg */";
        }

        object? method =
            GetFieldValue(obj, "m_method");

        if (method is MethodBase mb)
        {
            return FormatMethod(mb)
                   + " /* vararg */";
        }

        return FormatUnknownToken(
            token,
            obj);
    }

    // ============================================================
    // Formatting
    // ============================================================

    private static string FormatMethod(
        MethodBase method)
    {
        string instance =
            method.IsStatic ? "" : "instance ";

        string returnType =
            method is MethodInfo mi
                ? FormatType(mi.ReturnType)
                : "void";

        string owner;

        if (method is DynamicMethod)
        {
            owner = "<DynamicMethod>";
        }
        else
        {
            owner = method.DeclaringType != null
                ? FormatType(method.DeclaringType)
                : "<global>";
        }

        string name;

        if (method is ConstructorInfo constructor)
        {
            name = constructor.IsStatic
                ? ".cctor"
                : ".ctor";
        }
        else
        {
            name = method.Name;
        }

        if (method.IsGenericMethod)
        {
            Type[] args =
                method.GetGenericArguments();

            name += "<" +
                    string.Join(
                        ", ",
                        args.Select(FormatType)) +
                    ">";
        }

        string parameters =
            string.Join(
                ", ",
                method.GetParameters()
                    .Select(
                        x => FormatType(
                            x.ParameterType)));

        return instance +
               returnType +
               " " +
               owner +
               "::" +
               name +
               "(" +
               parameters +
               ")";
    }

    private static string FormatField(
        FieldInfo field)
    {
        string owner =
            field.DeclaringType != null
                ? FormatType(field.DeclaringType)
                : "<global>";

        return FormatType(field.FieldType) +
               " " +
               owner +
               "::" +
               field.Name;
    }

    private static string FormatType(Type type)
    {
        if (type == typeof(void))
            return "void";

        if (type == typeof(bool))
            return "bool";

        if (type == typeof(char))
            return "char";

        if (type == typeof(sbyte))
            return "int8";

        if (type == typeof(byte))
            return "unsigned int8";

        if (type == typeof(short))
            return "int16";

        if (type == typeof(ushort))
            return "unsigned int16";

        if (type == typeof(int))
            return "int32";

        if (type == typeof(uint))
            return "unsigned int32";

        if (type == typeof(long))
            return "int64";

        if (type == typeof(ulong))
            return "unsigned int64";

        if (type == typeof(float))
            return "float32";

        if (type == typeof(double))
            return "float64";

        if (type == typeof(string))
            return "string";

        if (type == typeof(object))
            return "object";

        if (type == typeof(IntPtr))
            return "native int";

        if (type == typeof(UIntPtr))
            return "native unsigned int";

        if (type.IsByRef)
        {
            return FormatType(
                       type.GetElementType()!) +
                   "&";
        }

        if (type.IsPointer)
        {
            return FormatType(
                       type.GetElementType()!) +
                   "*";
        }

        if (type.IsArray)
        {
            Type element =
                type.GetElementType()!;

            int rank = type.GetArrayRank();

            if (rank == 1)
                return FormatType(element) + "[]";

            return FormatType(element) +
                   "[" +
                   new string(',', rank - 1) +
                   "]";
        }

        if (type.IsGenericParameter)
        {
            return
                (type.DeclaringMethod != null
                    ? "!!"
                    : "!") +
                type.GenericParameterPosition;
        }

        if (type.IsGenericType)
        {
            Type definition =
                type.GetGenericTypeDefinition();

            string name =
                definition.FullName ??
                definition.Name;

            int tick =
                name.IndexOf('`');

            if (tick >= 0)
                name = name[..tick];

            Type[] args =
                type.GetGenericArguments();

            return name +
                   "<" +
                   string.Join(
                       ", ",
                       args.Select(FormatType)) +
                   ">";
        }

        return type.FullName ??
               type.Name;
    }

    private static string FormatLabel(
        int offset)
    {
        if (offset < 0)
            return $"IL_{offset:X8}";

        return "IL_" +
               offset.ToString("X4");
    }

    private static string FormatUnknownToken(
        int token,
        object? obj)
    {
        string type =
            obj?.GetType().FullName ??
            "null";

        return $"0x{token:X8} /* {type} */";
    }

    private static string Quote(string value)
    {
        var sb =
            new StringBuilder(
                value.Length + 2);

        sb.Append('"');

        foreach (char ch in value)
        {
            switch (ch)
            {
                case '\\':
                    sb.Append(@"\\");
                    break;

                case '"':
                    sb.Append("\\\"");
                    break;

                case '\r':
                    sb.Append(@"\r");
                    break;

                case '\n':
                    sb.Append(@"\n");
                    break;

                case '\t':
                    sb.Append(@"\t");
                    break;

                case '\0':
                    sb.Append(@"\0");
                    break;

                default:
                    if (char.IsControl(ch))
                    {
                        sb.Append("\\u");
                        sb.Append(
                            ((int)ch).ToString("X4"));
                    }
                    else
                    {
                        sb.Append(ch);
                    }

                    break;
            }
        }

        sb.Append('"');

        return sb.ToString();
    }

    // ============================================================
    // Reflection
    // ============================================================

    private static object? GetFieldValue(
        object instance,
        string fieldName)
    {
        Type? type =
            instance.GetType();

        while (type != null)
        {
            FieldInfo? field =
                type.GetField(
                    fieldName,
                    InstanceFlags |
                    BindingFlags.DeclaredOnly);

            if (field != null)
                return field.GetValue(instance);

            type = type.BaseType;
        }

        return null;
    }

    // ============================================================
    // IL binary reader
    // ============================================================

    private static byte ReadByte(
        byte[] data,
        ref int p)
    {
        EnsureAvailable(data, p, 1);

        return data[p++];
    }

    private static ushort ReadUInt16(
        byte[] data,
        ref int p)
    {
        EnsureAvailable(data, p, 2);

        ushort value =
            BinaryPrimitives.ReadUInt16LittleEndian(
                data.AsSpan(p, 2));

        p += 2;

        return value;
    }

    private static int ReadInt32(
        byte[] data,
        ref int p)
    {
        EnsureAvailable(data, p, 4);

        int value =
            BinaryPrimitives.ReadInt32LittleEndian(
                data.AsSpan(p, 4));

        p += 4;

        return value;
    }

    private static long ReadInt64(
        byte[] data,
        ref int p)
    {
        EnsureAvailable(data, p, 8);

        long value =
            BinaryPrimitives.ReadInt64LittleEndian(
                data.AsSpan(p, 8));

        p += 8;

        return value;
    }

    private static void EnsureAvailable(
        byte[] data,
        int offset,
        int size)
    {
        if (offset < 0 ||
            size < 0 ||
            offset > data.Length - size)
        {
            throw new BadImageFormatException(
                "IL operand 超出 IL stream 范围。");
        }
    }
}