using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;

namespace Unity.Properties.Reflection
{
    public class ReflectedFieldPropertyGenerator : IReflectedPropertyGenerator
    {
        private interface IMemberInfo
        {
            string Name { get; }
            Type PropertyType { get; }
            object GetValue(object obj);
            void SetValue(object obj, object value);
            IEnumerable<Attribute> GetCustomAttributes();
            bool CanWrite();
        }

        private struct FieldMember : IMemberInfo
        {
            private readonly FieldInfo m_FieldInfo;

            public FieldMember(FieldInfo fieldInfo) => m_FieldInfo = fieldInfo;
            public string Name => m_FieldInfo.Name;
            public Type PropertyType => m_FieldInfo.FieldType;
            public object GetValue(object obj) => m_FieldInfo.GetValue(obj);
            public void SetValue(object obj, object value) => m_FieldInfo.SetValue(obj, value);
            public IEnumerable<Attribute> GetCustomAttributes() => m_FieldInfo.GetCustomAttributes();
            public bool CanWrite() => true;
        }
        
        private struct PropertyMember : IMemberInfo
        {
            private readonly PropertyInfo m_PropertyInfo;

            public PropertyMember(PropertyInfo propertyInfo) => m_PropertyInfo = propertyInfo;
            public string Name => m_PropertyInfo.Name;
            public Type PropertyType => m_PropertyInfo.PropertyType;
            public object GetValue(object obj) => m_PropertyInfo.GetValue(obj);
            public void SetValue(object obj, object value) => m_PropertyInfo.SetValue(obj, value);
            public IEnumerable<Attribute> GetCustomAttributes() => m_PropertyInfo.GetCustomAttributes();
            public bool CanWrite() => m_PropertyInfo.CanWrite;
        }

        private struct ReflectedFieldProperty<TContainer, TValue> : IProperty<TContainer, TValue>
        {
            private readonly IMemberInfo m_Info;
            private readonly IPropertyAttributeCollection m_Attributes;
            private readonly bool m_ReadOnly;

            public string GetName() => m_Info.Name;
            public bool IsReadOnly => m_ReadOnly;
            public bool IsContainer => !(m_Info.PropertyType.IsPrimitive || m_Info.PropertyType.IsEnum || m_Info.PropertyType == typeof(string));
            public IPropertyAttributeCollection Attributes => m_Attributes;

            public ReflectedFieldProperty(IMemberInfo info)
            {
                m_Info = info;
                m_Attributes = new PropertyAttributeCollection(info.GetCustomAttributes().ToArray());
                m_ReadOnly = m_Attributes.HasAttribute<ReadOnlyAttribute>() || !info.CanWrite();
            }

            public TValue GetValue(ref TContainer container)
            {
                return (TValue) m_Info.GetValue(container);
            }

            public void SetValue(ref TContainer container, TValue value)
            {
                var boxed = (object) container;
                m_Info.SetValue(boxed, value);
                container = (TContainer) boxed;
            }
        }

