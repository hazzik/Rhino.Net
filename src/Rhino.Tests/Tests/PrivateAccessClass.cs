/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino.Tests;
using Sharpen;

namespace Rhino.Tests
{
	/// <summary>
	/// A class with private/protected/package private members, to test the Rhino
	/// feature Context.EnhancedJavaAccess, that allows bypassing Java
	/// member access restrictions.
	/// </summary>
	/// <remarks>
	/// A class with private/protected/package private members, to test the Rhino
	/// feature Context.EnhancedJavaAccess, that allows bypassing Java
	/// member access restrictions.
	/// </remarks>
	/// <author>Donna Malayeri</author>
	public class PrivateAccessClass
	{
		private PrivateAccessClass()
		{
		}

		internal PrivateAccessClass(string s)
		{
		}

		private PrivateAccessClass(int x)
		{
		}

		protected internal PrivateAccessClass(int x, string s)
		{
		}

		private class PrivateNestedClass
		{
			public PrivateNestedClass()
			{
			}

			internal int packagePrivateInt = 0;

			internal int privateInt = 1;

			protected internal int protectedInt = 2;
		}

		internal static int staticPackagePrivateInt = 0;

		private static int staticPrivateInt = 1;

		protected internal static int staticProtectedInt = 2;

		internal string packagePrivateString = "package private";

		private string privateString = "private";

		protected internal string protectedString = "protected";

		internal static int StaticPackagePrivateMethod()
		{
			return 0;
		}

		private static int StaticPrivateMethod()
		{
			return 1;
		}

		protected internal static int StaticProtectedMethod()
		{
			return 2;
		}

		internal virtual int PackagePrivateMethod()
		{
			return 3;
		}

		private int PrivateMethod()
		{
			return 4;
		}

		protected internal virtual int ProtectedMethod()
		{
			return 5;
		}

		private int javaBeanProperty = 6;

		public bool getterCalled = false;

		public bool setterCalled = false;

		public virtual int GetJavaBeanProperty()
		{
			getterCalled = true;
			return javaBeanProperty;
		}

		public virtual void SetJavaBeanProperty(int i)
		{
			setterCalled = true;
			javaBeanProperty = i;
		}

		public virtual int ReferenceToPrivateMembers()
		{
			PrivateAccessClass pac = new PrivateAccessClass();
			PrivateAccessClass pac2 = new PrivateAccessClass(2);
			PrivateAccessClass.PrivateNestedClass pnc = new PrivateAccessClass.PrivateNestedClass();
			System.Console.Out.WriteLine(privateString);
			pac2.PrivateMethod();
			// to silence warning
			return pnc.privateInt + staticPrivateInt + StaticPrivateMethod() + pac.PrivateMethod() + javaBeanProperty;
		}
	}
}
