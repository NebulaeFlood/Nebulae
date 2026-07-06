using System;

namespace Nebulae.Runtime.Emit.Inline
{
#pragma warning disable IDE0060

    public static partial class IL
    {
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
        /// 弹出栈顶的 <see cref="RuntimeTypeHandle"/> 并将对应的 <see cref="Type"/> 压入栈顶
        /// </summary>
        [Placeholder(PlaceholderCode.Ldtype, isPrimitive: false)]
        public static void Ldtype()
        {
            IL.Throw();
        }

        /// <summary>
        /// 获取表示当前方法的占位符未被正确内联时抛出的异常
        /// </summary>
        /// <returns>表示当前方法的占位符未被正确内联时抛出的异常。</returns>
        /// <remarks>
        /// <para>
        /// 此占位符设计用于表示方法结尾，防止编译时要求方法必须有返回值。
        /// </para>
        /// <para>
        /// <b>此占位符后的所有代码将被移除。</b>
        /// </para>
        /// </remarks>
        [Placeholder(PlaceholderCode.Fail, isPrimitive: false)]
        public static InvalidProgramException Fail()
        {
            return new InvalidProgramException(PlaceholderMessage);
        }


        /// <summary>
        /// 提供用于内联 IL 代码的占位方法
        /// </summary>
        public static class Emit
        {
            /// <summary>
            /// 不执行任何操作
            /// </summary>
            [Placeholder(PlaceholderCode.Nop)]
            public static void Nop()
            {
                IL.Throw();
            }

            /// <summary>
            /// 向调试器发出断点信号
            /// </summary>
            [Placeholder(PlaceholderCode.Break)]
            public static void Break()
            {
                IL.Throw();
            }


            /// <summary>
            /// 将指定参数压入栈顶
            /// </summary>
            /// <typeparam name="T">参数类型</typeparam>
            /// <param name="argument">目标参数</param>
            [Placeholder(PlaceholderCode.Ldarg, PlaceholderOperand.Argument)]
            public static void Ldarg<T>(T argument)
#if NET9_0_OR_GREATER
                where T : allows ref struct
#endif
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定参数压入栈顶
            /// </summary>
            /// <typeparam name="T">参数类型</typeparam>
            /// <param name="argument">目标参数</param>
            [Placeholder(PlaceholderCode.Ldarg, PlaceholderOperand.Argument)]
            public static void Ldarg<T>(out T argument)
#if NET9_0_OR_GREATER
                where T : allows ref struct
#endif
            {
                argument = Throw<T>();
            }

            /// <summary>
            /// 将指定参数的地址压入栈顶
            /// </summary>
            /// <typeparam name="T">参数类型</typeparam>
            /// <param name="argument">目标参数</param>
            [Placeholder(PlaceholderCode.Ldarga, PlaceholderOperand.Argument)]
            public static void Ldarga<T>(T argument)
#if NET9_0_OR_GREATER
                where T : allows ref struct
#endif
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定参数的地址压入栈顶
            /// </summary>
            /// <typeparam name="T">参数类型</typeparam>
            /// <param name="argument">目标参数</param>
            [Placeholder(PlaceholderCode.Ldarga, PlaceholderOperand.Argument)]
            public static void Ldarga<T>(out T argument)
#if NET9_0_OR_GREATER
                where T : allows ref struct
#endif
            {
                argument = Throw<T>();
            }

            /// <summary>
            /// 弹出栈顶值并存入指定参数
            /// </summary>
            /// <typeparam name="T">参数类型</typeparam>
            /// <param name="argument">目标参数</param>
            [Placeholder(PlaceholderCode.Starg, PlaceholderOperand.Argument)]
            public static void Starg<T>(T argument)
#if NET9_0_OR_GREATER
                where T : allows ref struct
#endif
            {
                IL.Throw();
            }

            /// <summary>
            /// 弹出栈顶值并存入指定参数
            /// </summary>
            /// <typeparam name="T">参数类型</typeparam>
            /// <param name="argument">目标参数</param>
            [Placeholder(PlaceholderCode.Starg, PlaceholderOperand.Argument)]
            public static void Starg<T>(out T argument)
#if NET9_0_OR_GREATER
                where T : allows ref struct
#endif
            {
                argument = Throw<T>();
            }


            /// <summary>
            /// 将指定局部变量压入栈顶
            /// </summary>
            /// <typeparam name="T">局部变量类型</typeparam>
            /// <param name="local">目标局部变量</param>
            [Placeholder(PlaceholderCode.Ldloc, PlaceholderOperand.Variable)]
            public static void Ldloc<T>(T local)
#if NET9_0_OR_GREATER
                where T : allows ref struct
#endif
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定局部变量的地址压入栈顶
            /// </summary>
            /// <typeparam name="T">局部变量类型</typeparam>
            /// <param name="local">目标局部变量</param>
            [Placeholder(PlaceholderCode.Ldloca, PlaceholderOperand.Variable)]
            public static void Ldloca<T>(T local)
#if NET9_0_OR_GREATER
                where T : allows ref struct
#endif
            {
                IL.Throw();
            }

