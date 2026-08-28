using Nebulae.Diagnostics;
using Nebulae.Reflection.Specifiers;
using Nebulae.Runtime.Emit.Inline;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

namespace Nebulae.Reflection.Extensions
{
    internal static class ILGeneratorExtensions
    {
        //------------------------------------------------------
        //
        //  Convert Helpers
        //
        //------------------------------------------------------

        #region Convert Helpers

        public static void EmitAsByRef(this ILGenerator il, Type valueType)
        {
            Debug.Assert(!valueType.IsByRef, "Only non-byref types can be converted to byref.");

            LocalBuilder local = il.DeclareLocal(valueType);

            il.EmitStloc(local);
            il.Emit(OpCodes.Ldloca_S, local);
        }

        public static void EmitConv(this ILGenerator il, Type sourceType, Type targetType, SpecifierCulture culture)
        {
            bool sourceByRef = sourceType.IsByRef;
            bool targetByRef = targetType.IsByRef;

            if (sourceByRef)
            {
                sourceType = sourceType.GetElementType()!;
            }

            if (targetByRef)
            {
                targetType = targetType.GetElementType()!;
            }

            if (targetType.IsAssignableFrom(sourceType))
            {
                il.EmitRefAdapt(sourceType, sourceByRef, targetType, targetByRef);
            }
            else if (typeof(IConvertible).IsAssignableFrom(sourceType))
            {
                il.EmitConv(sourceType, sourceByRef, targetType, targetByRef, culture);
            }
            else
            {
                il.EmitFallBackConv(sourceType, sourceByRef, targetType, targetByRef);
            }
        }

        private static void EmitRefAdapt(this ILGenerator il, Type sourceType, bool sourceByRef, Type targetType, bool targetByRef)
        {
            Debug.Assert(targetType.IsAssignableFrom(sourceType), "Target type should be assignable from source type.");

            if (sourceByRef)
            {
                il.EmitLdind(sourceType);
            }

            if (targetType.IsNullable())
            {
                il.Emit(
                    OpCodes.Newobj,
                    targetType.GetConstructor(
                        BindingFlags.Public | BindingFlags.Instance,
                        binder: null,
                        [sourceType],
                        modifiers: null)
                        ?? throw new MissingMemberException($"Cannot find constructor for nullable type '{targetType.AsLog()}?'."));
            }
            else
            {
                il.Emit(OpCodes.Box, sourceType);
            }

            if (targetByRef)
            {
                il.EmitAsByRef(targetType);
            }
        }

