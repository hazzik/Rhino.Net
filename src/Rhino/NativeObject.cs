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
using Rhino;
using Sharpen;

namespace Rhino
{
	/// <summary>This class implements the Object native object.</summary>
	/// <remarks>
	/// This class implements the Object native object.
	/// See ECMA 15.2.
	/// </remarks>
	/// <author>Norris Boyd</author>
	[System.Serializable]
	public class NativeObject : IdScriptableObject, IDictionary
	{
		internal const long serialVersionUID = -6345305608474346996L;

		private static readonly object OBJECT_TAG = "Object";

		internal static void Init(Scriptable scope, bool @sealed)
		{
			NativeObject obj = new NativeObject();
			obj.ExportAsJSClass(MAX_PROTOTYPE_ID, scope, @sealed);
		}

		public override string GetClassName()
		{
			return "Object";
		}

		public override string ToString()
		{
			return ScriptRuntime.DefaultObjectToString(this);
		}

		protected internal override void FillConstructorProperties(IdFunctionObject ctor)
		{
			AddIdFunctionProperty(ctor, OBJECT_TAG, ConstructorId_getPrototypeOf, "getPrototypeOf", 1);
			AddIdFunctionProperty(ctor, OBJECT_TAG, ConstructorId_keys, "keys", 1);
			AddIdFunctionProperty(ctor, OBJECT_TAG, ConstructorId_getOwnPropertyNames, "getOwnPropertyNames", 1);
			AddIdFunctionProperty(ctor, OBJECT_TAG, ConstructorId_getOwnPropertyDescriptor, "getOwnPropertyDescriptor", 2);
			AddIdFunctionProperty(ctor, OBJECT_TAG, ConstructorId_defineProperty, "defineProperty", 3);
			AddIdFunctionProperty(ctor, OBJECT_TAG, ConstructorId_isExtensible, "isExtensible", 1);
			AddIdFunctionProperty(ctor, OBJECT_TAG, ConstructorId_preventExtensions, "preventExtensions", 1);
			AddIdFunctionProperty(ctor, OBJECT_TAG, ConstructorId_defineProperties, "defineProperties", 2);
			AddIdFunctionProperty(ctor, OBJECT_TAG, ConstructorId_create, "create", 2);
			AddIdFunctionProperty(ctor, OBJECT_TAG, ConstructorId_isSealed, "isSealed", 1);
			AddIdFunctionProperty(ctor, OBJECT_TAG, ConstructorId_isFrozen, "isFrozen", 1);
			AddIdFunctionProperty(ctor, OBJECT_TAG, ConstructorId_seal, "seal", 1);
			AddIdFunctionProperty(ctor, OBJECT_TAG, ConstructorId_freeze, "freeze", 1);
			base.FillConstructorProperties(ctor);
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

				case Id_toLocaleString:
				{
					arity = 0;
					s = "toLocaleString";
					break;
				}

				case Id_valueOf:
				{
					arity = 0;
					s = "valueOf";
					break;
				}

				case Id_hasOwnProperty:
				{
					arity = 1;
					s = "hasOwnProperty";
					break;
				}

				case Id_propertyIsEnumerable:
				{
					arity = 1;
					s = "propertyIsEnumerable";
					break;
				}

				case Id_isPrototypeOf:
				{
					arity = 1;
					s = "isPrototypeOf";
					break;
				}

				case Id_toSource:
				{
					arity = 0;
					s = "toSource";
					break;
				}

				case Id___defineGetter__:
				{
					arity = 2;
					s = "__defineGetter__";
					break;
				}

				case Id___defineSetter__:
				{
					arity = 2;
					s = "__defineSetter__";
					break;
				}

				case Id___lookupGetter__:
				{
					arity = 1;
					s = "__lookupGetter__";
					break;
				}

				case Id___lookupSetter__:
				{
					arity = 1;
					s = "__lookupSetter__";
					break;
				}

				default:
				{
					throw new ArgumentException(id.ToString());
				}
			}
			InitPrototypeMethod(OBJECT_TAG, id, s, arity);
		}

