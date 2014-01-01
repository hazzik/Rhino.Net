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
using System.Reflection;
using System.Text;
using Rhino;
using Sharpen;

namespace Rhino
{
	/// <summary>This class implements the Array native object.</summary>
	/// <remarks>This class implements the Array native object.</remarks>
	/// <author>Norris Boyd</author>
	/// <author>Mike McCabe</author>
	[System.Serializable]
	public class NativeArray : IdScriptableObject, IList
	{
		internal const long serialVersionUID = 7331366857676127338L;

		private static readonly object ARRAY_TAG = "Array";

		private static readonly int NEGATIVE_ONE = Sharpen.Extensions.ValueOf(-1);

		internal static void Init(Scriptable scope, bool @sealed)
		{
			Rhino.NativeArray obj = new Rhino.NativeArray(0);
			obj.ExportAsJSClass(MAX_PROTOTYPE_ID, scope, @sealed);
		}

		internal static int GetMaximumInitialCapacity()
		{
			return maximumInitialCapacity;
		}

		internal static void SetMaximumInitialCapacity(int maximumInitialCapacity)
		{
			Rhino.NativeArray.maximumInitialCapacity = maximumInitialCapacity;
		}

		public NativeArray(long lengthArg)
		{
			denseOnly = lengthArg <= maximumInitialCapacity;
			if (denseOnly)
			{
				int intLength = (int)lengthArg;
				if (intLength < DEFAULT_INITIAL_CAPACITY)
				{
					intLength = DEFAULT_INITIAL_CAPACITY;
				}
				dense = new object[intLength];
				Arrays.Fill(dense, ScriptableConstants.NOT_FOUND);
			}
			length = lengthArg;
		}

		public NativeArray(object[] array)
		{
			denseOnly = true;
			dense = array;
			length = array.Length;
		}

		public override string GetClassName()
		{
			return "Array";
		}

		private const int Id_length = 1;

		private const int MAX_INSTANCE_ID = 1;

		protected internal override int GetMaxInstanceId()
		{
			return MAX_INSTANCE_ID;
		}

		protected internal override void SetInstanceIdAttributes(int id, int attr)
		{
			if (id == Id_length)
			{
				lengthAttr = attr;
			}
		}

		protected internal override int FindInstanceIdInfo(string s)
		{
			if (s.Equals("length"))
			{
				return InstanceIdInfo(lengthAttr, Id_length);
			}
			return base.FindInstanceIdInfo(s);
		}

		protected internal override string GetInstanceIdName(int id)
		{
			if (id == Id_length)
			{
				return "length";
			}
			return base.GetInstanceIdName(id);
		}

		protected internal override object GetInstanceIdValue(int id)
		{
			if (id == Id_length)
			{
				return ScriptRuntime.WrapNumber(length);
			}
			return base.GetInstanceIdValue(id);
		}

		protected internal override void SetInstanceIdValue(int id, object value)
		{
			if (id == Id_length)
			{
				SetLength(value);
				return;
			}
			base.SetInstanceIdValue(id, value);
		}

		protected internal override void FillConstructorProperties(IdFunctionObject ctor)
		{
			AddIdFunctionProperty(ctor, ARRAY_TAG, ConstructorId_join, "join", 1);
			AddIdFunctionProperty(ctor, ARRAY_TAG, ConstructorId_reverse, "reverse", 0);
			AddIdFunctionProperty(ctor, ARRAY_TAG, ConstructorId_sort, "sort", 1);
			AddIdFunctionProperty(ctor, ARRAY_TAG, ConstructorId_push, "push", 1);
			AddIdFunctionProperty(ctor, ARRAY_TAG, ConstructorId_pop, "pop", 0);
			AddIdFunctionProperty(ctor, ARRAY_TAG, ConstructorId_shift, "shift", 0);
			AddIdFunctionProperty(ctor, ARRAY_TAG, ConstructorId_unshift, "unshift", 1);
			AddIdFunctionProperty(ctor, ARRAY_TAG, ConstructorId_splice, "splice", 2);
			AddIdFunctionProperty(ctor, ARRAY_TAG, ConstructorId_concat, "concat", 1);
			AddIdFunctionProperty(ctor, ARRAY_TAG, ConstructorId_slice, "slice", 2);
			AddIdFunctionProperty(ctor, ARRAY_TAG, ConstructorId_indexOf, "indexOf", 1);
			AddIdFunctionProperty(ctor, ARRAY_TAG, ConstructorId_lastIndexOf, "lastIndexOf", 1);
			AddIdFunctionProperty(ctor, ARRAY_TAG, ConstructorId_every, "every", 1);
			AddIdFunctionProperty(ctor, ARRAY_TAG, ConstructorId_filter, "filter", 1);
			AddIdFunctionProperty(ctor, ARRAY_TAG, ConstructorId_forEach, "forEach", 1);
			AddIdFunctionProperty(ctor, ARRAY_TAG, ConstructorId_map, "map", 1);
			AddIdFunctionProperty(ctor, ARRAY_TAG, ConstructorId_some, "some", 1);
			AddIdFunctionProperty(ctor, ARRAY_TAG, ConstructorId_reduce, "reduce", 1);
			AddIdFunctionProperty(ctor, ARRAY_TAG, ConstructorId_reduceRight, "reduceRight", 1);
			AddIdFunctionProperty(ctor, ARRAY_TAG, ConstructorId_isArray, "isArray", 1);
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

				case Id_toSource:
				{
					arity = 0;
					s = "toSource";
					break;
				}

				case Id_join:
				{
					arity = 1;
					s = "join";
					break;
				}

				case Id_reverse:
				{
					arity = 0;
					s = "reverse";
					break;
				}

				case Id_sort:
				{
					arity = 1;
					s = "sort";
					break;
				}

				case Id_push:
				{
					arity = 1;
					s = "push";
					break;
				}

				case Id_pop:
				{
					arity = 0;
					s = "pop";
					break;
				}

				case Id_shift:
				{
					arity = 0;
					s = "shift";
					break;
				}

				case Id_unshift:
				{
					arity = 1;
					s = "unshift";
					break;
				}

				case Id_splice:
				{
					arity = 2;
					s = "splice";
					break;
				}

				case Id_concat:
				{
					arity = 1;
					s = "concat";
					break;
				}

				case Id_slice:
				{
					arity = 2;
					s = "slice";
					break;
				}

				case Id_indexOf:
				{
					arity = 1;
					s = "indexOf";
					break;
				}

				case Id_lastIndexOf:
				{
					arity = 1;
					s = "lastIndexOf";
					break;
				}

				case Id_every:
				{
					arity = 1;
					s = "every";
					break;
				}

				case Id_filter:
				{
					arity = 1;
					s = "filter";
					break;
				}

				case Id_forEach:
				{
					arity = 1;
					s = "forEach";
					break;
				}

				case Id_map:
				{
					arity = 1;
					s = "map";
					break;
				}

				case Id_some:
				{
					arity = 1;
					s = "some";
					break;
				}

				case Id_reduce:
				{
					arity = 1;
					s = "reduce";
					break;
				}

				case Id_reduceRight:
				{
					arity = 1;
					s = "reduceRight";
					break;
				}

				default:
				{
					throw new ArgumentException(id.ToString());
				}
			}
			InitPrototypeMethod(ARRAY_TAG, id, s, arity);
		}

