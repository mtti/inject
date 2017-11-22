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

using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace mtti.Inject
{
	public class EditorDependencyInjector
	{
		static EditorDependencyInjector()
		{
			CheckEditMode();
			EditorApplication.playmodeStateChanged += PlayModeStateChanged;
		}

		public static EditorDependencyInjector Instance
		{
			get
			{
				return instance;
			}
		}

		private static EditorDependencyInjector instance = null;

		[UnityEditor.Callbacks.DidReloadScripts]
		private static void DidReloadScripts()
		{
			CheckEditMode();
		}

		private static void PlayModeStateChanged()
		{
			if (EditorApplication.isPlaying)
			{
				instance = null;
			}
			CheckEditMode();
		}

		private static void CheckEditMode()
		{
			if (instance == null && !EditorApplication.isPlaying)
			{
				instance = new EditorDependencyInjector();
				instance.Initialize();
			}
		}

		public UnityEditorInjector Injector
		{
			get
			{
				return this.injector;
			}
		}

		private UnityEditorInjector injector;

		private EditorDependencyInjector()
		{
		}

		private void Initialize()
		{
			this.injector = new UnityEditorInjector();
			SceneManager.sceneLoaded += this.OnSceneLoaded;
			InjectAllScenes();
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
			}
		}

		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			this.injector.Inject(scene);
		}
	}
}
