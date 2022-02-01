#if !NET_DOTS
using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Properties.Internal;

namespace Unity.Properties
{
    readonly struct ConversionRegistry
    {
        class ConverterKeyComparer : IEqualityComparer<ConverterKey>
        {
            public bool Equals(ConverterKey x, ConverterKey y)
            {
                return x.SourceType == y.SourceType && x.DestinationType == y.DestinationType;
            }

            public int GetHashCode(ConverterKey obj)
            {
                return ((obj.SourceType != null ? obj.SourceType.GetHashCode() : 0) * 397) ^ (obj.DestinationType != null ? obj.DestinationType.GetHashCode() : 0);
            }
        }

        static readonly ConverterKeyComparer Comparer = new ConverterKeyComparer();

        readonly struct ConverterKey
        {
            public readonly Type SourceType;
            public readonly Type DestinationType;

            public ConverterKey(Type source, Type destination)
            {
                SourceType = source;
                DestinationType = destination;
            }
        }

        readonly Dictionary<ConverterKey, Delegate> m_Converters;

        ConversionRegistry(Dictionary<ConverterKey, Delegate> storage)
        {
            m_Converters = storage;
        }

        public static ConversionRegistry Create()
        {
            return new ConversionRegistry(new Dictionary<ConverterKey, Delegate>(Comparer));
        }

        public void Register(Type source, Type destination, Delegate converter)
        {
            m_Converters[new ConverterKey(source, destination)] = converter;
        }

        public Delegate GetConverter(Type source, Type destination)
        {
            var key = new ConverterKey(source, destination);
            return m_Converters.TryGetValue(key, out var converter)
                ? converter
                : null;
        }

        public bool TryGetConverter(Type source, Type destination, out Delegate converter)
        {
            converter = GetConverter(source, destination);
            return null != converter;
        }
    }

    /// <summary>
    /// Represents the method that will handle converting an object of type <typeparamref name="TSource"/> to an object of type <typeparamref name="TDestination"/>.
    /// </summary>
    /// <param name="value">The source value to be converted.</param>
    /// <typeparam name="TSource">The source type to convert from.</typeparam>
    /// <typeparam name="TDestination">The destination type to convert to.</typeparam>
    public delegate TDestination ConvertDelegate<in TSource, out TDestination>(TSource value);

    /// <summary>
    /// Helper class to handle type conversion during properties API calls.
    /// </summary>
    public static class TypeConversion
    {
        static readonly ConversionRegistry s_GlobalConverters = ConversionRegistry.Create();

#if !UNITY_DOTSRUNTIME
        internal static Func<string, (bool, UnityEngine.Object)> s_GlobalObjectIdConverter;
#endif

        static TypeConversion()
        {
            PrimitiveConverters.Register();
        }

        /// <summary>
        /// Registers a new converter from <typeparamref name="TSource"/> to <typeparamref name="TDestination"/>.
        /// </summary>
        /// <param name="converter"></param>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TDestination"></typeparam>
        public static void Register<TSource, TDestination>(ConvertDelegate<TSource, TDestination> converter)
        {
            s_GlobalConverters.Register(typeof(TSource), typeof(TDestination), converter);
        }


        /// <summary>
        /// Converts the specified value from <typeparamref name="TSource"/> to <typeparamref name="TDestination"/>.
        /// </summary>
        /// <param name="value">The source value to convert.</param>
        /// <typeparam name="TSource">The source type to convert from.</typeparam>
        /// <typeparam name="TDestination">The destination type to convert to.</typeparam>
        /// <returns>The value converted to the <typeparamref name="TDestination"/> type.</returns>
        /// <exception cref="InvalidOperationException">No converter is registered for the given types.</exception>
        public static TDestination Convert<TSource, TDestination>(ref TSource value)
        {
            if (!TryConvert<TSource, TDestination>(value, out var destination))
            {
                throw new InvalidOperationException($"TypeConversion no converter has been registered for SrcType=[{typeof(TSource)}] to DstType=[{typeof(TDestination)}]");
            }

            return destination;
        }

        /// <summary>
        /// Converts the specified value from <typeparamref name="TSource"/> to <typeparamref name="TDestination"/>.
        /// </summary>
        /// <param name="value">The source value to convert.</param>
        /// <typeparam name="TSource">The source type to convert from.</typeparam>
        /// <typeparam name="TDestination">The destination type to convert to.</typeparam>
        /// <returns>The value converted to the <typeparamref name="TDestination"/> type.</returns>
        /// <exception cref="InvalidOperationException">No converter is registered for the given types.</exception>
        public static TDestination Convert<TSource, TDestination>(TSource value)
        {
            return Convert<TSource, TDestination>(ref value);
        }
        
