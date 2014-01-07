/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Rhino;
using Rhino.Annotations;
using Rhino.Debug;
using Rhino.Utils;
using Sharpen;

namespace Rhino
{
	/// <summary>This is the default implementation of the Scriptable interface.</summary>
	/// <remarks>
	/// This is the default implementation of the Scriptable interface. This
	/// class provides convenient default behavior that makes it easier to
	/// define host objects.
	/// <p>
	/// Various properties and methods of JavaScript objects can be conveniently
	/// defined using methods of ScriptableObject.
	/// <p>
	/// Classes extending ScriptableObject must define the getClassName method.
	/// </remarks>
	/// <seealso cref="Scriptable">Scriptable</seealso>
	/// <author>Norris Boyd</author>
	[Serializable]
	public abstract class ScriptableObject : Scriptable, DebuggableObject, ConstProperties
	{
		internal const long serialVersionUID = 2829861078851942586L;

		/// <summary>The empty property attribute.</summary>
		/// <remarks>
		/// The empty property attribute.
		/// Used by getAttributes() and setAttributes().
		/// </remarks>
		/// <seealso cref="GetAttributes(string)">GetAttributes(string)</seealso>
		/// <seealso cref="SetAttributes(string, int)">SetAttributes(string, int)</seealso>
		public const int EMPTY = unchecked((int)(0x00));

		/// <summary>Property attribute indicating assignment to this property is ignored.</summary>
		/// <remarks>Property attribute indicating assignment to this property is ignored.</remarks>
		/// <seealso cref="Put(string, Scriptable, object)">Put(string, Scriptable, object)</seealso>
		/// <seealso cref="GetAttributes(string)">GetAttributes(string)</seealso>
		/// <seealso cref="SetAttributes(string, int)">SetAttributes(string, int)</seealso>
		public const int READONLY = unchecked((int)(0x01));

		/// <summary>Property attribute indicating property is not enumerated.</summary>
		/// <remarks>
		/// Property attribute indicating property is not enumerated.
		/// Only enumerated properties will be returned by getIds().
		/// </remarks>
		/// <seealso cref="GetIds()">GetIds()</seealso>
		/// <seealso cref="GetAttributes(string)">GetAttributes(string)</seealso>
		/// <seealso cref="SetAttributes(string, int)">SetAttributes(string, int)</seealso>
		public const int DONTENUM = unchecked((int)(0x02));

		/// <summary>Property attribute indicating property cannot be deleted.</summary>
		/// <remarks>Property attribute indicating property cannot be deleted.</remarks>
		/// <seealso cref="Delete(string)">Delete(string)</seealso>
		/// <seealso cref="GetAttributes(string)">GetAttributes(string)</seealso>
		/// <seealso cref="SetAttributes(string, int)">SetAttributes(string, int)</seealso>
		public const int PERMANENT = unchecked((int)(0x04));

		/// <summary>
		/// Property attribute indicating that this is a const property that has not
		/// been assigned yet.
		/// </summary>
		/// <remarks>
		/// Property attribute indicating that this is a const property that has not
		/// been assigned yet.  The first 'const' assignment to the property will
		/// clear this bit.
		/// </remarks>
		public const int UNINITIALIZED_CONST = unchecked((int)(0x08));

		public const int CONST = PERMANENT | READONLY | UNINITIALIZED_CONST;

		/// <summary>The prototype of this object.</summary>
		/// <remarks>The prototype of this object.</remarks>
		private Scriptable prototypeObject;

		/// <summary>The parent scope of this object.</summary>
		/// <remarks>The parent scope of this object.</remarks>
		private Scriptable parentScopeObject;

		[NonSerialized]
		private Slot[] slots;

		private int count;

		[NonSerialized]
		private Slot firstAdded;

		[NonSerialized]
		private Slot lastAdded;

		private volatile IDictionary<object, object> associatedValues;

		private const int SLOT_QUERY = 1;

		private const int SLOT_MODIFY = 2;

		private const int SLOT_MODIFY_CONST = 3;

		private const int SLOT_MODIFY_GETTER_SETTER = 4;

		private const int SLOT_CONVERT_ACCESSOR_TO_DATA = 5;

		private const int INITIAL_SLOT_SIZE = 4;

		private bool isExtensible = true;

		[Serializable]
		public class Slot
		{
			private const long serialVersionUID = -6090581677123995491L;

			internal string name;

			internal int indexOrHash;

			internal volatile short attributes;

			[NonSerialized]
			internal volatile bool wasDeleted;

			internal volatile object value;

			[NonSerialized]
			internal Slot next;

			[NonSerialized]
			internal volatile Slot orderedNext;

			internal Slot(string name, int indexOrHash, int attributes)
			{
				// API class
				// If count >= 0, it gives number of keys or if count < 0,
				// it indicates sealed object where ~count gives number of keys
				// gateways into the definition-order linked list of slots
				// initial slot array size, must be a power of 2
				// This can change due to caching
				// next in hash table bucket
				// next in linked list
				this.name = name;
				this.indexOrHash = indexOrHash;
				this.attributes = (short)attributes;
			}

			/// <exception cref="System.IO.IOException"></exception>
			/// <exception cref="System.TypeLoadException"></exception>
			private void ReadObject(ObjectInputStream @in)
			{
				@in.DefaultReadObject();
				if (name != null)
				{
					indexOrHash = name.GetHashCode();
				}
			}

			internal virtual bool SetValue(object value, Scriptable owner, Scriptable start)
			{
				if ((attributes & READONLY) != 0)
				{
					return true;
				}
				if (owner == start)
				{
					this.value = value;
					return true;
				}
				else
				{
					return false;
				}
			}

			internal virtual object GetValue(Scriptable start)
			{
				return value;
			}

			internal virtual int GetAttributes()
			{
				return attributes;
			}

			internal virtual void SetAttributes(int value)
			{
				lock (this)
				{
					CheckValidAttributes(value);
					attributes = (short)value;
				}
			}

			internal virtual void MarkDeleted()
			{
				wasDeleted = true;
				value = null;
				name = null;
			}

			internal virtual ScriptableObject GetPropertyDescriptor(Context cx, Scriptable scope)
			{
				return BuildDataDescriptor(scope, value, attributes);
			}
		}

		protected internal static ScriptableObject BuildDataDescriptor(Scriptable scope, object value, int attributes)
		{
			ScriptableObject desc = new NativeObject();
			ScriptRuntime.SetBuiltinProtoAndParent(desc, scope, TopLevel.Builtins.Object);
			desc.DefineProperty("value", value, EMPTY);
			desc.DefineProperty("writable", (attributes & READONLY) == 0, EMPTY);
			desc.DefineProperty("enumerable", (attributes & DONTENUM) == 0, EMPTY);
			desc.DefineProperty("configurable", (attributes & PERMANENT) == 0, EMPTY);
			return desc;
		}

		[Serializable]
		private sealed class GetterSlot : Slot
		{
			internal const long serialVersionUID = -4900574849788797588L;

			internal object getter;

			internal object setter;

			internal GetterSlot(string name, int indexOrHash, int attributes) : base(name, indexOrHash, attributes)
			{
			}

			internal override ScriptableObject GetPropertyDescriptor(Context cx, Scriptable scope)
			{
				int attr = GetAttributes();
				ScriptableObject desc = new NativeObject();
				ScriptRuntime.SetBuiltinProtoAndParent(desc, scope, TopLevel.Builtins.Object);
				desc.DefineProperty("enumerable", (attr & DONTENUM) == 0, EMPTY);
				desc.DefineProperty("configurable", (attr & PERMANENT) == 0, EMPTY);
				if (getter != null)
				{
					desc.DefineProperty("get", getter, EMPTY);
				}
				if (setter != null)
				{
					desc.DefineProperty("set", setter, EMPTY);
				}
				return desc;
			}

			internal override bool SetValue(object value, Scriptable owner, Scriptable start)
			{
				if (setter == null)
				{
					if (getter != null)
					{
						if (Context.GetContext().HasFeature(LanguageFeatures.StrictMode))
						{
							// Based on TC39 ES3.1 Draft of 9-Feb-2009, 8.12.4, step 2,
							// we should throw a TypeError in this case.
							throw ScriptRuntime.TypeError1("msg.set.prop.no.setter", name);
						}
						// Assignment to a property with only a getter defined. The
						// assignment is ignored. See bug 478047.
						return true;
					}
				}
				else
				{
					Context cx = Context.GetContext();
					if (setter is MemberBox)
					{
						MemberBox nativeSetter = (MemberBox)setter;
						Type[] pTypes = nativeSetter.argTypes;
						// XXX: cache tag since it is already calculated in
						// defineProperty ?
						Type valueType = pTypes[pTypes.Length - 1];
						int tag = FunctionObject.GetTypeTag(valueType);
						object actualArg = FunctionObject.ConvertArg(cx, start, value, tag);
						object setterThis;
						object[] args;
						if (nativeSetter.delegateTo == null)
						{
							setterThis = start;
							args = new object[] { actualArg };
						}
						else
						{
							setterThis = nativeSetter.delegateTo;
							args = new object[] { start, actualArg };
						}
						nativeSetter.Invoke(setterThis, args);
					}
					else
					{
						if (setter is Function)
						{
							Function f = (Function)setter;
							f.Call(cx, f.GetParentScope(), start, new object[] { value });
						}
					}
					return true;
				}
				return base.SetValue(value, owner, start);
			}

			internal override object GetValue(Scriptable start)
			{
				if (getter != null)
				{
					if (getter is MemberBox)
					{
						MemberBox nativeGetter = (MemberBox)getter;
						object getterThis;
						object[] args;
						if (nativeGetter.delegateTo == null)
						{
							getterThis = start;
							args = ScriptRuntime.emptyArgs;
						}
						else
						{
							getterThis = nativeGetter.delegateTo;
							args = new object[] { start };
						}
						return nativeGetter.Invoke(getterThis, args);
					}
					else
					{
						if (getter is Function)
						{
							Function f = (Function)getter;
							Context cx = Context.GetContext();
							return f.Call(cx, f.GetParentScope(), start, ScriptRuntime.emptyArgs);
						}
					}
				}
				object val = value;
				if (val is LazilyLoadedCtor)
				{
					LazilyLoadedCtor initializer = (LazilyLoadedCtor)val;
					try
					{
						initializer.Init();
					}
					finally
					{
						value = val = initializer.GetValue();
					}
				}
				return val;
			}

			internal override void MarkDeleted()
			{
				base.MarkDeleted();
				getter = null;
				setter = null;
			}
		}

		/// <summary>
		/// A wrapper around a slot that allows the slot to be used in a new slot
		/// table while keeping it functioning in its old slot table/linked list
		/// context.
		/// </summary>
		/// <remarks>
		/// A wrapper around a slot that allows the slot to be used in a new slot
		/// table while keeping it functioning in its old slot table/linked list
		/// context. This is used when linked slots are copied to a new slot table.
		/// In a multi-threaded environment, these slots may still be accessed
		/// through their old slot table. See bug 688458.
		/// </remarks>
		[Serializable]
		private class RelinkedSlot : Slot
		{
			internal readonly Slot slot;

			internal RelinkedSlot(Slot slot) : base(slot.name, slot.indexOrHash, slot.attributes)
			{
				// Make sure we always wrap the actual slot, not another relinked one
				this.slot = UnwrapSlot(slot);
			}

			internal override bool SetValue(object value, Scriptable owner, Scriptable start)
			{
				return slot.SetValue(value, owner, start);
			}

			internal override object GetValue(Scriptable start)
			{
				return slot.GetValue(start);
			}

			internal override ScriptableObject GetPropertyDescriptor(Context cx, Scriptable scope)
			{
				return slot.GetPropertyDescriptor(cx, scope);
			}

			internal override int GetAttributes()
			{
				return slot.GetAttributes();
			}

			internal override void SetAttributes(int value)
			{
				slot.SetAttributes(value);
			}

			internal override void MarkDeleted()
			{
				base.MarkDeleted();
				slot.MarkDeleted();
			}

			/// <exception cref="System.IO.IOException"></exception>
			private void WriteObject(ObjectOutputStream @out)
			{
				@out.WriteObject(slot);
			}
			// just serialize the wrapped slot
		}

		internal static void CheckValidAttributes(int attributes)
		{
			int mask = READONLY | DONTENUM | PERMANENT | UNINITIALIZED_CONST;
			if ((attributes & ~mask) != 0)
			{
				throw new ArgumentException(attributes.ToString());
			}
		}

		public ScriptableObject()
		{
		}

		public ScriptableObject(Scriptable scope, Scriptable prototype)
		{
			if (scope == null)
			{
				throw new ArgumentException();
			}
			parentScopeObject = scope;
			prototypeObject = prototype;
		}

		/// <summary>Gets the value that will be returned by calling the typeof operator on this object.</summary>
		/// <remarks>Gets the value that will be returned by calling the typeof operator on this object.</remarks>
		/// <returns>
		/// default is "object" unless
		/// <see cref="AvoidObjectDetection()">AvoidObjectDetection()</see>
		/// is <code>true</code> in which
		/// case it returns "undefined"
		/// </returns>
		public virtual string GetTypeOf()
		{
			return AvoidObjectDetection() ? "undefined" : "object";
		}

		/// <summary>Return the name of the class.</summary>
		/// <remarks>
		/// Return the name of the class.
		/// This is typically the same name as the constructor.
		/// Classes extending ScriptableObject must implement this abstract
		/// method.
		/// </remarks>
		public abstract string GetClassName();

		/// <summary>Returns true if the named property is defined.</summary>
		/// <remarks>Returns true if the named property is defined.</remarks>
		/// <param name="name">the name of the property</param>
		/// <param name="start">the object in which the lookup began</param>
		/// <returns>true if and only if the property was found in the object</returns>
		public virtual bool Has(string name, Scriptable start)
		{
			return null != GetSlot(name, 0, SLOT_QUERY);
		}

		/// <summary>Returns true if the property index is defined.</summary>
		/// <remarks>Returns true if the property index is defined.</remarks>
		/// <param name="index">the numeric index for the property</param>
		/// <param name="start">the object in which the lookup began</param>
		/// <returns>true if and only if the property was found in the object</returns>
		public virtual bool Has(int index, Scriptable start)
		{
			return null != GetSlot(null, index, SLOT_QUERY);
		}

		/// <summary>Returns the value of the named property or NOT_FOUND.</summary>
		/// <remarks>
		/// Returns the value of the named property or NOT_FOUND.
		/// If the property was created using defineProperty, the
		/// appropriate getter method is called.
		/// </remarks>
		/// <param name="name">the name of the property</param>
		/// <param name="start">the object in which the lookup began</param>
		/// <returns>the value of the property (may be null), or NOT_FOUND</returns>
		public virtual object Get(string name, Scriptable start)
		{
			Slot slot = GetSlot(name, 0, SLOT_QUERY);
			if (slot == null)
			{
				return ScriptableConstants.NOT_FOUND;
			}
			return slot.GetValue(start);
		}

