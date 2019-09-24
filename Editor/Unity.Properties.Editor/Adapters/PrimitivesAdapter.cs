namespace Unity.Properties.Editor
{
    sealed class PrimitivesAdapter<T> : InspectorAdapter<T>
        , IVisitAdapterPrimitives
        , IVisitAdapter<string>
        , IVisitAdapter
    {
        public PrimitivesAdapter(InspectorVisitor<T> visitor) : base(visitor)
        {
        }

        delegate TElement DrawHandler<in TProperty, TContainer, TValue, out TElement>(
            TProperty property,
            ref TContainer container,
            ref TValue value,
            InspectorVisitorContext visitorContext)
            where TProperty : IProperty<TContainer, TValue>;
        
        VisitStatus IVisitAdapter<sbyte>.Visit<TProperty, TContainer>(
            IPropertyVisitor visitor,
            TProperty property,
            ref TContainer container,
            ref sbyte value,
            ref ChangeTracker changeTracker)
            => VisitPrimitive(property, ref container, ref value, GuiFactory.SByteField);

        VisitStatus IVisitAdapter<short>.Visit<TProperty, TContainer>(
            IPropertyVisitor visitor,
            TProperty property,
            ref TContainer container,
            ref short value, 
            ref ChangeTracker changeTracker)
            => VisitPrimitive(property, ref container, ref value, GuiFactory.ShortField);

        VisitStatus IVisitAdapter<int>.Visit<TProperty, TContainer>(
            IPropertyVisitor visitor,
            TProperty property,
            ref TContainer container,
            ref int value, 
            ref ChangeTracker changeTracker)
            => VisitPrimitive(property, ref container, ref value, GuiFactory.IntField);
        
        VisitStatus IVisitAdapter<long>.Visit<TProperty, TContainer>(
            IPropertyVisitor visitor,
            TProperty property,
            ref TContainer container,
            ref long value,
            ref ChangeTracker changeTracker)
            => VisitPrimitive(property, ref container, ref value, GuiFactory.LongField);
        
        VisitStatus IVisitAdapter<byte>.Visit<TProperty, TContainer>(
            IPropertyVisitor visitor,
            TProperty property,
            ref TContainer container,
            ref byte value,
            ref ChangeTracker changeTracker)
            => VisitPrimitive(property, ref container, ref value, GuiFactory.ByteField);
        
        VisitStatus IVisitAdapter<ushort>.Visit<TProperty, TContainer>(
            IPropertyVisitor visitor,
            TProperty property,
            ref TContainer container,
            ref ushort value,
            ref ChangeTracker changeTracker)
            => VisitPrimitive(property, ref container, ref value, GuiFactory.UShortField);
        
        VisitStatus IVisitAdapter<uint>.Visit<TProperty, TContainer>(
            IPropertyVisitor visitor,
            TProperty property,
            ref TContainer container,
            ref uint value,
            ref ChangeTracker changeTracker)
            => VisitPrimitive(property, ref container, ref value, GuiFactory.UIntField);
        
        VisitStatus IVisitAdapter<ulong>.Visit<TProperty, TContainer>(
            IPropertyVisitor visitor,
            TProperty property,
            ref TContainer container,
            ref ulong value,
            ref ChangeTracker changeTracker)
            => VisitPrimitive(property, ref container, ref value, GuiFactory.ULongField);

        VisitStatus IVisitAdapter<float>.Visit<TProperty, TContainer>(
            IPropertyVisitor visitor,
            TProperty property,
            ref TContainer container,
            ref float value, 
            ref ChangeTracker changeTracker)
            => VisitPrimitive(property, ref container, ref value, GuiFactory.FloatField);
        
        VisitStatus IVisitAdapter<double>.Visit<TProperty, TContainer>(
            IPropertyVisitor visitor,
            TProperty property,
            ref TContainer container,
            ref double value,
            ref ChangeTracker changeTracker)
            => VisitPrimitive(property, ref container, ref value, GuiFactory.DoubleField);
        
        VisitStatus IVisitAdapter<bool>.Visit<TProperty, TContainer>(
            IPropertyVisitor visitor,
            TProperty property,
            ref TContainer container,
            ref bool value,
            ref ChangeTracker changeTracker)
            => VisitPrimitive(property, ref container, ref value, GuiFactory.Toggle);
        
        VisitStatus IVisitAdapter<char>.Visit<TProperty, TContainer>(
            IPropertyVisitor visitor,
            TProperty property,
            ref TContainer container,
            ref char value,
            ref ChangeTracker changeTracker)
            => VisitPrimitive(property, ref container, ref value, GuiFactory.CharField);
        
        VisitStatus IVisitAdapter.Visit<TProperty, TContainer, TValue>(
            IPropertyVisitor visitor,
            TProperty property, 
            ref TContainer container,
            ref TValue value,
            ref ChangeTracker changeTracker)
        {
            if (!typeof(TValue).IsEnum)
            {
                return VisitStatus.Unhandled;
            }

            if (RuntimeTypeInfoCache<TValue>.IsFlagsEnum())
            {
                GuiFactory.FlagsField(property, ref container, ref value, VisitorContext);
            }
            else
            {
                GuiFactory.EnumField(property, ref container, ref value, VisitorContext);
            }
            return VisitStatus.Override;
        }

        VisitStatus IVisitAdapter<string>.Visit<TProperty, TContainer>(
            IPropertyVisitor visitor,
            TProperty property,
            ref TContainer container,
            ref string value,
            ref ChangeTracker changeTracker)
            => VisitPrimitive(property, ref container, ref value, GuiFactory.TextField);
        
        VisitStatus VisitPrimitive<TProperty, TContainer, TValue, TElement>(
            TProperty property,
            ref TContainer container,
            ref TValue value,
            DrawHandler<TProperty, TContainer, TValue, TElement> handler
        )
            where TProperty : IProperty<TContainer, TValue>
        {
            handler(property, ref container, ref value, VisitorContext);
            return VisitStatus.Handled;
        }
    }
}
