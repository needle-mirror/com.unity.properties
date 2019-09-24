using System;
using UnityEngine.UIElements;

namespace Unity.Properties.Editor
{
    abstract class BaseFieldInspector<TField, TFieldValue, TValue> : IInspector<TValue>
        where TField : BaseField<TFieldValue>, new()
    {
        protected TField m_Field;
        
        protected abstract Connector<TFieldValue, TValue> Converter { get; }

        public VisualElement Build(InspectorContext<TValue> context)
        {
            m_Field = new TField {label = context.Name};
            m_Field.RegisterValueChangedCallback(evt => { context.Data = Converter.ToValue(evt.newValue); });
            return m_Field;
        }

        public void Update(InspectorContext<TValue> context)
        {
            m_Field.SetValueWithoutNotify(Converter.ToField(context.Data));
        }
    }
    
    abstract class BaseFieldInspector<TField, TValue> : IInspector<TValue>
        where TField : BaseField<TValue>, new()
    {
        protected TField m_Field;

        public VisualElement Build(InspectorContext<TValue> context)
        {
            m_Field = new TField {label = context.Name};
            m_Field.RegisterValueChangedCallback(evt => { context.Data = evt.newValue; });
            return m_Field;
        }

        public void Update(InspectorContext<TValue> context)
        {
            m_Field.SetValueWithoutNotify(context.Data);
        }
    }
}
