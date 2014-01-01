/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using NUnit.Framework;
using Sharpen;
using Thread = System.Threading.Thread;

namespace Rhino.Tests
{
	[TestFixture]
	public class Bug421071Test
	{
		private ContextFactory factory;

		private TopLevelScope globalScope;

		private Script testScript;

		/// <exception cref="System.Exception"></exception>
		[Test]
		public void TestProblemReplicator()
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
			const string scriptSource = "importPackage(java.util);\n" + "var searchmon = 3;\n" + "var searchday = 10;\n" + "var searchyear = 2008;\n" + "var searchwkday = 0;\n" + "\n" + "var myDate = Calendar.getInstance();\n // this is a java.util.Calendar" + "myDate.set(Calendar.MONTH, searchmon);\n" + "myDate.set(Calendar.DATE, searchday);\n" + "myDate.set(Calendar.YEAR, searchyear);\n" + "searchwkday.value = myDate.get(Calendar.DAY_OF_WEEK);";
			Context context = factory.EnterContext();
			try
			{
				return context.CompileString(scriptSource, "testScript", 1, null);
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
			var thread = new Thread(() =>
			{
				Context context = factory.EnterContext();
				try
				{
					// Run each script in its own scope, to keep global variables
					// defined in each script separate
					Scriptable threadScope = context.NewObject(globalScope);
					threadScope.SetPrototype(globalScope);
					threadScope.SetParentScope(null);
					testScript.Exec(context, threadScope);
				}
				catch (Exception ee)
				{
					Runtime.PrintStackTrace(ee);
				}
				finally
				{
					Context.Exit();
				}
			});
			thread.Start();
			thread.Join();
		}

		private class DynamicScopeContextFactory : ContextFactory
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

		private TopLevelScope CreateGlobalScope()
		{
			factory = new DynamicScopeContextFactory();
			Context context = factory.EnterContext();
			// noinspection deprecation
			var scope = new TopLevelScope(context);
			Context.Exit();
			return scope;
		}

		/// <exception cref="System.Exception"></exception>
		[SetUp]
		private void SetUp()
		{
			globalScope = CreateGlobalScope();
		}

		[Serializable]
		private class TopLevelScope : ImporterTopLevel
		{
			public TopLevelScope(Context context) : base(context)
			{
			}
		}
	}
}
