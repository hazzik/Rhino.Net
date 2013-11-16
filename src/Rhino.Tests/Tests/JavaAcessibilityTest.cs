/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino;
using Rhino.Drivers;
using Rhino.Tools.Shell;
using Sharpen;

namespace Rhino.Tests
{
	/// <author>donnamalayeri</author>
	[NUnit.Framework.TestFixture]
	public class JavaAcessibilityTest
	{
		protected internal readonly Global global = new Global();

		internal string importClass = "importClass(Packages.org.mozilla.javascript.tests.PrivateAccessClass)\n";

		public JavaAcessibilityTest()
		{
			global.Init(contextFactory);
		}

		[NUnit.Framework.SetUp]
		protected virtual void SetUp()
		{
			TestUtils.SetGlobalContextFactory(contextFactory);
		}

		[NUnit.Framework.TearDown]
		protected virtual void TearDown()
		{
			TestUtils.SetGlobalContextFactory(null);
		}

		private sealed class _ShellContextFactory_43 : ShellContextFactory
		{
			public _ShellContextFactory_43()
			{
			}

			protected override bool HasFeature(Context cx, int featureIndex)
			{
				if (featureIndex == Context.FEATURE_ENHANCED_JAVA_ACCESS)
				{
					return true;
				}
				return base.HasFeature(cx, featureIndex);
			}
		}

		private ContextFactory contextFactory = new _ShellContextFactory_43();

		[NUnit.Framework.Test]
		public virtual void TestAccessingFields()
		{
			object result = RunScript(importClass + "PrivateAccessClass.staticPackagePrivateInt");
			NUnit.Framework.Assert.AreEqual(0, result);
			result = RunScript(importClass + "PrivateAccessClass.staticPrivateInt");
			NUnit.Framework.Assert.AreEqual(1, result);
			result = RunScript(importClass + "PrivateAccessClass.staticProtectedInt");
			NUnit.Framework.Assert.AreEqual(2, result);
			result = RunScript(importClass + "new PrivateAccessClass().packagePrivateString");
			NUnit.Framework.Assert.AreEqual("package private", ((NativeJavaObject)result).Unwrap());
			result = RunScript(importClass + "new PrivateAccessClass().privateString");
			NUnit.Framework.Assert.AreEqual("private", ((NativeJavaObject)result).Unwrap());
			result = RunScript(importClass + "new PrivateAccessClass().protectedString");
			NUnit.Framework.Assert.AreEqual("protected", ((NativeJavaObject)result).Unwrap());
			result = RunScript(importClass + "new PrivateAccessClass.PrivateNestedClass().packagePrivateInt");
			NUnit.Framework.Assert.AreEqual(0, result);
			result = RunScript(importClass + "new PrivateAccessClass.PrivateNestedClass().privateInt");
			NUnit.Framework.Assert.AreEqual(1, result);
			result = RunScript(importClass + "new PrivateAccessClass.PrivateNestedClass().protectedInt");
			NUnit.Framework.Assert.AreEqual(2, result);
		}

		[NUnit.Framework.Test]
		public virtual void TestAccessingMethods()
		{
			object result = RunScript(importClass + "PrivateAccessClass.staticPackagePrivateMethod()");
			NUnit.Framework.Assert.AreEqual(0, result);
			result = RunScript(importClass + "PrivateAccessClass.staticPrivateMethod()");
			NUnit.Framework.Assert.AreEqual(1, result);
			result = RunScript(importClass + "PrivateAccessClass.staticProtectedMethod()");
			NUnit.Framework.Assert.AreEqual(2, result);
			result = RunScript(importClass + "new PrivateAccessClass().packagePrivateMethod()");
			NUnit.Framework.Assert.AreEqual(3, result);
			result = RunScript(importClass + "new PrivateAccessClass().privateMethod()");
			NUnit.Framework.Assert.AreEqual(4, result);
			result = RunScript(importClass + "new PrivateAccessClass().protectedMethod()");
			NUnit.Framework.Assert.AreEqual(5, result);
		}

		[NUnit.Framework.Test]
		public virtual void TestAccessingConstructors()
		{
			RunScript(importClass + "new PrivateAccessClass(\"foo\")");
			RunScript(importClass + "new PrivateAccessClass(5)");
			RunScript(importClass + "new PrivateAccessClass(5, \"foo\")");
		}

		[NUnit.Framework.Test]
		public virtual void TestAccessingJavaBeanProperty()
		{
			object result = RunScript(importClass + "var x = new PrivateAccessClass(); x.javaBeanProperty + ' ' + x.getterCalled;");
			NUnit.Framework.Assert.AreEqual("6 true", result);
			result = RunScript(importClass + "var x = new PrivateAccessClass(); x.javaBeanProperty = 4; x.javaBeanProperty + ' ' + x.setterCalled;");
			NUnit.Framework.Assert.AreEqual("4 true", result);
		}

		[NUnit.Framework.Test]
		public virtual void TestOverloadFunctionRegression()
		{
			object result = RunScript("(new java.util.GregorianCalendar()).set(3,4);'success';");
			NUnit.Framework.Assert.AreEqual("success", result);
		}

		private object RunScript(string scriptSourceText)
		{
			return contextFactory.Call(cx =>
			{
				Script script = cx.CompileString(scriptSourceText, string.Empty, 1, null);
				return script.Exec(cx, this.global);
			});
		}
	}
}