		public override object ExecIdCall(IdFunctionObject f, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (!f.HasTag(ARRAY_TAG))
			{
				return base.ExecIdCall(f, cx, scope, thisObj, args);
			}
			int id = f.MethodId();
			for (; ; )
			{
				switch (id)
				{
					case ConstructorId_join:
					case ConstructorId_reverse:
					case ConstructorId_sort:
					case ConstructorId_push:
					case ConstructorId_pop:
					case ConstructorId_shift:
					case ConstructorId_unshift:
					case ConstructorId_splice:
					case ConstructorId_concat:
					case ConstructorId_slice:
					case ConstructorId_indexOf:
					case ConstructorId_lastIndexOf:
					case ConstructorId_every:
					case ConstructorId_filter:
					case ConstructorId_forEach:
					case ConstructorId_map:
					case ConstructorId_some:
					case ConstructorId_reduce:
					case ConstructorId_reduceRight:
					{
						if (args.Length > 0)
						{
							thisObj = ScriptRuntime.ToObject(scope, args[0]);
							object[] newArgs = new object[args.Length - 1];
							for (int i = 0; i < newArgs.Length; i++)
							{
								newArgs[i] = args[i + 1];
							}
							args = newArgs;
						}
						id = -id;
						goto again_continue;
					}

					case ConstructorId_isArray:
					{
						return args.Length > 0 && (args[0] is Rhino.NativeArray);
					}

					case Id_constructor:
					{
						bool inNewExpr = (thisObj == null);
						if (!inNewExpr)
						{
							// IdFunctionObject.construct will set up parent, proto
							return f.Construct(cx, scope, args);
						}
						return JsConstructor(cx, scope, args);
					}

					case Id_toString:
					{
						return ToStringHelper(cx, scope, thisObj, cx.HasFeature(Context.FEATURE_TO_STRING_AS_SOURCE), false);
					}

					case Id_toLocaleString:
					{
						return ToStringHelper(cx, scope, thisObj, false, true);
					}

					case Id_toSource:
					{
						return ToStringHelper(cx, scope, thisObj, true, false);
					}

					case Id_join:
					{
						return Js_join(cx, thisObj, args);
					}

					case Id_reverse:
					{
						return Js_reverse(cx, thisObj, args);
					}

					case Id_sort:
					{
						return Js_sort(cx, scope, thisObj, args);
					}

					case Id_push:
					{
						return Js_push(cx, thisObj, args);
					}

					case Id_pop:
					{
						return Js_pop(cx, thisObj, args);
					}

					case Id_shift:
					{
						return Js_shift(cx, thisObj, args);
					}

					case Id_unshift:
					{
						return Js_unshift(cx, thisObj, args);
					}

					case Id_splice:
					{
						return Js_splice(cx, scope, thisObj, args);
					}

					case Id_concat:
					{
						return Js_concat(cx, scope, thisObj, args);
					}

					case Id_slice:
					{
						return Js_slice(cx, thisObj, args);
					}

					case Id_indexOf:
					{
						return IndexOfHelper(cx, thisObj, args, false);
					}

					case Id_lastIndexOf:
					{
						return IndexOfHelper(cx, thisObj, args, true);
					}

					case Id_every:
					case Id_filter:
					case Id_forEach:
					case Id_map:
					case Id_some:
					{
						return IterativeMethod(cx, id, scope, thisObj, args);
					}

					case Id_reduce:
					case Id_reduceRight:
					{
						return ReduceMethod(cx, id, scope, thisObj, args);
					}
				}
				throw new ArgumentException(id.ToString());
again_continue: ;
			}
again_break: ;
		}

		public override object Get(int index, Scriptable start)
		{
			if (!denseOnly && IsGetterOrSetter(null, index, false))
			{
				return base.Get(index, start);
			}
			if (dense != null && 0 <= index && index < dense.Length)
			{
				return dense[index];
			}
			return base.Get(index, start);
		}

		public override bool Has(int index, Scriptable start)
		{
			if (!denseOnly && IsGetterOrSetter(null, index, false))
			{
				return base.Has(index, start);
			}
			if (dense != null && 0 <= index && index < dense.Length)
			{
				return dense[index] != ScriptableConstants.NOT_FOUND;
			}
			return base.Has(index, start);
		}

		private static long ToArrayIndex(object id)
		{
			if (id is string)
			{
				return ToArrayIndex((string)id);
			}
			else
			{
				if (id is Number)
				{
					return ToArrayIndex(System.Convert.ToDouble(((Number)id)));
				}
			}
			return -1;
		}

		// if id is an array index (ECMA 15.4.0), return the number,
		// otherwise return -1L
		private static long ToArrayIndex(string id)
		{
			long index = ToArrayIndex(ScriptRuntime.ToNumber(id));
			// Assume that ScriptRuntime.toString(index) is the same
			// as java.lang.Long.toString(index) for long
			if (System.Convert.ToString(index).Equals(id))
			{
				return index;
			}
			return -1;
		}

		private static long ToArrayIndex(double d)
		{
			if (d == d)
			{
				long index = ScriptRuntime.ToUint32(d);
				if (index == d && index != 4294967295L)
				{
					return index;
				}
			}
			return -1;
		}

		private static int ToDenseIndex(object id)
		{
			long index = ToArrayIndex(id);
			return 0 <= index && index < int.MaxValue ? (int)index : -1;
		}

		public override void Put(string id, Scriptable start, object value)
		{
			base.Put(id, start, value);
			if (start == this)
			{
				// If the object is sealed, super will throw exception
				long index = ToArrayIndex(id);
				if (index >= length)
				{
					length = index + 1;
					denseOnly = false;
				}
			}
		}

		private bool EnsureCapacity(int capacity)
		{
			if (capacity > dense.Length)
			{
				if (capacity > MAX_PRE_GROW_SIZE)
				{
					denseOnly = false;
					return false;
				}
				capacity = Math.Max(capacity, (int)(dense.Length * GROW_FACTOR));
				object[] newDense = new object[capacity];
				System.Array.Copy(dense, 0, newDense, 0, dense.Length);
				Arrays.Fill(newDense, dense.Length, newDense.Length, ScriptableConstants.NOT_FOUND);
				dense = newDense;
			}
			return true;
		}

		public override void Put(int index, Scriptable start, object value)
		{
			if (start == this && !IsSealed() && dense != null && 0 <= index && (denseOnly || !IsGetterOrSetter(null, index, true)))
			{
				if (index < dense.Length)
				{
					dense[index] = value;
					if (this.length <= index)
					{
						this.length = (long)index + 1;
					}
					return;
				}
				else
				{
					if (denseOnly && index < dense.Length * GROW_FACTOR && EnsureCapacity(index + 1))
					{
						dense[index] = value;
						this.length = (long)index + 1;
						return;
					}
					else
					{
						denseOnly = false;
					}
				}
			}
			base.Put(index, start, value);
			if (start == this && (lengthAttr & READONLY) == 0)
			{
				// only set the array length if given an array index (ECMA 15.4.0)
				if (this.length <= index)
				{
					// avoid overflowing index!
					this.length = (long)index + 1;
				}
			}
		}

		public override void Delete(int index)
		{
			if (dense != null && 0 <= index && index < dense.Length && !IsSealed() && (denseOnly || !IsGetterOrSetter(null, index, true)))
			{
				dense[index] = ScriptableConstants.NOT_FOUND;
			}
			else
			{
				base.Delete(index);
			}
		}

		public override object[] GetIds()
		{
			object[] superIds = base.GetIds();
			if (dense == null)
			{
				return superIds;
			}
			int N = dense.Length;
			long currentLength = length;
			if (N > currentLength)
			{
				N = (int)currentLength;
			}
			if (N == 0)
			{
				return superIds;
			}
			int superLength = superIds.Length;
			object[] ids = new object[N + superLength];
			int presentCount = 0;
			for (int i = 0; i != N; ++i)
			{
				// Replace existing elements by their indexes
				if (dense[i] != ScriptableConstants.NOT_FOUND)
				{
					ids[presentCount] = Sharpen.Extensions.ValueOf(i);
					++presentCount;
				}
			}
			if (presentCount != N)
			{
				// dense contains deleted elems, need to shrink the result
				object[] tmp = new object[presentCount + superLength];
				System.Array.Copy(ids, 0, tmp, 0, presentCount);
				ids = tmp;
			}
			System.Array.Copy(superIds, 0, ids, presentCount, superLength);
			return ids;
		}

		public override object[] GetAllIds()
		{
			ICollection<object> allIds = new LinkedHashSet<object>(Arrays.AsList(this.GetIds()));
			Sharpen.Collections.AddAll(allIds, Arrays.AsList(base.GetAllIds()));
			return Sharpen.Collections.ToArray(allIds);
		}

