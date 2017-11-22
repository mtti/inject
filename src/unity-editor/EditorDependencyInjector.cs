﻿/*
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
		private static EditorDependencyInjector s_instance = null;

		static EditorDependencyInjector()
		{
			CheckEditMode();
			EditorApplication.playmodeStateChanged += PlayModeStateChanged;
		}

		public static EditorDependencyInjector Instance
		{
			get
			{
				return s_instance;
			}
		}

		[UnityEditor.Callbacks.DidReloadScripts]
		private static void DidReloadScripts()
		{
			CheckEditMode();
		}

		private static void PlayModeStateChanged()
		{
			if (EditorApplication.isPlaying)
			{
				s_instance = null;
			}
			CheckEditMode();
		}

		private static void CheckEditMode()
		{
			if (s_instance == null && !EditorApplication.isPlaying)
			{
				s_instance = new EditorDependencyInjector();
				s_instance.Initialize();
			}
		}

		private UnityEditorInjector _injector;

		public UnityEditorInjector Injector
		{
			get
			{
				return _injector;
			}
		}

		private EditorDependencyInjector()
		{
		}

		private void Initialize()
		{
			_injector = new UnityEditorInjector();
			SceneManager.sceneLoaded += OnSceneLoaded;
			InjectAllScenes();
		}

		private void InjectAllScenes()
		{
			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				var scene = SceneManager.GetSceneAt(i);
				if (scene.isLoaded)
				{
					_injector.Inject(scene);
				}
			}
		}

		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			_injector.Inject(scene);
		}
	}
}
