/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using Rhino;
using Rhino.Tests.Es5;
using Sharpen;

namespace Rhino.Tests.Es5
{
	/// <author>AndrГ© Bargull</author>
	[NUnit.Framework.TestFixture]
	public class Test262RegExpTest
	{
		private Context cx;

		private ScriptableObject scope;

		[NUnit.Framework.SetUp]
		public virtual void SetUp()
		{
			cx = Context.Enter();
			scope = cx.InitStandardObjects();
		}

		[NUnit.Framework.TearDown]
		public virtual void TearDown()
		{
			Context.Exit();
		}

		[NUnit.Framework.Test]
		public virtual void TestS15_10_2_9_A1_T4()
		{
			string source = "/\\b(\\w+) \\2\\b/.test('do you listen the the band');";
			string sourceName = "Conformance/15_Native/15.10_RegExp_Objects/15.10.2_Pattern_Semantics/15.10.2.9_AtomEscape/S15.10.2.9_A1_T4.js";
			cx.EvaluateString(scope, source, sourceName, 0, null);
		}

		[NUnit.Framework.Test]
		public virtual void TestS15_10_2_11_A1_T2()
		{
			IList<string> sources = new List<string>();
			sources.Add("/\\1/.exec('');");
			sources.Add("/\\2/.exec('');");
			sources.Add("/\\3/.exec('');");
			sources.Add("/\\4/.exec('');");
			sources.Add("/\\5/.exec('');");
			sources.Add("/\\6/.exec('');");
			sources.Add("/\\7/.exec('');");
			sources.Add("/\\8/.exec('');");
			sources.Add("/\\9/.exec('');");
			sources.Add("/\\10/.exec('');");
			string sourceName = "Conformance/15_Native/15.10_RegExp_Objects/15.10.2_Pattern_Semantics/15.10.2.11_DecimalEscape/S15.10.2.11_A1_T2.js";
			foreach (string source in sources)
			{
				cx.EvaluateString(scope, source, sourceName, 0, null);
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestS15_10_2_11_A1_T3()
		{
			string source = "/(?:A)\\2/.exec('AA');";
			string sourceName = "Conformance/15_Native/15.10_RegExp_Objects/15.10.2_Pattern_Semantics/15.10.2.11_DecimalEscape/S15.10.2.11_A1_T3.js";
			cx.EvaluateString(scope, source, sourceName, 0, null);
		}

		public virtual void TestS15_10_2_15_A1_T4()
		{
			string source = "(new RegExp('[\\\\Db-G]').exec('a'))";
			string sourceName = "Conformance/15_Native/15.10_RegExp_Objects/15.10.2_Pattern_Semantics/15.10.2.15_NonemptyClassRanges/S15.10.2.15_A1_T4.js";
			cx.EvaluateString(scope, source, sourceName, 0, null);
		}

		public virtual void TestS15_10_2_15_A1_T5()
		{
			string source = "(new RegExp('[\\\\sb-G]').exec('a'))";
			string sourceName = "Conformance/15_Native/15.10_RegExp_Objects/15.10.2_Pattern_Semantics/15.10.2.15_NonemptyClassRanges/S15.10.2.15_A1_T5.js";
			cx.EvaluateString(scope, source, sourceName, 0, null);
		}

		public virtual void TestS15_10_2_15_A1_T6()
		{
			string source = "(new RegExp('[\\\\Sb-G]').exec('a'))";
			string sourceName = "Conformance/15_Native/15.10_RegExp_Objects/15.10.2_Pattern_Semantics/15.10.2.15_NonemptyClassRanges/S15.10.2.15_A1_T6.js";
			cx.EvaluateString(scope, source, sourceName, 0, null);
		}

		public virtual void TestS15_10_2_15_A1_T7()
		{
			string source = "(new RegExp('[\\\\wb-G]').exec('a'))";
			string sourceName = "Conformance/15_Native/15.10_RegExp_Objects/15.10.2_Pattern_Semantics/15.10.2.15_NonemptyClassRanges/S15.10.2.15_A1_T7.js";
			cx.EvaluateString(scope, source, sourceName, 0, null);
		}

		public virtual void TestS15_10_2_15_A1_T8()
		{
			string source = "(new RegExp('[\\\\Wb-G]').exec('a'))";
			string sourceName = "Conformance/15_Native/15.10_RegExp_Objects/15.10.2_Pattern_Semantics/15.10.2.15_NonemptyClassRanges/S15.10.2.15_A1_T8.js";
			cx.EvaluateString(scope, source, sourceName, 0, null);
		}
	}
}
