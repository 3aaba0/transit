using System;
using System.Collections.Generic;
using System.Linq;
using iSynaptic.Commons;

namespace TransitAPIExample
{
    public static class ExtensionMethods
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source.IndexOf(toCheck, comp) >= 0;
        }
        public static Maybe<T> MaybeFirst<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.Any() ? enumerable.First().ToMaybe() : Maybe.NoValue;
        }

        public static Maybe<TResult> Select<T, TResult>(this Maybe<T> source, Func<T, TResult> map)
        {
            return new Maybe<TResult>(() => source.HasValue ? map(source.Value).ToMaybe() : Maybe.NoValue);
        }

        public static IEnumerable<TResult> SelectEnumerable<T, TResult>(this Maybe<IEnumerable<T>> source, Func<T, TResult> map)
        {
            return source.HasValue ? source.Value.Select(map) : Enumerable.Empty<TResult>();
        }

        public static Maybe<TResult> SelectMaybe<T, TResult>(this Maybe<T> source, Func<T, Maybe<TResult>> map)
        {
            return new Maybe<TResult>(() => source.HasValue ? map(source.Value) : Maybe.NoValue);
        }

        public static Maybe<T> Or<T>(this Maybe<T> source, T defaultValue)
        {
            return new Maybe<T>(() => source.HasValue ? source.Value.ToMaybe() : defaultValue.ToMaybe());
        }

        public static void Compute<TKey, TValue>(
                this Dictionary<TKey, TValue> dictionary,
                TKey key,
                Func<TValue, TValue> valueMapper,
                TValue defaultValue
            )
        {
            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, valueMapper(defaultValue));
            }
            else
            {
                dictionary[key] = valueMapper(dictionary[key]);
            }
        }
        public static void SetIfNull<TKey, TValue>(
            this Dictionary<TKey, TValue> dictionary,
            TKey key,
            TValue defaultValue
        )
        {
            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, defaultValue);
            }
        }
    }
}