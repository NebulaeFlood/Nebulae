using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Nebulae.Reflection
{
    /// <summary>
    /// 提供反射操作的扩展方法
    /// </summary>
    public static class Reflector
    {
        /// <summary>
        /// 默认搜索 <see cref="BindingFlags"/>
        /// </summary>
        public const BindingFlags DefaultLookup =
            BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic;


        /// <summary>
        /// 判断参数类型与指定类型是否兼容
        /// </summary>
        /// <param name="parameter">参数类型</param>
        /// <param name="cadidate">目标类型</param>
        /// <returns>若 <paramref name="cadidate"/> 与参数类型兼容，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        public static bool IsCompatible(Type parameter, Type cadidate)
        {
            if (parameter == cadidate)
            {
                return true;
            }

            if (parameter.IsValueType || cadidate.IsValueType)
            {
                return false;
            }

            return parameter.IsAssignableFrom(cadidate);
        }

        /// <summary>
        /// 判断类型是否为可空类型
        /// </summary>
        /// <param name="type">要判断的类型</param>
        /// <returns>若反对的类型为可空类型，返回 <see langword="true"/>；反之则返回 <see langword="false"/>。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullable(this Type type)
        {
            return type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
    }

    /// <summary>
    /// 定义指定类型的成员访问委托
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
#if NET9_0_OR_GREATER
    public static class Reflector<T> where T : allows ref struct
#else
    public static class Reflector<T>
#endif
    {
        /// <summary>
        /// 表示用于获取成员值的方法
        /// </summary>
        /// <typeparam name="TValue">成员类型</typeparam>
        /// <param name="target">目标对象</param>
        /// <returns><paramref name="target"/> 中目标成员的值。</returns>
#if NET9_0_OR_GREATER
        public delegate TValue Get<out TValue>(T target) where TValue : allows ref struct;
#else
        public delegate TValue Get<out TValue>(T target);
#endif

        /// <summary>
        /// 表示用于设置成员值的方法
        /// </summary>
        /// <typeparam name="TValue">成员类型</typeparam>
        /// <param name="target">目标对象</param>
        /// <param name="value">要设置给目标成员的值</param>
#if NET9_0_OR_GREATER
        public delegate void Set<in TValue>(T target, TValue value) where TValue : allows ref struct;
#else
        public delegate void Set<in TValue>(T target, TValue value);
#endif

        /// <summary>
        /// 表示用于获取成员引用的方法
        /// </summary>
        /// <typeparam name="TValue">成员类型</typeparam>
        /// <param name="target">目标对象</param>
        /// <returns><paramref name="target"/> 中目标成员的引用。</returns>
#if NET9_0_OR_GREATER
        public delegate ref TValue Ref<TValue>(T target) where TValue : allows ref struct;
#else
        public delegate ref TValue Ref<TValue>(T target);
#endif

        /// <summary>
        /// 定义指定类型的成员访问委托
        /// </summary>
        /// <remarks>提供按引用传递目标对象的委托。</remarks>
        public static class ByRef
        {
            /// <summary>
            /// 表示用于获取成员值的方法
            /// </summary>
            /// <typeparam name="TValue">成员类型</typeparam>
            /// <param name="target">目标对象</param>
            /// <returns><paramref name="target"/> 中目标成员的值。</returns>
#if NET9_0_OR_GREATER
            public delegate TValue Get<out TValue>(in T target) where TValue : allows ref struct;
#else
            public delegate TValue Get<out TValue>(in T target);
#endif

            /// <summary>
            /// 表示用于设置成员值的方法
            /// </summary>
            /// <typeparam name="TValue">成员类型</typeparam>
            /// <param name="target">目标对象</param>
            /// <param name="value">要设置给目标成员的值</param>
#if NET9_0_OR_GREATER
            public delegate void Set<in TValue>(in T target, TValue value) where TValue : allows ref struct;
#else
            public delegate void Set<in TValue>(in T target, TValue value);
#endif

            /// <summary>
            /// 表示用于获取成员引用的方法
            /// </summary>
            /// <typeparam name="TValue">成员类型</typeparam>
            /// <param name="target">目标对象</param>
            /// <returns><paramref name="target"/> 中目标成员的引用。</returns>
#if NET9_0_OR_GREATER
            public delegate ref TValue Ref<TValue>(in T target) where TValue : allows ref struct;
#else
            public delegate ref TValue Ref<TValue>(in T target);
#endif
        }

        /// <summary>
        /// 定义指定类型的成员访问委托
        /// </summary>
        /// <remarks>提供无需传递目标对象的委托。</remarks>
        public static class Close
        {
            /// <summary>
            /// 表示用于获取成员值的方法
            /// </summary>
            /// <typeparam name="TValue">成员类型</typeparam>
            /// <returns>目标成员的值。</returns>
#if NET9_0_OR_GREATER
            public delegate TValue Get<out TValue>() where TValue : allows ref struct;
#else
            public delegate TValue Get<out TValue>();
#endif

            /// <summary>
            /// 表示用于设置成员值的方法
            /// </summary>
            /// <typeparam name="TValue">成员类型</typeparam>
            /// <param name="value">要设置给目标成员的值</param>
#if NET9_0_OR_GREATER
            public delegate void Set<in TValue>(TValue value) where TValue : allows ref struct;
#else
            public delegate void Set<in TValue>(TValue value);
#endif

            /// <summary>
            /// 表示用于获取成员引用的方法
            /// </summary>
            /// <typeparam name="TValue">成员类型</typeparam>
            /// <returns>目标成员的引用。</returns>
#if NET9_0_OR_GREATER
            public delegate ref TValue Ref<TValue>() where TValue : allows ref struct;
#else
            public delegate ref TValue Ref<TValue>();
#endif
        }
    }
}