		public virtual int[] GetIndexIds()
		{
			object[] ids = GetIds();
			IList<int> indices = new List<int>(ids.Length);
			foreach (object id in ids)
			{
				int int32Id = ScriptRuntime.ToInt32(id);
				if (int32Id >= 0 && ScriptRuntime.ToString(int32Id).Equals(ScriptRuntime.ToString(id)))
				{
					indices.AddItem(int32Id);
				}
			}
			return Sharpen.Collections.ToArray(indices, new int[indices.Count]);
		}

		public override object GetDefaultValue(Type hint)
		{
			if (hint == ScriptRuntime.NumberClass)
			{
				Context cx = Context.GetContext();
				if (cx.GetLanguageVersion() == Context.VERSION_1_2)
				{
					return Sharpen.Extensions.ValueOf(length);
				}
			}
			return base.GetDefaultValue(hint);
		}

		private ScriptableObject DefaultIndexPropertyDescriptor(object value)
		{
			Scriptable scope = GetParentScope();
			if (scope == null)
			{
				scope = this;
			}
			ScriptableObject desc = new NativeObject();
			ScriptRuntime.SetBuiltinProtoAndParent(desc, scope, TopLevel.Builtins.Object);
			desc.DefineProperty("value", value, EMPTY);
			desc.DefineProperty("writable", true, EMPTY);
			desc.DefineProperty("enumerable", true, EMPTY);
			desc.DefineProperty("configurable", true, EMPTY);
			return desc;
		}

		public override int GetAttributes(int index)
		{
			if (dense != null && index >= 0 && index < dense.Length && dense[index] != ScriptableConstants.NOT_FOUND)
			{
				return EMPTY;
			}
			return base.GetAttributes(index);
		}

		protected internal override ScriptableObject GetOwnPropertyDescriptor(Context cx, object id)
		{
			if (dense != null)
			{
				int index = ToDenseIndex(id);
				if (0 <= index && index < dense.Length && dense[index] != ScriptableConstants.NOT_FOUND)
				{
					object value = dense[index];
					return DefaultIndexPropertyDescriptor(value);
				}
			}
			return base.GetOwnPropertyDescriptor(cx, id);
		}

		protected internal override void DefineOwnProperty(Context cx, object id, ScriptableObject desc, bool checkValid)
		{
			if (dense != null)
			{
				object[] values = dense;
				dense = null;
				denseOnly = false;
				for (int i = 0; i < values.Length; i++)
				{
					if (values[i] != ScriptableConstants.NOT_FOUND)
					{
						Put(i, this, values[i]);
					}
				}
			}
			long index = ToArrayIndex(id);
			if (index >= length)
			{
				length = index + 1;
			}
			base.DefineOwnProperty(cx, id, desc, checkValid);
		}

		/// <summary>See ECMA 15.4.1,2</summary>
		private static object JsConstructor(Context cx, Scriptable scope, object[] args)
		{
			if (args.Length == 0)
			{
				return new Rhino.NativeArray(0);
			}
			// Only use 1 arg as first element for version 1.2; for
			// any other version (including 1.3) follow ECMA and use it as
			// a length.
			if (cx.GetLanguageVersion() == Context.VERSION_1_2)
			{
				return new Rhino.NativeArray(args);
			}
			else
			{
				object arg0 = args[0];
				if (args.Length > 1 || !(arg0 is Number))
				{
					return new Rhino.NativeArray(args);
				}
				else
				{
					long len = ScriptRuntime.ToUint32(arg0);
					if (len != System.Convert.ToDouble(((Number)arg0)))
					{
						string msg = ScriptRuntime.GetMessage0("msg.arraylength.bad");
						throw ScriptRuntime.ConstructError("RangeError", msg);
					}
					return new Rhino.NativeArray(len);
				}
			}
		}

		public virtual long GetLength()
		{
			return length;
		}

		[System.ObsoleteAttribute(@"Use GetLength() instead.")]
		public virtual long JsGet_length()
		{
			return GetLength();
		}

		/// <summary>
		/// Change the value of the internal flag that determines whether all
		/// storage is handed by a dense backing array rather than an associative
		/// store.
		/// </summary>
		/// <remarks>
		/// Change the value of the internal flag that determines whether all
		/// storage is handed by a dense backing array rather than an associative
		/// store.
		/// </remarks>
		/// <param name="denseOnly">new value for denseOnly flag</param>
		/// <exception cref="System.ArgumentException">
		/// if an attempt is made to enable
		/// denseOnly after it was disabled; NativeArray code is not written
		/// to handle switching back to a dense representation
		/// </exception>
		internal virtual void SetDenseOnly(bool denseOnly)
		{
			if (denseOnly && !this.denseOnly)
			{
				throw new ArgumentException();
			}
			this.denseOnly = denseOnly;
		}

		private void SetLength(object val)
		{
			if ((lengthAttr & READONLY) != 0)
			{
				return;
			}
			double d = ScriptRuntime.ToNumber(val);
			long longVal = ScriptRuntime.ToUint32(d);
			if (longVal != d)
			{
				string msg = ScriptRuntime.GetMessage0("msg.arraylength.bad");
				throw ScriptRuntime.ConstructError("RangeError", msg);
			}
			if (denseOnly)
			{
				if (longVal < length)
				{
					// downcast okay because denseOnly
					Arrays.Fill(dense, (int)longVal, dense.Length, ScriptableConstants.NOT_FOUND);
					length = longVal;
					return;
				}
				else
				{
					if (longVal < MAX_PRE_GROW_SIZE && longVal < (length * GROW_FACTOR) && EnsureCapacity((int)longVal))
					{
						length = longVal;
						return;
					}
					else
					{
						denseOnly = false;
					}
				}
			}
			if (longVal < length)
			{
				// remove all properties between longVal and length
				if (length - longVal > unchecked((int)(0x1000)))
				{
					// assume that the representation is sparse
					object[] e = GetIds();
					// will only find in object itself
					for (int i = 0; i < e.Length; i++)
					{
						object id = e[i];
						if (id is string)
						{
							// > MAXINT will appear as string
							string strId = (string)id;
							long index = ToArrayIndex(strId);
							if (index >= longVal)
							{
								Delete(strId);
							}
						}
						else
						{
							int index = System.Convert.ToInt32(((int)id));
							if (index >= longVal)
							{
								Delete(index);
							}
						}
					}
				}
				else
				{
					// assume a dense representation
					for (long i = longVal; i < length; i++)
					{
						DeleteElem(this, i);
					}
				}
			}
			length = longVal;
		}

		internal static long GetLengthProperty(Context cx, Scriptable obj)
		{
			// These will both give numeric lengths within Uint32 range.
			if (obj is NativeString)
			{
				return ((NativeString)obj).GetLength();
			}
			else
			{
				if (obj is Rhino.NativeArray)
				{
					return ((Rhino.NativeArray)obj).GetLength();
				}
			}
			return ScriptRuntime.ToUint32(ScriptRuntime.GetObjectProp(obj, "length", cx));
		}

		private static object SetLengthProperty(Context cx, Scriptable target, long length)
		{
			return ScriptRuntime.SetObjectProp(target, "length", ScriptRuntime.WrapNumber(length), cx);
		}

		private static void DeleteElem(Scriptable target, long index)
		{
			int i = (int)index;
			if (i == index)
			{
				target.Delete(i);
			}
			else
			{
				target.Delete(System.Convert.ToString(index));
			}
		}

		private static object GetElem(Context cx, Scriptable target, long index)
		{
			if (index > int.MaxValue)
			{
				string id = System.Convert.ToString(index);
				return ScriptRuntime.GetObjectProp(target, id, cx);
			}
			else
			{
				return ScriptRuntime.GetObjectIndex(target, (int)index, cx);
			}
		}

		// same as getElem, but without converting NOT_FOUND to undefined
		private static object GetRawElem(Scriptable target, long index)
		{
			if (index > int.MaxValue)
			{
				return ScriptableObject.GetProperty(target, System.Convert.ToString(index));
			}
			else
			{
				return ScriptableObject.GetProperty(target, (int)index);
			}
		}

		private static void SetElem(Context cx, Scriptable target, long index, object value)
		{
			if (index > int.MaxValue)
			{
				string id = System.Convert.ToString(index);
				ScriptRuntime.SetObjectProp(target, id, value, cx);
			}
			else
			{
				ScriptRuntime.SetObjectIndex(target, (int)index, value, cx);
			}
		}

