using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.Properties.Editor
{
    static class CustomInspectorDatabase
    {
        public class UxmlPostProcessor : AssetPostprocessor
        {
            private const string k_UxmlExtension = ".uxml";
            private static void OnPostprocessAllAssets(
                string[] importedAssets,
                string[] deletedAssets,
                string[] movedAssets,
                string[] movedFromAssetPaths)
            {
                if (Process(importedAssets)) return;
                if (Process(deletedAssets)) return;
                if (Process(movedAssets)) return;
            }

            private static bool Process(string[] paths)
            {
                if (!paths.Any(path => path.EndsWith(k_UxmlExtension, StringComparison.InvariantCultureIgnoreCase)))
                    return false;
                // TODO: Signal property elements to potentially refresh themselves. 
                return true;
            }
        }

        static readonly Dictionary<Type, List<Type>> s_InspectorsPerType;

        static CustomInspectorDatabase()
        {
            s_InspectorsPerType = new Dictionary<Type, List<Type>>();
            RegisterCustomInspectors();
        }

        public static IInspector<TValue> GetInspector<TValue>()
        {
            return GetInspectorInstance<TValue>(s_InspectorsPerType);
        }
       
        private static IInspector<TValue> GetInspectorInstance<TValue>(Dictionary<Type, List<Type>> typeMap)
        {
            var type = typeof(TValue);
            if (typeMap.TryGetValue(type, out var inspector))
            {
                // TODO: Multiple inspectors can be created for any specific type. We need a way to resolve which one is going
                // to be used. Right now, it is dependent on compilation order.
                // We also need to support the equivalent of "inspector of child classes" as well.
                return (IInspector<TValue>) Activator.CreateInstance(inspector[0]);
            }
            return null;
        }

        private static void RegisterCustomInspectors()
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(IInspector<>)))
            {
                RegisterInspectorType(s_InspectorsPerType, typeof(IInspector<>), type);
            }
        }

        private static void RegisterInspectorType(Dictionary<Type, List<Type>> typeMap, Type interfaceType, Type inspectorType)
        {
            var inspectorInterface = inspectorType.GetInterface(interfaceType.FullName);
            if (null == inspectorInterface || inspectorType.IsAbstract || inspectorType.ContainsGenericParameters)
            {
                return;
            }

            var genericArguments = inspectorInterface.GetGenericArguments();
            var componentType = genericArguments[0];

            if (!TypeConstruction.HasParameterLessConstructor(inspectorType))
            {
                Debug.LogError($"Could not create a custom inspector for type `{inspectorType.Name}`: no default or empty constructor found.");
            }
            
            if (!typeMap.TryGetValue(componentType, out var list))
            {
                typeMap[componentType] = list = new List<Type>();
            }

            list.Add(inspectorType);
        }
    }
}
