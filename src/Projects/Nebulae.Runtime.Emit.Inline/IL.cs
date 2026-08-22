using System;
using System.Runtime.CompilerServices;

namespace Nebulae.Runtime.Emit.Inline
{
#pragma warning disable IDE0060

    /// <summary>
    /// 提供用于内联 IL 代码的占位方法和拓展方法
    /// </summary>
    [Reference(ReferenceType.Placeholder)]
    public static partial class IL
    {
        //------------------------------------------------------
        //
        //  Extended Instructions
        //
        //------------------------------------------------------

        #region Extended Instructions

        /// <summary>
        /// 获取表示当前方法的占位符未被正确内联时抛出的异常
        /// </summary>
        /// <returns>表示当前方法的占位符未被正确内联时抛出的异常。</returns>
        /// <remarks>
        /// <para>
        /// 此占位符必须以 <c>throw IL.Fail();</c> 的形式使用。
        /// </para>
        /// <para>
        /// 此占位符不会为重写后的代码生成控制流终止指令。
        /// </para>
        /// </remarks>
        [Placeholder(PlaceholderCode.Fail, isPrimitive: false)]
        public static InvalidProgramException Fail()
        {
            return new InvalidProgramException(PlaceholderMessage);
        }

        /// <summary>
        /// 定义一个标签
        /// </summary>
        /// <param name="name">标签名称</param>
        [Placeholder(PlaceholderCode.Label, PlaceholderOperand.String, isPrimitive: false)]
        public static void Label(string name)
        {
            IL.Throw();
        }

        /// <summary>
        /// 从栈顶弹出一个值
        /// </summary>
        /// <typeparam name="T">值的类型</typeparam>
        /// <returns>从栈顶弹出的值。</returns>
        [Placeholder(PlaceholderCode.Pop, isPrimitive: false)]
        public static T Pop<T>()
#if NET9_0_OR_GREATER
            where T : allows ref struct
#endif
        {
            return IL.Throw<T>();
        }

        /// <summary>
        /// 从栈顶弹出一个指针
        /// </summary>
        /// <returns>从栈顶弹出的指针。</returns>
        /// <remarks><b>此指令并不会自动将栈顶值转换为指针。</b></remarks>
        [Placeholder(PlaceholderCode.Pop, isPrimitive: false)]
        public static unsafe void* PopPointer()
        {
            throw new InvalidProgramException(PlaceholderMessage);
        }

        /// <summary>
        /// 从栈顶弹出一个指针
        /// </summary>
        /// <typeparam name="T">指针指向的类型</typeparam>
        /// <returns>从栈顶弹出的指针。</returns>
        /// <remarks><b>此指令并不会自动将栈顶值转换为指针。</b></remarks>
        [Placeholder(PlaceholderCode.Pop, isPrimitive: false)]
        public static unsafe T* PopPointer<T>()
            where T : unmanaged
        {
            throw new InvalidProgramException(PlaceholderMessage);
        }

        /// <summary>
        /// 从栈顶弹出一个引用
        /// </summary>
        /// <typeparam name="T">引用指向的类型</typeparam>
        /// <returns>从栈顶弹出的引用。</returns>
        /// <remarks><b>此指令并不会自动将栈顶值转换为引用。</b></remarks>
        [Placeholder(PlaceholderCode.Pop, isPrimitive: false)]
        public static ref T PopRef<T>()
#if NET9_0_OR_GREATER
            where T : allows ref struct
#endif
        {
            throw new InvalidProgramException(PlaceholderMessage);
        }

        /// <summary>
        /// 将指定值压入栈顶
        /// </summary>
        /// <typeparam name="T">值的类型</typeparam>
        /// <param name="value">要压入栈顶的值</param>
        [Placeholder(PlaceholderCode.Push, PlaceholderOperand.Value, isPrimitive: false)]
        public static void Push<T>(T value)
#if NET9_0_OR_GREATER
            where T : allows ref struct
#endif
        {
            IL.Throw();
        }

        /// <summary>
        /// 将指定指针压入栈顶
        /// </summary>
        /// <param name="pointer">要压入栈顶的指针</param>
        [Placeholder(PlaceholderCode.Push, PlaceholderOperand.Value, isPrimitive: false)]
        public static unsafe void Push(void* pointer)
        {
            IL.Throw();
        }

        /// <summary>
        /// 将指定指针压入栈顶
        /// </summary>
        /// <typeparam name="T">指针指向的类型</typeparam>
        /// <param name="pointer">要压入栈顶的指针</param>
        [Placeholder(PlaceholderCode.Push, PlaceholderOperand.Value, isPrimitive: false)]
        public static unsafe void Push<T>(T* pointer)
            where T : unmanaged
        {
            IL.Throw();
        }

        /// <summary>
        /// 将指定引用压入栈顶
        /// </summary>
        /// <typeparam name="T">引用指向的类型</typeparam>
        /// <param name="value">要压入栈顶的引用</param>
        [Placeholder(PlaceholderCode.Push, PlaceholderOperand.Value, isPrimitive: false)]
        public static void Push<T>(ref T value)
#if NET9_0_OR_GREATER
            where T : allows ref struct
#endif
        {
            IL.Throw();
        }

        /// <summary>
        /// 引用类型
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <returns>表示类型引用的 <see cref="TypeRef"/>。</returns>
        [Reference(ReferenceType.Type)]
        public static TypeRef Ref(Type type)
        {
            return Throw<TypeRef>();
        }

        /// <summary>
        /// 从当前方法返回
        /// </summary>
        /// <typeparam name="T">返回值的类型</typeparam>
        /// <remarks>
        /// <para>
        /// 此占位符必须以 <c><![CDATA[return IL.Ret<T>();]]></c> 的形式使用。
        /// </para>
        /// <para>
        /// 此占位符设计用于需要返回特定类型值的方法。
        /// </para>
        /// </remarks>
        [Placeholder(PlaceholderCode.Ret, isPrimitive: false)]
        public static T Ret<T>()
#if NET9_0_OR_GREATER
            where T : allows ref struct
#endif
        {
            return IL.Throw<T>();
        }

        #endregion


        //------------------------------------------------------
        //
        //  Internal Helpers
        //
        //------------------------------------------------------

        #region Internal Helpers

        private const string PlaceholderMessage =
            "An inline IL placeholder was executed because current method was not rewritten correctly.";


        internal static void Throw()
        {
            throw new InvalidProgramException(PlaceholderMessage);
        }

        internal static T Throw<T>()
#if NET9_0_OR_GREATER
            where T : allows ref struct
#endif
        {
            throw new InvalidProgramException(PlaceholderMessage);
        }

        #endregion
    }
}