		/// <summary>Returns the value of the indexed property or NOT_FOUND.</summary>
		/// <remarks>Returns the value of the indexed property or NOT_FOUND.</remarks>
		/// <param name="index">the numeric index for the property</param>
		/// <param name="start">the object in which the lookup began</param>
		/// <returns>the value of the property (may be null), or NOT_FOUND</returns>
		public virtual object Get(int index, Scriptable start)
		{
			Slot slot = GetSlot(null, index, SLOT_QUERY);
			if (slot == null)
			{
				return ScriptableConstants.NOT_FOUND;
			}
			return slot.GetValue(start);
		}

		/// <summary>Sets the value of the named property, creating it if need be.</summary>
		/// <remarks>
		/// Sets the value of the named property, creating it if need be.
		/// If the property was created using defineProperty, the
		/// appropriate setter method is called. <p>
		/// If the property's attributes include READONLY, no action is
		/// taken.
		/// This method will actually set the property in the start
		/// object.
		/// </remarks>
		/// <param name="name">the name of the property</param>
		/// <param name="start">the object whose property is being set</param>
		/// <param name="value">value to set the property to</param>
		public virtual void Put(string name, Scriptable start, object value)
		{
			if (PutImpl(name, 0, start, value))
			{
				return;
			}
			if (start == this)
			{
				throw Kit.CodeBug();
			}
			start.Put(name, start, value);
		}

		/// <summary>Sets the value of the indexed property, creating it if need be.</summary>
		/// <remarks>Sets the value of the indexed property, creating it if need be.</remarks>
		/// <param name="index">the numeric index for the property</param>
		/// <param name="start">the object whose property is being set</param>
		/// <param name="value">value to set the property to</param>
		public virtual void Put(int index, Scriptable start, object value)
		{
			if (PutImpl(null, index, start, value))
			{
				return;
			}
			if (start == this)
			{
				throw Kit.CodeBug();
			}
			start.Put(index, start, value);
		}

		/// <summary>Removes a named property from the object.</summary>
		/// <remarks>
		/// Removes a named property from the object.
		/// If the property is not found, or it has the PERMANENT attribute,
		/// no action is taken.
		/// </remarks>
		/// <param name="name">the name of the property</param>
		public virtual void Delete(string name)
		{
			CheckNotSealed(name, 0);
			RemoveSlot(name, 0);
		}

		/// <summary>Removes the indexed property from the object.</summary>
		/// <remarks>
		/// Removes the indexed property from the object.
		/// If the property is not found, or it has the PERMANENT attribute,
		/// no action is taken.
		/// </remarks>
		/// <param name="index">the numeric index for the property</param>
		public virtual void Delete(int index)
		{
			CheckNotSealed(null, index);
			RemoveSlot(null, index);
		}

		/// <summary>Sets the value of the named const property, creating it if need be.</summary>
		/// <remarks>
		/// Sets the value of the named const property, creating it if need be.
		/// If the property was created using defineProperty, the
		/// appropriate setter method is called. <p>
		/// If the property's attributes include READONLY, no action is
		/// taken.
		/// This method will actually set the property in the start
		/// object.
		/// </remarks>
		/// <param name="name">the name of the property</param>
		/// <param name="start">the object whose property is being set</param>
		/// <param name="value">value to set the property to</param>
		public virtual void PutConst(string name, Scriptable start, object value)
		{
			if (PutConstImpl(name, 0, start, value, READONLY))
			{
				return;
			}
			if (start == this)
			{
				throw Kit.CodeBug();
			}
			if (start is ConstProperties)
			{
				((ConstProperties)start).PutConst(name, start, value);
			}
			else
			{
				start.Put(name, start, value);
			}
		}

		public virtual void DefineConst(string name, Scriptable start)
		{
			if (PutConstImpl(name, 0, start, Undefined.instance, UNINITIALIZED_CONST))
			{
				return;
			}
			if (start == this)
			{
				throw Kit.CodeBug();
			}
			if (start is ConstProperties)
			{
				((ConstProperties)start).DefineConst(name, start);
			}
		}

		/// <summary>Returns true if the named property is defined as a const on this object.</summary>
		/// <remarks>Returns true if the named property is defined as a const on this object.</remarks>
		/// <param name="name"></param>
		/// <returns>
		/// true if the named property is defined as a const, false
		/// otherwise.
		/// </returns>
		public virtual bool IsConst(string name)
		{
			Slot slot = GetSlot(name, 0, SLOT_QUERY);
			if (slot == null)
			{
				return false;
			}
			return (slot.GetAttributes() & (PERMANENT | READONLY)) == (PERMANENT | READONLY);
		}

		/// <summary>Get the attributes of a named property.</summary>
		/// <remarks>
		/// Get the attributes of a named property.
		/// The property is specified by <code>name</code>
		/// as defined for <code>has</code>.<p>
		/// </remarks>
		/// <param name="name">the identifier for the property</param>
		/// <returns>the bitset of attributes</returns>
		/// <exception>
		/// EvaluatorException
		/// if the named property is not found
		/// </exception>
		/// <seealso cref="Has(string, Scriptable)">Has(string, Scriptable)</seealso>
		/// <seealso cref="READONLY">READONLY</seealso>
		/// <seealso cref="DONTENUM">DONTENUM</seealso>
		/// <seealso cref="PERMANENT">PERMANENT</seealso>
		/// <seealso cref="EMPTY">EMPTY</seealso>
		public virtual int GetAttributes(string name)
		{
			return FindAttributeSlot(name, 0, SLOT_QUERY).GetAttributes();
		}

		/// <summary>Get the attributes of an indexed property.</summary>
		/// <remarks>Get the attributes of an indexed property.</remarks>
		/// <param name="index">the numeric index for the property</param>
		/// <exception>
		/// EvaluatorException
		/// if the named property is not found
		/// is not found
		/// </exception>
		/// <returns>the bitset of attributes</returns>
		/// <seealso cref="Has(string, Scriptable)">Has(string, Scriptable)</seealso>
		/// <seealso cref="READONLY">READONLY</seealso>
		/// <seealso cref="DONTENUM">DONTENUM</seealso>
		/// <seealso cref="PERMANENT">PERMANENT</seealso>
		/// <seealso cref="EMPTY">EMPTY</seealso>
		public virtual int GetAttributes(int index)
		{
			return FindAttributeSlot(null, index, SLOT_QUERY).GetAttributes();
		}

		/// <summary>Set the attributes of a named property.</summary>
		/// <remarks>
		/// Set the attributes of a named property.
		/// The property is specified by <code>name</code>
		/// as defined for <code>has</code>.<p>
		/// The possible attributes are READONLY, DONTENUM,
		/// and PERMANENT. Combinations of attributes
		/// are expressed by the bitwise OR of attributes.
		/// EMPTY is the state of no attributes set. Any unused
		/// bits are reserved for future use.
		/// </remarks>
		/// <param name="name">the name of the property</param>
		/// <param name="attributes">the bitset of attributes</param>
		/// <exception>
		/// EvaluatorException
		/// if the named property is not found
		/// </exception>
		/// <seealso cref="Scriptable.Has(string,Scriptablee)">Scriptable.Has(string, Scriptable)</seealso>
		/// <seealso cref="READONLY">READONLY</seealso>
		/// <seealso cref="DONTENUM">DONTENUM</seealso>
		/// <seealso cref="PERMANENT">PERMANENT</seealso>
		/// <seealso cref="EMPTY">EMPTY</seealso>
		public virtual void SetAttributes(string name, int attributes)
		{
			CheckNotSealed(name, 0);
			FindAttributeSlot(name, 0, SLOT_MODIFY).SetAttributes(attributes);
		}

		/// <summary>Set the attributes of an indexed property.</summary>
		/// <remarks>Set the attributes of an indexed property.</remarks>
		/// <param name="index">the numeric index for the property</param>
		/// <param name="attributes">the bitset of attributes</param>
		/// <exception>
		/// EvaluatorException
		/// if the named property is not found
		/// </exception>
		/// <seealso cref="Scriptable.Has(string,Scriptablee)">Scriptable.Has(string, Scriptable)</seealso>
		/// <seealso cref="READONLY">READONLY</seealso>
		/// <seealso cref="DONTENUM">DONTENUM</seealso>
		/// <seealso cref="PERMANENT">PERMANENT</seealso>
		/// <seealso cref="EMPTY">EMPTY</seealso>
		public virtual void SetAttributes(int index, int attributes)
		{
			CheckNotSealed(null, index);
			FindAttributeSlot(null, index, SLOT_MODIFY).SetAttributes(attributes);
		}

		/// <summary>XXX: write docs.</summary>
		/// <remarks>XXX: write docs.</remarks>
		public virtual void SetGetterOrSetter(string name, int index, Callable getterOrSetter, bool isSetter)
		{
			SetGetterOrSetter(name, index, getterOrSetter, isSetter, false);
		}

		private void SetGetterOrSetter(string name, int index, Callable getterOrSetter, bool isSetter, bool force)
		{
			if (name != null && index != 0)
			{
				throw new ArgumentException(name);
			}
			if (!force)
			{
				CheckNotSealed(name, index);
			}
			GetterSlot gslot;
			if (IsExtensible())
			{
				gslot = (GetterSlot)GetSlot(name, index, SLOT_MODIFY_GETTER_SETTER);
			}
			else
			{
				Slot slot = UnwrapSlot(GetSlot(name, index, SLOT_QUERY));
				if (!(slot is GetterSlot))
				{
					return;
				}
				gslot = (GetterSlot)slot;
			}
			if (!force)
			{
				int attributes = gslot.GetAttributes();
				if ((attributes & READONLY) != 0)
				{
					throw Context.ReportRuntimeError1("msg.modify.readonly", name);
				}
			}
			if (isSetter)
			{
				gslot.setter = getterOrSetter;
			}
			else
			{
				gslot.getter = getterOrSetter;
			}
			gslot.value = Undefined.instance;
		}

		/// <summary>Get the getter or setter for a given property.</summary>
		/// <remarks>
		/// Get the getter or setter for a given property. Used by __lookupGetter__
		/// and __lookupSetter__.
		/// </remarks>
		/// <param name="name">Name of the object. If nonnull, index must be 0.</param>
		/// <param name="index">Index of the object. If nonzero, name must be null.</param>
		/// <param name="isSetter">If true, return the setter, otherwise return the getter.</param>
		/// <exception>
		/// IllegalArgumentException
		/// if both name and index are nonnull
		/// and nonzero respectively.
		/// </exception>
		/// <returns>
		/// Null if the property does not exist. Otherwise returns either
		/// the getter or the setter for the property, depending on
		/// the value of isSetter (may be undefined if unset).
		/// </returns>
		public virtual object GetGetterOrSetter(string name, int index, bool isSetter)
		{
			if (name != null && index != 0)
			{
				throw new ArgumentException(name);
			}
			Slot slot = UnwrapSlot(GetSlot(name, index, SLOT_QUERY));
			if (slot == null)
			{
				return null;
			}
			if (slot is GetterSlot)
			{
				GetterSlot gslot = (GetterSlot)slot;
				object result = isSetter ? gslot.setter : gslot.getter;
				return result != null ? result : Undefined.instance;
			}
			else
			{
				return Undefined.instance;
			}
		}

		/// <summary>Returns whether a property is a getter or a setter</summary>
		/// <param name="name">property name</param>
		/// <param name="index">property index</param>
		/// <param name="setter">true to check for a setter, false for a getter</param>
		/// <returns>whether the property is a getter or a setter</returns>
		protected internal virtual bool IsGetterOrSetter(string name, int index, bool setter)
		{
			Slot slot = UnwrapSlot(GetSlot(name, index, SLOT_QUERY));
			if (slot is GetterSlot)
			{
				if (setter && ((GetterSlot)slot).setter != null)
				{
					return true;
				}
				if (!setter && ((GetterSlot)slot).getter != null)
				{
					return true;
				}
			}
			return false;
		}

		internal virtual void AddLazilyInitializedValue(string name, int index, LazilyLoadedCtor init, int attributes)
		{
			if (name != null && index != 0)
			{
				throw new ArgumentException(name);
			}
			CheckNotSealed(name, index);
			GetterSlot gslot = (GetterSlot)GetSlot(name, index, SLOT_MODIFY_GETTER_SETTER);
			gslot.SetAttributes(attributes);
			gslot.getter = null;
			gslot.setter = null;
			gslot.value = init;
		}

		/// <summary>Returns the prototype of the object.</summary>
		/// <remarks>Returns the prototype of the object.</remarks>
		public virtual Scriptable GetPrototype()
		{
			return prototypeObject;
		}

		/// <summary>Sets the prototype of the object.</summary>
		/// <remarks>Sets the prototype of the object.</remarks>
		public virtual void SetPrototype(Scriptable m)
		{
			prototypeObject = m;
		}

		/// <summary>Returns the parent (enclosing) scope of the object.</summary>
		/// <remarks>Returns the parent (enclosing) scope of the object.</remarks>
		public virtual Scriptable GetParentScope()
		{
			return parentScopeObject;
		}

		/// <summary>Sets the parent (enclosing) scope of the object.</summary>
		/// <remarks>Sets the parent (enclosing) scope of the object.</remarks>
		public virtual void SetParentScope(Scriptable m)
		{
			parentScopeObject = m;
		}

		/// <summary>Returns an array of ids for the properties of the object.</summary>
		/// <remarks>
		/// Returns an array of ids for the properties of the object.
		/// <p>Any properties with the attribute DONTENUM are not listed. <p>
		/// </remarks>
		/// <returns>
		/// an array of java.lang.Objects with an entry for every
		/// listed property. Properties accessed via an integer index will
		/// have a corresponding
		/// Integer entry in the returned array. Properties accessed by
		/// a String will have a String entry in the returned array.
		/// </returns>
		public virtual object[] GetIds()
		{
			return GetIds(false);
		}

		/// <summary>Returns an array of ids for the properties of the object.</summary>
		/// <remarks>
		/// Returns an array of ids for the properties of the object.
		/// <p>All properties, even those with attribute DONTENUM, are listed. <p>
		/// </remarks>
		/// <returns>
		/// an array of java.lang.Objects with an entry for every
		/// listed property. Properties accessed via an integer index will
		/// have a corresponding
		/// Integer entry in the returned array. Properties accessed by
		/// a String will have a String entry in the returned array.
		/// </returns>
		public virtual object[] GetAllIds()
		{
			return GetIds(true);
		}

		/// <summary>Implements the [[DefaultValue]] internal method.</summary>
		/// <remarks>
		/// Implements the [[DefaultValue]] internal method.
		/// <p>Note that the toPrimitive conversion is a no-op for
		/// every type other than Object, for which [[DefaultValue]]
		/// is called. See ECMA 9.1.<p>
		/// A <code>hint</code> of null means "no hint".
		/// </remarks>
		/// <param name="typeHint">the type hint</param>
		/// <returns>
		/// the default value for the object
		/// See ECMA 8.6.2.6.
		/// </returns>
		public virtual object GetDefaultValue(Type typeHint)
		{
			return GetDefaultValue(this, typeHint);
		}

