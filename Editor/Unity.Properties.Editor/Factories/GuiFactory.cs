using System;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;

namespace Unity.Properties.Editor
{
    static class GuiFactory
    {
        public static Foldout Foldout<TProperty, TContainer, TValue>(
            TProperty property,
            ref TContainer container,
            ref TValue value,
            InspectorVisitorContext visitorContext,
            string name = null)
            where TProperty : IProperty<TContainer, TValue>
        {
            var propertyName = property.GetName();
            var foldout = new Foldout
            {
                name = propertyName,
                text = name ?? propertyName,
                bindingPath = propertyName,
            };
            SetTooltip(property, foldout);
            visitorContext.Parent.contentContainer.Add(foldout);
            return foldout;
        }

        public static Toggle Toggle<TProperty, TContainer>(
            TProperty property,
            ref TContainer container,
            ref bool value,
            InspectorVisitorContext visitorContext)
            where TProperty : IProperty<TContainer, bool>
            => Construct<TProperty, Toggle, TContainer, bool>(property, ref container, ref value, visitorContext);

        public static IntegerField SByteField<TProperty, TContainer>(
            TProperty property,
            ref TContainer container,
            ref sbyte value,
            InspectorVisitorContext visitorContext)
            where TProperty : IProperty<TContainer, sbyte>
            => Construct<TProperty, IntegerField, int, TContainer, sbyte>(property, ref container, ref value, visitorContext);

        public static IntegerField ByteField<TProperty, TContainer>(
            TProperty property,
            ref TContainer container,
            ref byte value,
            InspectorVisitorContext visitorContext)
            where TProperty : IProperty<TContainer, byte>
            => Construct<TProperty, IntegerField, int, TContainer, byte>(property, ref container, ref value, visitorContext);

        public static IntegerField UShortField<TProperty, TContainer>(
            TProperty property,
            ref TContainer container,
            ref ushort value,
            InspectorVisitorContext visitorContext)
            where TProperty : IProperty<TContainer, ushort>
            => Construct<TProperty, IntegerField, int, TContainer, ushort>(property, ref container, ref value, visitorContext);

        public static IntegerField ShortField<TProperty, TContainer>(
            TProperty property,
            ref TContainer container,
            ref short value,
            InspectorVisitorContext visitorContext)
            where TProperty : IProperty<TContainer, short>
            => Construct<TProperty, IntegerField, int, TContainer, short>(property, ref container, ref value, visitorContext);

        public static IntegerField IntField<TProperty, TContainer>(
            TProperty property,
            ref TContainer container,
            ref int value,
            InspectorVisitorContext visitorContext)
            where TProperty : IProperty<TContainer, int>
            => Construct<TProperty, IntegerField, TContainer, int>(property, ref container, ref value, visitorContext);

        public static IntegerField CollectionSizeField<TProperty, TContainer, TValue>(
            TProperty property,
            ref TContainer container,
            ref TValue value,
            InspectorVisitorContext visitorContext)
            where TProperty : ICollectionProperty<TContainer, TValue>
        {
            var hasTooltip = property.Attributes?.HasAttribute<TooltipAttribute>() ?? false;
            var field = new IntegerField
            {
                name = "CollectionSize",
                label = "Size",
                bindingPath = string.Empty,
                isDelayed = true
            };
            SetTooltip(property, field);

            field.SetValueWithoutNotify(property.GetCount(ref container));

            SetTooltip(property, field);
            var parent = visitorContext.Parent; 
            visitorContext.Parent.contentContainer.Add(field);
            field.RegisterValueChangedCallback(evt =>
            {
                visitorContext.Binding.SetCount(parent, Mathf.Clamp(evt.newValue, 0, int.MaxValue));
            });
            return field;
        }
        
        public static LongField UIntField<TProperty, TContainer>(
            TProperty property,
            ref TContainer container,
            ref uint value,
            InspectorVisitorContext visitorContext)
            where TProperty : IProperty<TContainer, uint>
            => Construct<TProperty, LongField, long, TContainer, uint>(property, ref container, ref value, visitorContext);

        public static LongField LongField<TProperty, TContainer>(
            TProperty property,
            ref TContainer container,
            ref long value,
            InspectorVisitorContext visitorContext)
            where TProperty : IProperty<TContainer, long>
            => Construct<TProperty, LongField, TContainer, long>(property, ref container, ref value, visitorContext);

        public static TextField ULongField<TProperty, TContainer>(
            TProperty property,
            ref TContainer container,
            ref ulong value,
            InspectorVisitorContext visitorContext)
            where TProperty : IProperty<TContainer, ulong>
            => Construct<TProperty, TextField, string, TContainer, ulong>(property, ref container, ref value, visitorContext);

