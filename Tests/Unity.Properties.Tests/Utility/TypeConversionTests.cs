using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Properties.Tests
{
    [TestFixture]
    [Category("Type Conversion")]
    class TypeConversionTests
    {
        struct Handle
        {
            public int Value { get; set; }
        }

        [Test]
        public void TypeConversion_WithNullableValueTypes_ConvertsAsExpected()
        {
            // T? => T?
            {
                int? value = null;
                Assert.That(TypeConversion.TryConvert(ref value, out int? result), Is.True);
                Assert.That(result, Is.Null);

                value = 50;
                Assert.That(TypeConversion.TryConvert(ref value, out result), Is.True);
                Assert.That(result, Is.EqualTo(50));
            }

            // T? => T
            // This should work only if T? is not null
            {
                int? nv = null;
                Assert.That(TypeConversion.TryConvert(ref nv, out int result), Is.False);

                nv = 50;
                Assert.That(TypeConversion.TryConvert(ref nv, out result), Is.True);
                Assert.That(result, Is.EqualTo(50));
            }

            // T => T?
            // This should work all the time
            {
                var value = 50;
                Assert.That(TypeConversion.TryConvert(ref value, out int? result), Is.True);
                Assert.That(result, Is.EqualTo(50));
            }

            // T? => U?
            // In pure C#, if U is assignable from T, it should be supported, but this is not a case we support.
            {
                int? value = null;
                Assert.That(TypeConversion.TryConvert(ref value, out float? result), Is.False);

                value = 50;
                Assert.That(TypeConversion.TryConvert(ref value, out result), Is.False);
            }

            // T? => U
            // In pure C#, if U is assignable from T, it should be supported when T is not null, but this is not a case we support.
            {
                int? nv = null;
                Assert.That(TypeConversion.TryConvert(ref nv, out float result), Is.False);

                nv = 50;
                Assert.That(TypeConversion.TryConvert(ref nv, out result), Is.False);
            }
        }

        [Test]
        public void TypeConversion_WithNullableEnumTypes_ConvertsAsExpected()
        {
            // T? => T?
            {
                CameraClearFlags? value = null;
                Assert.That(TypeConversion.TryConvert(ref value, out CameraClearFlags? result), Is.True);
                Assert.That(result, Is.Null);

                value = CameraClearFlags.Skybox;
                Assert.That(TypeConversion.TryConvert(ref value, out result), Is.True);
                Assert.That(result, Is.EqualTo(CameraClearFlags.Skybox));
            }

            // T? => T
            // This should work only if T? is not null
            {
                CameraClearFlags? value = null;
                Assert.That(TypeConversion.TryConvert(ref value, out CameraClearFlags result), Is.False);

                value = CameraClearFlags.Skybox;
                Assert.That(TypeConversion.TryConvert(ref value, out result), Is.True);
                Assert.That(result, Is.EqualTo(CameraClearFlags.Skybox));
            }

            // T => T?
            // This should work all the time
            {
                var value = CameraClearFlags.Skybox;
                Assert.That(TypeConversion.TryConvert(ref value, out CameraClearFlags? result), Is.True);
                Assert.That(result, Is.EqualTo(CameraClearFlags.Skybox));
            }

            // T? => U?
            {
                CameraClearFlags? value = null;
                Assert.That(TypeConversion.TryConvert(ref value, out Vector3? result), Is.False);

                value = CameraClearFlags.Skybox;
                Assert.That(TypeConversion.TryConvert(ref value, out result), Is.False);
            }

            // T? => U
            {
                CameraClearFlags? nv = null;
                Assert.That(TypeConversion.TryConvert(ref nv, out Vector3 result), Is.False);

                nv = CameraClearFlags.Skybox;
                Assert.That(TypeConversion.TryConvert(ref nv, out result), Is.False);
            }
        }

        [Test]
        public void ConverterRegistry_WithRegisteredConverters_CanReturnTypesConvertibleTo()
        {
            var registry = ConversionRegistry.Create();
            registry.Register(typeof(float), typeof(string), (TypeConverter<float, string>) ((ref float v) => v.ToString()));
            registry.Register(typeof(int), typeof(string), (TypeConverter<int, string>) ((ref int v) => v.ToString()));

            var list = new List<Type>();
            registry.GetAllTypesConvertingToType(typeof(string), list);
            Assert.That(list.Contains(typeof(float)), Is.True);
            Assert.That(list.Contains(typeof(int)), Is.True);
            Assert.That(list.Contains(typeof(Vector2)), Is.False);

            registry.Register(typeof(Vector2), typeof(string), (TypeConverter<Vector2, string>) ((ref Vector2 v) => v.ToString()));
            list.Clear();
            registry.GetAllTypesConvertingToType(typeof(string), list);
            Assert.That(list.Contains(typeof(Vector2)), Is.True);
        }

        [Test]
        public void ConverterRegistry_WithRegisteredConverters_CanUnregisterConverters()
        {
            var registry = ConversionRegistry.Create();
            registry.Register(typeof(float), typeof(string), (TypeConverter<float, string>) ((ref float v) => v.ToString()));
            registry.Register(typeof(int), typeof(string), (TypeConverter<int, string>) ((ref int v) => v.ToString()));

            Assert.That(registry.ConverterCount, Is.EqualTo(2));
            registry.Unregister(typeof(float), typeof(string));
            Assert.That(registry.ConverterCount, Is.EqualTo(1));

            registry.Unregister(typeof(Vector2), typeof(string));
            Assert.That(registry.ConverterCount, Is.EqualTo(1));

            registry.Unregister(typeof(int), typeof(string));
            Assert.That(registry.ConverterCount, Is.EqualTo(0));
        }

        [Test]
        public void ConverterRegistry_WithRegisteringANullDelegate_Throws()
        {
            var registry = ConversionRegistry.Create();
            Assert.Throws<ArgumentException>(() => { registry.Register(typeof(float), typeof(string), null); });
        }

        [Test]
        public void Conversion_ToUnityObject_WorksForAnyDerivedType()
        {
            var tex = new Texture2D(256, 256);
            var mappings = new Dictionary<Handle, Object>
            {
                { new Handle{ Value = 10 }, tex },
            };

            TypeConversion.Register((ref Handle handle) => mappings.TryGetValue(handle, out var value) ? value : null);

            var h = new Handle { Value = 5 };
            Assert.That(TypeConversion.TryConvert(ref h, out Texture2D texture), Is.False);
            Assert.That(texture, Is.Null);

            h.Value = 10;
            Assert.That(TypeConversion.TryConvert(ref h, out texture), Is.True);
            Assert.That(texture, Is.EqualTo(tex));
        }
        
        [Test]
        public void Conversion_FromToUnityObject_DoesNotThrowStackOverflow()
        {
            var texture = new Texture2D(256, 256);
            Assert.That(TypeConversion.TryConvert(ref texture, out UnityEngine.Object obj), Is.True);
            Assert.That(obj, Is.EqualTo(texture));
        }
    }
}
