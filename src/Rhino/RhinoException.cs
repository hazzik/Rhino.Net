/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using Sharpen;

namespace Rhino
{
	/// <summary>The class of exceptions thrown by the JavaScript engine.</summary>
	/// <remarks>The class of exceptions thrown by the JavaScript engine.</remarks>
	[System.Serializable]
	public abstract class RhinoException : Exception
	{
		protected RhinoException()
		{
			Evaluator e = Context.CreateInterpreter();
			if (e != null)
			{
				e.CaptureStackInfo(this);
			}
		}

		protected RhinoException(string details) : base(details)
		{
			Evaluator e = Context.CreateInterpreter();
			if (e != null)
			{
				e.CaptureStackInfo(this);
			}
		}

		protected RhinoException(string message, Exception inner)
			: base(message, inner)
		{
			Evaluator e = Context.CreateInterpreter();
			if (e != null)
			{
				e.CaptureStackInfo(this);
			}
		}

		protected RhinoException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		public sealed override string Message
		{
			get
			{
				string details = Details();
				if (sourceName == null || lineNumber <= 0)
				{
					return details;
				}
				StringBuilder buf = new StringBuilder(details);
				buf.Append(" (");
				if (sourceName != null)
				{
					buf.Append(sourceName);
				}
				if (lineNumber > 0)
				{
					buf.Append('#');
					buf.Append(lineNumber);
				}
				buf.Append(')');
				return buf.ToString();
			}
		}

		public virtual string Details()
		{
			return base.Message;
		}

		/// <summary>
		/// Get the uri of the script source containing the error, or null
		/// if that information is not available.
		/// </summary>
		/// <remarks>
		/// Get the uri of the script source containing the error, or null
		/// if that information is not available.
		/// </remarks>
		public string SourceName()
		{
			return sourceName;
		}

		/// <summary>Initialize the uri of the script source containing the error.</summary>
		/// <remarks>Initialize the uri of the script source containing the error.</remarks>
		/// <param name="sourceName">
		/// the uri of the script source responsible for the error.
		/// It should not be <tt>null</tt>.
		/// </param>
		/// <exception cref="System.InvalidOperationException">if the method is called more then once.</exception>
		public void InitSourceName(string sourceName)
		{
			if (sourceName == null)
			{
				throw new ArgumentException();
			}
			if (this.sourceName != null)
			{
				throw new InvalidOperationException();
			}
			this.sourceName = sourceName;
		}

		/// <summary>
		/// Returns the line number of the statement causing the error,
		/// or zero if not available.
		/// </summary>
		/// <remarks>
		/// Returns the line number of the statement causing the error,
		/// or zero if not available.
		/// </remarks>
		public int LineNumber()
		{
			return lineNumber;
		}

		/// <summary>Initialize the line number of the script statement causing the error.</summary>
		/// <remarks>Initialize the line number of the script statement causing the error.</remarks>
		/// <param name="lineNumber">
		/// the line number in the script source.
		/// It should be positive number.
		/// </param>
		/// <exception cref="System.InvalidOperationException">if the method is called more then once.</exception>
		public void InitLineNumber(int lineNumber)
		{
			if (lineNumber <= 0)
			{
				throw new ArgumentException(lineNumber.ToString());
			}
			if (this.lineNumber > 0)
			{
				throw new InvalidOperationException();
			}
			this.lineNumber = lineNumber;
		}

		/// <summary>The column number of the location of the error, or zero if unknown.</summary>
		/// <remarks>The column number of the location of the error, or zero if unknown.</remarks>
		public int ColumnNumber()
		{
			return columnNumber;
		}

