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
using Rhino.Utils;
using Sharpen;

namespace Rhino
{
	/// <summary>This class implements the Object native object.</summary>
	/// <remarks>
	/// This class implements the Object native object.
	/// See ECMA 15.2.
	/// </remarks>
	/// <author>Norris Boyd</author>
	[Serializable]
	public class NativeObject : IdScriptableObject, IDictionary, IDictionary<object, object>
	{
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

		public IEnumerator<KeyValuePair<object, object>> GetEnumerator()
		{
			throw new NotImplementedException();
		}

		void IDictionary.Remove(object key)
		{
			throw new NotImplementedException();
		}

		public override string ToString()
		{
			return ScriptRuntime.DefaultObjectToString(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
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
					if (cx.HasFeature(LanguageFeatures.ToStringAsSource))
					{
						string s = ScriptRuntime.DefaultObjectToSource(cx, scope, thisObj, args);
						int L = s.Length;
						if (L != 0 && s[0] == '(' && s[L - 1] == ')')
						{
							// Strip () that surrounds toSource
							s = s.Substring(1, L - 2);
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
					return result;
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
								PropertyAttributes attrs = so.GetAttributes(index);
								result = ((attrs & PropertyAttributes.DONTENUM) == 0);
							}
						}
						else
						{
							result = thisObj.Has(s, thisObj);
							if (result && thisObj is ScriptableObject)
							{
								ScriptableObject so = (ScriptableObject)thisObj;
								PropertyAttributes attrs = so.GetAttributes(s);
								result = ((attrs & PropertyAttributes.DONTENUM) == 0);
							}
						}
					}
					return result;
				}

