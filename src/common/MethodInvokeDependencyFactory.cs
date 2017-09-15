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

namespace mtti.Inject
{
    /// <summary>
    /// Dependency factory that creates a dependency instance by calling a method.
    /// </summary>
    public class MethodInvokeDependencyFactory : IDependencyFactory
    {
        private MethodInfo method = null;

        private object instance = null;

        private object[] parameters = null;

        public MethodInvokeDependencyFactory(MethodInfo method, object instance,
            object[] parameters)
        {
            this.method = method;
            this.instance = instance;
            this.parameters = parameters;
        }

        public object Get()
        {
            return this.method.Invoke(this.instance, this.parameters);
        }
    }
}
