using Nebulae.Collections;
using Nebulae.Runtime.Emit.Inline;
using System;
using System.Diagnostics;

namespace Nebulae.Reflection.Specifiers
{
    public readonly partial struct DelegateSpecifier
    {
        internal abstract class Closure
        {
            public abstract int Length { get; }


            /// <remarks>Used for compress, so the target must not be bound.</remarks>
            public abstract object? this[int index] { get; }


            public abstract Closure Bind(object? target);

            public abstract Closure Bind<T>(T target);

            public abstract Closure BindType(Type targetType);

            public abstract Closure Copy();

            public abstract Closure Compress(int index);

            public abstract Type GetArgumentTypeAt(int index);

            public abstract bool IsNullAt(int index);

            public abstract void Load(Closure closure);


            protected static TTo Cast<TFrom, TTo>(TFrom value)
            {
                if (typeof(TFrom).IsValueType)
                {
                    if (!typeof(TTo).IsValueType)
                    {
                        IL.Emit.Ldarg(value);
                        IL.Emit.Box(typeof(TFrom));
                        IL.Emit.Ret();
                    }

                    if (typeof(TFrom) == typeof(TTo))
                    {
                        IL.Emit.Ldarg(value);
                        IL.Emit.Ret();
                    }

                    // Convert TFrom to Nullable<TFrom>.
                    return (TTo)(object)value!;
                }

                IL.Emit.Ldarg(value);
                return IL.Ret<TTo>();
            }

            protected static Closure Create(Type closureType, Closure source)
            {
                var closure = (Closure)Activator.CreateInstance(closureType, nonPublic: true)!;
                closure.Load(source);
                return closure;
            }
        }

        internal sealed class ArrayClosure : Closure
        {
            public object?[] Args;


            public override int Length
            {
                get => Args.Length;
            }


            public override object? this[int index]
            {
                get => CollectionHelpers.Unsafe.Ref(Args, index);
            }


            public ArrayClosure()
            {
                Args = [];
            }

            public ArrayClosure(object?[] args)
            {
                Args = args;
            }


            public override Closure Bind(object? target)
            {
                Args[0] = target;
                return this;
            }

            public override Closure Bind<T>(T target)
            {
                Args[0] = target;
                return this;
            }

            public override Closure BindType(Type targetType)
            {
                return new ArrayClosure([null, .. Args]);
            }

            public override Closure Copy()
            {
                return new ArrayClosure((object?[])Args.Clone());
            }

            public override Closure Compress(int index)
            {
                throw new NotSupportedException(
                    $"Array closure does not support '{nameof(Compress)}'.");
            }

            public override Type GetArgumentTypeAt(int index)
            {
                Debug.Assert(Args[index] is not null, $"Argument at index {index} is null");
                return CollectionHelpers.Unsafe.Ref(Args, index)!.GetType();
            }

            public override bool IsNullAt(int index)
            {
                return CollectionHelpers.Unsafe.Ref(Args, index) is null;
            }

            public override void Load(Closure closure)
            {
                Args = [null, .. ((ArrayClosure)closure).Args];
            }
        }

        internal sealed class Closure<T> : Closure
        {
            public T? Value;


            public override int Length => 1;


            public override object? this[int index]
            {
                get => index switch
                {
                    0 => Value,
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid index is 0.")
                };
            }


            private Closure() { }

            public Closure(T? v)
            {
                Value = v;
            }


            public override Closure Bind(object? target)
            {
                Value = (T?)target;
                return this;
            }

            public override Closure Bind<TT>(TT target)
            {
                Value = Cast<TT, T>(target);
                return this;
            }

            public override Closure BindType(Type targetType)
            {
                return Create(
                    typeof(Closure<,>).MakeGenericType(targetType, typeof(T)),
                    this);
            }

            public override Closure Copy()
            {
                return new Closure<T>(Value);
            }

            public override Closure Compress(int index)
            {
                throw new NotSupportedException(
                    $"Cannot compress a 1-value closure.");
            }

            public override Type GetArgumentTypeAt(int index)
            {
                return index switch
                {
                    0 => typeof(T),
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid index is 0.")
                };
            }

            public override bool IsNullAt(int index)
            {
                return index switch
                {
                    0 => Value is null,
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid index is 0.")
                };
            }

            public override void Load(Closure closure)
            {
                throw new NotSupportedException(
                    "Cannot load any input arguments into a 1-value closure.");
            }
        }

        internal sealed class Closure<T, T1> : Closure
        {
            public T? Value;
            public T1? Value1;


            public override int Length => 2;


            public override object? this[int index]
            {
                get => index switch
                {
                    0 => Value,
                    1 => Value1,
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid indices are 0 to 1.")
                };
            }


            private Closure() { }

            public Closure(T? v, T1? v1)
            {
                Value = v;
                Value1 = v1;
            }


            public override Closure Bind(object? target)
            {
                Value = (T?)target;
                return this;
            }

            public override Closure Bind<TT>(TT target)
            {
                Value = Cast<TT, T>(target);
                return this;
            }

            public override Closure BindType(Type targetType)
            {
                return Create(
                    typeof(Closure<,,>).MakeGenericType(targetType, typeof(T), typeof(T1)),
                    this);
            }

            public override Closure Copy()
            {
                return new Closure<T, T1>(Value, Value1);
            }

            public override Closure Compress(int index)
            {
                return index switch
                {
                    0 => new Closure<T1>(Value1),
                    1 => new Closure<T>(Value),
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid indices are 0 to 1.")
                };
            }

            public override Type GetArgumentTypeAt(int index)
            {
                return index switch
                {
                    0 => typeof(T),
                    1 => typeof(T1),
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid indices are 0 to 1.")
                };
            }

            public override bool IsNullAt(int index)
            {
                return index switch
                {
                    0 => Value is null,
                    1 => Value1 is null,
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid indices are 0 to 1.")
                };
            }

            public override void Load(Closure closure)
            {
                var source = (Closure<T1>)closure;
                Value1 = source.Value;
            }
        }

        internal sealed class Closure<T, T1, T2> : Closure
        {
            public T? Value;
            public T1? Value1;
            public T2? Value2;


            public override int Length => 3;


            public override object? this[int index]
            {
                get => index switch
                {
                    0 => Value,
                    1 => Value1,
                    2 => Value2,
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid indices are 0 to 2.")
                };
            }


            private Closure() { }

            public Closure(T? v, T1? v1, T2? v2)
            {
                Value = v;
                Value1 = v1;
                Value2 = v2;
            }


            public override Closure Bind(object? target)
            {
                Value = (T?)target;
                return this;
            }

            public override Closure Bind<TT>(TT target)
            {
                Value = Cast<TT, T>(target);
                return this;
            }

            public override Closure BindType(Type targetType)
            {
                return Create(
                    typeof(Closure<,,,>).MakeGenericType(targetType, typeof(T), typeof(T1), typeof(T2)),
                    this);
            }

            public override Closure Copy()
            {
                return new Closure<T, T1, T2>(Value, Value1, Value2);
            }

            public override Closure Compress(int index)
            {
                return index switch
                {
                    0 => new Closure<T1, T2>(Value1, Value2),
                    1 => new Closure<T, T2>(Value, Value2),
                    2 => new Closure<T, T1>(Value, Value1),
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid indices are 0 to 2.")
                };
            }

            public override Type GetArgumentTypeAt(int index)
            {
                return index switch
                {
                    0 => typeof(T),
                    1 => typeof(T1),
                    2 => typeof(T2),
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid indices are 0 to 2.")
                };
            }

            public override bool IsNullAt(int index)
            {
                return index switch
                {
                    0 => Value is null,
                    1 => Value1 is null,
                    2 => Value2 is null,
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid indices are 0 to 2.")
                };
            }

            public override void Load(Closure closure)
            {
                var source = (Closure<T1, T2>)closure;
                Value1 = source.Value;
                Value2 = source.Value1;
            }
        }

        internal sealed class Closure<T, T1, T2, T3> : Closure
        {
            public T? Value;
            public T1? Value1;
            public T2? Value2;
            public T3? Value3;


            public override int Length => 4;


            public override object? this[int index]
            {
                get => index switch
                {
                    0 => Value,
                    1 => Value1,
                    2 => Value2,
                    3 => Value3,
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid indices are 0 to 3.")
                };
            }


            private Closure() { }

            public Closure(T? v, T1? v1, T2? v2, T3? v3)
            {
                Value = v;
                Value1 = v1;
                Value2 = v2;
                Value3 = v3;
            }


            public override Closure Bind(object? target)
            {
                Value = (T?)target;
                return this;
            }

            public override Closure Bind<TT>(TT target)
            {
                Value = Cast<TT, T>(target);
                return this;
            }

            public override Closure BindType(Type targetType)
            {
                return Create(
                    typeof(Closure<,,,,>).MakeGenericType(targetType, typeof(T), typeof(T1), typeof(T2), typeof(T3)),
                    this);
            }

            public override Closure Copy()
            {
                return new Closure<T, T1, T2, T3>(Value, Value1, Value2, Value3);
            }

            public override Closure Compress(int index)
            {
                return index switch
                {
                    0 => new Closure<T1, T2, T3>(Value1, Value2, Value3),
                    1 => new Closure<T, T2, T3>(Value, Value2, Value3),
                    2 => new Closure<T, T1, T3>(Value, Value1, Value3),
                    3 => new Closure<T, T1, T2>(Value, Value1, Value2),
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid indices are 0 to 3.")
                };
            }

            public override Type GetArgumentTypeAt(int index)
            {
                return index switch
                {
                    0 => typeof(T),
                    1 => typeof(T1),
                    2 => typeof(T2),
                    3 => typeof(T3),
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid indices are 0 to 3.")
                };
            }

            public override bool IsNullAt(int index)
            {
                return index switch
                {
                    0 => Value is null,
                    1 => Value1 is null,
                    2 => Value2 is null,
                    3 => Value3 is null,
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid indices are 0 to 3.")
                };
            }

            public override void Load(Closure closure)
            {
                var source = (Closure<T1, T2, T3>)closure;
                Value1 = source.Value;
                Value2 = source.Value1;
                Value3 = source.Value2;
            }
        }

        internal sealed class Closure<T, T1, T2, T3, T4> : Closure
        {
            public T? Value;
            public T1? Value1;
            public T2? Value2;
            public T3? Value3;
            public T4? Value4;


            public override int Length => 5;


            public override object? this[int index]
            {
                get => index switch
                {
                    0 => Value,
                    1 => Value1,
                    2 => Value2,
                    3 => Value3,
                    4 => Value4,
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid indices are 0 to 4.")
                };
            }


            private Closure() { }

            public Closure(T? v, T1? v1, T2? v2, T3? v3, T4? v4)
            {
                Value = v;
                Value1 = v1;
                Value2 = v2;
                Value3 = v3;
                Value4 = v4;
            }


            public override Closure Bind(object? target)
            {
                Value = (T?)target;
                return this;
            }

            public override Closure Bind<TT>(TT target)
            {
                Value = Cast<TT, T>(target);
                return this;
            }

            public override Closure BindType(Type targetType)
            {
                return Create(
                    typeof(Closure<,,,,,>).MakeGenericType(targetType, typeof(T), typeof(T1), typeof(T2), typeof(T3), typeof(T4)),
                    this);
            }

            public override Closure Copy()
            {
                return new Closure<T, T1, T2, T3, T4>(
                    Value,
                    Value1,
                    Value2,
                    Value3,
                    Value4);
            }

            public override Closure Compress(int index)
            {
                return index switch
                {
                    0 => new Closure<T1, T2, T3, T4>(Value1, Value2, Value3, Value4),
                    1 => new Closure<T, T2, T3, T4>(Value, Value2, Value3, Value4),
                    2 => new Closure<T, T1, T3, T4>(Value, Value1, Value3, Value4),
                    3 => new Closure<T, T1, T2, T4>(Value, Value1, Value2, Value4),
                    4 => new Closure<T, T1, T2, T3>(Value, Value1, Value2, Value3),
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid indices are 0 to 4.")
                };
            }

            public override Type GetArgumentTypeAt(int index)
            {
                return index switch
                {
                    0 => typeof(T),
                    1 => typeof(T1),
                    2 => typeof(T2),
                    3 => typeof(T3),
                    4 => typeof(T4),
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid indices are 0 to 4.")
                };
            }

            public override bool IsNullAt(int index)
            {
                return index switch
                {
                    0 => Value is null,
                    1 => Value1 is null,
                    2 => Value2 is null,
                    3 => Value3 is null,
                    4 => Value4 is null,
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid indices are 0 to 4.")
                };
            }

            public override void Load(Closure closure)
            {
                var source = (Closure<T1, T2, T3, T4>)closure;
                Value1 = source.Value;
                Value2 = source.Value1;
                Value3 = source.Value2;
                Value4 = source.Value3;
            }
        }

        internal sealed class Closure<T, T1, T2, T3, T4, T5> : Closure
        {
            public T? Value;
            public T1? Value1;
            public T2? Value2;
            public T3? Value3;
            public T4? Value4;
            public T5? Value5;


            public override int Length => 6;


            public override object? this[int index]
            {
                get => index switch
                {
                    0 => Value,
                    1 => Value1,
                    2 => Value2,
                    3 => Value3,
                    4 => Value4,
                    5 => Value5,
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid indices are 0 to 5.")
                };
            }


            private Closure() { }

            public Closure(T? v, T1? v1, T2? v2, T3? v3, T4? v4, T5? v5)
            {
                Value = v;
                Value1 = v1;
                Value2 = v2;
                Value3 = v3;
                Value4 = v4;
                Value5 = v5;
            }


            public override Closure Bind(object? target)
            {
                Value = (T?)target;
                return this;
            }

            public override Closure Bind<TT>(TT target)
            {
                Value = Cast<TT, T>(target);
                return this;
            }

            public override Closure BindType(Type targetType)
            {
                return Create(
                    typeof(Closure<,,,,,,>).MakeGenericType(targetType, typeof(T), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5)),
                    this);
            }

            public override Closure Copy()
            {
                return new Closure<T, T1, T2, T3, T4, T5>(
                    Value,
                    Value1,
                    Value2,
                    Value3,
                    Value4,
                    Value5);
            }

            public override Closure Compress(int index)
            {
                return index switch
                {
                    0 => new Closure<T1, T2, T3, T4, T5>(Value1, Value2, Value3, Value4, Value5),
                    1 => new Closure<T, T2, T3, T4, T5>(Value, Value2, Value3, Value4, Value5),
                    2 => new Closure<T, T1, T3, T4, T5>(Value, Value1, Value3, Value4, Value5),
                    3 => new Closure<T, T1, T2, T4, T5>(Value, Value1, Value2, Value4, Value5),
                    4 => new Closure<T, T1, T2, T3, T5>(Value, Value1, Value2, Value3, Value5),
                    5 => new Closure<T, T1, T2, T3, T4>(Value, Value1, Value2, Value3, Value4),
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid indices are 0 to 5.")
                };
            }

            public override Type GetArgumentTypeAt(int index)
            {
                return index switch
                {
                    0 => typeof(T),
                    1 => typeof(T1),
                    2 => typeof(T2),
                    3 => typeof(T3),
                    4 => typeof(T4),
                    5 => typeof(T5),
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid indices are 0 to 5.")
                };
            }

            public override bool IsNullAt(int index)
            {
                return index switch
                {
                    0 => Value is null,
                    1 => Value1 is null,
                    2 => Value2 is null,
                    3 => Value3 is null,
                    4 => Value4 is null,
                    5 => Value5 is null,
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid indices are 0 to 5.")
                };
            }

            public override void Load(Closure closure)
            {
                var source = (Closure<T1, T2, T3, T4, T5>)closure;
                Value1 = source.Value;
                Value2 = source.Value1;
                Value3 = source.Value2;
                Value4 = source.Value3;
                Value5 = source.Value4;
            }
        }