		// Similar as setElem(), but triggers deleteElem() if value is NOT_FOUND
		private static void SetRawElem(Context cx, Scriptable target, long index, object value)
		{
			if (value == ScriptableConstants.NOT_FOUND)
			{
				DeleteElem(target, index);
			}
			else
			{
				SetElem(cx, target, index, value);
			}
		}

		private static string ToStringHelper(Context cx, Scriptable scope, Scriptable thisObj, bool toSource, bool toLocale)
		{
			long length = GetLengthProperty(cx, thisObj);
			StringBuilder result = new StringBuilder(256);
			// whether to return '4,unquoted,5' or '[4, "quoted", 5]'
			string separator;
			if (toSource)
			{
				result.Append('[');
				separator = ", ";
			}
			else
			{
				separator = ",";
			}
			bool haslast = false;
			long i = 0;
			bool toplevel;
			bool iterating;
			if (cx.iterating == null)
			{
				toplevel = true;
				iterating = false;
				cx.iterating = new ObjToIntMap(31);
			}
			else
			{
				toplevel = false;
				iterating = cx.iterating.Has(thisObj);
			}
			// Make sure cx.iterating is set to null when done
			// so we don't leak memory
			try
			{
				if (!iterating)
				{
					cx.iterating.Put(thisObj, 0);
					// stop recursion.
					// make toSource print null and undefined values in recent versions
					bool skipUndefinedAndNull = !toSource || cx.GetLanguageVersion() < Context.VERSION_1_5;
					for (i = 0; i < length; i++)
					{
						if (i > 0)
						{
							result.Append(separator);
						}
						object elem = GetRawElem(thisObj, i);
						if (elem == ScriptableConstants.NOT_FOUND || (skipUndefinedAndNull && (elem == null || elem == Undefined.instance)))
						{
							haslast = false;
							continue;
						}
						haslast = true;
						if (toSource)
						{
							result.Append(ScriptRuntime.Uneval(cx, scope, elem));
						}
						else
						{
							if (elem is string)
							{
								string s = (string)elem;
								if (toSource)
								{
									result.Append('\"');
									result.Append(ScriptRuntime.EscapeString(s));
									result.Append('\"');
								}
								else
								{
									result.Append(s);
								}
							}
							else
							{
								if (toLocale)
								{
									Callable fun;
									Scriptable funThis;
									fun = ScriptRuntime.GetPropFunctionAndThis(elem, "toLocaleString", cx);
									funThis = ScriptRuntime.LastStoredScriptable(cx);
									elem = fun.Call(cx, scope, funThis, ScriptRuntime.emptyArgs);
								}
								result.Append(ScriptRuntime.ToString(elem));
							}
						}
					}
				}
			}
			finally
			{
				if (toplevel)
				{
					cx.iterating = null;
				}
			}
			if (toSource)
			{
				//for [,,].length behavior; we want toString to be symmetric.
				if (!haslast && i > 0)
				{
					result.Append(", ]");
				}
				else
				{
					result.Append(']');
				}
			}
			return result.ToString();
		}

		/// <summary>See ECMA 15.4.4.3</summary>
		private static string Js_join(Context cx, Scriptable thisObj, object[] args)
		{
			long llength = GetLengthProperty(cx, thisObj);
			int length = (int)llength;
			if (llength != length)
			{
				throw Context.ReportRuntimeError1("msg.arraylength.too.big", llength.ToString());
			}
			// if no args, use "," as separator
			string separator = (args.Length < 1 || args[0] == Undefined.instance) ? "," : ScriptRuntime.ToString(args[0]);
			if (thisObj is Rhino.NativeArray)
			{
				Rhino.NativeArray na = (Rhino.NativeArray)thisObj;
				if (na.denseOnly)
				{
					StringBuilder sb = new StringBuilder();
					for (int i = 0; i < length; i++)
					{
						if (i != 0)
						{
							sb.Append(separator);
						}
						if (i < na.dense.Length)
						{
							object temp = na.dense[i];
							if (temp != null && temp != Undefined.instance && temp != ScriptableConstants.NOT_FOUND)
							{
								sb.Append(ScriptRuntime.ToString(temp));
							}
						}
					}
					return sb.ToString();
				}
			}
			if (length == 0)
			{
				return string.Empty;
			}
			string[] buf = new string[length];
			int total_size = 0;
			for (int i_1 = 0; i_1 != length; i_1++)
			{
				object temp = GetElem(cx, thisObj, i_1);
				if (temp != null && temp != Undefined.instance)
				{
					string str = ScriptRuntime.ToString(temp);
					total_size += str.Length;
					buf[i_1] = str;
				}
			}
			total_size += (length - 1) * separator.Length;
			StringBuilder sb_1 = new StringBuilder(total_size);
			for (int i_2 = 0; i_2 != length; i_2++)
			{
				if (i_2 != 0)
				{
					sb_1.Append(separator);
				}
				string str = buf[i_2];
				if (str != null)
				{
					// str == null for undefined or null
					sb_1.Append(str);
				}
			}
			return sb_1.ToString();
		}

		/// <summary>See ECMA 15.4.4.4</summary>
		private static Scriptable Js_reverse(Context cx, Scriptable thisObj, object[] args)
		{
			if (thisObj is Rhino.NativeArray)
			{
				Rhino.NativeArray na = (Rhino.NativeArray)thisObj;
				if (na.denseOnly)
				{
					for (int i = 0, j = ((int)na.length) - 1; i < j; i++, j--)
					{
						object temp = na.dense[i];
						na.dense[i] = na.dense[j];
						na.dense[j] = temp;
					}
					return thisObj;
				}
			}
			long len = GetLengthProperty(cx, thisObj);
			long half = len / 2;
			for (long i_1 = 0; i_1 < half; i_1++)
			{
				long j = len - i_1 - 1;
				object temp1 = GetRawElem(thisObj, i_1);
				object temp2 = GetRawElem(thisObj, j);
				SetRawElem(cx, thisObj, i_1, temp2);
				SetRawElem(cx, thisObj, j, temp1);
			}
			return thisObj;
		}

		/// <summary>See ECMA 15.4.4.5</summary>
		private static Scriptable Js_sort(Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			IComparer<object> comparator;
			if (args.Length > 0 && Undefined.instance != args[0])
			{
				Callable jsCompareFunction = ScriptRuntime.GetValueFunctionAndThis(args[0], cx);
				Scriptable funThis = ScriptRuntime.LastStoredScriptable(cx);
				object[] cmpBuf = new object[2];
				// Buffer for cmp arguments
				comparator = new _IComparer_969(cmpBuf, jsCompareFunction, cx, scope, funThis);
			}
			else
			{
				// sort undefined to end
				// ??? double and 0???
				comparator = new _IComparer_996();
			}
			// sort undefined to end
			long llength = GetLengthProperty(cx, thisObj);
			int length = (int)llength;
			if (llength != length)
			{
				throw Context.ReportRuntimeError1("msg.arraylength.too.big", llength.ToString());
			}
			// copy the JS array into a working array, so it can be
			// sorted cheaply.
			object[] working = new object[length];
			for (int i = 0; i != length; ++i)
			{
				working[i] = GetRawElem(thisObj, i);
			}
			Arrays.Sort(working, comparator);
			// copy the working array back into thisObj
			for (int i_1 = 0; i_1 < length; ++i_1)
			{
				SetRawElem(cx, thisObj, i_1, working[i_1]);
			}
			return thisObj;
		}

		private sealed class _IComparer_969 : IComparer<object>
		{
			public _IComparer_969(object[] cmpBuf, Callable jsCompareFunction, Context cx, Scriptable scope, Scriptable funThis)
			{
				this.cmpBuf = cmpBuf;
				this.jsCompareFunction = jsCompareFunction;
				this.cx = cx;
				this.scope = scope;
				this.funThis = funThis;
			}

