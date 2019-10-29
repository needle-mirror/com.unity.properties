using UnityEditor;

namespace Unity.Properties
{
    /// <summary>
    /// No need to ever use this class or call its functions. These only exist to be invoked in generated code paths
    /// (which themselves will never be called at runtime) only to hint to the Ahead Of Time compiler which types
    /// to generate specialized function bodies for.
    /// </summary>
    public static class AOTFunctionGenerator
    {
        public static void GenerateAOTFunctions<TProperty, TContainer, TValue>()
            where TProperty : IProperty<TContainer, TValue>
        {
            TProperty property = default(TProperty);
            TContainer container = default(TContainer);
            TValue value = default(TValue);
            ChangeTracker changeTracker = default(ChangeTracker);
            
            PropertyVisitor propertyVisitor = new PropertyVisitor();
            propertyVisitor.TryVisitContainerWithAdapters(property, ref container, ref value, ref changeTracker);
            propertyVisitor.TryVisitValueWithAdapters(property, ref container, ref value, ref changeTracker);
            PropertyVisitorAdapterExtensions.TryVisitContainer(null, null, property, ref container, ref value, ref changeTracker);
            PropertyVisitorAdapterExtensions.TryVisitValue(null, null, property, ref container, ref value, ref changeTracker);
        }
        
        public static void GenerateAOTCollectionFunctions<TProperty, TContainer, TValue>()
            where TProperty : ICollectionProperty<TContainer, TValue>
        {
            TProperty property = default(TProperty);
            TContainer container = default(TContainer);
            TValue value = default(TValue);
            ChangeTracker changeTracker = default(ChangeTracker);
            var getter = new VisitCollectionElementCallback<TContainer>();
            
            PropertyVisitor propertyVisitor = new PropertyVisitor();
            propertyVisitor.TryVisitContainerWithAdapters(property, ref container, ref value, ref changeTracker);
            propertyVisitor.TryVisitCollectionWithAdapters(property, ref container, ref value, ref changeTracker);
            propertyVisitor.TryVisitValueWithAdapters(property, ref container, ref value, ref changeTracker);
            PropertyVisitorAdapterExtensions.TryVisitCollection(null, null, property, ref container, ref value, ref changeTracker);
            PropertyVisitorAdapterExtensions.TryVisitContainer(null, null, property, ref container, ref value, ref changeTracker);
            PropertyVisitorAdapterExtensions.TryVisitValue(null, null, property, ref container, ref value, ref changeTracker);
            
            var arrayProperty = new ArrayProperty<TContainer, TValue>();
            arrayProperty.GetPropertyAtIndex(ref container, 0, ref changeTracker, ref getter);
            var arrayCollectionElementProperty = new ArrayProperty<TContainer, TValue>.CollectionElementProperty();
            arrayCollectionElementProperty.GetValue(ref container);
            arrayCollectionElementProperty.SetValue(ref container, value);
            propertyVisitor.VisitProperty<ArrayProperty<TContainer, TValue>.CollectionElementProperty, TContainer, TValue>(arrayCollectionElementProperty, ref container, ref changeTracker);
            
            var listProperty = new ListProperty<TContainer, TValue>();
            listProperty.GetPropertyAtIndex(ref container, 0, ref changeTracker, ref getter);
            var listCollectionElementProperty = new ListProperty<TContainer, TValue>.CollectionElementProperty();
            listCollectionElementProperty.GetValue(ref container);
            listCollectionElementProperty.SetValue(ref container, value);
            propertyVisitor.VisitProperty<ListProperty<TContainer, TValue>.CollectionElementProperty, TContainer, TValue>(listCollectionElementProperty, ref container, ref changeTracker);
        }
    }
}