        internal sealed class Closure<T, T1, T2, T3, T4, T5, T6> : Closure
        {
            public T? Value;
            public T1? Value1;
            public T2? Value2;
            public T3? Value3;
            public T4? Value4;
            public T5? Value5;
            public T6? Value6;


            public override int Length => 7;


            public override object? this[int index]
            {
                get => index switch
                {
                    0 => Value,
                    1 => Value1,
                    2 => Value2,
                    3 => Value3,
                    4 => Value4,
                    5 => Value5,
                    6 => Value6,
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid indices are 0 to 6.")
                };
            }


            private Closure() { }

            public Closure(T? v, T1? v1, T2? v2, T3? v3, T4? v4, T5? v5, T6? v6)
            {
                Value = v;
                Value1 = v1;
                Value2 = v2;
                Value3 = v3;
                Value4 = v4;
                Value5 = v5;
                Value6 = v6;
            }


            public override Closure Bind(object? target)
            {
                Value = (T?)target;
                return this;
            }

            public override Closure Bind<TT>(TT target)
            {
                Value = Cast<TT, T>(target);
                return this;
            }

            public override Closure BindType(Type targetType)
            {
                return Create(
                    typeof(Closure<,,,,,,,>).MakeGenericType(targetType, typeof(T), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6)),
                    this);
            }


