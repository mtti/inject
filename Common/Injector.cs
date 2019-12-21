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

namespace mtti.Inject
{
    /// <summary>
    /// A minimalistic dependency injection container.
    /// </summary>
    public class Injector
    {
        /// <summary>
        /// Finds all types in the current application domain which have a specific attribute.
        /// </summary>
        public static void FindAllTypesWithAttribute(Type attributeType, List<Type> result)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                try
                {
                    var types = assemblies[i].GetTypes();
                    for (int j = 0; j < types.Length; j++)
                    {
                        var attribute = Attribute.GetCustomAttribute(types[j], attributeType, false);
                        if (attribute != null)
                        {
                            result.Add(types[j]);
                        }
                    }
                }
                catch (ReflectionTypeLoadException e)
                {
#if UNITY
                    UnityEngine.Debug.LogErrorFormat(
                        "ReflectionTypeLoadException with assembly {0}", assemblies[i].FullName);
#endif
                    throw e;
                }
            }
        }

        /// <summary>
        /// Get all methods in a type which have the specified attribute.
        /// </summary>
        /// <param name="targetType">Target type.</param>
        /// <param name="attributeType">Attribute type.</param>
        /// <param name="result">Result.</param>
        public static void GetMethodsWithAttribute(Type targetType, Type attributeType,
            List<MethodInfo> result)
        {
            var methods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public
                | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                var attribute = Attribute.GetCustomAttribute(methods[i], attributeType, true);
                if (attribute != null)
                {
                    result.Add(methods[i]);
                }
            }
        }

        /// <summary>
        /// Find all static methods in the current application domain with an attribute.
        /// </summary>
        /// <param name="attributeType">Attribute type.</param>
        /// <param name="result">Result.</param>
        public static void GetStaticMethodsWithAttribute(Type attributeType,
            List<MethodInfo> result)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                var types = assemblies[i].GetTypes();
                for (int j = 0; j < types.Length; j++)
                {
                    GetStaticMethodsWithAttribute(types[j], attributeType, result);
                }
            }
        }

        /// <summary>
        /// Get all methods in a type which have the specified attribute.
        /// </summary>
        /// <param name="targetType">Target type.</param>
        /// <param name="attributeType">Attribute type.</param>
        /// <param name="result">Result.</param>
        public static void GetStaticMethodsWithAttribute(Type targetType, Type attributeType,
            List<MethodInfo> result)
        {
            var methods = targetType.GetMethods(BindingFlags.Static | BindingFlags.Public
                | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                var attribute = Attribute.GetCustomAttribute(methods[i], attributeType, true);
                if (attribute != null)
                {
                    result.Add(methods[i]);
                }
            }
        }

        /// <summary>
        /// Listeners for the OnUpdate event.
        /// </summary>
        private List<IUpdateReceiver> _updateReceivers = new List<IUpdateReceiver>();

        /// <summary>
        /// The type of the attribute used to find inject targets. This is changeable to allow
        /// <see cref="mtti.Inject.UnityEditorInjector"/> to inject dependencies inside the Unity
        /// editor using its own <see cref="mtti.Inject.InjectInEditorAttribute"/> rather than the
        /// default <see cref="mtti.Inject.InjectAttribute"/>.
        /// </summary>
        private Type _attributeType = typeof(InjectAttribute);

        /// <summary>
        /// The type of the attribute used to find optional inject targets.
        /// </summary>
        private Type _optionalAttributeType = typeof(InjectOptionalAttribute);

        /// <summary>
        /// Stores the injectable dependencies.
        /// </summary>
        private Dictionary<Type, object> _dependencies = new Dictionary<Type, object>();

        /// <summary>
        /// Factories for lazy dependencies keyed by dependency type.
        /// </summary>
        private Dictionary<Type, IDependencyFactory> _lazyFactories
            = new Dictionary<Type, IDependencyFactory>();

        /// <summary>
        /// Indexes dependent types by dependency type.
        /// </summary>
        private Dictionary<Type, HashSet<Type>> _relationships
            = new Dictionary<Type, HashSet<Type>>();

        /// <summary>
        /// Caches injectable fields per type so that they don't need to be looked up every time an
        /// object of that type is injected.
        /// </summary>
        private Dictionary<Type, List<FieldInfo>> _fieldCache
            = new Dictionary<Type, List<FieldInfo>>();

        /// <summary>
        /// Caches optionally injectable fields.
        /// </summary>
        private Dictionary<Type, List<FieldInfo>> _optionalFieldCache
            = new Dictionary<Type, List<FieldInfo>>();

        /// <summary>
        /// Caches injectable methods per type.
        /// </summary>
        private Dictionary<Type, List<MethodInfo>> _methodCache
            = new Dictionary<Type, List<MethodInfo>>();

        /// <summary>
        /// Caches arguments of injectable methods per type. List indexes match those of
        /// methodCache.
        /// </summary>
        private Dictionary<Type, List<object[]>> _argumentCache
            = new Dictionary<Type, List<object[]>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="mtti.Inject.Injector"/> class.
        /// </summary>
        public Injector()
        {
            Initialize(typeof(InjectAttribute), typeof(InjectOptionalAttribute));
        }

        protected Injector(Type attributeType, Type optionalAttributeType)
        {
            Initialize(attributeType, optionalAttributeType);
        }

        /// <summary>
        /// Add a new injectable dependency to this injector. The generic version.
        /// </summary>
        /// <param name="obj">The concrete implementation of T</param>
        /// <typeparam name="T">The type of the dependency, typically an interface.</typeparam>
        public Injector Bind<T>(T obj) where T : class
        {
            var type = typeof(T);
            return Bind(type, obj);
        }

        /// <summary>
        /// Add a new injectable dependency to this injector. The non-generic version.
        /// </summary>
        /// <param name="type">The type of the dependency, typically an interface.</param>
        /// <param name="obj">An instance of the injectable.</param>
        public Injector Bind(Type type, object obj)
        {
            if (_dependencies.ContainsKey(type))
            {
                throw new DependencyInjectionException("Already bound: " + type.ToString());
            }

            if (_relationships.ContainsKey(type))
            {
                foreach (var targetType in _relationships[type])
                {
                    ClearCaches(targetType);
                }
            }

            _dependencies[type] = obj;
            Inject(obj);

            BindUpdateable(obj);

            return this;
        }

        /// <summary>
        /// Retrieve a dependency by its contract type. The generic version.
        /// </summary>
        /// <typeparam name="T">The dependency's contract type, typically an interface.</typeparam>
        public T Get<T>() where T : class
        {
            return Get(typeof(T)) as T;
        }

        /// <summary>
        /// Retrieve a dependency by its contract type. If the dependency is unmet, <c>null</c>
        /// is returned instead of throwing an exception. The generic version.
        /// </summary>
        /// <typeparam name="T">The dependency's contract type, typically an interface.</typeparam>
        public T GetOptional<T>() where T : class
        {
            return GetOptional(typeof(T)) as T;
        }

        /// <summary>
        /// Retrieve a dependency by its contract type. The non-generic version.
        /// </summary>
        /// <param name="type">The dependency's contract type, typically an interface.</param>
        public object Get(Type type)
        {
            if (type == typeof(bool))
            {
                return true;
            }

            if (_dependencies.ContainsKey(type))
            {
                return _dependencies[type];
            }
            else if (_lazyFactories.ContainsKey(type))
            {
                return InitializeLazy(type);
            }
            else if (!type.IsInterface)
            {
                throw new DependencyInjectionException(string.Format(
                        "Unmet dependency: {0}. Did you mean to specify an interface instead?",
                        type.ToString()));
            }
            else
            {
                throw new DependencyInjectionException(string.Format(
                        "Unmet dependency: {0}.", type.ToString()));
            }
        }

        /// <summary>
        /// Retrieve a dependency by its contract type. If the dependency is unmet, <c>null</c>
        /// is returned instead of throwing an exception. The non-generic version.
        /// </summary>
        /// <param name="type">The dependency's contract type, typically an interface.</param>
        public object GetOptional(Type type)
        {
            if (_dependencies.ContainsKey(type))
            {
                return _dependencies[type];
            }
            else if (_lazyFactories.ContainsKey(type))
            {
                return InitializeLazy(type);
            }
            return null;
        }

        /// <summary>
        /// Inject dependencies into an object. Fields with
        /// <see cref="mtti.Inject.InjectAttribute"/> will be set to values from this injector.
        /// Methods with <see cref="mtti.Inject.InjectAttribute"/> will be executed with arguments
        /// set to values from this injector. Methods must have at least one parameter to be called.
        /// </summary>
        /// <param name="target">The target object.</param>
        public void Inject(object target)
        {
            var targetType = target.GetType();
            InjectFields(target, targetType);
            InjectMethods(target, targetType);
        }

        /// <summary>
        /// Call OnUpdate on all bound services that implement IUpdateReceiver.
        /// </summary>
        public void OnUpdate()
        {
            for (int i = 0, count = _updateReceivers.Count; i < count; i++)
            {
                _updateReceivers[i].OnUpdate();
            }
        }

        /// <summary>
        /// Finds injectables marked with <see cref="mtti.Inject.ServiceAttribute"/> and binds them
        /// as lazy dependencies.
        /// </summary>
        protected internal void BindLazyFromCurrentAppDomain()
        {
            var types = new List<Type>();
            FindAllTypesWithAttribute(typeof(ServiceAttribute), types);
            for (int i = 0, count = types.Count; i < count; i++)
            {
                var attribute = (ServiceAttribute)Attribute.GetCustomAttribute(types[i],
                                    typeof(ServiceAttribute), false);
                var keyType = attribute.KeyType ?? types[i];
                BindLazy(keyType, types[i]);
            }

            var methods = new List<MethodInfo>();
            GetStaticMethodsWithAttribute(typeof(ServiceAttribute), methods);
            for (int i = 0, count = methods.Count; i < count; i++)
            {
                BindLazy(methods[i]);
            }
        }

        private void Initialize(Type attributeType, Type optionalAttributeType)
        {
            _attributeType = attributeType;
            _optionalAttributeType = optionalAttributeType;
            _dependencies[typeof(Injector)] = this;
        }

        /// <summary>
        /// Add object to the internal list of OnUpdate listeners if it implements IUpdateReceiver.
        /// </summary>
        /// <param name="target">Target.</param>
        private void BindUpdateable(object target)
        {
            var updateable = target as IUpdateReceiver;
            if (updateable != null && !_updateReceivers.Contains(updateable))
            {
                _updateReceivers.Add(updateable);
            }
        }

        /// <summary>
        /// Inject dependencies into an object's fields that have the inject attribute.
        /// </summary>
        /// <param name="target">Target object.</param>
        /// <param name="type">Type object's type.</param>
        private void InjectFields(object target, Type type)
        {
            List<FieldInfo> cachedFields = null;
            List<FieldInfo> optionalCachedFields = null;

            if (_fieldCache.ContainsKey(type))
            {
                cachedFields = _fieldCache[type];
                optionalCachedFields = _optionalFieldCache[type];
            }
            else
            {
                cachedFields = new List<FieldInfo>();
                optionalCachedFields = new List<FieldInfo>();

                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public
                    | BindingFlags.NonPublic);
                for (int i = 0; i < fields.Length; i++)
                {
                    var attribute
                        = Attribute.GetCustomAttribute(fields[i], _attributeType, true);
                    if (attribute != null)
                    {
                        cachedFields.Add(fields[i]);
                    }

                    var optionalAttribute
                        = Attribute.GetCustomAttribute(fields[i], _optionalAttributeType, true);
                    if (optionalAttribute != null)
                    {
                        optionalCachedFields.Add(fields[i]);
                    }

                    if (attribute != null || optionalAttribute != null)
                    {
                        AddRelationship(fields[i].FieldType, type);
                    }
                }
                _fieldCache[type] = cachedFields;
                _optionalFieldCache[type] = optionalCachedFields;
            }

            for (int i = 0, count = cachedFields.Count; i < count; i++)
            {
                FieldInfo field = cachedFields[i];
                field.SetValue(target, Get(field.FieldType));
            }

            for (int i = 0, count = optionalCachedFields.Count; i < count; i++)
            {
                FieldInfo field = optionalCachedFields[i];
                object dependency = GetOptional(field.FieldType);
                if (dependency != null)
                {
                    field.SetValue(target, dependency);
                }
            }
        }

        /// <summary>
        /// Inject dependencies into an object by its methods that have the inject attribute.
        /// </summary>
        /// <param name="target">Target object.</param>
        /// <param name="type">The target object's type.</param>
        private void InjectMethods(object target, Type type)
        {
            List<MethodInfo> cachedMethods = null;
            List<object[]> cachedArguments = null;

            if (_methodCache.ContainsKey(type))
            {
                cachedMethods = _methodCache[type];
                cachedArguments = _argumentCache[type];
            }
            else
            {
                cachedMethods = new List<MethodInfo>();
                cachedArguments = new List<object[]>();

                var methods = type.GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0; i < methods.Length; i++)
                {
                    var attribute
                        = Attribute.GetCustomAttribute(methods[i], _attributeType, true);
                    if (attribute == null)
                    {
                        continue;
                    }

                    var parameters = methods[i].GetParameters();
                    object[] arguments = new object[parameters.Length];
                    for (int j = 0; j < parameters.Length; j++)
                    {
                        AddRelationship(parameters[j].ParameterType, type);
                        arguments[j] = Get(parameters[j].ParameterType);
                    }

                    cachedMethods.Add(methods[i]);
                    cachedArguments.Add(arguments);
                }
                _methodCache[type] = cachedMethods;
                _argumentCache[type] = cachedArguments;
            }

            for (int i = 0, count = cachedMethods.Count; i < count; i++)
            {
                cachedMethods[i].Invoke(target, cachedArguments[i]);
            }
        }

        /// <summary>
        /// Remember that targetType depends on dependencyType.
        /// </summary>
        /// <param name="dependencyType">Dependency type.</param>
        /// <param name="targetType">Target type.</param>
        private void AddRelationship(Type dependencyType, Type targetType)
        {
            if (!_relationships.ContainsKey(dependencyType))
            {
                _relationships[dependencyType] = new HashSet<Type>();
            }
            _relationships[dependencyType].Add(targetType);
        }

        private void ClearCaches(Type targetType)
        {
            _fieldCache.Remove(targetType);
            _optionalFieldCache.Remove(targetType);
            _methodCache.Remove(targetType);
            _argumentCache.Remove(targetType);
        }

        /// <summary>
        /// Add a lazy dependency into the injector. Lazy dependencies are initialized when they're
        /// first required during dependency injection.
        /// </summary>
        /// <param name="keyType">The key type of the dependency, typically an interface.</param>
        /// <param name="type">The concrete type of the dependency.</param>
        private void BindLazy(Type keyType, Type type)
        {
            if (!keyType.IsAssignableFrom(type))
            {
                throw new DependencyInjectionException(string.Format("{0} does not implement {1}",
                        type.FullName, keyType.FullName));
            }
            _lazyFactories[keyType] = new DefaultConstructorDependencyFactory(type);
        }

        /// <summary>
        /// Add a lazy dependency provided by a static method.
        /// </summary>
        /// <param name="method">Method.</param>
        private void BindLazy(MethodInfo method)
        {
            if (!method.IsStatic)
            {
                throw new DependencyInjectionException(
                    string.Format("{0} is not static", method.Name));
            }
            _lazyFactories[method.ReturnType] = new MethodInvokeDependencyFactory(
                method, null, null);
        }

        /// <summary>
        /// Create an instance of a lazy dependency and bind it as an actual dependency.
        /// </summary>
        /// <returns>The created instance.</returns>
        /// <param name="type">The lazy dependency's key type.</param>
        private object InitializeLazy(Type type)
        {
            if (!_lazyFactories.ContainsKey(type))
            {
                throw new UnmetDependencyException(
                    string.Format("No factory found for service {0}", type.ToString())
                );
            }

            var instance = _lazyFactories[type].Get();
            if (instance == null)
            {
                throw new UnmetDependencyException(
                    string.Format("The factory for service {0} returned null", type.ToString())
                );
            }

            Bind(type, instance);
            return instance;
        }
    }
}