        private static void EmitConv(this ILGenerator il, Type sourceType, bool sourceByRef, Type targetType, bool targetByRef, SpecifierCulture culture)
        {
            if (sourceByRef)
            {
                if (!sourceType.IsValueType)
                {
                    il.Emit(OpCodes.Ldind_Ref);
                }
            }
            else if (sourceType.IsValueType)
            {
                il.EmitAsByRef(sourceType);
            }

            switch (Type.GetTypeCode(targetType))
            {
                case TypeCode.Boolean:
                    il.Emit(OpCodes.Call, GetCultureInfo(culture));

                    if (sourceType.IsValueType)
                    {
                        il.Emit(OpCodes.Constrained, sourceType);
                    }

                    il.Emit(OpCodes.Callvirt, ToBoolean());
                    break;
                case TypeCode.Char:
                    il.Emit(OpCodes.Call, GetCultureInfo(culture));

                    if (sourceType.IsValueType)
                    {
                        il.Emit(OpCodes.Constrained, sourceType);
                    }

                    il.Emit(OpCodes.Callvirt, ToChar());
                    break;
                case TypeCode.SByte:
                    il.Emit(OpCodes.Call, GetCultureInfo(culture));

                    if (sourceType.IsValueType)
                    {
                        il.Emit(OpCodes.Constrained, sourceType);
                    }

                    il.Emit(OpCodes.Callvirt, ToSByte());
                    break;
                case TypeCode.Byte:
                    il.Emit(OpCodes.Call, GetCultureInfo(culture));

                    if (sourceType.IsValueType)
                    {
                        il.Emit(OpCodes.Constrained, sourceType);
                    }

                    il.Emit(OpCodes.Callvirt, ToByte());
                    break;
                case TypeCode.Int16:
                    il.Emit(OpCodes.Call, GetCultureInfo(culture));

                    if (sourceType.IsValueType)
                    {
                        il.Emit(OpCodes.Constrained, sourceType);
                    }

                    il.Emit(OpCodes.Callvirt, ToInt16());
                    break;
                case TypeCode.UInt16:
                    il.Emit(OpCodes.Call, GetCultureInfo(culture));

                    if (sourceType.IsValueType)
                    {
                        il.Emit(OpCodes.Constrained, sourceType);
                    }

                    il.Emit(OpCodes.Callvirt, ToUInt16());
                    break;
                case TypeCode.Int32:
                    il.Emit(OpCodes.Call, GetCultureInfo(culture));

                    if (sourceType.IsValueType)
                    {
                        il.Emit(OpCodes.Constrained, sourceType);
                    }

                    il.Emit(OpCodes.Callvirt, ToInt32());
                    break;
                case TypeCode.UInt32:
                    il.Emit(OpCodes.Call, GetCultureInfo(culture));

                    if (sourceType.IsValueType)
                    {
                        il.Emit(OpCodes.Constrained, sourceType);
                    }

                    il.Emit(OpCodes.Callvirt, ToUInt32());
                    break;
                case TypeCode.Int64:
                    il.Emit(OpCodes.Call, GetCultureInfo(culture));

                    if (sourceType.IsValueType)
                    {
                        il.Emit(OpCodes.Constrained, sourceType);
                    }

                    il.Emit(OpCodes.Callvirt, ToInt64());
                    break;
                case TypeCode.UInt64:
                    il.Emit(OpCodes.Call, GetCultureInfo(culture));

                    if (sourceType.IsValueType)
                    {
                        il.Emit(OpCodes.Constrained, sourceType);
                    }

                    il.Emit(OpCodes.Callvirt, ToUInt64());
                    break;
                case TypeCode.Single:
                    il.Emit(OpCodes.Call, GetCultureInfo(culture));

                    if (sourceType.IsValueType)
                    {
                        il.Emit(OpCodes.Constrained, sourceType);
                    }

                    il.Emit(OpCodes.Callvirt, ToSingle());
                    break;
                case TypeCode.Double:
                    il.Emit(OpCodes.Call, GetCultureInfo(culture));

                    if (sourceType.IsValueType)
                    {
                        il.Emit(OpCodes.Constrained, sourceType);
                    }

                    il.Emit(OpCodes.Callvirt, ToDouble());
                    break;
                case TypeCode.Decimal:
                    il.Emit(OpCodes.Call, GetCultureInfo(culture));

                    if (sourceType.IsValueType)
                    {
                        il.Emit(OpCodes.Constrained, sourceType);
                    }

                    il.Emit(OpCodes.Callvirt, ToDecimal());
                    break;
                case TypeCode.DateTime:
                    il.Emit(OpCodes.Call, GetCultureInfo(culture));

                    if (sourceType.IsValueType)
                    {
                        il.Emit(OpCodes.Constrained, sourceType);
                    }

                    il.Emit(OpCodes.Callvirt, ToDateTime());
                    break;
                case TypeCode.String:
                    il.Emit(OpCodes.Call, GetCultureInfo(culture));

                    if (sourceType.IsValueType)
                    {
                        il.Emit(OpCodes.Constrained, sourceType);
                    }

                    il.Emit(OpCodes.Callvirt, ToString());
                    break;
                default:
                    il.Emit(OpCodes.Ldtoken, targetType);
                    il.Emit(OpCodes.Call, GetTypeFromHandle());
                    il.Emit(OpCodes.Call, GetCultureInfo(culture));

                    if (sourceType.IsValueType)
                    {
                        il.Emit(OpCodes.Constrained, sourceType);
                    }

                    il.Emit(OpCodes.Callvirt, ToType());

                    if (targetType.IsValueType)
                    {
                        il.Emit(targetByRef ? OpCodes.Unbox : OpCodes.Unbox_Any, targetType);
                    }
                    else
                    {
                        il.Emit(OpCodes.Castclass, targetType);

                        if (targetByRef)
                        {
                            il.EmitAsByRef(targetType);
                        }
                    }
                    return;
            }

            if (targetByRef)
            {
                il.EmitAsByRef(targetType);
            }


            static MethodInfo GetCultureInfo(SpecifierCulture culture)
            {
                switch (culture)
                {
                    case SpecifierCulture.CurrentCulture:
                        IL.Emit.Ldtoken(
                            IL.Ref(typeof(CultureInfo))
                                .Property(nameof(CultureInfo.CurrentCulture))
                                .Get);
                        break;
                    case SpecifierCulture.CurrentUICulture:
                        IL.Emit.Ldtoken(
                            IL.Ref(typeof(CultureInfo))
                                .Property(nameof(CultureInfo.CurrentUICulture))
                                .Get);
                        break;
                    case SpecifierCulture.DefaultThreadCurrentCulture:
                        IL.Emit.Ldtoken(
                            IL.Ref(typeof(CultureInfo))
                                .Property(nameof(CultureInfo.DefaultThreadCurrentCulture))
                                .Get);
                        break;
                    case SpecifierCulture.DefaultThreadCurrentUICulture:
                        IL.Emit.Ldtoken(
                            IL.Ref(typeof(CultureInfo))
                                .Property(nameof(CultureInfo.DefaultThreadCurrentUICulture))
                                .Get);
                        break;
                    case SpecifierCulture.InstalledUICulture:
                        IL.Emit.Ldtoken(
                            IL.Ref(typeof(CultureInfo))
                                .Property(nameof(CultureInfo.InstalledUICulture))
                                .Get);
                        break;
                    case SpecifierCulture.InvariantCulture:
                        IL.Emit.Ldtoken(
                            IL.Ref(typeof(CultureInfo))
                                .Property(nameof(CultureInfo.InvariantCulture))
                                .Get);
                        break;
                    default:
                        throw new NotSupportedException(
                            $"Specifier culture info '{culture}' is not supported.");
                }

                IL.Emit.Call(
                    IL.Ref(typeof(MethodBase))
                        .Method(nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));

                return IL.Ret<MethodInfo>();
            }

            static MethodInfo GetTypeFromHandle()
            {
                IL.Emit.Ldtoken(
                    IL.Ref(typeof(Type))
                        .Method(nameof(Type.GetTypeFromHandle), typeof(RuntimeTypeHandle)));
                IL.Emit.Call(
                    IL.Ref(typeof(MethodBase))
                        .Method(nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));

                return IL.Ret<MethodInfo>();
            }

            static MethodInfo ToBoolean()
            {
                IL.Emit.Ldtoken(
                    IL.Ref(typeof(IConvertible))
                        .Method(nameof(IConvertible.ToBoolean), typeof(IFormatProvider)));
                IL.Emit.Call(
                    IL.Ref(typeof(MethodBase))
                        .Method(nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));

                return IL.Ret<MethodInfo>();
            }

            static MethodInfo ToChar()
            {
                IL.Emit.Ldtoken(
                    IL.Ref(typeof(IConvertible))
                        .Method(nameof(IConvertible.ToChar), typeof(IFormatProvider)));
                IL.Emit.Call(
                    IL.Ref(typeof(MethodBase))
                        .Method(nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));

                return IL.Ret<MethodInfo>();
            }

            static MethodInfo ToSByte()
            {
                IL.Emit.Ldtoken(
                    IL.Ref(typeof(IConvertible))
                        .Method(nameof(IConvertible.ToSByte), typeof(IFormatProvider)));
                IL.Emit.Call(
                    IL.Ref(typeof(MethodBase))
                        .Method(nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));

                return IL.Ret<MethodInfo>();
            }

            static MethodInfo ToByte()
            {
                IL.Emit.Ldtoken(
                    IL.Ref(typeof(IConvertible))
                        .Method(nameof(IConvertible.ToByte), typeof(IFormatProvider)));
                IL.Emit.Call(
                    IL.Ref(typeof(MethodBase))
                        .Method(nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));

                return IL.Ret<MethodInfo>();
            }

            static MethodInfo ToInt16()
            {
                IL.Emit.Ldtoken(
                    IL.Ref(typeof(IConvertible))
                        .Method(nameof(IConvertible.ToInt16), typeof(IFormatProvider)));
                IL.Emit.Call(
                    IL.Ref(typeof(MethodBase))
                        .Method(nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));

                return IL.Ret<MethodInfo>();
            }

            static MethodInfo ToUInt16()
            {
                IL.Emit.Ldtoken(
                    IL.Ref(typeof(IConvertible))
                        .Method(nameof(IConvertible.ToUInt16), typeof(IFormatProvider)));
                IL.Emit.Call(
                    IL.Ref(typeof(MethodBase))
                        .Method(nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));

                return IL.Ret<MethodInfo>();
            }

            static MethodInfo ToInt32()
            {
                IL.Emit.Ldtoken(
                    IL.Ref(typeof(IConvertible))
                        .Method(nameof(IConvertible.ToInt32), typeof(IFormatProvider)));
                IL.Emit.Call(
                    IL.Ref(typeof(MethodBase))
                        .Method(nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));

                return IL.Ret<MethodInfo>();
            }

            static MethodInfo ToUInt32()
            {
                IL.Emit.Ldtoken(
                    IL.Ref(typeof(IConvertible))
                        .Method(nameof(IConvertible.ToUInt32), typeof(IFormatProvider)));
                IL.Emit.Call(
                    IL.Ref(typeof(MethodBase))
                        .Method(nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));

                return IL.Ret<MethodInfo>();
            }

            static MethodInfo ToInt64()
            {
                IL.Emit.Ldtoken(
                    IL.Ref(typeof(IConvertible))
                        .Method(nameof(IConvertible.ToInt64), typeof(IFormatProvider)));
                IL.Emit.Call(
                    IL.Ref(typeof(MethodBase))
                        .Method(nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));

                return IL.Ret<MethodInfo>();
            }

            static MethodInfo ToUInt64()
            {
                IL.Emit.Ldtoken(
                    IL.Ref(typeof(IConvertible))
                        .Method(nameof(IConvertible.ToUInt64), typeof(IFormatProvider)));
                IL.Emit.Call(
                    IL.Ref(typeof(MethodBase))
                        .Method(nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));

                return IL.Ret<MethodInfo>();
            }

            static MethodInfo ToSingle()
            {
                IL.Emit.Ldtoken(
                    IL.Ref(typeof(IConvertible))
                        .Method(nameof(IConvertible.ToSingle), typeof(IFormatProvider)));
                IL.Emit.Call(
                    IL.Ref(typeof(MethodBase))
                        .Method(nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));

                return IL.Ret<MethodInfo>();
            }

            static MethodInfo ToDouble()
            {
                IL.Emit.Ldtoken(
                    IL.Ref(typeof(IConvertible))
                        .Method(nameof(IConvertible.ToDouble), typeof(IFormatProvider)));
                IL.Emit.Call(
                    IL.Ref(typeof(MethodBase))
                        .Method(nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));

                return IL.Ret<MethodInfo>();
            }

            static MethodInfo ToDecimal()
            {
                IL.Emit.Ldtoken(
                    IL.Ref(typeof(IConvertible))
                        .Method(nameof(IConvertible.ToDecimal), typeof(IFormatProvider)));
                IL.Emit.Call(
                    IL.Ref(typeof(MethodBase))
                        .Method(nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));

                return IL.Ret<MethodInfo>();
            }

            static MethodInfo ToDateTime()
            {
                IL.Emit.Ldtoken(
                    IL.Ref(typeof(IConvertible))
                        .Method(nameof(IConvertible.ToDateTime), typeof(IFormatProvider)));
                IL.Emit.Call(
                    IL.Ref(typeof(MethodBase))
                        .Method(nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));

                return IL.Ret<MethodInfo>();
            }

            static MethodInfo ToString()
            {
                IL.Emit.Ldtoken(
                    IL.Ref(typeof(IConvertible))
                        .Method(nameof(IConvertible.ToString), typeof(IFormatProvider)));
                IL.Emit.Call(
                    IL.Ref(typeof(MethodBase))
                        .Method(nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));

                return IL.Ret<MethodInfo>();
            }

            static MethodInfo ToType()
            {
                IL.Emit.Ldtoken(
                    IL.Ref(typeof(IConvertible))
                        .Method(nameof(IConvertible.ToType), typeof(Type), typeof(IFormatProvider)));
                IL.Emit.Call(
                    IL.Ref(typeof(MethodBase))
                        .Method(nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));

                return IL.Ret<MethodInfo>();
            }
        }

