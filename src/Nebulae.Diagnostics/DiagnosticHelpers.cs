using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace Nebulae.Diagnostics
{
    /// <summary>
    /// 分析日志生成的工具类
    /// </summary>
    [DebuggerStepThrough]
    public static class DiagnosticHelpers
    {
        /// <summary>
        /// <see langword="null"/> 对象的日志文本表示
        /// </summary>
        public const string Null = "System.Null";


        //------------------------------------------------------
        //
        //  String Methods
        //
        //------------------------------------------------------

        #region String Methods

        /// <summary>
        /// 将对象转换为日志文本
        /// </summary>
        /// <typeparam name="T">对象的类型</typeparam>
        /// <param name="obj">要转换的对象</param>
        /// <returns>由 <paramref name="obj"/> 转换的日志文本。</returns>
        /// <remarks>
        /// </remarks>
        public static string AsLog<T>(this T? obj)
        {
            if (obj is null)
            {
                return Null;
            }

            if (obj is Type type)
            {
                return new StringBuilder(64)
                    .FormatType(type)
                    .ToString();
            }

            if (obj is Delegate @delegate)
            {
                return new StringBuilder(128)
                    .FormatDelegate(@delegate)
                    .ToString();
            }

            if (obj is MemberInfo member)
            {
                return new StringBuilder(128)
                    .FormatMember(member)
                    .ToString();
            }

            if (obj is DBNull)
            {
                return "System.DBNull";
            }

            if (obj is ParameterInfo[] parameters)
            {
                return new StringBuilder(128)
                    .FormatParameters(parameters)
                    .ToString();
            }

            if (obj is Type[] types)
            {
                return new StringBuilder(128)
                    .FormatTypes(types)
                    .ToString();
            }

            if (obj is string str)
            {
                return str.AsLog();
            }

            if (obj is char c)
            {
                return char.IsWhiteSpace(c) ? "Char.WhiteSpace" : c.ToString();
            }

            return obj.ToString()
                ?? new StringBuilder(64).FormatType(obj.GetType()).ToString();
        }

        /// <summary>
        /// 将字符串转换为日志文本
        /// </summary>
        /// <param name="str">要转换的字符串</param>
        /// <returns>由 <paramref name="str"/> 转换的日志文本。</returns>
        /// <remarks>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string AsLog(this string? str)
        {
            if (str is null)
            {
                return Null;
            }

            if (str.Length is 0)
            {
                return "String.Empty";
            }

            if (IsWhiteSpace(str))
            {
                return "String.WhiteSpace";
            }

            return str;
        }

        /// <summary>
        /// 将类型转换为日志文本
        /// </summary>
        /// <param name="type">要转换的类型</param>
        /// <returns>由 <paramref name="type"/> 转换的日志文本。</returns>
        public static string AsLog(this Type? type)
        {
            if (type is null)
            {
                return Null;
            }

            return new StringBuilder(64)
                .FormatType(type)
                .ToString();
        }

        /// <summary>
        /// 将类型数组转换为日志文本
        /// </summary>
        /// <param name="types">要转换的类型数组</param>
        /// <returns>由 <paramref name="types"/> 转换的日志文本。</returns>
        public static string AsLog(this Type[]? types)
        {
            if (types is null)
            {
                return Null;
            }

            return new StringBuilder(128)
                .FormatTypes(types)
                .ToString();
        }

        /// <summary>
        /// 将参数信息数组转换为日志文本
        /// </summary>
        /// <param name="parameters">要转换的参数信息数组数组</param>
        /// <returns>由 <paramref name="parameters"/> 转换的日志文本。</returns>
        public static string AsLog(this ParameterInfo[]? parameters)
        {
            if (parameters is null)
            {
                return Null;
            }

            return new StringBuilder(128)
                .FormatParameters(parameters)
                .ToString();
        }

        /// <summary>
        /// 将委托转换为日志文本
        /// </summary>
        /// <param name="delegate">要转换的委托</param>
        /// <returns>由 <paramref name="delegate"/> 转换的日志文本。</returns>
        public static string AsLog(this Delegate? @delegate)
        {
            if (@delegate is null)
            {
                return Null;
            }

            return new StringBuilder(128)
                .FormatDelegate(@delegate)
                .ToString();
        }

        /// <summary>
        /// 将方法转换为日志文本
        /// </summary>
        /// <param name="method">要转换的方法</param>
        /// <returns>由 <paramref name="method"/> 转换的日志文本。</returns>
        public static string AsLog(this MethodInfo? method)
        {
            if (method is null)
            {
                return Null;
            }

            return new StringBuilder(128)
                .FormatMethod(method)
                .ToString();
        }

        /// <summary>
        /// 将成员转换为日志文本
        /// </summary>
        /// <param name="member">要转换的成员</param>
        /// <returns>由 <paramref name="member"/> 转换的日志文本。</returns>
        public static string AsLog(this MemberInfo? member)
        {
            if (member is null)
            {
                return Null;
            }

            return new StringBuilder(64)
                .FormatMember(member)
                .ToString();
        }

        #endregion


        //------------------------------------------------------
        //
        //  Builder Methods
        //
        //------------------------------------------------------

        #region Builder Methods

        /// <summary>
        /// 将对象的格式化日志追加到字符串构建器
        /// </summary>
        /// <typeparam name="T">对象的类型</typeparam>
        /// <param name="builder">字符串构建器</param>
        /// <param name="obj">格式对象</param>
        /// <returns>添加了格式化信息的字符串构建器。</returns>
        public static StringBuilder Format<T>(this StringBuilder builder, T? obj)
        {
            if (obj is null)
            {
                return builder.Append(Null);
            }

            if (obj is Delegate @delegate)
            {
                return builder
                    .FormatType(@delegate.GetType()).Append('(')
                    .FormatMethod(@delegate.Method).Append(')');
            }

            if (obj is MemberInfo member)
            {
                return builder.FormatMember(member);
            }

            if (obj is ParameterInfo[] parameters)
            {
                return builder.FormatParameters(parameters);
            }

            if (obj is DBNull)
            {
                return builder.Append("System.DBNull");
            }

            if (obj is string str)
            {
                return builder.Format(str);
            }

            if (obj is char c)
            {
                return builder.Append(char.IsWhiteSpace(c) ? "Char.WhiteSpace" : c.ToString());
            }

            str = obj.ToString()!;

            if (str is null)
            {
                return builder.FormatType(obj.GetType());
            }

            return builder.Append(str);
        }

        /// <summary>
        /// 将字符串的格式化到字符串构建器
        /// </summary>
        /// <param name="builder">字符串构建器</param>
        /// <param name="str">目标字符串</param>
        /// <returns>添加了格式化类型信息的字符串构建器。</returns>
        public static StringBuilder Format(this StringBuilder builder, string? str)
        {
            if (str is null)
            {
                return builder.Append(Null);
            }

            if (str.Length is 0)
            {
                return builder.Append("String.Empty");
            }

            if (IsWhiteSpace(str))
            {
                return builder.Append("String.WhiteSpace");
            }

            return builder.Append(str);
        }

        /// <summary>
        /// 将类型信息格式化到字符串构建器
        /// </summary>
        /// <param name="builder">字符串构建器</param>
        /// <param name="type">类型信息</param>
        /// <returns>添加了格式化类型信息的字符串构建器。</returns>
        public static StringBuilder Format(this StringBuilder builder, Type type)
        {
            ThrowHelpers.ThrowIfArgumentNull(type);
            return builder.FormatType(type);
        }

        /// <summary>
        /// 将类型信息数组格式化到字符串构建器
        /// </summary>
        /// <param name="builder">字符串构建器</param>
        /// <param name="types">类型信息数组</param>
        /// <returns>添加了格式化类型信息数组的字符串构建器。</returns>
        public static StringBuilder Format(this StringBuilder builder, Type[] types)
        {
            ThrowHelpers.ThrowIfArgumentNull(types);
            return builder.FormatTypes(types);
        }

        /// <summary>
        /// 将参数信息数组格式化到字符串构建器
        /// </summary>
        /// <param name="builder">字符串构建器</param>
        /// <param name="parameters">参数信息数组</param>
        /// <returns>添加了格式化参数信息数组的字符串构建器。</returns>
        public static StringBuilder Format(this StringBuilder builder, ParameterInfo[] parameters)
        {
            ThrowHelpers.ThrowIfArgumentNull(parameters);
            return builder.FormatParameters(parameters);
        }

        /// <summary>
        /// 将委托格式化到字符串构建器
        /// </summary>
        /// <param name="builder">字符串构建器</param>
        /// <param name="delegate">委托对象</param>
        /// <returns>添加了格式化委托信息的字符串构建器。</returns>
        public static StringBuilder Format(this StringBuilder builder, Delegate @delegate)
        {
            ThrowHelpers.ThrowIfArgumentNull(@delegate);
            return builder.FormatDelegate(@delegate);
        }

        /// <summary>
        /// 将方法格式化到字符串构建器
        /// </summary>
        /// <param name="builder">字符串构建器</param>
        /// <param name="method">方法信息</param>
        /// <returns>添加了格式化方法信息的字符串构建器。</returns>
        public static StringBuilder Format(this StringBuilder builder, MethodInfo method)
        {
            ThrowHelpers.ThrowIfArgumentNull(method);
            return builder.FormatMethod(method);
        }

        /// <summary>
        /// 将成员格式化到字符串构建器
        /// </summary>
        /// <param name="builder">字符串构建器</param>
        /// <param name="member">成员信息</param>
        /// <returns>添加了格式化成员信息的字符串构建器。</returns>
        public static StringBuilder Format(this StringBuilder builder, MemberInfo member)
        {
            ThrowHelpers.ThrowIfArgumentNull(member);
            return builder.FormatMember(member);
        }

        #endregion


        /// <summary>
        /// 将对象转储到日志输出
        /// </summary>
        /// <param name="obj">要转换的对象</param>
        /// <param name="subject">日志主语</param>
        /// <returns>返回传入的 <paramref name="obj"/>。</returns>
        public static T Dump<T>(this T obj, [CallerMemberName] string? subject = null)
        {
            string? message;

            if (obj is string || obj is not IEnumerable collection)
            {
                message = obj.AsLog();
            }
            else
            {
                var builder = new StringBuilder(128).Append('[');
                var isFirst = true;

                foreach (var item in collection)
                {
                    if (!isFirst)
                    {
                        builder.Append(", ");
                    }

                    if (item is string || item is not IEnumerable)
                    {
                        builder.Append(item.AsLog());
                    }
                    else
                    {
                        builder.Append("[...]");
                    }

                    isFirst = false;
                }

                message = builder.Append(']').ToString();
            }

#if RIMWORLD // On my RimWorld mods.
            Verse.Log.Message($"<color=#3F48CC>[{subject}]</color> {message}");
#endif
            message = $"[{subject}] {message}";

            Console.WriteLine(message);
            Debug.WriteLine(message);
            Trace.WriteLine(message);

            return obj;
        }

        /// <summary>
        /// 将对象转储到日志输出
        /// </summary>
        /// <param name="obj">要转换的对象</param>
        /// <param name="stackTrace">调用堆栈</param>
        /// <param name="subject">日志主语</param>
        /// <returns>返回传入的 <paramref name="obj"/>。</returns>
        public static T Dump<T>(this T obj, StackTrace stackTrace, [CallerMemberName] string? subject = null)
        {
            ThrowHelpers.ThrowIfArgumentNull(stackTrace);

            string? message;

            if (obj is string || obj is not IEnumerable collection)
            {
                message = obj.AsLog();
            }
            else
            {
                var builder = new StringBuilder(128).Append('[');
                var isFirst = true;

                foreach (var item in collection)
                {
                    if (!isFirst)
                    {
                        builder.Append(", ");
                    }

                    if (item is string || item is not IEnumerable)
                    {
                        builder.Append(item.AsLog());
                    }
                    else
                    {
                        builder.Append("[...]");
                    }

                    isFirst = false;
                }

                message = builder.Append(']').ToString();
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                subject = DateTime.Now.ToString("HH:mm:ss.fff");
            }

            message = $"[{subject}] {message}{Environment.NewLine}{stackTrace}";

            Console.WriteLine(message);
            Debug.WriteLine(message);
            Trace.WriteLine(message);

            return obj;
        }


        //------------------------------------------------------
        //
        //  Private Format Helpers
        //
        //------------------------------------------------------

        #region Private Format Helpers

        private static StringBuilder FormatDeclaringType(this StringBuilder builder, Type? declaringType, bool scoped = true)
        {
            if (declaringType is null)
            {
                return builder.Append("[global] ");
            }

            return builder
                .FormatType(declaringType, scoped)
                .Append('.');
        }

        private static StringBuilder FormatMember(this StringBuilder builder, MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Constructor:
                    return builder
                        .FormatType(typeof(void))
                        .Append(' ')
                        .FormatDeclaringType(member.DeclaringType)
                        .Append("ctor(")
                        .FormatParameters(((ConstructorInfo)member).GetParameters())
                        .Append(')');
                case MemberTypes.Event:
                case MemberTypes.Field:
                    return builder.FormatDeclaringType(member.DeclaringType).Append(member.Name);
                case MemberTypes.Property:
                    var parameters = ((PropertyInfo)member).GetIndexParameters();

                    if (parameters.Length < 1)
                    {
                        return builder.FormatDeclaringType(member.DeclaringType).Append(member.Name);
                    }
                    else
                    {
                        return builder.FormatDeclaringType(member.DeclaringType).Append("this[").FormatParameters(parameters).Append(']');
                    }
                case MemberTypes.Method:
                    return builder.FormatMethod((MethodInfo)member);
                case MemberTypes.NestedType:
                case MemberTypes.TypeInfo:
                    return builder.FormatType((Type)member);
                default:
                    return builder.Append(member);
            }
        }

        private static StringBuilder FormatDelegate(this StringBuilder builder, Delegate @delegate)
        {
            var invocationList = @delegate.GetInvocationList();

            if (invocationList.Length is 1)
            {
                builder
                    .Append("[delegate]")
                    .FormatType(@delegate.GetType()).Append('(')
                    .FormatMethod(@delegate.Method).Append(')');
            }
            else
            {
                builder
                    .Append("[multicast delegate]")
                    .FormatType(@delegate.GetType()).Append('(')
                    .FormatMethod(@delegate.Method).Append(')');

                for (int i = 1; i < invocationList.Length; i++)
                {
                    builder.AppendLine().FormatDelegate(invocationList[i]);
                }
            }

            return builder;
        }

        private static StringBuilder FormatMethod(this StringBuilder builder, MethodInfo method)
        {
            builder
                .FormatType(method.ReturnType).Append(' ')
                .FormatDeclaringType(method.DeclaringType)
                .Append(method.Name);

            if (method.IsGenericMethod)
            {
                var genericArguments = method.GetGenericArguments();

                builder.Append('<').FormatType(genericArguments[0]);

                for (int i = 1; i < genericArguments.Length; i++)
                {
                    builder.Append(", ").FormatType(genericArguments[i]);
                }

                builder.Append('>');
            }

            return builder.Append('(').FormatParameters(method.GetParameters()).Append(')');
        }

        private static StringBuilder FormatParameters(this StringBuilder builder, ParameterInfo[] parameters)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                ParameterInfo parameter = parameters[i];
                Type parameterType = parameter.ParameterType;

                if (parameter.IsIn)
                {
                    builder.Append("in ").FormatType(parameterType.GetElementType()!);
                }
                else if (parameter.IsOut)
                {
                    builder.Append("out ").FormatType(parameterType.GetElementType()!);
                }
                else if (parameterType.IsByRef)
                {
                    builder.Append("ref ").FormatType(parameterType.GetElementType()!);
                }
                else
                {
                    builder.FormatType(parameterType);
                }

                if (!string.IsNullOrEmpty(parameter.Name))
                {
                    builder.Append(' ').Append(parameter.Name);
                }
            }

            return builder;
        }

        private static StringBuilder FormatType(this StringBuilder builder, Type type, bool scoped = true)
        {
            var elementType = type.GetElementType();

            if (elementType is null)
            {
                if (type.IsGenericType)
                {
                    if (type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        var underlyingType = Nullable.GetUnderlyingType(type);

                        if (underlyingType is not null)
                        {
                            return FormatType(builder, underlyingType, scoped).Append('?');
                        }
                    }

                    if (scoped)
                    {
                        builder.Append(type.Namespace).Append('.');
                    }

                    if (type.IsNested)
                    {
                        FormatType(builder, type.DeclaringType!, scoped: false).Append('+');
                    }

                    builder.Append(type.Name.TrimEnd(GenericTypeNameTrimChars)).Append('<');

                    var genericArguments = type.GetGenericArguments();

                    FormatType(builder, genericArguments[0], scoped);

                    for (int i = 1; i < genericArguments.Length; i++)
                    {
                        builder.Append(", ").FormatType(genericArguments[i], scoped);
                    }

                    return builder.Append('>');
                }
                else
                {
                    if (!type.IsGenericParameter)
                    {
                        if (scoped)
                        {
                            builder.Append(type.Namespace).Append('.');
                        }

                        if (type.IsNested)
                        {
                            FormatType(builder, type.DeclaringType!, scoped: false).Append('+');
                        }
                    }

                    return builder.Append(type.Name);
                }
            }
            else
            {
                FormatType(builder, elementType);

                if (type.IsArray)
                {
                    var rank = type.GetArrayRank();

                    if (rank > 1)
                    {
                        builder.Append('[').Append(new string(',', rank - 1)).Append(']');
                    }
                    else
                    {
                        builder.Append("[]");
                    }
                }

                if (type.IsByRef)
                {
                    builder.Append('&');
                }

                if (type.IsPointer)
                {
                    builder.Append('*');
                }

                return builder;
            }
        }

        private static StringBuilder FormatTypes(this StringBuilder builder, Type[] types)
        {
            for (int i = 0; i < types.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ").FormatType(types[i]);
                }
                else
                {
                    builder.Append(types[i]);
                }
            }

            return builder;
        }

        private static bool IsWhiteSpace(string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (!char.IsWhiteSpace(str[i]))
                {
                    return false;
                }
            }

            return true;
        }

        #endregion


        private static readonly char[] GenericTypeNameTrimChars = ['`', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'];
    }
}
