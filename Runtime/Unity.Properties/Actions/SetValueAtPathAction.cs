using System;

namespace Unity.Properties
{
    static partial class Actions
    {
        public static void SetValue<TContainer, TTargetValue>(ref TContainer target, PropertyPath propertyPath, int index, TTargetValue value, ref ChangeTracker changeTracker)
        {
            var action = new SetValueAtPathAction<TContainer, TTargetValue>( propertyPath, index, value);
            PropertyBagResolver.Resolve<TContainer>()
                .FindProperty(propertyPath[index].Name, ref target, ref changeTracker, ref action);
        }
        
        public static void VisitProperty<TContainer, TProperty, TPropertyValue, TTargetValue>(TProperty property, ref TContainer container, PropertyPath path, int index, TTargetValue value, ref ChangeTracker changeTracker)
            where TProperty : IProperty<TContainer, TPropertyValue>
        {
            if (index < path.PartsCount - 1)
            {
                var sub = property.GetValue(ref container);
                SetValue(ref sub, path, index + 1, value, ref changeTracker);
                property.SetValue(ref container, sub);
            }
            else
            {
                if (TypeConversion.TryConvert(value, out TPropertyValue convertedValue))
                {
                    property.SetValue(ref container, convertedValue);
                }
                else
                {
                    throw new InvalidCastException($"Could not set value of type {typeof(TTargetValue).Name} at `{path}`");
                }
            }
        }
        
        public static void VisitCollectionProperty<TContainer, TProperty, TPropertyValue, TTargetValue>(TProperty property, ref TContainer container, PropertyPath path, int index, TTargetValue value, ref ChangeTracker changeTracker)
            where TProperty : ICollectionProperty<TContainer, TPropertyValue>
        {
            var getter = new SetCollectionItemGetter<TContainer, TTargetValue>(path, index, value);
            property.GetPropertyAtIndex(ref container, path[index].Index, ref changeTracker, ref getter);
        }
    }
    
    readonly struct SetValueAtPathAction<TContainer, TTargetValue> : IPropertyGetter<TContainer>
    {
        private readonly PropertyPath m_Path;
        private readonly TTargetValue m_Value;
        private readonly int m_Index;

        internal SetValueAtPathAction(PropertyPath propertyMPath, int mIndex, TTargetValue mValue)
        {
            m_Path = propertyMPath;
            m_Index = mIndex;
            m_Value = mValue;
        }

        void IPropertyGetter<TContainer>.VisitProperty<TProperty, TPropertyValue>(TProperty property, ref TContainer container, ref ChangeTracker changeTracker) => 
            Actions.VisitProperty<TContainer, TProperty, TPropertyValue, TTargetValue>(property, ref container, m_Path, m_Index, m_Value, ref changeTracker);

        void IPropertyGetter<TContainer>.VisitCollectionProperty<TProperty, TPropertyValue>(TProperty property, ref TContainer container, ref ChangeTracker changeTracker) =>
            Actions.VisitCollectionProperty<TContainer, TProperty, TPropertyValue, TTargetValue>(property, ref container, m_Path, m_Index, m_Value, ref changeTracker);
    }

    readonly struct SetCollectionItemGetter<TContainer, TTargetValue> : ICollectionElementPropertyGetter<TContainer>
    {
        private readonly PropertyPath m_Path;
        private readonly TTargetValue m_Value;
        private readonly int m_Index;
                
        internal SetCollectionItemGetter(PropertyPath propertyMPath, int mIndex, TTargetValue value)
        {
            m_Path = propertyMPath;
            m_Index = mIndex;
            m_Value = value;
        }
                
        void ICollectionElementPropertyGetter<TContainer>.VisitProperty<TProperty, TPropertyValue>(TProperty property, ref TContainer container, ref ChangeTracker changeTracker) => 
            Actions.VisitProperty<TContainer, TProperty, TPropertyValue, TTargetValue>(property, ref container, m_Path, m_Index, m_Value, ref changeTracker);

        void ICollectionElementPropertyGetter<TContainer>.VisitCollectionProperty<TProperty, TPropertyValue>(TProperty property, ref TContainer container, ref ChangeTracker changeTracker) =>
            Actions.VisitCollectionProperty<TContainer, TProperty, TPropertyValue, TTargetValue>(property, ref container, m_Path, m_Index, m_Value, ref changeTracker);
    }
}