			public int Compare(object x, object y)
			{
				if (x == Scriptable.NOT_FOUND)
				{
					return y == Scriptable.NOT_FOUND ? 0 : 1;
				}
				else
				{
					if (y == Scriptable.NOT_FOUND)
					{
						return -1;
					}
					else
					{
						if (x == Undefined.instance)
						{
							return y == Undefined.instance ? 0 : 1;
						}
						else
						{
							if (y == Undefined.instance)
							{
								return -1;
							}
						}
					}
				}
				cmpBuf[0] = x;
				cmpBuf[1] = y;
				object ret = jsCompareFunction.Call(cx, scope, funThis, cmpBuf);
				double d = ScriptRuntime.ToNumber(ret);
				if (d < 0)
				{
					return -1;
				}
				else
				{
					if (d > 0)
					{
						return +1;
					}
				}
				return 0;
			}

			private readonly object[] cmpBuf;

			private readonly Callable jsCompareFunction;

			private readonly Context cx;

			private readonly Scriptable scope;

			private readonly Scriptable funThis;
		}

		private sealed class _IComparer_996 : IComparer<object>
		{
			public _IComparer_996()
			{
			}

			public int Compare(object x, object y)
			{
				if (x == Scriptable.NOT_FOUND)
				{
					return y == Scriptable.NOT_FOUND ? 0 : 1;
				}
				else
				{
					if (y == Scriptable.NOT_FOUND)
					{
						return -1;
					}
					else
					{
						if (x == Undefined.instance)
						{
							return y == Undefined.instance ? 0 : 1;
						}
						else
						{
							if (y == Undefined.instance)
							{
								return -1;
							}
						}
					}
				}
				string a = ScriptRuntime.ToString(x);
				string b = ScriptRuntime.ToString(y);
				return string.CompareOrdinal(a, b);
			}
		}

		/// <summary>Non-ECMA methods.</summary>
		/// <remarks>Non-ECMA methods.</remarks>
		private static object Js_push(Context cx, Scriptable thisObj, object[] args)
		{
			if (thisObj is Rhino.NativeArray)
			{
				Rhino.NativeArray na = (Rhino.NativeArray)thisObj;
				if (na.denseOnly && na.EnsureCapacity((int)na.length + args.Length))
				{
					for (int i = 0; i < args.Length; i++)
					{
						na.dense[(int)na.length++] = args[i];
					}
					return ScriptRuntime.WrapNumber(na.length);
				}
			}
			long length = GetLengthProperty(cx, thisObj);
			for (int i_1 = 0; i_1 < args.Length; i_1++)
			{
				SetElem(cx, thisObj, length + i_1, args[i_1]);
			}
			length += args.Length;
			object lengthObj = SetLengthProperty(cx, thisObj, length);
			if (cx.GetLanguageVersion() == Context.VERSION_1_2)
			{
				// if JS1.2 && no arguments, return undefined.
				return args.Length == 0 ? Undefined.instance : args[args.Length - 1];
			}
			else
			{
				return lengthObj;
			}
		}

		private static object Js_pop(Context cx, Scriptable thisObj, object[] args)
		{
			object result;
			if (thisObj is Rhino.NativeArray)
			{
				Rhino.NativeArray na = (Rhino.NativeArray)thisObj;
				if (na.denseOnly && na.length > 0)
				{
					na.length--;
					result = na.dense[(int)na.length];
					na.dense[(int)na.length] = ScriptableConstants.NOT_FOUND;
					return result;
				}
			}
			long length = GetLengthProperty(cx, thisObj);
			if (length > 0)
			{
				length--;
				// Get the to-be-deleted property's value.
				result = GetElem(cx, thisObj, length);
			}
			else
			{
				// We don't need to delete the last property, because
				// setLength does that for us.
				result = Undefined.instance;
			}
			// necessary to match js even when length < 0; js pop will give a
			// length property to any target it is called on.
			SetLengthProperty(cx, thisObj, length);
			return result;
		}

		private static object Js_shift(Context cx, Scriptable thisObj, object[] args)
		{
			if (thisObj is Rhino.NativeArray)
			{
				Rhino.NativeArray na = (Rhino.NativeArray)thisObj;
				if (na.denseOnly && na.length > 0)
				{
					na.length--;
					object result = na.dense[0];
					System.Array.Copy(na.dense, 1, na.dense, 0, (int)na.length);
					na.dense[(int)na.length] = ScriptableConstants.NOT_FOUND;
					return result == ScriptableConstants.NOT_FOUND ? Undefined.instance : result;
				}
			}
			object result_1;
			long length = GetLengthProperty(cx, thisObj);
			if (length > 0)
			{
				long i = 0;
				length--;
				// Get the to-be-deleted property's value.
				result_1 = GetElem(cx, thisObj, i);
				if (length > 0)
				{
					for (i = 1; i <= length; i++)
					{
						object temp = GetRawElem(thisObj, i);
						SetRawElem(cx, thisObj, i - 1, temp);
					}
				}
			}
			else
			{
				// We don't need to delete the last property, because
				// setLength does that for us.
				result_1 = Undefined.instance;
			}
			SetLengthProperty(cx, thisObj, length);
			return result_1;
		}

		private static object Js_unshift(Context cx, Scriptable thisObj, object[] args)
		{
			if (thisObj is Rhino.NativeArray)
			{
				Rhino.NativeArray na = (Rhino.NativeArray)thisObj;
				if (na.denseOnly && na.EnsureCapacity((int)na.length + args.Length))
				{
					System.Array.Copy(na.dense, 0, na.dense, args.Length, (int)na.length);
					for (int i = 0; i < args.Length; i++)
					{
						na.dense[i] = args[i];
					}
					na.length += args.Length;
					return ScriptRuntime.WrapNumber(na.length);
				}
			}
			long length = GetLengthProperty(cx, thisObj);
			int argc = args.Length;
			if (args.Length > 0)
			{
				if (length > 0)
				{
					for (long last = length - 1; last >= 0; last--)
					{
						object temp = GetRawElem(thisObj, last);
						SetRawElem(cx, thisObj, last + argc, temp);
					}
				}
				for (int i = 0; i < args.Length; i++)
				{
					SetElem(cx, thisObj, i, args[i]);
				}
				length += args.Length;
				return SetLengthProperty(cx, thisObj, length);
			}
			return ScriptRuntime.WrapNumber(length);
		}

		private static object Js_splice(Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			Rhino.NativeArray na = null;
			bool denseMode = false;
			if (thisObj is Rhino.NativeArray)
			{
				na = (Rhino.NativeArray)thisObj;
				denseMode = na.denseOnly;
			}
			scope = GetTopLevelScope(scope);
			int argc = args.Length;
			if (argc == 0)
			{
				return cx.NewArray(scope, 0);
			}
			long length = GetLengthProperty(cx, thisObj);
			long begin = ToSliceIndex(ScriptRuntime.ToInteger(args[0]), length);
			argc--;
			long count;
			if (args.Length == 1)
			{
				count = length - begin;
			}
			else
			{
				double dcount = ScriptRuntime.ToInteger(args[1]);
				if (dcount < 0)
				{
					count = 0;
				}
				else
				{
					if (dcount > (length - begin))
					{
						count = length - begin;
					}
					else
					{
						count = (long)dcount;
					}
				}
				argc--;
			}
			long end = begin + count;
			object result;
			if (count != 0)
			{
				if (count == 1 && (cx.GetLanguageVersion() == Context.VERSION_1_2))
				{
					result = GetElem(cx, thisObj, begin);
				}
				else
				{
					if (denseMode)
					{
						int intLen = (int)(end - begin);
						object[] copy = new object[intLen];
						System.Array.Copy(na.dense, (int)begin, copy, 0, intLen);
						result = cx.NewArray(scope, copy);
					}
					else
					{
						Scriptable resultArray = cx.NewArray(scope, 0);
						for (long last = begin; last != end; last++)
						{
							object temp = GetRawElem(thisObj, last);
							if (temp != ScriptableConstants.NOT_FOUND)
							{
								SetElem(cx, resultArray, last - begin, temp);
							}
						}
						// Need to set length for sparse result array
						SetLengthProperty(cx, resultArray, end - begin);
						result = resultArray;
					}
				}
			}
			else
			{
				// (count == 0)
				if (cx.GetLanguageVersion() == Context.VERSION_1_2)
				{
					result = Undefined.instance;
				}
				else
				{
					result = cx.NewArray(scope, 0);
				}
			}
			long delta = argc - count;
			if (denseMode && length + delta < int.MaxValue && na.EnsureCapacity((int)(length + delta)))
			{
				System.Array.Copy(na.dense, (int)end, na.dense, (int)(begin + argc), (int)(length - end));
				if (argc > 0)
				{
					System.Array.Copy(args, 2, na.dense, (int)begin, argc);
				}
				if (delta < 0)
				{
					Arrays.Fill(na.dense, (int)(length + delta), (int)length, ScriptableConstants.NOT_FOUND);
				}
				na.length = length + delta;
				return result;
			}
			if (delta > 0)
			{
				for (long last = length - 1; last >= end; last--)
				{
					object temp = GetRawElem(thisObj, last);
					SetRawElem(cx, thisObj, last + delta, temp);
				}
			}
			else
			{
				if (delta < 0)
				{
					for (long last = end; last < length; last++)
					{
						object temp = GetRawElem(thisObj, last);
						SetRawElem(cx, thisObj, last + delta, temp);
					}
				}
			}
			int argoffset = args.Length - argc;
			for (int i = 0; i < argc; i++)
			{
				SetElem(cx, thisObj, begin + i, args[i + argoffset]);
			}
			SetLengthProperty(cx, thisObj, length + delta);
			return result;
		}