        private static void EmitFallBackConv(this ILGenerator il, Type sourceType, bool sourceByRef, Type targetType, bool targetByRef)
        {
            if (sourceByRef)
            {
                il.EmitLdind(sourceType);
            }

            if (sourceType.IsValueType)
            {
                // Ensure right sequence of value in the exception message.
                il.Emit(OpCodes.Box, sourceType);
                // Throw "InvalidCastException".
                il.Emit(OpCodes.Castclass, targetType);
            }
            else if (targetType.IsValueType)
            {
                il.Emit(targetByRef ? OpCodes.Unbox : OpCodes.Unbox_Any, targetType);
                return;
            }
            else
            {
                // Throw "InvalidCastException".
                il.Emit(OpCodes.Castclass, targetType);
            }

            if (targetByRef)
            {
                il.EmitAsByRef(targetType);
            }
        }

        #endregion


        //------------------------------------------------------
        //
        //  Debug Helpers
        //
        //------------------------------------------------------

        #region Debug Helpers
#if DEBUG

        public static void EmitDump(this ILGenerator il, Type valueType)
        {
            il.Emit(OpCodes.Dup);

            if (valueType.IsByRef)
            {
                valueType = valueType.GetElementType()!;
                il.EmitLdind(valueType);
            }

            if (valueType.IsValueType)
            {
                il.Emit(OpCodes.Box, valueType);
            }

            il.Emit(OpCodes.Ldstr, "IL Diagnostic");
            il.Emit(OpCodes.Call, Inspect());
            il.Emit(OpCodes.Pop);


            static MethodInfo Inspect()
            {
                IL.Emit.Ldtoken(
                    IL.Ref(typeof(DiagnosticHelpers))
                        .Method(nameof(DiagnosticHelpers.Inspect), 1, typeof(GenericRef), typeof(string))
                        .MakeGeneric(typeof(object)));
                IL.Emit.Call(
                    IL.Ref(typeof(MethodBase))
                        .Method(nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));

                return IL.Ret<MethodInfo>();
            }
        }

