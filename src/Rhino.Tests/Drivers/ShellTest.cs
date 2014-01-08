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
using Rhino;
using Rhino.Drivers;
using Rhino.Tools.Shell;
using Sharpen;

namespace Rhino.Drivers
{
	/// <version>$Id: ShellTest.java,v 1.14 2011/03/29 15:17:49 hannes%helma.at Exp $</version>
	public class ShellTest
	{
		private sealed class _FileFilter_25 : FileFilter
		{
			public _FileFilter_25()
			{
			}

			public bool Accept(FilePath pathname)
			{
				return pathname.IsDirectory() && !pathname.GetName().Equals("CVS");
			}
		}

		public static readonly FileFilter DIRECTORY_FILTER = new _FileFilter_25();

		private sealed class _FileFilter_32 : FileFilter
		{
			public _FileFilter_32()
			{
			}

			public bool Accept(FilePath pathname)
			{
				return pathname.GetName().EndsWith(".js") && !pathname.GetName().Equals("shell.js") && !pathname.GetName().Equals("browser.js") && !pathname.GetName().Equals("template.js");
			}
		}

		public static readonly FileFilter TEST_FILTER = new _FileFilter_32();

		public static string GetStackTrace(Exception t)
		{
			ByteArrayOutputStream bytes = new ByteArrayOutputStream();
			Sharpen.Runtime.PrintStackTrace(t, new TextWriter(bytes));
			return Sharpen.Runtime.GetStringForBytes(bytes.ToByteArray());
		}

		private static void RunFileIfExists(Context cx, Scriptable global, FilePath f)
		{
			if (f.IsFile())
			{
				Main.ProcessFileNoThrow(cx, global, f.GetPath());
			}
		}

		private class TestState
		{
			internal bool finished;

			internal ShellTest.ErrorReporterWrapper errors;

			internal int exitCode = 0;
		}

		public abstract class Status
		{
			private bool negative;

			public void SetNegative()
			{
				this.negative = true;
			}

			public bool IsNegative()
			{
				return this.negative;
			}

			public void HadErrors(ShellTest.Status.JsError[] errors)
			{
				if (!negative && errors.Length > 0)
				{
					Failed("JavaScript errors:\n" + ShellTest.Status.JsError.ToString(errors));
				}
				else
				{
					if (negative && errors.Length == 0)
					{
						Failed("Should have produced runtime error.");
					}
				}
			}

			public void HadErrors(FilePath jsFile, ShellTest.Status.JsError[] errors)
			{
				if (!negative && errors.Length > 0)
				{
					Failed("JavaScript errors in " + jsFile + ":\n" + ShellTest.Status.JsError.ToString(errors));
				}
				else
				{
					if (negative && errors.Length == 0)
					{
						Failed("Should have produced runtime error in " + jsFile + ".");
					}
				}
			}

			public abstract void Running(FilePath jsFile);

			public abstract void Failed(string s);

			public abstract void Threw(Exception t);

			public abstract void TimedOut();

			public abstract void ExitCodesWere(int expected, int actual);

			public abstract void OutputWas(string s);

			internal static ShellTest.Status Compose(ShellTest.Status[] array)
			{
				return new _Status_96(array);
			}

			private sealed class _Status_96 : ShellTest.Status
			{
				public _Status_96(ShellTest.Status[] array)
				{
					this.array = array;
				}

				public override void Running(FilePath file)
				{
					for (int i = 0; i < array.Length; i++)
					{
						array[i].Running(file);
					}
				}

				public override void Threw(Exception t)
				{
					for (int i = 0; i < array.Length; i++)
					{
						array[i].Threw(t);
					}
				}

				public override void Failed(string s)
				{
					for (int i = 0; i < array.Length; i++)
					{
						array[i].Failed(s);
					}
				}

				public override void ExitCodesWere(int expected, int actual)
				{
					for (int i = 0; i < array.Length; i++)
					{
						array[i].ExitCodesWere(expected, actual);
					}
				}

				public override void OutputWas(string s)
				{
					for (int i = 0; i < array.Length; i++)
					{
						array[i].OutputWas(s);
					}
				}

				public override void TimedOut()
				{
					for (int i = 0; i < array.Length; i++)
					{
						array[i].TimedOut();
					}
				}

				private readonly ShellTest.Status[] array;
			}

