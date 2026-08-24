using Nebulae.Diagnostics;
using System;
using System.Reflection;

namespace Nebulae.Reflection.Extensions
{
    /// <summary>
    /// 提供 <see cref="Type"/> 的拓展方法
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// 搜索指定的构造函数
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <param name="parameterTypes">函数参数类型</param>
        /// <returns>表示指定构造函数的 <see cref="ConstructorInfo"/>；若未找到，则返回 <see langword="null"/>。</returns>
        public static ConstructorInfo? Constructor(this Type type, params Type[] parameterTypes)
        {
            return type.GetConstructor(Reflector.DefaultLookup & ~BindingFlags.Static, binder: null, parameterTypes, modifiers: null);
        }

        /// <summary>
        /// 搜索指定的事件
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <param name="name">事件名称</param>
        /// <returns>表示指定名称事件的 <see cref="EventInfo"/>；若未找到，则返回 <see langword="null"/>。</returns>
        public static EventInfo? Event(this Type type, string name)
        {
            return type.GetEvent(name, Reflector.DefaultLookup);
        }

        /// <summary>
        /// 搜索指定的字段
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <param name="name">字段名称</param>
        /// <returns>表示指定名称字段的 <see cref="FieldInfo"/>；若未找到，则返回 <see langword="null"/>。</returns>
        public static FieldInfo? Field(this Type type, string name)
        {
            return type.GetField(name, Reflector.DefaultLookup);
        }

        /// <summary>
        /// 搜索指定的索引器
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <returns>表示索引器的 <see cref="PropertyInfo"/>；若未找到，则返回 <see langword="null"/>。</returns>
        public static PropertyInfo? Indexer(this Type type)
        {
            var properties = type.GetProperties(Reflector.DefaultLookup);

            for (int i = 0; i < properties.Length; i++)
            {
                var propertyInfo = properties[i];
                var parameters = propertyInfo.GetIndexParameters();

                if (parameters.Length > 0)
                {
                    return propertyInfo;
                }
            }

            return null;
        }

        /// <summary>
        /// 搜索指定的索引器
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <param name="returnType">索引器的返回类型</param>
        /// <returns>表示索引器的 <see cref="PropertyInfo"/>；若未找到，则返回 <see langword="null"/>。</returns>
        public static PropertyInfo? Indexer(this Type type, Type returnType)
        {
            ThrowHelpers.ThrowIfArgumentNull(returnType);

            var properties = type.GetProperties(Reflector.DefaultLookup);

            for (int i = 0; i < properties.Length; i++)
            {
                var propertyInfo = properties[i];
                var parameters = propertyInfo.GetIndexParameters();

                if (parameters.Length > 0
                    && propertyInfo.PropertyType == returnType)
                {
                    return propertyInfo;
                }
            }

            return null;
        }

        /// <summary>
        /// 搜索指定的索引器
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <param name="parameterTypes">索引器参数类型</param>
        /// <returns>表示索引器的 <see cref="PropertyInfo"/>；若未找到，则返回 <see langword="null"/>。</returns>
        public static PropertyInfo? Indexer(this Type type, params Type[]? parameterTypes)
        {
            ThrowHelpers.ThrowIfArgumentNull(parameterTypes);

            var properties = type.GetProperties(Reflector.DefaultLookup);

            for (int i = 0; i < properties.Length; i++)
            {
                var propertyInfo = properties[i];
                var parameters = propertyInfo.GetIndexParameters();

                if (parameters.Length > 0
                    && parameters.SequenceEqual(parameterTypes))
                {
                    return propertyInfo;
                }
            }

            return null;
        }

        /// <summary>
        /// 搜索指定的索引器
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <param name="returnType">索引器返回类型</param>
        /// <param name="parameterTypes">索引器参数类型</param>
        /// <returns>表示索引器的 <see cref="PropertyInfo"/>；若未找到，则返回 <see langword="null"/>。</returns>
        public static PropertyInfo? Indexer(this Type type, Type returnType, params Type[] parameterTypes)
        {
            ThrowHelpers.ThrowIfArgumentNull(returnType);
            ThrowHelpers.ThrowIfArgumentNull(parameterTypes);

            var properties = type.GetProperties(Reflector.DefaultLookup);

            for (int i = 0; i < properties.Length; i++)
            {
                var propertyInfo = properties[i];
                var parameters = propertyInfo.GetIndexParameters();

                if (parameters.Length > 0
                    && propertyInfo.PropertyType == returnType
                    && parameters.SequenceEqual(parameterTypes))
                {
                    return propertyInfo;
                }
            }

            return null;
        }

        /// <summary>
        /// 搜索指定的方法
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <param name="name">方法名称</param>
        /// <returns>表示指定名称方法的 <see cref="MethodInfo"/>；若未找到，则返回 <see langword="null"/>。</returns>
        public static MethodInfo? Method(this Type type, string name)
        {
            return type.GetMethod(name, Reflector.DefaultLookup);
        }

        /// <summary>
        /// 搜索指定的方法
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <param name="name">方法名称</param>
        /// <param name="parameterTypes">方法参数类型</param>
        /// <returns>表示指定名称方法的 <see cref="MethodInfo"/>；若未找到，则返回 <see langword="null"/>。</returns>
        public static MethodInfo? Method(this Type type, string name, params Type[] parameterTypes)
        {
            return type.GetMethod(
                name,
                Reflector.DefaultLookup,
                binder: null,
                parameterTypes,
                modifiers: null);
        }


        private static bool SequenceEqual(this ParameterInfo[] left, Type[] right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i].ParameterType != right[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
