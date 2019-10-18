using NUnit.Framework;

namespace Unity.Properties.Tests
{
    [TestFixture]
    partial class PropertyContainerTransferTests
    {
        [Test]
        public void PropertyContainer_Transfer_ReferenceFieldCopiesReference()
        {
            var src = new ClassContainerWithNestedClass {Container = new ClassContainerWithPrimitives()};
            var dst = new ClassContainerWithNestedClass {Container = null};

            PropertyContainer.Transfer(ref dst, ref src);

            Assert.That(dst.Container, Is.Not.Null);
            Assert.That(ReferenceEquals(src.Container, dst.Container));
        }
        
        [Test]
        public void PropertyContainer_Transfer_ReferenceFieldAssignsNull()
        {
            var src = new ClassContainerWithNestedClass {Container = null};
            var dst = new ClassContainerWithNestedClass {Container = new ClassContainerWithPrimitives()};
            
            PropertyContainer.Transfer(ref dst, ref src);

            Assert.That(dst.Container, Is.Null);
        }
        
        [Test]
        public void PropertyContainer_Transfer_ReferenceFieldWithDifferentSourceTypeDoesNotThrow()
        {
            var src = new StructContainerWithNestedStruct {Container = new StructContainerWithPrimitives()};
            var dst = new ClassContainerWithNestedClass {Container = null};

            Assert.DoesNotThrow(() =>
            {
                PropertyContainer.Transfer(ref dst, ref src);
            });

            Assert.That(dst.Container, Is.Null);
        }
    }
}