		/// <summary>Initialize the column number of the script statement causing the error.</summary>
		/// <remarks>Initialize the column number of the script statement causing the error.</remarks>
		/// <param name="columnNumber">
		/// the column number in the script source.
		/// It should be positive number.
		/// </param>
		/// <exception cref="System.InvalidOperationException">if the method is called more then once.</exception>
		public void InitColumnNumber(int columnNumber)
		{
			if (columnNumber <= 0)
			{
				throw new ArgumentException(columnNumber.ToString());
			}
			if (this.columnNumber > 0)
			{
				throw new InvalidOperationException();
			}
			this.columnNumber = columnNumber;
		}

		/// <summary>The source text of the line causing the error, or null if unknown.</summary>
		/// <remarks>The source text of the line causing the error, or null if unknown.</remarks>
		public string LineSource()
		{
			return lineSource;
		}

		/// <summary>Initialize the text of the source line containing the error.</summary>
		/// <remarks>Initialize the text of the source line containing the error.</remarks>
		/// <param name="lineSource">
		/// the text of the source line responsible for the error.
		/// It should not be <tt>null</tt>.
		/// </param>
		/// <exception cref="System.InvalidOperationException">if the method is called more then once.</exception>
		public void InitLineSource(string lineSource)
		{
			if (lineSource == null)
			{
				throw new ArgumentException();
			}
			if (this.lineSource != null)
			{
				throw new InvalidOperationException();
			}
			this.lineSource = lineSource;
		}

		internal void RecordErrorOrigin(string sourceName, int lineNumber, string lineSource, int columnNumber)
		{
			// XXX: for compatibility allow for now -1 to mean 0
			if (lineNumber == -1)
			{
				lineNumber = 0;
			}
			if (sourceName != null)
			{
				InitSourceName(sourceName);
			}
			if (lineNumber != 0)
			{
				InitLineNumber(lineNumber);
			}
			if (lineSource != null)
			{
				InitLineSource(lineSource);
			}
			if (columnNumber != 0)
			{
				InitColumnNumber(columnNumber);
			}
		}

		private string GenerateStackTrace()
		{
			// Get stable reference to work properly with concurrent access
			string origStackTrace = ToString();
			Evaluator e = Context.CreateInterpreter();
			if (e != null)
			{
				return e.GetPatchedStack(this, origStackTrace);
			}
			return null;
		}

		/// <summary>Get a string representing the script stack of this exception.</summary>
		/// <remarks>
		/// Get a string representing the script stack of this exception.
		/// If optimization is enabled, this includes java stack elements
		/// whose source and method names suggest they have been generated
		/// by the Rhino script compiler.
		/// </remarks>
		/// <returns>a script stack dump</returns>
		/// <since>1.6R6</since>
		public virtual string GetScriptStackTrace()
		{
			StringBuilder buffer = new StringBuilder();
			string lineSeparator = Environment.NewLine;
			ScriptStackElement[] stack = GetScriptStack();
			foreach (ScriptStackElement elem in stack)
			{
				if (useMozillaStackStyle)
				{
					elem.RenderMozillaStyle(buffer);
				}
				else
				{
					elem.RenderJavaStyle(buffer);
				}
				buffer.Append(lineSeparator);
			}
			return buffer.ToString();
		}