		public override object ExecIdCall(IdFunctionObject f, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (!f.HasTag(OBJECT_TAG))
			{
				return base.ExecIdCall(f, cx, scope, thisObj, args);
			}
			int id = f.MethodId();
			switch (id)
			{
				case Id_constructor:
				{
					if (thisObj != null)
					{
						// BaseFunction.construct will set up parent, proto
						return f.Construct(cx, scope, args);
					}
					if (args.Length == 0 || args[0] == null || args[0] == Undefined.instance)
					{
						return new NativeObject();
					}
					return ScriptRuntime.ToObject(cx, scope, args[0]);
				}

				case Id_toLocaleString:
				case Id_toString:
				{
					// For now just alias toString
					if (cx.HasFeature(Context.FEATURE_TO_STRING_AS_SOURCE))
					{
						string s = ScriptRuntime.DefaultObjectToSource(cx, scope, thisObj, args);
						int L = s.Length;
						if (L != 0 && s[0] == '(' && s[L - 1] == ')')
						{
							// Strip () that surrounds toSource
							s = Sharpen.Runtime.Substring(s, 1, L - 1);
						}
						return s;
					}
					return ScriptRuntime.DefaultObjectToString(thisObj);
				}

				case Id_valueOf:
				{
					return thisObj;
				}

				case Id_hasOwnProperty:
				{
					bool result;
					if (args.Length == 0)
					{
						result = false;
					}
					else
					{
						string s = ScriptRuntime.ToStringIdOrIndex(cx, args[0]);
						if (s == null)
						{
							int index = ScriptRuntime.LastIndexResult(cx);
							result = thisObj.Has(index, thisObj);
						}
						else
						{
							result = thisObj.Has(s, thisObj);
						}
					}
					return ScriptRuntime.WrapBoolean(result);
				}

				case Id_propertyIsEnumerable:
				{
					bool result;
					if (args.Length == 0)
					{
						result = false;
					}
					else
					{
						string s = ScriptRuntime.ToStringIdOrIndex(cx, args[0]);
						if (s == null)
						{
							int index = ScriptRuntime.LastIndexResult(cx);
							result = thisObj.Has(index, thisObj);
							if (result && thisObj is ScriptableObject)
							{
								ScriptableObject so = (ScriptableObject)thisObj;
								int attrs = so.GetAttributes(index);
								result = ((attrs & ScriptableObject.DONTENUM) == 0);
							}
						}
						else
						{
							result = thisObj.Has(s, thisObj);
							if (result && thisObj is ScriptableObject)
							{
								ScriptableObject so = (ScriptableObject)thisObj;
								int attrs = so.GetAttributes(s);
								result = ((attrs & ScriptableObject.DONTENUM) == 0);
							}
						}
					}
					return ScriptRuntime.WrapBoolean(result);
				}

				case Id_isPrototypeOf:
				{
					bool result = false;
					if (args.Length != 0 && args[0] is Scriptable)
					{
						Scriptable v = (Scriptable)args[0];
						do
						{
							v = v.GetPrototype();
							if (v == thisObj)
							{
								result = true;
								break;
							}
						}
						while (v != null);
					}
					return ScriptRuntime.WrapBoolean(result);
				}

				case Id_toSource:
				{
					return ScriptRuntime.DefaultObjectToSource(cx, scope, thisObj, args);
				}

				case Id___defineGetter__:
				case Id___defineSetter__:
				{
					if (args.Length < 2 || !(args[1] is Callable))
					{
						object badArg = (args.Length >= 2 ? args[1] : Undefined.instance);
						throw ScriptRuntime.NotFunctionError(badArg);
					}
					if (!(thisObj is ScriptableObject))
					{
						throw Context.ReportRuntimeError2("msg.extend.scriptable", thisObj.GetType().FullName, args[0].ToString());
					}
					ScriptableObject so = (ScriptableObject)thisObj;
					string name = ScriptRuntime.ToStringIdOrIndex(cx, args[0]);
					int index = (name != null ? 0 : ScriptRuntime.LastIndexResult(cx));
					Callable getterOrSetter = (Callable)args[1];
					bool isSetter = (id == Id___defineSetter__);
					so.SetGetterOrSetter(name, index, getterOrSetter, isSetter);
					if (so is NativeArray)
					{
						((NativeArray)so).SetDenseOnly(false);
					}
					return Undefined.instance;
				}

				case Id___lookupGetter__:
				case Id___lookupSetter__:
				{
					if (args.Length < 1 || !(thisObj is ScriptableObject))
					{
						return Undefined.instance;
					}
					ScriptableObject so = (ScriptableObject)thisObj;
					string name = ScriptRuntime.ToStringIdOrIndex(cx, args[0]);
					int index = (name != null ? 0 : ScriptRuntime.LastIndexResult(cx));
					bool isSetter = (id == Id___lookupSetter__);
					object gs;
					for (; ; )
					{
						gs = so.GetGetterOrSetter(name, index, isSetter);
						if (gs != null)
						{
							break;
						}
						// If there is no getter or setter for the object itself,
						// how about the prototype?
						Scriptable v = so.GetPrototype();
						if (v == null)
						{
							break;
						}
						if (v is ScriptableObject)
						{
							so = (ScriptableObject)v;
						}
						else
						{
							break;
						}
					}
					if (gs != null)
					{
						return gs;
					}
					return Undefined.instance;
				}

				case ConstructorId_getPrototypeOf:
				{
					object arg = args.Length < 1 ? Undefined.instance : args[0];
					Scriptable obj = EnsureScriptable(arg);
					return obj.GetPrototype();
				}

				case ConstructorId_keys:
				{
					object arg = args.Length < 1 ? Undefined.instance : args[0];
					Scriptable obj = EnsureScriptable(arg);
					object[] ids = obj.GetIds();
					for (int i = 0; i < ids.Length; i++)
					{
						ids[i] = ScriptRuntime.ToString(ids[i]);
					}
					return cx.NewArray(scope, ids);
				}

				case ConstructorId_getOwnPropertyNames:
				{
					object arg = args.Length < 1 ? Undefined.instance : args[0];
					ScriptableObject obj = EnsureScriptableObject(arg);
					object[] ids = obj.GetAllIds();
					for (int i = 0; i < ids.Length; i++)
					{
						ids[i] = ScriptRuntime.ToString(ids[i]);
					}
					return cx.NewArray(scope, ids);
				}

				case ConstructorId_getOwnPropertyDescriptor:
				{
					object arg = args.Length < 1 ? Undefined.instance : args[0];
					// TODO(norris): There's a deeper issue here if
					// arg instanceof Scriptable. Should we create a new
					// interface to admit the new ECMAScript 5 operations?
					ScriptableObject obj = EnsureScriptableObject(arg);
					object nameArg = args.Length < 2 ? Undefined.instance : args[1];
					string name = ScriptRuntime.ToString(nameArg);
					Scriptable desc = obj.GetOwnPropertyDescriptor(cx, name);
					return desc == null ? Undefined.instance : desc;
				}

				case ConstructorId_defineProperty:
				{
					object arg = args.Length < 1 ? Undefined.instance : args[0];
					ScriptableObject obj = EnsureScriptableObject(arg);
					object name = args.Length < 2 ? Undefined.instance : args[1];
					object descArg = args.Length < 3 ? Undefined.instance : args[2];
					ScriptableObject desc = EnsureScriptableObject(descArg);
					obj.DefineOwnProperty(cx, name, desc);
					return obj;
				}

				case ConstructorId_isExtensible:
				{
					object arg = args.Length < 1 ? Undefined.instance : args[0];
					ScriptableObject obj = EnsureScriptableObject(arg);
					return obj.IsExtensible();
				}

				case ConstructorId_preventExtensions:
				{
					object arg = args.Length < 1 ? Undefined.instance : args[0];
					ScriptableObject obj = EnsureScriptableObject(arg);
					obj.PreventExtensions();
					return obj;
				}

				case ConstructorId_defineProperties:
				{
					object arg = args.Length < 1 ? Undefined.instance : args[0];
					ScriptableObject obj = EnsureScriptableObject(arg);
					object propsObj = args.Length < 2 ? Undefined.instance : args[1];
					Scriptable props = Context.ToObject(propsObj, GetParentScope());
					obj.DefineOwnProperties(cx, EnsureScriptableObject(props));
					return obj;
				}

				case ConstructorId_create:
				{
					object arg = args.Length < 1 ? Undefined.instance : args[0];
					Scriptable obj = (arg == null) ? null : EnsureScriptable(arg);
					ScriptableObject newObject = new NativeObject();
					newObject.SetParentScope(this.GetParentScope());
					newObject.SetPrototype(obj);
					if (args.Length > 1 && args[1] != Undefined.instance)
					{
						Scriptable props = Context.ToObject(args[1], GetParentScope());
						newObject.DefineOwnProperties(cx, EnsureScriptableObject(props));
					}
					return newObject;
				}

				case ConstructorId_isSealed:
				{
					object arg = args.Length < 1 ? Undefined.instance : args[0];
					ScriptableObject obj = EnsureScriptableObject(arg);
					if (obj.IsExtensible())
					{
						return false;
					}
					foreach (object name in obj.GetAllIds())
					{
						object configurable = obj.GetOwnPropertyDescriptor(cx, name).Get("configurable");
						if (true.Equals(configurable))
						{
							return false;
						}
					}
					return true;
				}

				case ConstructorId_isFrozen:
				{
					object arg = args.Length < 1 ? Undefined.instance : args[0];
					ScriptableObject obj = EnsureScriptableObject(arg);
					if (obj.IsExtensible())
					{
						return false;
					}
					foreach (object name in obj.GetAllIds())
					{
						ScriptableObject desc = obj.GetOwnPropertyDescriptor(cx, name);
						if (true.Equals(desc.Get("configurable")))
						{
							return false;
						}
						if (IsDataDescriptor(desc) && true.Equals(desc.Get("writable")))
						{
							return false;
						}
					}
					return true;
				}

				case ConstructorId_seal:
				{
					object arg = args.Length < 1 ? Undefined.instance : args[0];
					ScriptableObject obj = EnsureScriptableObject(arg);
					foreach (object name in obj.GetAllIds())
					{
						ScriptableObject desc = obj.GetOwnPropertyDescriptor(cx, name);
						if (true.Equals(desc.Get("configurable")))
						{
							desc.Put("configurable", desc, false);
							obj.DefineOwnProperty(cx, name, desc, false);
						}
					}
					obj.PreventExtensions();
					return obj;
				}

				case ConstructorId_freeze:
				{
					object arg = args.Length < 1 ? Undefined.instance : args[0];
					ScriptableObject obj = EnsureScriptableObject(arg);
					foreach (object name in obj.GetAllIds())
					{
						ScriptableObject desc = obj.GetOwnPropertyDescriptor(cx, name);
						if (IsDataDescriptor(desc) && true.Equals(desc.Get("writable")))
						{
							desc.Put("writable", desc, false);
						}
						if (true.Equals(desc.Get("configurable")))
						{
							desc.Put("configurable", desc, false);
						}
						obj.DefineOwnProperty(cx, name, desc, false);
					}
					obj.PreventExtensions();
					return obj;
				}

				default:
				{
					throw new ArgumentException(id.ToString());
				}
			}
		}

