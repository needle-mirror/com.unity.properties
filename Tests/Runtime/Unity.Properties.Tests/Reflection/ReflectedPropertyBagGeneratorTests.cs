using NUnit.Framework;
using System;

namespace Unity.Properties.Reflection.Tests
{
    [TestFixture]
    class ReflectedPropertyBagGeneratorTests
    {
        struct ContainerWithPrivateFields
        {
#pragma warning disable 649
            [Property] int m_Int32Value;
            int m_HiddenInt32Value;
#pragma warning restore 649
        }

        struct ContainerWithCharField
        {
#pragma warning disable 649
            public char c;
#pragma warning restore 649
        }

        struct ContainerWithProperties
        {
            [Property] public int IntProperty { get; }
            public int HiddenInt32Property { get; }
        }

        class ClassContainerWithBaseClass : BaseClassContainerWithPrivateFields
        {
#pragma warning disable 649
            public bool m_BoolValue;
#pragma warning restore 649
        }

        class BaseClassContainerWithPrivateFields : AnotherBaseClassContainerWithPrivateFields
        {
#pragma warning disable 649
            [Property] float m_FloatValue;
            float m_HiddenFloatValue;
#pragma warning restore 649
        }

        class AnotherBaseClassContainerWithPrivateFields
        {
#pragma warning disable 649
            [Property] int m_Int32Value;
            int m_HiddenInt32Value;
#pragma warning restore 649
        }

        class ClassContainerWithDuplicateFields : BaseClassContainerWithPrivateFields
        {
#pragma warning disable 649
            [Property] float m_FloatValue;
            float m_HiddenFloatValue;
#pragma warning restore 649
        }

        struct AssertThatPropertyIsOfType<TContainer, TExpected> : IPropertyGetter<TContainer>
        {
            public void VisitProperty<TProperty, TValue>(TProperty property, ref TContainer container, ref ChangeTracker changeTracker) 
                where TProperty : IProperty<TContainer, TValue>
            {
                Assert.That(property.GetType(), Is.EqualTo(typeof(TExpected)));
            }

            public void VisitCollectionProperty<TProperty, TValue>(TProperty property, ref TContainer container, ref ChangeTracker changeTracker) 
                where TProperty : ICollectionProperty<TContainer, TValue> => 
                throw new System.NotImplementedException();
        }

        /// <summary>
        /// Tests that the <see cref="ReflectedPropertyBagProvider"/> correctly generates properties for private <see cref="PropertyAttribute"/> fields.
        /// </summary>
        [Test]
        public void ReflectedPropertyBagGenerator_PrivateFields()
        {
            var propertyBag = new ReflectedPropertyBagProvider().Generate<ContainerWithPrivateFields>();
            Assert.That(propertyBag.HasProperty("m_Int32Value"), Is.True);
            Assert.That(propertyBag.HasProperty("m_HiddenInt32Value"), Is.False);
        }

        /// <summary>
        /// Tests that the <see cref="ReflectedPropertyBagProvider"/> correctly generates properties for base class private <see cref="PropertyAttribute"/> fields.
        /// </summary>
        [Test]
        public void ReflectedPropertyBagGenerator_BaseClassPrivateFields()
        {
            var propertyBag = new ReflectedPropertyBagProvider().Generate<ClassContainerWithBaseClass>();
            Assert.That(propertyBag.HasProperty("m_BoolValue"), Is.True);
            Assert.That(propertyBag.HasProperty("m_FloatValue"), Is.True);
            Assert.That(propertyBag.HasProperty("m_HiddenFloatValue"), Is.False);
            Assert.That(propertyBag.HasProperty("m_Int32Value"), Is.True);
            Assert.That(propertyBag.HasProperty("m_HiddenInt32Value"), Is.False);
        }

        /// <summary>
        /// Tests that the <see cref="ReflectedPropertyBagProvider"/> correctly throws for duplicate <see cref="PropertyAttribute"/> fields.
        /// </summary>
        [Test]
        public void ReflectedPropertyBagGenerator_DuplicateFields()
        {
            var e = Assert.Throws<System.Reflection.TargetInvocationException>(() =>
            {
                new ReflectedPropertyBagProvider().Generate<ClassContainerWithDuplicateFields>();
            });
            Assert.That(e.InnerException, Is.TypeOf<InvalidOperationException>());
        }

        /// <summary>
        /// Tests that the <see cref="ReflectedPropertyBagProvider"/> correctly generates a <see cref="UnmanagedProperty{TContainer,TValue}"/> for char fields.
        /// </summary>
        [Test]
        public void ReflectedPropertyBagGenerator_UnmanagedProperty_Char()
        {
            var propertyBag = new ReflectedPropertyBagProvider().Generate<ContainerWithCharField>();
            var container = default(ContainerWithCharField);
            var changeTracker = default(ChangeTracker);
            var action = new AssertThatPropertyIsOfType<ContainerWithCharField, UnmanagedProperty<ContainerWithCharField, char>>();
            propertyBag.FindProperty("c", ref container, ref changeTracker, ref action);
        }
        
        /// <summary>
        /// Tests that the <see cref="ReflectedPropertyBagProvider"/> correctly generates from property fields.
        /// </summary>
        [Test]
        public void ReflectedPropertyBagGenerator_CSharpProperties()
        {
            var propertyBag = new ReflectedPropertyBagProvider().Generate<ContainerWithProperties>();
            Assert.That(propertyBag.HasProperty("IntProperty"), Is.True);
            Assert.That(propertyBag.HasProperty("HiddenInt32Property"), Is.False);
        }
    }
}