            /// <summary>
            /// 弹出栈顶值并存入指定局部变量
            /// </summary>
            /// <typeparam name="T">局部变量类型</typeparam>
            /// <param name="local">目标局部变量</param>
            [Placeholder(PlaceholderCode.Stloc, PlaceholderOperand.Variable)]
            public static void Stloc<T>(T local)
#if NET9_0_OR_GREATER
                where T : allows ref struct
#endif
            {
                IL.Throw();
            }

            /// <summary>
            /// 弹出栈顶值并存入指定局部变量
            /// </summary>
            /// <typeparam name="T">局部变量类型</typeparam>
            /// <param name="local">目标局部变量</param>
            [Placeholder(PlaceholderCode.Stloc, PlaceholderOperand.Variable)]
            public static void Stloc<T>(out T local)
#if NET9_0_OR_GREATER
                where T : allows ref struct
#endif
            {
                local = Throw<T>();
            }


            /// <summary>
            /// 将空引用压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Ldnull)]
            public static void Ldnull()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定的 32 位整数压入栈顶
            /// </summary>
            /// <param name="value">要加载的值</param>
            [Placeholder(PlaceholderCode.Ldc_I4, PlaceholderOperand.Int32)]
            public static void Ldc_I4(int value)
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定的 64 位整数压入栈顶
            /// </summary>
            /// <param name="value">要加载的值</param>
            [Placeholder(PlaceholderCode.Ldc_I8, PlaceholderOperand.Int64)]
            public static void Ldc_I8(long value)
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定的 32 位浮点数压入栈顶
            /// </summary>
            /// <param name="value">要加载的值</param>
            [Placeholder(PlaceholderCode.Ldc_R4, PlaceholderOperand.Single)]
            public static void Ldc_R4(float value)
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定的 64 位浮点数压入栈顶
            /// </summary>
            /// <param name="value">要加载的值</param>
            [Placeholder(PlaceholderCode.Ldc_R8, PlaceholderOperand.Double)]
            public static void Ldc_R8(double value)
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定字符串压入栈顶
            /// </summary>
            /// <param name="value">要加载的字符串</param>
            [Placeholder(PlaceholderCode.Ldstr, PlaceholderOperand.String)]
            public static void Ldstr(string value)
            {
                IL.Throw();
            }


            /// <summary>
            /// 复制栈顶值并将副本压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Dup)]
            public static void Dup()
            {
                IL.Throw();
            }

            /// <summary>
            /// 弹出栈顶值
            /// </summary>
            [Placeholder(PlaceholderCode.Pop)]
            public static void Pop()
            {
                IL.Throw();
            }


            /// <summary>
            /// 将指定实例字段的值压入栈顶
            /// </summary>
            /// <param name="field">目标字段</param>
            [Placeholder(PlaceholderCode.Ldfld, PlaceholderOperand.FieldRef)]
            public static void Ldfld(FieldRef field)
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定实例字段的地址压入栈顶
            /// </summary>
            /// <param name="field">目标字段</param>
            [Placeholder(PlaceholderCode.Ldflda, PlaceholderOperand.FieldRef)]
            public static void Ldflda(FieldRef field)
            {
                IL.Throw();
            }

            /// <summary>
            /// 弹出栈顶值并存入指定实例字段
            /// </summary>
            /// <param name="field">目标字段</param>
            [Placeholder(PlaceholderCode.Stfld, PlaceholderOperand.FieldRef)]
            public static void Stfld(FieldRef field)
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定静态字段的值压入栈顶
            /// </summary>
            /// <param name="field">目标字段</param>
            [Placeholder(PlaceholderCode.Ldsfld, PlaceholderOperand.FieldRef)]
            public static void Ldsfld(FieldRef field)
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定静态字段的地址压入栈顶
            /// </summary>
            /// <param name="field">目标字段</param>
            [Placeholder(PlaceholderCode.Ldsflda, PlaceholderOperand.FieldRef)]
            public static void Ldsflda(FieldRef field)
            {
                IL.Throw();
            }

            /// <summary>
            /// 弹出栈顶值并存入指定静态字段
            /// </summary>
            /// <param name="field">目标字段</param>
            [Placeholder(PlaceholderCode.Stsfld, PlaceholderOperand.FieldRef)]
            public static void Stsfld(FieldRef field)
            {
                IL.Throw();
            }


            /// <summary>
            /// 创建指定元素类型的一维零基数组并将其压入栈顶
            /// </summary>
            /// <param name="type">数组元素类型</param>
            [Placeholder(PlaceholderCode.Newarr, PlaceholderOperand.TypeRef)]
            public static void Newarr(Type type)
            {
                IL.Throw();
            }

            /// <summary>
            /// 弹出数组并将其长度压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Ldlen)]
            public static void Ldlen()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定类型的数组元素压入栈顶
            /// </summary>
            /// <param name="type">数组元素类型</param>
            [Placeholder(PlaceholderCode.Ldelem, PlaceholderOperand.TypeRef)]
            public static void Ldelem(Type type)
            {
                IL.Throw();
            }

