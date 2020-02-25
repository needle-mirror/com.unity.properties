using System;
using System.Reflection;

namespace Unity.Properties.Internal
{
    struct RuntimeTypeInfoCache
    {
        public static bool IsContainerType(Type type)
        {
            return !(type.IsPrimitive || type.IsPointer || type.IsEnum || type == typeof(string));
        }
    }

    /// <summary>
    /// Helper class to avoid paying the cost of runtime type lookups.
    ///
    /// This is also used to abstract underlying type info in the runtime (e.g. RuntimeTypeHandle vs StaticTypeReg)
    /// </summary>
    struct RuntimeTypeInfoCache<T>
    {
        static readonly bool s_IsValueType;
        static readonly bool s_IsPrimitive;
        static readonly bool s_IsString;
        static readonly bool s_IsContainerType;
        static readonly bool s_IsInterface;
        static readonly bool s_IsAbstract;
        static readonly bool s_IsArray;
        static readonly bool s_IsEnum;
        static readonly bool s_IsEnumFlags;
        static readonly bool s_IsNullable;
        static readonly bool s_CanBeNull;
        
        static RuntimeTypeInfoCache()
        {
            var type = typeof(T);
            s_IsValueType = type.IsValueType;
            s_IsPrimitive = type.IsPrimitive;
            s_IsString = typeof(T) == typeof(string);
            s_IsContainerType = RuntimeTypeInfoCache.IsContainerType(typeof(T));
            s_IsInterface = type.IsInterface;
            s_IsAbstract = type.IsAbstract;
            s_IsArray = type.IsArray;
            s_IsEnum = type.IsEnum;
            
#if !NET_DOTS
            s_IsEnumFlags = s_IsEnum && null != type.GetCustomAttribute<FlagsAttribute>();
            s_IsNullable = Nullable.GetUnderlyingType(typeof(T)) != null;
#else
            s_IsEnumFlags = false;
            s_IsNullable = false;
#endif
            
            s_CanBeNull = !s_IsValueType || s_IsNullable;
        }
        
        public static bool IsValueType() => s_IsValueType;
        public static bool IsPrimitive() => s_IsPrimitive;
        public static bool IsPrimitiveOrString() => s_IsPrimitive || s_IsString;
        public static bool IsContainerType() => s_IsContainerType;
        public static bool IsInterface() => s_IsInterface;
        public static bool IsAbstract() => s_IsAbstract;
        public static bool IsArray() => s_IsArray;
        public static bool IsAbstractOrInterface() => s_IsAbstract || s_IsInterface;
        public static bool IsEnum() => s_IsEnum;
        public static bool IsFlagsEnum() => s_IsEnumFlags;
        public static bool IsNullable() => s_IsNullable;
        public static bool CanBeNull() => s_CanBeNull;
    }
}