			internal class JsError
			{
				internal static string ToString(ShellTest.Status.JsError[] e)
				{
					string rv = string.Empty;
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
					string locationLine = string.Empty;
					if (sourceName != null)
					{
						locationLine += sourceName + ":";
					}
					if (line != 0)
					{
						locationLine += line + ": ";
					}
					locationLine += message;
					string sourceLine = this.lineSource;
					string errCaret = null;
					if (lineSource != null)
					{
						errCaret = string.Empty;
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

		private class ErrorReporterWrapper : ErrorReporter
		{
			private ErrorReporter original;

			private List<ShellTest.Status.JsError> errors = new List<ShellTest.Status.JsError>();

			internal ErrorReporterWrapper(ErrorReporter original)
			{
				this.original = original;
			}

			private void AddError(string @string, string string0, int i, string string1, int i0)
			{
				errors.Add(new ShellTest.Status.JsError(@string, string0, i, string1, i0));
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

		private static void CallStop(Sharpen.Thread t)
		{
			t.Stop();
		}

		/// <exception cref="System.Exception"></exception>
		public static void Run(ShellContextFactory shellContextFactory, FilePath jsFile, ShellTest.Parameters parameters, ShellTest.Status status)
		{
			Global global = new Global();
			ByteArrayOutputStream @out = new ByteArrayOutputStream();
			TextWriter p = new TextWriter(@out);
			global.SetOut(p);
			global.SetErr(p);
			global.DefineFunctionProperties(new string[] { "options" }, typeof(ShellTest), ScriptableObject.DONTENUM | ScriptableObject.PERMANENT | ScriptableObject.READONLY);
			// test suite expects keywords to be disallowed as identifiers
			shellContextFactory.SetAllowReservedKeywords(false);
			ShellTest.TestState testState = new ShellTest.TestState();
			if (jsFile.GetName().EndsWith("-n.js"))
			{
				status.SetNegative();
			}
			Exception[] thrown = new Exception[] { null };
			Sharpen.Thread t = new Sharpen.Thread(new _Runnable_274(shellContextFactory, thrown, testState, status, jsFile, global), jsFile.GetPath());
			t.SetDaemon(true);
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
			status.OutputWas(Sharpen.Runtime.GetStringForBytes(@out.ToByteArray()));
			BufferedReader r = new BufferedReader(new StreamReader(new ByteArrayInputStream(@out.ToByteArray())));
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

		private sealed class _Runnable_274 : Runnable
		{
			public _Runnable_274(ShellContextFactory shellContextFactory, Exception[] thrown, ShellTest.TestState testState, ShellTest.Status status, FilePath jsFile, Global global)
			{
				this.shellContextFactory = shellContextFactory;
				this.thrown = thrown;
				this.testState = testState;
				this.status = status;
				this.jsFile = jsFile;
				this.global = global;
			}

			public void Run()
			{
				try
				{
					shellContextFactory.Call(new _ContextAction_280(status, jsFile, testState, global));
				}
				catch (Exception t)
				{
					thrown[0] = t;
				}
				catch (Exception t)
				{
					thrown[0] = t;
				}
				finally
				{
					lock (testState)
					{
						testState.finished = true;
					}
				}
			}

			private sealed class _ContextAction_280 : ContextAction
			{
				public _ContextAction_280(ShellTest.Status status, FilePath jsFile, ShellTest.TestState testState, Global global)
				{
					this.status = status;
					this.jsFile = jsFile;
					this.testState = testState;
					this.global = global;
				}

				public object Run(Context cx)
				{
					status.Running(jsFile);
					testState.errors = new ShellTest.ErrorReporterWrapper(cx.GetErrorReporter());
					cx.SetErrorReporter(testState.errors);
					global.Init(cx);
					try
					{
						ShellTest.RunFileIfExists(cx, global, new FilePath(jsFile.GetParentFile().GetParentFile().GetParentFile(), "shell.js"));
						ShellTest.RunFileIfExists(cx, global, new FilePath(jsFile.GetParentFile().GetParentFile(), "shell.js"));
						ShellTest.RunFileIfExists(cx, global, new FilePath(jsFile.GetParentFile(), "shell.js"));
						ShellTest.RunFileIfExists(cx, global, jsFile);
						status.HadErrors(jsFile, Sharpen.Collections.ToArray(testState.errors.errors, new ShellTest.Status.JsError[0]));
					}
					catch (ThreadDeath)
					{
					}
					catch (Exception t)
					{
						status.Threw(t);
					}
					return null;
				}

				private readonly ShellTest.Status status;

				private readonly FilePath jsFile;

				private readonly ShellTest.TestState testState;

				private readonly Global global;
			}

			private readonly ShellContextFactory shellContextFactory;

			private readonly Exception[] thrown;

			private readonly ShellTest.TestState testState;

			private readonly ShellTest.Status status;

			private readonly FilePath jsFile;

			private readonly Global global;
		}

		// Global function to mimic options() function in spidermonkey.
		// It looks like this toggles jit compiler mode in spidermonkey
		// when called with "jit" as argument. Our version is a no-op
		// and returns an empty string.
		public static string Options()
		{
			return string.Empty;
		}
	}
}
