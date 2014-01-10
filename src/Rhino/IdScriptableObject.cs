/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.IO;
using Rhino;
using Sharpen;

namespace Rhino
{
	/// <summary>Base class for native object implementation that uses IdFunctionObject to export its methods to script via <class-name>.prototype object.</summary>
	/// <remarks>
	/// Base class for native object implementation that uses IdFunctionObject to export its methods to script via <class-name>.prototype object.
	/// Any descendant should implement at least the following methods:
	/// findInstanceIdInfo
	/// getInstanceIdName
	/// execIdCall
	/// methodArity
	/// To define non-function properties, the descendant should override
	/// getInstanceIdValue
	/// setInstanceIdValue
	/// to get/set property value and provide its default attributes.
	/// To customize initialization of constructor and prototype objects, descendant
	/// may override scopeInit or fillConstructorProperties methods.
	/// </remarks>
	[System.Serializable]
	public abstract class IdScriptableObject : ScriptableObject, IdFunctionCall
	{
		[System.NonSerialized]
		private IdScriptableObject.PrototypeValues prototypeValues;

		[System.Serializable]
		private sealed class PrototypeValues
		{
			private const int NAME_SLOT = 1;

			private const int SLOT_SPAN = 2;

			private IdScriptableObject obj;

			private int maxId;

			private object[] valueArray;

			private PropertyAttributes[] attributeArray;

			internal int constructorId;

			private IdFunctionObject constructor;

			private PropertyAttributes constructorAttrs;

			internal PrototypeValues(IdScriptableObject obj, int maxId)
			{
				// The following helps to avoid creation of valueArray during runtime
				// initialization for common case of "constructor" property
				if (obj == null)
				{
					throw new ArgumentException();
				}
				if (maxId < 1)
				{
					throw new ArgumentException();
				}
				this.obj = obj;
				this.maxId = maxId;
			}

			internal int GetMaxId()
			{
				return maxId;
			}

			internal void InitValue(int id, string name, object value, PropertyAttributes attributes)
			{
				if (!(1 <= id && id <= maxId))
				{
					throw new ArgumentException();
				}
				if (name == null)
				{
					throw new ArgumentException();
				}
				if (value == ScriptableConstants.NOT_FOUND)
				{
					throw new ArgumentException();
				}
				ScriptableObject.CheckValidAttributes(attributes);
				if (obj.FindPrototypeId(name) != id)
				{
					throw new ArgumentException(name);
				}
				if (id == constructorId)
				{
					if (!(value is IdFunctionObject))
					{
						throw new ArgumentException("consructor should be initialized with IdFunctionObject");
					}
					constructor = (IdFunctionObject)value;
					constructorAttrs = attributes;
					return;
				}
				InitSlot(id, name, value, attributes);
			}

			private void InitSlot(int id, string name, object value, PropertyAttributes attributes)
			{
				object[] array = valueArray;
				if (array == null)
				{
					throw new InvalidOperationException();
				}
				if (value == null)
				{
					value = UniqueTag.NULL_VALUE;
				}
				int index = (id - 1) * SLOT_SPAN;
				lock (this)
				{
					object value2 = array[index];
					if (value2 == null)
					{
						array[index] = value;
						array[index + NAME_SLOT] = name;
						attributeArray[id - 1] = attributes;
					}
					else
					{
						if (!name.Equals(array[index + NAME_SLOT]))
						{
							throw new InvalidOperationException();
						}
					}
				}
			}

			internal IdFunctionObject CreatePrecachedConstructor()
			{
				if (constructorId != 0)
				{
					throw new InvalidOperationException();
				}
				constructorId = obj.FindPrototypeId("constructor");
				if (constructorId == 0)
				{
					throw new InvalidOperationException("No id for constructor property");
				}
				obj.InitPrototypeId(constructorId);
				if (constructor == null)
				{
					throw new InvalidOperationException(obj.GetType().FullName + ".initPrototypeId() did not " + "initialize id=" + constructorId);
				}
				constructor.InitFunction(obj.GetClassName(), ScriptableObject.GetTopLevelScope(obj));
				constructor.MarkAsConstructor(obj);
				return constructor;
			}

			internal int FindId(string name)
			{
				return obj.FindPrototypeId(name);
			}