        /// <summary>
        /// Converts the specified value from <typeparamref name="TSource"/> to <typeparamref name="TDestination"/>.
        /// </summary>
        /// <param name="source">The source value to convert.</param>
        /// <param name="destination">When this method returns, contains the converted destination value if the conversion succeeded; otherwise, default.</param>
        /// <typeparam name="TSource">The source type to convert from.</typeparam>
        /// <typeparam name="TDestination">The destination type to convert to.</typeparam>
        ///<returns><see langword="true"/> if the conversion succeeded; otherwise, <see langword="false"/>.</returns>
        public static bool TryConvert<TSource, TDestination>(ref TSource source, out TDestination destination)
        {
            if (s_GlobalConverters.TryGetConverter(typeof(TSource), typeof(TDestination), out var converter))
            {
                destination = ((ConvertDelegate<TSource, TDestination>) converter)(source);
                return true;
            }

            if (RuntimeTypeInfoCache<TDestination>.IsNullable)
            {
                var underlyingType = Nullable.GetUnderlyingType(typeof(TDestination));

                if (underlyingType.IsEnum)
                {
                    var enumUnderlyingType = Enum.GetUnderlyingType(underlyingType);
                    var value = System.Convert.ChangeType(source, enumUnderlyingType);
                    destination = (TDestination) Enum.ToObject(underlyingType, value);
                    return true;
                }
                
                destination = (TDestination) System.Convert.ChangeType(source, underlyingType);
                return true;
            }

#if !UNITY_DOTSRUNTIME
            if (RuntimeTypeInfoCache<TDestination>.IsUnityObject)
            {
                if (TryConvertToUnityEngineObject(ref source, out destination))
                {
                    return true;
                }
            }
#endif

            if (RuntimeTypeInfoCache<TDestination>.IsEnum)
            {
                if (TryConvertToEnum(ref source, out destination))
                {
                    return true;
                }
            }

            // Could be boxing :(
            if (source is TDestination assignable)
            {
                destination = assignable;
                return true;
            }

            if (typeof(TDestination).IsAssignableFrom(typeof(TSource)))
            {
                destination = (TDestination) (object) source;
                return true;
            }

            destination = default;
            return false;
        }
        
        /// <summary>
        /// Converts the specified value from <typeparamref name="TSource"/> to <typeparamref name="TDestination"/>.
        /// </summary>
        /// <param name="source">The source value to convert.</param>
        /// <param name="destination">When this method returns, contains the converted destination value if the conversion succeeded; otherwise, default.</param>
        /// <typeparam name="TSource">The source type to convert from.</typeparam>
        /// <typeparam name="TDestination">The destination type to convert to.</typeparam>
        ///<returns><see langword="true"/> if the conversion succeeded; otherwise, <see langword="false"/>.</returns>
        public static bool TryConvert<TSource, TDestination>(TSource source, out TDestination destination)
        {
            return TryConvert(ref source, out destination);
        }

#if !UNITY_DOTSRUNTIME
        static bool TryConvertToUnityEngineObject<TSource, TDestination>(ref TSource source, out TDestination destination)
        {
            if (!typeof(UnityEngine.Object).IsAssignableFrom(typeof(TDestination)))
            {
                destination = default;
                return false;
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(typeof(TSource)) && null == source)
            {
                destination = default;
                return true;
            }

#if UNITY_EDITOR
            var sourceType = typeof(TSource);

            if ((sourceType.IsClass && null != source) || sourceType.IsValueType)
            {
                var str = source.ToString();

                if (UnityEditor.GlobalObjectId.TryParse(str, out var id))
                {
                    var obj = UnityEditor.GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);
                    destination = (TDestination) (object) obj;
                    return true;
                }

                if (str == new UnityEditor.GlobalObjectId().ToString())
                {
                    destination = (TDestination) (object) null;
                    return true;
                }
            }

#endif
            destination = default;
            return false;
        }
#endif

