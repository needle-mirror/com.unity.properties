using System;
using NUnit.Framework;

namespace Unity.Properties.Tests
{
    [TestFixture]
    partial class PropertyContainerConstructTests
    {
        [Test]
        public void PropertyContainer_Construct_AbstractFieldDoesNotCreateNewInstance()
        {
            var src = new ClassContainerWithAbstractField {Container = new DerivedClassA()};
            var dst = new ClassContainerWithAbstractField {Container = new DerivedClassA()};

            var reference = dst.Container;

            PropertyContainer.Construct(ref dst, ref src);

            Assert.That(ReferenceEquals(reference, dst.Container));
            Assert.That(!ReferenceEquals(src.Container, dst.Container));
        }

        [Test]
        public void PropertyContainer_Construct_AbstractFieldConstructsInstanceOfDifferentDerivedType()
        {
            var src = new ClassContainerWithAbstractField {Container = new DerivedClassA()};
            var dst = new ClassContainerWithAbstractField {Container = new DerivedClassB()};

            PropertyContainer.Construct(ref dst, ref src);

            Assert.That(dst.Container, Is.Not.Null);
            Assert.That(dst.Container, Is.TypeOf<DerivedClassA>());
        }

        [Test]
        public void PropertyContainer_Construct_AbstractFieldConstructsNewInstanceWithCorrectDerivedType()
        {
            var src = new ClassContainerWithAbstractField {Container = new DerivedClassA {BaseIntValue = 1, A = 5}};
            var dst = new ClassContainerWithAbstractField {Container = null};

            PropertyContainer.Construct(ref dst, ref src);

            Assert.That(dst.Container, Is.Not.Null);
            Assert.That(dst.Container, Is.TypeOf<DerivedClassA>());
        }

        [Test]
        public void PropertyContainer_Construct_AbstractFieldConstructsNewInstanceFromDynamicSourceType()
        {
            var src = new StructContainerWithNestedDynamicContainer {Container = new DynamicContainer(typeof(DerivedClassA).AssemblyQualifiedName)};
            var dst = new ClassContainerWithAbstractField {Container = null};

            PropertyContainer.Construct(ref dst, ref src, new PropertyContainerConstructOptions {TypeIdentifierKey = DynamicContainer.TypeIdentifierKey});

            Assert.That(dst.Container, Is.Not.Null);

            src = new StructContainerWithNestedDynamicContainer {Container = new DynamicContainer("unknown type")};
            dst = new ClassContainerWithAbstractField {Container = null};

            Assert.Throws<InvalidOperationException>(() => { PropertyContainer.Construct(ref dst, ref src, new PropertyContainerConstructOptions {TypeIdentifierKey = DynamicContainer.TypeIdentifierKey}); });
        }
    }
}