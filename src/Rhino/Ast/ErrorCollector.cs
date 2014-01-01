/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Text;
using Rhino;
using Rhino.Ast;
using Sharpen;

namespace Rhino.Ast
{
	/// <summary>An error reporter that gathers the errors and warnings for later display.</summary>
	/// <remarks>
	/// An error reporter that gathers the errors and warnings for later display.
	/// This a useful
	/// <see cref="Rhino.ErrorReporter">Rhino.ErrorReporter</see>
	/// when the
	/// <see cref="Rhino.CompilerEnvirons">Rhino.CompilerEnvirons</see>
	/// is set to
	/// ide-mode (for IDEs).
	/// </remarks>
	/// <author>Steve Yegge</author>
	public class ErrorCollector : IdeErrorReporter
	{
		private IList<ParseProblem> errors = new List<ParseProblem>();

		/// <summary>This is not called during AST generation.</summary>
		/// <remarks>
		/// This is not called during AST generation.
		/// <see cref="Warning(string, string, int, int)">Warning(string, string, int, int)</see>
		/// is used instead.
		/// </remarks>
		/// <exception cref="System.NotSupportedException">System.NotSupportedException</exception>
		public virtual void Warning(string message, string sourceName, int line, string lineSource, int lineOffset)
		{
			throw new NotSupportedException();
		}

		/// <inheritDoc></inheritDoc>
		public virtual void Warning(string message, string sourceName, int offset, int length)
		{
			errors.AddItem(new ParseProblem(ParseProblem.Type.Warning, message, sourceName, offset, length));
		}

		/// <summary>This is not called during AST generation.</summary>
		/// <remarks>
		/// This is not called during AST generation.
		/// <see cref="Warning(string, string, int, int)">Warning(string, string, int, int)</see>
		/// is used instead.
		/// </remarks>
		/// <exception cref="System.NotSupportedException">System.NotSupportedException</exception>
		public virtual void Error(string message, string sourceName, int line, string lineSource, int lineOffset)
		{
			throw new NotSupportedException();
		}

		/// <inheritDoc></inheritDoc>
		public virtual void Error(string message, string sourceName, int fileOffset, int length)
		{
			errors.AddItem(new ParseProblem(ParseProblem.Type.Error, message, sourceName, fileOffset, length));
		}

		/// <inheritDoc></inheritDoc>
		public virtual EvaluatorException RuntimeError(string message, string sourceName, int line, string lineSource, int lineOffset)
		{
			throw new NotSupportedException();
		}

		/// <summary>Returns the list of errors and warnings produced during parsing.</summary>
		/// <remarks>Returns the list of errors and warnings produced during parsing.</remarks>
		public virtual IList<ParseProblem> GetErrors()
		{
			return errors;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder(errors.Count * 100);
			foreach (ParseProblem pp in errors)
			{
				sb.Append(pp.ToString()).Append("\n");
			}
			return sb.ToString();
		}
	}
}