		public static object GetDefaultValue(Scriptable @object, Type typeHint)
		{
			Context cx = null;
			for (int i = 0; i < 2; i++)
			{
				bool tryToString;
				if (typeHint == ScriptRuntime.StringClass)
				{
					tryToString = (i == 0);
				}
				else
				{
					tryToString = (i == 1);
				}
				string methodName;
				object[] args;
				if (tryToString)
				{
					methodName = "toString";
					args = ScriptRuntime.emptyArgs;
				}
				else
				{
					methodName = "valueOf";
					args = new object[1];
					string hint;
					if (typeHint == null)
					{
						hint = "undefined";
					}
					else
					{
						if (typeHint == ScriptRuntime.StringClass)
						{
							hint = "string";
						}
						else
						{
							if (typeHint == ScriptRuntime.ScriptableClass)
							{
								hint = "object";
							}
							else
							{
								if (typeHint == ScriptRuntime.FunctionClass)
								{
									hint = "function";
								}
								else
								{
									if (typeHint == ScriptRuntime.BooleanClass || typeHint == typeof(bool))
									{
										hint = "boolean";
									}
									else
									{
										if (typeHint == ScriptRuntime.NumberClass || typeHint == ScriptRuntime.ByteClass || typeHint == typeof(byte) || typeHint == ScriptRuntime.ShortClass || typeHint == typeof(short) || typeHint == ScriptRuntime.IntegerClass || typeHint == typeof(int) || typeHint == ScriptRuntime.FloatClass || typeHint == typeof(float) || typeHint == ScriptRuntime.DoubleClass || typeHint == typeof(double))
										{
											hint = "number";
										}
										else
										{
											throw Context.ReportRuntimeError1("msg.invalid.type", typeHint.ToString());
										}
									}
								}
							}
						}
					}
					args[0] = hint;
				}
				object v = GetProperty(@object, methodName);
				if (!(v is Function))
				{
					continue;
				}
				Function fun = (Function)v;
				if (cx == null)
				{
					cx = Context.GetContext();
				}
				v = fun.Call(cx, fun.GetParentScope(), @object, args);
				if (v != null)
				{
					if (!(v is Scriptable))
					{
						return v;
					}
					if (typeHint == ScriptRuntime.ScriptableClass || typeHint == ScriptRuntime.FunctionClass)
					{
						return v;
					}
					if (tryToString && v is Wrapper)
					{
						// Let a wrapped java.lang.String pass for a primitive
						// string.
						object u = ((Wrapper)v).Unwrap();
						if (u is string)
						{
							return u;
						}
					}
				}
			}
			// fall through to error
			string arg = (typeHint == null) ? "undefined" : typeHint.FullName;
			throw ScriptRuntime.TypeError1("msg.default.value", arg);
		}

		/// <summary>Implements the instanceof operator.</summary>
		/// <remarks>
		/// Implements the instanceof operator.
		/// <p>This operator has been proposed to ECMA.
		/// </remarks>
		/// <param name="instance">
		/// The value that appeared on the LHS of the instanceof
		/// operator
		/// </param>
		/// <returns>true if "this" appears in value's prototype chain</returns>
		public virtual bool HasInstance(Scriptable instance)
		{
			// Default for JS objects (other than Function) is to do prototype
			// chasing.  This will be overridden in NativeFunction and non-JS
			// objects.
			return ScriptRuntime.JsDelegatesTo(instance, this);
		}

		/// <summary>
		/// Emulate the SpiderMonkey (and Firefox) feature of allowing
		/// custom objects to avoid detection by normal "object detection"
		/// code patterns.
		/// </summary>
		/// <remarks>
		/// Emulate the SpiderMonkey (and Firefox) feature of allowing
		/// custom objects to avoid detection by normal "object detection"
		/// code patterns. This is used to implement document.all.
		/// See https://bugzilla.mozilla.org/show_bug.cgi?id=412247.
		/// This is an analog to JOF_DETECTING from SpiderMonkey; see
		/// https://bugzilla.mozilla.org/show_bug.cgi?id=248549.
		/// Other than this special case, embeddings should return false.
		/// </remarks>
		/// <returns>true if this object should avoid object detection</returns>
		/// <since>1.7R1</since>
		public virtual bool AvoidObjectDetection()
		{
			return false;
		}

		/// <summary>Custom <tt>==</tt> operator.</summary>
		/// <remarks>
		/// Custom <tt>==</tt> operator.
		/// Must return
		/// <see cref="ScriptableConstants.NOT_FOUND">ScriptableConstants.NOT_FOUND</see>
		/// if this object does not
		/// have custom equality operator for the given value,
		/// <tt>Boolean.TRUE</tt> if this object is equivalent to <tt>value</tt>,
		/// <tt>Boolean.FALSE</tt> if this object is not equivalent to
		/// <tt>value</tt>.
		/// <p>
		/// The default implementation returns Boolean.TRUE
		/// if <tt>this == value</tt> or
		/// <see cref="ScriptableConstants.NOT_FOUND">ScriptableConstants.NOT_FOUND</see>
		/// otherwise.
		/// It indicates that by default custom equality is available only if
		/// <tt>value</tt> is <tt>this</tt> in which case true is returned.
		/// </remarks>
		protected internal virtual object EquivalentValues(object value)
		{
			return (this == value) ? true : ScriptableConstants.NOT_FOUND;
		}

		/// <summary>Defines JavaScript objects from a Java class that implements Scriptable.</summary>
		/// <remarks>
		/// Defines JavaScript objects from a Java class that implements Scriptable.
		/// If the given class has a method
		/// <pre>
		/// static void init(Context cx, Scriptable scope, boolean sealed);</pre>
		/// or its compatibility form
		/// <pre>
		/// static void init(Scriptable scope);</pre>
		/// then it is invoked and no further initialization is done.<p>
		/// However, if no such a method is found, then the class's constructors and
		/// methods are used to initialize a class in the following manner.<p>
		/// First, the zero-parameter constructor of the class is called to
		/// create the prototype. If no such constructor exists,
		/// a
		/// <see cref="EvaluatorException">EvaluatorException</see>
		/// is thrown. <p>
		/// Next, all methods are scanned for special prefixes that indicate that they
		/// have special meaning for defining JavaScript objects.
		/// These special prefixes are
		/// <ul>
		/// <li><code>jsFunction_</code> for a JavaScript function
		/// <li><code>jsStaticFunction_</code> for a JavaScript function that
		/// is a property of the constructor
		/// <li><code>jsGet_</code> for a getter of a JavaScript property
		/// <li><code>jsSet_</code> for a setter of a JavaScript property
		/// <li><code>jsConstructor</code> for a JavaScript function that
		/// is the constructor
		/// </ul><p>
		/// If the method's name begins with "jsFunction_", a JavaScript function
		/// is created with a name formed from the rest of the Java method name
		/// following "jsFunction_". So a Java method named "jsFunction_foo" will
		/// define a JavaScript method "foo". Calling this JavaScript function
		/// will cause the Java method to be called. The parameters of the method
		/// must be of number and types as defined by the FunctionObject class.
		/// The JavaScript function is then added as a property
		/// of the prototype. <p>
		/// If the method's name begins with "jsStaticFunction_", it is handled
		/// similarly except that the resulting JavaScript function is added as a
		/// property of the constructor object. The Java method must be static.
		/// If the method's name begins with "jsGet_" or "jsSet_", the method is
		/// considered to define a property. Accesses to the defined property
		/// will result in calls to these getter and setter methods. If no
		/// setter is defined, the property is defined as READONLY.<p>
		/// If the method's name is "jsConstructor", the method is
		/// considered to define the body of the constructor. Only one
		/// method of this name may be defined. You may use the varargs forms
		/// for constructors documented in
		/// <see cref="FunctionObject.FunctionObject(string, System.Reflection.MemberInfo, Scriptable)">FunctionObject.FunctionObject(string, System.Reflection.MemberInfo, Scriptable)</see>
		/// If no method is found that can serve as constructor, a Java
		/// constructor will be selected to serve as the JavaScript
		/// constructor in the following manner. If the class has only one
		/// Java constructor, that constructor is used to define
		/// the JavaScript constructor. If the the class has two constructors,
		/// one must be the zero-argument constructor (otherwise an
		/// <see cref="EvaluatorException">EvaluatorException</see>
		/// would have already been thrown
		/// when the prototype was to be created). In this case
		/// the Java constructor with one or more parameters will be used
		/// to define the JavaScript constructor. If the class has three
		/// or more constructors, an
		/// <see cref="EvaluatorException">EvaluatorException</see>
		/// will be thrown.<p>
		/// Finally, if there is a method
		/// <pre>
		/// static void finishInit(Scriptable scope, FunctionObject constructor,
		/// Scriptable prototype)</pre>
		/// it will be called to finish any initialization. The <code>scope</code>
		/// argument will be passed, along with the newly created constructor and
		/// the newly created prototype.<p>
		/// </remarks>
		/// <param name="scope">The scope in which to define the constructor.</param>
		/// <param name="clazz">
		/// The Java class to use to define the JavaScript objects
		/// and properties.
		/// </param>
		/// <exception>
		/// IllegalAccessException
		/// if access is not available
		/// to a reflected class member
		/// </exception>
		/// <exception>
		/// InstantiationException
		/// if unable to instantiate
		/// the named class
		/// </exception>
		/// <exception>
		/// InvocationTargetException
		/// if an exception is thrown
		/// during execution of methods of the named class
		/// </exception>
		/// <seealso cref="Function">Function</seealso>
		/// <seealso cref="FunctionObject">FunctionObject</seealso>
		/// <seealso cref="READONLY">READONLY</seealso>
		/// <seealso cref="DefineProperty(string, System.Type{T}, int)">DefineProperty(string, System.Type&lt;T&gt;, int)</seealso>
		/// <exception cref="System.MemberAccessException"></exception>
		/// <exception cref="Sharpen.InstantiationException"></exception>
		/// <exception cref="System.Reflection.TargetInvocationException"></exception>
		public static void DefineClass<T>(Scriptable scope) where T:Scriptable
		{
			DefineClass<T>(scope, false, false);
		}
		
		/// <summary>Defines JavaScript objects from a Java class that implements Scriptable.</summary>
		/// <remarks>
		/// Defines JavaScript objects from a Java class that implements Scriptable.
		/// If the given class has a method
		/// <pre>
		/// static void init(Context cx, Scriptable scope, boolean sealed);</pre>
		/// or its compatibility form
		/// <pre>
		/// static void init(Scriptable scope);</pre>
		/// then it is invoked and no further initialization is done.<p>
		/// However, if no such a method is found, then the class's constructors and
		/// methods are used to initialize a class in the following manner.<p>
		/// First, the zero-parameter constructor of the class is called to
		/// create the prototype. If no such constructor exists,
		/// a
		/// <see cref="EvaluatorException">EvaluatorException</see>
		/// is thrown. <p>
		/// Next, all methods are scanned for special prefixes that indicate that they
		/// have special meaning for defining JavaScript objects.
		/// These special prefixes are
		/// <ul>
		/// <li><code>jsFunction_</code> for a JavaScript function
		/// <li><code>jsStaticFunction_</code> for a JavaScript function that
		/// is a property of the constructor
		/// <li><code>jsGet_</code> for a getter of a JavaScript property
		/// <li><code>jsSet_</code> for a setter of a JavaScript property
		/// <li><code>jsConstructor</code> for a JavaScript function that
		/// is the constructor
		/// </ul><p>
		/// If the method's name begins with "jsFunction_", a JavaScript function
		/// is created with a name formed from the rest of the Java method name
		/// following "jsFunction_". So a Java method named "jsFunction_foo" will
		/// define a JavaScript method "foo". Calling this JavaScript function
		/// will cause the Java method to be called. The parameters of the method
		/// must be of number and types as defined by the FunctionObject class.
		/// The JavaScript function is then added as a property
		/// of the prototype. <p>
		/// If the method's name begins with "jsStaticFunction_", it is handled
		/// similarly except that the resulting JavaScript function is added as a
		/// property of the constructor object. The Java method must be static.
		/// If the method's name begins with "jsGet_" or "jsSet_", the method is
		/// considered to define a property. Accesses to the defined property
		/// will result in calls to these getter and setter methods. If no
		/// setter is defined, the property is defined as READONLY.<p>
		/// If the method's name is "jsConstructor", the method is
		/// considered to define the body of the constructor. Only one
		/// method of this name may be defined. You may use the varargs forms
		/// for constructors documented in
		/// <see cref="FunctionObject.FunctionObject(string, System.Reflection.MemberInfo, Scriptable)">FunctionObject.FunctionObject(string, System.Reflection.MemberInfo, Scriptable)</see>
		/// If no method is found that can serve as constructor, a Java
		/// constructor will be selected to serve as the JavaScript
		/// constructor in the following manner. If the class has only one
		/// Java constructor, that constructor is used to define
		/// the JavaScript constructor. If the the class has two constructors,
		/// one must be the zero-argument constructor (otherwise an
		/// <see cref="EvaluatorException">EvaluatorException</see>
		/// would have already been thrown
		/// when the prototype was to be created). In this case
		/// the Java constructor with one or more parameters will be used
		/// to define the JavaScript constructor. If the class has three
		/// or more constructors, an
		/// <see cref="EvaluatorException">EvaluatorException</see>
		/// will be thrown.<p>
		/// Finally, if there is a method
		/// <pre>
		/// static void finishInit(Scriptable scope, FunctionObject constructor,
		/// Scriptable prototype)</pre>
		/// it will be called to finish any initialization. The <code>scope</code>
		/// argument will be passed, along with the newly created constructor and
		/// the newly created prototype.<p>
		/// </remarks>
		/// <param name="scope">The scope in which to define the constructor.</param>
		/// <param name="clazz">
		/// The Java class to use to define the JavaScript objects
		/// and properties.
		/// </param>
		/// <exception>
		/// IllegalAccessException
		/// if access is not available
		/// to a reflected class member
		/// </exception>
		/// <exception>
		/// InstantiationException
		/// if unable to instantiate
		/// the named class
		/// </exception>
		/// <exception>
		/// InvocationTargetException
		/// if an exception is thrown
		/// during execution of methods of the named class
		/// </exception>
		/// <seealso cref="Function">Function</seealso>
		/// <seealso cref="FunctionObject">FunctionObject</seealso>
		/// <seealso cref="READONLY">READONLY</seealso>
		/// <seealso cref="DefineProperty(string, System.Type, int)">DefineProperty(string, System.Type&lt;T&gt;, int)</seealso>
		/// <exception cref="System.MemberAccessException"></exception>
		/// <exception cref="Sharpen.InstantiationException"></exception>
		/// <exception cref="System.Reflection.TargetInvocationException"></exception>
		public static void DefineClass(Scriptable scope, Type type)
		{
			DefineClass(scope, type, false, false);
		}