        static bool TryConvertToEnum<TSource, TDestination>(ref TSource source, out TDestination destination)
        {
            if (!typeof(TDestination).IsEnum)
            {
                destination = default;
                return false;
            }

            if (typeof(TSource) == typeof(string))
            {
                try
                {
                    destination = (TDestination) Enum.Parse(typeof(TDestination), (string) (object) source);
                }
                catch (ArgumentException)
                {
                    destination = default;
                    return false;
                }

                return true;
            }

            if (typeof(TSource).IsAssignableFrom(typeof(TDestination)))
            {
                destination = (TDestination) Enum.ToObject(typeof(TDestination), source);
                return true;
            }

            var sourceTypeCode = Type.GetTypeCode(typeof(TSource));
            var destinationTypeCode = Type.GetTypeCode(typeof(TDestination));
            
            // Enums are tricky, and we need to handle narrowing conversion manually. Might as well do all possible valid use-cases.
            switch (sourceTypeCode)
            {
                case TypeCode.UInt64:
                    var uLongValue = Convert<TSource, ulong>(ref source);
                    switch (destinationTypeCode)
                    { 
                        case TypeCode.Int32:
                            destination = (TDestination) (object) Convert<ulong, int>(ref uLongValue);
                            break;
                        case TypeCode.Byte:
                            destination = (TDestination) (object) Convert<ulong, byte>(ref uLongValue);
                            break;
                        case TypeCode.Int16:
                            destination = (TDestination) (object) Convert<ulong, short>(ref uLongValue);
                            break;
                        case TypeCode.Int64:
                            destination = (TDestination) (object) Convert<ulong, long>(ref uLongValue);
                            break;
                        case TypeCode.SByte:
                            destination = (TDestination) (object) Convert<ulong, sbyte>(ref uLongValue);
                            break;
                        case TypeCode.UInt16:
                            destination = (TDestination) (object) Convert<ulong, ushort>(ref uLongValue);
                            break;
                        case TypeCode.UInt32:
                            destination = (TDestination) (object) Convert<ulong, uint>(ref uLongValue);
                            break;
                        case TypeCode.UInt64:
                            destination = (TDestination) (object) Convert<TSource, ulong>(ref source);
                            break;
                        default:
                            destination = default;
                            return false;
                    }

                    break;
                case TypeCode.Int32:
                    var intValue = Convert<TSource, int>(ref source);
                    switch (destinationTypeCode)
                    {
                        case TypeCode.Int32:
                            destination = (TDestination) (object) intValue;
                            break;
                        case TypeCode.Byte:
                            destination = (TDestination) (object) Convert<int, byte>(ref intValue);
                            break;
                        case TypeCode.Int16:
                            destination = (TDestination) (object) Convert<int, short>(ref intValue);
                            break;
                        case TypeCode.Int64:
                            destination = (TDestination) (object) Convert<int, long>(ref intValue);
                            break;
                        case TypeCode.SByte:
                            destination = (TDestination) (object) Convert<int, sbyte>(ref intValue);
                            break;
                        case TypeCode.UInt16:
                            destination = (TDestination) (object) Convert<int, ushort>(ref intValue);
                            break;
                        case TypeCode.UInt32:
                            destination = (TDestination) (object) Convert<int, uint>(ref intValue);
                            break;
                        case TypeCode.UInt64:
                            destination = (TDestination) (object) Convert<int, ulong>(ref intValue);
                            break;
                        default:
                            destination = default;
                            return false;
                    }

                    break;
                case TypeCode.Byte:
                    var byteValue = Convert<TSource, byte>(ref source);
                    switch (destinationTypeCode)
                    {
                        case TypeCode.Byte:
                            destination = (TDestination) (object) byteValue;
                            break;
                        case TypeCode.Int16:
                            destination = (TDestination) (object) Convert<byte, short>(ref byteValue);
                            break;
                        case TypeCode.Int32:
                            destination = (TDestination) (object) Convert<byte, int>(ref byteValue);
                            break;
                        case TypeCode.Int64:
                            destination = (TDestination) (object) Convert<byte, long>(ref byteValue);
                            break;
                        case TypeCode.SByte:
                            destination = (TDestination) (object) Convert<byte, sbyte>(ref byteValue);
                            break;
                        case TypeCode.UInt16:
                            destination = (TDestination) (object) Convert<byte, ushort>(ref byteValue);
                            break;
                        case TypeCode.UInt32:
                            destination = (TDestination) (object) Convert<byte, uint>(ref byteValue);
                            break;
                        case TypeCode.UInt64:
                            destination = (TDestination) (object) Convert<byte, ulong>(ref byteValue);
                            break;
                        default:
                            destination = default;
                            return false;
                    }

                    break;
                case TypeCode.SByte:
                    var sByteValue = Convert<TSource, sbyte>(ref source);
                    switch (destinationTypeCode)
                    {
                        case TypeCode.Byte:
                            destination = (TDestination) (object) Convert<sbyte, byte>(ref sByteValue);
                            break;
                        case TypeCode.Int16:
                            destination = (TDestination) (object) Convert<sbyte, short>(ref sByteValue);
                            break;
                        case TypeCode.Int32:
                            destination = (TDestination) (object) Convert<sbyte, int>(ref sByteValue);
                            break;
                        case TypeCode.Int64:
                            destination = (TDestination) (object) Convert<sbyte, long>(ref sByteValue);
                            break;
                        case TypeCode.SByte:
                            destination = (TDestination) (object) sByteValue;
                            break;
                        case TypeCode.UInt16:
                            destination = (TDestination) (object) Convert<sbyte, ushort>(ref sByteValue);
                            break;
                        case TypeCode.UInt32:
                            destination = (TDestination) (object) Convert<sbyte, uint>(ref sByteValue);
                            break;
                        case TypeCode.UInt64:
                            destination = (TDestination) (object) Convert<sbyte, ulong>(ref sByteValue);
                            break;
                        default:
                            destination = default;
                            return false;
                    }

                    break;
                case TypeCode.Int16:
                    var shortValue = Convert<TSource, short>(ref source);
                    switch (destinationTypeCode)
                    {
                        case TypeCode.Byte:
                            destination = (TDestination) (object) Convert<short, byte>(ref shortValue);
                            break;
                        case TypeCode.Int16:
                            destination = (TDestination) (object) shortValue;
                            break;
                        case TypeCode.Int32:
                            destination = (TDestination) (object) Convert<short, int>(ref shortValue);
                            break;
                        case TypeCode.Int64:
                            destination = (TDestination) (object) Convert<short, long>(ref shortValue);
                            break;
                        case TypeCode.SByte:
                            destination = (TDestination) (object) Convert<short, sbyte>(ref shortValue);
                            break;
                        case TypeCode.UInt16:
                            destination = (TDestination) (object) Convert<short, ushort>(ref shortValue);
                            break;
                        case TypeCode.UInt32:
                            destination = (TDestination) (object) Convert<short, uint>(ref shortValue);
                            break;
                        case TypeCode.UInt64:
                            destination = (TDestination) (object) Convert<short, ulong>(ref shortValue);
                            break;
                        default:
                            destination = default;
                            return false;
                    }

                    break;
                case TypeCode.UInt16:
                    var uShortValue = Convert<TSource, ushort>(ref source);
                    switch (destinationTypeCode)
                    {
                        case TypeCode.Byte:
                            destination = (TDestination) (object) Convert<ushort, byte>(ref uShortValue);
                            break;
                        case TypeCode.Int16:
                            destination = (TDestination) (object) Convert<ushort, short>(ref uShortValue);
                            break;
                        case TypeCode.Int32:
                            destination = (TDestination) (object) Convert<ushort, int>(ref uShortValue);
                            break;
                        case TypeCode.Int64:
                            destination = (TDestination) (object) Convert<ushort, long>(ref uShortValue);
                            break;
                        case TypeCode.SByte:
                            destination = (TDestination) (object) Convert<ushort, sbyte>(ref uShortValue);
                            break;
                        case TypeCode.UInt16:
                            destination = (TDestination) (object) uShortValue;
                            break;
                        case TypeCode.UInt32:
                            destination = (TDestination) (object) Convert<ushort, uint>(ref uShortValue);
                            break;
                        case TypeCode.UInt64:
                            destination = (TDestination) (object) Convert<ushort, ulong>(ref uShortValue);
                            break;
                        default:
                            destination = default;
                            return false;
                    }

                    break;
                case TypeCode.UInt32:
                    var uintValue = Convert<TSource, uint>(ref source);
                    switch (destinationTypeCode)
                    {
                        case TypeCode.Byte:
                            destination = (TDestination) (object) Convert<uint, byte>(ref uintValue);
                            break;
                        case TypeCode.Int16:
                            destination = (TDestination) (object) Convert<uint, short>(ref uintValue);
                            break;
                        case TypeCode.Int32:
                            destination = (TDestination) (object) Convert<uint, int>(ref uintValue);
                            break;
                        case TypeCode.Int64:
                            destination = (TDestination) (object) Convert<uint, long>(ref uintValue);
                            break;
                        case TypeCode.SByte:
                            destination = (TDestination) (object) Convert<uint, sbyte>(ref uintValue);
                            break;
                        case TypeCode.UInt16:
                            destination = (TDestination) (object) Convert<uint, ushort>(ref uintValue);
                            break;
                        case TypeCode.UInt32:
                            destination = (TDestination) (object) uintValue;
                            break;
                        case TypeCode.UInt64:
                            destination = (TDestination) (object) Convert<uint, ulong>(ref uintValue);
                            break;
                        default:
                            destination = default;
                            return false;
                    }

                    break;
                case TypeCode.Int64:
                    var longValue = Convert<TSource, long>(ref source);
                    switch (destinationTypeCode)
                    {
                        case TypeCode.Byte:
                            destination = (TDestination) (object) Convert<long, byte>(ref longValue);
                            break;
                        case TypeCode.Int16:
                            destination = (TDestination) (object) Convert<long, short>(ref longValue);
                            break;
                        case TypeCode.Int32:
                            destination = (TDestination) (object) Convert<long, int>(ref longValue);
                            break;
                        case TypeCode.Int64:
                            destination = (TDestination) (object) Convert<long, long>(ref longValue);
                            break;
                        case TypeCode.SByte:
                            destination = (TDestination) (object) longValue;
                            break;
                        case TypeCode.UInt16:
                            destination = (TDestination) (object) Convert<long, ushort>(ref longValue);
                            break;
                        case TypeCode.UInt32:
                            destination = (TDestination) (object) Convert<long, uint>(ref longValue);
                            break;
                        case TypeCode.UInt64:
                            destination = (TDestination) (object) Convert<long, ulong>(ref longValue);
                            break;
                        default:
                            destination = default;
                            return false;
                    }

                    break;
                default:
                    destination = default;
                    return false;
            }

            return true;
        }
        