        public static void EmitWriteLine(this ILGenerator il, string message)
        {
            il.Emit(OpCodes.Ldstr, message);
            il.Emit(OpCodes.Call, WriteLine());

            static MethodInfo WriteLine()
            {
                IL.Emit.Ldtoken(
                    IL.Ref(typeof(ILGeneratorExtensions))
                        .Method(nameof(ILGeneratorExtensions.WriteLine), typeof(string)));
                IL.Emit.Call(
                    IL.Ref(typeof(MethodBase))
                        .Method(nameof(MethodBase.GetMethodFromHandle), typeof(RuntimeMethodHandle)));

                return IL.Ret<MethodInfo>();
            }
        }

        private static void WriteLine(string message)
        {
            Console.WriteLine(message);
            Debug.WriteLine(message);
            Trace.WriteLine(message);
        }

#endif
        #endregion


        //------------------------------------------------------
        //
        //  Load Helpers
        //
        //------------------------------------------------------

        #region Load Helpers

        public static bool IsConstant(this Type valueType)
        {
            return Type.GetTypeCode(valueType) switch
            {
                TypeCode.DBNull => true,
                TypeCode.Boolean => true,
                TypeCode.Char => true,
                TypeCode.SByte => true,
                TypeCode.Byte => true,
                TypeCode.Int16 => true,
                TypeCode.UInt16 => true,
                TypeCode.Int32 => true,
                TypeCode.UInt32 => true,
                TypeCode.Int64 => true,
                TypeCode.UInt64 => true,
                TypeCode.Single => true,
                TypeCode.Double => true,
                TypeCode.String => true,
                _ => false,
            };
        }

