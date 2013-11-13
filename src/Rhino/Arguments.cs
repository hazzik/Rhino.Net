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
	/// <summary>This class implements the "arguments" object.</summary>
	/// <remarks>
	/// This class implements the "arguments" object.
	/// See ECMA 10.1.8
	/// </remarks>
	/// <seealso cref="NativeCall">NativeCall</seealso>
	/// <author>Norris Boyd</author>
	[System.Serializable]
	internal sealed class Arguments : IdScriptableObject
	{
		internal const long serialVersionUID = 4275508002492040609L;

		private const string FTAG = "Arguments";

		public Arguments(NativeCall activation)
		{
			this.activation = activation;
			Scriptable parent = activation.GetParentScope();
			SetParentScope(parent);
			SetPrototype(ScriptableObject.GetObjectPrototype(parent));
			args = activation.originalArgs;
			lengthObj = Sharpen.Extensions.ValueOf(args.Length);
			NativeFunction f = activation.function;
			calleeObj = f;
			Scriptable topLevel = GetTopLevelScope(parent);
			constructor = GetProperty(topLevel, "Object");
			int version = f.GetLanguageVersion();
			if (version <= Context.VERSION_1_3 && version != Context.VERSION_DEFAULT)
			{
				callerObj = null;
			}
			else
			{
				callerObj = ScriptableConstants.NOT_FOUND;
			}
		}

		public override string GetClassName()
		{
			return FTAG;
		}

		private object Arg(int index)
		{
			if (index < 0 || args.Length <= index)
			{
				return ScriptableConstants.NOT_FOUND;
			}
			return args[index];
		}

		// the following helper methods assume that 0 < index < args.length
		private void PutIntoActivation(int index, object value)
		{
			string argName = activation.function.GetParamOrVarName(index);
			activation.Put(argName, activation, value);
		}

		private object GetFromActivation(int index)
		{
			string argName = activation.function.GetParamOrVarName(index);
			return activation.Get(argName, activation);
		}

		private void ReplaceArg(int index, object value)
		{
			if (SharedWithActivation(index))
			{
				PutIntoActivation(index, value);
			}
			lock (this)
			{
				if (args == activation.originalArgs)
				{
					args = args.Clone();
				}
				args[index] = value;
			}
		}

		private void RemoveArg(int index)
		{
			lock (this)
			{
				if (args[index] != ScriptableConstants.NOT_FOUND)
				{
					if (args == activation.originalArgs)
					{
						args = args.Clone();
					}
					args[index] = ScriptableConstants.NOT_FOUND;
				}
			}
		}

		// end helpers
		public override bool Has(int index, Scriptable start)
		{
			if (Arg(index) != ScriptableConstants.NOT_FOUND)
			{
				return true;
			}
			return base.Has(index, start);
		}

		public override object Get(int index, Scriptable start)
		{
			object value = Arg(index);
			if (value == ScriptableConstants.NOT_FOUND)
			{
				return base.Get(index, start);
			}
			else
			{
				if (SharedWithActivation(index))
				{
					return GetFromActivation(index);
				}
				else
				{
					return value;
				}
			}
		}

		private bool SharedWithActivation(int index)
		{
			NativeFunction f = activation.function;
			int definedCount = f.GetParamCount();
			if (index < definedCount)
			{
				// Check if argument is not hidden by later argument with the same
				// name as hidden arguments are not shared with activation
				if (index < definedCount - 1)
				{
					string argName = f.GetParamOrVarName(index);
					for (int i = index + 1; i < definedCount; i++)
					{
						if (argName.Equals(f.GetParamOrVarName(i)))
						{
							return false;
						}
					}
				}
				return true;
			}
			return false;
		}

		public override void Put(int index, Scriptable start, object value)
		{
			if (Arg(index) == ScriptableConstants.NOT_FOUND)
			{
				base.Put(index, start, value);
			}
			else
			{
				ReplaceArg(index, value);
			}
		}

		public override void Delete(int index)
		{
			if (0 <= index && index < args.Length)
			{
				RemoveArg(index);
			}
			base.Delete(index);
		}

		private const int Id_callee = 1;

		private const int Id_length = 2;

		private const int Id_caller = 3;

		private const int Id_constructor = 4;

		private const int MAX_INSTANCE_ID = Id_constructor;

		// #string_id_map#
		protected internal override int GetMaxInstanceId()
		{
			return MAX_INSTANCE_ID;
		}

		protected internal override int FindInstanceIdInfo(string s)
		{
			int id;
			// #generated# Last update: 2010-01-06 05:48:21 ARST
			id = 0;
			string X = null;
			int c;
			int s_length = s.Length;
			if (s_length == 6)
			{
				c = s[5];
				if (c == 'e')
				{
					X = "callee";
					id = Id_callee;
				}
				else
				{
					if (c == 'h')
					{
						X = "length";
						id = Id_length;
					}
					else
					{
						if (c == 'r')
						{
							X = "caller";
							id = Id_caller;
						}
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
			if (X != null && X != s && !X.Equals(s))
			{
				id = 0;
			}
			goto L0_break;
L0_break: ;
			// #/generated#
			if (id == 0)
			{
				return base.FindInstanceIdInfo(s);
			}
			int attr;
			switch (id)
			{
				case Id_callee:
				case Id_caller:
				case Id_length:
				case Id_constructor:
				{
					attr = DONTENUM;
					break;
				}

				default:
				{
					throw new InvalidOperationException();
				}
			}
			return InstanceIdInfo(attr, id);
		}

		// #/string_id_map#
		protected internal override string GetInstanceIdName(int id)
		{
			switch (id)
			{
				case Id_callee:
				{
					return "callee";
				}

				case Id_length:
				{
					return "length";
				}

				case Id_caller:
				{
					return "caller";
				}

				case Id_constructor:
				{
					return "constructor";
				}
			}
			return null;
		}

		protected internal override object GetInstanceIdValue(int id)
		{
			switch (id)
			{
				case Id_callee:
				{
					return calleeObj;
				}

				case Id_length:
				{
					return lengthObj;
				}

				case Id_caller:
				{
					object value = callerObj;
					if (value == UniqueTag.NULL_VALUE)
					{
						value = null;
					}
					else
					{
						if (value == null)
						{
							NativeCall caller = activation.parentActivationCall;
							if (caller != null)
							{
								value = caller.Get("arguments", caller);
							}
						}
					}
					return value;
				}

				case Id_constructor:
				{
					return constructor;
				}
			}
			return base.GetInstanceIdValue(id);
		}

		protected internal override void SetInstanceIdValue(int id, object value)
		{
			switch (id)
			{
				case Id_callee:
				{
					calleeObj = value;
					return;
				}

				case Id_length:
				{
					lengthObj = value;
					return;
				}

				case Id_caller:
				{
					callerObj = (value != null) ? value : UniqueTag.NULL_VALUE;
					return;
				}

				case Id_constructor:
				{
					constructor = value;
					return;
				}
			}
			base.SetInstanceIdValue(id, value);
		}

		internal override object[] GetIds(bool getAll)
		{
			object[] ids = base.GetIds(getAll);
			if (args.Length != 0)
			{
				bool[] present = new bool[args.Length];
				int extraCount = args.Length;
				for (int i = 0; i != ids.Length; ++i)
				{
					object id = ids[i];
					if (id is int)
					{
						int index = ((int)id);
						if (0 <= index && index < args.Length)
						{
							if (!present[index])
							{
								present[index] = true;
								extraCount--;
							}
						}
					}
				}
				if (!getAll)
				{
					// avoid adding args which were redefined to non-enumerable
					for (int i_1 = 0; i_1 < present.Length; i_1++)
					{
						if (!present[i_1] && base.Has(i_1, this))
						{
							present[i_1] = true;
							extraCount--;
						}
					}
				}
				if (extraCount != 0)
				{
					object[] tmp = new object[extraCount + ids.Length];
					System.Array.Copy(ids, 0, tmp, extraCount, ids.Length);
					ids = tmp;
					int offset = 0;
					for (int i_1 = 0; i_1 != args.Length; ++i_1)
					{
						if (present == null || !present[i_1])
						{
							ids[offset] = Sharpen.Extensions.ValueOf(i_1);
							++offset;
						}
					}
					if (offset != extraCount)
					{
						Kit.CodeBug();
					}
				}
			}
			return ids;
		}

		protected internal override ScriptableObject GetOwnPropertyDescriptor(Context cx, object id)
		{
			double d = ScriptRuntime.ToNumber(id);
			int index = (int)d;
			if (d != index)
			{
				return base.GetOwnPropertyDescriptor(cx, id);
			}
			object value = Arg(index);
			if (value == ScriptableConstants.NOT_FOUND)
			{
				return base.GetOwnPropertyDescriptor(cx, id);
			}
			if (SharedWithActivation(index))
			{
				value = GetFromActivation(index);
			}
			if (base.Has(index, this))
			{
				// the descriptor has been redefined
				ScriptableObject desc = base.GetOwnPropertyDescriptor(cx, id);
				desc.Put("value", desc, value);
				return desc;
			}
			else
			{
				Scriptable scope = GetParentScope();
				if (scope == null)
				{
					scope = this;
				}
				return BuildDataDescriptor(scope, value, EMPTY);
			}
		}

		protected internal override void DefineOwnProperty(Context cx, object id, ScriptableObject desc, bool checkValid)
		{
			base.DefineOwnProperty(cx, id, desc, checkValid);
			double d = ScriptRuntime.ToNumber(id);
			int index = (int)d;
			if (d != index)
			{
				return;
			}
			object value = Arg(index);
			if (value == ScriptableConstants.NOT_FOUND)
			{
				return;
			}
			if (IsAccessorDescriptor(desc))
			{
				RemoveArg(index);
				return;
			}
			object newValue = GetProperty(desc, "value");
			if (newValue == ScriptableConstants.NOT_FOUND)
			{
				return;
			}
			ReplaceArg(index, newValue);
			if (IsFalse(GetProperty(desc, "writable")))
			{
				RemoveArg(index);
			}
		}

		private object callerObj;

		private object calleeObj;

		private object lengthObj;

		private object constructor;

		private NativeCall activation;

		private object[] args;
		// Fields to hold caller, callee and length properties,
		// where NOT_FOUND value tags deleted properties.
		// In addition if callerObj == NULL_VALUE, it tags null for scripts, as
		// initial callerObj == null means access to caller arguments available
		// only in JS <= 1.3 scripts
		// Initially args holds activation.getOriginalArgs(), but any modification
		// of its elements triggers creation of a copy. If its element holds NOT_FOUND,
		// it indicates deleted index, in which case super class is queried.
	}
}
