/*
Copyright 2017-2022 Matti Hiltunen

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

namespace mtti.Inject
{
    /// <summary>
    /// Dependency factory that creates a dependency instance by calling a method.
    /// </summary>
    public class MethodInvokeDependencyFactory : IDependencyFactory
    {
        private MethodInfo _method = null;

        private object _instance = null;

        private object[] _parameters = null;

        public MethodInvokeDependencyFactory(MethodInfo method, object instance,
            object[] parameters)
        {
            _method = method;
            _instance = instance;
            _parameters = parameters;
        }

        public object Get()
        {
            return _method.Invoke(_instance, _parameters);
        }
    }
}