			internal bool Has(int id)
			{
				object[] array = valueArray;
				if (array == null)
				{
					// Not yet initialized, assume all exists
					return true;
				}
				int valueSlot = (id - 1) * SLOT_SPAN;
				object value = array[valueSlot];
				if (value == null)
				{
					// The particular entry has not been yet initialized
					return true;
				}
				return value != ScriptableConstants.NOT_FOUND;
			}

			internal object Get(int id)
			{
				object value = EnsureId(id);
				if (value == UniqueTag.NULL_VALUE)
				{
					value = null;
				}
				return value;
			}

			internal void Set(int id, Scriptable start, object value)
			{
				if (value == ScriptableConstants.NOT_FOUND)
				{
					throw new ArgumentException();
				}
				EnsureId(id);
				PropertyAttributes attr = attributeArray[id - 1];
				if ((attr & PropertyAttributes.READONLY) == 0)
				{
					if (start == obj)
					{
						if (value == null)
						{
							value = UniqueTag.NULL_VALUE;
						}
						int valueSlot = (id - 1) * SLOT_SPAN;
						lock (this)
						{
							valueArray[valueSlot] = value;
						}
					}
					else
					{
						int nameSlot = (id - 1) * SLOT_SPAN + NAME_SLOT;
						string name = (string)valueArray[nameSlot];
						start.Put(name, start, value);
					}
				}
			}

			internal void Delete(int id)
			{
				EnsureId(id);
				PropertyAttributes attr = attributeArray[id - 1];
				if ((attr & PropertyAttributes.PERMANENT) == 0)
				{
					int valueSlot = (id - 1) * SLOT_SPAN;
					lock (this)
					{
						valueArray[valueSlot] = ScriptableConstants.NOT_FOUND;
						attributeArray[id - 1] = PropertyAttributes.EMPTY;
					}
				}
			}

			internal PropertyAttributes GetAttributes(int id)
			{
				EnsureId(id);
				return attributeArray[id - 1];
			}

			internal void SetAttributes(int id, PropertyAttributes attributes)
			{
				ScriptableObject.CheckValidAttributes(attributes);
				EnsureId(id);
				lock (this)
				{
					attributeArray[id - 1] = attributes;
				}
			}

			internal object[] GetNames(bool getAll, object[] extraEntries)
			{
				object[] names = null;
				int count = 0;
				for (int id = 1; id <= maxId; ++id)
				{
					object value = EnsureId(id);
					if (getAll || (attributeArray[id - 1] & PropertyAttributes.DONTENUM) == 0)
					{
						if (value != ScriptableConstants.NOT_FOUND)
						{
							int nameSlot = (id - 1) * SLOT_SPAN + NAME_SLOT;
							string name = (string)valueArray[nameSlot];
							if (names == null)
							{
								names = new object[maxId];
							}
							names[count++] = name;
						}
					}
				}
				if (count == 0)
				{
					return extraEntries;
				}
				else
				{
					if (extraEntries == null || extraEntries.Length == 0)
					{
						if (count != names.Length)
						{
							object[] tmp = new object[count];
							System.Array.Copy(names, 0, tmp, 0, count);
							names = tmp;
						}
						return names;
					}
					else
					{
						int extra = extraEntries.Length;
						object[] tmp = new object[extra + count];
						System.Array.Copy(extraEntries, 0, tmp, 0, extra);
						System.Array.Copy(names, 0, tmp, extra, count);
						return tmp;
					}
				}
			}

			private object EnsureId(int id)
			{
				object[] array = valueArray;
				if (array == null)
				{
					lock (this)
					{
						array = valueArray;
						if (array == null)
						{
							array = new object[maxId * SLOT_SPAN];
							valueArray = array;
							attributeArray = new PropertyAttributes[maxId];
						}
					}
				}
				int valueSlot = (id - 1) * SLOT_SPAN;
				object value = array[valueSlot];
				if (value == null)
				{
					if (id == constructorId)
					{
						InitSlot(constructorId, "constructor", constructor, constructorAttrs);
						constructor = null;
					}
					else
					{
						// no need to refer it any longer
						obj.InitPrototypeId(id);
					}
					value = array[valueSlot];
					if (value == null)
					{
						throw new InvalidOperationException(obj.GetType().FullName + ".initPrototypeId(int id) " + "did not initialize id=" + id);
					}
				}
				return value;
			}
		}