		/// <summary>
		/// Defines JavaScript objects from a Java class, optionally
		/// allowing sealing.
		/// </summary>
		/// <remarks>
		/// Defines JavaScript objects from a Java class, optionally
		/// allowing sealing.
		/// Similar to <code>defineClass(Scriptable scope, Class clazz)</code>
		/// except that sealing is allowed. An object that is sealed cannot have
		/// properties added or removed. Note that sealing is not allowed in
		/// the current ECMA/ISO language specification, but is likely for
		/// the next version.
		/// </remarks>
		/// <param name="scope">The scope in which to define the constructor.</param>
		/// <param name="clazz">
		/// The Java class to use to define the JavaScript objects
		/// and properties. The class must implement Scriptable.
		/// </param>
		/// <param name="sealed">
		/// Whether or not to create sealed standard objects that
		/// cannot be modified.
		/// </param>
		/// <exception>
		/// IllegalAccessException
		/// if access is not available
		/// to a reflected class member
		/// </exception>
		/// <exception>
		/// InstantiationException
		/// if unable to instantiate
		/// the named class
		/// </exception>
		/// <exception>
		/// InvocationTargetException
		/// if an exception is thrown
		/// during execution of methods of the named class
		/// </exception>
		/// <since>1.4R3</since>
		/// <exception cref="System.MemberAccessException"></exception>
		/// <exception cref="Sharpen.InstantiationException"></exception>
		/// <exception cref="System.Reflection.TargetInvocationException"></exception>
		public static void DefineClass<T>(Scriptable scope, bool @sealed) where T:Scriptable
		{
			DefineClass<T>(scope, @sealed, false);
		}

		/// <summary>
		/// Defines JavaScript objects from a Java class, optionally
		/// allowing sealing and mapping of Java inheritance to JavaScript
		/// prototype-based inheritance.
		/// </summary>
		/// <remarks>
		/// Defines JavaScript objects from a Java class, optionally
		/// allowing sealing and mapping of Java inheritance to JavaScript
		/// prototype-based inheritance.
		/// Similar to <code>defineClass(Scriptable scope, Class clazz)</code>
		/// except that sealing and inheritance mapping are allowed. An object
		/// that is sealed cannot have properties added or removed. Note that
		/// sealing is not allowed in the current ECMA/ISO language specification,
		/// but is likely for the next version.
		/// </remarks>
		/// <param name="scope">The scope in which to define the constructor.</param>
		/// <param name="clazz">
		/// The Java class to use to define the JavaScript objects
		/// and properties. The class must implement Scriptable.
		/// </param>
		/// <param name="sealed">
		/// Whether or not to create sealed standard objects that
		/// cannot be modified.
		/// </param>
		/// <param name="mapInheritance">
		/// Whether or not to map Java inheritance to
		/// JavaScript prototype-based inheritance.
		/// </param>
		/// <returns>the class name for the prototype of the specified class</returns>
		/// <exception>
		/// IllegalAccessException
		/// if access is not available
		/// to a reflected class member
		/// </exception>
		/// <exception>
		/// InstantiationException
		/// if unable to instantiate
		/// the named class
		/// </exception>
		/// <exception>
		/// InvocationTargetException
		/// if an exception is thrown
		/// during execution of methods of the named class
		/// </exception>
		/// <since>1.6R2</since>
		/// <exception cref="System.MemberAccessException"></exception>
		/// <exception cref="Sharpen.InstantiationException"></exception>
		/// <exception cref="System.Reflection.TargetInvocationException"></exception>
		public static string DefineClass<T>(Scriptable scope, bool @sealed, bool mapInheritance) where T:Scriptable
		{
			return DefineClass(scope, typeof (T), @sealed, mapInheritance);
		}

		/// <summary>
		/// Defines JavaScript objects from a Java class, optionally
		/// allowing sealing and mapping of Java inheritance to JavaScript
		/// prototype-based inheritance.
		/// </summary>
		/// <remarks>
		/// Defines JavaScript objects from a Java class, optionally
		/// allowing sealing and mapping of Java inheritance to JavaScript
		/// prototype-based inheritance.
		/// Similar to <code>defineClass(Scriptable scope, Class clazz)</code>
		/// except that sealing and inheritance mapping are allowed. An object
		/// that is sealed cannot have properties added or removed. Note that
		/// sealing is not allowed in the current ECMA/ISO language specification,
		/// but is likely for the next version.
		/// </remarks>
		/// <param name="scope">The scope in which to define the constructor.</param>
		/// <param name="clazz">
		/// The Java class to use to define the JavaScript objects
		/// and properties. The class must implement Scriptable.
		/// </param>
		/// <param name="sealed">
		/// Whether or not to create sealed standard objects that
		/// cannot be modified.
		/// </param>
		/// <param name="mapInheritance">
		/// Whether or not to map Java inheritance to
		/// JavaScript prototype-based inheritance.
		/// </param>
		/// <returns>the class name for the prototype of the specified class</returns>
		/// <exception>
		/// IllegalAccessException
		/// if access is not available
		/// to a reflected class member
		/// </exception>
		/// <exception>
		/// InstantiationException
		/// if unable to instantiate
		/// the named class
		/// </exception>
		/// <exception>
		/// InvocationTargetException
		/// if an exception is thrown
		/// during execution of methods of the named class
		/// </exception>
		/// <since>1.6R2</since>
		/// <exception cref="System.MemberAccessException"></exception>
		/// <exception cref="Sharpen.InstantiationException"></exception>
		/// <exception cref="System.Reflection.TargetInvocationException"></exception>
		public static string DefineClass(Scriptable scope, Type clazz, bool @sealed, bool mapInheritance)
		{
			BaseFunction ctor = BuildClassCtor(scope, clazz, @sealed, mapInheritance);
			if (ctor == null)
			{
				return null;
			}
			string name = ctor.GetClassPrototype().GetClassName();
			DefineProperty(scope, name, ctor, DONTENUM);
			return name;
		}

		internal static BaseFunction BuildClassCtor(Scriptable scope, Type clazz, bool @sealed, bool mapInheritance)
		{
			MethodInfo[] methods = FunctionObject.GetMethodList(clazz);
			for (int i = 0; i < methods.Length; i++)
			{
				MethodInfo method = methods[i];
				if (!method.Name.Equals("Init"))
				{
					continue;
				}
				Type[] parmTypes = method.GetParameterTypes();
				if (parmTypes.Length == 3 && parmTypes[0] == ScriptRuntime.ContextClass && parmTypes[1] == ScriptRuntime.ScriptableClass && parmTypes[2] == typeof(bool) && method.IsStatic)
				{
					object[] args = new object[] { Context.GetContext(), scope, @sealed };
					method.Invoke(null, args);
					return null;
				}
				if (parmTypes.Length == 1 && parmTypes[0] == ScriptRuntime.ScriptableClass && method.IsStatic)
				{
					object[] args = new object[] { scope };
					method.Invoke(null, args);
					return null;
				}
			}
			// If we got here, there isn't an "init" method with the right
			// parameter types.
			ConstructorInfo[] ctors = clazz.GetConstructors();
			ConstructorInfo protoCtor = null;
			for (int i_1 = 0; i_1 < ctors.Length; i_1++)
			{
				if (ctors[i_1].GetParameterTypes().Length == 0)
				{
					protoCtor = ctors[i_1];
					break;
				}
			}
			if (protoCtor == null)
			{
				throw Context.ReportRuntimeError1("msg.zero.arg.ctor", clazz.FullName);
			}
			Scriptable proto = (Scriptable)protoCtor.NewInstance(ScriptRuntime.emptyArgs);
			string className = proto.GetClassName();
			// check for possible redefinition
			object existing = GetProperty(GetTopLevelScope(scope), className);
			if (existing is BaseFunction)
			{
				object existingProto = ((BaseFunction)existing).GetPrototypeProperty();
				if (existingProto != null && clazz == existingProto.GetType())
				{
					return (BaseFunction)existing;
				}
			}
			// Set the prototype's prototype, trying to map Java inheritance to JS
			// prototype-based inheritance if requested to do so.
			Scriptable superProto = null;
			if (mapInheritance)
			{
				Type superClass = clazz.BaseType;
				if (ScriptRuntime.ScriptableClass.IsAssignableFrom(superClass) && !superClass.IsAbstract)
				{
					Type superScriptable = ExtendsScriptable(superClass);
					string name = DefineClass(scope, superScriptable, @sealed, mapInheritance);
					if (name != null)
					{
						superProto = GetClassPrototype(scope, name);
					}
				}
			}
			if (superProto == null)
			{
				superProto = GetObjectPrototype(scope);
			}
			proto.SetPrototype(superProto);
			// Find out whether there are any methods that begin with
			// "js". If so, then only methods that begin with special
			// prefixes will be defined as JavaScript entities.
			string functionPrefix = "jsFunction_";
			string staticFunctionPrefix = "jsStaticFunction_";
			string getterPrefix = "jsGet_";
			string setterPrefix = "jsSet_";
			string ctorName = "jsConstructor";
			MethodBase ctorMember = FindAnnotatedMember(methods, typeof (JSConstructor));
			if (ctorMember == null)
			{
				ctorMember = FindAnnotatedMember(ctors, typeof(JSConstructor));
			}
			if (ctorMember == null)
			{
				ctorMember = FunctionObject.FindSingleMethod(methods, ctorName);
			}
			if (ctorMember == null)
			{
				if (ctors.Length == 1)
				{
					ctorMember = ctors[0];
				}
				else
				{
					if (ctors.Length == 2)
					{
						if (ctors[0].GetParameterTypes().Length == 0)
						{
							ctorMember = ctors[1];
						}
						else
						{
							if (ctors[1].GetParameterTypes().Length == 0)
							{
								ctorMember = ctors[0];
							}
						}
					}
				}
				if (ctorMember == null)
				{
					throw Context.ReportRuntimeError1("msg.ctor.multiple.parms", clazz.FullName);
				}
			}
			FunctionObject ctor = new FunctionObject(className, ctorMember, scope);
			if (ctor.IsVarArgsMethod())
			{
				throw Context.ReportRuntimeError1("msg.varargs.ctor", ctorMember.Name);
			}
			ctor.InitAsConstructor(scope, proto);
			MethodInfo finishInit = null;
			HashSet<string> staticNames = new HashSet<string>();
			HashSet<string> instanceNames = new HashSet<string>();
			foreach (MethodInfo method_1 in methods)
			{
				if (method_1 == ctorMember)
				{
					continue;
				}
				string name = method_1.Name;
				if (name.Equals("finishInit"))
				{
					Type[] parmTypes = method_1.GetParameterTypes();
					if (parmTypes.Length == 3 && parmTypes[0] == ScriptRuntime.ScriptableClass && parmTypes[1] == typeof(FunctionObject) && parmTypes[2] == ScriptRuntime.ScriptableClass && method_1.IsStatic)
					{
						finishInit = method_1;
						continue;
					}
				}
				// ignore any compiler generated methods.
				if (name.IndexOf('$') != -1)
				{
					continue;
				}
				if (name.Equals(ctorName))
				{
					continue;
				}
				Attribute annotation = null;
				string prefix = null;
				if (method_1.IsAnnotationPresent(typeof(JSFunction)))
				{
					annotation = method_1.GetCustomAttribute<JSFunction>();
				}
				else
				{
					if (method_1.IsAnnotationPresent(typeof(JSStaticFunction)))
					{
						annotation = method_1.GetCustomAttribute<JSStaticFunction>();
					}
					else
					{
						if (method_1.IsAnnotationPresent(typeof(JSGetter)))
						{
							annotation = method_1.GetCustomAttribute<JSGetter>();
						}
						else
						{
							if (method_1.IsAnnotationPresent(typeof(JSSetter)))
							{
								continue;
							}
						}
					}
				}
				if (annotation == null)
				{
					if (name.StartsWith(functionPrefix))
					{
						prefix = functionPrefix;
					}
					else
					{
						if (name.StartsWith(staticFunctionPrefix))
						{
							prefix = staticFunctionPrefix;
						}
						else
						{
							if (name.StartsWith(getterPrefix))
							{
								prefix = getterPrefix;
							}
							else
							{
								if (annotation == null)
								{
									// note that setterPrefix is among the unhandled names here -
									// we deal with that when we see the getter
									continue;
								}
							}
						}
					}
				}
				bool isStatic = annotation is JSStaticFunction || prefix == staticFunctionPrefix;
				HashSet<string> names = isStatic ? staticNames : instanceNames;
				string propName = GetPropertyName(name, prefix, annotation);
				if (names.Contains(propName))
				{
					throw Context.ReportRuntimeError2("duplicate.defineClass.name", name, propName);
				}
				names.Add(propName);
				name = propName;
				if (annotation is JSGetter || prefix == getterPrefix)
				{
					if (!(proto is ScriptableObject))
					{
						throw Context.ReportRuntimeError2("msg.extend.scriptable", proto.GetType().ToString(), name);
					}
					MethodInfo setter = FindSetterMethod(methods, name, setterPrefix);
					int attr = PERMANENT | DONTENUM | (setter != null ? 0 : READONLY);
					((ScriptableObject)proto).DefineProperty(name, null, method_1, setter, attr);
					continue;
				}
				if (isStatic && !method_1.IsStatic)
				{
					throw Context.ReportRuntimeError("jsStaticFunction must be used with static method.");
				}
				FunctionObject f = new FunctionObject(name, method_1, proto);
				if (f.IsVarArgsConstructor())
				{
					throw Context.ReportRuntimeError1("msg.varargs.fun", ctorMember.Name);
				}
				DefineProperty(isStatic ? ctor : proto, name, f, DONTENUM);
				if (@sealed)
				{
					f.SealObject();
				}
			}
			// Call user code to complete initialization if necessary.
			if (finishInit != null)
			{
				object[] finishArgs = new object[] { scope, ctor, proto };
				finishInit.Invoke(null, finishArgs);
			}
			// Seal the object if necessary.
			if (@sealed)
			{
				ctor.SealObject();
				if (proto is ScriptableObject)
				{
					((ScriptableObject)proto).SealObject();
				}
			}
			return ctor;
		}

		private static T FindAnnotatedMember<T>(IEnumerable<T> members, Type annotation) where T: class, ICustomAttributeProvider
		{
			foreach (T member in members)
			{
				if (member.GetCustomAttributes(annotation, true).Any())
				{
					return member;
				}
			}
			return null;
		}

		private static MethodInfo FindSetterMethod(MethodInfo[] methods, string name, string prefix)
		{
			string newStyleName = "set" + Char.ToUpper(name[0]) + name.Substring(1);
			foreach (MethodInfo method in methods)
			{
				JSSetter annotation = method.GetCustomAttribute<JSSetter>();
				if (annotation != null)
				{
					if (name.Equals(annotation.Value) || (string.Empty.Equals(annotation.Value) && newStyleName.Equals(method.Name)))
					{
						return method;
					}
				}
			}
			string oldStyleName = prefix + name;
			foreach (MethodInfo method_1 in methods)
			{
				if (oldStyleName.Equals(method_1.Name))
				{
					return method_1;
				}
			}
			return null;
		}