        static class PrimitiveConverters
        {
            public static void Register()
            {
                // signed integral types
                RegisterInt8Converters();
                RegisterInt16Converters();
                RegisterInt32Converters();
                RegisterInt64Converters();

                // unsigned integral types
                RegisterUInt8Converters();
                RegisterUInt16Converters();
                RegisterUInt32Converters();
                RegisterUInt64Converters();

                // floating point types
                RegisterFloat32Converters();
                RegisterFloat64Converters();

                // .net types
                RegisterBooleanConverters();
                RegisterCharConverters();
                RegisterStringConverters();
                RegisterObjectConverters();

                // Unity vector types
                RegisterVectorConverters();
                
                // support System.Guid by default
                TypeConversion.Register<string, Guid>(g => new Guid(g));
            }

            static void RegisterInt8Converters()
            {
                TypeConversion.Register((sbyte v) => (char) v);
                TypeConversion.Register((sbyte v) => v != 0);
                TypeConversion.Register((sbyte v) => (sbyte) v);
                TypeConversion.Register((sbyte v) => (short) v);
                TypeConversion.Register((sbyte v) => (int) v);
                TypeConversion.Register((sbyte v) => (long) v);
                TypeConversion.Register((sbyte v) => (byte) v);
                TypeConversion.Register((sbyte v) => (ushort) v);
                TypeConversion.Register((sbyte v) => (uint) v);
                TypeConversion.Register((sbyte v) => (ulong) v);
                TypeConversion.Register((sbyte v) => (float) v);
                TypeConversion.Register((sbyte v) => (double) v);
                TypeConversion.Register((sbyte v) => (object) v);
            }

