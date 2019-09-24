using System.Text;
using UnityEngine;

namespace Unity.Properties.Editor
{
    class InspectorVisitor<T> : PropertyVisitor
    {
        struct GetCustomInspectorCallback : IContainerTypeCallback
        {
            readonly InspectorVisitor<T> m_Visitor;
            readonly PropertyPath m_PropertyPath;
            readonly string m_PropertyName;
            readonly InspectorVisitLevel m_VisitLevel;
            bool Visited { get; set; }

            GetCustomInspectorCallback(InspectorVisitor<T> visitor, PropertyPath propertyPath, string propertyName,
                InspectorVisitLevel visitLevel)
            {
                m_Visitor = visitor;
                Visited = false;
                m_PropertyPath = propertyPath;
                m_PropertyName = propertyName;
                m_VisitLevel = visitLevel;
            }

            public static bool Execute(object container, InspectorVisitor<T> visitor, PropertyPath path,
                string propertyName, InspectorVisitLevel visitLevel)
            {
                var action = new GetCustomInspectorCallback(visitor, path, propertyName, visitLevel);
                PropertyBagResolver.Resolve(container.GetType()).Cast(ref action);
                return action.Visited;
            }

            public void Invoke<TValueType>()
            {
                Visited = TryGetInspector<TValueType>(m_Visitor, m_PropertyPath, m_PropertyName, m_VisitLevel);;
            }
        }

        public readonly T Target;
        readonly StringBuilder m_Path = new StringBuilder(64, 1024);

        public InspectorVisitorContext VisitorContext { get; }

        public InspectorVisitor(PropertyElement bindingElement, T target)
        {
            VisitorContext = new InspectorVisitorContext(bindingElement);
            Target = target;
        }

        public override bool IsExcluded<TProperty, TContainer, TValue>(TProperty property, ref TContainer container)
        {
            return property.Attributes?.HasAttribute<HideInInspector>() ?? false;
        }

        void AddToPath(string str)
        {
            if (m_Path.Length > 0)
            {
                if (!(str.StartsWith("[") && str.EndsWith("]")))
                {
                    m_Path.Append('.');
                }
            }

            m_Path.Append(str);
        }

        void RemoveFromPath(string str)
        {
            var length = m_Path.Length;
            if (length == str.Length)
            {
                m_Path.Clear();
            }
            else if (str.StartsWith("[") && str.EndsWith("]"))
            {
                m_Path.Remove(length - str.Length, str.Length);
            }
            else
            {
                m_Path.Remove(length - str.Length - 1, str.Length + 1);
            }
        }

        protected override VisitStatus BeginContainer<TProperty, TContainer, TValue>(TProperty property,
            ref TContainer container,
            ref TValue value, ref ChangeTracker changeTracker)
        {
            // Root value 
            if (container is PropertyWrapper<TValue> || value is PropertyWrapper<TValue>)
            {
                if (TryGetInspector(ref value, new PropertyPath(string.Empty), string.Empty,
                    InspectorVisitLevel.Target))
                {
                    return VisitStatus.Override;
                }

                PropertyContainer.Visit(ref value, this);
                return VisitStatus.Override;
            }

            // Regular value
            var propName = property.GetName();
            AddToPath(propName);
            try
            {
                if (TryGetInspector(ref value, new PropertyPath(m_Path.ToString()), propName,
                    InspectorVisitLevel.Field))
                {
                    return VisitStatus.Override;
                }

                var foldout = GuiFactory.Foldout(property, ref container, ref value, VisitorContext);
                using (VisitorContext.MakeParentScope(foldout))
                {
                    PropertyContainer.Visit(ref value, this);
                }

                return VisitStatus.Override;
            }
            finally
            {
                RemoveFromPath(propName);
            }
        }

        protected override VisitStatus BeginCollection<TProperty, TContainer, TValue>(TProperty property,
            ref TContainer container,
            ref TValue value, ref ChangeTracker changeTracker)
        {
            var propName = property.GetName();
            AddToPath(propName);
            try
            {
                if (container is PropertyWrapper<TValue> || value is PropertyWrapper<TValue>)
                {
                    GuiFactory.CollectionSizeField(property, ref container, ref value, VisitorContext);
                    for (int i = 0, count = property.GetCount(ref container); i < count; i++)
                    {
                        var callback = new VisitCollectionElementCallback<TContainer>(this);
                        var elementChangeTracker = new ChangeTracker(changeTracker.VersionStorage);

                        property.GetPropertyAtIndex(ref container, i, ref elementChangeTracker, ref callback);

                        if (elementChangeTracker.IsChanged())
                        {
                            changeTracker.IncrementVersion<TProperty, TContainer, TValue>(property, ref container);
                        }
                    }

                    return VisitStatus.Override;
                }

                if (typeof(TValue).IsClass && null == value)
                {
                    var nullElement = new NullElement<TValue>(property.GetName());
                    VisitorContext.Parent.contentContainer.Add(nullElement);

                    return VisitStatus.Override;
                }

                var foldout = GuiFactory.Foldout(property, ref container, ref value, VisitorContext);

                using (VisitorContext.MakeParentScope(foldout))
                {
                    GuiFactory.CollectionSizeField(property, ref container, ref value, VisitorContext);
                    for (int i = 0, count = property.GetCount(ref container); i < count; i++)
                    {
                        var callback = new VisitCollectionElementCallback<TContainer>(this);
                        var elementChangeTracker = new ChangeTracker(changeTracker.VersionStorage);

                        property.GetPropertyAtIndex(ref container, i, ref elementChangeTracker, ref callback);

                        if (elementChangeTracker.IsChanged())
                        {
                            changeTracker.IncrementVersion<TProperty, TContainer, TValue>(property, ref container);
                        }
                    }
                }

                return VisitStatus.Override;
            }
            finally
            {
                RemoveFromPath(propName);
            }
        }

        private bool TryGetInspector<TValue>(ref TValue value, PropertyPath path, string name,
            InspectorVisitLevel visitLevel)
            => GetCustomInspectorCallback.Execute(value, this, path, name, visitLevel)
               || TryGetInspector<TValue>(this, path, name, visitLevel);

        private static bool TryGetInspector<TValue>(InspectorVisitor<T> visitor, PropertyPath propertyPath, string propertyName, InspectorVisitLevel visitLevel)
        {
            var inspector = CustomInspectorDatabase.GetInspector<TValue>();
            if (null == inspector)
            {
                return false;
            }
            var proxy = new InspectorContext<TValue>(visitor.VisitorContext.Binding, propertyPath, propertyName, visitLevel);
            var customInspector = new CustomInspectorElement<TValue>(inspector, proxy);
            visitor.VisitorContext.Parent.contentContainer.Add(customInspector);
            inspector.Update(proxy);
            return true;
        }
    }
}