        public static void EmitLdarg(this ILGenerator il, short position)
        {
            switch (position)
            {
                case 0:
                    il.Emit(OpCodes.Ldarg_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Ldarg_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Ldarg_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Ldarg_3);
                    break;
                default:
                    il.Emit(OpCodes.Ldarg_S, position);
                    break;
            }
        }

        public static void EmitLdc(this ILGenerator il, object value, Type valueType)
        {
            Debug.Assert(value is not null, "Use 'Emitldnull' for null values.");

            switch (Type.GetTypeCode(valueType))
            {
                case TypeCode.DBNull:
                    il.Emit(OpCodes.Ldsfld, GetDBNull());
                    break;
                case TypeCode.Boolean:
                    il.Emit((bool)value! ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                    break;
                case TypeCode.Char:
                    il.EmitLdcI4((char)value!);
                    break;
                case TypeCode.SByte:
                    il.EmitLdcI4((sbyte)value!);
                    break;
                case TypeCode.Byte:
                    il.EmitLdcI4((byte)value!);
                    break;
                case TypeCode.Int16:
                    il.EmitLdcI4((short)value!);
                    break;
                case TypeCode.UInt16:
                    il.EmitLdcI4((ushort)value!);
                    break;
                case TypeCode.Int32:
                    il.EmitLdcI4((int)value!);
                    break;
                case TypeCode.UInt32:
                    il.EmitLdcI4(unchecked((int)(uint)value!));
                    break;
                case TypeCode.Int64:
                    il.Emit(OpCodes.Ldc_I8, (long)value!);
                    break;
                case TypeCode.UInt64:
                    il.Emit(OpCodes.Ldc_I8, unchecked((long)(ulong)value!));
                    break;
                case TypeCode.Single:
                    il.Emit(OpCodes.Ldc_R4, (float)value!);
                    break;
                case TypeCode.Double:
                    il.Emit(OpCodes.Ldc_R8, (double)value!);
                    break;
                case TypeCode.String:
                    il.Emit(OpCodes.Ldstr, (string)value!);
                    break;
                default:
                    throw new NotSupportedException(
                        $"Value '{value.AsLog()}' of type '{valueType.AsLog()}' is not supported.");
            }


            static FieldInfo GetDBNull()
            {
                IL.Emit.Ldtoken(
                    IL.Ref(typeof(DBNull))
                        .Field(nameof(DBNull.Value)));
                IL.Emit.Call(
                    IL.Ref(typeof(FieldInfo))
                        .Method(nameof(FieldInfo.GetFieldFromHandle), typeof(RuntimeFieldHandle)));

                return IL.Ret<FieldInfo>();
            }
        }

        public static void EmitLdcI4(this ILGenerator il, int value)
        {
            switch (value)
            {
                case -1:
                    il.Emit(OpCodes.Ldc_I4_M1);
                    break;
                case 0:
                    il.Emit(OpCodes.Ldc_I4_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Ldc_I4_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Ldc_I4_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Ldc_I4_3);
                    break;
                case 4:
                    il.Emit(OpCodes.Ldc_I4_4);
                    break;
                case 5:
                    il.Emit(OpCodes.Ldc_I4_5);
                    break;
                case 6:
                    il.Emit(OpCodes.Ldc_I4_6);
                    break;
                case 7:
                    il.Emit(OpCodes.Ldc_I4_7);
                    break;
                case 8:
                    il.Emit(OpCodes.Ldc_I4_8);
                    break;
                default:
                    if (value >= sbyte.MinValue && value <= sbyte.MaxValue)
                    {
                        il.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldc_I4, value);
                    }
                    break;
            }
        }

