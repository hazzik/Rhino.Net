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
	/// <summary>This class implements generator objects.</summary>
	/// <remarks>
	/// This class implements generator objects. See
	/// http://developer.mozilla.org/en/docs/New_in_JavaScript_1.7#Generators
	/// </remarks>
	/// <author>Norris Boyd</author>
	[System.Serializable]
	public sealed class NativeGenerator : IdScriptableObject
	{
		private static readonly object GENERATOR_TAG = "Generator";

		internal static Rhino.NativeGenerator Init(ScriptableObject scope, bool @sealed)
		{
			// Generator
			// Can't use "NativeGenerator().exportAsJSClass" since we don't want
			// to define "Generator" as a constructor in the top-level scope.
			Rhino.NativeGenerator prototype = new Rhino.NativeGenerator();
			if (scope != null)
			{
				prototype.ParentScope = scope;
				prototype.SetPrototype(GetObjectPrototype(scope));
			}
			prototype.ActivatePrototypeMap(MAX_PROTOTYPE_ID);
			if (@sealed)
			{
				prototype.SealObject();
			}
			// Need to access Generator prototype when constructing
			// Generator instances, but don't have a generator constructor
			// to use to find the prototype. Use the "associateValue"
			// approach instead.
			if (scope != null)
			{
				scope.AssociateValue(GENERATOR_TAG, prototype);
			}
			return prototype;
		}

		/// <summary>Only for constructing the prototype object.</summary>
		/// <remarks>Only for constructing the prototype object.</remarks>
		private NativeGenerator()
		{
		}

		public NativeGenerator(Scriptable scope, NativeFunction function, object savedState)
		{
			this.function = function;
			this.savedState = savedState;
			// Set parent and prototype properties. Since we don't have a
			// "Generator" constructor in the top scope, we stash the
			// prototype in the top scope's associated value.
			Scriptable top = ScriptableObject.GetTopLevelScope(scope);
			this.ParentScope = top;
			Rhino.NativeGenerator prototype = (Rhino.NativeGenerator)ScriptableObject.GetTopScopeValue(top, GENERATOR_TAG);
			this.SetPrototype(prototype);
		}

		public const int GENERATOR_SEND = 0;

		public const int GENERATOR_THROW = 1;

		public const int GENERATOR_CLOSE = 2;

		public override string GetClassName()
		{
			return "Generator";
		}

		private class CloseGeneratorAction
		{
			private readonly NativeGenerator generator;

			internal CloseGeneratorAction(NativeGenerator generator)
			{
				this.generator = generator;
			}

			public virtual object Run(Context cx)
			{
				Scriptable scope = GetTopLevelScope(generator);
				Callable closeGenerator = new _Callable_84();
				return ScriptRuntime.DoTopCall(closeGenerator, cx, scope, generator, null);
			}

			private sealed class _Callable_84 : Callable
			{
				public object Call(Context cx, Scriptable scope, Scriptable thisObj, object[] args)
				{
					return ((NativeGenerator)thisObj).Resume(cx, scope, NativeGenerator.GENERATOR_CLOSE, new NativeGenerator.GeneratorClosedException());
				}
			}
		}

		protected internal override void InitPrototypeId(int id)
		{
			string s;
			int arity;
			switch (id)
			{
				case Id_close:
				{
					arity = 1;
					s = "close";
					break;
				}

				case Id_next:
				{
					arity = 1;
					s = "next";
					break;
				}

				case Id_send:
				{
					arity = 0;
					s = "send";
					break;
				}

				case Id_throw:
				{
					arity = 0;
					s = "throw";
					break;
				}

				case Id___iterator__:
				{
					arity = 1;
					s = "__iterator__";
					break;
				}

				default:
				{
					throw new ArgumentException(id.ToString());
				}
			}
			InitPrototypeMethod(GENERATOR_TAG, id, s, arity);
		}

		public override object ExecIdCall(IdFunctionObject f, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (!f.HasTag(GENERATOR_TAG))
			{
				return base.ExecIdCall(f, cx, scope, thisObj, args);
			}
			int id = f.MethodId();
			if (!(thisObj is NativeGenerator))
			{
				throw IncompatibleCallError(f);
			}
			NativeGenerator generator = (NativeGenerator)thisObj;
			switch (id)
			{
				case Id_close:
				{
					// need to run any pending finally clauses
					return generator.Resume(cx, scope, GENERATOR_CLOSE, new NativeGenerator.GeneratorClosedException());
				}

				case Id_next:
				{
					// arguments to next() are ignored
					generator.firstTime = false;
					return generator.Resume(cx, scope, GENERATOR_SEND, Undefined.instance);
				}

				case Id_send:
				{
					object arg = args.Length > 0 ? args[0] : Undefined.instance;
					if (generator.firstTime && !arg.Equals(Undefined.instance))
					{
						throw ScriptRuntime.TypeError0("msg.send.newborn");
					}
					return generator.Resume(cx, scope, GENERATOR_SEND, arg);
				}

				case Id_throw:
				{
					return generator.Resume(cx, scope, GENERATOR_THROW, args.Length > 0 ? args[0] : Undefined.instance);
				}

				case Id___iterator__:
				{
					return thisObj;
				}

				default:
				{
					throw new ArgumentException(id.ToString());
				}
			}
		}

		private object Resume(Context cx, Scriptable scope, int operation, object value)
		{
			if (savedState == null)
			{
				if (operation == GENERATOR_CLOSE)
				{
					return Undefined.instance;
				}
				object thrown;
				if (operation == GENERATOR_THROW)
				{
					thrown = value;
				}
				else
				{
					thrown = NativeIterator.GetStopIterationObject(scope);
				}
				throw new JavaScriptException(thrown, lineSource, lineNumber);
			}
			try
			{
				lock (this)
				{
					// generator execution is necessarily single-threaded and
					// non-reentrant.
					// See https://bugzilla.mozilla.org/show_bug.cgi?id=349263
					if (locked)
					{
						throw ScriptRuntime.TypeError0("msg.already.exec.gen");
					}
					locked = true;
				}
				return function.ResumeGenerator(cx, scope, operation, savedState, value);
			}
			catch (NativeGenerator.GeneratorClosedException)
			{
				// On closing a generator in the compile path, the generator
				// throws a special exception. This ensures execution of all pending
				// finalizers and will not get caught by user code.
				return Undefined.instance;
			}
			catch (RhinoException e)
			{
				lineNumber = e.LineNumber();
				lineSource = e.LineSource();
				savedState = null;
				throw;
			}
			finally
			{
				lock (this)
				{
					locked = false;
				}
				if (operation == GENERATOR_CLOSE)
				{
					savedState = null;
				}
			}
		}

		// #string_id_map#
		protected internal override int FindPrototypeId(string s)
		{
			int id;
			// #generated# Last update: 2007-06-14 13:13:03 EDT
			id = 0;
			string X = null;
			int c;
			int s_length = s.Length;
			if (s_length == 4)
			{
				c = s[0];
				if (c == 'n')
				{
					X = "next";
					id = Id_next;
				}
				else
				{
					if (c == 's')
					{
						X = "send";
						id = Id_send;
					}
				}
			}
			else
			{
				if (s_length == 5)
				{
					c = s[0];
					if (c == 'c')
					{
						X = "close";
						id = Id_close;
					}
					else
					{
						if (c == 't')
						{
							X = "throw";
							id = Id_throw;
						}
					}
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

		private const int Id_close = 1;

		private const int Id_next = 2;

		private const int Id_send = 3;

		private const int Id_throw = 4;

		private const int Id___iterator__ = 5;

		private const int MAX_PROTOTYPE_ID = 5;

		private NativeFunction function;

		private object savedState;

		private string lineSource;

		private int lineNumber;

		private bool firstTime = true;

		private bool locked;

		[System.Serializable]
		public class GeneratorClosedException : Exception
		{
			// #/string_id_map#
		}
	}
}
