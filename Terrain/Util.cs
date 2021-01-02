using System;
using System.Diagnostics;

using static Godot.GD;

namespace ParticlesSandbox
{
    public static class Util
    {
        [Conditional("DEBUG")]
        public static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                PrintErr(message);
            }
        }
    }

    /// <summary>
    /// Generates an object on-demand and caches it, while still allowing
    /// that object to be reclaimed by garbage collection.
    /// </summary>
    /// <typeparam name="T">The type of the object referenced.</typeparam>
    [DebuggerDisplay("{ValueForDebugDisplay}")]
    public class WeakLazy<T> where T : class
    {
        readonly Func<T> build;
        readonly WeakReference<T?> weak;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public T Value
        {
            get
            {
                if (!weak.TryGetTarget(out var value))
                {
                    value = build();
                    weak.SetTarget(value);
                }

                return value;
            }
        }

        internal T? ValueForDebugDisplay
        {
            get
            {
                if (!weak.TryGetTarget(out var value))
                    value = null;

                return value;
            }
        }

        public WeakLazy(Func<T> valueFactory)
        {
            build = valueFactory;
            weak = new WeakReference<T?>(null);
        }

        public static implicit operator T(WeakLazy<T> obj)
            => obj.Value;
    }

    public static class WeakLazy
    {
        public static WeakLazy<T> FromDefaultConstructor<T>() where T : class, new()
            => new WeakLazy<T>(() => new T());
    }
}