        public static void EmitLdind(this ILGenerator il, Type valueType)
        {
            switch (Type.GetTypeCode(valueType))
            {
                case TypeCode.Boolean:
                    il.Emit(OpCodes.Ldind_I1);
                    break;
                case TypeCode.Byte:
                    il.Emit(OpCodes.Ldind_U1);
                    break;
                case TypeCode.Char:
                    il.Emit(OpCodes.Ldind_U2);
                    break;
                case TypeCode.Double:
                    il.Emit(OpCodes.Ldind_R8);
                    break;
                case TypeCode.Int16:
                    il.Emit(OpCodes.Ldind_I2);
                    break;
                case TypeCode.Int32:
                    il.Emit(OpCodes.Ldind_I4);
                    break;
                case TypeCode.Int64:
                    il.Emit(OpCodes.Ldind_I8);
                    break;
                case TypeCode.SByte:
                    il.Emit(OpCodes.Ldind_I1);
                    break;
                case TypeCode.Single:
                    il.Emit(OpCodes.Ldind_R4);
                    break;
                case TypeCode.UInt16:
                    il.Emit(OpCodes.Ldind_U2);
                    break;
                case TypeCode.UInt32:
                    il.Emit(OpCodes.Ldind_U4);
                    break;
                case TypeCode.UInt64:
                    il.Emit(OpCodes.Ldind_I8);
                    break;
                default:
                    if (valueType.IsValueType)
                    {
                        il.Emit(OpCodes.Ldobj, valueType);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldind_Ref);
                    }
                    break;
            }
        }

