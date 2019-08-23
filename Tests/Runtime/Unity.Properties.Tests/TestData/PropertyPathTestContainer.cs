using System.Collections.Generic;

namespace Unity.Properties.Tests
{
    public class PropertyPathTestContainer
    {
        public struct NestedPropertyPathTestContainer
        {
            public int Int;

            public List<double> Doubles;
        }
        
        public float Float = 5.0f;
        public List<string> Strings = new List<string>
        {
            "one", "two", "three"
        };

        public NestedPropertyPathTestContainer Nested = new NestedPropertyPathTestContainer
        {
            Int = 15,
            Doubles = new List<double> {1.0, 2.0, 3.0}
        };

    }
}