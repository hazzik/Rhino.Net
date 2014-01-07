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
	/// <summary>This class implements iterator objects.</summary>
	/// <remarks>
	/// This class implements iterator objects. See
	/// http://developer.mozilla.org/en/docs/New_in_JavaScript_1.7#Iterators
	/// </remarks>
	/// <author>Norris Boyd</author>
	[Serializable]
	public sealed class NativeIterator : IdScriptableObject
	{
		private const long serialVersionUID = -4136968203581667681L;

		private static readonly object ITERATOR_TAG = "Iterator";

		internal static void Init(ScriptableObject scope, bool @sealed)
		{
			// Iterator
			NativeIterator iterator = new NativeIterator();
			iterator.ExportAsJSClass(MAX_PROTOTYPE_ID, scope, @sealed);
			// Generator
			NativeGenerator.Init(scope, @sealed);
			// StopIteration
			NativeObject obj = new StopIteration();
			obj.SetPrototype(GetObjectPrototype(scope));
			obj.SetParentScope(scope);
			if (@sealed)
			{
				obj.SealObject();
			}
			DefineProperty(scope, STOP_ITERATION, obj, PropertyAttributes.DONTENUM);
			// Use "associateValue" so that generators can continue to
			// throw StopIteration even if the property of the global
			// scope is replaced or deleted.
			scope.AssociateValue(ITERATOR_TAG, obj);
		}

		/// <summary>Only for constructing the prototype object.</summary>
		/// <remarks>Only for constructing the prototype object.</remarks>
		private NativeIterator()
		{
		}

		private NativeIterator(object objectIterator)
		{
			this.objectIterator = objectIterator;
		}

		/// <summary>Get the value of the "StopIteration" object.</summary>
		/// <remarks>
		/// Get the value of the "StopIteration" object. Note that this value
		/// is stored in the top-level scope using "associateValue" so the
		/// value can still be found even if a script overwrites or deletes
		/// the global "StopIteration" property.
		/// </remarks>
		/// <param name="scope">a scope whose parent chain reaches a top-level scope</param>
		/// <returns>the StopIteration object</returns>
		public static object GetStopIterationObject(Scriptable scope)
		{
			Scriptable top = GetTopLevelScope(scope);
			return GetTopScopeValue(top, ITERATOR_TAG);
		}

		private const string STOP_ITERATION = "StopIteration";

		public const string ITERATOR_PROPERTY_NAME = "__iterator__";

		[Serializable]
		internal class StopIteration : NativeObject
		{
			private const long serialVersionUID = 2485151085722377663L;

			public override string GetClassName()
			{
				return STOP_ITERATION;
			}

			public override bool HasInstance(Scriptable instance)
			{
				return instance is StopIteration;
			}
		}

		public override string GetClassName()
		{
			return "Iterator";
		}

		protected internal override void InitPrototypeId(int id)
		{
			string s;
			int arity;
			switch (id)
			{
				case Id_constructor:
				{
					arity = 2;
					s = "constructor";
					break;
				}

				case Id_next:
				{
					arity = 0;
					s = "next";
					break;
				}

				case Id___iterator__:
				{
					arity = 1;
					s = ITERATOR_PROPERTY_NAME;
					break;
				}

				default:
				{
					throw new ArgumentException(id.ToString());
				}
			}
			InitPrototypeMethod(ITERATOR_TAG, id, s, arity);
		}

		public override object ExecIdCall(IdFunctionObject f, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (!f.HasTag(ITERATOR_TAG))
			{
				return base.ExecIdCall(f, cx, scope, thisObj, args);
			}
			int id = f.MethodId();
			if (id == Id_constructor)
			{
				return JsConstructor(cx, scope, thisObj, args);
			}
			if (!(thisObj is NativeIterator))
			{
				throw IncompatibleCallError(f);
			}
			NativeIterator iterator = (NativeIterator)thisObj;
			switch (id)
			{
				case Id_next:
				{
					return iterator.Next(cx, scope);
				}

				case Id___iterator__:
				{
					/// XXX: what about argument? SpiderMonkey apparently ignores it
					return thisObj;
				}

				default:
				{
					throw new ArgumentException(id.ToString());
				}
			}
		}

		private static object JsConstructor(Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (args.Length == 0 || args[0] == null || args[0] == Undefined.instance)
			{
				object argument = args.Length == 0 ? Undefined.instance : args[0];
				throw ScriptRuntime.TypeError1("msg.no.properties", ScriptRuntime.ToString(argument));
			}
			Scriptable obj = ScriptRuntime.ToObject(scope, args[0]);
			bool keyOnly = args.Length > 1 && ScriptRuntime.ToBoolean(args[1]);
			if (thisObj != null)
			{
				// Called as a function. Convert to iterator if possible.
				// For objects that implement java.lang.Iterable or
				// java.util.Iterator, have JavaScript Iterator call the underlying
				// iteration methods
				IEnumerator iterator = GetEnumerator(cx, scope, obj);
				if (iterator != null)
				{
					scope = GetTopLevelScope(scope);
					return cx.GetWrapFactory().Wrap(cx, scope, new WrappedJavaIterator(iterator, scope), typeof(WrappedJavaIterator));
				}
				// Otherwise, just call the runtime routine
				Scriptable jsIterator = ScriptRuntime.ToIterator(cx, scope, obj, keyOnly);
				if (jsIterator != null)
				{
					return jsIterator;
				}
			}
			// Otherwise, just set up to iterate over the properties of the object.
			// Do not call __iterator__ method.
			object objectIterator = ScriptRuntime.EnumInit(obj, cx, keyOnly ? ScriptRuntime.ENUMERATE_KEYS_NO_ITERATOR : ScriptRuntime.ENUMERATE_ARRAY_NO_ITERATOR);
			ScriptRuntime.SetEnumNumbers(objectIterator, true);
			NativeIterator result = new NativeIterator(objectIterator);
			result.SetPrototype(GetClassPrototype(scope, result.GetClassName()));
			result.SetParentScope(scope);
			return result;
		}

		private object Next(Context cx, Scriptable scope)
		{
			bool b = ScriptRuntime.EnumNext(objectIterator);
			if (!b)
			{
				// Out of values. Throw StopIteration.
				throw new JavaScriptException(GetStopIterationObject(scope), null, 0);
			}
			return ScriptRuntime.EnumId(objectIterator, cx);
		}

		public class WrappedJavaIterator
		{
			internal WrappedJavaIterator(IEnumerator iterator, Scriptable scope)
			{
				this.iterator = iterator;
				this.scope = scope;
			}

			public virtual object Next()
			{
				if (!iterator.MoveNext())
				{
					// Out of values. Throw StopIteration.
					throw new JavaScriptException(GetStopIterationObject(scope), null, 0);
				}
				return iterator.Current;
			}

			public virtual object __iterator__(bool b)
			{
				return this;
			}

			private IEnumerator iterator;

			private Scriptable scope;
		}

		/// <summary>
		/// If "obj" is a java.util.Iterator or a java.lang.Iterable, return a
		/// wrapping as a JavaScript Iterator.
		/// </summary>
		/// <remarks>
		/// If "obj" is a java.util.Iterator or a java.lang.Iterable, return a
		/// wrapping as a JavaScript Iterator. Otherwise, return null.
		/// This method is in VMBridge since Iterable is a JDK 1.5 addition.
		/// </remarks>
		private static IEnumerator GetEnumerator(Context cx, Scriptable scope, object obj)
		{
			var wrapper = obj as Wrapper;
			if (wrapper != null)
			{
				object unwrapped = wrapper.Unwrap();
				var enumerator = unwrapped as IEnumerator;
				if (enumerator != null)
				{
					return enumerator;
				}
				var enumerable = unwrapped as IEnumerable;
				if (enumerable != null)
				{
					return enumerable.GetEnumerator();
				}
			}
			return null;
		}

		// #string_id_map#
		protected internal override int FindPrototypeId(string s)
		{
			int id;
			// #generated# Last update: 2007-06-11 09:43:19 EDT
			id = 0;
			string X = null;
			int s_length = s.Length;
			if (s_length == 4)
			{
				X = "next";
				id = Id_next;
			}
			else
			{
				if (s_length == 11)
				{
					X = "constructor";
					id = Id_constructor;
				}
				else
				{
					if (s_length == 12)
					{
						X = "__iterator__";
						id = Id___iterator__;
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

		private const int Id_next = 2;

		private const int Id___iterator__ = 3;

		private const int MAX_PROTOTYPE_ID = 3;

		private object objectIterator;
		// #/string_id_map#
	}
}
