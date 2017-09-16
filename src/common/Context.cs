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
    public class Context
    {
        /// <summary>
        /// Finds all types in the current application domain which have a specific attribute.
        /// </summary>
        public static void FindAllTypesWithAttribute(Type attributeType, List<Type> result)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
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

        private List<IUpdate> updateables = new List<IUpdate>();

        /// <summary>
        /// The type of the attribute used to find inject methods. This is changeable to allow
        /// <see cref="mtti.Inject.UnityEditorContext"/> to inject dependencies inside the Unity
        /// editor using its own <see cref="mtti.Inject.InjectInEditorAttribute"/> rather than the
        /// default <see cref="mtti.Inject.InjectAttribute"/>.
        /// </summary>
        private Type attributeType = typeof(InjectAttribute);

        /// <summary>
        /// Stores the injectable dependencies.
        /// </summary>
        private Dictionary<Type, object> dependencies = new Dictionary<Type, object>();

        /// <summary>
        /// Factories for lazy dependencies keyed by dependency type.
        /// </summary>
        private Dictionary<Type, IDependencyFactory> lazyFactories
            = new Dictionary<Type, IDependencyFactory>();

        /// <summary>
        /// Indexes dependent types by dependency type.
        /// </summary>
        private Dictionary<Type, HashSet<Type>> relationships
            = new Dictionary<Type, HashSet<Type>>();

        /// <summary>
        /// Caches injectable fields per type so that they don't need to be looked up every time an object of that
        /// type is injected.
        /// </summary>
        private Dictionary<Type, List<FieldInfo>> fieldCache = new Dictionary<Type, List<FieldInfo>>();

        /// <summary>
        /// Caches injectable methods per type.
        /// </summary>
        private Dictionary<Type, List<MethodInfo>> methodCache
            = new Dictionary<Type, List<MethodInfo>>();

        /// <summary>
        /// Caches arguments of injectable methods per type. List indexes match those of
        /// methodCache.
        /// </summary>
        private Dictionary<Type, List<object[]>> argumentCache
            = new Dictionary<Type, List<object[]>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="mtti.Inject.Context"/> class.
        /// </summary>
        public Context()
        {
            this.Initialize(typeof(InjectAttribute));
        }

        protected Context(Type attributeType)
        {
            this.Initialize(attributeType);
        }

        /// <summary>
        /// Add a new injectable dependency to this context. The generic version.
        /// </summary>
        /// <param name="obj">The concrete implementation of T</param>
        /// <typeparam name="T">The type of the dependency, typically an interface.</typeparam>
        public Context Bind<T>(T obj) where T : class
        {
            var type = typeof(T);
            return Bind(type, obj);
        }

        /// <summary>
        /// Add a new injectable dependency to this context. The non-generic version.
        /// </summary>
        /// <param name="type">The type of the dependency, typically an interface.</param>
        /// <param name="obj">An instance of the injectable.</param>
        public Context Bind(Type type, object obj)
        {
            if (this.dependencies.ContainsKey(type))
            {
                throw new DependencyInjectionException("Already bound: " + type.ToString());
            }

            if (this.relationships.ContainsKey(type))
            {
                foreach (var targetType in this.relationships[type])
                {
                    this.ClearCaches(targetType);
                }
            }

            this.dependencies[type] = obj;
            Inject(obj);

            BindUpdateable(obj);

            return this;
        }

        /// <summary>
        /// Retrieve a dependency by its type. The generic version.
        /// </summary>
        /// <typeparam name="T">The type of the dependency, typically an interface.</typeparam>
        public T Get<T>() where T : class
        {
            return this.Get(typeof(T)) as T;
        }

        /// <summary>
        /// Get the specified type. The non-generic version.
        /// </summary>
        /// <param name="type">The type of the dependency, typically and interface.</param>
        public object Get(Type type)
        {
            if (this.dependencies.ContainsKey(type))
            {
                return this.dependencies[type];
            }
            else if (this.lazyFactories.ContainsKey(type))
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
        /// Inject dependencies into an object. Fields with
        /// <see cref="mtti.Inject.InjectAttribute"/> will be set to values from this context.
        /// Methods with <see cref="mtti.Inject.InjectAttribute"/> will be executed with arguments
        /// set to values from this context. Methods must have at least one parameter to be called.
        /// </summary>
        /// <param name="target">The target object.</param>
        public void Inject(object target)
        {
            var targetType = target.GetType();
            this.InjectFields(target, targetType);
            this.InjectMethods(target, targetType);
        }

        /// <summary>
        /// Call OnUpdate on all bound services that implement IUpdate.
        /// </summary>
        public void OnUpdate()
        {
            for (int i = 0, count = this.updateables.Count; i < count; i++)
            {
                this.updateables[i].OnUpdate();
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

        private void Initialize(Type attributeType)
        {
            this.attributeType = attributeType;
            this.dependencies[typeof(Context)] = this;
        }

        /// <summary>
        /// Add object to the internal list of OnUpdate listeners if it implements IUpdate.
        /// </summary>
        /// <param name="target">Target.</param>
        private void BindUpdateable(object target)
        {
            var updateable = target as IUpdate;
            if (updateable != null && !this.updateables.Contains(updateable))
            {
                this.updateables.Add(updateable);
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

            if (this.fieldCache.ContainsKey(type))
            {
                cachedFields = this.fieldCache[type];
            }
            else
            {
                cachedFields = new List<FieldInfo>();

                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0; i < fields.Length; i++)
                {
                    var attribute
                        = Attribute.GetCustomAttribute(fields[i], this.attributeType, true);
                    if (attribute != null)
                    {
                        this.AddRelationship(fields[i].FieldType, type);
                        cachedFields.Add(fields[i]);
                    }
                }
                this.fieldCache[type] = cachedFields;
            }

            int count = cachedFields.Count;
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    var field = cachedFields[i];
                    field.SetValue(target, this.Get(field.FieldType));
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

            if (this.methodCache.ContainsKey(type))
            {
                cachedMethods = this.methodCache[type];
                cachedArguments = this.argumentCache[type];
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
                        = Attribute.GetCustomAttribute(methods[i], this.attributeType, true);
                    if (attribute == null)
                    {
                        continue;
                    }

                    var parameters = methods[i].GetParameters();
                    object[] arguments = new object[parameters.Length];
                    for (int j = 0; j < parameters.Length; j++)
                    {
                        this.AddRelationship(parameters[j].ParameterType, type);
                        arguments[j] = this.Get(parameters[j].ParameterType);
                    }

                    cachedMethods.Add(methods[i]);
                    cachedArguments.Add(arguments);
                }
                this.methodCache[type] = cachedMethods;
                this.argumentCache[type] = cachedArguments;
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
            if (!this.relationships.ContainsKey(dependencyType))
            {
                this.relationships[dependencyType] = new HashSet<Type>();
            }
            this.relationships[dependencyType].Add(targetType);
        }

        private void ClearCaches(Type targetType)
        {
            this.fieldCache.Remove(targetType);
            this.methodCache.Remove(targetType);
            this.argumentCache.Remove(targetType);
        }

        /// <summary>
        /// Add a lazy dependency into the context. Lazy dependencies are initialized when they're
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
            this.lazyFactories[keyType] = new DefaultConstructorDependencyFactory(type);
            //UnityEngine.Debug.LogFormat("BindLazy {0}", keyType.FullName);
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
            this.lazyFactories[method.ReturnType] = new MethodInvokeDependencyFactory(
                method, null, null);
        }

        /// <summary>
        /// Create an instance of a lazy dependency and bind it as an actual dependency.
        /// </summary>
        /// <returns>The created instance.</returns>
        /// <param name="type">The lazy dependency's key type.</param>
        private object InitializeLazy(Type type)
        {
            //UnityEngine.Debug.LogFormat("InitializeLazy {0}", type.FullName);
            if (!this.lazyFactories.ContainsKey(type))
            {
                throw new DependencyInjectionException("Unmet dependency: " + type.ToString());
            }
            var instance = this.lazyFactories[type].Get();
            Bind(type, instance);
            return instance;
        }
    }
}
