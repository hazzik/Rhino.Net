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
	[System.Serializable]
	internal class SpecialRef : Ref
	{
		internal const long serialVersionUID = -7521596632456797847L;

		private const int SPECIAL_NONE = 0;

		private const int SPECIAL_PROTO = 1;

		private const int SPECIAL_PARENT = 2;

		private Scriptable target;

		private int type;

		private string name;

		private SpecialRef(Scriptable target, int type, string name)
		{
			this.target = target;
			this.type = type;
			this.name = name;
		}

		internal static Ref CreateSpecial(Context cx, object @object, string name)
		{
			Scriptable target = ScriptRuntime.ToObjectOrNull(cx, @object);
			if (target == null)
			{
				throw ScriptRuntime.UndefReadError(@object, name);
			}
			int type;
			if (name.Equals("__proto__"))
			{
				type = SPECIAL_PROTO;
			}
			else
			{
				if (name.Equals("__parent__"))
				{
					type = SPECIAL_PARENT;
				}
				else
				{
					throw new ArgumentException(name);
				}
			}
			if (!cx.HasFeature(Context.FEATURE_PARENT_PROTO_PROPERTIES))
			{
				// Clear special after checking for valid name!
				type = SPECIAL_NONE;
			}
			return new Rhino.SpecialRef(target, type, name);
		}

		public override object Get(Context cx)
		{
			switch (type)
			{
				case SPECIAL_NONE:
				{
					return ScriptRuntime.GetObjectProp(target, name, cx);
				}

				case SPECIAL_PROTO:
				{
					return target.GetPrototype();
				}

				case SPECIAL_PARENT:
				{
					return target.GetParentScope();
				}

				default:
				{
					throw Kit.CodeBug();
				}
			}
		}

		public override object Set(Context cx, object value)
		{
			switch (type)
			{
				case SPECIAL_NONE:
				{
					return ScriptRuntime.SetObjectProp(target, name, value, cx);
				}

				case SPECIAL_PROTO:
				case SPECIAL_PARENT:
				{
					Scriptable obj = ScriptRuntime.ToObjectOrNull(cx, value);
					if (obj != null)
					{
						// Check that obj does not contain on its prototype/scope
						// chain to prevent cycles
						Scriptable search = obj;
						do
						{
							if (search == target)
							{
								throw Context.ReportRuntimeError1("msg.cyclic.value", name);
							}
							if (type == SPECIAL_PROTO)
							{
								search = search.GetPrototype();
							}
							else
							{
								search = search.GetParentScope();
							}
						}
						while (search != null);
					}
					if (type == SPECIAL_PROTO)
					{
						target.SetPrototype(obj);
					}
					else
					{
						target.SetParentScope(obj);
					}
					return obj;
				}

				default:
				{
					throw Kit.CodeBug();
				}
			}
		}

		public override bool Has(Context cx)
		{
			if (type == SPECIAL_NONE)
			{
				return ScriptRuntime.HasObjectElem(target, name, cx);
			}
			return true;
		}

		public override bool Delete(Context cx)
		{
			if (type == SPECIAL_NONE)
			{
				return ScriptRuntime.DeleteObjectElem(target, name, cx);
			}
			return false;
		}
	}
}