		private static string GetPropertyName(string methodName, string prefix, Attribute annotation)
		{
			if (prefix != null)
			{
				return methodName.Substring(prefix.Length);
			}
			string propName = null;
			var jsGetter = annotation as JSGetter;
			if (jsGetter != null)
			{
				propName = jsGetter.Value;
				if (string.IsNullOrEmpty(propName))
				{
					if (methodName.Length > 3 && methodName.StartsWith("get"))
					{
						propName = methodName.Substring(3);
						if (Char.IsUpper(propName[0]))
						{
							if (propName.Length == 1)
							{
								propName = propName.ToLower();
							}
							else
							{
								if (!Char.IsUpper(propName[1]))
								{
									propName = Char.ToLower(propName[0]) + propName.Substring(1);
								}
							}
						}
					}
				}
			}
			else
			{
				var jsFunction = annotation as JSFunction;
				if (jsFunction != null)
				{
					propName = jsFunction.Value;
				}
				else
				{
					var jsStaticFunction = annotation as JSStaticFunction;
					if (jsStaticFunction != null)
					{
						propName = jsStaticFunction.Value;
					}
				}
			}
			if (string.IsNullOrEmpty(propName))
			{
				propName = methodName;
			}
			return propName;
		}

		private static Type ExtendsScriptable(Type c)
		{
			if (ScriptRuntime.ScriptableClass.IsAssignableFrom(c))
			{
				return c;
			}
			return null;
		}

		/// <summary>Define a JavaScript property.</summary>
		/// <remarks>
		/// Define a JavaScript property.
		/// Creates the property with an initial value and sets its attributes.
		/// </remarks>
		/// <param name="propertyName">the name of the property to define.</param>
		/// <param name="value">the initial value of the property</param>
		/// <param name="attributes">the attributes of the JavaScript property</param>
		/// <seealso cref="Scriptable.Put(string, SIScriptable object)">Scriptable.Put(string, Scriptable, object)</seealso>
		public virtual void DefineProperty(string propertyName, object value, int attributes)
		{
			CheckNotSealed(propertyName, 0);
			Put(propertyName, this, value);
			SetAttributes(propertyName, attributes);
		}

		/// <summary>Utility method to add properties to arbitrary Scriptable object.</summary>
		/// <remarks>
		/// Utility method to add properties to arbitrary Scriptable object.
		/// If destination is instance of ScriptableObject, calls
		/// defineProperty there, otherwise calls put in destination
		/// ignoring attributes
		/// </remarks>
		public static void DefineProperty(Scriptable destination, string propertyName, object value, int attributes)
		{
			if (!(destination is ScriptableObject))
			{
				destination.Put(propertyName, destination, value);
				return;
			}
			ScriptableObject so = (ScriptableObject)destination;
			so.DefineProperty(propertyName, value, attributes);
		}

		/// <summary>Utility method to add properties to arbitrary Scriptable object.</summary>
		/// <remarks>
		/// Utility method to add properties to arbitrary Scriptable object.
		/// If destination is instance of ScriptableObject, calls
		/// defineProperty there, otherwise calls put in destination
		/// ignoring attributes
		/// </remarks>
		public static void DefineConstProperty(Scriptable destination, string propertyName)
		{
			if (destination is ConstProperties)
			{
				ConstProperties cp = (ConstProperties)destination;
				cp.DefineConst(propertyName, destination);
			}
			else
			{
				DefineProperty(destination, propertyName, Undefined.instance, CONST);
			}
		}

		/// <summary>Define a JavaScript property with getter and setter side effects.</summary>
		/// <remarks>
		/// Define a JavaScript property with getter and setter side effects.
		/// If the setter is not found, the attribute READONLY is added to
		/// the given attributes. <p>
		/// The getter must be a method with zero parameters, and the setter, if
		/// found, must be a method with one parameter.<p>
		/// </remarks>
		/// <param name="propertyName">
		/// the name of the property to define. This name
		/// also affects the name of the setter and getter
		/// to search for. If the propertyId is "foo", then
		/// <code>clazz</code> will be searched for "getFoo"
		/// and "setFoo" methods.
		/// </param>
		/// <param name="clazz">the Java class to search for the getter and setter</param>
		/// <param name="attributes">the attributes of the JavaScript property</param>
		/// <seealso cref="Scriptable.Put(string, SIScriptable object)">Scriptable.Put(string, Scriptable, object)</seealso>
		public virtual void DefineProperty(string propertyName, Type clazz, int attributes)
		{
			int length = propertyName.Length;
			if (length == 0)
			{
				throw new ArgumentException();
			}
			char[] buf = new char[3 + length];
			propertyName.CopyTo(0, buf, 3, length);
			buf[3] = Char.ToUpper(buf[3]);
			buf[0] = 'g';
			buf[1] = 'e';
			buf[2] = 't';
			string getterName = new string(buf);
			buf[0] = 's';
			string setterName = new string(buf);
			MethodInfo[] methods = FunctionObject.GetMethodList(clazz);
			MethodInfo getter = FunctionObject.FindSingleMethod(methods, getterName);
			MethodInfo setter = FunctionObject.FindSingleMethod(methods, setterName);
			if (setter == null)
			{
				attributes |= READONLY;
			}
			DefineProperty(propertyName, null, getter, setter == null ? null : setter, attributes);
		}

		/// <summary>Define a JavaScript property.</summary>
		/// <remarks>
		/// Define a JavaScript property.
		/// Use this method only if you wish to define getters and setters for
		/// a given property in a ScriptableObject. To create a property without
		/// special getter or setter side effects, use
		/// <code>defineProperty(String,int)</code>.
		/// If <code>setter</code> is null, the attribute READONLY is added to
		/// the given attributes.<p>
		/// Several forms of getters or setters are allowed. In all cases the
		/// type of the value parameter can be any one of the following types:
		/// Object, String, boolean, Scriptable, byte, short, int, long, float,
		/// or double. The runtime will perform appropriate conversions based
		/// upon the type of the parameter (see description in FunctionObject).
		/// The first forms are nonstatic methods of the class referred to
		/// by 'this':
		/// <pre>
		/// Object getFoo();
		/// void setFoo(SomeType value);</pre>
		/// Next are static methods that may be of any class; the object whose
		/// property is being accessed is passed in as an extra argument:
		/// <pre>
		/// static Object getFoo(Scriptable obj);
		/// static void setFoo(Scriptable obj, SomeType value);</pre>
		/// Finally, it is possible to delegate to another object entirely using
		/// the <code>delegateTo</code> parameter. In this case the methods are
		/// nonstatic methods of the class delegated to, and the object whose
		/// property is being accessed is passed in as an extra argument:
		/// <pre>
		/// Object getFoo(Scriptable obj);
		/// void setFoo(Scriptable obj, SomeType value);</pre>
		/// </remarks>
		/// <param name="propertyName">the name of the property to define.</param>
		/// <param name="delegateTo">
		/// an object to call the getter and setter methods on,
		/// or null, depending on the form used above.
		/// </param>
		/// <param name="getter">the method to invoke to get the value of the property</param>
		/// <param name="setter">the method to invoke to set the value of the property</param>
		/// <param name="attributes">the attributes of the JavaScript property</param>
		public virtual void DefineProperty(string propertyName, object delegateTo, MethodInfo getter, MethodInfo setter, int attributes)
		{
			MemberBox getterBox = null;
			if (getter != null)
			{
				getterBox = new MemberBox(getter);
				bool delegatedForm;
				if (!getter.IsStatic)
				{
					delegatedForm = (delegateTo != null);
					getterBox.delegateTo = delegateTo;
				}
				else
				{
					delegatedForm = true;
					// Ignore delegateTo for static getter but store
					// non-null delegateTo indicator.
					getterBox.delegateTo = typeof(void);
				}
				string errorId = null;
				Type[] parmTypes = getter.GetParameterTypes();
				if (parmTypes.Length == 0)
				{
					if (delegatedForm)
					{
						errorId = "msg.obj.getter.parms";
					}
				}
				else
				{
					if (parmTypes.Length == 1)
					{
						var argType = parmTypes[0];
						// Allow ScriptableObject for compatibility
						if (!(argType == ScriptRuntime.ScriptableClass || argType == ScriptRuntime.ScriptableObjectClass))
						{
							errorId = "msg.bad.getter.parms";
						}
						else
						{
							if (!delegatedForm)
							{
								errorId = "msg.bad.getter.parms";
							}
						}
					}
					else
					{
						errorId = "msg.bad.getter.parms";
					}
				}
				if (errorId != null)
				{
					throw Context.ReportRuntimeError1(errorId, getter.ToString());
				}
			}
			MemberBox setterBox = null;
			if (setter != null)
			{
				if (setter.ReturnType != typeof(void))
				{
					throw Context.ReportRuntimeError1("msg.setter.return", setter.ToString());
				}
				setterBox = new MemberBox(setter);
				bool delegatedForm;
				if (!setter.IsStatic)
				{
					delegatedForm = (delegateTo != null);
					setterBox.delegateTo = delegateTo;
				}
				else
				{
					delegatedForm = true;
					// Ignore delegateTo for static setter but store
					// non-null delegateTo indicator.
					setterBox.delegateTo = typeof(void);
				}
				string errorId = null;
				Type[] parmTypes = setter.GetParameterTypes();
				if (parmTypes.Length == 1)
				{
					if (delegatedForm)
					{
						errorId = "msg.setter2.expected";
					}
				}
				else
				{
					if (parmTypes.Length == 2)
					{
						object argType = parmTypes[0];
						// Allow ScriptableObject for compatibility
						if (!(argType == ScriptRuntime.ScriptableClass || argType == ScriptRuntime.ScriptableObjectClass))
						{
							errorId = "msg.setter2.parms";
						}
						else
						{
							if (!delegatedForm)
							{
								errorId = "msg.setter1.parms";
							}
						}
					}
					else
					{
						errorId = "msg.setter.parms";
					}
				}
				if (errorId != null)
				{
					throw Context.ReportRuntimeError1(errorId, setter.ToString());
				}
			}
			GetterSlot gslot = (GetterSlot)GetSlot(propertyName, 0, SLOT_MODIFY_GETTER_SETTER);
			gslot.SetAttributes(attributes);
			gslot.getter = getterBox;
			gslot.setter = setterBox;
		}

		/// <summary>Defines one or more properties on this object.</summary>
		/// <remarks>Defines one or more properties on this object.</remarks>
		/// <param name="cx">the current Context</param>
		/// <param name="props">a map of property ids to property descriptors</param>
		public virtual void DefineOwnProperties(Context cx, ScriptableObject props)
		{
			object[] ids = props.GetIds();
			foreach (object id in ids)
			{
				object descObj = props.Get(id);
				ScriptableObject desc = EnsureScriptableObject(descObj);
				CheckPropertyDefinition(desc);
			}
			foreach (object id_1 in ids)
			{
				ScriptableObject desc = (ScriptableObject)props.Get(id_1);
				DefineOwnProperty(cx, id_1, desc);
			}
		}

		/// <summary>Defines a property on an object.</summary>
		/// <remarks>Defines a property on an object.</remarks>
		/// <param name="cx">the current Context</param>
		/// <param name="id">the name/index of the property</param>
		/// <param name="desc">the new property descriptor, as described in 8.6.1</param>
		public virtual void DefineOwnProperty(Context cx, object id, ScriptableObject desc)
		{
			CheckPropertyDefinition(desc);
			DefineOwnProperty(cx, id, desc, true);
		}

		/// <summary>Defines a property on an object.</summary>
		/// <remarks>
		/// Defines a property on an object.
		/// Based on [[DefineOwnProperty]] from 8.12.10 of the spec.
		/// </remarks>
		/// <param name="cx">the current Context</param>
		/// <param name="id">the name/index of the property</param>
		/// <param name="desc">the new property descriptor, as described in 8.6.1</param>
		/// <param name="checkValid">whether to perform validity checks</param>
		protected internal virtual void DefineOwnProperty(Context cx, object id, ScriptableObject desc, bool checkValid)
		{
			Slot slot = GetSlot(cx, id, SLOT_QUERY);
			bool isNew = slot == null;
			if (checkValid)
			{
				ScriptableObject current = slot == null ? null : slot.GetPropertyDescriptor(cx, this);
				string name = ScriptRuntime.ToString(id);
				CheckPropertyChange(name, current, desc);
			}
			bool isAccessor = IsAccessorDescriptor(desc);
			int attributes;
			if (slot == null)
			{
				// new slot
				slot = GetSlot(cx, id, isAccessor ? SLOT_MODIFY_GETTER_SETTER : SLOT_MODIFY);
				attributes = ApplyDescriptorToAttributeBitset(DONTENUM | READONLY | PERMANENT, desc);
			}
			else
			{
				attributes = ApplyDescriptorToAttributeBitset(slot.GetAttributes(), desc);
			}
			slot = UnwrapSlot(slot);
			if (isAccessor)
			{
				if (!(slot is GetterSlot))
				{
					slot = GetSlot(cx, id, SLOT_MODIFY_GETTER_SETTER);
				}
				GetterSlot gslot = (GetterSlot)slot;
				object getter = GetProperty(desc, "get");
				if (getter != ScriptableConstants.NOT_FOUND)
				{
					gslot.getter = getter;
				}
				object setter = GetProperty(desc, "set");
				if (setter != ScriptableConstants.NOT_FOUND)
				{
					gslot.setter = setter;
				}
				gslot.value = Undefined.instance;
				gslot.SetAttributes(attributes);
			}
			else
			{
				if (slot is GetterSlot && IsDataDescriptor(desc))
				{
					slot = GetSlot(cx, id, SLOT_CONVERT_ACCESSOR_TO_DATA);
				}
				object value = GetProperty(desc, "value");
				if (value != ScriptableConstants.NOT_FOUND)
				{
					slot.value = value;
				}
				else
				{
					if (isNew)
					{
						slot.value = Undefined.instance;
					}
				}
				slot.SetAttributes(attributes);
			}
		}

		protected internal virtual void CheckPropertyDefinition(ScriptableObject desc)
		{
			object getter = GetProperty(desc, "get");
			if (getter != ScriptableConstants.NOT_FOUND && getter != Undefined.instance && !(getter is Callable))
			{
				throw ScriptRuntime.NotFunctionError(getter);
			}
			object setter = GetProperty(desc, "set");
			if (setter != ScriptableConstants.NOT_FOUND && setter != Undefined.instance && !(setter is Callable))
			{
				throw ScriptRuntime.NotFunctionError(setter);
			}
			if (IsDataDescriptor(desc) && IsAccessorDescriptor(desc))
			{
				throw ScriptRuntime.TypeError0("msg.both.data.and.accessor.desc");
			}
		}