        public static FloatField FloatField<TProperty, TContainer>(
            TProperty property,
            ref TContainer container,
            ref float value,
            InspectorVisitorContext visitorContext)
            where TProperty : IProperty<TContainer, float>
            => Construct<TProperty, FloatField, TContainer, float>(property, ref container,
                ref value, visitorContext);

        public static DoubleField DoubleField<TProperty, TContainer>(
            TProperty property,
            ref TContainer container,
            ref double value,
            InspectorVisitorContext visitorContext)
            where TProperty : IProperty<TContainer, double>
            => Construct<TProperty, DoubleField, TContainer, double>(property, ref container, ref value, visitorContext);

        public static TextField CharField<TProperty, TContainer>(
            TProperty property,
            ref TContainer container,
            ref char value,
            InspectorVisitorContext visitorContext)
            where TProperty : IProperty<TContainer, char>
            => Construct<TProperty, TextField, string, TContainer, char>(property, ref container, ref value, visitorContext);
        
        public static TextField TextField<TProperty, TContainer>(
            TProperty property,
            ref TContainer container,
            ref string value,
            InspectorVisitorContext visitorContext)
            where TProperty : IProperty<TContainer, string>
            => Construct<TProperty, TextField, string, TContainer, string>(property, ref container, ref value, visitorContext);

        public static EnumFlagsField FlagsField<TProperty, TContainer, TValue>(
            TProperty property,
            ref TContainer container,
            ref TValue value,
            InspectorVisitorContext visitorContext)
            where TProperty : IProperty<TContainer, TValue>
        {
            if (!typeof(TValue).IsEnum)
            {
                throw new ArgumentException();
            }
            var name = property.GetName();
            var element = new EnumFlagsField(value as Enum) {bindingPath = name};
            SetNames(property, element);
            SetTooltip(property, element);
            visitorContext.Parent.contentContainer.Add(element);
            return element;
        }
        
        public static EnumField EnumField<TProperty, TContainer, TValue>(
            TProperty property,
            ref TContainer container,
            ref TValue value,
            InspectorVisitorContext visitorContext) 
            where TProperty : IProperty<TContainer, TValue>
        {
            if (!typeof(TValue).IsEnum)
            {
                throw new ArgumentException();
            }
            var name = property.GetName();
            var element = new EnumField(value as Enum) {bindingPath = name};
            SetNames(property, element);
            SetTooltip(property, element);
            visitorContext.Parent.contentContainer.Add(element);
            return element;
        }
        
        private static TElement ConstructBase<TProperty, TContainer, TElement, TFieldValue, TValue>(
            TProperty property)
            where TProperty : IProperty<TContainer, TValue>
            where TElement : BaseField<TFieldValue>, new()
        {
            var element = new TElement();
            SetNames(property, element);
            SetTooltip(property, element);

            if (property.IsReadOnly)
            {
                element.SetEnabled(false);
            }
            return element;
        }

        public static void SetNames<TProperty, TValue>(TProperty property, BaseField<TValue> element)
            where TProperty : IProperty
        {
            var name = property.GetName();
            element.name = name;
            element.label = ObjectNames.NicifyVariableName(name);
            element.bindingPath = name;
            element.AddToClassList(name);
        }

        public static void SetTooltip<TProperty>(TProperty property, VisualElement element)
            where TProperty : IProperty
        {
            if(property.Attributes?.HasAttribute<TooltipAttribute>() ?? false)
            {
                element.tooltip = property.Attributes.GetAttribute<TooltipAttribute>().tooltip;
            }
        }

        public static TElement Construct<TProperty, TElement, TContainer, TValue>(
            TProperty property,
            ref TContainer container,
            ref TValue value,
            InspectorVisitorContext visitorContext
        )
            where TProperty : IProperty<TContainer, TValue>
            where TElement : BaseField<TValue>, new()
        {
            return Construct<TProperty, TElement, TValue, TContainer, TValue>(property, ref container, ref value, visitorContext);
        }
        
        public static ObjectField Construct<TProperty, TContainer, TValue>(
            TProperty property,
            ref TContainer container,
            UnityEngine.Object value,
            InspectorVisitorContext visitorContext
        )
            where TProperty : IProperty<TContainer, TValue>
        {
            var element = ConstructBase<TProperty, TContainer, ObjectField, UnityEngine.Object, TValue>(property);
            visitorContext.Parent.contentContainer.Add(element);
            return element;
        }

        public static TElement Construct<TProperty, TElement, TFieldType, TContainer, TValue>(
            TProperty property,
            ref TContainer container,
            ref TValue value,
            InspectorVisitorContext visitorContext
        )
            where TProperty : IProperty<TContainer, TValue>
            where TElement : BaseField<TFieldType>, new()
        {
            var element = ConstructBase<TProperty, TContainer, TElement, TFieldType, TValue>(property);
            visitorContext.Parent.contentContainer.Add(element);
            return element;
        }
    }
}