        public static void EmitLdnull(this ILGenerator il, Type valueType)
        {
            if (valueType.IsByRef)
            {
                valueType = valueType.GetElementType()!;
                LocalBuilder local = il.DeclareLocal(valueType);

                if (valueType.IsValueType)
                {
                    il.Emit(OpCodes.Ldloca_S, local);
                    il.Emit(OpCodes.Initobj, valueType);
                }
                else
                {
                    il.Emit(OpCodes.Ldnull);
                    il.EmitStloc(local);
                }

                il.Emit(OpCodes.Ldloca_S, local);
            }
            else
            {
                if (valueType.IsValueType)
                {
                    LocalBuilder local = il.DeclareLocal(valueType);

                    il.Emit(OpCodes.Ldloca_S, local);
                    il.Emit(OpCodes.Initobj, valueType);
                    il.Emit(OpCodes.Ldloc_S, local);
                }
                else
                {
                    il.Emit(OpCodes.Ldnull);
                }
            }
        }

        public static void EmitLdtarg(this ILGenerator il, Type argumentType, Type targetType, short position)
        {
            bool sourceByRef = argumentType.IsByRef;

            if (sourceByRef)
            {
                argumentType = argumentType.GetElementType()!;
            }

            if (targetType.IsByRef)
            {
                targetType = targetType.GetElementType()!;
            }

            bool targetByRef = targetType.IsValueType;

            if (targetType == argumentType)
            {
                if (sourceByRef == targetByRef)
                {
                    il.EmitLdarg(position);
                }
                else if (sourceByRef)
                {
                    il.EmitLdarg(position);
                    il.EmitLdind(argumentType);
                }
                else
                {
                    il.Emit(OpCodes.Ldarga_S, position);
                }
            }
            else if (targetType.IsAssignableFrom(argumentType))
            {
                il.EmitLdarg(position);
                il.EmitRefAdapt(argumentType, sourceByRef, targetType, targetByRef);
            }
            else
            {
                il.EmitLdarg(position);
                il.EmitFallBackConv(argumentType, sourceByRef, targetType, targetByRef);
            }
        }

        #endregion


        public static void EmitStloc(this ILGenerator il, LocalBuilder local)
        {
            switch (local.LocalIndex)
            {
                case 0:
                    il.Emit(OpCodes.Stloc_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Stloc_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Stloc_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Stloc_3);
                    break;
                default:
                    il.Emit(OpCodes.Stloc_S, local);
                    break;
            }
        }
    }
}