		private static Scriptable Js_concat(Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			// create an empty Array to return.
			scope = GetTopLevelScope(scope);
			Function ctor = ScriptRuntime.GetExistingCtor(cx, scope, "Array");
			Scriptable result = ctor.Construct(cx, scope, ScriptRuntime.emptyArgs);
			if (thisObj is Rhino.NativeArray && result is Rhino.NativeArray)
			{
				Rhino.NativeArray denseThis = (Rhino.NativeArray)thisObj;
				Rhino.NativeArray denseResult = (Rhino.NativeArray)result;
				if (denseThis.denseOnly && denseResult.denseOnly)
				{
					// First calculate length of resulting array
					bool canUseDense = true;
					int length = (int)denseThis.length;
					for (int i = 0; i < args.Length && canUseDense; i++)
					{
						if (args[i] is Rhino.NativeArray)
						{
							// only try to use dense approach for Array-like
							// objects that are actually NativeArrays
							Rhino.NativeArray arg = (Rhino.NativeArray)args[i];
							canUseDense = arg.denseOnly;
							length += arg.length;
						}
						else
						{
							length++;
						}
					}
					if (canUseDense && denseResult.EnsureCapacity(length))
					{
						System.Array.Copy(denseThis.dense, 0, denseResult.dense, 0, (int)denseThis.length);
						int cursor = (int)denseThis.length;
						for (int i_1 = 0; i_1 < args.Length && canUseDense; i_1++)
						{
							if (args[i_1] is Rhino.NativeArray)
							{
								Rhino.NativeArray arg = (Rhino.NativeArray)args[i_1];
								System.Array.Copy(arg.dense, 0, denseResult.dense, cursor, (int)arg.length);
								cursor += (int)arg.length;
							}
							else
							{
								denseResult.dense[cursor++] = args[i_1];
							}
						}
						denseResult.length = length;
						return result;
					}
				}
			}
			long length_1;
			long slot = 0;
			if (ScriptRuntime.InstanceOf(thisObj, ctor, cx))
			{
				length_1 = GetLengthProperty(cx, thisObj);
				// Copy from the target object into the result
				for (slot = 0; slot < length_1; slot++)
				{
					object temp = GetRawElem(thisObj, slot);
					if (temp != ScriptableConstants.NOT_FOUND)
					{
						SetElem(cx, result, slot, temp);
					}
				}
			}
			else
			{
				SetElem(cx, result, slot++, thisObj);
			}
			for (int i_2 = 0; i_2 < args.Length; i_2++)
			{
				if (ScriptRuntime.InstanceOf(args[i_2], ctor, cx))
				{
					// ScriptRuntime.instanceOf => instanceof Scriptable
					Scriptable arg = (Scriptable)args[i_2];
					length_1 = GetLengthProperty(cx, arg);
					for (long j = 0; j < length_1; j++, slot++)
					{
						object temp = GetRawElem(arg, j);
						if (temp != ScriptableConstants.NOT_FOUND)
						{
							SetElem(cx, result, slot, temp);
						}
					}
				}
				else
				{
					SetElem(cx, result, slot++, args[i_2]);
				}
			}
			SetLengthProperty(cx, result, slot);
			return result;
		}

		private Scriptable Js_slice(Context cx, Scriptable thisObj, object[] args)
		{
			Scriptable scope = GetTopLevelScope(this);
			Scriptable result = cx.NewArray(scope, 0);
			long length = GetLengthProperty(cx, thisObj);
			long begin;
			long end;
			if (args.Length == 0)
			{
				begin = 0;
				end = length;
			}
			else
			{
				begin = ToSliceIndex(ScriptRuntime.ToInteger(args[0]), length);
				if (args.Length == 1)
				{
					end = length;
				}
				else
				{
					end = ToSliceIndex(ScriptRuntime.ToInteger(args[1]), length);
				}
			}
			for (long slot = begin; slot < end; slot++)
			{
				object temp = GetRawElem(thisObj, slot);
				if (temp != ScriptableConstants.NOT_FOUND)
				{
					SetElem(cx, result, slot - begin, temp);
				}
			}
			SetLengthProperty(cx, result, Math.Max(0, end - begin));
			return result;
		}

		private static long ToSliceIndex(double value, long length)
		{
			long result;
			if (value < 0.0)
			{
				if (value + length < 0.0)
				{
					result = 0;
				}
				else
				{
					result = (long)(value + length);
				}
			}
			else
			{
				if (value > length)
				{
					result = length;
				}
				else
				{
					result = (long)value;
				}
			}
			return result;
		}

		/// <summary>Implements the methods "indexOf" and "lastIndexOf".</summary>
		/// <remarks>Implements the methods "indexOf" and "lastIndexOf".</remarks>
		private object IndexOfHelper(Context cx, Scriptable thisObj, object[] args, bool isLast)
		{
			object compareTo = args.Length > 0 ? args[0] : Undefined.instance;
			long length = GetLengthProperty(cx, thisObj);
			long start;
			if (isLast)
			{
				// lastIndexOf
				if (args.Length < 2)
				{
					// default
					start = length - 1;
				}
				else
				{
					start = (long)ScriptRuntime.ToInteger(args[1]);
					if (start >= length)
					{
						start = length - 1;
					}
					else
					{
						if (start < 0)
						{
							start += length;
						}
					}
					if (start < 0)
					{
						return NEGATIVE_ONE;
					}
				}
			}
			else
			{
				// indexOf
				if (args.Length < 2)
				{
					// default
					start = 0;
				}
				else
				{
					start = (long)ScriptRuntime.ToInteger(args[1]);
					if (start < 0)
					{
						start += length;
						if (start < 0)
						{
							start = 0;
						}
					}
					if (start > length - 1)
					{
						return NEGATIVE_ONE;
					}
				}
			}
			if (thisObj is Rhino.NativeArray)
			{
				Rhino.NativeArray na = (Rhino.NativeArray)thisObj;
				if (na.denseOnly)
				{
					if (isLast)
					{
						for (int i = (int)start; i >= 0; i--)
						{
							if (na.dense[i] != ScriptableConstants.NOT_FOUND && ScriptRuntime.ShallowEq(na.dense[i], compareTo))
							{
								return Sharpen.Extensions.ValueOf(i);
							}
						}
					}
					else
					{
						for (int i = (int)start; i < length; i++)
						{
							if (na.dense[i] != ScriptableConstants.NOT_FOUND && ScriptRuntime.ShallowEq(na.dense[i], compareTo))
							{
								return Sharpen.Extensions.ValueOf(i);
							}
						}
					}
					return NEGATIVE_ONE;
				}
			}
			if (isLast)
			{
				for (long i = start; i >= 0; i--)
				{
					object val = GetRawElem(thisObj, i);
					if (val != ScriptableConstants.NOT_FOUND && ScriptRuntime.ShallowEq(val, compareTo))
					{
						return Sharpen.Extensions.ValueOf(i);
					}
				}
			}
			else
			{
				for (long i = start; i < length; i++)
				{
					object val = GetRawElem(thisObj, i);
					if (val != ScriptableConstants.NOT_FOUND && ScriptRuntime.ShallowEq(val, compareTo))
					{
						return Sharpen.Extensions.ValueOf(i);
					}
				}
			}
			return NEGATIVE_ONE;
		}