		public IdScriptableObject()
		{
		}

		public IdScriptableObject(Scriptable scope, Scriptable prototype) : base(scope, prototype)
		{
		}

		protected internal object DefaultGet(string name)
		{
			return base.Get(name, this);
		}

		protected internal void DefaultPut(string name, object value)
		{
			base.Put(name, this, value);
		}

		public override bool Has(string name, Scriptable start)
		{
			InstanceIdInfo info = FindInstanceIdInfo(name);
			if (info != null)
			{
				PropertyAttributes attr = info.Attributes;
				if ((attr & PropertyAttributes.PERMANENT) != 0)
				{
					return true;
				}
				int id = info.Id;
				return ScriptableConstants.NOT_FOUND != GetInstanceIdValue(id);
			}
			if (prototypeValues != null)
			{
				int id = prototypeValues.FindId(name);
				if (id != 0)
				{
					return prototypeValues.Has(id);
				}
			}
			return base.Has(name, start);
		}

		public override object Get(string name, Scriptable start)
		{
			// Check for slot first for performance. This is a very hot code
			// path that should be further optimized.
			object value = base.Get(name, start);
			if (value != ScriptableConstants.NOT_FOUND)
			{
				return value;
			}
			InstanceIdInfo info = FindInstanceIdInfo(name);
			if (info != null)
			{
				int id = info.Id;
				value = GetInstanceIdValue(id);
				if (value != ScriptableConstants.NOT_FOUND)
				{
					return value;
				}
			}
			if (prototypeValues != null)
			{
				int id = prototypeValues.FindId(name);
				if (id != 0)
				{
					value = prototypeValues.Get(id);
					if (value != ScriptableConstants.NOT_FOUND)
					{
						return value;
					}
				}
			}
			return ScriptableConstants.NOT_FOUND;
		}

		public override void Put(string name, Scriptable start, object value)
		{
			InstanceIdInfo info = FindInstanceIdInfo(name);
			if (info != null)
			{
				if (start == this && IsSealed())
				{
					throw Context.ReportRuntimeError1("msg.modify.sealed", name);
				}
				PropertyAttributes attr = info.Attributes;
				if ((attr & PropertyAttributes.READONLY) == 0)
				{
					if (start == this)
					{
						int id = info.Id;
						SetInstanceIdValue(id, value);
					}
					else
					{
						start.Put(name, start, value);
					}
				}
				return;
			}
			if (prototypeValues != null)
			{
				int id = prototypeValues.FindId(name);
				if (id != 0)
				{
					if (start == this && IsSealed())
					{
						throw Context.ReportRuntimeError1("msg.modify.sealed", name);
					}
					prototypeValues.Set(id, start, value);
					return;
				}
			}
			base.Put(name, start, value);
		}

		public override void Delete(string name)
		{
			InstanceIdInfo info = FindInstanceIdInfo(name);
			if (info != null)
			{
				// Let the super class to throw exceptions for sealed objects
				if (!IsSealed())
				{
					PropertyAttributes attr = info.Attributes;
					if ((attr & PropertyAttributes.PERMANENT) == 0)
					{
						int id = info.Id;
						SetInstanceIdValue(id, ScriptableConstants.NOT_FOUND);
					}
					return;
				}
			}
			if (prototypeValues != null)
			{
				int id = prototypeValues.FindId(name);
				if (id != 0)
				{
					if (!IsSealed())
					{
						prototypeValues.Delete(id);
					}
					return;
				}
			}
			base.Delete(name);
		}

		public override PropertyAttributes GetAttributes(string name)
		{
			InstanceIdInfo info = FindInstanceIdInfo(name);
			if (info != null)
			{
				PropertyAttributes attr = info.Attributes;
				return attr;
			}
			if (prototypeValues != null)
			{
				int id = prototypeValues.FindId(name);
				if (id != 0)
				{
					return prototypeValues.GetAttributes(id);
				}
			}
			return base.GetAttributes(name);
		}

