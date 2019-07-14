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

namespace mtti.Inject
{
    /// <summary>
    /// Dependency factory that creates a dependency instance by calling its empty default
    /// constructor.
    /// </summary>
    public class DefaultConstructorDependencyFactory : IDependencyFactory
    {
        private Type _type = null;

        public DefaultConstructorDependencyFactory(Type type)
        {
            _type = type;
        }

        public object Get()
        {
            return Activator.CreateInstance(_type);
        }
    }
}