		// methods implementing java.util.Map
		public virtual bool ContainsKey(object key)
		{
			if (key is string)
			{
				return Has((string)key, this);
			}
			else
			{
				if (key.IsNumber())
				{
					return Has(System.Convert.ToInt32(key), this);
				}
			}
			return false;
		}

		public virtual bool ContainsValue(object value)
		{
			foreach (object obj in ((ICollection<object>)Values))
			{
				if (value == obj || value != null && value.Equals(obj))
				{
					return true;
				}
			}
			return false;
		}

		public virtual object Remove(object key)
		{
			object value = Get(key);
			if (key is string)
			{
				Delete((string)key);
			}
			else
			{
				if (key.IsNumber())
				{
					Delete(System.Convert.ToInt32(key));
				}
			}
			return value;
		}

		public virtual ICollection<object> Keys
		{
			get
			{
				return new NativeObject.KeySet(this);
			}
		}

		public virtual ICollection<object> Values
		{
			get
			{
				return new NativeObject.ValueCollection(this);
			}
		}

		public virtual ICollection<KeyValuePair<object, object>> EntrySet()
		{
			return new NativeObject.EntrySet(this);
		}

		public virtual object Put(object key, object value)
		{
			throw new NotSupportedException();
		}

		public virtual void PutAll(IDictionary m)
		{
			throw new NotSupportedException();
		}

