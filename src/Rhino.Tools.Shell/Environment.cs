/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rhino;
using Sharpen;

namespace Rhino.Tools.Shell
{
	/// <summary>
	/// Environment, intended to be instantiated at global scope, provides
	/// a natural way to access System properties from JavaScript.
	/// </summary>
	/// <remarks>
	/// Environment, intended to be instantiated at global scope, provides
	/// a natural way to access System properties from JavaScript.
	/// </remarks>
	/// <author>Patrick C. Beard</author>
	[System.Serializable]
	public class Environment : ScriptableObject
	{
		private Rhino.Tools.Shell.Environment thePrototypeInstance = null;

		public static void DefineClass(ScriptableObject scope)
		{
			try
			{
				ScriptableObject.DefineClass<Rhino.Tools.Shell.Environment>(scope);
			}
			catch (Exception e)
			{
				throw new Exception(e.Message);
			}
		}

		public override string GetClassName()
		{
			return "Environment";
		}

		public Environment()
		{
			if (thePrototypeInstance == null)
			{
				thePrototypeInstance = this;
			}
		}

		public Environment(ScriptableObject scope)
		{
			SetParentScope(scope);
			object ctor = ScriptRuntime.GetTopLevelProp(scope, "Environment");
			if (ctor != null && ctor is Scriptable)
			{
				Scriptable s = (Scriptable)ctor;
				SetPrototype((Scriptable)s.Get("prototype", s));
			}
		}

		public override bool Has(string name, Scriptable start)
		{
			if (this == thePrototypeInstance)
			{
				return base.Has(name, start);
			}
			return (Runtime.GetProperty(name) != null);
		}

		public override object Get(string name, Scriptable start)
		{
			if (this == thePrototypeInstance)
			{
				return base.Get(name, start);
			}
			string result = Runtime.GetProperty(name);
			if (result != null)
			{
				return ScriptRuntime.ToObject(GetParentScope(), result);
			}
			else
			{
				return ScriptableConstants.NOT_FOUND;
			}
		}

		public override void Put(string name, Scriptable start, object value)
		{
			if (this == thePrototypeInstance)
			{
				base.Put(name, start, value);
			}
			else
			{
				Runtime.GetProperties()[name] = ScriptRuntime.ToString(value);
			}
		}

		private object[] CollectIds()
		{
			Hashtable props = Runtime.GetProperties();
			return props.Keys.Cast<object>().ToArray();
		}

		public override object[] GetIds()
		{
			if (this == thePrototypeInstance)
			{
				return base.GetIds();
			}
			return CollectIds();
		}

		public override object[] GetAllIds()
		{
			if (this == thePrototypeInstance)
			{
				return base.GetAllIds();
			}
			return CollectIds();
		}
	}
}
