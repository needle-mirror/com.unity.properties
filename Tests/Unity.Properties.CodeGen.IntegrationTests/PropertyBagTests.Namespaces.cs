using Blah.A.B;
using NUnit.Framework;
using Unity.Properties;

namespace Blah
{
    namespace A.B
    {
        public class Test
        {
            public class Foo
            {
                public class Bar
                {
                    [GeneratePropertyBag]
                    public class ClassWithNestedNamespacesAndTypes
                    {

                    }
                }
            }
        }
    }
}

namespace Unity.Properties.CodeGen.IntegrationTests
{
    namespace NestedNamespace
    {
        // ReSharper disable once ArrangeTypeModifiers
        [GeneratePropertyBag]
        class ClassWithMultipleNamespaceScopes
        {
#pragma warning disable 649
            public int Value;
#pragma warning restore 649
        }
    }

    [TestFixture]
    sealed partial class SourceGeneratorsTestFixture
    {
        [Test]
        public void ClassWithMultipleNamespaceScopes_HasPropertyBagGenerated()
        {
            AssertPropertyBagIsCodeGenerated<NestedNamespace.ClassWithMultipleNamespaceScopes>();
        }

        [Test]
        public void ClassWithNestedNamespacesAndTypes_HasPropertyBagGenerated()
        {
            AssertPropertyBagIsCodeGenerated<Test.Foo.Bar.ClassWithNestedNamespacesAndTypes>();
        }
    }
}
