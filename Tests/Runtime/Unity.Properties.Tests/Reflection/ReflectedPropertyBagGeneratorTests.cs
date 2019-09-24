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

        struct ContainerWithPrivateProperties
        {
            [Property] int IntProperty { get; }
            int HiddenInt32Property { get; }
        }

        class ClassContainerWithBaseClassAndPrivateFields : BaseClassContainerWithPrivateFields
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

        class ClassContainerWithBaseClassAndPrivateProperties : BaseClassContainerWithPrivateProperties
        {
#pragma warning disable 649
            [Property] public bool BoolProperty { get; }
#pragma warning restore 649
        }

        class BaseClassContainerWithPrivateProperties : AnotherBaseClassContainerWithPrivateProperties
        {
#pragma warning disable 649
            [Property] float FloatProperty { get; }
            float HiddenFloatProperty { get; }
#pragma warning restore 649
        }

        class AnotherBaseClassContainerWithPrivateProperties
        {
#pragma warning disable 649
            [Property] int Int32Property { get; }
            int HiddenInt32Property { get; }
#pragma warning restore 649
        }

        class ClassContainerWithDuplicateFields : BaseClassContainerWithPrivateFields
        {
#pragma warning disable 649
            [Property] float m_FloatValue;
            float m_HiddenFloatValue;
#pragma warning restore 649
        }

        class ClassContainerWithDuplicateProperties : BaseClassContainerWithPrivateProperties
        {
#pragma warning disable 649
            [Property] float FloatProperty { get; }
            float HiddenFloatProperty { get; }
#pragma warning restore 649
        }

        class ClassContainerWithoutFields : BaseClassContainerWithInternalField
        {
        }

        class BaseClassContainerWithInternalField
        {
#pragma warning disable 649
            [Property] internal int m_IntValue;
            internal float m_HiddenFloatValue;
#pragma warning restore 649
        }

        class ClassContainerWithVirtualField
        {
#pragma warning disable 649
            public int m_IntValue;
#pragma warning restore 649
            [Property] public virtual float m_FloatValue => 5.0f;
        }

        class ClassContainerWithOverrideField : ClassContainerWithVirtualField
        {
#pragma warning disable 649
            public new int m_IntValue;
#pragma warning restore 649
            [Property] public override float m_FloatValue => 10.0f;
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

        struct AssertThatPropertyValueIsEqualTo<TContainer, TExpected> : IPropertyGetter<TContainer>
        {
            public TExpected ExpectedValue;

            public void VisitProperty<TProperty, TValue>(TProperty property, ref TContainer container, ref ChangeTracker changeTracker)
                where TProperty : IProperty<TContainer, TValue>
            {
                Assert.That(property.GetValue(ref container), Is.EqualTo(ExpectedValue));
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
            var propertyBag = new ReflectedPropertyBagProvider().Generate<ClassContainerWithBaseClassAndPrivateFields>();
            Assert.That(propertyBag.HasProperty("m_BoolValue"), Is.True);
            Assert.That(propertyBag.HasProperty("m_FloatValue"), Is.True);
            Assert.That(propertyBag.HasProperty("m_HiddenFloatValue"), Is.False);
            Assert.That(propertyBag.HasProperty("m_Int32Value"), Is.True);
            Assert.That(propertyBag.HasProperty("m_HiddenInt32Value"), Is.False);
        }

        /// <summary>
        /// Tests that the <see cref="ReflectedPropertyBagProvider"/> correctly generates properties for private <see cref="PropertyAttribute"/> properties.
        /// </summary>
        [Test]
        public void ReflectedPropertyBagGenerator_PrivateProperties()
        {
            var propertyBag = new ReflectedPropertyBagProvider().Generate<ContainerWithPrivateProperties>();
            Assert.That(propertyBag.HasProperty("IntProperty"), Is.True);
            Assert.That(propertyBag.HasProperty("HiddenInt32Property"), Is.False);
        }

        /// <summary>
        /// Tests that the <see cref="ReflectedPropertyBagProvider"/> correctly generates properties for base class private <see cref="PropertyAttribute"/> properties.
        /// </summary>
        [Test]
        public void ReflectedPropertyBagGenerator_BaseClassPrivateProperties()
        {
            var propertyBag = new ReflectedPropertyBagProvider().Generate<ClassContainerWithBaseClassAndPrivateProperties>();
            Assert.That(propertyBag.HasProperty("BoolProperty"), Is.True);
            Assert.That(propertyBag.HasProperty("FloatProperty"), Is.True);
            Assert.That(propertyBag.HasProperty("HiddenFloatProperty"), Is.False);
            Assert.That(propertyBag.HasProperty("Int32Property"), Is.True);
            Assert.That(propertyBag.HasProperty("HiddenInt32Property"), Is.False);
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
        /// Tests that the <see cref="ReflectedPropertyBagProvider"/> correctly throws for duplicate <see cref="PropertyAttribute"/> properties.
        /// </summary>
        [Test]
        public void ReflectedPropertyBagGenerator_DuplicateProperties()
        {
            var e = Assert.Throws<System.Reflection.TargetInvocationException>(() =>
            {
                new ReflectedPropertyBagProvider().Generate<ClassContainerWithDuplicateProperties>();
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
            Assert.That(propertyBag.FindProperty("c", ref container, ref changeTracker, ref action), Is.True);
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

        /// <summary>
        /// Tests that the <see cref="ReflectedPropertyBagProvider"/> correctly generates from internal property fields in base class.
        /// </summary>
        [Test]
        public void ReflectedPropertyBagGenerator_BaseClassInternalFields()
        {
            var propertyBag = new ReflectedPropertyBagProvider().Generate<ClassContainerWithoutFields>();
            Assert.That(propertyBag.HasProperty("m_IntValue"), Is.True);
            Assert.That(propertyBag.HasProperty("m_HiddenFloatValue"), Is.False);
        }

        /// <summary>
        /// Tests that the <see cref="ReflectedPropertyBagProvider"/> correctly generates from new and virtual/override fields.
        /// </summary>
        [Test]
        public void ReflectedPropertyBagGenerator_ClassOverrideFields()
        {
            var propertyBag = new ReflectedPropertyBagProvider().Generate<ClassContainerWithOverrideField>();
            Assert.That(propertyBag.HasProperty("m_IntValue"), Is.True);
            Assert.That(propertyBag.HasProperty("m_FloatValue"), Is.True);
        }

        /// <summary>
        /// Tests that the<see cref="ReflectedPropertyBagProvider"/> stores the correct property from virtual field.
        /// </summary>
        [Test]
        public void ReflectedPropertyBagGenerator_ClassVirtualFieldValue()
        {
            var propertyBag = new ReflectedPropertyBagProvider().Generate<ClassContainerWithVirtualField>();
            var container = new ClassContainerWithVirtualField();
            var changeTracker = default(ChangeTracker);
            var action = new AssertThatPropertyValueIsEqualTo<ClassContainerWithVirtualField, float> { ExpectedValue = 5.0f };
            Assert.That(propertyBag.FindProperty("m_FloatValue", ref container, ref changeTracker, ref action), Is.True);
        }

        /// <summary>
        /// Tests that the<see cref="ReflectedPropertyBagProvider"/> stores the correct property from overridden field.
        /// </summary>
        [Test]
        public void ReflectedPropertyBagGenerator_ClassOverriddenFieldValue()
        {
            var propertyBag = new ReflectedPropertyBagProvider().Generate<ClassContainerWithOverrideField>();
            var container = new ClassContainerWithOverrideField();
            var changeTracker = default(ChangeTracker);
            var action = new AssertThatPropertyValueIsEqualTo<ClassContainerWithOverrideField, float> { ExpectedValue = 10.0f };
            Assert.That(propertyBag.FindProperty("m_FloatValue", ref container, ref changeTracker, ref action), Is.True);
        }
    }
}
