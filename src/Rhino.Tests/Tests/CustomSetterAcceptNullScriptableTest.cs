/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Reflection;
using Rhino;
using Rhino.Tests;
using Sharpen;

namespace Rhino.Tests
{
	/// <summary>
	/// Takes care that it's possible to set <code>null</code> value
	/// when using custom setter for a
	/// <see cref="Rhino.Scriptable">Rhino.Scriptable</see>
	/// object.
	/// See https://bugzilla.mozilla.org/show_bug.cgi?id=461138
	/// </summary>
	/// <author>Marc Guillemot</author>
	[NUnit.Framework.TestFixture]
	public class CustomSetterAcceptNullScriptableTest
	{
		[System.Serializable]
		public class Foo : ScriptableObject
		{
			public override string GetClassName()
			{
				return "Foo";
			}

			public virtual void SetMyProp(CustomSetterAcceptNullScriptableTest.Foo2 s)
			{
			}
		}

		[System.Serializable]
		public class Foo2 : ScriptableObject
		{
			public override string GetClassName()
			{
				return "Foo2";
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestSetNullForScriptableSetter()
		{
			string scriptCode = "foo.myProp = new Foo2();\n" + "foo.myProp = null;";
			ContextFactory factory = new ContextFactory();
			Context cx = factory.EnterContext();
			try
			{
				ScriptableObject topScope = cx.InitStandardObjects();
				CustomSetterAcceptNullScriptableTest.Foo foo = new CustomSetterAcceptNullScriptableTest.Foo();
				// define custom setter method
				MethodInfo setMyPropMethod = typeof (CustomSetterAcceptNullScriptableTest.Foo).GetMethod("SetMyProp", new[] { typeof (CustomSetterAcceptNullScriptableTest.Foo2) });
				foo.DefineProperty("myProp", null, null, setMyPropMethod, PropertyAttributes.EMPTY);
				topScope.Put("foo", topScope, foo);
				ScriptableObject.DefineClass<CustomSetterAcceptNullScriptableTest.Foo2>(topScope);
				cx.EvaluateString(topScope, scriptCode, "myScript", 1, null);
			}
			finally
			{
				Context.Exit();
			}
		}
	}
}
