/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Rhino.Tests;
using Rhino.Tests.Tests;
using Sharpen;

namespace Rhino.Tests.Tests
{
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class Bug421071Test
	{
		private ContextFactory factory;

		private Bug421071Test.TopLevelScope globalScope;

		private Script testScript;

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestProblemReplicator()
		{
			// before debugging please put the breakpoint in the
			// NativeJavaPackage.getPkgProperty()
			// and observe names passed in there
			testScript = CompileScript();
			RunTestScript();
			// this one does not get to the
			// NativeJavaPackage.getPkgProperty() on my
			// variables
			RunTestScript();
		}

		// however this one does
		private Script CompileScript()
		{
			string scriptSource = "importPackage(java.util);\n" + "var searchmon = 3;\n" + "var searchday = 10;\n" + "var searchyear = 2008;\n" + "var searchwkday = 0;\n" + "\n" + "var myDate = Calendar.getInstance();\n // this is a java.util.Calendar" + "myDate.set(Calendar.MONTH, searchmon);\n" + "myDate.set(Calendar.DATE, searchday);\n" + "myDate.set(Calendar.YEAR, searchyear);\n" + "searchwkday.value = myDate.get(Calendar.DAY_OF_WEEK);";
			Script script;
			Context context = factory.EnterContext();
			try
			{
				script = context.CompileString(scriptSource, "testScript", 1, null);
				return script;
			}
			finally
			{
				Context.Exit();
			}
		}

		/// <exception cref="System.Exception"></exception>
		private void RunTestScript()
		{
			// will start new thread to get as close as possible to original
			// environment, however the same behavior is exposed using new
			// ScriptRunner(script).run();
			Sharpen.Thread thread = new Sharpen.Thread(new Bug421071Test.ScriptRunner(this, testScript));
			thread.Start();
			thread.Join();
		}

		internal class DynamicScopeContextFactory : ContextFactory
		{
			protected override bool HasFeature(Context cx, int featureIndex)
			{
				if (featureIndex == Context.FEATURE_DYNAMIC_SCOPE)
				{
					return true;
				}
				return base.HasFeature(cx, featureIndex);
			}
		}

		private Bug421071Test.TopLevelScope CreateGlobalScope()
		{
			factory = new Bug421071Test.DynamicScopeContextFactory();
			Context context = factory.EnterContext();
			// noinspection deprecation
			Bug421071Test.TopLevelScope globalScope = new Bug421071Test.TopLevelScope(this, context);
			Context.Exit();
			return globalScope;
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.SetUp]
		protected virtual void SetUp()
		{
			globalScope = CreateGlobalScope();
		}

		[System.Serializable]
		private class TopLevelScope : ImporterTopLevel
		{
			private const long serialVersionUID = 7831526694313927899L;

			public TopLevelScope(Bug421071Test _enclosing, Context context) : base(context)
			{
				this._enclosing = _enclosing;
			}

			private readonly Bug421071Test _enclosing;
		}

		private class ScriptRunner : Runnable
		{
			private Script script;

			public ScriptRunner(Bug421071Test _enclosing, Script script)
			{
				this._enclosing = _enclosing;
				this.script = script;
			}

			public virtual void Run()
			{
				Context context = this._enclosing.factory.EnterContext();
				try
				{
					// Run each script in its own scope, to keep global variables
					// defined in each script separate
					Scriptable threadScope = context.NewObject(this._enclosing.globalScope);
					threadScope.SetPrototype(this._enclosing.globalScope);
					threadScope.SetParentScope(null);
					this.script.Exec(context, threadScope);
				}
				catch (Exception ee)
				{
					Sharpen.Runtime.PrintStackTrace(ee);
				}
				finally
				{
					Context.Exit();
				}
			}

			private readonly Bug421071Test _enclosing;
		}
	}
}