		protected internal virtual void CheckPropertyChange(string id, ScriptableObject current, ScriptableObject desc)
		{
			if (current == null)
			{
				// new property
				if (!IsExtensible())
				{
					throw ScriptRuntime.TypeError0("msg.not.extensible");
				}
			}
			else
			{
				if (IsFalse(current.Get("configurable", current)))
				{
					if (IsTrue(GetProperty(desc, "configurable")))
					{
						throw ScriptRuntime.TypeError1("msg.change.configurable.false.to.true", id);
					}
					if (IsTrue(current.Get("enumerable", current)) != IsTrue(GetProperty(desc, "enumerable")))
					{
						throw ScriptRuntime.TypeError1("msg.change.enumerable.with.configurable.false", id);
					}
					bool isData = IsDataDescriptor(desc);
					bool isAccessor = IsAccessorDescriptor(desc);
					if (!isData && !isAccessor)
					{
					}
					else
					{
						// no further validation required for generic descriptor
						if (isData && IsDataDescriptor(current))
						{
							if (IsFalse(current.Get("writable", current)))
							{
								if (IsTrue(GetProperty(desc, "writable")))
								{
									throw ScriptRuntime.TypeError1("msg.change.writable.false.to.true.with.configurable.false", id);
								}
								if (!SameValue(GetProperty(desc, "value"), current.Get("value", current)))
								{
									throw ScriptRuntime.TypeError1("msg.change.value.with.writable.false", id);
								}
							}
						}
						else
						{
							if (isAccessor && IsAccessorDescriptor(current))
							{
								if (!SameValue(GetProperty(desc, "set"), current.Get("set", current)))
								{
									throw ScriptRuntime.TypeError1("msg.change.setter.with.configurable.false", id);
								}
								if (!SameValue(GetProperty(desc, "get"), current.Get("get", current)))
								{
									throw ScriptRuntime.TypeError1("msg.change.getter.with.configurable.false", id);
								}
							}
							else
							{
								if (IsDataDescriptor(current))
								{
									throw ScriptRuntime.TypeError1("msg.change.property.data.to.accessor.with.configurable.false", id);
								}
								else
								{
									throw ScriptRuntime.TypeError1("msg.change.property.accessor.to.data.with.configurable.false", id);
								}
							}
						}
					}
				}
			}
		}

		protected internal static bool IsTrue(object value)
		{
			return (value != ScriptableConstants.NOT_FOUND) && ScriptRuntime.ToBoolean(value);
		}

		protected internal static bool IsFalse(object value)
		{
			return !IsTrue(value);
		}

		/// <summary>
		/// Implements SameValue as described in ES5 9.12, additionally checking
		/// if new value is defined.
		/// </summary>
		/// <remarks>
		/// Implements SameValue as described in ES5 9.12, additionally checking
		/// if new value is defined.
		/// </remarks>
		/// <param name="newValue">the new value</param>
		/// <param name="currentValue">the current value</param>
		/// <returns>true if values are the same as defined by ES5 9.12</returns>
		protected internal virtual bool SameValue(object newValue, object currentValue)
		{
			if (newValue == ScriptableConstants.NOT_FOUND)
			{
				return true;
			}
			if (currentValue == ScriptableConstants.NOT_FOUND)
			{
				currentValue = Undefined.instance;
			}
			// Special rules for numbers: NaN is considered the same value,
			// while zeroes with different signs are considered different.
			if (currentValue.IsNumber() && newValue.IsNumber())
			{
				double d1 = Convert.ToDouble(currentValue);
				double d2 = Convert.ToDouble(newValue);
				if (double.IsNaN(d1) && double.IsNaN(d2))
				{
					return true;
				}
				if (d1 == 0.0 && BitConverter.DoubleToInt64Bits(d1) != BitConverter.DoubleToInt64Bits(d2))
				{
					return false;
				}
			}
			return ScriptRuntime.ShallowEq(currentValue, newValue);
		}

		protected internal virtual int ApplyDescriptorToAttributeBitset(int attributes, ScriptableObject desc)
		{
			object enumerable = GetProperty(desc, "enumerable");
			if (enumerable != ScriptableConstants.NOT_FOUND)
			{
				attributes = ScriptRuntime.ToBoolean(enumerable) ? attributes & ~DONTENUM : attributes | DONTENUM;
			}
			object writable = GetProperty(desc, "writable");
			if (writable != ScriptableConstants.NOT_FOUND)
			{
				attributes = ScriptRuntime.ToBoolean(writable) ? attributes & ~READONLY : attributes | READONLY;
			}
			object configurable = GetProperty(desc, "configurable");
			if (configurable != ScriptableConstants.NOT_FOUND)
			{
				attributes = ScriptRuntime.ToBoolean(configurable) ? attributes & ~PERMANENT : attributes | PERMANENT;
			}
			return attributes;
		}

		/// <summary>Implements IsDataDescriptor as described in ES5 8.10.2</summary>
		/// <param name="desc">a property descriptor</param>
		/// <returns>true if this is a data descriptor.</returns>
		protected internal virtual bool IsDataDescriptor(ScriptableObject desc)
		{
			return HasProperty(desc, "value") || HasProperty(desc, "writable");
		}

		/// <summary>Implements IsAccessorDescriptor as described in ES5 8.10.1</summary>
		/// <param name="desc">a property descriptor</param>
		/// <returns>true if this is an accessor descriptor.</returns>
		protected internal virtual bool IsAccessorDescriptor(ScriptableObject desc)
		{
			return HasProperty(desc, "get") || HasProperty(desc, "set");
		}

		/// <summary>Implements IsGenericDescriptor as described in ES5 8.10.3</summary>
		/// <param name="desc">a property descriptor</param>
		/// <returns>true if this is a generic descriptor.</returns>
		protected internal virtual bool IsGenericDescriptor(ScriptableObject desc)
		{
			return !IsDataDescriptor(desc) && !IsAccessorDescriptor(desc);
		}

		protected internal static Scriptable EnsureScriptable(object arg)
		{
			if (!(arg is Scriptable))
			{
				throw ScriptRuntime.TypeError1("msg.arg.not.object", ScriptRuntime.TypeOf(arg));
			}
			return (Scriptable)arg;
		}

		protected internal static ScriptableObject EnsureScriptableObject(object arg)
		{
			if (!(arg is ScriptableObject))
			{
				throw ScriptRuntime.TypeError1("msg.arg.not.object", ScriptRuntime.TypeOf(arg));
			}
			return (ScriptableObject)arg;
		}

		/// <summary>
		/// Search for names in a class, adding the resulting methods
		/// as properties.
		/// </summary>
		/// <remarks>
		/// Search for names in a class, adding the resulting methods
		/// as properties.
		/// <p> Uses reflection to find the methods of the given names. Then
		/// FunctionObjects are constructed from the methods found, and
		/// are added to this object as properties with the given names.
		/// </remarks>
		/// <param name="names">the names of the Methods to add as function properties</param>
		/// <param name="clazz">the class to search for the Methods</param>
		/// <param name="attributes">the attributes of the new properties</param>
		/// <seealso cref="FunctionObject">FunctionObject</seealso>
		public virtual void DefineFunctionProperties(string[] names, Type clazz, int attributes)
		{
			MethodInfo[] methods = FunctionObject.GetMethodList(clazz);
			for (int i = 0; i < names.Length; i++)
			{
				string name = names[i];
				MethodInfo m = FunctionObject.FindSingleMethod(methods, name);
				if (m == null)
				{
					throw Context.ReportRuntimeError2("msg.method.not.found", name, clazz.FullName);
				}
				FunctionObject f = new FunctionObject(name, m, this);
				DefineProperty(name, f, attributes);
			}
		}

		/// <summary>Get the Object.prototype property.</summary>
		/// <remarks>
		/// Get the Object.prototype property.
		/// See ECMA 15.2.4.
		/// </remarks>
		public static Scriptable GetObjectPrototype(Scriptable scope)
		{
			return TopLevel.GetBuiltinPrototype(GetTopLevelScope(scope), TopLevel.Builtins.Object);
		}

		/// <summary>Get the Function.prototype property.</summary>
		/// <remarks>
		/// Get the Function.prototype property.
		/// See ECMA 15.3.4.
		/// </remarks>
		public static Scriptable GetFunctionPrototype(Scriptable scope)
		{
			return TopLevel.GetBuiltinPrototype(GetTopLevelScope(scope), TopLevel.Builtins.Function);
		}

		public static Scriptable GetArrayPrototype(Scriptable scope)
		{
			return TopLevel.GetBuiltinPrototype(GetTopLevelScope(scope), TopLevel.Builtins.Array);
		}

		/// <summary>Get the prototype for the named class.</summary>
		/// <remarks>
		/// Get the prototype for the named class.
		/// For example, <code>getClassPrototype(s, "Date")</code> will first
		/// walk up the parent chain to find the outermost scope, then will
		/// search that scope for the Date constructor, and then will
		/// return Date.prototype. If any of the lookups fail, or
		/// the prototype is not a JavaScript object, then null will
		/// be returned.
		/// </remarks>
		/// <param name="scope">an object in the scope chain</param>
		/// <param name="className">the name of the constructor</param>
		/// <returns>
		/// the prototype for the named class, or null if it
		/// cannot be found.
		/// </returns>
		public static Scriptable GetClassPrototype(Scriptable scope, string className)
		{
			scope = GetTopLevelScope(scope);
			object ctor = GetProperty(scope, className);
			object proto;
			if (ctor is BaseFunction)
			{
				proto = ((BaseFunction)ctor).GetPrototypeProperty();
			}
			else
			{
				if (ctor is Scriptable)
				{
					Scriptable ctorObj = (Scriptable)ctor;
					proto = ctorObj.Get("prototype", ctorObj);
				}
				else
				{
					return null;
				}
			}
			if (proto is Scriptable)
			{
				return (Scriptable)proto;
			}
			return null;
		}

		/// <summary>Get the global scope.</summary>
		/// <remarks>
		/// Get the global scope.
		/// <p>Walks the parent scope chain to find an object with a null
		/// parent scope (the global object).
		/// </remarks>
		/// <param name="obj">a JavaScript object</param>
		/// <returns>the corresponding global scope</returns>
		public static Scriptable GetTopLevelScope(Scriptable obj)
		{
			for (; ; )
			{
				Scriptable parent = obj.GetParentScope();
				if (parent == null)
				{
					return obj;
				}
				obj = parent;
			}
		}

		public virtual bool IsExtensible()
		{
			return isExtensible;
		}

		public virtual void PreventExtensions()
		{
			isExtensible = false;
		}

		/// <summary>Seal this object.</summary>
		/// <remarks>
		/// Seal this object.
		/// It is an error to add properties to or delete properties from
		/// a sealed object. It is possible to change the value of an
		/// existing property. Once an object is sealed it may not be unsealed.
		/// </remarks>
		/// <since>1.4R3</since>
		public virtual void SealObject()
		{
			lock (this)
			{
				if (count >= 0)
				{
					// Make sure all LazilyLoadedCtors are initialized before sealing.
					Slot slot = firstAdded;
					while (slot != null)
					{
						object value = slot.value;
						if (value is LazilyLoadedCtor)
						{
							LazilyLoadedCtor initializer = (LazilyLoadedCtor)value;
							try
							{
								initializer.Init();
							}
							finally
							{
								slot.value = initializer.GetValue();
							}
						}
						slot = slot.orderedNext;
					}
					count = ~count;
				}
			}
		}

		/// <summary>Return true if this object is sealed.</summary>
		/// <remarks>Return true if this object is sealed.</remarks>
		/// <returns>true if sealed, false otherwise.</returns>
		/// <since>1.4R3</since>
		/// <seealso cref="SealObject()">SealObject()</seealso>
		public bool IsSealed()
		{
			return count < 0;
		}

		private void CheckNotSealed(string name, int index)
		{
			if (!IsSealed())
			{
				return;
			}
			string str = (name != null) ? name : index.ToString();
			throw Context.ReportRuntimeError1("msg.modify.sealed", str);
		}

		/// <summary>Gets a named property from an object or any object in its prototype chain.</summary>
		/// <remarks>
		/// Gets a named property from an object or any object in its prototype chain.
		/// <p>
		/// Searches the prototype chain for a property named <code>name</code>.
		/// <p>
		/// </remarks>
		/// <param name="obj">a JavaScript object</param>
		/// <param name="name">a property name</param>
		/// <returns>
		/// the value of a property with name <code>name</code> found in
		/// <code>obj</code> or any object in its prototype chain, or
		/// <code>ScriptableConstants.NOT_FOUND</code> if not found
		/// </returns>
		/// <since>1.5R2</since>
		public static object GetProperty(Scriptable obj, string name)
		{
			Scriptable start = obj;
			object result;
			do
			{
				result = obj.Get(name, start);
				if (result != ScriptableConstants.NOT_FOUND)
				{
					break;
				}
				obj = obj.GetPrototype();
			}
			while (obj != null);
			return result;
		}

		/// <summary>
		/// Gets an indexed property from an object or any object in its prototype
		/// chain and coerces it to the requested Java type.
		/// </summary>
		/// <remarks>
		/// Gets an indexed property from an object or any object in its prototype
		/// chain and coerces it to the requested Java type.
		/// <p>
		/// Searches the prototype chain for a property with integral index
		/// <code>index</code>. Note that if you wish to look for properties with numerical
		/// but non-integral indicies, you should use getProperty(Scriptable,String) with
		/// the string value of the index.
		/// <p>
		/// </remarks>
		/// <param name="s">a JavaScript object</param>
		/// <param name="index">an integral index</param>
		/// <param name="type">the required Java type of the result</param>
		/// <returns>
		/// the value of a property with name <code>name</code> found in
		/// <code>obj</code> or any object in its prototype chain, or
		/// null if not found. Note that it does not return
		/// <see cref="ScriptableConstants.NOT_FOUND">ScriptableConstants.NOT_FOUND</see>
		/// as it can ordinarily not be
		/// converted to most of the types.
		/// </returns>
		/// <since>1.7R3</since>
		public static T GetTypedProperty<T>(Scriptable s, int index)
		{
			Type type = typeof(T);
			object val = GetProperty(s, index);
			if (val == ScriptableConstants.NOT_FOUND)
			{
				val = null;
			}
			return (T) Context.JsToJava(val, type);
		}

		/// <summary>Gets an indexed property from an object or any object in its prototype chain.</summary>
		/// <remarks>
		/// Gets an indexed property from an object or any object in its prototype chain.
		/// <p>
		/// Searches the prototype chain for a property with integral index
		/// <code>index</code>. Note that if you wish to look for properties with numerical
		/// but non-integral indicies, you should use getProperty(Scriptable,String) with
		/// the string value of the index.
		/// <p>
		/// </remarks>
		/// <param name="obj">a JavaScript object</param>
		/// <param name="index">an integral index</param>
		/// <returns>
		/// the value of a property with index <code>index</code> found in
		/// <code>obj</code> or any object in its prototype chain, or
		/// <code>ScriptableConstants.NOT_FOUND</code> if not found
		/// </returns>
		/// <since>1.5R2</since>
		public static object GetProperty(Scriptable obj, int index)
		{
			Scriptable start = obj;
			object result;
			do
			{
				result = obj.Get(index, start);
				if (result != ScriptableConstants.NOT_FOUND)
				{
					break;
				}
				obj = obj.GetPrototype();
			}
			while (obj != null);
			return result;
		}

		/// <summary>
		/// Gets a named property from an object or any object in its prototype chain
		/// and coerces it to the requested Java type.
		/// </summary>
		/// <remarks>
		/// Gets a named property from an object or any object in its prototype chain
		/// and coerces it to the requested Java type.
		/// <p>
		/// Searches the prototype chain for a property named <code>name</code>.
		/// <p>
		/// </remarks>
		/// <param name="s">a JavaScript object</param>
		/// <param name="name">a property name</param>
		/// <param name="type">the required Java type of the result</param>
		/// <returns>
		/// the value of a property with name <code>name</code> found in
		/// <code>obj</code> or any object in its prototype chain, or
		/// null if not found. Note that it does not return
		/// <see cref="ScriptableConstants.NOT_FOUND">ScriptableConstants.NOT_FOUND</see>
		/// as it can ordinarily not be
		/// converted to most of the types.
		/// </returns>
		/// <since>1.7R3</since>
		public static T GetTypedProperty<T>(Scriptable s, string name)
		{
			Type type = typeof(T);
			object val = GetProperty(s, name);
			if (val == ScriptableConstants.NOT_FOUND)
			{
				val = null;
			}
			return (T) Context.JsToJava(val, type);
		}