            static void RegisterInt16Converters()
            {
                TypeConversion.Register((short v) => (char) v);
                TypeConversion.Register((short v) => v != 0);
                TypeConversion.Register((short v) =>  (sbyte) v);
                TypeConversion.Register((short v) => (short) v);
                TypeConversion.Register((short v) => (int) v);
                TypeConversion.Register((short v) => (long) v);
                TypeConversion.Register((short v) => (byte) v);
                TypeConversion.Register((short v) => (ushort) v);
                TypeConversion.Register((short v) => (uint) v);
                TypeConversion.Register((short v) =>  (ulong) v);
                TypeConversion.Register((short v) => (float) v);
                TypeConversion.Register((short v) => (double) v);
                TypeConversion.Register((short v) => (object) v);
            }

            static void RegisterInt32Converters()
            { 
                TypeConversion.Register((int v) => (char) v);
                TypeConversion.Register((int v) => v != 0);
                TypeConversion.Register((int v) => (sbyte) v);
                TypeConversion.Register((int v) => (short) v);
                TypeConversion.Register((int v) => (int) v);
                TypeConversion.Register((int v) => (long) v);
                TypeConversion.Register((int v) => (byte) v);
                TypeConversion.Register((int v) => (ushort) v);
                TypeConversion.Register((int v) => (uint) v);
                TypeConversion.Register((int v) => (ulong) v);
                TypeConversion.Register((int v) => (float) v);
                TypeConversion.Register((int v) => (double) v);
                TypeConversion.Register((int v) => (object) v);
            }

