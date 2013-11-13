/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using NUnit.Framework;
using Rhino.Tests;
using Sharpen;

namespace Rhino.Tests.Testing
{
	/// <summary>
	/// <p>An error reporter for testing that verifies that messages reported to the
	/// reporter are expected.</p>
	/// <p>Sample use</p>
	/// <pre>
	/// TestErrorReporter e =
	/// new TestErrorReporter(null, new String[] { "first warning" });
	/// ...
	/// </summary>
	/// <remarks>
	/// <p>An error reporter for testing that verifies that messages reported to the
	/// reporter are expected.</p>
	/// <p>Sample use</p>
	/// <pre>
	/// TestErrorReporter e =
	/// new TestErrorReporter(null, new String[] { "first warning" });
	/// ...
	/// assertTrue(e.hasEncounteredAllWarnings());
	/// </pre>
	/// </remarks>
	/// <author>Pascal-Louis Perez</author>
	public class TestErrorReporter : Assert, ErrorReporter
	{
		private readonly string[] errors;

		private readonly string[] warnings;

		private int errorsIndex = 0;

		private int warningsIndex = 0;

		public TestErrorReporter(string[] errors, string[] warnings)
		{
			this.errors = errors;
			this.warnings = warnings;
		}

		public virtual void Error(string message, string sourceName, int line, string lineSource, int lineOffset)
		{
			if (errors != null && errorsIndex < errors.Length)
			{
				NUnit.Framework.Assert.AreEqual(errors[errorsIndex++], message);
			}
			else
			{
				Fail("extra error: " + message);
			}
		}

		public virtual void Warning(string message, string sourceName, int line, string lineSource, int lineOffset)
		{
			if (warnings != null && warningsIndex < warnings.Length)
			{
				NUnit.Framework.Assert.AreEqual(warnings[warningsIndex++], message);
			}
			else
			{
				Fail("extra warning: " + message);
			}
		}

		public virtual EvaluatorException RuntimeError(string message, string sourceName, int line, string lineSource, int lineOffset)
		{
			throw new NotSupportedException();
		}

		/// <summary>Returns whether all warnings were reported to this reporter.</summary>
		/// <remarks>Returns whether all warnings were reported to this reporter.</remarks>
		public virtual bool HasEncounteredAllWarnings()
		{
			return (warnings == null) ? warningsIndex == 0 : warnings.Length == warningsIndex;
		}

		/// <summary>Returns whether all errors were reported to this reporter.</summary>
		/// <remarks>Returns whether all errors were reported to this reporter.</remarks>
		public virtual bool HasEncounteredAllErrors()
		{
			return (errors == null) ? errorsIndex == 0 : errors.Length == errorsIndex;
		}
	}
}
