using NUnit.Framework;

namespace Unity.Properties.Tests
{
    [TestFixture]
    partial class PropertyContainerConstructTests
    {
        [Test]
        public void PropertyContainer_Construct_ArrayFieldConstructsNewInstance()
        {
            var src = new ClassContainerWithArrays {IntArray = new int[5]};
            var dst = new ClassContainerWithArrays {IntArray = null};

            PropertyContainer.Construct(ref dst, ref src);

            Assert.That(dst.IntArray, Is.Not.Null);
            Assert.That(!ReferenceEquals(src.IntArray, dst.IntArray));
        }
        
        [Test]
        public void PropertyContainer_Construct_ArrayFieldAssignsNull()
        {
            var src = new ClassContainerWithArrays {IntArray = null};
            var dst = new ClassContainerWithArrays {IntArray = new int[5]};

            PropertyContainer.Construct(ref dst, ref src);

            Assert.That(dst.IntArray, Is.Null);
        }
    }
}