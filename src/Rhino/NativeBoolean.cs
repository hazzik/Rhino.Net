/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Rhino;
using Sharpen;

namespace Rhino
{
	/// <summary>This class implements the Boolean native object.</summary>
	/// <remarks>
	/// This class implements the Boolean native object.
	/// See ECMA 15.6.
	/// </remarks>
	/// <author>Norris Boyd</author>
	[System.Serializable]
	internal sealed class NativeBoolean : IdScriptableObject
	{
		private static readonly object BOOLEAN_TAG = "Boolean";

		internal static void Init(Scriptable scope, bool @sealed)
		{
			Rhino.NativeBoolean obj = new Rhino.NativeBoolean(false);
			obj.ExportAsJSClass(MAX_PROTOTYPE_ID, scope, @sealed);
		}

		internal NativeBoolean(bool b)
		{
			booleanValue = b;
		}

		public override string GetClassName()
		{
			return "Boolean";
		}

		public override object GetDefaultValue(Type typeHint)
		{
			// This is actually non-ECMA, but will be proposed
			// as a change in round 2.
			if (typeHint == ScriptRuntime.BooleanClass)
			{
				return booleanValue;
			}
			return base.GetDefaultValue(typeHint);
		}

		protected internal override void InitPrototypeId(int id)
		{
			string s;
			int arity;
			switch (id)
			{
				case Id_constructor:
				{
					arity = 1;
					s = "constructor";
					break;
				}

				case Id_toString:
				{
					arity = 0;
					s = "toString";
					break;
				}

				case Id_toSource:
				{
					arity = 0;
					s = "toSource";
					break;
				}

				case Id_valueOf:
				{
					arity = 0;
					s = "valueOf";
					break;
				}

				default:
				{
					throw new ArgumentException(id.ToString());
				}
			}
			InitPrototypeMethod(BOOLEAN_TAG, id, s, arity);
		}

		public override object ExecIdCall(IdFunctionObject f, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (!f.HasTag(BOOLEAN_TAG))
			{
				return base.ExecIdCall(f, cx, scope, thisObj, args);
			}
			int id = f.MethodId();
			if (id == Id_constructor)
			{
				bool b;
				if (args.Length == 0)
				{
					b = false;
				}
				else
				{
					var scriptableObject = args[0] as ScriptableObject;
					b = scriptableObject != null && scriptableObject.AvoidObjectDetection() || ScriptRuntime.ToBoolean(args[0]);
				}
				if (thisObj == null)
				{
					// new Boolean(val) creates a new boolean object.
					return new Rhino.NativeBoolean(b);
				}
				// Boolean(val) converts val to a boolean.
				return b;
			}
			// The rest of Boolean.prototype methods require thisObj to be Boolean
			if (!(thisObj is Rhino.NativeBoolean))
			{
				throw IncompatibleCallError(f);
			}
			bool value = ((Rhino.NativeBoolean)thisObj).booleanValue;
			switch (id)
			{
				case Id_toString:
				{
					return value ? "true" : "false";
				}

				case Id_toSource:
				{
					return value ? "(new Boolean(true))" : "(new Boolean(false))";
				}

				case Id_valueOf:
				{
					return value;
				}
			}
			throw new ArgumentException(id.ToString());
		}

		// #string_id_map#
		protected internal override int FindPrototypeId(string s)
		{
			int id;
			// #generated# Last update: 2007-05-09 08:15:31 EDT
			id = 0;
			string X = null;
			int c;
			int s_length = s.Length;
			if (s_length == 7)
			{
				X = "valueOf";
				id = Id_valueOf;
			}
			else
			{
				if (s_length == 8)
				{
					c = s[3];
					if (c == 'o')
					{
						X = "toSource";
						id = Id_toSource;
					}
					else
					{
						if (c == 't')
						{
							X = "toString";
							id = Id_toString;
						}
					}
				}
				else
				{
					if (s_length == 11)
					{
						X = "constructor";
						id = Id_constructor;
					}
				}
			}
			if (X != null && X != s && !X.Equals(s))
			{
				id = 0;
			}
			goto L0_break;
L0_break: ;
			// #/generated#
			return id;
		}

		private const int Id_constructor = 1;

		private const int Id_toString = 2;

		private const int Id_toSource = 3;

		private const int Id_valueOf = 4;

		private const int MAX_PROTOTYPE_ID = 4;

		private bool booleanValue;
		// #/string_id_map#
	}
}