            /// <summary>
            /// 将 <see cref="nint"/> 数组元素压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Ldelem_I)]
            public static void Ldelem_I()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将 <see cref="sbyte"/> 数组元素压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Ldelem_I1)]
            public static void Ldelem_I1()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将 <see cref="short"/> 数组元素压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Ldelem_I2)]
            public static void Ldelem_I2()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将 <see cref="int"/> 数组元素压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Ldelem_I4)]
            public static void Ldelem_I4()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将 <see cref="long"/> 数组元素压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Ldelem_I8)]
            public static void Ldelem_I8()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将 <see cref="byte"/> 数组元素压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Ldelem_U1)]
            public static void Ldelem_U1()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将 <see cref="ushort"/> 数组元素压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Ldelem_U2)]
            public static void Ldelem_U2()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将 <see cref="uint"/> 数组元素压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Ldelem_U4)]
            public static void Ldelem_U4()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将 <see cref="float"/> 数组元素压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Ldelem_R4)]
            public static void Ldelem_R4()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将 <see cref="double"/> 数组元素压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Ldelem_R8)]
            public static void Ldelem_R8()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将引用类型数组元素压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Ldelem_Ref)]
            public static void Ldelem_Ref()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定类型的数组元素地址压入栈顶
            /// </summary>
            /// <param name="type">数组元素类型</param>
            [Placeholder(PlaceholderCode.Ldelema, PlaceholderOperand.TypeRef)]
            public static void Ldelema(Type type)
            {
                IL.Throw();
            }

            /// <summary>
            /// 弹出栈顶值并存入指定类型的数组
            /// </summary>
            /// <param name="type">数组元素类型</param>
            [Placeholder(PlaceholderCode.Stelem, PlaceholderOperand.TypeRef)]
            public static void Stelem(Type type)
            {
                IL.Throw();
            }

            /// <summary>
            /// 弹出栈顶值并存入 <see cref="nint"/> 数组
            /// </summary>
            [Placeholder(PlaceholderCode.Stelem_I)]
            public static void Stelem_I()
            {
                IL.Throw();
            }

            /// <summary>
            /// 弹出栈顶值并存入 <see cref="sbyte"/> 数组
            /// </summary>
            [Placeholder(PlaceholderCode.Stelem_I1)]
            public static void Stelem_I1()
            {
                IL.Throw();
            }

            /// <summary>
            /// 弹出栈顶值并存入 <see cref="short"/> 数组
            /// </summary>
            [Placeholder(PlaceholderCode.Stelem_I2)]
            public static void Stelem_I2()
            {
                IL.Throw();
            }

            /// <summary>
            /// 弹出栈顶值并存入 <see cref="int"/> 数组
            /// </summary>
            [Placeholder(PlaceholderCode.Stelem_I4)]
            public static void Stelem_I4()
            {
                IL.Throw();
            }

            /// <summary>
            /// 弹出栈顶值并存入 <see cref="long"/> 数组
            /// </summary>
            [Placeholder(PlaceholderCode.Stelem_I8)]
            public static void Stelem_I8()
            {
                IL.Throw();
            }

            /// <summary>
            /// 弹出栈顶值并存入 <see cref="float"/> 数组
            /// </summary>
            [Placeholder(PlaceholderCode.Stelem_R4)]
            public static void Stelem_R4()
            {
                IL.Throw();
            }

            /// <summary>
            /// 弹出栈顶值并存入 <see cref="double"/> 数组
            /// </summary>
            [Placeholder(PlaceholderCode.Stelem_R8)]
            public static void Stelem_R8()
            {
                IL.Throw();
            }

            /// <summary>
            /// 弹出栈顶值并存入引用类型数组
            /// </summary>
            [Placeholder(PlaceholderCode.Stelem_Ref)]
            public static void Stelem_Ref()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定地址处的 <see cref="nint"/> 压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Ldind_I)]
            public static void Ldind_I()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定地址处的 <see cref="sbyte"/> 压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Ldind_I1)]
            public static void Ldind_I1()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定地址处的 <see cref="short"/> 压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Ldind_I2)]
            public static void Ldind_I2()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定地址处的 <see cref="int"/> 压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Ldind_I4)]
            public static void Ldind_I4()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定地址处的 <see cref="long"/> 压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Ldind_I8)]
            public static void Ldind_I8()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定地址处的 <see cref="byte"/> 压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Ldind_U1)]
            public static void Ldind_U1()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定地址处的 <see cref="ushort"/> 压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Ldind_U2)]
            public static void Ldind_U2()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定地址处的 <see cref="uint"/> 压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Ldind_U4)]
            public static void Ldind_U4()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定地址处的 <see cref="float"/> 压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Ldind_R4)]
            public static void Ldind_R4()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定地址处的 <see cref="double"/> 压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Ldind_R8)]
            public static void Ldind_R8()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定地址处的对象引用压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Ldind_Ref)]
            public static void Ldind_Ref()
            {
                IL.Throw();
            }

            /// <summary>
            /// 弹出栈顶的 <see cref="nint"/> 值并存入指定地址
            /// </summary>
            [Placeholder(PlaceholderCode.Stind_I)]
            public static void Stind_I()
            {
                IL.Throw();
            }

            /// <summary>
            /// 弹出栈顶的 <see cref="sbyte"/> 值并存入指定地址
            /// </summary>
            [Placeholder(PlaceholderCode.Stind_I1)]
            public static void Stind_I1()
            {
                IL.Throw();
            }

            /// <summary>
            /// 弹出栈顶的 <see cref="short"/> 值并存入指定地址
            /// </summary>
            [Placeholder(PlaceholderCode.Stind_I2)]
            public static void Stind_I2()
            {
                IL.Throw();
            }

            /// <summary>
            /// 弹出栈顶的 <see cref="int"/> 值并存入指定地址
            /// </summary>
            [Placeholder(PlaceholderCode.Stind_I4)]
            public static void Stind_I4()
            {
                IL.Throw();
            }

            /// <summary>
            /// 弹出栈顶的 <see cref="long"/> 值并存入指定地址
            /// </summary>
            [Placeholder(PlaceholderCode.Stind_I8)]
            public static void Stind_I8()
            {
                IL.Throw();
            }

            /// <summary>
            /// 弹出栈顶的 <see cref="float"/> 值并存入指定地址
            /// </summary>
            [Placeholder(PlaceholderCode.Stind_R4)]
            public static void Stind_R4()
            {
                IL.Throw();
            }

            /// <summary>
            /// 弹出栈顶的 <see cref="double"/> 值并存入指定地址
            /// </summary>
            [Placeholder(PlaceholderCode.Stind_R8)]
            public static void Stind_R8()
            {
                IL.Throw();
            }

            /// <summary>
            /// 弹出栈顶的对象引用并存入指定地址
            /// </summary>
            [Placeholder(PlaceholderCode.Stind_Ref)]
            public static void Stind_Ref()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定地址处的值类型对象复制到栈顶
            /// </summary>
            /// <param name="type">目标值类型</param>
            [Placeholder(PlaceholderCode.Ldobj, PlaceholderOperand.TypeRef)]
            public static void Ldobj(Type type)
            {
                IL.Throw();
            }

            /// <summary>
            /// 弹出栈顶值并复制到指定地址
            /// </summary>
            /// <param name="type">目标值类型</param>
            [Placeholder(PlaceholderCode.Stobj, PlaceholderOperand.TypeRef)]
            public static void Stobj(Type type)
            {
                IL.Throw();
            }

            /// <summary>
            /// 将一个地址处的值类型对象复制到另一个地址
            /// </summary>
            /// <param name="type">要复制的值类型</param>
            [Placeholder(PlaceholderCode.Cpobj, PlaceholderOperand.TypeRef)]
            public static void Cpobj(Type type)
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定地址处的值类型对象初始化为空值或零
            /// </summary>
            /// <param name="type">要初始化的值类型</param>
            [Placeholder(PlaceholderCode.Initobj, PlaceholderOperand.TypeRef)]
            public static void Initobj(Type type)
            {
                IL.Throw();
            }


            /// <summary>
            /// 调用指定方法
            /// </summary>
            /// <param name="method">目标方法</param>
            [Placeholder(PlaceholderCode.Call, PlaceholderOperand.MethodRef)]
            public static void Call(MethodRef method)
            {
                IL.Throw();
            }

            /// <summary>
            /// 对实例执行虚方法调用
            /// </summary>
            /// <param name="method">目标方法</param>
            [Placeholder(PlaceholderCode.Callvirt, PlaceholderOperand.MethodRef)]
            public static void Callvirt(MethodRef method)
            {
                IL.Throw();
            }

            /// <summary>
            /// 通过栈顶函数指针调用指定签名的方法
            /// </summary>
            /// <param name="method">用于生成调用签名的方法引用</param>
            [Placeholder(PlaceholderCode.Calli, PlaceholderOperand.Signature)]
            public static void Calli(MethodRef method)
            {
                IL.Throw();
            }

            /// <summary>
            /// 调用指定构造函数并将新对象压入栈顶
            /// </summary>
            /// <param name="method">目标构造函数</param>
            [Placeholder(PlaceholderCode.Newobj, PlaceholderOperand.MethodRef)]
            public static void Newobj(MethodRef method)
            {
                IL.Throw();
            }

            /// <summary>
            /// 将当前方法的执行转移到指定方法
            /// </summary>
            /// <param name="method">目标方法</param>
            [Placeholder(PlaceholderCode.Jmp, PlaceholderOperand.MethodRef)]
            public static void Jmp(MethodRef method)
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定静态方法的函数指针压入栈顶
            /// </summary>
            /// <param name="method">目标方法</param>
            [Placeholder(PlaceholderCode.Ldftn, PlaceholderOperand.MethodRef)]
            public static void Ldftn(MethodRef method)
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定实例虚方法的函数指针压入栈顶
            /// </summary>
            /// <param name="method">目标方法</param>
            [Placeholder(PlaceholderCode.Ldvirtftn, PlaceholderOperand.MethodRef)]
            public static void Ldvirtftn(MethodRef method)
            {
                IL.Throw();
            }

            /// <summary>
            /// 从当前方法返回
            /// </summary>
            [Placeholder(PlaceholderCode.Ret)]
            public static void Ret()
            {
                IL.Throw();
            }


            /// <summary>
            /// 无条件跳转到指定标签
            /// </summary>
            /// <param name="label">目标标签名称</param>
            [Placeholder(PlaceholderCode.Br, PlaceholderOperand.Branch)]
            public static void Br(string label)
            {
                IL.Throw();
            }

            /// <summary>
            /// 当栈顶值为零、空引用或 <see langword="false"/> 时跳转到指定标签
            /// </summary>
            /// <param name="label">目标标签名称</param>
            [Placeholder(PlaceholderCode.Brfalse, PlaceholderOperand.Branch)]
            public static void Brfalse(string label)
            {
                IL.Throw();
            }

            /// <summary>
            /// 当栈顶值非零、非空引用或为 <see langword="true"/> 时跳转到指定标签
            /// </summary>
            /// <param name="label">目标标签名称</param>
            [Placeholder(PlaceholderCode.Brtrue, PlaceholderOperand.Branch)]
            public static void Brtrue(string label)
            {
                IL.Throw();
            }

            /// <summary>
            /// 当栈顶两个值相等时跳转到指定标签
            /// </summary>
            /// <param name="label">目标标签名称</param>
            [Placeholder(PlaceholderCode.Beq, PlaceholderOperand.Branch)]
            public static void Beq(string label)
            {
                IL.Throw();
            }

            /// <summary>
            /// 当栈顶两个值不相等时跳转到指定标签
            /// </summary>
            /// <param name="label">目标标签名称</param>
            [Placeholder(PlaceholderCode.Bne_Un, PlaceholderOperand.Branch)]
            public static void Bne_Un(string label)
            {
                IL.Throw();
            }

            /// <summary>
            /// 当第一个值大于或等于第二个有符号值时跳转到指定标签
            /// </summary>
            /// <param name="label">目标标签名称</param>
            [Placeholder(PlaceholderCode.Bge, PlaceholderOperand.Branch)]
            public static void Bge(string label)
            {
                IL.Throw();
            }

            /// <summary>
            /// 当第一个值大于或等于第二个无符号值时跳转到指定标签
            /// </summary>
            /// <param name="label">目标标签名称</param>
            [Placeholder(PlaceholderCode.Bge_Un, PlaceholderOperand.Branch)]
            public static void Bge_Un(string label)
            {
                IL.Throw();
            }

            /// <summary>
            /// 当第一个值大于第二个有符号值时跳转到指定标签
            /// </summary>
            /// <param name="label">目标标签名称</param>
            [Placeholder(PlaceholderCode.Bgt, PlaceholderOperand.Branch)]
            public static void Bgt(string label)
            {
                IL.Throw();
            }

            /// <summary>
            /// 当第一个值大于第二个无符号值时跳转到指定标签
            /// </summary>
            /// <param name="label">目标标签名称</param>
            [Placeholder(PlaceholderCode.Bgt_Un, PlaceholderOperand.Branch)]
            public static void Bgt_Un(string label)
            {
                IL.Throw();
            }

            /// <summary>
            /// 当第一个值小于或等于第二个有符号值时跳转到指定标签
            /// </summary>
            /// <param name="label">目标标签名称</param>
            [Placeholder(PlaceholderCode.Ble, PlaceholderOperand.Branch)]
            public static void Ble(string label)
            {
                IL.Throw();
            }

            /// <summary>
            /// 当第一个值小于或等于第二个无符号值时跳转到指定标签
            /// </summary>
            /// <param name="label">目标标签名称</param>
            [Placeholder(PlaceholderCode.Ble_Un, PlaceholderOperand.Branch)]
            public static void Ble_Un(string label)
            {
                IL.Throw();
            }

            /// <summary>
            /// 当第一个值小于第二个有符号值时跳转到指定标签
            /// </summary>
            /// <param name="label">目标标签名称</param>
            [Placeholder(PlaceholderCode.Blt, PlaceholderOperand.Branch)]
            public static void Blt(string label)
            {
                IL.Throw();
            }

            /// <summary>
            /// 当第一个值小于第二个无符号值时跳转到指定标签
            /// </summary>
            /// <param name="label">目标标签名称</param>
            [Placeholder(PlaceholderCode.Blt_Un, PlaceholderOperand.Branch)]
            public static void Blt_Un(string label)
            {
                IL.Throw();
            }

            /// <summary>
            /// 根据栈顶索引跳转到指定标签
            /// </summary>
            /// <param name="labels">按索引排列的目标标签名称</param>
            [Placeholder(PlaceholderCode.Switch, PlaceholderOperand.Branches)]
            public static void Switch(params string[] labels)
            {
                IL.Throw();
            }

            /// <summary>
            /// 退出受保护区域并跳转到指定标签
            /// </summary>
            /// <param name="label">目标标签名称</param>
            [Placeholder(PlaceholderCode.Leave, PlaceholderOperand.Branch)]
            public static void Leave(string label)
            {
                IL.Throw();
            }


            /// <summary>
            /// 比较栈顶两个值是否相等并将结果压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Ceq)]
            public static void Ceq()
            {
                IL.Throw();
            }

            /// <summary>
            /// 比较第一个值是否大于第二个有符号值并将结果压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Cgt)]
            public static void Cgt()
            {
                IL.Throw();
            }

            /// <summary>
            /// 比较第一个值是否大于第二个无符号值并将结果压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Cgt_Un)]
            public static void Cgt_Un()
            {
                IL.Throw();
            }

            /// <summary>
            /// 比较第一个值是否小于第二个有符号值并将结果压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Clt)]
            public static void Clt()
            {
                IL.Throw();
            }

            /// <summary>
            /// 比较第一个值是否小于第二个无符号值并将结果压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Clt_Un)]
            public static void Clt_Un()
            {
                IL.Throw();
            }


            /// <summary>
            /// 将栈顶两个值相加并将结果压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Add)]
            public static void Add()
            {
                IL.Throw();
            }

            /// <summary>
            /// 以有符号整数执行溢出检查加法
            /// </summary>
            [Placeholder(PlaceholderCode.Add_Ovf)]
            public static void Add_Ovf()
            {
                IL.Throw();
            }

            /// <summary>
            /// 以无符号整数执行溢出检查加法
            /// </summary>
            [Placeholder(PlaceholderCode.Add_Ovf_Un)]
            public static void Add_Ovf_Un()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将第二个栈顶值减去栈顶值并将结果压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Sub)]
            public static void Sub()
            {
                IL.Throw();
            }

            /// <summary>
            /// 以有符号整数执行溢出检查减法
            /// </summary>
            [Placeholder(PlaceholderCode.Sub_Ovf)]
            public static void Sub_Ovf()
            {
                IL.Throw();
            }

            /// <summary>
            /// 以无符号整数执行溢出检查减法
            /// </summary>
            [Placeholder(PlaceholderCode.Sub_Ovf_Un)]
            public static void Sub_Ovf_Un()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将栈顶两个值相乘并将结果压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Mul)]
            public static void Mul()
            {
                IL.Throw();
            }

            /// <summary>
            /// 以有符号整数执行溢出检查乘法
            /// </summary>
            [Placeholder(PlaceholderCode.Mul_Ovf)]
            public static void Mul_Ovf()
            {
                IL.Throw();
            }

            /// <summary>
            /// 以无符号整数执行溢出检查乘法
            /// </summary>
            [Placeholder(PlaceholderCode.Mul_Ovf_Un)]
            public static void Mul_Ovf_Un()
            {
                IL.Throw();
            }

            /// <summary>
            /// 以有符号数或浮点数执行除法
            /// </summary>
            [Placeholder(PlaceholderCode.Div)]
            public static void Div()
            {
                IL.Throw();
            }

            /// <summary>
            /// 以无符号整数执行除法
            /// </summary>
            [Placeholder(PlaceholderCode.Div_Un)]
            public static void Div_Un()
            {
                IL.Throw();
            }

            /// <summary>
            /// 以有符号数计算除法余数
            /// </summary>
            [Placeholder(PlaceholderCode.Rem)]
            public static void Rem()
            {
                IL.Throw();
            }

            /// <summary>
            /// 以无符号整数计算除法余数
            /// </summary>
            [Placeholder(PlaceholderCode.Rem_Un)]
            public static void Rem_Un()
            {
                IL.Throw();
            }

            /// <summary>
            /// 对栈顶值取负并将结果压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Neg)]
            public static void Neg()
            {
                IL.Throw();
            }


            /// <summary>
            /// 对栈顶两个整数执行按位与运算
            /// </summary>
            [Placeholder(PlaceholderCode.And)]
            public static void And()
            {
                IL.Throw();
            }

            /// <summary>
            /// 对栈顶两个整数执行按位或运算
            /// </summary>
            [Placeholder(PlaceholderCode.Or)]
            public static void Or()
            {
                IL.Throw();
            }

            /// <summary>
            /// 对栈顶两个整数执行按位异或运算
            /// </summary>
            [Placeholder(PlaceholderCode.Xor)]
            public static void Xor()
            {
                IL.Throw();
            }

            /// <summary>
            /// 对栈顶整数执行按位取反运算
            /// </summary>
            [Placeholder(PlaceholderCode.Not)]
            public static void Not()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将整数左移指定的位数
            /// </summary>
            [Placeholder(PlaceholderCode.Shl)]
            public static void Shl()
            {
                IL.Throw();
            }

            /// <summary>
            /// 对有符号整数执行右移运算
            /// </summary>
            [Placeholder(PlaceholderCode.Shr)]
            public static void Shr()
            {
                IL.Throw();
            }

            /// <summary>
            /// 对无符号整数执行右移运算
            /// </summary>
            [Placeholder(PlaceholderCode.Shr_Un)]
            public static void Shr_Un()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将栈顶值转换为 <see cref="nint"/>
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_I)]
            public static void Conv_I()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将栈顶值转换为 <see cref="sbyte"/> 并将结果压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_I1)]
            public static void Conv_I1()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将栈顶值转换为 <see cref="short"/> 并将结果压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_I2)]
            public static void Conv_I2()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将栈顶值转换为 <see cref="int"/> 并将结果压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_I4)]
            public static void Conv_I4()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将栈顶值转换为 <see cref="long"/>
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_I8)]
            public static void Conv_I8()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将栈顶值转换为 <see cref="nuint"/>
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_U)]
            public static void Conv_U()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将栈顶值转换为 <see cref="byte"/>
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_U1)]
            public static void Conv_U1()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将栈顶值转换为 <see cref="ushort"/>
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_U2)]
            public static void Conv_U2()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将栈顶值转换为 <see cref="uint"/>
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_U4)]
            public static void Conv_U4()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将栈顶值转换为 <see cref="ulong"/>
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_U8)]
            public static void Conv_U8()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将栈顶无符号整数转换为 <see cref="float"/>
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_R_Un)]
            public static void Conv_R_Un()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将栈顶值转换为 <see cref="float"/>
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_R4)]
            public static void Conv_R4()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将栈顶值转换为 <see cref="double"/>
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_R8)]
            public static void Conv_R8()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将栈顶值转换为 <see cref="nint"/> 并执行溢出检查
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_Ovf_I)]
            public static void Conv_Ovf_I()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将栈顶值转换为 <see cref="sbyte"/> 并执行溢出检查
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_Ovf_I1)]
            public static void Conv_Ovf_I1()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将栈顶值转换为 <see cref="short"/> 并执行溢出检查
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_Ovf_I2)]
            public static void Conv_Ovf_I2()
            {
                IL.Throw();
            }

            /// <summary>
            ///  将栈顶值转换为 <see cref="int"/> 并执行溢出检查
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_Ovf_I4)]
            public static void Conv_Ovf_I4()
            {
                IL.Throw();
            }

            /// <summary>
            ///  将栈顶值转换为 <see cref="long"/> 并执行溢出检查
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_Ovf_I8)]
            public static void Conv_Ovf_I8()
            {
                IL.Throw();
            }

            /// <summary>
            ///  将栈顶值转换为 <see cref="nuint"/> 并执行溢出检查
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_Ovf_U)]
            public static void Conv_Ovf_U()
            {
                IL.Throw();
            }

            /// <summary>
            ///  将栈顶值转换为 <see cref="byte"/> 并执行溢出检查
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_Ovf_U1)]
            public static void Conv_Ovf_U1()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将栈顶值转换为 <see cref="ushort"/> 并执行溢出检查
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_Ovf_U2)]
            public static void Conv_Ovf_U2()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将栈顶值转换为 <see cref="uint"/> 并执行溢出检查
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_Ovf_U4)]
            public static void Conv_Ovf_U4()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将栈顶值转换为 <see cref="ulong"/> 并执行溢出检查
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_Ovf_U8)]
            public static void Conv_Ovf_U8()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将无符号栈顶值转换为 <see cref="nint"/> 并执行溢出检查
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_Ovf_I_Un)]
            public static void Conv_Ovf_I_Un()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将无符号栈顶值转换为 <see cref="sbyte"/> 并执行溢出检查
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_Ovf_I1_Un)]
            public static void Conv_Ovf_I1_Un()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将无符号栈顶值转换为 <see cref="short"/> 并执行溢出检查
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_Ovf_I2_Un)]
            public static void Conv_Ovf_I2_Un()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将无符号栈顶值转换为 <see cref="int"/> 并执行溢出检查
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_Ovf_I4_Un)]
            public static void Conv_Ovf_I4_Un()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将无符号栈顶值转换为 <see cref="long"/> 并执行溢出检查
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_Ovf_I8_Un)]
            public static void Conv_Ovf_I8_Un()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将无符号栈顶值转换为 <see cref="nuint"/> 并执行溢出检查
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_Ovf_U_Un)]
            public static void Conv_Ovf_U_Un()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将无符号栈顶值转换为 <see cref="byte"/> 并执行溢出检查
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_Ovf_U1_Un)]
            public static void Conv_Ovf_U1_Un()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将无符号栈顶值转换为 <see cref="ushort"/> 并执行溢出检查
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_Ovf_U2_Un)]
            public static void Conv_Ovf_U2_Un()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将无符号栈顶值转换为 <see cref="uint"/> 并执行溢出检查
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_Ovf_U4_Un)]
            public static void Conv_Ovf_U4_Un()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将无符号栈顶值转换为 <see cref="ulong"/> 并执行溢出检查
            /// </summary>
            [Placeholder(PlaceholderCode.Conv_Ovf_U8_Un)]
            public static void Conv_Ovf_U8_Un()
            {
                IL.Throw();
            }

            /// <summary>
            /// 检查栈顶浮点数是否为有限值
            /// </summary>
            [Placeholder(PlaceholderCode.Ckfinite)]
            public static void Ckfinite()
            {
                IL.Throw();
            }

            /// <summary>
            /// 装箱栈顶的指定类型值
            /// </summary>
            /// <param name="type">要装箱的值类型</param>
            [Placeholder(PlaceholderCode.Box, PlaceholderOperand.TypeRef)]
            public static void Box(Type type)
            {
                IL.Throw();
            }

            /// <summary>
            /// 将栈顶对象拆箱并将值地址压入栈顶
            /// </summary>
            /// <param name="type">目标值类型</param>
            [Placeholder(PlaceholderCode.Unbox, PlaceholderOperand.TypeRef)]
            public static void Unbox(Type type)
            {
                IL.Throw();
            }

            /// <summary>
            /// 将栈顶对象拆箱或转换为指定类型的值
            /// </summary>
            /// <param name="type">目标类型</param>
            [Placeholder(PlaceholderCode.Unbox_Any, PlaceholderOperand.TypeRef)]
            public static void Unbox_Any(Type type)
            {
                IL.Throw();
            }

            /// <summary>
            /// 将栈顶对象转换为指定引用类型
            /// </summary>
            /// <param name="type">目标引用类型</param>
            [Placeholder(PlaceholderCode.Castclass, PlaceholderOperand.TypeRef)]
            public static void Castclass(Type type)
            {
                IL.Throw();
            }

            /// <summary>
            /// 测试栈顶对象是否为指定类型并将结果压入栈顶
            /// </summary>
            /// <param name="type">目标引用类型</param>
            [Placeholder(PlaceholderCode.Isinst, PlaceholderOperand.TypeRef)]
            public static void Isinst(Type type)
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定类型的运行时句柄压入栈顶
            /// </summary>
            /// <param name="type">目标类型</param>
            [Placeholder(PlaceholderCode.Ldtoken, PlaceholderOperand.TypeRef)]
            public static void Ldtoken(Type type)
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定字段的运行时句柄压入栈顶
            /// </summary>
            /// <param name="field">目标字段</param>
            [Placeholder(PlaceholderCode.Ldtoken, PlaceholderOperand.FieldRef)]
            public static void Ldtoken(FieldRef field)
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定方法的运行时句柄压入栈顶
            /// </summary>
            /// <param name="method">目标方法</param>
            [Placeholder(PlaceholderCode.Ldtoken, PlaceholderOperand.MethodRef)]
            public static void Ldtoken(MethodRef method)
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定类型的大小压入栈顶
            /// </summary>
            /// <param name="type">目标类型</param>
            [Placeholder(PlaceholderCode.Sizeof, PlaceholderOperand.TypeRef)]
            public static void Sizeof(Type type)
            {
                IL.Throw();
            }


            /// <summary>
            /// 从栈顶地址创建指定类型的类型化引用
            /// </summary>
            /// <param name="type">引用的目标类型</param>
            [Placeholder(PlaceholderCode.Mkrefany, PlaceholderOperand.TypeRef)]
            public static void Mkrefany(Type type)
            {
                IL.Throw();
            }

            /// <summary>
            /// 从类型化引用中取得指定类型的地址
            /// </summary>
            /// <param name="type">引用的目标类型</param>
            [Placeholder(PlaceholderCode.Refanyval, PlaceholderOperand.TypeRef)]
            public static void Refanyval(Type type)
            {
                IL.Throw();
            }

            /// <summary>
            /// 从类型化引用中取得运行时类型句柄
            /// </summary>
            /// <param name="type">类型化引用包含的类型</param>
            [Placeholder(PlaceholderCode.Refanytype, PlaceholderOperand.TypeRef)]
            public static void Refanytype(Type type)
            {
                IL.Throw();
            }


            /// <summary>
            /// 从本地动态内存池分配指定字节数并将地址压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Localloc)]
            public static void Localloc()
            {
                IL.Throw();
            }

            /// <summary>
            /// 将指定数量的字节从源地址复制到目标地址
            /// </summary>
            [Placeholder(PlaceholderCode.Cpblk)]
            public static void Cpblk()
            {
                IL.Throw();
            }

            /// <summary>
            /// 使用指定字节值初始化目标内存块
            /// </summary>
            [Placeholder(PlaceholderCode.Initblk)]
            public static void Initblk()
            {
                IL.Throw();
            }

            /// <summary>
            /// 抛出栈顶异常对象
            /// </summary>
            [Placeholder(PlaceholderCode.Throw)]
            public static void Throw()
            {
                IL.Throw();
            }

            /// <summary>
            /// 重新抛出当前异常
            /// </summary>
            [Placeholder(PlaceholderCode.Rethrow)]
            public static void Rethrow()
            {
                IL.Throw();
            }

            /// <summary>
            /// 结束当前 finally、fault 或受保护区域
            /// </summary>
            [Placeholder(PlaceholderCode.Endfinally)]
            public static void Endfinally()
            {
                IL.Throw();
            }

            /// <summary>
            /// 结束异常筛选器并返回筛选结果
            /// </summary>
            [Placeholder(PlaceholderCode.Endfilter)]
            public static void Endfilter()
            {
                IL.Throw();
            }

            /// <summary>
            /// 指定后续间接内存访问的地址对齐方式
            /// </summary>
            /// <param name="value">对齐字节数，只能为 1、2 或 4</param>
            [Placeholder(PlaceholderCode.Unaligned, PlaceholderOperand.Byte, isPrefix: true)]
            public static void Unaligned(byte value)
            {
                IL.Throw();
            }

            /// <summary>
            /// 指定后续内存访问为易失性访问
            /// </summary>
            [Placeholder(PlaceholderCode.Volatile, isPrefix: true)]
            public static void Volatile()
            {
                IL.Throw();
            }

            /// <summary>
            /// 指定后续方法调用为尾调用
            /// </summary>
            [Placeholder(PlaceholderCode.Tail, isPrefix: true)]
            public static void Tail()
            {
                IL.Throw();
            }

            /// <summary>
            /// 约束后续虚方法调用的接收类型
            /// </summary>
            /// <param name="type">约束类型</param>
            [Placeholder(PlaceholderCode.Constrained, PlaceholderOperand.TypeRef, isPrefix: true)]
            public static void Constrained(Type type)
            {
                IL.Throw();
            }

            /// <summary>
            /// 指定后续数组元素地址操作返回只读托管指针
            /// </summary>
            [Placeholder(PlaceholderCode.Readonly, isPrefix: true)]
            public static void Readonly()
            {
                IL.Throw();
            }

            /// <summary>
            /// 禁用后续指令的指定运行时检查
            /// </summary>
            /// <param name="value">要禁用的检查标志</param>
            [Placeholder(PlaceholderCode.No, PlaceholderOperand.Byte, isPrefix: true)]
            public static void No(byte value)
            {
                IL.Throw();
            }


            /// <summary>
            /// 将当前可变参数方法的参数列表句柄压入栈顶
            /// </summary>
            [Placeholder(PlaceholderCode.Arglist)]
            public static void Arglist()
            {
                IL.Throw();
            }
        }
    }
}