		/// <summary>Implements the methods "every", "filter", "forEach", "map", and "some".</summary>
		/// <remarks>Implements the methods "every", "filter", "forEach", "map", and "some".</remarks>
		private object IterativeMethod(Context cx, int id, Scriptable scope, Scriptable thisObj, object[] args)
		{
			object callbackArg = args.Length > 0 ? args[0] : Undefined.instance;
			if (callbackArg == null || !(callbackArg is Function))
			{
				throw ScriptRuntime.NotFunctionError(callbackArg);
			}
			Function f = (Function)callbackArg;
			Scriptable parent = ScriptableObject.GetTopLevelScope(f);
			Scriptable thisArg;
			if (args.Length < 2 || args[1] == null || args[1] == Undefined.instance)
			{
				thisArg = parent;
			}
			else
			{
				thisArg = ScriptRuntime.ToObject(cx, scope, args[1]);
			}
			long length = GetLengthProperty(cx, thisObj);
			int resultLength = id == Id_map ? (int)length : 0;
			Scriptable array = cx.NewArray(scope, resultLength);
			long j = 0;
			for (long i = 0; i < length; i++)
			{
				object[] innerArgs = new object[3];
				object elem = GetRawElem(thisObj, i);
				if (elem == ScriptableConstants.NOT_FOUND)
				{
					continue;
				}
				innerArgs[0] = elem;
				innerArgs[1] = Sharpen.Extensions.ValueOf(i);
				innerArgs[2] = thisObj;
				object result = f.Call(cx, parent, thisArg, innerArgs);
				switch (id)
				{
					case Id_every:
					{
						if (!ScriptRuntime.ToBoolean(result))
						{
							return false;
						}
						break;
					}

					case Id_filter:
					{
						if (ScriptRuntime.ToBoolean(result))
						{
							SetElem(cx, array, j++, innerArgs[0]);
						}
						break;
					}

					case Id_forEach:
					{
						break;
					}

					case Id_map:
					{
						SetElem(cx, array, i, result);
						break;
					}

					case Id_some:
					{
						if (ScriptRuntime.ToBoolean(result))
						{
							return true;
						}
						break;
					}
				}
			}
			switch (id)
			{
				case Id_every:
				{
					return true;
				}

				case Id_filter:
				case Id_map:
				{
					return array;
				}

				case Id_some:
				{
					return false;
				}

				case Id_forEach:
				default:
				{
					return Undefined.instance;
				}
			}
		}

		/// <summary>Implements the methods "reduce" and "reduceRight".</summary>
		/// <remarks>Implements the methods "reduce" and "reduceRight".</remarks>
		private object ReduceMethod(Context cx, int id, Scriptable scope, Scriptable thisObj, object[] args)
		{
			object callbackArg = args.Length > 0 ? args[0] : Undefined.instance;
			if (callbackArg == null || !(callbackArg is Function))
			{
				throw ScriptRuntime.NotFunctionError(callbackArg);
			}
			Function f = (Function)callbackArg;
			Scriptable parent = ScriptableObject.GetTopLevelScope(f);
			long length = GetLengthProperty(cx, thisObj);
			// hack to serve both reduce and reduceRight with the same loop
			bool movingLeft = id == Id_reduce;
			object value = args.Length > 1 ? args[1] : ScriptableConstants.NOT_FOUND;
			for (long i = 0; i < length; i++)
			{
				long index = movingLeft ? i : (length - 1 - i);
				object elem = GetRawElem(thisObj, index);
				if (elem == ScriptableConstants.NOT_FOUND)
				{
					continue;
				}
				if (value == ScriptableConstants.NOT_FOUND)
				{
					// no initial value passed, use first element found as inital value
					value = elem;
				}
				else
				{
					object[] innerArgs = new object[] { value, elem, index, thisObj };
					value = f.Call(cx, parent, parent, innerArgs);
				}
			}
			if (value == ScriptableConstants.NOT_FOUND)
			{
				// reproduce spidermonkey error message
				throw ScriptRuntime.TypeError0("msg.empty.array.reduce");
			}
			return value;
		}

		// methods to implement java.util.List
		public virtual bool Contains(object o)
		{
			return IndexOf(o) > -1;
		}

		public virtual object[] ToArray()
		{
			return Sharpen.Collections.ToArray(this, ScriptRuntime.emptyArgs);
		}

		public virtual object[] ToArray(object[] a)
		{
			long longLen = length;
			if (longLen > int.MaxValue)
			{
				throw new InvalidOperationException();
			}
			int len = (int)longLen;
			object[] array = a.Length >= len ? a : (object[])System.Array.CreateInstance(a.GetType().GetElementType(), len);
			for (int i = 0; i < len; i++)
			{
				array[i] = this[i];
			}
			return array;
		}

		public virtual bool ContainsAll(ICollection c)
		{
			foreach (object aC in c)
			{
				if (!Contains(aC))
				{
					return false;
				}
			}
			return true;
		}

		public override int Size()
		{
			long longLen = length;
			if (longLen > int.MaxValue)
			{
				throw new InvalidOperationException();
			}
			return (int)longLen;
		}

		public override bool IsEmpty()
		{
			return length == 0;
		}