		/// <summary>
		/// Returns whether a named property is defined in an object or any object
		/// in its prototype chain.
		/// </summary>
		/// <remarks>
		/// Returns whether a named property is defined in an object or any object
		/// in its prototype chain.
		/// <p>
		/// Searches the prototype chain for a property named <code>name</code>.
		/// <p>
		/// </remarks>
		/// <param name="obj">a JavaScript object</param>
		/// <param name="name">a property name</param>
		/// <returns>the true if property was found</returns>
		/// <since>1.5R2</since>
		public static bool HasProperty(Scriptable obj, string name)
		{
			return null != GetBase(obj, name);
		}

		/// <summary>
		/// If hasProperty(obj, name) would return true, then if the property that
		/// was found is compatible with the new property, this method just returns.
		/// </summary>
		/// <remarks>
		/// If hasProperty(obj, name) would return true, then if the property that
		/// was found is compatible with the new property, this method just returns.
		/// If the property is not compatible, then an exception is thrown.
		/// A property redefinition is incompatible if the first definition was a
		/// const declaration or if this one is.  They are compatible only if neither
		/// was const.
		/// </remarks>
		public static void RedefineProperty(Scriptable obj, string name, bool isConst)
		{
			Scriptable @base = GetBase(obj, name);
			if (@base == null)
			{
				return;
			}
			if (@base is ConstProperties)
			{
				ConstProperties cp = (ConstProperties)@base;
				if (cp.IsConst(name))
				{
					throw ScriptRuntime.TypeError1("msg.const.redecl", name);
				}
			}
			if (isConst)
			{
				throw ScriptRuntime.TypeError1("msg.var.redecl", name);
			}
		}

		/// <summary>
		/// Returns whether an indexed property is defined in an object or any object
		/// in its prototype chain.
		/// </summary>
		/// <remarks>
		/// Returns whether an indexed property is defined in an object or any object
		/// in its prototype chain.
		/// <p>
		/// Searches the prototype chain for a property with index <code>index</code>.
		/// <p>
		/// </remarks>
		/// <param name="obj">a JavaScript object</param>
		/// <param name="index">a property index</param>
		/// <returns>the true if property was found</returns>
		/// <since>1.5R2</since>
		public static bool HasProperty(Scriptable obj, int index)
		{
			return null != GetBase(obj, index);
		}

		/// <summary>Puts a named property in an object or in an object in its prototype chain.</summary>
		/// <remarks>
		/// Puts a named property in an object or in an object in its prototype chain.
		/// <p>
		/// Searches for the named property in the prototype chain. If it is found,
		/// the value of the property in <code>obj</code> is changed through a call
		/// to
		/// <see cref="Scriptable.Put(string, SIScriptable object)">Scriptable.Put(string, Scriptable, object)</see>
		/// on the
		/// prototype passing <code>obj</code> as the <code>start</code> argument.
		/// This allows the prototype to veto the property setting in case the
		/// prototype defines the property with [[ReadOnly]] attribute. If the
		/// property is not found, it is added in <code>obj</code>.
		/// </remarks>
		/// <param name="obj">a JavaScript object</param>
		/// <param name="name">a property name</param>
		/// <param name="value">any JavaScript value accepted by Scriptable.put</param>
		/// <since>1.5R2</since>
		public static void PutProperty(Scriptable obj, string name, object value)
		{
			Scriptable @base = GetBase(obj, name) ?? obj;
			@base.Put(name, obj, value);
		}

		/// <summary>Puts a named property in an object or in an object in its prototype chain.</summary>
		/// <remarks>
		/// Puts a named property in an object or in an object in its prototype chain.
		/// <p>
		/// Searches for the named property in the prototype chain. If it is found,
		/// the value of the property in <code>obj</code> is changed through a call
		/// to
		/// <see cref="Scriptable.Put(string, SIScriptable object)">Scriptable.Put(string, Scriptable, object)</see>
		/// on the
		/// prototype passing <code>obj</code> as the <code>start</code> argument.
		/// This allows the prototype to veto the property setting in case the
		/// prototype defines the property with [[ReadOnly]] attribute. If the
		/// property is not found, it is added in <code>obj</code>.
		/// </remarks>
		/// <param name="obj">a JavaScript object</param>
		/// <param name="name">a property name</param>
		/// <param name="value">any JavaScript value accepted by Scriptable.put</param>
		/// <since>1.5R2</since>
		public static void PutConstProperty(Scriptable obj, string name, object value)
		{
			Scriptable @base = GetBase(obj, name);
			if (@base == null)
			{
				@base = obj;
			}
			if (@base is ConstProperties)
			{
				((ConstProperties)@base).PutConst(name, obj, value);
			}
		}

		/// <summary>Puts an indexed property in an object or in an object in its prototype chain.</summary>
		/// <remarks>
		/// Puts an indexed property in an object or in an object in its prototype chain.
		/// <p>
		/// Searches for the indexed property in the prototype chain. If it is found,
		/// the value of the property in <code>obj</code> is changed through a call
		/// to
		/// <see cref="Scriptable.Put(int, SIScriptable object)">Scriptable.Put(int, Scriptable, object)</see>
		/// on the prototype
		/// passing <code>obj</code> as the <code>start</code> argument. This allows
		/// the prototype to veto the property setting in case the prototype defines
		/// the property with [[ReadOnly]] attribute. If the property is not found,
		/// it is added in <code>obj</code>.
		/// </remarks>
		/// <param name="obj">a JavaScript object</param>
		/// <param name="index">a property index</param>
		/// <param name="value">any JavaScript value accepted by Scriptable.put</param>
		/// <since>1.5R2</since>
		public static void PutProperty(Scriptable obj, int index, object value)
		{
			Scriptable @base = GetBase(obj, index);
			if (@base == null)
			{
				@base = obj;
			}
			@base.Put(index, obj, value);
		}

		/// <summary>Removes the property from an object or its prototype chain.</summary>
		/// <remarks>
		/// Removes the property from an object or its prototype chain.
		/// <p>
		/// Searches for a property with <code>name</code> in obj or
		/// its prototype chain. If it is found, the object's delete
		/// method is called.
		/// </remarks>
		/// <param name="obj">a JavaScript object</param>
		/// <param name="name">a property name</param>
		/// <returns>true if the property doesn't exist or was successfully removed</returns>
		/// <since>1.5R2</since>
		public static bool DeleteProperty(Scriptable obj, string name)
		{
			Scriptable @base = GetBase(obj, name);
			if (@base == null)
			{
				return true;
			}
			@base.Delete(name);
			return !@base.Has(name, obj);
		}

		/// <summary>Removes the property from an object or its prototype chain.</summary>
		/// <remarks>
		/// Removes the property from an object or its prototype chain.
		/// <p>
		/// Searches for a property with <code>index</code> in obj or
		/// its prototype chain. If it is found, the object's delete
		/// method is called.
		/// </remarks>
		/// <param name="obj">a JavaScript object</param>
		/// <param name="index">a property index</param>
		/// <returns>true if the property doesn't exist or was successfully removed</returns>
		/// <since>1.5R2</since>
		public static bool DeleteProperty(Scriptable obj, int index)
		{
			Scriptable @base = GetBase(obj, index);
			if (@base == null)
			{
				return true;
			}
			@base.Delete(index);
			return !@base.Has(index, obj);
		}

		/// <summary>Returns an array of all ids from an object and its prototypes.</summary>
		/// <remarks>
		/// Returns an array of all ids from an object and its prototypes.
		/// <p>
		/// </remarks>
		/// <param name="obj">a JavaScript object</param>
		/// <returns>
		/// an array of all ids from all object in the prototype chain.
		/// If a given id occurs multiple times in the prototype chain,
		/// it will occur only once in this list.
		/// </returns>
		/// <since>1.5R2</since>
		public static object[] GetPropertyIds(Scriptable obj)
		{
			if (obj == null)
			{
				return ScriptRuntime.emptyArgs;
			}
			object[] result = obj.GetIds();
			ObjToIntMap map = null;
			for (; ; )
			{
				obj = obj.GetPrototype();
				if (obj == null)
				{
					break;
				}
				object[] ids = obj.GetIds();
				if (ids.Length == 0)
				{
					continue;
				}
				if (map == null)
				{
					if (result.Length == 0)
					{
						result = ids;
						continue;
					}
					map = new ObjToIntMap(result.Length + ids.Length);
					for (int i = 0; i != result.Length; ++i)
					{
						map.Intern(result[i]);
					}
					result = null;
				}
				// Allow to GC the result
				for (int i_1 = 0; i_1 != ids.Length; ++i_1)
				{
					map.Intern(ids[i_1]);
				}
			}
			if (map != null)
			{
				result = map.GetKeys();
			}
			return result;
		}

		/// <summary>Call a method of an object.</summary>
		/// <remarks>Call a method of an object.</remarks>
		/// <param name="obj">the JavaScript object</param>
		/// <param name="methodName">the name of the function property</param>
		/// <param name="args">the arguments for the call</param>
		/// <seealso cref="Context.GetCurrentContext()">Context.GetCurrentContext()</seealso>
		public static object CallMethod(Scriptable obj, string methodName, object[] args)
		{
			return CallMethod(null, obj, methodName, args);
		}

		/// <summary>Call a method of an object.</summary>
		/// <remarks>Call a method of an object.</remarks>
		/// <param name="cx">the Context object associated with the current thread.</param>
		/// <param name="obj">the JavaScript object</param>
		/// <param name="methodName">the name of the function property</param>
		/// <param name="args">the arguments for the call</param>
		public static object CallMethod(Context cx, Scriptable obj, string methodName, object[] args)
		{
			object funObj = GetProperty(obj, methodName);
			if (!(funObj is Function))
			{
				throw ScriptRuntime.NotFunctionError(obj, methodName);
			}
			Function fun = (Function)funObj;
			// XXX: What should be the scope when calling funObj?
			// The following favor scope stored in the object on the assumption
			// that is more useful especially under dynamic scope setup.
			// An alternative is to check for dynamic scope flag
			// and use ScriptableObject.getTopLevelScope(fun) if the flag is not
			// set. But that require access to Context and messy code
			// so for now it is not checked.
			Scriptable scope = GetTopLevelScope(obj);
			if (cx != null)
			{
				return fun.Call(cx, scope, obj, args);
			}
			else
			{
				return Context.Call(null, fun, scope, obj, args);
			}
		}

		private static Scriptable GetBase(Scriptable obj, string name)
		{
			do
			{
				if (obj.Has(name, obj))
				{
					break;
				}
				obj = obj.GetPrototype();
			}
			while (obj != null);
			return obj;
		}

		private static Scriptable GetBase(Scriptable obj, int index)
		{
			do
			{
				if (obj.Has(index, obj))
				{
					break;
				}
				obj = obj.GetPrototype();
			}
			while (obj != null);
			return obj;
		}

		/// <summary>Get arbitrary application-specific value associated with this object.</summary>
		/// <remarks>Get arbitrary application-specific value associated with this object.</remarks>
		/// <param name="key">key object to select particular value.</param>
		/// <seealso cref="AssociateValue(object, object)">AssociateValue(object, object)</seealso>
		public object GetAssociatedValue(object key)
		{
			IDictionary<object, object> h = associatedValues;
			if (h == null)
			{
				return null;
			}
			return h.Get(key);
		}

		/// <summary>
		/// Get arbitrary application-specific value associated with the top scope
		/// of the given scope.
		/// </summary>
		/// <remarks>
		/// Get arbitrary application-specific value associated with the top scope
		/// of the given scope.
		/// The method first calls
		/// <see cref="GetTopLevelScope(Scriptable)">GetTopLevelScope(Scriptable)</see>
		/// and then searches the prototype chain of the top scope for the first
		/// object containing the associated value with the given key.
		/// </remarks>
		/// <param name="scope">the starting scope.</param>
		/// <param name="key">key object to select particular value.</param>
		/// <seealso cref="GetAssociatedValue(object)">GetAssociatedValue(object)</seealso>
		public static object GetTopScopeValue(Scriptable scope, object key)
		{
			scope = GetTopLevelScope(scope);
			for (; ; )
			{
				if (scope is ScriptableObject)
				{
					ScriptableObject so = (ScriptableObject)scope;
					object value = so.GetAssociatedValue(key);
					if (value != null)
					{
						return value;
					}
				}
				scope = scope.GetPrototype();
				if (scope == null)
				{
					return null;
				}
			}
		}

		/// <summary>Associate arbitrary application-specific value with this object.</summary>
		/// <remarks>
		/// Associate arbitrary application-specific value with this object.
		/// Value can only be associated with the given object and key only once.
		/// The method ignores any subsequent attempts to change the already
		/// associated value.
		/// <p> The associated values are not serialized.
		/// </remarks>
		/// <param name="key">key object to select particular value.</param>
		/// <param name="value">the value to associate</param>
		/// <returns>
		/// the passed value if the method is called first time for the
		/// given key or old value for any subsequent calls.
		/// </returns>
		/// <seealso cref="GetAssociatedValue(object)">GetAssociatedValue(object)</seealso>
		public object AssociateValue(object key, object value)
		{
			lock (this)
			{
				if (value == null)
				{
					throw new ArgumentException();
				}
				IDictionary<object, object> h = associatedValues;
				if (h == null)
				{
					h = new Dictionary<object, object>();
					associatedValues = h;
				}
				return Kit.InitHash(h, key, value);
			}
		}

		/// <param name="name"></param>
		/// <param name="index"></param>
		/// <param name="start"></param>
		/// <param name="value"></param>
		/// <returns>
		/// false if this != start and no slot was found.  true if this == start
		/// or this != start and a READONLY slot was found.
		/// </returns>
		private bool PutImpl(string name, int index, Scriptable start, object value)
		{
			// This method is very hot (basically called on each assignment)
			// so we inline the extensible/sealed checks below.
			Slot slot;
			if (this != start)
			{
				slot = GetSlot(name, index, SLOT_QUERY);
				if (slot == null)
				{
					return false;
				}
			}
			else
			{
				if (!isExtensible)
				{
					slot = GetSlot(name, index, SLOT_QUERY);
					if (slot == null)
					{
						return true;
					}
				}
				else
				{
					if (count < 0)
					{
						CheckNotSealed(name, index);
					}
					slot = GetSlot(name, index, SLOT_MODIFY);
				}
			}
			return slot.SetValue(value, this, start);
		}

