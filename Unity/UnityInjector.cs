/*
Copyright 2017 mtti

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

   http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace mtti.Inject
{
    public enum GetComponentAttributeType
    {
        GetComponent,
        GetComponentInChildren,
        EnsureComponent
    }

    public class FieldInfoAndAttributeType
    {
        public FieldInfo Field;

        public GetComponentAttributeType Type;

        public FieldInfoAndAttributeType(FieldInfo field, GetComponentAttributeType type)
        {
            Field = field;
            Type = type;
        }
    }

    /// <summary>
    /// Unity-specific <see cref="mtti.Inject.Injector"/> with additional methods for injecting
    /// depdendencies into Unity scenes and GameObjects.
    /// </summary>
    public class UnityInjector : Injector
    {
        private static void CheckForComponentAttribute(
            Type type,
            GetComponentAttributeType typeEnum,
            FieldInfo field,
            List<FieldInfoAndAttributeType> cachedFields
        )
        {
            object attribute = Attribute.GetCustomAttribute(field, type, true);
            if (attribute != null)
            {
                cachedFields.Add(new FieldInfoAndAttributeType(field, typeEnum));
            }
        }

        /// <summary>
        /// Temporarily holds the MonoBehaviours of a GameObject while dependencies are injected
        /// into them.
        /// </summary>
        protected List<MonoBehaviour> _componentBuffer = new List<MonoBehaviour>();

        private Dictionary<Type, List<FieldInfoAndAttributeType>> _componentReceiverCache
            = new Dictionary<Type, List<FieldInfoAndAttributeType>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="mtti.Inject.UnityInjector"/> class.
        /// </summary>
        public UnityInjector() : base()
        {
            Initialize();
        }

        protected UnityInjector(Type attributeType, Type optionalAttributeType)
            : base(attributeType, optionalAttributeType)
        {
            Initialize();
        }

        /// <summary>
        /// Inject dependencies into all GameObjects in a scene.
        /// </summary>
        /// <param name="scene">A Unity scene.</param>
        public void Inject(Scene scene)
        {
            if (!scene.isLoaded)
            {
                return;
            }
            var rootGameObjects = scene.GetRootGameObjects();
            for (int i = 0; i < rootGameObjects.Length; i++)
            {
                Inject(rootGameObjects[i]);
            }
        }

        /// <summary>
        /// Inject dependencies into a Unity GameObject and all its children.
        /// </summary>
        /// <param name="obj">Target GameObject.</param>
        public void Inject(GameObject obj)
        {
            obj.GetComponents<MonoBehaviour>(_componentBuffer);
            for (int i = _componentBuffer.Count - 1; i >= 0; i--)
            {
#if UNITY_EDITOR
                if (UnityEditor.EditorApplication.isPlaying)
                {
                    GetUnityComponents(_componentBuffer[i]);
                }
#else
                GetUnityComponents(_componentBuffer[i]);
#endif
                Inject(_componentBuffer[i]);
            }
            _componentBuffer.Clear();

            for (int i = 0, count = obj.transform.childCount; i < count; i++)
            {
                Inject(obj.transform.GetChild(i).gameObject);
            }
        }

        private void Initialize()
        {
            Bind<UnityInjector>(this);
        }

        private void GetUnityComponents(MonoBehaviour target)
        {
            List<FieldInfoAndAttributeType> cachedFields;
            Type type = target.GetType();

            if (_componentReceiverCache.ContainsKey(type))
            {
                cachedFields = _componentReceiverCache[type];
            }
            else
            {
                cachedFields = new List<FieldInfoAndAttributeType>();
                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public
                        | BindingFlags.NonPublic);
                for (int i = 0; i < fields.Length; i++)
                {
                    CheckForComponentAttribute(
                        typeof(GetComponentAttribute),
                        GetComponentAttributeType.GetComponent,
                        fields[i],
                        cachedFields
                    );
                    CheckForComponentAttribute(
                        typeof(GetComponentInChildrenAttribute),
                        GetComponentAttributeType.GetComponentInChildren,
                        fields[i],
                        cachedFields
                    );
                    CheckForComponentAttribute(
                        typeof(EnsureComponentAttribute),
                        GetComponentAttributeType.EnsureComponent,
                        fields[i],
                        cachedFields
                    );
                }
                _componentReceiverCache[type] = cachedFields;
            }

            for (int i = 0, count = cachedFields.Count; i < count; i++)
            {
                FieldInfoAndAttributeType receiver = cachedFields[i];
                switch (receiver.Type)
                {
                    case GetComponentAttributeType.GetComponent:
                        receiver.Field.SetValue(
                            target,
                            target.gameObject.GetComponent(receiver.Field.FieldType)
                        );
                        break;
                    case GetComponentAttributeType.GetComponentInChildren:
                        receiver.Field.SetValue(
                            target,
                            target.gameObject.GetComponentInChildren(receiver.Field.FieldType, true)
                        );
                        break;
                    case GetComponentAttributeType.EnsureComponent:
                        object component = target.gameObject.GetComponent(receiver.Field.FieldType);

                        // As of Unity 2017.2.1f1, GetComponent doesn't actually return null, it
                        // returns a "Unity fake null" instance of the requested type which has
                        // overridden the Equals method. It seems that using the == operator on
                        // a System.Object doesn't use the overridden method, however, so we do
                        // these two checks here to be sure.
                        if (component == null || component.Equals(null))
                        {
                            component = target.gameObject.AddComponent(receiver.Field.FieldType);
                        }

                        receiver.Field.SetValue(
                            target,
                            component
                        );
                        break;
                }
            }
        }
    }
}
