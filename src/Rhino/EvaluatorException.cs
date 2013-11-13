/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino;
using Sharpen;

namespace Rhino
{
	/// <summary>The class of exceptions thrown by the JavaScript engine.</summary>
	/// <remarks>The class of exceptions thrown by the JavaScript engine.</remarks>
	[System.Serializable]
	public class EvaluatorException : RhinoException
	{
		internal const long serialVersionUID = -8743165779676009808L;

		public EvaluatorException(string detail) : base(detail)
		{
		}

		/// <summary>Create an exception with the specified detail message.</summary>
		/// <remarks>
		/// Create an exception with the specified detail message.
		/// Errors internal to the JavaScript engine will simply throw a
		/// RuntimeException.
		/// </remarks>
		/// <param name="detail">the error message</param>
		/// <param name="sourceName">the name of the source reponsible for the error</param>
		/// <param name="lineNumber">the line number of the source</param>
		public EvaluatorException(string detail, string sourceName, int lineNumber) : this(detail, sourceName, lineNumber, null, 0)
		{
		}

		/// <summary>Create an exception with the specified detail message.</summary>
		/// <remarks>
		/// Create an exception with the specified detail message.
		/// Errors internal to the JavaScript engine will simply throw a
		/// RuntimeException.
		/// </remarks>
		/// <param name="detail">the error message</param>
		/// <param name="sourceName">the name of the source responsible for the error</param>
		/// <param name="lineNumber">the line number of the source</param>
		/// <param name="columnNumber">
		/// the columnNumber of the source (may be zero if
		/// unknown)
		/// </param>
		/// <param name="lineSource">
		/// the source of the line containing the error (may be
		/// null if unknown)
		/// </param>
		public EvaluatorException(string detail, string sourceName, int lineNumber, string lineSource, int columnNumber) : base(detail)
		{
			RecordErrorOrigin(sourceName, lineNumber, lineSource, columnNumber);
		}

		[System.ObsoleteAttribute(@"Use RhinoException.SourceName() from the super class.")]
		public virtual string GetSourceName()
		{
			return SourceName();
		}

		[System.ObsoleteAttribute(@"Use RhinoException.LineNumber() from the super class.")]
		public virtual int GetLineNumber()
		{
			return LineNumber();
		}

		[System.ObsoleteAttribute(@"Use RhinoException.ColumnNumber() from the super class.")]
		public virtual int GetColumnNumber()
		{
			return ColumnNumber();
		}

		[System.ObsoleteAttribute(@"Use RhinoException.LineSource() from the super class.")]
		public virtual string GetLineSource()
		{
			return LineSource();
		}
	}
}
