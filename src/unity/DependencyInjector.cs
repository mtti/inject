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
	public class DependencyInjector : MonoBehaviour
	{
		public UnityInjector Injector
		{
			get
			{
				return this.injector;
			}
		}

		private UnityInjector injector;

		private bool started = false;

		private void Awake()
		{
			this.injector = new UnityInjector();
			this.injector.BindLazyFromCurrentAppDomain();
		}

		private void Start()
		{
			InjectAllScenes();
			SceneManager.sceneLoaded += this.OnSceneLoaded;
			this.started = true;
		}

		private void OnDestroy()
		{
			SceneManager.sceneLoaded -= this.OnSceneLoaded;
		}

		private void Update()
		{
			this.injector.OnUpdate();
		}

		private void InjectAllScenes()
		{
			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				var scene = SceneManager.GetSceneAt(i);
				if (scene.isLoaded)
				{
					this.injector.Inject(scene);
				}
				else
				{
					Debug.LogWarningFormat("Not injecting dependencies to scene {0} as it's not loaded",
						scene.path);
				}
			}
		}

		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			if (this.started)
			{
				this.injector.Inject(scene);
			}
		}
	}
}