		public virtual void Clear()
		{
			throw new NotSupportedException();
		}

		internal class EntrySet : AbstractSet<KeyValuePair<object, object>>
		{
			public override IEnumerator<KeyValuePair<object, object>> GetEnumerator()
			{
				return new _IEnumerator_483(this);
			}

			private sealed class _IEnumerator_483 : IEnumerator<KeyValuePair<object, object>>
			{
				public _IEnumerator_483(EntrySet _enclosing)
				{
					this._enclosing = _enclosing;
					this.ids = this._enclosing._enclosing.GetIds();
					this.key = null;
					this.index = 0;
				}

				internal object[] ids;

				internal object key;

				internal int index;

				public bool HasNext()
				{
					return this.index < this.ids.Length;
				}

				public KeyValuePair<object, object> Next()
				{
					object ekey = this.key = this.ids[this.index++];
					object value = this._enclosing._enclosing.Get(this.key);
					return new _KeyValuePair_495(ekey, value);
				}

				private sealed class _KeyValuePair_495 : KeyValuePair<object, object>
				{
					public _KeyValuePair_495(object ekey, object value)
					{
						this.ekey = ekey;
						this.value = value;
					}

					public object Key
					{
						get
						{
							return ekey;
						}
					}

					public object Value
					{
						get
						{
							return value;
						}
					}

