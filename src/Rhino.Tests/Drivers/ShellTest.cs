/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Rhino.Tools.Shell;
using Sharpen;
using Thread = System.Threading.Thread;

namespace Rhino.Drivers
{
	/// <version>$Id: ShellTest.java,v 1.14 2011/03/29 15:17:49 hannes%helma.at Exp $</version>
	public class ShellTest
	{
		public static bool DIRECTORY_FILTER(FileSystemInfo pathname)
		{
			return pathname is DirectoryInfo && !pathname.Name.Equals("CVS");
		}

		public static bool TEST_FILTER(FileSystemInfo pathname)
		{
			var name = pathname.Name;
			return name.EndsWith(".js") && !name.Equals("shell.js") && !name.Equals("browser.js") && !name.Equals("template.js");
		}

		public static string GetStackTrace(Exception t)
		{
			ByteArrayOutputStream bytes = new ByteArrayOutputStream();
			Runtime.PrintStackTrace(t, new StreamWriter(bytes));
			return Encoding.UTF8.GetString(bytes.ToByteArray());
		}

		private static void RunFileIfExists(Context cx, Scriptable global, FileInfo f)
		{
			if (f.Exists)
			{
				Rhino.Tools.Shell.Program.ProcessFileNoThrow(cx, global, f.FullName);
			}
		}

		private class TestState
		{
			internal bool finished;

			internal ErrorReporterWrapper errors;

			internal int exitCode = 0;
		}
		public abstract class Status
		{
			public bool Negative { get; set; }

			public void HadErrors(JsError[] errors)
			{
				if (!Negative && errors.Length > 0)
				{
					Failed("JavaScript errors:\n" + JsError.ToString(errors));
				}
				else
				{
					if (Negative && errors.Length == 0)
					{
						Failed("Should have produced runtime error.");
					}
				}
			}

			public void HadErrors(FileInfo jsFile, JsError[] errors)
			{
				if (!Negative && errors.Length > 0)
				{
					Failed("JavaScript errors in " + jsFile.FullName + ":\n" + JsError.ToString(errors));
				}
				else
				{
					if (Negative && errors.Length == 0)
					{
						Failed("Should have produced runtime error in " + jsFile.FullName + ".");
					}
				}
			}

			public abstract void Running(FileInfo jsFile);

			public abstract void Failed(string s);

			public abstract void Threw(Exception t);

			public abstract void TimedOut();

			public abstract void ExitCodesWere(int expected, int actual);

			public abstract void OutputWas(string s);

			internal static Status Compose(Status[] array)
			{
				return new CompositeStatus(array);
			}
		}

		private class ErrorReporterWrapper : ErrorReporter
		{
			private ErrorReporter original;

			public List<ShellTest.JsError> errors = new List<ShellTest.JsError>();

			internal ErrorReporterWrapper(ErrorReporter original)
			{
				this.original = original;
			}

			private void AddError(string @string, string string0, int i, string string1, int i0)
			{
				errors.Add(new ShellTest.JsError(@string, string0, i, string1, i0));
			}

			public virtual void Warning(string @string, string string0, int i, string string1, int i0)
			{
				original.Warning(@string, string0, i, string1, i0);
			}

			public virtual EvaluatorException RuntimeError(string @string, string string0, int i, string string1, int i0)
			{
				return original.RuntimeError(@string, string0, i, string1, i0);
			}

			public virtual void Error(string @string, string string0, int i, string string1, int i0)
			{
				AddError(@string, string0, i, string1, i0);
			}
		}

		public abstract class Parameters
		{
			public abstract int GetTimeoutMilliseconds();
		}

		private static void CallStop(Thread t)
		{
			t.Abort();
		}

		/// <exception cref="System.Exception"></exception>
		public static void Run(ShellContextFactory shellContextFactory, FileInfo jsFile, Parameters parameters, Status status)
		{
			Global global = new Global();
			MemoryStream @out = new MemoryStream();
			TextWriter p = new StreamWriter(@out);
			global.SetOut(p);
			global.SetErr(p);
			global.DefineFunctionProperties(new[] { "options" }, typeof(ShellTest), ScriptableObject.DONTENUM | ScriptableObject.PERMANENT | ScriptableObject.READONLY);
			// test suite expects keywords to be disallowed as identifiers
			shellContextFactory.SetAllowReservedKeywords(false);
			TestState testState = new TestState();
			if (jsFile.Name.EndsWith("-n.js"))
			{
				status.Negative = true;
			}
			Exception[] thrown = { null };
			ThreadStart thread = () => Thread(shellContextFactory, jsFile, status, testState, global, thrown);
			var t = new Thread(thread)
			{
				Name = jsFile.Name,
				IsBackground = true
			};
			t.Start();
			t.Join(parameters.GetTimeoutMilliseconds());
			lock (testState)
			{
				if (!testState.finished)
				{
					CallStop(t);
					status.TimedOut();
				}
			}
			int expectedExitCode = 0;
			p.Flush();
			status.OutputWas(Encoding.UTF8.GetString(@out.ToArray()));
			StreamReader r = new StreamReader(new MemoryStream(@out.ToArray()));
			string failures = string.Empty;
			for (; ; )
			{
				string s = r.ReadLine();
				if (s == null)
				{
					break;
				}
				if (s.IndexOf("FAILED!") != -1)
				{
					failures += s + '\n';
				}
				int expex = s.IndexOf("EXPECT EXIT CODE ");
				if (expex != -1)
				{
					expectedExitCode = s[expex + "EXPECT EXIT CODE ".Length] - '0';
				}
			}
			if (thrown[0] != null)
			{
				status.Threw(thrown[0]);
			}
			status.ExitCodesWere(expectedExitCode, testState.exitCode);
			if (failures != string.Empty)
			{
				status.Failed(failures);
			}
		}

