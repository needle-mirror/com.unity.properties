using System;

namespace Unity.Properties
{
    static partial class Actions
    {
        public static TTargetValue GetValue<TContainer, TTargetValue>(ref TContainer target, PropertyPath propertyPath, int index, ref ChangeTracker changeTracker)
        {
            var action = new GetValueAtPathAction<TContainer, TTargetValue>(propertyPath, index);
            PropertyBagResolver.Resolve<TContainer>()
                .FindProperty(propertyPath[index].Name, ref target, ref changeTracker, ref action);
            return action.Value;
        }
        
        public static TTargetValue VisitProperty<TContainer, TProperty, TValue, TTargetValue>(TProperty property, ref TContainer container, PropertyPath path, int index, ref ChangeTracker changeTracker)
            where TProperty : IProperty<TContainer, TValue>
        {
            if (index < path.PartsCount - 1)
            {
                var sub = property.GetValue(ref container);
                return GetValue<TValue, TTargetValue>(ref sub, path, index + 1, ref changeTracker);
            }
            
            if (TypeConversion.TryConvert(property.GetValue(ref container), out TTargetValue value))
            {
                return value;
            }
            else
            {
                throw new InvalidCastException($"Could not get value of type {typeof(TTargetValue).Name} at `{path}`");
            }
        }

        public static TTargetValue VisitCollectionProperty<TContainer, TProperty, TValue, TTargetValue>(TProperty property, ref TContainer container, PropertyPath path, int index, ref ChangeTracker changeTracker)
            where TProperty : ICollectionProperty<TContainer, TValue>
        {
            var callback = new GetCollectionItemGetter<TContainer, TTargetValue>(path, index);
            property.GetPropertyAtIndex(ref container, path[index].Index, ref changeTracker, ref callback);
            return callback.Value;
        }
    }
    
    struct GetValueAtPathAction<TContainer, TTargetValue> : IPropertyGetter<TContainer>
    {
        private PropertyPath m_Path;
        private int m_Index;

        public TTargetValue Value { get; private set; }

        internal GetValueAtPathAction(PropertyPath propertyMPath, int mIndex)
        {
            m_Path = propertyMPath;
            m_Index = mIndex;
            Value = default;
        }

        void IPropertyGetter<TContainer>.VisitProperty<TProperty, TPropertyValue>(TProperty property, ref TContainer container, ref ChangeTracker changeTracker) =>
            Value = Actions.VisitProperty<TContainer, TProperty, TPropertyValue, TTargetValue>(property, ref container, m_Path, m_Index, ref changeTracker);

        void IPropertyGetter<TContainer>.VisitCollectionProperty<TProperty, TPropertyValue>(TProperty property, ref TContainer container, ref ChangeTracker changeTracker) =>
            Value = Actions.VisitCollectionProperty<TContainer, TProperty, TPropertyValue, TTargetValue>(property, ref container, m_Path, m_Index, ref changeTracker);
    }
    
    struct GetCollectionItemGetter<TContainer, TTargetValue> : ICollectionElementPropertyGetter<TContainer>
    {
        private PropertyPath m_Path;
        private int m_Index;
                
        public TTargetValue Value { get; private set; }

        internal GetCollectionItemGetter(PropertyPath propertyMPath, int mIndex)
        {
            m_Path = propertyMPath;
            m_Index = mIndex;
            Value = default;
        }
                
        void ICollectionElementPropertyGetter<TContainer>.VisitProperty<TProperty, TPropertyValue>(TProperty property, ref TContainer container, ref ChangeTracker changeTracker) => 
            Value = Actions.VisitProperty<TContainer, TProperty, TPropertyValue, TTargetValue>(property, ref container, m_Path, m_Index, ref changeTracker);

        void ICollectionElementPropertyGetter<TContainer>.VisitCollectionProperty<TProperty, TPropertyValue>(TProperty property, ref TContainer container, ref ChangeTracker changeTracker) =>
            Value = Actions.VisitCollectionProperty<TContainer, TProperty, TPropertyValue, TTargetValue>(property, ref container, m_Path, m_Index, ref changeTracker);
    }
}