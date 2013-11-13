/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino.Tests;
using Rhino.Tests.Tests;
using Sharpen;

namespace Rhino.Tests.Tests
{
	[NUnit.Framework.TestFixture]
	public class DefineClassMapInheritance
	{
		[System.Serializable]
		public class Food : ScriptableObject
		{
			public override string GetClassName()
			{
				return GetType().Name;
			}
		}

		[System.Serializable]
		public class Fruit : DefineClassMapInheritance.Food
		{
		}

		[System.Serializable]
		public class Vegetable : DefineClassMapInheritance.Food
		{
		}

		/// <exception cref="System.MemberAccessException"></exception>
		/// <exception cref="Sharpen.InstantiationException"></exception>
		/// <exception cref="System.Reflection.TargetInvocationException"></exception>
		[NUnit.Framework.Test]
		public virtual void Test()
		{
			Context cx = Context.Enter();
			try
			{
				ScriptableObject scope = cx.InitStandardObjects();
				// define two classes that share a parent prototype
				ScriptableObject.DefineClass<DefineClassMapInheritance.Fruit>(scope, false, true);
				ScriptableObject.DefineClass<DefineClassMapInheritance.Vegetable>(scope, false, true);
				NUnit.Framework.Assert.AreEqual(true, Evaluate(cx, scope, "(new Fruit instanceof Food)"));
				NUnit.Framework.Assert.AreEqual(true, Evaluate(cx, scope, "(new Vegetable instanceof Food)"));
			}
			finally
			{
				Context.Exit();
			}
		}

		private static object Evaluate(Context cx, ScriptableObject scope, string source)
		{
			return cx.EvaluateString(scope, source, "<eval>", 1, null);
		}
	}
}
