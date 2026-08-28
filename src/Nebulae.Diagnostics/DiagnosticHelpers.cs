using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;

namespace Nebulae.Diagnostics
{
    /// <summary>
    /// 分析日志生成的工具类
    /// </summary>
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

            if (obj is ParameterInfo[] parameters)
            {
                return new StringBuilder(128)
                    .FormatParameters(parameters)
                    .ToString();
            }

            if (obj is ParameterInfo parameter)
            {
                return new StringBuilder(64)
                    .FormatParameter(parameter)
                    .ToString();
            }

            if (obj is Type[] types)
            {
                return new StringBuilder(128)
                    .FormatTypes(types)
                    .ToString();
            }

            if (obj is DBNull)
            {
                return "System.DBNull";
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
        /// 将参数信息转换为日志文本
        /// </summary>
        /// <param name="parameter">要转换的参数信息</param>
        /// <returns>由 <paramref name="parameter"/> 转换的日志文本。</returns>
        public static string AsLog(this ParameterInfo? parameter)
        {
            if (parameter is null)
            {
                return Null;
            }
            return new StringBuilder(64)
                .FormatParameter(parameter)
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
        /// 将对象的格式化日志追加到 <see cref="StringBuilder"/>
        /// </summary>
        /// <typeparam name="T">对象的类型</typeparam>
        /// <param name="builder">目标 <see cref="StringBuilder"/></param>
        /// <param name="obj">格式对象</param>
        /// <returns>添加了格式化信息的 <see cref="StringBuilder"/>。</returns>
        public static StringBuilder Format<T>(this StringBuilder builder, T? obj)
        {
            if (obj is null)
            {
                return builder.Append(Null);
            }

            if (obj is Type type)
            {
                return builder.FormatType(type);
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

            if (obj is ParameterInfo parameter)
            {
                return builder.FormatParameter(parameter);
            }

            if (obj is Type[] types)
            {
                return builder.FormatTypes(types);
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
                return char.IsWhiteSpace(c)
                    ? builder.Append("Char.WhiteSpace")
                    : builder.Append(c);
            }

            str = obj.ToString()!;

            if (str is null)
            {
                return builder.FormatType(obj.GetType());
            }

            return builder.Append(str);
        }

        /// <summary>
        /// 将字符串的格式化到 <see cref="StringBuilder"/>
        /// </summary>
        /// <param name="builder">目标 <see cref="StringBuilder"/></param>
        /// <param name="str">目标字符串</param>
        /// <returns>添加了格式化类型信息的 <see cref="StringBuilder"/>。</returns>
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
        /// 将类型信息格式化到 <see cref="StringBuilder"/>
        /// </summary>
        /// <param name="builder">目标 <see cref="StringBuilder"/></param>
        /// <param name="type">类型信息</param>
        /// <returns>添加了格式化类型信息的 <see cref="StringBuilder"/>。</returns>
        public static StringBuilder Format(this StringBuilder builder, Type type)
        {
            ThrowHelpers.ThrowIfArgumentNull(type);
            return builder.FormatType(type);
        }

        /// <summary>
        /// 将类型信息数组格式化到 <see cref="StringBuilder"/>
        /// </summary>
        /// <param name="builder">目标 <see cref="StringBuilder"/></param>
        /// <param name="types">类型信息数组</param>
        /// <returns>添加了格式化类型信息数组的 <see cref="StringBuilder"/>。</returns>
        public static StringBuilder Format(this StringBuilder builder, scoped ReadOnlySpan<Type> types)
        {
            return builder.FormatTypes(types);
        }

        /// <summary>
        /// 将参数信息格式化到 <see cref="StringBuilder"/>
        /// </summary>
        /// <param name="builder">目标 <see cref="StringBuilder"/></param>
        /// <param name="parameter">参数信息</param>
        /// <returns>添加了格式化参数信息的 <see cref="StringBuilder"/></returns>
        public static StringBuilder Format(this StringBuilder builder, ParameterInfo parameter)
        {
            ThrowHelpers.ThrowIfArgumentNull(parameter);
            return builder.FormatParameter(parameter);
        }

        /// <summary>
        /// 将参数信息数组格式化到 <see cref="StringBuilder"/>
        /// </summary>
        /// <param name="builder">目标 <see cref="StringBuilder"/></param>
        /// <param name="parameters">参数信息数组</param>
        /// <returns>添加了格式化参数信息数组的 <see cref="StringBuilder"/>。</returns>
        public static StringBuilder Format(this StringBuilder builder, scoped ReadOnlySpan<ParameterInfo> parameters)
        {
            return builder.FormatParameters(parameters);
        }

        /// <summary>
        /// 将委托格式化到 <see cref="StringBuilder"/>
        /// </summary>
        /// <param name="builder">目标 <see cref="StringBuilder"/></param>
        /// <param name="delegate">委托对象</param>
        /// <returns>添加了格式化委托信息的 <see cref="StringBuilder"/>。</returns>
        public static StringBuilder Format(this StringBuilder builder, Delegate @delegate)
        {
            ThrowHelpers.ThrowIfArgumentNull(@delegate);
            return builder.FormatDelegate(@delegate);
        }

        /// <summary>
        /// 将方法格式化到 <see cref="StringBuilder"/>
        /// </summary>
        /// <param name="builder">目标 <see cref="StringBuilder"/></param>
        /// <param name="method">方法信息</param>
        /// <returns>添加了格式化方法信息的 <see cref="StringBuilder"/>。</returns>
        public static StringBuilder Format(this StringBuilder builder, MethodInfo method)
        {
            ThrowHelpers.ThrowIfArgumentNull(method);
            return builder.FormatMethod(method);
        }

        /// <summary>
        /// 将成员格式化到 <see cref="StringBuilder"/>
        /// </summary>
        /// <param name="builder">目标 <see cref="StringBuilder"/></param>
        /// <param name="member">成员信息</param>
        /// <returns>添加了格式化成员信息的 <see cref="StringBuilder"/>。</returns>
        public static StringBuilder Format(this StringBuilder builder, MemberInfo member)
        {
            ThrowHelpers.ThrowIfArgumentNull(member);
            return builder.FormatMember(member);
        }

        #endregion


        //------------------------------------------------------
        //
        //  Debugger Methods
        //
        //------------------------------------------------------

        #region Debugger Methods

        /// <summary>
        /// 将对象转储到日志输出
        /// </summary>
        /// <param name="obj">要转换的对象</param>
        /// <param name="subject">日志主语</param>
        /// <returns>返回传入的 <paramref name="obj"/>。</returns>
        /// <remarks>此方法主要在调试时使用，以便在开发过程中查看对象的当前状态。</remarks>
        public static T Dump<T>(this T obj, [CallerMemberName] string? subject = null)
        {
            var builder = new StringBuilder(128)
                .Append('[')
                .Append(string.IsNullOrWhiteSpace(subject) ? DateTime.Now.ToString("HH:mm:ss.fff") : subject)
                .Append("] ")
                .Dump(obj);

            string message = builder.ToString();

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
        /// <remarks>此方法主要在调试时使用，以便在开发过程中查看对象的当前状态。</remarks>
        public static T Dump<T>(this T obj, StackTrace stackTrace, [CallerMemberName] string? subject = null)
        {
            ThrowHelpers.ThrowIfArgumentNull(stackTrace);

            var builder = new StringBuilder(128)
                .Append('[')
                .Append(string.IsNullOrWhiteSpace(subject) ? DateTime.Now.ToString("HH:mm:ss.fff") : subject)
                .Append("] ")
                .Dump(obj)
                .AppendLine()
                .Append(stackTrace.ToString());

            string message = builder.ToString();

            Console.WriteLine(message);
            Debug.WriteLine(message);
            Trace.WriteLine(message);

            return obj;
        }

        private static StringBuilder Dump<T>(this StringBuilder builder, T obj)
        {
            if (obj is string || obj is not IEnumerable collection)
            {
                builder.Format(obj);
            }
            else
            {
                builder.Append('[');
                bool isFirst = true;

                foreach (var item in collection)
                {
                    if (!isFirst)
                    {
                        builder.Append(", ");
                    }

                    if (item is string || item is not IEnumerable)
                    {
                        builder.Format(item);
                    }
                    else
                    {
                        builder.Append("[...]");
                    }

                    isFirst = false;
                }

                builder.Append(']');
            }

            return builder;
        }

        /// <summary>
        /// 将对象的详细详细按格式转储到日志输出
        /// </summary>
        /// <param name="obj">要转换的对象</param>
        /// <param name="subject">日志主语</param>
        /// <returns>返回传入的 <paramref name="obj"/>。</returns>
        /// <remarks>此方法主要在调试时使用，以便在开发过程中查看对象的当前状态。</remarks>
        public static T? Inspect<T>(this T? obj, [CallerMemberName] string? subject = null)
        {
            var builder = new StringBuilder(128)
                .Append('[')
                .Append(subject ?? new StackFrame(1).ToString() ?? Null)
                .Append(']')
                .Inspect(obj, new StackTrace(true));

            string message = builder.ToString();

            Console.WriteLine(message);
            Debug.WriteLine(message);
            Trace.WriteLine(message);

            return obj;
        }

        private static StringBuilder Inspect<T>(this StringBuilder builder, T? obj, StackTrace stackTrace)
        {
            builder.AppendLine(" --- Inspect ---")
                .Append('{')
                .AppendLine()
                .Append("  Value: ")
                .Dump(obj)
                .AppendLine()
                .Append("  Type: ");

            if (typeof(T).IsValueType)
            {
                builder.Format(typeof(T))
                    .AppendLine();
            }
            else if (obj is not null)
            {
                builder.Format(obj.GetType())
                    .AppendLine();
            }
            else
            {
                builder.AppendLine($" Type: {Null}");
            }

            if (stackTrace.FrameCount is not 0)
            {
                builder.AppendLine("  StackTrace:")
                    .Append(stackTrace.ToString());
            }

            return builder.Append('}');
        }

        #endregion


        //------------------------------------------------------
        //
        //  Private Format Helpers
        //
        //------------------------------------------------------

        #region Private Format Helpers

        private static StringBuilder FormatDeclaringType(this StringBuilder builder, MemberInfo member, bool scoped = true)
        {
#if !NETSTANDARD2_0
            if (member is DynamicMethod)
            {
                return builder.Append("[dynamic] ");
            }
#endif
            var declaringType = member.DeclaringType;

            if (declaringType is null)
            {
                return builder.Append("[global] ");
            }

            if (member.MemberType is MemberTypes.Constructor)
            {
                return builder.FormatType(declaringType, scoped);
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
                    var constructor = (ConstructorInfo)member;

                    return builder
                        .FormatType(typeof(void))
                        .Append(' ')
                        .FormatDeclaringType(member)
                        .Append(constructor.IsStatic ? ".cctor(" : ".ctor(")
                        .FormatParameters(constructor.GetParameters())
                        .Append(')');
                case MemberTypes.Event:
                case MemberTypes.Field:
                    return builder
                        .FormatDeclaringType(member)
                        .Append(member.Name);
                case MemberTypes.Property:
                    var parameters = ((PropertyInfo)member).GetIndexParameters();

                    if (parameters.Length < 1)
                    {
                        return builder
                            .FormatDeclaringType(member)
                            .Append(member.Name);
                    }
                    else
                    {
                        return builder
                            .FormatDeclaringType(member)
                            .Append('[')
                            .FormatParameters(parameters)
                            .Append(']');
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
                    .Append("[delegate]->")
                    .FormatType(@delegate.GetType()).Append('(')
                    .FormatMethod(@delegate.Method).Append(')');
            }
            else
            {
                @delegate = invocationList[0];

                builder
                    .AppendLine("[multicast delegate]->{")
                    .Append("\t[delegate]->")
                    .FormatType(@delegate.GetType()).Append('(')
                    .FormatMethod(@delegate.Method).Append(')');

                for (int i = 1; i < invocationList.Length; i++)
                {
                    builder.AppendLine().Append('\t').FormatDelegate(invocationList[i]);
                }

                builder.AppendLine().Append('}');
            }

            return builder;
        }

        private static StringBuilder FormatMethod(this StringBuilder builder, MethodInfo method)
        {
            builder.FormatType(method.ReturnType)
                .Append(' ')
                .FormatDeclaringType(method)
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

        private static StringBuilder FormatParameter(this StringBuilder builder, ParameterInfo parameter)
        {
            Type parameterType = parameter.ParameterType;

            switch (parameter.Attributes & (ParameterAttributes.In | ParameterAttributes.Out))
            {
                case ParameterAttributes.In | ParameterAttributes.Out:
                    builder.Append("ref ");
                    parameterType = parameterType.GetElementType()!;
                    break;
                case ParameterAttributes.In:
                    builder.Append("in ");
                    parameterType = parameterType.GetElementType()!;
                    break;
                case ParameterAttributes.Out:
                    builder.Append("out ");
                    parameterType = parameterType.GetElementType()!;
                    break;
                default:
                    if (parameterType.IsByRef)
                    {
                        builder.Append("ref ");
                        parameterType = parameterType.GetElementType()!;
                    }
                    break;
            }

            builder.FormatType(parameterType);

            if (!string.IsNullOrEmpty(parameter.Name))
            {
                builder.Append(' ').Append(parameter.Name);
            }

            return builder;
        }

        private static StringBuilder FormatParameters(this StringBuilder builder, scoped ReadOnlySpan<ParameterInfo> parameters)
        {
            if (parameters.Length is 0)
            {
                return builder;
            }

            builder.FormatParameter(parameters[0]);

            for (int i = 1; i < parameters.Length; i++)
            {
                builder.Append(", ").FormatParameter(parameters[i]);
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
                            return builder
                                .FormatType(underlyingType, scoped)
                                .Append('?');
                        }
                    }

                    if (scoped)
                    {
                        builder.Append(type.Namespace).Append('.');
                    }

                    if (type.IsNested)
                    {
                        builder
                            .FormatType(type.DeclaringType!, scoped: false)
                            .Append('+');
                    }

                    builder.Append(type.Name.TrimEnd(GenericTypeNameTrimChars)).Append('<');

                    var genericArguments = type.GetGenericArguments();
                    builder.FormatType(genericArguments[0], scoped);

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
                            builder
                                .FormatType(type.DeclaringType!, scoped: false)
                                .Append('+');
                        }
                    }

                    return builder.Append(type.Name);
                }
            }
            else
            {
                builder.FormatType(elementType);

                if (type.IsArray)
                {
                    var rank = type.GetArrayRank();

                    if (rank > 1)
                    {
                        builder
                            .Append('[')
                            .Append(new string(',', rank - 1))
                            .Append(']');
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

        private static StringBuilder FormatTypes(this StringBuilder builder, scoped ReadOnlySpan<Type> types)
        {
            if (types.Length < 1)
            {
                return builder;
            }

            builder.FormatType(types[0]);

            for (int i = 1; i < types.Length; i++)
            {
                builder.Append(", ").FormatType(types[i]);
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


        private static readonly char[] GenericTypeNameTrimChars =
            ['`', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'];
    }
}
