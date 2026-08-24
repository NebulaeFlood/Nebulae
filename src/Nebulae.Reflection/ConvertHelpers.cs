using Nebulae.Diagnostics;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Nebulae.Reflection
{
    /// <summary>
    /// 类型转换的工具类
    /// </summary>
    public static class ConvertHelpers
    {
        /// <summary>
        /// 将值转换为指定的类型
        /// </summary>
        /// <typeparam name="TFrom">源类型</typeparam>
        /// <typeparam name="TTo">目标类型</typeparam>
        /// <param name="value">目标值</param>
        /// <param name="provider">区域性特定的格式设置信息</param>
        /// <returns>由 <paramref name="value"/> 转换的 <typeparamref name="TTo"/> 类型的值。</returns>
        /// <remarks>
        /// <para>
        /// 转换基于 <see cref="IConvertible"/> 接口。
        /// </para>
        /// <para>
        /// <b>不支持 <see cref="Nullable{T}"/> 类型的转换</b>
        /// </para>
        /// </remarks>
        [return: NotNullIfNotNull(nameof(value))]
        public static TTo? ChangeType<TFrom, TTo>(TFrom value, IFormatProvider? provider)
            where TTo : notnull
        {
            if (value is null)
            {
                if (!typeof(TTo).IsValueType)
                {
                    return default;
                }

                throw new InvalidCastException(
                    $"Cannot convert from '{DiagnosticHelpers.Null}' to '{typeof(TTo).AsLog()}'.");
            }

            if (value is TTo result)
            {
                return result;
            }

            if (value is not IConvertible source)
            {
                throw new InvalidCastException(
                    $"Cannot convert value '{value.AsLog()}' " +
                    $"of type '{value.GetType().AsLog()}' " +
                    $"to '{typeof(TTo).AsLog()}'.");
            }

            Type targetType = typeof(TTo);

            try
            {
                return Type.GetTypeCode(targetType) switch
                {
                    TypeCode.Boolean => Reinterpret(source.ToBoolean(provider)),
                    TypeCode.Byte => Reinterpret(source.ToByte(provider)),
                    TypeCode.Char => Reinterpret(source.ToChar(provider)),
                    TypeCode.DateTime => Reinterpret(source.ToDateTime(provider)),
                    TypeCode.Decimal => Reinterpret(source.ToDecimal(provider)),
                    TypeCode.Double => Reinterpret(source.ToDouble(provider)),
                    TypeCode.Int16 => Reinterpret(source.ToInt16(provider)),
                    TypeCode.Int32 => Reinterpret(source.ToInt32(provider)),
                    TypeCode.Int64 => Reinterpret(source.ToInt64(provider)),
                    TypeCode.SByte => Reinterpret(source.ToSByte(provider)),
                    TypeCode.Single => Reinterpret(source.ToSingle(provider)),
                    TypeCode.String => Reinterpret(source.ToString(provider)),
                    TypeCode.UInt16 => Reinterpret(source.ToUInt16(provider)),
                    TypeCode.UInt32 => Reinterpret(source.ToUInt32(provider)),
                    TypeCode.UInt64 => Reinterpret(source.ToUInt64(provider)),
                    _ => (TTo)source.ToType(targetType, provider),
                };
            }
            catch (Exception e)
            {
                throw new InvalidCastException(
                    $"Cannot convert value '{value.AsLog()}' " +
                    $"of type '{value.GetType().AsLog()}' " +
                    $"to '{targetType.AsLog()}'.", e);
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static TTo Reinterpret<TSource>(TSource value)
            {
                return Unsafe.As<TSource, TTo>(ref value);
            }
        }


        /// <summary>
        /// 通过 <see cref="TypeCode"/> 判断类型是否可通过 <see cref="System.Convert"/> 互相转换
        /// </summary>
        /// <param name="sourceType">将转换的源类型</param>
        /// <param name="targetType">将转换的目标类型</param>
        /// <returns>若源类型与目标类型能互相转换，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        /// <remarks>
        /// <para>
        /// 该方法检查 <see cref="System.Convert"/>
        /// 能否将 <paramref name="sourceType"/>
        /// 和 <paramref name="targetType"/> <b>互相转换</b>。
        /// </para>
        /// <para>
        /// 当 <paramref name="sourceType"/>
        /// 与 <paramref name="targetType"/> 相同时，
        /// 可能返回 <see langword="false"/>。
        /// </para>
        /// <para>
        /// 任一参数为 <see langword="null"/> 时，
        /// 返回 <see langword="false"/>。
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsConvertible(Type sourceType, Type targetType)
        {
            return IsConvertible(Type.GetTypeCode(sourceType), Type.GetTypeCode(targetType));
        }

        /// <summary>
        /// 通过 <see cref="TypeCode"/> 判断类型是否可通过 <see cref="System.Convert"/> 互相转换
        /// </summary>
        /// <param name="sourceTypeCode">将转换的源类型的 <see cref="TypeCode"/></param>
        /// <param name="targetTypeCode">将转换的目标类型的 <see cref="TypeCode"/></param>
        /// <returns>若源类型与目标类型能互相转换，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        /// <remarks>
        /// <para>
        /// 该方法检查 <see cref="System.Convert"/>
        /// 能否将源类型和目标类型<b>互相转换</b>。
        /// </para>
        /// <para>
        /// 当 <paramref name="sourceTypeCode"/> 与 <paramref name="targetTypeCode"/> 相同时，可能返回 <see langword="false"/>。
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsConvertible(TypeCode sourceTypeCode, TypeCode targetTypeCode)
        {
            const int ConvertibleTypeCodeMask =
                (1 << (int)TypeCode.String) |
                (1 << (int)TypeCode.Int32) | (1 << (int)TypeCode.Int64) |
                (1 << (int)TypeCode.Single) | (1 << (int)TypeCode.Double) |
                (1 << (int)TypeCode.Decimal) | (1 << (int)TypeCode.Boolean) |
                (1 << (int)TypeCode.Byte) | (1 << (int)TypeCode.Int16) |
                (1 << (int)TypeCode.UInt32) | (1 << (int)TypeCode.UInt64) |
                (1 << (int)TypeCode.UInt16) | (1 << (int)TypeCode.SByte);

            const int CharConvertibleTypeCodeMask =
                (1 << (int)TypeCode.String) |
                (1 << (int)TypeCode.Int32) | (1 << (int)TypeCode.Int64) |
                (1 << (int)TypeCode.Byte) | (1 << (int)TypeCode.Int16) |
                (1 << (int)TypeCode.UInt32) | (1 << (int)TypeCode.UInt64) |
                (1 << (int)TypeCode.UInt16) | (1 << (int)TypeCode.SByte);

            if (sourceTypeCode is TypeCode.DateTime)
            {
                return targetTypeCode is TypeCode.String;
            }

            if (targetTypeCode is TypeCode.DateTime)
            {
                return sourceTypeCode is TypeCode.String;
            }

            if (sourceTypeCode is TypeCode.Char)
            {
                return (CharConvertibleTypeCodeMask & (1 << (int)targetTypeCode)) != 0;
            }

            if (targetTypeCode is TypeCode.Char)
            {
                return (CharConvertibleTypeCodeMask & (1 << (int)sourceTypeCode)) != 0;
            }

            return (ConvertibleTypeCodeMask & (1 << (int)sourceTypeCode)) != 0
                && (ConvertibleTypeCodeMask & (1 << (int)targetTypeCode)) != 0;
        }


        /// <summary>
        /// 转换异常抛出的工具类
        /// </summary>
        public static class ThrowHelpers
        {
            /// <summary>
            /// 抛出转换无效时的异常
            /// </summary>
            /// <param name="sourceType">源类型</param>
            /// <param name="targetType">目标类型</param>
            /// <param name="innerException">内部异常</param>
            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ThrowInvalidCastException(Type sourceType, Type targetType, Exception? innerException = null)
            {
                throw new InvalidCastException($"Cannot convert from '{sourceType.AsLog()}' to '{targetType.AsLog()}'.", innerException);
            }

            /// <summary>
            /// 抛出转换无效时的异常
            /// </summary>
            /// <typeparam name="T">源数据的类型</typeparam>
            /// <param name="source">源数据</param>
            /// <param name="targetType">目标类型</param>
            /// <param name="innerException">内部异常</param>
            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ThrowInvalidCastException<T>(T source, Type targetType, Exception? innerException = null)
            {
                if (source is null)
                {
                    throw new InvalidCastException($"Cannot convert value '{DiagnosticHelpers.Null}' to '{targetType.AsLog()}'.", innerException);
                }
                else
                {
                    throw new InvalidCastException($"Cannot convert value '{source.AsLog()}' of type '{source.GetType().AsLog()}' to '{targetType.AsLog()}'.", innerException);
                }
            }

            /// <summary>
            /// 抛出转换无效时的异常
            /// </summary>
            /// <param name="memberInfo">提供源数据的成员的信息</param>
            /// <param name="targetType">目标类型</param>
            /// <param name="innerException">内部异常</param>
            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ThrowInvalidMemberCastException(FieldInfo memberInfo, Type targetType, Exception? innerException = null)
            {
                throw new InvalidCastException($"Cannot convert field '{memberInfo.AsLog()}' of type '{memberInfo.FieldType.AsLog()}' to '{targetType.AsLog()}'.", innerException);
            }

            /// <summary>
            /// 抛出转换无效时的异常
            /// </summary>
            /// <param name="memberInfo">提供源数据的成员的信息</param>
            /// <param name="targetType">目标类型</param>
            /// <param name="innerException">内部异常</param>
            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ThrowInvalidMemberCastException(PropertyInfo memberInfo, Type targetType, Exception? innerException = null)
            {
                throw new InvalidCastException($"Cannot convert property '{memberInfo.AsLog()}' of type '{memberInfo.PropertyType.AsLog()}' to '{targetType.AsLog()}'.", innerException);
            }

            /// <summary>
            /// 抛出转换无效时的异常
            /// </summary>
            /// <typeparam name="T">源数据的类型</typeparam>
            /// <param name="source">源数据</param>
            /// <param name="memberInfo">提供源数据的成员的信息</param>
            /// <param name="targetType">目标类型</param>
            /// <param name="innerException">内部异常</param>
            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ThrowInvalidMemberCastExceptionWithValue<T>(T source, FieldInfo memberInfo, Type targetType, Exception? innerException = null)
            {
                if (source is null)
                {
                    throw new InvalidCastException(
                        $"Cannot convert field '{memberInfo.AsLog()}' of type '{memberInfo.FieldType.AsLog()}' to '{targetType.AsLog()}'. " +
                        $"The field value was '{DiagnosticHelpers.Null}'.", innerException);
                }
                else
                {
                    throw new InvalidCastException(
                        $"Cannot convert field '{memberInfo.AsLog()}' of type '{memberInfo.FieldType.AsLog()}' to '{targetType.AsLog()}'. " +
                        $"The field value was '{source.AsLog()}' of type '{source.GetType().AsLog()}'.", innerException);
                }
            }

            /// <summary>
            /// 抛出转换无效时的异常
            /// </summary>
            /// <typeparam name="T">源数据的类型</typeparam>
            /// <param name="source">源数据</param>
            /// <param name="memberInfo">提供源数据的成员的信息</param>
            /// <param name="targetType">目标类型</param>
            /// <param name="innerException">内部异常</param>
            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ThrowInvalidMemberCastExceptionWithValue<T>(T source, PropertyInfo memberInfo, Type targetType, Exception? innerException = null)
            {
                if (source is null)
                {
                    throw new InvalidCastException(
                        $"Cannot convert property '{memberInfo.AsLog()}' of type '{memberInfo.PropertyType.AsLog()}' to '{targetType.AsLog()}'. " +
                        $"The property value was '{DiagnosticHelpers.Null}'.", innerException);
                }
                else
                {
                    throw new InvalidCastException(
                        $"Cannot convert property '{memberInfo.AsLog()}' of type '{memberInfo.PropertyType.AsLog()}' to '{targetType.AsLog()}'. " +
                        $"The property value was '{source.AsLog()}' of type '{source.GetType().AsLog()}'.", innerException);
                }
            }
        }
    }
}
