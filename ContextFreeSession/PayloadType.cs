using System;
using System.Collections.Generic;
using System.Linq;

namespace ContextFreeSession.Design
{
    public class PayloadType
    {
        private readonly Type type;

        private static readonly Dictionary<Type, string> typeKeywords = new()
        {
            { typeof(bool), "bool" },
            { typeof(byte), "byte" },
            { typeof(sbyte), "sbyte" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(long), "long" },
            { typeof(ulong), "ulong" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(decimal), "decimal" },
            { typeof(char), "char" },
            { typeof(string), "string" },
            { typeof(object), "object" },
        };

        private static readonly HashSet<Type> specialTupleTypes = new()
        {
            typeof(ValueTuple<,>),
            typeof(ValueTuple<,,>),
            typeof(ValueTuple<,,,>),
            typeof(ValueTuple<,,,,>),
            typeof(ValueTuple<,,,,,>),
            typeof(ValueTuple<,,,,,,>),
            typeof(ValueTuple<,,,,,,,>),
        };

        private PayloadType(Type type)
        {
            this.type = type;
        }

        public static PayloadType Create<T>()
        {
            return new PayloadType(typeof(T));
        }

        public override string ToString()
        {
            return ToString(ShortName);
        }

        private string ToString(Func<Type, string> func)
        {
            // 配列型
            if (type.IsArray && type.GetElementType() is Type elementType)
            {
                var ranks = new List<int>() { type.GetArrayRank() };
                while (elementType.IsArray)
                {
                    ranks.Add(elementType.GetArrayRank());
                    elementType = elementType.GetElementType()!;
                }
                var brackets = new string(ranks.SelectMany(n => $"[{new string(',', n - 1)}]").ToArray());
                return $"{new PayloadType(elementType).ToString(func)}{brackets}";
            }
            // null 許容値型
            if (Nullable.GetUnderlyingType(type) is Type nullable)
            {
                return $"{new PayloadType(nullable).ToString(func)}?";
            }
            // その他の複合型
            if (type.IsGenericType)
            {
                // null 許容注釈された参照型
                if (type.GetGenericTypeDefinition() == typeof(Null<>))
                {
                    return $"{new PayloadType(type.GetGenericArguments()[0]).ToString(func)}?";
                }
                // 二要素以上のタプル
                if (specialTupleTypes.Contains(type.GetGenericTypeDefinition()))
                {
                    var elementTypes = type.GetGenericArguments();
                    var tupleList = elementTypes.Take(7).ToList();


                    while (elementTypes.Length == 8)
                    {
                        elementTypes = elementTypes[7].GetGenericArguments();
                        tupleList.AddRange(elementTypes.Take(7));
                    }


                    return $"({string.Join(", ", tupleList.Select(t => new PayloadType(t).ToString(func)))})";
                }
                // その他のジェネリック型
                else
                {
                    return $"{func(type)}<{string.Join(", ", type.GetGenericArguments().Select(t => new PayloadType(t).ToString(func)))}>";
                }
            }
            // 単体型
            return func(type);
        }

        private static string ShortName(Type type)
        {
            switch (TypeKeyword(type))
            {
                case string keyword:
                    return keyword;
                default:
                    return type.Name.Split("`")[0];
            }
        }

        private static string CertainName(Type type)
        {
            switch (TypeKeyword(type))
            {
                case string keyword:
                    return keyword;
                default:
                    return type.FullName?.Split("`")[0] ?? throw new ArgumentException(null, nameof(type));
            }
        }

        private static string? TypeKeyword(Type type)
        {
            return typeKeywords.GetValueOrDefault(type);
        }
    }

    // null 許容注釈を表現するための型
    public class Null<T> where T : class
    {
        private Null() { }
    }
}
