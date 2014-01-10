/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using Rhino;
using Rhino.Tests;
using Sharpen;

namespace Rhino.Tests
{
	public class Evaluator
	{
		public static object Eval(string source)
		{
			return Eval(source, null);
		}

		public static object Eval(string source, string id, Scriptable @object)
		{
			return Eval(source, new Dictionary<string, Scriptable> { { id, @object } });
		}

		public static object Eval(string source, IDictionary<string, Scriptable> bindings)
		{
			Context cx = ContextFactory.GetGlobal().EnterContext();
			try
			{
				Scriptable scope = cx.InitStandardObjects();
				if (bindings != null)
				{
					foreach (KeyValuePair<string, Scriptable> entry in bindings)
					{
						Scriptable @object = entry.Value;
						@object.ParentScope = scope;
						scope.Put(entry.Key, scope, @object);
					}
				}
				return cx.EvaluateString(scope, source, "source", 1, null);
			}
			finally
			{
				Context.Exit();
			}
		}
	}
}