		/// <summary>
		/// Get the script stack of this exception as an array of
		/// <see cref="ScriptStackElement">ScriptStackElement</see>
		/// s.
		/// If optimization is enabled, this includes java stack elements
		/// whose source and method names suggest they have been generated
		/// by the Rhino script compiler.
		/// </summary>
		/// <returns>the script stack for this exception</returns>
		/// <since>1.7R3</since>
		public virtual ScriptStackElement[] GetScriptStack()
		{
			IList<ScriptStackElement> list = new List<ScriptStackElement>();
			ScriptStackElement[][] interpreterStack = null;
			if (interpreterStackInfo != null)
			{
				var interpreter = Context.CreateInterpreter() as Interpreter;
				if (interpreter != null)
				{
					interpreterStack = interpreter.GetScriptStackElements(this);
				}
			}
			int interpreterStackIndex = 0;

			StackFrame[] stack = new StackTrace(this).GetFrames();
			// Pattern to recover function name from java method name -
			// see Codegen.getBodyMethodName()
			// kudos to Marc Guillemot for coming up with this
			foreach (var e in stack)
			{
				string fileName = e.GetFileName();
				var method = e.GetMethod();
				if (method.Name.StartsWith("_c_") && e.GetFileLineNumber() > -1 && fileName != null && !fileName.EndsWith(".cs"))
				{
					// the method representing the main script is always "_c_script_0" -
					// at least we hope so
					string methodName = GetMethodName(method);
					list.Add(new ScriptStackElement(fileName, methodName, e.GetFileLineNumber()));
				}
				else
				{
					if ("Rhino.Interpreter" == method.DeclaringType.FullName && "InterpretLoop" == method.Name && interpreterStack != null && interpreterStack.Length > interpreterStackIndex)
					{
						foreach (ScriptStackElement elem in interpreterStack[interpreterStackIndex++])
						{
							list.Add(elem);
						}
					}
				}
			}
			return list.ToArray();
		}

		private static string GetMethodName(MethodBase method)
		{
			string methodName = method.Name;
			if ("_c_script_0" == methodName) return null;
			var matches = methodNameRegex.Matches(methodName);
			if (matches.Count == 0) return null;
			Group grp = matches[0].Groups[1];
			if (!grp.Success) return null;
			return grp.Value;
		}

		public void PrintStackTrace(PrintWriter s)
		{
			if (interpreterStackInfo == null)
			{
				s.WriteLine(this);
			}
			else
			{
				s.Write(GenerateStackTrace());
			}
		}

		public void PrintStackTrace(TextWriter s)
		{
			if (interpreterStackInfo == null)
			{
				s.WriteLine(this);
			}
			else
			{
				s.Write(GenerateStackTrace());
			}
		}

		/// <summary>
		/// Returns true if subclasses of <code>RhinoException</code>
		/// use the Mozilla/Firefox style of rendering script stacks
		/// (<code>functionName()@fileName:lineNumber</code>)
		/// instead of Rhino's own Java-inspired format
		/// (<code>    at fileName:lineNumber (functionName)</code>).
		/// </summary>
		/// <remarks>
		/// Returns true if subclasses of <code>RhinoException</code>
		/// use the Mozilla/Firefox style of rendering script stacks
		/// (<code>functionName()@fileName:lineNumber</code>)
		/// instead of Rhino's own Java-inspired format
		/// (<code>    at fileName:lineNumber (functionName)</code>).
		/// </remarks>
		/// <returns>true if stack is rendered in Mozilla/Firefox style</returns>
		/// <seealso cref="ScriptStackElement">ScriptStackElement</seealso>
		/// <since>1.7R3</since>
		public static bool UsesMozillaStackStyle()
		{
			return useMozillaStackStyle;
		}

		/// <summary>
		/// Tell subclasses of <code>RhinoException</code> whether to
		/// use the Mozilla/Firefox style of rendering script stacks
		/// (<code>functionName()@fileName:lineNumber</code>)
		/// instead of Rhino's own Java-inspired format
		/// (<code>    at fileName:lineNumber (functionName)</code>)
		/// </summary>
		/// <param name="flag">whether to render stacks in Mozilla/Firefox style</param>
		/// <seealso cref="ScriptStackElement">ScriptStackElement</seealso>
		/// <since>1.7R3</since>
		public static void UseMozillaStackStyle(bool flag)
		{
			useMozillaStackStyle = flag;
		}

		private static bool useMozillaStackStyle = false;

		private string sourceName;

		private int lineNumber;

		private string lineSource;

		private int columnNumber;

		internal object interpreterStackInfo;

		internal int[] interpreterLineData;
	    private static readonly Regex methodNameRegex = new Regex ("_c_(.*)_\\d+", RegexOptions.Compiled);
	}
}