            public override Closure Copy()
            {
                return new Closure<T, T1, T2, T3, T4, T5, T6>(
                    Value,
                    Value1,
                    Value2,
                    Value3,
                    Value4,
                    Value5,
                    Value6);
            }

            public override Closure Compress(int index)
            {
                return index switch
                {
                    0 => new Closure<T1, T2, T3, T4, T5, T6>(Value1, Value2, Value3, Value4, Value5, Value6),
                    1 => new Closure<T, T2, T3, T4, T5, T6>(Value, Value2, Value3, Value4, Value5, Value6),
                    2 => new Closure<T, T1, T3, T4, T5, T6>(Value, Value1, Value3, Value4, Value5, Value6),
                    3 => new Closure<T, T1, T2, T4, T5, T6>(Value, Value1, Value2, Value4, Value5, Value6),
                    4 => new Closure<T, T1, T2, T3, T5, T6>(Value, Value1, Value2, Value3, Value5, Value6),
                    5 => new Closure<T, T1, T2, T3, T4, T6>(Value, Value1, Value2, Value3, Value4, Value6),
                    6 => new Closure<T, T1, T2, T3, T4, T5>(Value, Value1, Value2, Value3, Value4, Value5),
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid indices are 0 to 6.")
                };
            }

            public override Type GetArgumentTypeAt(int index)
            {
                return index switch
                {
                    0 => typeof(T),
                    1 => typeof(T1),
                    2 => typeof(T2),
                    3 => typeof(T3),
                    4 => typeof(T4),
                    5 => typeof(T5),
                    6 => typeof(T6),
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid indices are 0 to 6.")
                };
            }

