using System;

namespace Nebulae.Runtime.Emit.Inline
{
#if NET10_0_OR_GREATER
#pragma warning disable CA1822
#endif
#pragma warning disable IDE0060

    /// <summary>
    /// 类型引用
    /// </summary>
    [Reference(ReferenceType.Type)]
    public sealed class TypeRef
    {
        private TypeRef() { }


        /// <summary>
        /// 获取类型构造函数的引用
        /// </summary>
        /// <param name="parameterTypes">参数类型</param>
        /// <returns>表示构造函数引用的 <see cref="MethodRef"/>。</returns>
        /// <remarks>使用 <see cref="GenericRef"/> 匹配目标函数参数中的泛型参数声明。</remarks>
        [Reference(ReferenceType.Constructor)]
        public MethodRef Constructor(params Type[] parameterTypes)
        {
            return IL.Throw<MethodRef>();
        }

        /// <summary>
        /// 获取指定名称的事件的引用
        /// </summary>
        /// <param name="name">事件名称</param>
        /// <returns>表示事件引用的 <see cref="EventRef"/>。</returns>
        [Reference(ReferenceType.Event)]
        public EventRef Event(string name)
        {
            return IL.Throw<EventRef>();
        }

        /// <summary>
        /// 获取指定名称的字段的引用
        /// </summary>
        /// <param name="name">字段名称</param>
        /// <returns>表示字段引用的 <see cref="FieldRef"/>。</returns>
        [Reference(ReferenceType.Field)]
        public FieldRef Field(string name)
        {
            return IL.Throw<FieldRef>();
        }

        /// <summary>
        /// 获取指定参数类型的索引器的引用
        /// </summary>
        /// <param name="parameterTypes">参数类型</param>
        /// <returns>表示索引器引用的 <see cref="IndexerRef"/>。</returns>
        [Reference(ReferenceType.Indexer)]
        public IndexerRef Indexer(params Type[] parameterTypes)
        {
            return IL.Throw<IndexerRef>();
        }

        /// <summary>
        /// 获取指定名称的方法的引用
        /// </summary>
        /// <param name="name">方法名称</param>
        /// <returns>表示方法引用的 <see cref="MethodRef"/>。</returns>
        [Reference(ReferenceType.Method)]
        public MethodRef Method(string name)
        {
            return IL.Throw<MethodRef>();
        }

        /// <summary>
        /// 获取指定名称和参数类型的方法的引用
        /// </summary>
        /// <param name="name">方法名称</param>
        /// <param name="parameterTypes">参数类型</param>
        /// <returns>表示方法引用的 <see cref="MethodRef"/>。</returns>
        /// <remarks>使用 <see cref="GenericRef"/> 匹配目标中的泛型参数声明。</remarks>
        [Reference(ReferenceType.Method)]
        public MethodRef Method(string name, params Type[] parameterTypes)
        {
            return IL.Throw<MethodRef>();
        }

        /// <summary>
        /// 获取指定名称、返回类型和参数类型的方法的引用
        /// </summary>
        /// <param name="name">方法名称</param>
        /// <param name="returnType">返回类型</param>
        /// <param name="parameterTypes">参数类型</param>
        /// <returns>表示方法引用的 <see cref="MethodRef"/>。</returns>
        /// <remarks>使用 <see cref="GenericRef"/> 匹配目标中的泛型参数声明。</remarks>
        [Reference(ReferenceType.Method)]
        public MethodRef Method(string name, Type returnType, params Type[] parameterTypes)
        {
            return IL.Throw<MethodRef>();
        }

        /// <summary>
        /// 获取指定名称的属性的引用
        /// </summary>
        /// <param name="name">属性名称</param>
        /// <returns>表示属性引用的 <see cref="PropertyRef"/>。</returns>
        [Reference(ReferenceType.Property)]
        public PropertyRef Property(string name)
        {
            return IL.Throw<PropertyRef>();
        }
    }
}
