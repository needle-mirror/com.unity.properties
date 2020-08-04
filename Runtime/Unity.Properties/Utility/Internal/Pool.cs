using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.Properties.Internal
{
    class Pool<T> where T:class
    {
        internal static string ErrorString =>
            $"Trying to release object of type `{typeof(T).Name}` that is already pooled.";
        
        readonly Stack<T> m_Stack;
        readonly Func<T> m_CreateFunc;
        readonly Action<T> m_OnRelease;

        public Pool(Func<T> createInstanceFunc, Action<T> onRelease)
        {
            m_CreateFunc = createInstanceFunc;
            m_Stack = new Stack<T>();
            m_OnRelease = onRelease;
        }

        public T Get()
        {
            return m_Stack.Count == 0 ? m_CreateFunc() : m_Stack.Pop();
        }

        public void Release(T element)
        {
            if (m_Stack.Count > 0 && Contains(element))
            {
                Debug.LogError(ErrorString);
                return;
            }

            m_OnRelease?.Invoke(element);
            m_Stack.Push(element);
        }

        bool Contains(T element)
        {
            return m_Stack.Any(e => ReferenceEquals(e, element));
        }
    }
}