            static void RegisterInt64Converters()
            {
                TypeConversion.Register((long v) => (char) v);
                TypeConversion.Register((long v) => v != 0);
                TypeConversion.Register((long v) => (sbyte) v);
                TypeConversion.Register((long v) => (short) v);
                TypeConversion.Register((long v) => (int) v);
                TypeConversion.Register((long v) => (long) v);
                TypeConversion.Register((long v) => (byte) v);
                TypeConversion.Register((long v) => (ushort) v);
                TypeConversion.Register((long v) => (uint) v);
                TypeConversion.Register((long v) => (ulong) v);
                TypeConversion.Register((long v) => (float) v);
                TypeConversion.Register((long v) => (double) v);
                TypeConversion.Register((long v) => (object) v);
            }

            static void RegisterUInt8Converters()
            {
                TypeConversion.Register((byte v) => (char) v);
                TypeConversion.Register((byte v) => v != 0);
                TypeConversion.Register((byte v) => (sbyte) v);
                TypeConversion.Register((byte v) => (short) v);
                TypeConversion.Register((byte v) => (int) v);
                TypeConversion.Register((byte v) => (long) v);
                TypeConversion.Register((byte v) => (byte) v);
                TypeConversion.Register((byte v) => (ushort) v);
                TypeConversion.Register((byte v) => (uint) v);
                TypeConversion.Register((byte v) => (ulong) v);
                TypeConversion.Register((byte v) => (float) v);
                TypeConversion.Register((byte v) => (double) v);
                TypeConversion.Register((byte v) => (object) v);
            }

            static void RegisterUInt16Converters()
            {
                TypeConversion.Register((ushort v) => (char) v);
                TypeConversion.Register((ushort v) => v != 0);
                TypeConversion.Register((ushort v) => (sbyte) v);
                TypeConversion.Register((ushort v) => (short) v);
                TypeConversion.Register((ushort v) => (int) v);
                TypeConversion.Register((ushort v) => (long) v);
                TypeConversion.Register((ushort v) => (byte) v);
                TypeConversion.Register((ushort v) => (ushort) v);
                TypeConversion.Register((ushort v) => (uint) v);
                TypeConversion.Register((ushort v) => (ulong) v);
                TypeConversion.Register((ushort v) => (float) v);
                TypeConversion.Register((ushort v) => (double) v);
                TypeConversion.Register((ushort v) => (object) v);
            }

            static void RegisterUInt32Converters()
            {
                TypeConversion.Register((uint v) => (char) v);
                TypeConversion.Register((uint v) => v != 0);
                TypeConversion.Register((uint v) => (sbyte) v);
                TypeConversion.Register((uint v) => (short) v);
                TypeConversion.Register((uint v) => (int) v);
                TypeConversion.Register((uint v) => (long) v);
                TypeConversion.Register((uint v) => (byte) v);
                TypeConversion.Register((uint v) => (ushort) v);
                TypeConversion.Register((uint v) => (uint) v);
                TypeConversion.Register((uint v) => (ulong) v);
                TypeConversion.Register((uint v) => (float) v);
                TypeConversion.Register((uint v) => (double) v);
                TypeConversion.Register((uint v) => (object) v);
            }

            static void RegisterUInt64Converters()
            {
                TypeConversion.Register((ulong v) => (char) v);
                TypeConversion.Register((ulong v) => v != 0);
                TypeConversion.Register((ulong v) => (sbyte) v);
                TypeConversion.Register((ulong v) => (short) v);
                TypeConversion.Register((ulong v) => (int) v);
                TypeConversion.Register((ulong v) => (long) v);
                TypeConversion.Register((ulong v) => (byte) v);
                TypeConversion.Register((ulong v) => (ushort) v);
                TypeConversion.Register((ulong v) => (uint) v);
                TypeConversion.Register((ulong v) => (ulong) v);
                TypeConversion.Register((ulong v) => (float) v);
                TypeConversion.Register((ulong v) => (double) v);
                TypeConversion.Register((ulong v) => (object) v);
                TypeConversion.Register((ulong v) => v.ToString());
            }