            public override bool IsNullAt(int index)
            {
                return index switch
                {
                    0 => Value is null,
                    1 => Value1 is null,
                    2 => Value2 is null,
                    3 => Value3 is null,
                    4 => Value4 is null,
                    5 => Value5 is null,
                    6 => Value6 is null,
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid indices are 0 to 6.")
                };
            }

            public override void Load(Closure closure)
            {
                var source = (Closure<T1, T2, T3, T4, T5, T6>)closure;
                Value1 = source.Value;
                Value2 = source.Value1;
                Value3 = source.Value2;
                Value4 = source.Value3;
                Value5 = source.Value4;
                Value6 = source.Value5;
            }
        }

        internal sealed class Closure<T, T1, T2, T3, T4, T5, T6, T7> : Closure
        {
            public T? Value;
            public T1? Value1;
            public T2? Value2;
            public T3? Value3;
            public T4? Value4;
            public T5? Value5;
            public T6? Value6;
            public T7? Value7;


            public override int Length => 8;


            public override object? this[int index]
            {
                get => index switch
                {
                    0 => Value,
                    1 => Value1,
                    2 => Value2,
                    3 => Value3,
                    4 => Value4,
                    5 => Value5,
                    6 => Value6,
                    7 => Value7,
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid indices are 0 to 7.")
                };
            }


            private Closure() { }

            public Closure(T? v, T1? v1, T2? v2, T3? v3, T4? v4, T5? v5, T6? v6, T7? v7)
            {
                Value = v;
                Value1 = v1;
                Value2 = v2;
                Value3 = v3;
                Value4 = v4;
                Value5 = v5;
                Value6 = v6;
                Value7 = v7;
            }


            public override Closure Bind(object? target)
            {
                Value = (T?)target;
                return this;
            }

            public override Closure Bind<TT>(TT target)
            {
                Value = Cast<TT, T>(target);
                return this;
            }

            public override Closure BindType(Type targetType)
            {
                return Create(
                    typeof(Closure<,,,,,,,,>).MakeGenericType(targetType, typeof(T), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7)),
                    this);
            }

            public override Closure Copy()
            {
                return new Closure<T, T1, T2, T3, T4, T5, T6, T7>(
                    Value,
                    Value1,
                    Value2,
                    Value3,
                    Value4,
                    Value5,
                    Value6,
                    Value7);
            }

            public override Closure Compress(int index)
            {
                return index switch
                {
                    0 => new Closure<T1, T2, T3, T4, T5, T6, T7>(Value1, Value2, Value3, Value4, Value5, Value6, Value7),
                    1 => new Closure<T, T2, T3, T4, T5, T6, T7>(Value, Value2, Value3, Value4, Value5, Value6, Value7),
                    2 => new Closure<T, T1, T3, T4, T5, T6, T7>(Value, Value1, Value3, Value4, Value5, Value6, Value7),
                    3 => new Closure<T, T1, T2, T4, T5, T6, T7>(Value, Value1, Value2, Value4, Value5, Value6, Value7),
                    4 => new Closure<T, T1, T2, T3, T5, T6, T7>(Value, Value1, Value2, Value3, Value5, Value6, Value7),
                    5 => new Closure<T, T1, T2, T3, T4, T6, T7>(Value, Value1, Value2, Value3, Value4, Value6, Value7),
                    6 => new Closure<T, T1, T2, T3, T4, T5, T7>(Value, Value1, Value2, Value3, Value4, Value5, Value7),
                    7 => new Closure<T, T1, T2, T3, T4, T5, T6>(Value, Value1, Value2, Value3, Value4, Value5, Value6),
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid indices are 0 to 7.")
                };
            }

            public override Type GetArgumentTypeAt(int index)
            {
                return index switch
                {
                    0 => typeof(T),
                    1 => typeof(T1),
                    2 => typeof(T2),
                    3 => typeof(T3),
                    4 => typeof(T4),
                    5 => typeof(T5),
                    6 => typeof(T6),
                    7 => typeof(T7),
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid indices are 0 to 7.")
                };
            }

            public override bool IsNullAt(int index)
            {
                return index switch
                {
                    0 => Value is null,
                    1 => Value1 is null,
                    2 => Value2 is null,
                    3 => Value3 is null,
                    4 => Value4 is null,
                    5 => Value5 is null,
                    6 => Value6 is null,
                    7 => Value7 is null,
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid indices are 0 to 7.")
                };
            }

            public override void Load(Closure closure)
            {
                var source = (Closure<T1, T2, T3, T4, T5, T6, T7>)closure;
                Value1 = source.Value;
                Value2 = source.Value1;
                Value3 = source.Value2;
                Value4 = source.Value3;
                Value5 = source.Value4;
                Value6 = source.Value5;
                Value7 = source.Value6;
            }
        }

        /// <remarks>Closure with 9 value will storage the target and 8 arguments.</remarks>
        internal sealed class Closure<T, T1, T2, T3, T4, T5, T6, T7, T8> : Closure
        {
            public T? Value;
            public T1? Value1;
            public T2? Value2;
            public T3? Value3;
            public T4? Value4;
            public T5? Value5;
            public T6? Value6;
            public T7? Value7;
            public T8? Value8;


            public override int Length => 9;


            public override object? this[int index]
            {

                get => throw new NotSupportedException(
                    "Cannot get 9-value closure value by index.");
            }


            private Closure() { }

            public Closure(T? v, T1? v1, T2? v2, T3? v3, T4? v4, T5? v5, T6? v6, T7? v7, T8? v8)
            {
                Value = v;
                Value1 = v1;
                Value2 = v2;
                Value3 = v3;
                Value4 = v4;
                Value5 = v5;
                Value6 = v6;
                Value7 = v7;
                Value8 = v8;
            }


            public override Closure Bind(object? target)
            {
                Value = (T?)target;
                return this;
            }

            public override Closure Bind<TT>(TT target)
            {
                Value = Cast<TT, T>(target);
                return this;
            }

            public override Closure BindType(Type targetType)
            {
                throw new NotSupportedException(
                    "Cannot bind any target to a 9-value closure.");
            }

            public override Closure Copy()
            {
                return new Closure<T, T1, T2, T3, T4, T5, T6, T7, T8>(
                    Value,
                    Value1,
                    Value2,
                    Value3,
                    Value4,
                    Value5,
                    Value6,
                    Value7,
                    Value8);
            }

            public override Closure Compress(int index)
            {
                // Compression only happens when
                // the target is not bound.
                throw new NotSupportedException(
                    "Cannot compress a 9-value closure.");
            }

            public override Type GetArgumentTypeAt(int index)
            {
                return index switch
                {
                    0 => typeof(T),
                    1 => typeof(T1),
                    2 => typeof(T2),
                    3 => typeof(T3),
                    4 => typeof(T4),
                    5 => typeof(T5),
                    6 => typeof(T6),
                    7 => typeof(T7),
                    8 => typeof(T8),
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid indices are 0 to 8.")
                };
            }

            public override bool IsNullAt(int index)
            {
                return index switch
                {
                    0 => Value is null,
                    1 => Value1 is null,
                    2 => Value2 is null,
                    3 => Value3 is null,
                    4 => Value4 is null,
                    5 => Value5 is null,
                    6 => Value6 is null,
                    7 => Value7 is null,
                    8 => Value8 is null,
                    _ => throw new ArgumentOutOfRangeException(nameof(index),
                        $"Invalid argument index: {index}. " +
                        $"Valid indices are 0 to 8.")
                };
            }

            public override void Load(Closure closure)
            {
                var source = (Closure<T1, T2, T3, T4, T5, T6, T7, T8>)closure;
                Value1 = source.Value;
                Value2 = source.Value1;
                Value3 = source.Value2;
                Value4 = source.Value3;
                Value5 = source.Value4;
                Value6 = source.Value5;
                Value7 = source.Value6;
                Value8 = source.Value7;
            }
        }
    }
}
