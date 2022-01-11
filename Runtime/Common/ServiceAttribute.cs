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

namespace mtti.Inject
{
    /// <summary>
    /// Marks a class or static method as a provider of a service.
    /// </summary>
    /// <remarks>
    /// When <see cref="mtti.Inject.Injector.BindLazyFromCurrentAppDomain"/> is called, the injector
    /// will find every class and method with this attribute in the current application injector and
    /// add them as lazy dependencies.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class ServiceAttribute : Attribute
    {
        public Type KeyType
        {
            get
            {
                return _keyType;
            }
        }

        private Type _keyType;

        public ServiceAttribute()
        {
            _keyType = null;
        }

        public ServiceAttribute(Type keyType)
        {
            _keyType = keyType;
        }
    }
}
