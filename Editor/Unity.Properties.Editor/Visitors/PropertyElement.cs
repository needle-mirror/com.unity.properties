using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.Properties.Editor
{
    public class PropertyElement : BindableElement, IBinding
    {
        interface IBindingTarget
        {
            void Visit(PropertyElement context);
            bool Visit(PropertyElement context, VisualElement parent, PropertyPath path);
            void RegisterBindings(PropertyElement context);
            void UnregisterBindings(PropertyElement context);
            void SetValue<TValue>(PropertyPath path, TValue value);
            TValue GetValue<TValue>(PropertyPath path);
            T GetTarget<T>();
            void Refresh(bool force);
            bool TrySetCount(PropertyPath path, int count);
        }

        class BindingTarget<TTarget> : IBindingTarget
        {
            TTarget m_Target;
            PropertyElement m_Element;

            public BindingTarget(TTarget target)
            {
                m_Target = target;
            }

            public void Visit(PropertyElement context)
            {
                m_Element = context;
                if (null != m_Target)
                {
                    var visitor = new BindingVisitor(context);
                    PropertyContainer.Visit(m_Target, visitor);
                    visitor.GetElementToFullPathMappings(context.m_LastBindingsFound);
                }
            }

            public bool Visit(PropertyElement context, VisualElement parent, PropertyPath path)
            {
                m_Element = context;
                var visitor = new InspectorVisitor<TTarget>(m_Element, m_Target);
                visitor.AddAdapter(new UnityObjectAdapter<TTarget>(visitor));
                visitor.AddAdapter(new NullAdapter<TTarget>(visitor));
                visitor.AddAdapter(new PrimitivesAdapter<TTarget>(visitor));
                var changerTracker = new ChangeTracker();
                using (visitor.VisitorContext.MakeParentScope(parent))
                {
                    return PropertyContainer.TryVisitAtPath(ref m_Target, path, visitor, ref changerTracker);
                }
            }

            public void RegisterBindings(PropertyElement context)
            {
                m_Element = context;
                if (null != m_Target)
                {
                    PropertyContainer.Visit(m_Target,
                        new BindingVisitor(context, BindingVisitor.BindingRegistration.Register));
                }
            }

            public void UnregisterBindings(PropertyElement context)
            {
                m_Element = context;
                if (null != m_Target)
                {
                    PropertyContainer.Visit(m_Target,
                        new BindingVisitor(context, BindingVisitor.BindingRegistration.Unregister));
                }
            }
            
            public void SetValue<TValue>(PropertyPath path, TValue value)
            {
                PropertyContainer.SetValueAtPath(ref m_Target, path, value);
            }
            
            public TValue GetValue<TValue>(PropertyPath path)
            {
                return PropertyContainer.GetValueAtPath<TTarget, TValue>(ref m_Target, path);
            }

            public T GetTarget<T>()
            {
                return TypeConversion.TryConvert<TTarget, T>(m_Target, out var val) ? val : default;
            }
            
            public void Refresh(bool force)
            {
                if (force)
                {
                    m_Element.ForceSetTarget(m_Target);
                }
                else
                {
                    m_Element.SetTarget(m_Target);
                }
            }

            public bool TrySetCount(PropertyPath propertyPath, int count)
            {
                return PropertyContainer.TrySetCountAtPath(ref m_Target, propertyPath, count);
            }
        }

        public delegate void ChangeHandler(PropertyElement element);

        IBindingTarget m_BindingTarget;
        readonly Dictionary<VisualElement, string> m_LastBindingsFound = new Dictionary<VisualElement, string>();

        public event ChangeHandler OnChanged = delegate { };

        public T GetTarget<T>()
        {
            return null == m_BindingTarget ? default : m_BindingTarget.GetTarget<T>();
        }

        private void SetTargetImpl<T>(T target)
        {
            Clear();
            if (null != target)
            {
                m_BindingTarget?.UnregisterBindings(this);
                m_BindingTarget = new BindingTarget<T>(target);
                var visitor = new InspectorVisitor<T>(this, target);
                visitor.AddAdapter(new UnityObjectAdapter<T>(visitor));
                visitor.AddAdapter(new NullAdapter<T>(visitor));
                visitor.AddAdapter(new PrimitivesAdapter<T>(visitor));
                using (visitor.VisitorContext.MakeParentScope(this))
                {
                    PropertyContainer.Visit(new PropertyWrapper<T>(target), visitor);
                }

                m_BindingTarget.RegisterBindings(this);
                OnUpdate();
            }
            else
            {
                m_BindingTarget?.UnregisterBindings(this);
                m_BindingTarget = null;
            }
        }

        void ForceSetTarget<T>(T target)
        {
            SetTargetImpl(target);
        }
        
        public void SetTarget<T>(T target)
        {
            if (null != m_BindingTarget)
            {
                var current = m_BindingTarget.GetTarget<T>();
                if ((null != target && target.Equals(current)) || StructureValidation.SameStructure(ref target, ref current))
                {
                    m_BindingTarget = new BindingTarget<T>(target);
                    OnUpdate();
                    return;
                }
            }

            SetTargetImpl(target);
        }

        internal bool TryVisitAtPath(VisualElement root, PropertyPath path)
        {
            return m_BindingTarget.Visit(this, root, path);
        }

        public PropertyElement()
        {
            binding = this;
        }

        void IBinding.PreUpdate()
        {
            // Nothing to do
        }

        void IBinding.Update()
        {
            OnUpdate();
        }

        void IBinding.Release()
        {
            // Nothing to do
        }

        public void SetValueAtPath<TValueType>(PropertyPath path, TValueType value)
        {
            m_BindingTarget.SetValue(path, value);
            NotifyChanged();
        }
        
        public TValueType GetValueAtPath<TValueType>(PropertyPath path)
        {
            return m_BindingTarget.GetValue<TValueType>(path);
        }
        
        public TValueType SetValue<TValueType>(VisualElement field, TValueType value)
        {
            if (!m_LastBindingsFound.TryGetValue(field, out var path))
            {
                return value;
            }
            
            m_BindingTarget.SetValue(new PropertyPath(path), value);
            var newValue = m_BindingTarget.GetValue<TValueType>(new PropertyPath(path));
            NotifyChanged();
            return newValue;
        }

        public void SetCount(VisualElement field, int count)
        {
            if (!m_LastBindingsFound.TryGetValue(field, out var path))
            {
                return;
            }

            if (!m_BindingTarget.TrySetCount(new PropertyPath(path), count))
            {
                return;
            }
            
            Refresh(true);
            NotifyChanged();
        }

        void OnUpdate()
        {
            m_BindingTarget?.Visit(this);
        }

        public void Refresh(bool force = false)
        {
            m_BindingTarget.Refresh(force);
        }

        internal void NotifyChanged()
        {
            OnChanged(this);
        }
    }
}