            static void RegisterFloat32Converters()
            {
                TypeConversion.Register((float v) => (char) v);
                TypeConversion.Register((float v) => Math.Abs(v) > float.Epsilon);
                TypeConversion.Register((float v) => (sbyte) v);
                TypeConversion.Register((float v) => (short) v);
                TypeConversion.Register((float v) => (int) v);
                TypeConversion.Register((float v) => (long) v);
                TypeConversion.Register((float v) => (byte) v);
                TypeConversion.Register((float v) => (ushort) v);
                TypeConversion.Register((float v) => (uint) v);
                TypeConversion.Register((float v) => (ulong) v);
                TypeConversion.Register((float v) => (float) v);
                TypeConversion.Register((float v) => (double) v);
                TypeConversion.Register((float v) => (object) v);
            }

            static void RegisterFloat64Converters()
            {
                TypeConversion.Register((double v) => (char) v);
                TypeConversion.Register((double v) => Math.Abs(v) > double.Epsilon);
                TypeConversion.Register((double v) => (sbyte) v);
                TypeConversion.Register((double v) => (short) v);
                TypeConversion.Register((double v) => (int) v);
                TypeConversion.Register((double v) => (long) v);
                TypeConversion.Register((double v) => (byte) v);
                TypeConversion.Register((double v) => (ushort) v);
                TypeConversion.Register((double v) => (uint) v);
                TypeConversion.Register((double v) => (ulong) v);
                TypeConversion.Register((double v) => (float) v);
                TypeConversion.Register((double v) => (double) v);
                TypeConversion.Register((double v) => (object) v);
            }

            static void RegisterBooleanConverters()
            {
                TypeConversion.Register((bool v) => v ? (char) 1 : (char) 0);
                TypeConversion.Register((bool v) => v);
                TypeConversion.Register((bool v) => v ? (sbyte) 1 : (sbyte) 0);
                TypeConversion.Register((bool v) => v ? (short) 1 : (short) 0);
                TypeConversion.Register((bool v) => v ? (int) 1 : (int) 0);
                TypeConversion.Register((bool v) => v ? (long) 1 : (long) 0);
                TypeConversion.Register((bool v) => v ? (byte) 1 : (byte) 0);
                TypeConversion.Register((bool v) => v ? (ushort) 1 : (ushort) 0);
                TypeConversion.Register((bool v) => v ? (uint) 1 : (uint) 0);
                TypeConversion.Register((bool v) => v ? (ulong) 1 : (ulong) 0);
                TypeConversion.Register((bool v) => v ? (float) 1 : (float) 0);
                TypeConversion.Register((bool v) => v ? (double) 1 : (double) 0);
                TypeConversion.Register((bool v) => (object) v);
            }
            
            static void RegisterVectorConverters()
            {
#if !UNITY_DOTSRUNTIME
                TypeConversion.Register((UnityEngine.Vector2 v) => new UnityEngine.Vector2Int((int)v.x, (int)v.y));
                TypeConversion.Register((UnityEngine.Vector3 v) => new UnityEngine.Vector3Int((int)v.x, (int)v.y, (int)v.z));
                TypeConversion.Register((UnityEngine.Vector2Int v) => v);
                TypeConversion.Register((UnityEngine.Vector3Int v) => v);
#endif
            }