		private static void Thread(ShellContextFactory shellContextFactory, FileInfo jsFile, Status status, TestState testState, Global global, Exception[] thrown)
		{
			try
			{
				shellContextFactory.Call(cx => O(jsFile, status, testState, global, cx));
			}
			catch (Exception t2)
			{
				thrown [0] = t2;
			}
			finally
			{
				lock (testState)
				{
					testState.finished = true;
				}
			}
		}

		private static object O(FileInfo jsFile, Status status, TestState testState, Global global, Context cx)
		{
			status.Running(jsFile);
			testState.errors = new ErrorReporterWrapper(cx.GetErrorReporter());
			cx.SetErrorReporter(testState.errors);

			global.Init(cx);

			try
			{
				RunFileIfExists(cx, global, new FileInfo(jsFile.DirectoryName + "/../../" + "shell.js"));
				RunFileIfExists(cx, global, new FileInfo(jsFile.DirectoryName + "/../" + "shell.js"));
				RunFileIfExists(cx, global, new FileInfo(jsFile.DirectoryName + "/shell.js"));
				RunFileIfExists(cx, global, jsFile);
				status.HadErrors(jsFile, testState.errors.errors.ToArray());
			}
			catch (ThreadAbortException)
			{
			}
			catch (Exception t1)
			{
				status.Threw(t1);
			}
			return null;
		}

		// Global function to mimic options() function in spidermonkey.
		// It looks like this toggles jit compiler mode in spidermonkey
		// when called with "jit" as argument. Our version is a no-op
		// and returns an empty string.
		public static string Options()
		{
			return string.Empty;
		}
		private sealed class CompositeStatus : Status
		{
			public CompositeStatus(Status[] array)
			{
				this.array = array;
			}

			public override void Running(FileInfo file)
			{
				foreach (Status status in array)
				{
					status.Running(file);
				}
			}

			public override void Threw(Exception t)
			{
				foreach (Status status in array)
				{
					status.Threw(t);
				}
			}

			public override void Failed(string s)
			{
				foreach (Status status in array)
				{
					status.Failed(s);
				}
			}

			public override void ExitCodesWere(int expected, int actual)
			{
				foreach (Status status in array)
				{
					status.ExitCodesWere(expected, actual);
				}
			}

			public override void OutputWas(string s)
			{
				foreach (Status status in array)
				{
					status.OutputWas(s);
				}
			}

			public override void TimedOut()
			{
				foreach (Status status in array)
				{
					status.TimedOut();
				}
			}

			private readonly Status[] array;
		}
		public class JsError
		{
			internal static string ToString(JsError[] e)
			{
				string rv = String.Empty;
				for (int i = 0; i < e.Length; i++)
				{
					rv += e[i].ToString();
					if (i + 1 != e.Length)
					{
						rv += "\n";
					}
				}
				return rv;
			}

			private string message;

			private string sourceName;

			private int line;

			private string lineSource;

			private int lineOffset;

			internal JsError(string message, string sourceName, int line, string lineSource, int lineOffset)
			{
				this.message = message;
				this.sourceName = sourceName;
				this.line = line;
				this.lineSource = lineSource;
				this.lineOffset = lineOffset;
			}

			public override string ToString()
			{
				string locationLine = String.Empty;
				if (sourceName != null)
				{
					locationLine += sourceName + ":";
				}
				if (line != 0)
				{
					locationLine += line + ": ";
				}
				locationLine += message;
				string sourceLine = lineSource;
				string errCaret = null;
				if (lineSource != null)
				{
					errCaret = String.Empty;
					for (int i = 0; i < lineSource.Length; i++)
					{
						char c = lineSource[i];
						if (i < lineOffset - 1)
						{
							if (c == '\t')
							{
								errCaret += "\t";
							}
							else
							{
								errCaret += " ";
							}
						}
						else
						{
							if (i == lineOffset - 1)
							{
								errCaret += "^";
							}
						}
					}
				}
				string rv = locationLine;
				if (sourceLine != null)
				{
					rv += "\n" + sourceLine;
				}
				if (errCaret != null)
				{
					rv += "\n" + errCaret;
				}
				return rv;
			}

			internal virtual string GetMessage()
			{
				return message;
			}

			internal virtual string GetSourceName()
			{
				return sourceName;
			}

			internal virtual int GetLine()
			{
				return line;
			}

			internal virtual string GetLineSource()
			{
				return lineSource;
			}

			internal virtual int GetLineOffset()
			{
				return lineOffset;
			}
		}
	}
}
