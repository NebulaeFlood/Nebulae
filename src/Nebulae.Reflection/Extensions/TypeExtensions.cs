using System;
using System.Collections.Generic;
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
            return type.GetConstructor(
                Reflector.DefaultLookup & ~BindingFlags.Static,
                binder: null,
                parameterTypes,
                modifiers: null);
        }

        /// <summary>
        /// 搜索指定的构造函数
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <param name="flags">搜索标志</param>
        /// <param name="parameterTypes">函数参数类型</param>
        /// <returns>表示指定构造函数的 <see cref="ConstructorInfo"/>；若未找到，则返回 <see langword="null"/>。</returns>
        public static ConstructorInfo? Constructor(this Type type, BindingFlags flags, params Type[] parameterTypes)
        {
            return type.GetConstructor(
                flags,
                binder: null,
                parameterTypes,
                modifiers: null);
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
        /// 搜索指定的事件
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <param name="name">事件名称</param>
        /// <param name="flags">搜索标志</param>
        /// <returns>表示指定名称事件的 <see cref="EventInfo"/>；若未找到，则返回 <see langword="null"/>。</returns>
        public static EventInfo? Event(this Type type, string name, BindingFlags flags)
        {
            return type.GetEvent(name, flags);
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
        /// 搜索指定的字段
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <param name="name">字段名称</param>
        /// <param name="flags">搜索标志</param>
        /// <returns>表示指定名称字段的 <see cref="FieldInfo"/>；若未找到，则返回 <see langword="null"/>。</returns>
        public static FieldInfo? Field(this Type type, string name, BindingFlags flags)
        {
            return type.GetField(name, flags);
        }

        /// <summary>
        /// 搜索指定的索引器
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <param name="parameterTypes">索引器参数类型</param>
        /// <returns>表示指定索引器的 <see cref="PropertyInfo"/>；若未找到，则返回 <see langword="null"/>。</returns>
        /// <remarks>仅支持搜索<b>遵循标准 C# 编译器成员生成约定</b>的索引器。</remarks>
        public static PropertyInfo? Indexer(this Type type, params Type[] parameterTypes)
        {
            const BindingFlags Lookup = BindingFlags.Instance |
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            IList<CustomAttributeData> attributes = type.GetCustomAttributesData();
            int count = attributes.Count;

            for (int i = 0; i < count; i++)
            {
                CustomAttributeData data = attributes[i];

                if (data.AttributeType != typeof(DefaultMemberAttribute))
                {
                    continue;
                }

                if (data.ConstructorArguments[0].Value is not string memberName)
                {
                    break;
                }

                return type.GetProperty(
                    memberName,
                    Lookup,
                    binder: null,
                    returnType: null,
                    parameterTypes,
                    modifiers: null)
                    ?? type.BaseType?.Indexer(parameterTypes);
            }

            return type.BaseType?.Indexer(parameterTypes);
        }

        /// <summary>
        /// 搜索指定的索引器
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <param name="flags">搜索标志</param>
        /// <param name="parameterTypes">索引器参数类型</param>
        /// <returns>表示指定索引器的 <see cref="PropertyInfo"/>；若未找到，则返回 <see langword="null"/>。</returns>
        public static PropertyInfo? Indexer(this Type type, BindingFlags flags, params Type[] parameterTypes)
        {
            IList<CustomAttributeData> attributes = type.GetCustomAttributesData();
            int count = attributes.Count;

            PropertyInfo? indexer = null;
            BindingFlags lookup = flags | BindingFlags.DeclaredOnly;

            for (int i = 0; i < count; i++)
            {
                CustomAttributeData data = attributes[i];

                if (data.AttributeType != typeof(DefaultMemberAttribute))
                {
                    continue;
                }

                if (data.ConstructorArguments[0].Value is not string memberName)
                {
                    break;
                }

                indexer = type.GetProperty(
                    memberName,
                    lookup,
                    binder: null,
                    returnType: null,
                    parameterTypes,
                    modifiers: null);
                break;
            }

            if (indexer is not null)
            {
                return indexer;
            }

            if ((flags & BindingFlags.DeclaredOnly) != 0)
            {
                return null;
            }

            return type.BaseType?.Indexer(flags, parameterTypes);
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

        /// <summary>
        /// 搜索指定的方法
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <param name="name">方法名称</param>
        /// <param name="flags">搜索标志</param>
        /// <param name="parameterTypes">方法参数类型</param>
        /// <returns>表示指定名称方法的 <see cref="MethodInfo"/>；若未找到，则返回 <see langword="null"/>。</returns>
        public static MethodInfo? Method(this Type type, string name, BindingFlags flags, params Type[] parameterTypes)
        {
            return type.GetMethod(
                name,
                flags,
                binder: null,
                parameterTypes,
                modifiers: null);
        }

        /// <summary>
        /// 搜索指定的属性
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <param name="name">属性名称</param>
        /// <returns>表示指定名称属性的 <see cref="PropertyInfo"/>；若未找到，则返回 <see langword="null"/>。</returns>
        public static PropertyInfo? Property(this Type type, string name)
        {
            return type.GetProperty(name, Reflector.DefaultLookup);
        }

        /// <summary>
        /// 搜索指定的属性
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <param name="name">属性名称</param>
        /// <param name="flags">搜索标志</param>
        /// <returns>表示指定名称属性的 <see cref="PropertyInfo"/>；若未找到，则返回 <see langword="null"/>。</returns>
        public static PropertyInfo? Property(this Type type, string name, BindingFlags flags)
        {
            return type.GetProperty(name, flags);
        }
    }
}