				case Id_isPrototypeOf:
				{
					bool result = false;
					if (args.Length != 0 && args[0] is Scriptable)
					{
						Scriptable v = (Scriptable)args[0];
						do
						{
							v = v.Prototype;
							if (v == thisObj)
							{
								result = true;
								break;
							}
						}
						while (v != null);
					}
					return result;
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
					var nativeArray = so as NativeArray;
					if (nativeArray != null)
					{
						nativeArray.SetDenseOnly(false);
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
						Scriptable v = so.Prototype;
						if (v == null)
						{
							break;
						}
						var o = v as ScriptableObject;
						if (o != null)
						{
							so = o;
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
					return obj.Prototype;
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
					Scriptable props = Context.ToObject(propsObj, ParentScope);
					obj.DefineOwnProperties(cx, EnsureScriptableObject(props));
					return obj;
				}

				case ConstructorId_create:
				{
					object arg = args.Length < 1 ? Undefined.instance : args[0];
					Scriptable obj = (arg == null) ? null : EnsureScriptable(arg);
					ScriptableObject newObject = new NativeObject();
					newObject.ParentScope = ParentScope;
					newObject.Prototype = obj;
					if (args.Length > 1 && args[1] != Undefined.instance)
					{
						Scriptable props = Context.ToObject(args[1], ParentScope);
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

		public virtual bool ContainsKey(object key)
		{
			var stringKey = key as string;
			if (stringKey != null)
			{
				return Has(stringKey, this);
			}
			if (key.IsNumber())
			{
				return Has(Convert.ToInt32(key), this);
			}
			return false;
		}

		public virtual bool ContainsValue(object value)
		{
			foreach (object obj in Values)
			{
				if (value == obj || value != null && value.Equals(obj))
				{
					return true;
				}
			}
			return false;
		}

		IDictionaryEnumerator IDictionary.GetEnumerator()
		{
			throw new NotImplementedException();
		}

		public virtual bool Remove(object key)
		{
			var stringKey = key as string;
			if (stringKey != null)
			{
				Delete(stringKey);
			}
			else
			{
				if (key.IsNumber())
				{
					Delete(Convert.ToInt32(key));
				}
			}
			return true;
		}

		public bool TryGetValue(object key, out object value)
		{
			if (!ContainsKey(key))
			{
				value = null;
				return false;
			}
			else
			{
				value = Get(key);
				return true;
			}
		}

		public object this[object key]
		{
			get { return Get(key); }
			set { throw new NotSupportedException(); }
		}

		public virtual ICollection<object> Keys
		{
			get { return new KeyCollection(this); }
		}

		ICollection IDictionary.Values
		{
			get { return new ValueCollection(this); }
		}

		ICollection IDictionary.Keys
		{
			get { return new KeyCollection(this); }
		}

		public virtual ICollection<object> Values
		{
			get { return new ValueCollection(this); }
		}

		public bool Contains(object key)
		{
			return ContainsKey(key);
		}

		public void Add(object key, object value)
		{
			throw new NotSupportedException();
		}

		public void Add(KeyValuePair<object, object> item)
		{
			throw new NotSupportedException();
		}

		public virtual void Clear()
		{
			throw new NotSupportedException();
		}

		public bool Contains(KeyValuePair<object, object> item)
		{
			object value;
			return TryGetValue(item.Key, out value) && value == item.Value;
		}

		public void CopyTo(KeyValuePair<object, object>[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}

		public bool Remove(KeyValuePair<object, object> item)
		{
			throw new NotImplementedException();
		}

		public void CopyTo(Array array, int index)
		{
			throw new NotImplementedException();
		}

		public int Count
		{
			get { return Size(); }
		}

		public object SyncRoot
		{
			get { return this; }
		}

		public bool IsSynchronized
		{
			get { return false; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool IsFixedSize
		{
			get { return false; }
		}

		private class KeyCollection : ICollection<object>, ICollection
		{
			void ICollection<object>.Add(object item)
			{
				throw new NotSupportedException();
			}

			void ICollection<object>.Clear()
			{
				throw new NotSupportedException();
			}

			bool ICollection<object>.Contains(object key)
			{
				return _obj.ContainsKey(key);
			}

			void ICollection<object>.CopyTo(object[] array, int arrayIndex)
			{
				throw new NotImplementedException();
			}

			bool ICollection<object>.Remove(object item)
			{
				throw new NotSupportedException();
			}

			public IEnumerator<object> GetEnumerator()
			{
				return ((IEnumerable<object>) _obj.GetIds()).GetEnumerator();
			}

			void ICollection.CopyTo(Array array, int index)
			{
				throw new NotImplementedException();
			}

			public int Count
			{
				get { return _obj.Size(); }
			}

			object ICollection.SyncRoot
			{
				get { return _obj.SyncRoot; }
			}

			bool ICollection.IsSynchronized
			{
				get { return false; }
			}

			bool ICollection<object>.IsReadOnly
			{
				get { return true; }
			}

			internal KeyCollection(NativeObject obj)
			{
				_obj = obj;
			}

			private readonly NativeObject _obj;
			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		private class ValueCollection : ICollection<object>, ICollection
		{
			public IEnumerator<object> GetEnumerator()
			{
				var ids = _obj.GetIds();
				var index = 0;
				while (index < ids.Length)
				{
					yield return _obj.Get(ids[index++]);
				}
			}

			void ICollection<object>.Add(object item)
			{
				throw new NotSupportedException();
			}

			void ICollection<object>.Clear()
			{
				throw new NotSupportedException();
			}

			bool ICollection<object>.Contains(object item)
			{
				return Enumerable.Contains(this, item);
			}

			void ICollection<object>.CopyTo(object[] array, int arrayIndex)
			{
				throw new NotImplementedException();
			}

			bool ICollection<object>.Remove(object item)
			{
				throw new NotSupportedException();
			}

			void ICollection.CopyTo(Array array, int index)
			{
				throw new NotImplementedException();
			}

			public int Count
			{
				get { return _obj.Size(); }
			}

			object ICollection.SyncRoot
			{
				get { return _obj.SyncRoot; }
			}

			bool ICollection.IsSynchronized
			{
				get { return false; }
			}

			bool ICollection<object>.IsReadOnly
			{
				get { return true; }
			}

			internal ValueCollection(NativeObject obj)
			{
				_obj = obj;
			}

			private readonly NativeObject _obj;

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
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