		public virtual object Get(long index)
		{
			if (index < 0 || index >= length)
			{
				throw new IndexOutOfRangeException();
			}
			object value = GetRawElem(this, index);
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

		public virtual object Get(int index)
		{
			return Get((long)index);
		}

		public virtual int IndexOf(object o)
		{
			long longLen = length;
			if (longLen > int.MaxValue)
			{
				throw new InvalidOperationException();
			}
			int len = (int)longLen;
			if (o == null)
			{
				for (int i = 0; i < len; i++)
				{
					if (this[i] == null)
					{
						return i;
					}
				}
			}
			else
			{
				for (int i = 0; i < len; i++)
				{
					if (o.Equals(this[i]))
					{
						return i;
					}
				}
			}
			return -1;
		}

		public virtual int LastIndexOf(object o)
		{
			long longLen = length;
			if (longLen > int.MaxValue)
			{
				throw new InvalidOperationException();
			}
			int len = (int)longLen;
			if (o == null)
			{
				for (int i = len - 1; i >= 0; i--)
				{
					if (this[i] == null)
					{
						return i;
					}
				}
			}
			else
			{
				for (int i = len - 1; i >= 0; i--)
				{
					if (o.Equals(this[i]))
					{
						return i;
					}
				}
			}
			return -1;
		}

		public virtual IEnumerator GetEnumerator()
		{
			return ListIterator(0);
		}

		public virtual Sharpen.ListIterator ListIterator()
		{
			return ListIterator(0);
		}

		public virtual Sharpen.ListIterator ListIterator(int start)
		{
			long longLen = length;
			if (longLen > int.MaxValue)
			{
				throw new InvalidOperationException();
			}
			int len = (int)longLen;
			if (start < 0 || start > len)
			{
				throw new IndexOutOfRangeException("Index: " + start);
			}
			return new _ListIterator_1788(start, len);
		}

		private sealed class _ListIterator_1788 : Sharpen.ListIterator
		{
			public _ListIterator_1788(int start, int len)
			{
				this.start = start;
				this.len = len;
				this.cursor = start;
			}

			internal int cursor;

			public bool HasNext()
			{
				return this.cursor < len;
			}

			public object Next()
			{
				if (this.cursor == len)
				{
					throw new NoSuchElementException();
				}
				return this[this.cursor++];
			}

			public bool HasPrevious()
			{
				return this.cursor > 0;
			}

			public object Previous()
			{
				if (this.cursor == 0)
				{
					throw new NoSuchElementException();
				}
				return this[--this.cursor];
			}

			public int NextIndex()
			{
				return this.cursor;
			}

			public int PreviousIndex()
			{
				return this.cursor - 1;
			}

			public void Remove()
			{
				throw new NotSupportedException();
			}

			public void Add(object o)
			{
				throw new NotSupportedException();
			}

			public void Set(object o)
			{
				throw new NotSupportedException();
			}

			private readonly int start;

			private readonly int len;
		}

		public virtual bool AddItem(object o)
		{
			throw new NotSupportedException();
		}

		public virtual bool Remove(object o)
		{
			throw new NotSupportedException();
		}

		public virtual bool AddAll(ICollection c)
		{
			throw new NotSupportedException();
		}

		public virtual bool RemoveAll(ICollection c)
		{
			throw new NotSupportedException();
		}

		public virtual bool RetainAll(ICollection c)
		{
			throw new NotSupportedException();
		}

		public virtual void Clear()
		{
			throw new NotSupportedException();
		}

		public virtual void Add(int index, object element)
		{
			throw new NotSupportedException();
		}

		public virtual bool AddRange(int index, ICollection c)
		{
			throw new NotSupportedException();
		}

		public virtual object Set(int index, object element)
		{
			throw new NotSupportedException();
		}

		public virtual object Remove(int index)
		{
			throw new NotSupportedException();
		}

		public virtual IList SubList(int fromIndex, int toIndex)
		{
			throw new NotSupportedException();
		}

		// #string_id_map#
		protected internal override int FindPrototypeId(string s)
		{
			int id;
			// #generated# Last update: 2005-09-26 15:47:42 EDT
			id = 0;
			string X = null;
			int c;
			switch (s.Length)
			{
				case 3:
				{
					c = s[0];
					if (c == 'm')
					{
						if (s[2] == 'p' && s[1] == 'a')
						{
							id = Id_map;
							goto L0_break;
						}
					}
					else
					{
						if (c == 'p')
						{
							if (s[2] == 'p' && s[1] == 'o')
							{
								id = Id_pop;
								goto L0_break;
							}
						}
					}
					goto L_break;
				}

				case 4:
				{
					switch (s[2])
					{
						case 'i':
						{
							X = "join";
							id = Id_join;
							goto L_break;
						}

						case 'm':
						{
							X = "some";
							id = Id_some;
							goto L_break;
						}

						case 'r':
						{
							X = "sort";
							id = Id_sort;
							goto L_break;
						}

						case 's':
						{
							X = "push";
							id = Id_push;
							goto L_break;
						}
					}
					goto L_break;
				}

				case 5:
				{
					c = s[1];
					if (c == 'h')
					{
						X = "shift";
						id = Id_shift;
					}
					else
					{
						if (c == 'l')
						{
							X = "slice";
							id = Id_slice;
						}
						else
						{
							if (c == 'v')
							{
								X = "every";
								id = Id_every;
							}
						}
					}
					goto L_break;
				}

				case 6:
				{
					c = s[0];
					if (c == 'c')
					{
						X = "concat";
						id = Id_concat;
					}
					else
					{
						if (c == 'f')
						{
							X = "filter";
							id = Id_filter;
						}
						else
						{
							if (c == 's')
							{
								X = "splice";
								id = Id_splice;
							}
							else
							{
								if (c == 'r')
								{
									X = "reduce";
									id = Id_reduce;
								}
							}
						}
					}
					goto L_break;
				}

				case 7:
				{
					switch (s[0])
					{
						case 'f':
						{
							X = "forEach";
							id = Id_forEach;
							goto L_break;
						}

						case 'i':
						{
							X = "indexOf";
							id = Id_indexOf;
							goto L_break;
						}

						case 'r':
						{
							X = "reverse";
							id = Id_reverse;
							goto L_break;
						}

						case 'u':
						{
							X = "unshift";
							id = Id_unshift;
							goto L_break;
						}
					}
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
					c = s[0];
					if (c == 'c')
					{
						X = "constructor";
						id = Id_constructor;
					}
					else
					{
						if (c == 'l')
						{
							X = "lastIndexOf";
							id = Id_lastIndexOf;
						}
						else
						{
							if (c == 'r')
							{
								X = "reduceRight";
								id = Id_reduceRight;
							}
						}
					}
					goto L_break;
				}

				case 14:
				{
					X = "toLocaleString";
					id = Id_toLocaleString;
					goto L_break;
				}
			}
L_break: ;
			if (X != null && X != s && !X.Equals(s))
			{
				id = 0;
			}
L0_break: ;
			// #/generated#
			return id;
		}

		private const int Id_constructor = 1;

		private const int Id_toString = 2;

		private const int Id_toLocaleString = 3;

		private const int Id_toSource = 4;

		private const int Id_join = 5;

		private const int Id_reverse = 6;

		private const int Id_sort = 7;

		private const int Id_push = 8;

		private const int Id_pop = 9;

		private const int Id_shift = 10;

		private const int Id_unshift = 11;

		private const int Id_splice = 12;

		private const int Id_concat = 13;

		private const int Id_slice = 14;

		private const int Id_indexOf = 15;

		private const int Id_lastIndexOf = 16;

		private const int Id_every = 17;

		private const int Id_filter = 18;

		private const int Id_forEach = 19;

		private const int Id_map = 20;

		private const int Id_some = 21;

		private const int Id_reduce = 22;

		private const int Id_reduceRight = 23;

		private const int MAX_PROTOTYPE_ID = 23;

		private const int ConstructorId_join = -Id_join;

		private const int ConstructorId_reverse = -Id_reverse;

		private const int ConstructorId_sort = -Id_sort;

		private const int ConstructorId_push = -Id_push;

		private const int ConstructorId_pop = -Id_pop;

		private const int ConstructorId_shift = -Id_shift;

		private const int ConstructorId_unshift = -Id_unshift;

		private const int ConstructorId_splice = -Id_splice;

		private const int ConstructorId_concat = -Id_concat;

		private const int ConstructorId_slice = -Id_slice;

		private const int ConstructorId_indexOf = -Id_indexOf;

		private const int ConstructorId_lastIndexOf = -Id_lastIndexOf;

		private const int ConstructorId_every = -Id_every;

		private const int ConstructorId_filter = -Id_filter;

		private const int ConstructorId_forEach = -Id_forEach;

		private const int ConstructorId_map = -Id_map;

		private const int ConstructorId_some = -Id_some;

		private const int ConstructorId_reduce = -Id_reduce;

		private const int ConstructorId_reduceRight = -Id_reduceRight;

		private const int ConstructorId_isArray = -24;

		/// <summary>Internal representation of the JavaScript array's length property.</summary>
		/// <remarks>Internal representation of the JavaScript array's length property.</remarks>
		private long length;

		/// <summary>Attributes of the array's length property</summary>
		private int lengthAttr = DONTENUM | PERMANENT;

		/// <summary>Fast storage for dense arrays.</summary>
		/// <remarks>
		/// Fast storage for dense arrays. Sparse arrays will use the superclass's
		/// hashtable storage scheme.
		/// </remarks>
		private object[] dense;

		/// <summary>True if all numeric properties are stored in <code>dense</code>.</summary>
		/// <remarks>True if all numeric properties are stored in <code>dense</code>.</remarks>
		private bool denseOnly;

		/// <summary>The maximum size of <code>dense</code> that will be allocated initially.</summary>
		/// <remarks>The maximum size of <code>dense</code> that will be allocated initially.</remarks>
		private static int maximumInitialCapacity = 10000;

		/// <summary>The default capacity for <code>dense</code>.</summary>
		/// <remarks>The default capacity for <code>dense</code>.</remarks>
		private const int DEFAULT_INITIAL_CAPACITY = 10;

		/// <summary>The factor to grow <code>dense</code> by.</summary>
		/// <remarks>The factor to grow <code>dense</code> by.</remarks>
		private const double GROW_FACTOR = 1.5;

		private const int MAX_PRE_GROW_SIZE = (int)(int.MaxValue / GROW_FACTOR);
		// #/string_id_map#
	}
}
