using System;

namespace Unity.Properties
{
    public static partial class PropertyContainer
    { 
        public static void Visit<TContainer, TVisitor>(TContainer container, TVisitor visitor, IVersionStorage versionStorage = null)
            where TVisitor : IPropertyVisitor
        {
            Visit(ref container, visitor, versionStorage);
        }
        
        public static void Visit<TContainer, TVisitor>(ref TContainer container, TVisitor visitor, IVersionStorage versionStorage = null)
            where TVisitor : IPropertyVisitor
        {
            var changeTracker = new ChangeTracker(versionStorage);
            Visit(ref container, visitor, ref changeTracker);
        }
        
        public static void Visit<TContainer, TVisitor>(ref TContainer container, TVisitor visitor, ref ChangeTracker changeTracker)
            where TVisitor : IPropertyVisitor
        {
            if (!RuntimeTypeInfoCache<TContainer>.IsValueType() && container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }
            
            if (RuntimeTypeInfoCache<TContainer>.IsAbstractOrInterface() || typeof(TContainer) != container.GetType())
            {
                PropertyBagResolver.Resolve(container.GetType())?.Accept(container, visitor, ref changeTracker);
            }
            else
            {
                PropertyBagResolver.Resolve<TContainer>()?.Accept(ref container, visitor, ref changeTracker);
            }
        }
    }
}