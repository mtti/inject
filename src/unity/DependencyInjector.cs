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
				return _injector;
			}
		}

		private UnityInjector _injector;

		private bool _started = false;

		private void InjectAllScenes()
		{
			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				var scene = SceneManager.GetSceneAt(i);
				if (scene.isLoaded)
				{
					_injector.Inject(scene);
				}
				else
				{
					Debug.LogWarningFormat(
                        "Not injecting dependencies to scene {0} as it's not loaded", scene.path);
				}
			}
		}

		private void Awake()
		{
			_injector = new UnityInjector();
			_injector.BindLazyFromCurrentAppDomain();
		}

		private void Start()
		{
			InjectAllScenes();
			SceneManager.sceneLoaded += OnSceneLoaded;
			_started = true;
		}

		private void OnDestroy()
		{
			SceneManager.sceneLoaded -= OnSceneLoaded;
		}

		private void Update()
		{
			_injector.OnUpdate();
		}

		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			if (_started)
			{
				_injector.Inject(scene);
			}
		}
	}
}
