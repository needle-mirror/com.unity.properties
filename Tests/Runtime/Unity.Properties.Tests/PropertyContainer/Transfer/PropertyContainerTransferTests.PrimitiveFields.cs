using NUnit.Framework;

namespace Unity.Properties.Tests
{
    [TestFixture]
    partial class PropertyContainerTransferTests
    {
        [Test]
        public void PropertyContainer_Transfer_PrimitiveFields()
        {
            var src = new StructContainerWithPrimitives {Int32Value = 10};
            var dst = new StructContainerWithPrimitives {Int32Value = 20};

            PropertyContainer.Transfer(ref dst, ref src);

            Assert.AreEqual(10, dst.Int32Value);
        }
        
        [Test]
        public void PropertyContainer_Transfer_NestedPrimitiveFieldsWithDifferentContainerTypes()
        {
            var src = new StructContainerWithNestedStruct {Container = new StructContainerWithPrimitives {Int32Value = 10}};
            var dst = new ClassContainerWithNestedClass {Container = new ClassContainerWithPrimitives()};

            Assert.DoesNotThrow(() =>
            {
                PropertyContainer.Transfer(ref dst, ref src);
            });

            Assert.That(dst.Container.Int32Value, Is.EqualTo(10));
        }
        
        [Test]
        public void PropertyContainer_Transfer_FlagsEnumFields()
        {
            var src = new StructContainerWithPrimitives {FlagsEnum = FlagsEnum.Value1 | FlagsEnum.Value4 };
            var dst = new StructContainerWithPrimitives {FlagsEnum = FlagsEnum.None};

            PropertyContainer.Transfer(ref dst, ref src);

            Assert.AreEqual(FlagsEnum.Value1 | FlagsEnum.Value4, dst.FlagsEnum);
        }
    }
}