using NUnit.Framework;

namespace Unity.Properties.Tests
{
    [TestFixture]
    internal class PropertyPathTests
    {
        [Test]
        public void CanConstructPropertyPathManually()
        {
            var propertyPath = new PropertyPath();
            Assert.That(propertyPath.PartsCount, Is.EqualTo(0));
            propertyPath.Push("Foo");
            Assert.That(propertyPath.PartsCount, Is.EqualTo(1));
            Assert.That(propertyPath[0].IsListItem, Is.EqualTo(false));
            Assert.That(propertyPath[0].Name, Is.EqualTo("Foo"));
            Assert.That(propertyPath[0].Index, Is.EqualTo(-1));
            
            propertyPath.Push("Bar", 5);
            Assert.That(propertyPath.PartsCount, Is.EqualTo(2));
            Assert.That(propertyPath[1].IsListItem, Is.EqualTo(true));
            Assert.That(propertyPath[1].Name, Is.EqualTo("Bar"));
            Assert.That(propertyPath[1].Index, Is.EqualTo(5));
            
            propertyPath.Push("Bee", PropertyPath.InvalidListIndex);
            Assert.That(propertyPath.PartsCount, Is.EqualTo(3));
            Assert.That(propertyPath[2].IsListItem, Is.EqualTo(false));
            Assert.That(propertyPath[2].Name, Is.EqualTo("Bee"));
            Assert.That(propertyPath[2].Index, Is.EqualTo(-1));
            
            Assert.That(propertyPath.ToString(), Is.EqualTo("Foo.Bar[5].Bee"));
            
            propertyPath.Pop();
            
            Assert.That(propertyPath.PartsCount, Is.EqualTo(2));
            Assert.That(propertyPath.ToString(), Is.EqualTo("Foo.Bar[5]"));
            
            propertyPath.Clear();
            
            Assert.That(propertyPath.PartsCount, Is.EqualTo(0));
            Assert.That(propertyPath.ToString(), Is.EqualTo(string.Empty));
        }

        [Test]
        [TestCase("")]
        [TestCase("Foo")]
        [TestCase("Foo[0]")]
        [TestCase("Foo[0].Bar")]
        [TestCase("Foo[0].Bar[1]")]
        [TestCase("Foo.Bar")]
        [TestCase("Foo.Bar[0]")]
        public void CanConstructAPropertyPathFromAString(string path)
        {
            Assert.That(() => CreateFromString(path), Throws.Nothing);
        }

        [Test]
        [TestCase("", 0)]
        [TestCase("Foo", 1)]
        [TestCase("Foo[0]", 1)]
        [TestCase("Foo[0].Bar", 2)]
        [TestCase("Foo[0].Bar[1]", 2)]
        [TestCase("Foo.Bar", 2)]
        [TestCase("Foo.Bar[0]", 2)]
        [TestCase("Foo.Foo.Foo.Foo.Foo", 5)]
        public void PropertyPathHasTheRightAmountOfParts(string path, int partCount)
        {
            var propertyPath = new PropertyPath(path);
            Assert.That(propertyPath.PartsCount, Is.EqualTo(partCount));
        }

        [Test]
        [TestCase("Foo[0]", 0)]
        [TestCase("Foo[1]", 1)]
        [TestCase("Foo.Bar[2]", 2)]
        [TestCase("Foo.Bar[12]", 12)]
        [TestCase("Foo[0].Foo[1].Foo[2].Foo[3].Foo[4]", 0, 1, 2, 3, 4)]
        public void PropertyPathMapsListIndicesCorrectly(string path, params int[] indices)
        {
            var propertyPath = new PropertyPath(path);
            var listIndex = 0;
            for (var i = 0; i < propertyPath.PartsCount; ++i)
            {
                var part = propertyPath[i];
                if (part.IsListItem)
                {
                    Assert.That(part.Index, Is.EqualTo(indices[listIndex]));
                    ++listIndex;
                }
            }
        }
        
        [Test]
        [TestCase("Foo[-1]")]
        [TestCase("Foo.Bar[-20]")]
        public void ThrowsWhenUsingNegativeIndices(string path)
        {
            Assert.That(() => CreateFromString(path), Throws.ArgumentException);
        }
        
        [Test]
        [TestCase("Foo[lol]")]
        [TestCase("Foo.Bar[\"one\"]")]
        public void ThrowsWhenUsingNonNumericIndices(string path)
        {
            Assert.That(() => CreateFromString(path), Throws.ArgumentException);
        }
        
        [Test]
        [TestCase("[0]")]
        [TestCase("[1].Bar")]
        public void ThrowsOnUsingIndicesAtPathRoot(string path)
        {
            Assert.That(() => CreateFromString(path), Throws.ArgumentException);
        }

        [Test]
        public void CanSetValueAtPath()
        {
            var container = new PropertyPathTestContainer();
            Assert.That(container.Float, Is.EqualTo(5.0f));
            Assert.That(container.Strings, Is.EqualTo(new []{"one", "two", "three"}));
            Assert.That(container.Nested.Int, Is.EqualTo(15));
            Assert.That(container.Nested.Doubles, Is.EqualTo(new []{1.0, 2.0, 3.0}));
            
            PropertyContainer.SetValueAtPath(ref container, new PropertyPath("Float"), 20.0f);
            Assert.That(container.Float, Is.EqualTo(20.0f));
            
            PropertyContainer.SetValueAtPath(ref container, new PropertyPath("Strings[1]"), "four");
            Assert.That(container.Strings, Is.EqualTo(new []{"one", "four", "three"}));
            
            PropertyContainer.SetValueAtPath(ref container, new PropertyPath("Nested.Int"), 5);
            Assert.That(container.Nested.Int, Is.EqualTo(5));
            
            PropertyContainer.SetValueAtPath(ref container, new PropertyPath("Nested.Doubles[2]"), 6.0);
            Assert.That(container.Nested.Doubles, Is.EqualTo(new []{1.0, 2.0, 6.0}));
        }
        
        [Test]
        public void CanGetValueAtPath()
        {
            var container = new PropertyPathTestContainer();
            Assert.That(container.Float, Is.EqualTo(5.0f));
            Assert.That(container.Strings, Is.EqualTo(new []{"one", "two", "three"}));
            Assert.That(container.Nested.Int, Is.EqualTo(15));
            Assert.That(container.Nested.Doubles, Is.EqualTo(new []{1.0, 2.0, 3.0}));

            container.Float = 20.0f;
            Assert.That(PropertyContainer.GetValueAtPath<PropertyPathTestContainer, float>(ref container, new PropertyPath("Float")), Is.EqualTo(container.Float));

            container.Strings[1] = "four";
            Assert.That(PropertyContainer.GetValueAtPath<PropertyPathTestContainer, string>(ref container, new PropertyPath("Strings[1]")), Is.EqualTo("four"));

            container.Nested.Int = 5;
            
            Assert.That(PropertyContainer.GetValueAtPath<PropertyPathTestContainer, int>(ref container, new PropertyPath("Nested.Int")), Is.EqualTo(container.Nested.Int));

            container.Nested.Doubles[2] = 6.0;
            Assert.That(PropertyContainer.GetValueAtPath<PropertyPathTestContainer, double>(ref container, new PropertyPath("Nested.Doubles[2]")), Is.EqualTo(6.0));
        }
        
        private static PropertyPath CreateFromString(string path)
        {
            return new PropertyPath(path);
        }
    }
}