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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace mtti.Inject
{
    /// <summary>
    /// Unity-specific <see cref="mtti.Common.Context"/> with additional methods for injecting
    /// depdendencies into Unity scenes and GameObjects.
    /// </summary>
    public class UnityContext : Context
    {
        /// <summary>
        /// Temporarily holds the MonoBehaviours of a GameObject while dependencies are injected
        /// into them.
        /// </summary>
        protected List<MonoBehaviour> componentBuffer = new List<MonoBehaviour>();

        /// <summary>
        /// Initializes a new instance of the <see cref="mtti.Common.Unity.UnityContext"/> class.
        /// </summary>
        public UnityContext() : base()
        {
            Initialize();
        }

        protected UnityContext(Type attributeType) : base(attributeType)
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
            //Debug.LogFormat("Injecting {0} root game objects in scene {1}", rootGameObjects.Length, scene.name);
            for (int i = 0; i < rootGameObjects.Length; i++)
            {
                this.Inject(rootGameObjects[i]);
            }
        }

        /// <summary>
        /// Inject dependencies into a Unity GameObject and all it's children.
        /// </summary>
        /// <param name="obj">Target GameObject.</param>
        public void Inject(GameObject obj)
        {
            //Debug.LogFormat("Injecting {0}", obj.name);
            obj.GetComponents<MonoBehaviour>(this.componentBuffer);
            for (int i = 0, count = this.componentBuffer.Count; i < count; i++)
            {
                //Debug.LogFormat("Injecting component {0}", this.componentBuffer[i].GetType().FullName);
                this.Inject(this.componentBuffer[i]);
            }
            this.componentBuffer.Clear();

            for (int i = 0, count = obj.transform.childCount; i < count; i++)
            {
                this.Inject(obj.transform.GetChild(i).gameObject);
            }
        }

        private void Initialize()
        {
            this.Bind<UnityContext>(this);
        }
    }
}
