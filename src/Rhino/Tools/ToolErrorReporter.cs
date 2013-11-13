/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Globalization;
using System.IO;
using System.Text;
using Rhino;
using Sharpen;

namespace Rhino.Tools
{
	/// <summary>Error reporter for tools.</summary>
	/// <remarks>
	/// Error reporter for tools.
	/// Currently used by both the shell and the compiler.
	/// </remarks>
	public class ToolErrorReporter : ErrorReporter
	{
		public ToolErrorReporter(bool reportWarnings) : this(reportWarnings, System.Console.Error)
		{
		}

		public ToolErrorReporter(bool reportWarnings, TextWriter err)
		{
			this.reportWarnings = reportWarnings;
			this.err = err;
		}

		/// <summary>
		/// Look up the message corresponding to messageId in the
		/// org.mozilla.javascript.tools.shell.resources.Messages property file.
		/// </summary>
		/// <remarks>
		/// Look up the message corresponding to messageId in the
		/// org.mozilla.javascript.tools.shell.resources.Messages property file.
		/// For internationalization support.
		/// </remarks>
		public static string GetMessage(string messageId)
		{
			return GetMessage(messageId, (object[])null);
		}

		public static string GetMessage(string messageId, string argument)
		{
			object[] args = new object[] { argument };
			return GetMessage(messageId, args);
		}

		public static string GetMessage(string messageId, object arg1, object arg2)
		{
			object[] args = new object[] { arg1, arg2 };
			return GetMessage(messageId, args);
		}

		public static string GetMessage(string messageId, object[] args)
		{
			Context cx = Context.GetCurrentContext();
			CultureInfo locale = cx == null ? CultureInfo.CurrentCulture : cx.GetLocale();
			// ResourceBundle does caching.
			ResourceBundle rb = ResourceBundle.GetBundle("org.mozilla.javascript.tools.resources.Messages", locale);
			string formatString;
			try
			{
				formatString = rb.GetString(messageId);
			}
			catch (MissingResourceException)
			{
				throw new Exception("no message resource found for message property " + messageId);
			}
			if (args == null)
			{
				return formatString;
			}
			else
			{
				MessageFormat formatter = new MessageFormat(formatString);
				return formatter.Format(args);
			}
		}

		private static string GetExceptionMessage(RhinoException ex)
		{
			string msg;
			if (ex is JavaScriptException)
			{
				msg = GetMessage("msg.uncaughtJSException", ex.Details());
			}
			else
			{
				if (ex is EcmaError)
				{
					msg = GetMessage("msg.uncaughtEcmaError", ex.Details());
				}
				else
				{
					if (ex is EvaluatorException)
					{
						msg = ex.Details();
					}
					else
					{
						msg = ex.ToString();
					}
				}
			}
			return msg;
		}

		public virtual void Warning(string message, string sourceName, int line, string lineSource, int lineOffset)
		{
			if (!reportWarnings)
			{
				return;
			}
			ReportErrorMessage(message, sourceName, line, lineSource, lineOffset, true);
		}

		public virtual void Error(string message, string sourceName, int line, string lineSource, int lineOffset)
		{
			hasReportedErrorFlag = true;
			ReportErrorMessage(message, sourceName, line, lineSource, lineOffset, false);
		}

		public virtual EvaluatorException RuntimeError(string message, string sourceName, int line, string lineSource, int lineOffset)
		{
			return new EvaluatorException(message, sourceName, line, lineSource, lineOffset);
		}

		public virtual bool HasReportedError()
		{
			return hasReportedErrorFlag;
		}

		public virtual bool IsReportingWarnings()
		{
			return this.reportWarnings;
		}

		public virtual void SetIsReportingWarnings(bool reportWarnings)
		{
			this.reportWarnings = reportWarnings;
		}

		public static void ReportException(ErrorReporter er, RhinoException ex)
		{
			if (er is Rhino.Tools.ToolErrorReporter)
			{
				((Rhino.Tools.ToolErrorReporter)er).ReportException(ex);
			}
			else
			{
				string msg = GetExceptionMessage(ex);
				er.Error(msg, ex.SourceName(), ex.LineNumber(), ex.LineSource(), ex.ColumnNumber());
			}
		}

		public virtual void ReportException(RhinoException ex)
		{
			if (ex is WrappedException)
			{
				WrappedException we = (WrappedException)ex;
				Sharpen.Runtime.PrintStackTrace(we, err);
			}
			else
			{
				string lineSeparator = SecurityUtilities.GetSystemProperty("line.separator");
				string msg = GetExceptionMessage(ex) + lineSeparator + ex.GetScriptStackTrace();
				ReportErrorMessage(msg, ex.SourceName(), ex.LineNumber(), ex.LineSource(), ex.ColumnNumber(), false);
			}
		}

		private void ReportErrorMessage(string message, string sourceName, int line, string lineSource, int lineOffset, bool justWarning)
		{
			if (line > 0)
			{
				string lineStr = line.ToString();
				if (sourceName != null)
				{
					object[] args = new object[] { sourceName, lineStr, message };
					message = GetMessage("msg.format3", args);
				}
				else
				{
					object[] args = new object[] { lineStr, message };
					message = GetMessage("msg.format2", args);
				}
			}
			else
			{
				object[] args = new object[] { message };
				message = GetMessage("msg.format1", args);
			}
			if (justWarning)
			{
				message = GetMessage("msg.warning", message);
			}
			err.WriteLine(messagePrefix + message);
			if (null != lineSource)
			{
				err.WriteLine(messagePrefix + lineSource);
				err.WriteLine(messagePrefix + BuildIndicator(lineOffset));
			}
		}

		private string BuildIndicator(int offset)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < offset - 1; i++)
			{
				sb.Append(".");
			}
			sb.Append("^");
			return sb.ToString();
		}

		private const string messagePrefix = "js: ";

		private bool hasReportedErrorFlag;

		private bool reportWarnings;

		private TextWriter err;
	}
}
