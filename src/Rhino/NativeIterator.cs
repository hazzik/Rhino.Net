/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
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
	[System.Serializable]
	public sealed class NativeIterator : IdScriptableObject
	{
		private const long serialVersionUID = -4136968203581667681L;

		private static readonly object ITERATOR_TAG = "Iterator";

		internal static void Init(ScriptableObject scope, bool @sealed)
		{
			// Iterator
			Rhino.NativeIterator iterator = new Rhino.NativeIterator();
			iterator.ExportAsJSClass(MAX_PROTOTYPE_ID, scope, @sealed);
			// Generator
			NativeGenerator.Init(scope, @sealed);
			// StopIteration
			NativeObject obj = new NativeIterator.StopIteration();
			obj.SetPrototype(GetObjectPrototype(scope));
			obj.SetParentScope(scope);
			if (@sealed)
			{
				obj.SealObject();
			}
			ScriptableObject.DefineProperty(scope, STOP_ITERATION, obj, ScriptableObject.DONTENUM);
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
			Scriptable top = ScriptableObject.GetTopLevelScope(scope);
			return ScriptableObject.GetTopScopeValue(top, ITERATOR_TAG);
		}

		private const string STOP_ITERATION = "StopIteration";

		public const string ITERATOR_PROPERTY_NAME = "__iterator__";

		[System.Serializable]
		internal class StopIteration : NativeObject
		{
			private const long serialVersionUID = 2485151085722377663L;

			public override string GetClassName()
			{
				return STOP_ITERATION;
			}

			public override bool HasInstance(Scriptable instance)
			{
				return instance is NativeIterator.StopIteration;
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
				IEnumerator<object> iterator = VMBridge.instance.GetJavaIterator(cx, scope, obj);
				if (iterator != null)
				{
					scope = ScriptableObject.GetTopLevelScope(scope);
					return cx.GetWrapFactory().Wrap(cx, scope, new NativeIterator.WrappedJavaIterator(iterator, scope), typeof(NativeIterator.WrappedJavaIterator));
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
			result.SetPrototype(ScriptableObject.GetClassPrototype(scope, result.GetClassName()));
			result.SetParentScope(scope);
			return result;
		}

		private object Next(Context cx, Scriptable scope)
		{
			bool b = ScriptRuntime.EnumNext(this.objectIterator);
			if (!b)
			{
				// Out of values. Throw StopIteration.
				throw new JavaScriptException(NativeIterator.GetStopIterationObject(scope), null, 0);
			}
			return ScriptRuntime.EnumId(this.objectIterator, cx);
		}

		public class WrappedJavaIterator
		{
			internal WrappedJavaIterator(IEnumerator<object> iterator, Scriptable scope)
			{
				this.iterator = iterator;
				this.scope = scope;
			}

			public virtual object Next()
			{
				if (!iterator.HasNext())
				{
					// Out of values. Throw StopIteration.
					throw new JavaScriptException(NativeIterator.GetStopIterationObject(scope), null, 0);
				}
				return iterator.Next();
			}

			public virtual object __iterator__(bool b)
			{
				return this;
			}

			private IEnumerator<object> iterator;

			private Scriptable scope;
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