		/// <param name="name"></param>
		/// <param name="index"></param>
		/// <param name="start"></param>
		/// <param name="value"></param>
		/// <param name="constFlag">
		/// EMPTY means normal put.  UNINITIALIZED_CONST means
		/// defineConstProperty.  READONLY means const initialization expression.
		/// </param>
		/// <returns>
		/// false if this != start and no slot was found.  true if this == start
		/// or this != start and a READONLY slot was found.
		/// </returns>
		private bool PutConstImpl(string name, int index, Scriptable start, object value, int constFlag)
		{
			System.Diagnostics.Debug.Assert((constFlag != EMPTY));
			Slot slot;
			if (this != start)
			{
				slot = GetSlot(name, index, SLOT_QUERY);
				if (slot == null)
				{
					return false;
				}
			}
			else
			{
				if (!IsExtensible())
				{
					slot = GetSlot(name, index, SLOT_QUERY);
					if (slot == null)
					{
						return true;
					}
				}
				else
				{
					CheckNotSealed(name, index);
					// either const hoisted declaration or initialization
					slot = UnwrapSlot(GetSlot(name, index, SLOT_MODIFY_CONST));
					int attr = slot.GetAttributes();
					if ((attr & READONLY) == 0)
					{
						throw Context.ReportRuntimeError1("msg.var.redecl", name);
					}
					if ((attr & UNINITIALIZED_CONST) != 0)
					{
						slot.value = value;
						// clear the bit on const initialization
						if (constFlag != UNINITIALIZED_CONST)
						{
							slot.SetAttributes(attr & ~UNINITIALIZED_CONST);
						}
					}
					return true;
				}
			}
			return slot.SetValue(value, this, start);
		}

		private Slot FindAttributeSlot(string name, int index, int accessType)
		{
			Slot slot = GetSlot(name, index, accessType);
			if (slot == null)
			{
				string str = (name != null ? name : index.ToString());
				throw Context.ReportRuntimeError1("msg.prop.not.found", str);
			}
			return slot;
		}

		private static Slot UnwrapSlot(Slot slot)
		{
			return (slot is RelinkedSlot) ? ((RelinkedSlot)slot).slot : slot;
		}

		/// <summary>Locate the slot with given name or index.</summary>
		/// <remarks>
		/// Locate the slot with given name or index. Depending on the accessType
		/// parameter and the current slot status, a new slot may be allocated.
		/// </remarks>
		/// <param name="name">property name or null if slot holds spare array index.</param>
		/// <param name="index">index or 0 if slot holds property name.</param>
		private Slot GetSlot(string name, int index, int accessType)
		{
			// Check the hashtable without using synchronization
			Slot[] slotsLocalRef = slots;
			// Get stable local reference
			if (slotsLocalRef == null && accessType == SLOT_QUERY)
			{
				return null;
			}
			int indexOrHash = (name != null ? name.GetHashCode() : index);
			if (slotsLocalRef != null)
			{
				Slot slot;
				int slotIndex = GetSlotIndex(slotsLocalRef.Length, indexOrHash);
				for (slot = slotsLocalRef[slotIndex]; slot != null; slot = slot.next)
				{
					object sname = slot.name;
					if (indexOrHash == slot.indexOrHash && (sname == name || (name != null && name.Equals(sname))))
					{
						break;
					}
				}
				switch (accessType)
				{
					case SLOT_QUERY:
					{
						return slot;
					}

					case SLOT_MODIFY:
					case SLOT_MODIFY_CONST:
					{
						if (slot != null)
						{
							return slot;
						}
						break;
					}

					case SLOT_MODIFY_GETTER_SETTER:
					{
						slot = UnwrapSlot(slot);
						if (slot is GetterSlot)
						{
							return slot;
						}
						break;
					}

					case SLOT_CONVERT_ACCESSOR_TO_DATA:
					{
						slot = UnwrapSlot(slot);
						if (!(slot is GetterSlot))
						{
							return slot;
						}
						break;
					}
				}
			}
			// A new slot has to be inserted or the old has to be replaced
			// by GetterSlot. Time to synchronize.
			return CreateSlot(name, indexOrHash, accessType);
		}

		private Slot CreateSlot(string name, int indexOrHash, int accessType)
		{
			lock (this)
			{
				Slot[] slotsLocalRef = slots;
				int insertPos;
				if (count == 0)
				{
					// Always throw away old slots if any on empty insert.
					slotsLocalRef = new Slot[INITIAL_SLOT_SIZE];
					slots = slotsLocalRef;
					insertPos = GetSlotIndex(slotsLocalRef.Length, indexOrHash);
				}
				else
				{
					int tableSize = slotsLocalRef.Length;
					insertPos = GetSlotIndex(tableSize, indexOrHash);
					Slot prev = slotsLocalRef[insertPos];
					Slot slot = prev;
					while (slot != null)
					{
						if (slot.indexOrHash == indexOrHash && (slot.name == name || (name != null && name.Equals(slot.name))))
						{
							break;
						}
						prev = slot;
						slot = slot.next;
					}
					if (slot != null)
					{
						// A slot with same name/index already exists. This means that
						// a slot is being redefined from a value to a getter slot or
						// vice versa, or it could be a race in application code.
						// Check if we need to replace the slot depending on the
						// accessType flag and return the appropriate slot instance.
						Slot inner = UnwrapSlot(slot);
						Slot newSlot;
						if (accessType == SLOT_MODIFY_GETTER_SETTER && !(inner is GetterSlot))
						{
							newSlot = new GetterSlot(name, indexOrHash, inner.GetAttributes());
						}
						else
						{
							if (accessType == SLOT_CONVERT_ACCESSOR_TO_DATA && (inner is GetterSlot))
							{
								newSlot = new Slot(name, indexOrHash, inner.GetAttributes());
							}
							else
							{
								if (accessType == SLOT_MODIFY_CONST)
								{
									return null;
								}
								else
								{
									return inner;
								}
							}
						}
						newSlot.value = inner.value;
						newSlot.next = slot.next;
						// add new slot to linked list
						if (lastAdded != null)
						{
							lastAdded.orderedNext = newSlot;
						}
						if (firstAdded == null)
						{
							firstAdded = newSlot;
						}
						lastAdded = newSlot;
						// add new slot to hash table
						if (prev == slot)
						{
							slotsLocalRef[insertPos] = newSlot;
						}
						else
						{
							prev.next = newSlot;
						}
						// other housekeeping
						slot.MarkDeleted();
						return newSlot;
					}
					else
					{
						// Check if the table is not too full before inserting.
						if (4 * (count + 1) > 3 * slotsLocalRef.Length)
						{
							// table size must be a power of 2, always grow by x2
							slotsLocalRef = new Slot[slotsLocalRef.Length * 2];
							CopyTable(slots, slotsLocalRef, count);
							slots = slotsLocalRef;
							insertPos = GetSlotIndex(slotsLocalRef.Length, indexOrHash);
						}
					}
				}
				Slot newSlot_1 = (accessType == SLOT_MODIFY_GETTER_SETTER ? new GetterSlot(name, indexOrHash, 0) : new Slot(name, indexOrHash, 0));
				if (accessType == SLOT_MODIFY_CONST)
				{
					newSlot_1.SetAttributes(CONST);
				}
				++count;
				// add new slot to linked list
				if (lastAdded != null)
				{
					lastAdded.orderedNext = newSlot_1;
				}
				if (firstAdded == null)
				{
					firstAdded = newSlot_1;
				}
				lastAdded = newSlot_1;
				// add new slot to hash table, return it
				AddKnownAbsentSlot(slotsLocalRef, newSlot_1, insertPos);
				return newSlot_1;
			}
		}

		private void RemoveSlot(string name, int index)
		{
			lock (this)
			{
				int indexOrHash = (name != null ? name.GetHashCode() : index);
				Slot[] slotsLocalRef = slots;
				if (count != 0)
				{
					int tableSize = slotsLocalRef.Length;
					int slotIndex = GetSlotIndex(tableSize, indexOrHash);
					Slot prev = slotsLocalRef[slotIndex];
					Slot slot = prev;
					while (slot != null)
					{
						if (slot.indexOrHash == indexOrHash && (slot.name == name || (name != null && name.Equals(slot.name))))
						{
							break;
						}
						prev = slot;
						slot = slot.next;
					}
					if (slot != null && (slot.GetAttributes() & PERMANENT) == 0)
					{
						count--;
						// remove slot from hash table
						if (prev == slot)
						{
							slotsLocalRef[slotIndex] = slot.next;
						}
						else
						{
							prev.next = slot.next;
						}
						// remove from ordered list. Previously this was done lazily in
						// getIds() but delete is an infrequent operation so O(n)
						// should be ok
						// ordered list always uses the actual slot
						Slot deleted = UnwrapSlot(slot);
						if (deleted == firstAdded)
						{
							prev = null;
							firstAdded = deleted.orderedNext;
						}
						else
						{
							prev = firstAdded;
							while (prev.orderedNext != deleted)
							{
								prev = prev.orderedNext;
							}
							prev.orderedNext = deleted.orderedNext;
						}
						if (deleted == lastAdded)
						{
							lastAdded = prev;
						}
						// Mark the slot as removed.
						slot.MarkDeleted();
					}
				}
			}
		}

		private static int GetSlotIndex(int tableSize, int indexOrHash)
		{
			// tableSize is a power of 2
			return indexOrHash & (tableSize - 1);
		}

		// Must be inside synchronized (this)
		private static void CopyTable(Slot[] oldSlots, Slot[] newSlots, int count)
		{
			if (count == 0)
			{
				throw Kit.CodeBug();
			}
			int tableSize = newSlots.Length;
			int i = oldSlots.Length;
			for (; ; )
			{
				--i;
				Slot slot = oldSlots[i];
				while (slot != null)
				{
					int insertPos = GetSlotIndex(tableSize, slot.indexOrHash);
					// If slot has next chain in old table use a new
					// RelinkedSlot wrapper to keep old table valid
					Slot insSlot = slot.next == null ? slot : new RelinkedSlot(slot);
					AddKnownAbsentSlot(newSlots, insSlot, insertPos);
					slot = slot.next;
					if (--count == 0)
					{
						return;
					}
				}
			}
		}

		/// <summary>Add slot with keys that are known to absent from the table.</summary>
		/// <remarks>
		/// Add slot with keys that are known to absent from the table.
		/// This is an optimization to use when inserting into empty table,
		/// after table growth or during deserialization.
		/// </remarks>
		private static void AddKnownAbsentSlot(Slot[] slots, Slot slot, int insertPos)
		{
			if (slots[insertPos] == null)
			{
				slots[insertPos] = slot;
			}
			else
			{
				Slot prev = slots[insertPos];
				Slot next = prev.next;
				while (next != null)
				{
					prev = next;
					next = prev.next;
				}
				prev.next = slot;
			}
		}

		internal virtual object[] GetIds(bool getAll)
		{
			Slot[] s = slots;
			object[] a = ScriptRuntime.emptyArgs;
			if (s == null)
			{
				return a;
			}
			int c = 0;
			Slot slot = firstAdded;
			while (slot != null && slot.wasDeleted)
			{
				// we used to removed deleted slots from the linked list here
				// but this is now done in removeSlot(). There may still be deleted
				// slots (e.g. from slot conversion) but we don't want to mess
				// with the list in unsynchronized code.
				slot = slot.orderedNext;
			}
			while (slot != null)
			{
				if (getAll || (slot.GetAttributes() & DONTENUM) == 0)
				{
					if (c == 0)
					{
						a = new object[s.Length];
					}
					a[c++] = slot.name ?? (object) slot.indexOrHash;
				}
				slot = slot.orderedNext;
				while (slot != null && slot.wasDeleted)
				{
					// skip deleted slots, see comment above
					slot = slot.orderedNext;
				}
			}
			if (c == a.Length)
			{
				return a;
			}
			object[] result = new object[c];
			Array.Copy(a, 0, result, 0, c);
			return result;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteObject(ObjectOutputStream @out)
		{
			lock (this)
			{
				@out.DefaultWriteObject();
				int objectsCount = count;
				if (objectsCount < 0)
				{
					// "this" was sealed
					objectsCount = ~objectsCount;
				}
				if (objectsCount == 0)
				{
					@out.WriteInt(0);
				}
				else
				{
					@out.WriteInt(slots.Length);
					Slot slot = firstAdded;
					while (slot != null && slot.wasDeleted)
					{
						// as long as we're traversing the order-added linked list,
						// remove deleted slots
						slot = slot.orderedNext;
					}
					firstAdded = slot;
					while (slot != null)
					{
						@out.WriteObject(slot);
						Slot next = slot.orderedNext;
						while (next != null && next.wasDeleted)
						{
							// remove deleted slots
							next = next.orderedNext;
						}
						slot.orderedNext = next;
						slot = next;
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.TypeLoadException"></exception>
		private void ReadObject(ObjectInputStream @in)
		{
			@in.DefaultReadObject();
			int tableSize = @in.ReadInt();
			if (tableSize != 0)
			{
				// If tableSize is not a power of 2 find the closest
				// power of 2 >= the original size.
				if ((tableSize & (tableSize - 1)) != 0)
				{
					if (tableSize > 1 << 30)
					{
						throw new Exception("Property table overflow");
					}
					int newSize = INITIAL_SLOT_SIZE;
					while (newSize < tableSize)
					{
						newSize <<= 1;
					}
					tableSize = newSize;
				}
				slots = new Slot[tableSize];
				int objectsCount = count;
				if (objectsCount < 0)
				{
					// "this" was sealed
					objectsCount = ~objectsCount;
				}
				Slot prev = null;
				for (int i = 0; i != objectsCount; ++i)
				{
					lastAdded = (Slot)@in.ReadObject();
					if (i == 0)
					{
						firstAdded = lastAdded;
					}
					else
					{
						prev.orderedNext = lastAdded;
					}
					int slotIndex = GetSlotIndex(tableSize, lastAdded.indexOrHash);
					AddKnownAbsentSlot(slots, lastAdded, slotIndex);
					prev = lastAdded;
				}
			}
		}

		protected internal virtual ScriptableObject GetOwnPropertyDescriptor(Context cx, object id)
		{
			Slot slot = GetSlot(cx, id, SLOT_QUERY);
			if (slot == null)
			{
				return null;
			}
			Scriptable scope = GetParentScope();
			return slot.GetPropertyDescriptor(cx, scope ?? this);
		}

		protected internal virtual Slot GetSlot(Context cx, object id, int accessType)
		{
			string name = ScriptRuntime.ToStringIdOrIndex(cx, id);
			if (name == null)
			{
				return GetSlot(null, ScriptRuntime.LastIndexResult(cx), accessType);
			}
			else
			{
				return GetSlot(name, 0, accessType);
			}
		}

		// Partial implementation of java.util.Map. See NativeObject for
		// a subclass that implements java.util.Map.
		public virtual int Size()
		{
			return count < 0 ? ~count : count;
		}

		public virtual bool IsEmpty()
		{
			return count == 0 || count == -1;
		}

		public virtual object Get(object key)
		{
			object value = null;
			if (key is string)
			{
				value = Get((string)key, this);
			}
			else
			{
				if (key.IsNumber())
				{
					value = Get(Convert.ToInt32(key), this);
				}
			}
			if (value == ScriptableConstants.NOT_FOUND || value == Undefined.instance)
			{
				return null;
			}
			else
			{
				if (value is Wrapper)
				{
					return ((Wrapper)value).Unwrap();
				}
				else
				{
					return value;
				}
			}
		}
	}
}
