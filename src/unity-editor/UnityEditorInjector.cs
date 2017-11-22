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
using UnityEngine;
using UnityEditor;

namespace mtti.Inject
{
	/// <summary>
	/// A variant of <see cref="mtti.Inject.Unity.UnityInjector"/> for use inside the Unity editor.
	/// Injects dependencies to methods with <see cref="mtti.Inject.InjectInEditorAttribute"/>
	/// instead of the usual <see cref="mtti.Inject.InjectAttribute"/>.
	/// </summary>
	public class UnityEditorInjector : UnityInjector
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="mtti.Inject.Unity.UnityInjector"/> class.
		/// </summary>
		public UnityEditorInjector() : base(typeof(InjectInEditorAttribute))
		{
			BindLazyFromCurrentAppDomain();
		}
	}
}