        private struct ReflectedListProperty<TContainer, TValue, TElement> : ICollectionProperty<TContainer, TValue>
            where TValue : IList<TElement>
        {
            private struct CollectionElementProperty : ICollectionElementProperty<TContainer, TElement>
            {
                private readonly ReflectedListProperty<TContainer, TValue, TElement> m_Property;
                private readonly IPropertyAttributeCollection m_Attributes;
                private readonly int m_Index;

                public string GetName() => "[" + Index + "]";
                public bool IsReadOnly => false;
                public bool IsContainer => RuntimeTypeInfoCache<TElement>.IsContainerType();
                public IPropertyAttributeCollection Attributes => m_Attributes;
                public int Index => m_Index;

                public CollectionElementProperty(ReflectedListProperty<TContainer, TValue, TElement> property, int index, IPropertyAttributeCollection attributes = null)
                {
                    m_Property = property;
                    m_Attributes = attributes;
                    m_Index = index;
                }

                public TElement GetValue(ref TContainer container)
                {
                    return m_Property.GetValue(ref container)[Index];
                }

                public void SetValue(ref TContainer container, TElement value)
                {
                    m_Property.GetValue(ref container)[Index] = value;
                }
            }

            private readonly IMemberInfo m_Info;
            private readonly IPropertyAttributeCollection m_Attributes;
            private readonly bool m_ReadOnly;

            public string GetName() => m_Info.Name;
            public bool IsReadOnly => false;
            public bool IsContainer => !(m_Info.PropertyType.IsPrimitive || m_Info.PropertyType.IsEnum || m_Info.PropertyType == typeof(string));
            public IPropertyAttributeCollection Attributes => m_Attributes;

            public ReflectedListProperty(IMemberInfo info)
            {
                m_Info = info;
                m_Attributes = new PropertyAttributeCollection(info.GetCustomAttributes().ToArray());
                m_ReadOnly = m_Attributes.HasAttribute<ReadOnlyAttribute>() || !info.CanWrite();
            }

            public TValue GetValue(ref TContainer container)
            {
                return (TValue) m_Info.GetValue(container);
            }

            public void SetValue(ref TContainer container, TValue value)
            {
                m_Info.SetValue(container, value);
            }

            public int GetCount(ref TContainer container)
            {
                return GetValue(ref container).Count;
            }

            public void SetCount(ref TContainer container, int count)
            {
                var list = GetValue(ref container);

                if (list.Count == count)
                {
                    return;
                }

                if (list.Count < count)
                {
                    for (var i = list.Count; i < count; i++)
                        list.Add(default(TElement));
                }
                else
                {
                    for (var i = list.Count - 1; i >= count; i--)
                        list.RemoveAt(i);
                }
            }

            public void Clear(ref TContainer container)
            {
                GetValue(ref container).Clear();
            }

            public void GetPropertyAtIndex<TGetter>(ref TContainer container, int index, ref ChangeTracker changeTracker, TGetter getter) where TGetter : ICollectionElementPropertyGetter<TContainer>
            {
                getter.VisitProperty<CollectionElementProperty, TElement>(new CollectionElementProperty(this, index), ref container, ref changeTracker);
            }
        }

        public bool Generate<TContainer, TValue>(FieldInfo fieldInfo, ReflectedPropertyBag<TContainer> propertyBag)
        {
            return Generate<TContainer, TValue>(new FieldMember(fieldInfo), propertyBag);
        }

        public bool Generate<TContainer, TValue>(PropertyInfo propertyInfo, ReflectedPropertyBag<TContainer> propertyBag)
        {
            return Generate<TContainer, TValue>(new PropertyMember(propertyInfo), propertyBag);
        }
        
        private bool Generate<TContainer, TValue>(IMemberInfo memberInfo, ReflectedPropertyBag<TContainer> propertyBag)
        {
            if (typeof(TValue).IsGenericType && typeof(IList<>).IsAssignableFrom(typeof(TValue).GetGenericTypeDefinition()))
            {
                var elementType = typeof(TValue).GetGenericArguments()[0];
                var method = typeof(ReflectedFieldPropertyGenerator).GetMethod(nameof(GenerateListProperty), BindingFlags.Instance | BindingFlags.NonPublic);
                var genericMethod = method.MakeGenericMethod(typeof(TContainer), memberInfo.PropertyType, elementType);
                genericMethod.Invoke(this, new object[] {memberInfo, propertyBag});
            }
            else
            {
                propertyBag.AddProperty<ReflectedFieldProperty<TContainer, TValue>, TValue>(
                    new ReflectedFieldProperty<TContainer, TValue>(memberInfo));
            }

            return true;
        }

        private void GenerateListProperty<TContainer, TValue, TElement>(IMemberInfo member, ReflectedPropertyBag<TContainer> propertyBag)
            where TValue : IList<TElement>
        {
            propertyBag.AddCollectionProperty<ReflectedListProperty<TContainer, TValue, TElement>, TValue>(
                new ReflectedListProperty<TContainer, TValue, TElement>(member));
        }
    }
}