					public object SetValue(object value)
					{
						throw new NotSupportedException();
					}

					public override bool Equals(object other)
					{
						if (!(other is DictionaryEntry))
						{
							return false;
						}
						DictionaryEntry e = (DictionaryEntry)other;
						return (ekey == null ? e.Key == null : ekey.Equals(e.Key)) && (value == null ? e.Value == null : value.Equals(e.Value));
					}

					public override int GetHashCode()
					{
						return (ekey == null ? 0 : ekey.GetHashCode()) ^ (value == null ? 0 : value.GetHashCode());
					}

					public override string ToString()
					{
						return ekey + "=" + value;
					}

					private readonly object ekey;

					private readonly object value;
				}

				public void Remove()
				{
					if (this.key == null)
					{
						throw new InvalidOperationException();
					}
					Sharpen.Collections.Remove(this._enclosing._enclosing, this.key);
					this.key = null;
				}

				private readonly EntrySet _enclosing;
			}

			public override int Count
			{
				get
				{
					return this._enclosing.Size();
				}
			}

			internal EntrySet(NativeObject _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly NativeObject _enclosing;
		}

		internal class KeySet : AbstractSet<object>
		{
			public override bool Contains(object key)
			{
				return this._enclosing.ContainsKey(key);
			}

			public override IEnumerator<object> GetEnumerator()
			{
				return new _IEnumerator_553(this);
			}

			private sealed class _IEnumerator_553 : IEnumerator<object>
			{
				public _IEnumerator_553(KeySet _enclosing)
				{
					this._enclosing = _enclosing;
					this.ids = this._enclosing._enclosing.GetIds();
					this.index = 0;
				}

				internal object[] ids;

				internal object key;

				internal int index;

				public bool HasNext()
				{
					return this.index < this.ids.Length;
				}

				public object Next()
				{
					try
					{
						return (this.key = this.ids[this.index++]);
					}
					catch (IndexOutOfRangeException)
					{
						this.key = null;
						throw new NoSuchElementException();
					}
				}

				public void Remove()
				{
					if (this.key == null)
					{
						throw new InvalidOperationException();
					}
					Sharpen.Collections.Remove(this._enclosing._enclosing, this.key);
					this.key = null;
				}

				private readonly KeySet _enclosing;
			}

			public override int Count
			{
				get
				{
					return this._enclosing.Size();
				}
			}

			internal KeySet(NativeObject _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly NativeObject _enclosing;
		}

		internal class ValueCollection : AbstractCollection<object>
		{
			public override IEnumerator<object> GetEnumerator()
			{
				return new _IEnumerator_591(this);
			}

