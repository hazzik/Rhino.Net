/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino;
using Rhino.Tests;
using Sharpen;

namespace Rhino.Tests
{
	/// <summary>Example of defining global functions.</summary>
	/// <remarks>Example of defining global functions.</remarks>
	/// <author>Norris Boyd</author>
	[NUnit.Framework.TestFixture]
	public class DefineFunctionPropertiesTest
	{
		internal ScriptableObject global;

		internal static object key = "DefineFunctionPropertiesTest";

		/// <summary>
		/// Demonstrates how to create global functions in JavaScript
		/// from static methods defined in Java.
		/// </summary>
		/// <remarks>
		/// Demonstrates how to create global functions in JavaScript
		/// from static methods defined in Java.
		/// </remarks>
		[NUnit.Framework.SetUp]
		protected virtual void SetUp()
		{
			Context cx = Context.Enter();
			try
			{
				global = cx.InitStandardObjects();
				string[] names = new string[] { "f", "g" };
				global.DefineFunctionProperties(names, typeof(DefineFunctionPropertiesTest), PropertyAttributes.DONTENUM);
			}
			finally
			{
				Context.Exit();
			}
		}

		/// <summary>Simple global function that doubles its input.</summary>
		/// <remarks>Simple global function that doubles its input.</remarks>
		public static int F(int a)
		{
			return a * 2;
		}

		/// <summary>Simple test: call 'f' defined above</summary>
		[NUnit.Framework.Test]
		public virtual void TestSimpleFunction()
		{
			Context cx = Context.Enter();
			try
			{
				object result = cx.EvaluateString(global, "f(7) + 1", "test source", 1, null);
				NUnit.Framework.Assert.AreEqual(15.0, result);
			}
			finally
			{
				Context.Exit();
			}
		}

		/// <summary>
		/// More complicated example: this form of call allows variable
		/// argument lists, and allows access to the 'this' object.
		/// </summary>
		/// <remarks>
		/// More complicated example: this form of call allows variable
		/// argument lists, and allows access to the 'this' object. For
		/// a global function, the 'this' object is the global object.
		/// In this case we look up a value that we associated with the global
		/// object using
		/// <see cref="Rhino.ScriptableObject.GetAssociatedValue(object)">Rhino.ScriptableObject.GetAssociatedValue(object)</see>
		/// .
		/// </remarks>
		public static object G(Context cx, Scriptable thisObj, object[] args, Function funObj)
		{
			object arg = args.Length > 0 ? args[0] : Undefined.instance;
			object privateValue = Undefined.instance;
			if (thisObj is ScriptableObject)
			{
				privateValue = ((ScriptableObject)thisObj).GetAssociatedValue(key);
			}
			return arg.ToString() + privateValue;
		}

		/// <summary>
		/// Associate a value with the global scope and call function 'g'
		/// defined above.
		/// </summary>
		/// <remarks>
		/// Associate a value with the global scope and call function 'g'
		/// defined above.
		/// </remarks>
		[NUnit.Framework.Test]
		public virtual void TestPrivateData()
		{
			Context cx = Context.Enter();
			try
			{
				global.AssociateValue(key, "bar");
				object result = cx.EvaluateString(global, "g('foo');", "test source", 1, null);
				NUnit.Framework.Assert.AreEqual("foobar", result);
			}
			finally
			{
				Context.Exit();
			}
		}
	}
}