		public override void SetAttributes(string name, PropertyAttributes attributes)
		{
			ScriptableObject.CheckValidAttributes(attributes);
			InstanceIdInfo info = FindInstanceIdInfo(name);
			if (info != null)
			{
				int id = info.Id;
				PropertyAttributes currentAttributes = info.Attributes;
				if (attributes != currentAttributes)
				{
					SetInstanceIdAttributes(id, attributes);
				}
				return;
			}
			if (prototypeValues != null)
			{
				int id = prototypeValues.FindId(name);
				if (id != 0)
				{
					prototypeValues.SetAttributes(id, attributes);
					return;
				}
			}
			base.SetAttributes(name, attributes);
		}

		internal override object[] GetIds(bool getAll)
		{
			object[] result = base.GetIds(getAll);
			if (prototypeValues != null)
			{
				result = prototypeValues.GetNames(getAll, result);
			}
			int maxInstanceId = GetMaxInstanceId();
			if (maxInstanceId != 0)
			{
				object[] ids = null;
				int count = 0;
				for (int id = maxInstanceId; id != 0; --id)
				{
					string name = GetInstanceIdName(id);
					InstanceIdInfo info = FindInstanceIdInfo(name);
					if (info != null)
					{
						PropertyAttributes attr = info.Attributes;
						if ((attr & PropertyAttributes.PERMANENT) == 0)
						{
							if (ScriptableConstants.NOT_FOUND == GetInstanceIdValue(id))
							{
								continue;
							}
						}
						if (getAll || (attr & PropertyAttributes.DONTENUM) == 0)
						{
							if (count == 0)
							{
								// Need extra room for no more then [1..id] names
								ids = new object[id];
							}
							ids[count++] = name;
						}
					}
				}
				if (count != 0)
				{
					if (result.Length == 0 && ids.Length == count)
					{
						result = ids;
					}
					else
					{
						object[] tmp = new object[result.Length + count];
						System.Array.Copy(result, 0, tmp, 0, result.Length);
						System.Array.Copy(ids, 0, tmp, result.Length, count);
						result = tmp;
					}
				}
			}
			return result;
		}

		/// <summary>Get maximum id findInstanceIdInfo can generate.</summary>
		/// <remarks>Get maximum id findInstanceIdInfo can generate.</remarks>
		protected internal virtual int GetMaxInstanceId()
		{
			return 0;
		}

		protected internal static InstanceIdInfo InstanceIdInfo(PropertyAttributes attributes, int id)
		{
			return new InstanceIdInfo(id, attributes);
		}

		/// <summary>Map name to id of instance property.</summary>
		/// <remarks>
		/// Map name to id of instance property.
		/// Should return 0 if not found or the result of
		/// <see cref="InstanceIdInfo(int, int)">InstanceIdInfo(int, int)</see>
		/// .
		/// </remarks>
		protected internal virtual InstanceIdInfo FindInstanceIdInfo(string name)
		{
			return null;
		}

		/// <summary>Map id back to property name it defines.</summary>
		/// <remarks>Map id back to property name it defines.</remarks>
		protected internal virtual string GetInstanceIdName(int id)
		{
			throw new ArgumentException(id.ToString());
		}

		/// <summary>Get id value.</summary>
		/// <remarks>
		/// Get id value.
		/// If id value is constant, descendant can call cacheIdValue to store
		/// value in the permanent cache.
		/// Default implementation creates IdFunctionObject instance for given id
		/// and cache its value
		/// </remarks>
		protected internal virtual object GetInstanceIdValue(int id)
		{
			throw new InvalidOperationException(id.ToString());
		}

		/// <summary>Set or delete id value.</summary>
		/// <remarks>
		/// Set or delete id value. If value == NOT_FOUND , the implementation
		/// should make sure that the following getInstanceIdValue return NOT_FOUND.
		/// </remarks>
		protected internal virtual void SetInstanceIdValue(int id, object value)
		{
			throw new InvalidOperationException(id.ToString());
		}

		/// <summary>Update the attributes of the given instance property.</summary>
		/// <remarks>
		/// Update the attributes of the given instance property. Classes which
		/// want to support changing property attributes via Object.defineProperty
		/// must override this method. The default implementation throws
		/// InternalError.
		/// </remarks>
		/// <param name="id">the instance property id</param>
		/// <param name="attr">the new attribute bitset</param>
		protected internal virtual void SetInstanceIdAttributes(int id, PropertyAttributes attr)
		{
			throw ScriptRuntime.ConstructError("InternalError", "Changing attributes not supported for " + GetClassName() + " " + GetInstanceIdName(id) + " property");
		}