            static void RegisterCharConverters()
            {
                TypeConversion.Register((string v) =>
                {
                    if (v.Length != 1)
                    {
                        throw new Exception("Not a valid char");
                    }

                    return v[0];
                });
                TypeConversion.Register((char v) => v);
                TypeConversion.Register((char v) => v != (char) 0);
                TypeConversion.Register((char v) => (sbyte) v);
                TypeConversion.Register((char v) => (short) v);
                TypeConversion.Register((char v) => (int) v);
                TypeConversion.Register((char v) => (long) v);
                TypeConversion.Register((char v) => (byte) v);
                TypeConversion.Register((char v) => (ushort) v);
                TypeConversion.Register((char v) => (uint) v);
                TypeConversion.Register((char v) => (ulong) v);
                TypeConversion.Register((char v) => (float) v);
                TypeConversion.Register((char v) => (double) v);
                TypeConversion.Register((char v) => (object) v);
                TypeConversion.Register((char v) => v.ToString());
            }

static void RegisterStringConverters()
            {
                TypeConversion.Register((string v) => v);
                TypeConversion.Register((string v) => !string.IsNullOrEmpty(v) ? v[0] : '\0');
                TypeConversion.Register((char v) => v.ToString());
                TypeConversion.Register((string v) =>
                {
                    if (bool.TryParse(v, out var r))
                        return r;

                    return double.TryParse(v, out var fromDouble)
                        ? Convert<double, bool>(fromDouble)
                        : default;
                });
                TypeConversion.Register((bool v) => v.ToString());
                TypeConversion.Register((string v) =>
                {
                    if (sbyte.TryParse(v, out var r))
                        return r;

                    return double.TryParse(v, out var fromDouble)
                        ? Convert<double, sbyte>(fromDouble)
                        : default;
                });
                TypeConversion.Register((sbyte v) => v.ToString());
                TypeConversion.Register((string v) =>
                {
                    if (short.TryParse(v, out var r))
                        return r;

                    return double.TryParse(v, out var fromDouble)
                        ? Convert<double, short>(fromDouble)
                        : default;
                });
                TypeConversion.Register((short v) => v.ToString());
                TypeConversion.Register((string v) =>
                {
                    if (int.TryParse(v, out var r))
                        return r;

                    return double.TryParse(v, out var fromDouble)
                        ? Convert<double, int>(fromDouble)
                        : default;
                });
                TypeConversion.Register((int v) => v.ToString());
                TypeConversion.Register((string v) =>
                {
                    if (long.TryParse(v, out var r))
                        return r;

                    return double.TryParse(v, out var fromDouble)
                        ? Convert<double, long>(fromDouble)
                        : default;
                });
                TypeConversion.Register((long v) => v.ToString());
                TypeConversion.Register((string v) =>
                {
                    if (byte.TryParse(v, out var r))
                        return r;

                    return double.TryParse(v, out var fromDouble)
                        ? Convert<double, byte>(fromDouble)
                        : default;
                });
                TypeConversion.Register((byte v) => v.ToString());
                TypeConversion.Register((string v) =>
                {
                    if (ushort.TryParse(v, out var r))
                        return r;

                    return double.TryParse(v, out var fromDouble)
                        ? Convert<double, ushort>(fromDouble)
                        : default;
                });
                TypeConversion.Register((ushort v) => v.ToString());
                TypeConversion.Register((string v) =>
                {
                    if (uint.TryParse(v, out var r))
                        return r;

                    return double.TryParse(v, out var fromDouble)
                        ? Convert<double, uint>(fromDouble)
                        : default;
                });
                TypeConversion.Register((uint v) => v.ToString());
                TypeConversion.Register((string v) =>
                {
                    if (ulong.TryParse(v, out var r))
                    {
                        return r;
                    }

                    return double.TryParse(v, out var fromDouble)
                        ? Convert<double, ulong>(fromDouble)
                        : default;
                });
                TypeConversion.Register((ulong v) => v.ToString());
                TypeConversion.Register((string v) =>
                {
                    if (float.TryParse(v, out var r))
                        return r;

                    return double.TryParse(v, out var fromDouble)
                        ? Convert<double, float>(fromDouble)
                        : default;
                });
                TypeConversion.Register((float v) => v.ToString(CultureInfo.InvariantCulture));
                TypeConversion.Register((string v) =>
                {
                    double.TryParse(v, out var r);
                    return r;
                });
                TypeConversion.Register((double v) => v.ToString(CultureInfo.InvariantCulture));
                TypeConversion.Register((string v) => v);
            }

            static void RegisterObjectConverters()
            {
                TypeConversion.Register((object v) => v is char value ? value : default);
                TypeConversion.Register((object v) => v is bool value ? value : default);
                TypeConversion.Register((object v) => v is sbyte value ? value : default);
                TypeConversion.Register((object v) => v is short value ? value : default);
                TypeConversion.Register((object v) => v is int value ? value : default);
                TypeConversion.Register((object v) => v is long value ? value : default);
                TypeConversion.Register((object v) => v is byte value ? value : default);
                TypeConversion.Register((object v) => v is ushort value ? value : default);
                TypeConversion.Register((object v) => v is uint value ? value : default);
                TypeConversion.Register((object v) => v is ulong value ? value : default);
                TypeConversion.Register((object v) => v is float value ? value : default);
                TypeConversion.Register((object v) => v is double value ? value : default);
                TypeConversion.Register((object v) => v);
            }
        }
    }
}
#endif