			private sealed class _IEnumerator_591 : IEnumerator<object>
			{
				public _IEnumerator_591(ValueCollection _enclosing)
				{
					this._enclosing = _enclosing;
					this.ids = this._enclosing._enclosing.GetIds();
					this.index = 0;
				}

				internal object[] ids;

				internal object key;

				internal int index;

				public bool HasNext()
				{
					return this.index < this.ids.Length;
				}

				public object Next()
				{
					return this._enclosing._enclosing.Get((this.key = this.ids[this.index++]));
				}

				public void Remove()
				{
					if (this.key == null)
					{
						throw new InvalidOperationException();
					}
					Sharpen.Collections.Remove(this._enclosing._enclosing, this.key);
					this.key = null;
				}

				private readonly ValueCollection _enclosing;
			}

			public override int Count
			{
				get
				{
					return this._enclosing.Size();
				}
			}

			internal ValueCollection(NativeObject _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly NativeObject _enclosing;
		}

		// #string_id_map#
		protected internal override int FindPrototypeId(string s)
		{
			int id;
			// #generated# Last update: 2007-05-09 08:15:55 EDT
			id = 0;
			string X = null;
			int c;
			switch (s.Length)
			{
				case 7:
				{
					X = "valueOf";
					id = Id_valueOf;
					goto L_break;
				}

				case 8:
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
					goto L_break;
				}

				case 11:
				{
					X = "constructor";
					id = Id_constructor;
					goto L_break;
				}

				case 13:
				{
					X = "isPrototypeOf";
					id = Id_isPrototypeOf;
					goto L_break;
				}

				case 14:
				{
					c = s[0];
					if (c == 'h')
					{
						X = "hasOwnProperty";
						id = Id_hasOwnProperty;
					}
					else
					{
						if (c == 't')
						{
							X = "toLocaleString";
							id = Id_toLocaleString;
						}
					}
					goto L_break;
				}

				case 16:
				{
					c = s[2];
					if (c == 'd')
					{
						c = s[8];
						if (c == 'G')
						{
							X = "__defineGetter__";
							id = Id___defineGetter__;
						}
						else
						{
							if (c == 'S')
							{
								X = "__defineSetter__";
								id = Id___defineSetter__;
							}
						}
					}
					else
					{
						if (c == 'l')
						{
							c = s[8];
							if (c == 'G')
							{
								X = "__lookupGetter__";
								id = Id___lookupGetter__;
							}
							else
							{
								if (c == 'S')
								{
									X = "__lookupSetter__";
									id = Id___lookupSetter__;
								}
							}
						}
					}
					goto L_break;
				}

				case 20:
				{
					X = "propertyIsEnumerable";
					id = Id_propertyIsEnumerable;
					goto L_break;
				}
			}
L_break: ;
			if (X != null && X != s && !X.Equals(s))
			{
				id = 0;
			}
			goto L0_break;
L0_break: ;
			// #/generated#
			return id;
		}

		private const int ConstructorId_getPrototypeOf = -1;

		private const int ConstructorId_keys = -2;

		private const int ConstructorId_getOwnPropertyNames = -3;

		private const int ConstructorId_getOwnPropertyDescriptor = -4;

		private const int ConstructorId_defineProperty = -5;

		private const int ConstructorId_isExtensible = -6;

		private const int ConstructorId_preventExtensions = -7;

		private const int ConstructorId_defineProperties = -8;

		private const int ConstructorId_create = -9;

		private const int ConstructorId_isSealed = -10;

		private const int ConstructorId_isFrozen = -11;

		private const int ConstructorId_seal = -12;

		private const int ConstructorId_freeze = -13;

		private const int Id_constructor = 1;

		private const int Id_toString = 2;

		private const int Id_toLocaleString = 3;

		private const int Id_valueOf = 4;

		private const int Id_hasOwnProperty = 5;

		private const int Id_propertyIsEnumerable = 6;

		private const int Id_isPrototypeOf = 7;

		private const int Id_toSource = 8;

		private const int Id___defineGetter__ = 9;

		private const int Id___defineSetter__ = 10;

		private const int Id___lookupGetter__ = 11;

		private const int Id___lookupSetter__ = 12;

		private const int MAX_PROTOTYPE_ID = 12;
		// #/string_id_map#
	}
}