		/// <summary>
		/// 'thisObj' will be null if invoked as constructor, in which case
		/// instance of Scriptable should be returned.
		/// </summary>
		/// <remarks>
		/// 'thisObj' will be null if invoked as constructor, in which case
		/// instance of Scriptable should be returned.
		/// </remarks>
		public virtual object ExecIdCall(IdFunctionObject f, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			throw f.Unknown();
		}

		public IdFunctionObject ExportAsJSClass(int maxPrototypeId, Scriptable scope, bool @sealed)
		{
			// Set scope and prototype unless this is top level scope itself
			if (scope != this && scope != null)
			{
				ParentScope = scope;
				Prototype = GetObjectPrototype(scope);
			}
			ActivatePrototypeMap(maxPrototypeId);
			IdFunctionObject ctor = prototypeValues.CreatePrecachedConstructor();
			if (@sealed)
			{
				SealObject();
			}
			FillConstructorProperties(ctor);
			if (@sealed)
			{
				ctor.SealObject();
			}
			ctor.ExportAsScopeProperty();
			return ctor;
		}

		public bool HasPrototypeMap()
		{
			return prototypeValues != null;
		}

		public void ActivatePrototypeMap(int maxPrototypeId)
		{
			IdScriptableObject.PrototypeValues values = new IdScriptableObject.PrototypeValues(this, maxPrototypeId);
			lock (this)
			{
				if (prototypeValues != null)
				{
					throw new InvalidOperationException();
				}
				prototypeValues = values;
			}
		}

		public void InitPrototypeMethod(object tag, int id, string name, int arity)
		{
			Scriptable scope = ScriptableObject.GetTopLevelScope(this);
			IdFunctionObject f = NewIdFunction(tag, id, name, arity, scope);
			prototypeValues.InitValue(id, name, f, PropertyAttributes.DONTENUM);
		}

		public void InitPrototypeConstructor(IdFunctionObject f)
		{
			int id = prototypeValues.constructorId;
			if (id == 0)
			{
				throw new InvalidOperationException();
			}
			if (f.MethodId() != id)
			{
				throw new ArgumentException();
			}
			if (IsSealed())
			{
				f.SealObject();
			}
			prototypeValues.InitValue(id, "constructor", f, PropertyAttributes.DONTENUM);
		}

		public void InitPrototypeValue(int id, string name, object value, PropertyAttributes attributes)
		{
			prototypeValues.InitValue(id, name, value, attributes);
		}

		protected internal virtual void InitPrototypeId(int id)
		{
			throw new InvalidOperationException(id.ToString());
		}

		protected internal virtual int FindPrototypeId(string name)
		{
			throw new InvalidOperationException(name);
		}

		protected internal virtual void FillConstructorProperties(IdFunctionObject ctor)
		{
		}

		protected internal virtual void AddIdFunctionProperty(Scriptable obj, object tag, int id, string name, int arity)
		{
			Scriptable scope = ScriptableObject.GetTopLevelScope(obj);
			IdFunctionObject f = NewIdFunction(tag, id, name, arity, scope);
			f.AddAsProperty(obj);
		}

		/// <summary>
		/// Utility method to construct type error to indicate incompatible call
		/// when converting script thisObj to a particular type is not possible.
		/// </summary>
		/// <remarks>
		/// Utility method to construct type error to indicate incompatible call
		/// when converting script thisObj to a particular type is not possible.
		/// Possible usage would be to have a private function like realThis:
		/// <pre>
		/// private static NativeSomething realThis(Scriptable thisObj,
		/// IdFunctionObject f)
		/// {
		/// if (!(thisObj instanceof NativeSomething))
		/// throw incompatibleCallError(f);
		/// return (NativeSomething)thisObj;
		/// }
		/// </pre>
		/// Note that although such function can be implemented universally via
		/// java.lang.Class.isInstance(), it would be much more slower.
		/// </remarks>
		/// <param name="f">
		/// function that is attempting to convert 'this'
		/// object.
		/// </param>
		/// <returns>
		/// Scriptable object suitable for a check by the instanceof
		/// operator.
		/// </returns>
		/// <exception cref="System.Exception">if no more instanceof target can be found</exception>
		protected internal static EcmaError IncompatibleCallError(IdFunctionObject f)
		{
			throw ScriptRuntime.TypeError1("msg.incompat.call", f.GetFunctionName());
		}

		private IdFunctionObject NewIdFunction(object tag, int id, string name, int arity, Scriptable scope)
		{
			IdFunctionObject f = new IdFunctionObject(this, tag, id, name, arity, scope);
			if (IsSealed())
			{
				f.SealObject();
			}
			return f;
		}

		public override void DefineOwnProperty(Context cx, object key, ScriptableObject desc)
		{
			if (key is string)
			{
				string name = (string)key;
				InstanceIdInfo info = FindInstanceIdInfo(name);
				if (info != null)
				{
					int id = info.Id;
					if (IsAccessorDescriptor(desc))
					{
						Delete(id);
					}
					else
					{
						// it will be replaced with a slot
						CheckPropertyDefinition(desc);
						ScriptableObject current = GetOwnPropertyDescriptor(cx, key);
						CheckPropertyChange(name, current, desc);
						PropertyAttributes attr = info.Attributes;
						object value = GetProperty(desc, "value");
						if (value != ScriptableConstants.NOT_FOUND && (attr & PropertyAttributes.READONLY) == 0)
						{
							object currentValue = GetInstanceIdValue(id);
							if (!SameValue(value, currentValue))
							{
								SetInstanceIdValue(id, value);
							}
						}
						SetAttributes(name, (PropertyAttributes) ApplyDescriptorToAttributeBitset(attr, desc));
						return;
					}
				}
				if (prototypeValues != null)
				{
					int id = prototypeValues.FindId(name);
					if (id != 0)
					{
						if (IsAccessorDescriptor(desc))
						{
							prototypeValues.Delete(id);
						}
						else
						{
							// it will be replaced with a slot
							CheckPropertyDefinition(desc);
							ScriptableObject current = GetOwnPropertyDescriptor(cx, key);
							CheckPropertyChange(name, current, desc);
							PropertyAttributes attr = prototypeValues.GetAttributes(id);
							object value = GetProperty(desc, "value");
							if (value != ScriptableConstants.NOT_FOUND && (attr & PropertyAttributes.READONLY) == 0)
							{
								object currentValue = prototypeValues.Get(id);
								if (!SameValue(value, currentValue))
								{
									prototypeValues.Set(id, this, value);
								}
							}
							prototypeValues.SetAttributes(id, ApplyDescriptorToAttributeBitset(attr, desc));
							return;
						}
					}
				}
			}
			base.DefineOwnProperty(cx, key, desc);
		}

		protected internal override ScriptableObject GetOwnPropertyDescriptor(Context cx, object id)
		{
			ScriptableObject desc = base.GetOwnPropertyDescriptor(cx, id);
			if (desc == null && id is string)
			{
				desc = GetBuiltInDescriptor((string)id);
			}
			return desc;
		}

		private ScriptableObject GetBuiltInDescriptor(string name)
		{
			object value;
			PropertyAttributes attr;
			Scriptable scope = ParentScope;
			if (scope == null)
			{
				scope = this;
			}
			InstanceIdInfo info = FindInstanceIdInfo(name);
			if (info != null)
			{
				int id = info.Id;
				value = GetInstanceIdValue(id);
				attr = info.Attributes;
				return BuildDataDescriptor(scope, value, attr);
			}
			if (prototypeValues != null)
			{
				int id = prototypeValues.FindId(name);
				if (id != 0)
				{
					value = prototypeValues.Get(id);
					attr = prototypeValues.GetAttributes(id);
					return BuildDataDescriptor(scope, value, attr);
				}
			}
			return null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.TypeLoadException"></exception>
		private void ReadObject(ObjectInputStream stream)
		{
			stream.DefaultReadObject();
			int maxPrototypeId = stream.ReadInt();
			if (maxPrototypeId != 0)
			{
				ActivatePrototypeMap(maxPrototypeId);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteObject(ObjectOutputStream stream)
		{
			stream.DefaultWriteObject();
			int maxPrototypeId = 0;
			if (prototypeValues != null)
			{
				maxPrototypeId = prototypeValues.GetMaxId();
			}
			stream.WriteInt(maxPrototypeId);
		}
	}
}
