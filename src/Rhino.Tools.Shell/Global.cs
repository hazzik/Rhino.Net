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
using Rhino;
using Rhino.CommonJS.Module;
using Rhino.CommonJS.Module.Provider;
using Rhino.Serialize;
using Rhino.Tools;
using Rhino.Tools.Shell;
using Sharpen;

namespace Rhino.Tools.Shell
{
	/// <summary>This class provides for sharing functions across multiple threads.</summary>
	/// <remarks>
	/// This class provides for sharing functions across multiple threads.
	/// This is of particular interest to server applications.
	/// </remarks>
	/// <author>Norris Boyd</author>
	[System.Serializable]
	public class Global : ImporterTopLevel
	{
		internal const long serialVersionUID = 4029130780977538005L;

		internal NativeArray history;

		internal bool attemptedJLineLoad;

		private ShellConsole console;

		private Stream inStream;

		private TextWriter outStream;

		private TextWriter errStream;

		private bool sealedStdLib = false;

		internal bool initialized;

		private QuitAction quitAction;

		private string[] prompts = new string[] { "js> ", "  > " };

		private Dictionary<string, string> doctestCanonicalizations;

		public Global()
		{
		}

		public Global(Context cx)
		{
			Init(cx);
		}

		public virtual bool IsInitialized()
		{
			return initialized;
		}

		/// <summary>Set the action to call from quit().</summary>
		/// <remarks>Set the action to call from quit().</remarks>
		public virtual void InitQuitAction(QuitAction quitAction)
		{
			if (quitAction == null)
			{
				throw new ArgumentException("quitAction is null");
			}
			if (this.quitAction != null)
			{
				throw new ArgumentException("The method is once-call.");
			}
			this.quitAction = quitAction;
		}

		public virtual void Init(ContextFactory factory)
		{
			factory.Call(cx =>
			{
				Init(cx);
				return null;
			});
		}

		public virtual void Init(Context cx)
		{
			// Define some global functions particular to the shell. Note
			// that these functions are not part of ECMA.
			InitStandardObjects(cx, sealedStdLib);
			string[] names = new string[] { "defineClass", "deserialize", "doctest", "gc", "help", "load", "loadClass", "print", "quit", "readFile", "readUrl", "runCommand", "seal", "serialize", "spawn", "sync", "toint32", "version" };
			DefineFunctionProperties(names, typeof(Rhino.Tools.Shell.Global), ScriptableObject.DONTENUM);
			// Set up "environment" in the global scope to provide access to the
			// System environment variables.
			Environment.DefineClass(this);
			Environment environment = new Environment(this);
			DefineProperty("environment", environment, ScriptableObject.DONTENUM);
			history = (NativeArray)cx.NewArray(this, 0);
			DefineProperty("history", history, ScriptableObject.DONTENUM);
			initialized = true;
		}

		public virtual Require InstallRequire(Context cx, IList<string> modulePath, bool sandboxed)
		{
			RequireBuilder rb = new RequireBuilder();
			rb.SetSandboxed(sandboxed);
			IList<Uri> uris = new List<Uri>();
			if (modulePath != null)
			{
				foreach (string path in modulePath)
				{
					try
					{
						Uri uri = new Uri(path);
						if (!uri.IsAbsoluteUri)
						{
							// call resolve("") to canonify the path
							uri = new FilePath(path).ToURI().Resolve(string.Empty);
						}
						if (!uri.ToString().EndsWith("/"))
						{
							// make sure URI always terminates with slash to
							// avoid loading from unintended locations
							uri = new Uri(uri + "/");
						}
						uris.Add(uri);
					}
					catch (URISyntaxException usx)
					{
						throw new Exception(usx);
					}
				}
			}
			rb.SetModuleScriptProvider(new SoftCachingModuleScriptProvider(new UrlModuleSourceProvider(uris, null)));
			Require require = rb.CreateRequire(cx, this);
			require.Install(this);
			return require;
		}

		/// <summary>Print a help message.</summary>
		/// <remarks>
		/// Print a help message.
		/// This method is defined as a JavaScript function.
		/// </remarks>
		public static void Help(Context cx, Scriptable thisObj, object[] args, Function funObj)
		{
			TextWriter @out = GetInstance(funObj).GetOut();
			@out.WriteLine(ToolErrorReporter.GetMessage("msg.help"));
		}

		public static void Gc(Context cx, Scriptable thisObj, object[] args, Function funObj)
		{
			System.GC.Collect();
		}

		/// <summary>Print the string values of its arguments.</summary>
		/// <remarks>
		/// Print the string values of its arguments.
		/// This method is defined as a JavaScript function.
		/// Note that its arguments are of the "varargs" form, which
		/// allows it to handle an arbitrary number of arguments
		/// supplied to the JavaScript function.
		/// </remarks>
		public static object Print(Context cx, Scriptable thisObj, object[] args, Function funObj)
		{
			TextWriter @out = GetInstance(funObj).GetOut();
			for (int i = 0; i < args.Length; i++)
			{
				if (i > 0)
				{
					@out.Write(" ");
				}
				// Convert the arbitrary JavaScript value into a string form.
				string s = Context.ToString(args[i]);
				@out.Write(s);
			}
			@out.WriteLine();
			return Context.GetUndefinedValue();
		}

		/// <summary>
		/// Call embedding-specific quit action passing its argument as
		/// int32 exit code.
		/// </summary>
		/// <remarks>
		/// Call embedding-specific quit action passing its argument as
		/// int32 exit code.
		/// This method is defined as a JavaScript function.
		/// </remarks>
		public static void Quit(Context cx, Scriptable thisObj, object[] args, Function funObj)
		{
			Rhino.Tools.Shell.Global global = GetInstance(funObj);
			if (global.quitAction != null)
			{
				int exitCode = (args.Length == 0 ? 0 : ScriptRuntime.ToInt32(args[0]));
				global.quitAction(cx, exitCode);
			}
		}

		/// <summary>Get and set the language version.</summary>
		/// <remarks>
		/// Get and set the language version.
		/// This method is defined as a JavaScript function.
		/// </remarks>
		public static double Version(Context cx, Scriptable thisObj, object[] args, Function funObj)
		{
			double result = cx.GetLanguageVersion();
			if (args.Length > 0)
			{
				double d = Context.ToNumber(args[0]);
				cx.SetLanguageVersion((int)d);
			}
			return result;
		}

		/// <summary>Load and execute a set of JavaScript source files.</summary>
		/// <remarks>
		/// Load and execute a set of JavaScript source files.
		/// This method is defined as a JavaScript function.
		/// </remarks>
		public static void Load(Context cx, Scriptable thisObj, object[] args, Function funObj)
		{
			foreach (object arg in args)
			{
				string file = Context.ToString(arg);
				try
				{
					Program.ProcessFile(cx, thisObj, file);
				}
				catch (IOException ioex)
				{
					string msg = ToolErrorReporter.GetMessage("msg.couldnt.read.source", file, ioex.Message);
					throw Context.ReportRuntimeError(msg);
				}
				catch (VirtualMachineError ex)
				{
					// Treat StackOverflow and OutOfMemory as runtime errors
					Sharpen.Runtime.PrintStackTrace(ex);
					string msg = ToolErrorReporter.GetMessage("msg.uncaughtJSException", ex.ToString());
					throw Context.ReportRuntimeError(msg);
				}
			}
		}

		/// <summary>
		/// Load a Java class that defines a JavaScript object using the
		/// conventions outlined in ScriptableObject.defineClass.
		/// </summary>
		/// <remarks>
		/// Load a Java class that defines a JavaScript object using the
		/// conventions outlined in ScriptableObject.defineClass.
		/// <p>
		/// This method is defined as a JavaScript function.
		/// </remarks>
		/// <exception>
		/// IllegalAccessException
		/// if access is not available
		/// to a reflected class member
		/// </exception>
		/// <exception>
		/// InstantiationException
		/// if unable to instantiate
		/// the named class
		/// </exception>
		/// <exception>
		/// InvocationTargetException
		/// if an exception is thrown
		/// during execution of methods of the named class
		/// </exception>
		/// <seealso cref="Rhino.ScriptableObject.DefineClass{T}(Rhino.Scriptable, System.Type{T})">Rhino.ScriptableObject.DefineClass&lt;T&gt;(Rhino.Scriptable, System.Type&lt;T&gt;)</seealso>
		/// <exception cref="System.MemberAccessException"></exception>
		/// <exception cref="Sharpen.InstantiationException"></exception>
		/// <exception cref="System.Reflection.TargetInvocationException"></exception>
		public static void DefineClass(Context cx, Scriptable thisObj, object[] args, Function funObj)
		{
			Type clazz = GetClass(args);
			if (!typeof(Scriptable).IsAssignableFrom(clazz))
			{
				throw ReportRuntimeError("msg.must.implement.Scriptable");
			}
			ScriptableObject.DefineClass(thisObj, (Type)clazz);
		}

		/// <summary>Load and execute a script compiled to a class file.</summary>
		/// <remarks>
		/// Load and execute a script compiled to a class file.
		/// <p>
		/// This method is defined as a JavaScript function.
		/// When called as a JavaScript function, a single argument is
		/// expected. This argument should be the name of a class that
		/// implements the Script interface, as will any script
		/// compiled by jsc.
		/// </remarks>
		/// <exception>
		/// IllegalAccessException
		/// if access is not available
		/// to the class
		/// </exception>
		/// <exception>
		/// InstantiationException
		/// if unable to instantiate
		/// the named class
		/// </exception>
		/// <exception cref="System.MemberAccessException"></exception>
		/// <exception cref="Sharpen.InstantiationException"></exception>
		public static void LoadClass(Context cx, Scriptable thisObj, object[] args, Function funObj)
		{
			Type clazz = GetClass(args);
			if (!typeof(Script).IsAssignableFrom(clazz))
			{
				throw ReportRuntimeError("msg.must.implement.Script");
			}
			Script script = (Script)System.Activator.CreateInstance(clazz);
			script.Exec(cx, thisObj);
		}

		private static Type GetClass(object[] args)
		{
			if (args.Length == 0)
			{
				throw ReportRuntimeError("msg.expected.string.arg");
			}
			object arg0 = args[0];
			if (arg0 is Wrapper)
			{
				object wrapped = ((Wrapper)arg0).Unwrap();
				if (wrapped is Type)
				{
					return (Type)wrapped;
				}
			}
			string className = Context.ToString(args[0]);
			try
			{
				return Sharpen.Runtime.GetType(className);
			}
			catch (TypeLoadException)
			{
				throw ReportRuntimeError("msg.class.not.found", className);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static void Serialize(Context cx, Scriptable thisObj, object[] args, Function funObj)
		{
			if (args.Length < 2)
			{
				throw Context.ReportRuntimeError("Expected an object to serialize and a filename to write " + "the serialization to");
			}
			object obj = args[0];
			string filename = Context.ToString(args[1]);
			FileOutputStream fos = new FileOutputStream(filename);
			Scriptable scope = ScriptableObject.GetTopLevelScope(thisObj);
			ScriptableOutputStream @out = new ScriptableOutputStream(fos, scope);
			@out.WriteObject(obj);
			@out.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.TypeLoadException"></exception>
		public static object Deserialize(Context cx, Scriptable thisObj, object[] args, Function funObj)
		{
			if (args.Length < 1)
			{
				throw Context.ReportRuntimeError("Expected a filename to read the serialization from");
			}
			string filename = Context.ToString(args[0]);
			FileInputStream fis = new FileInputStream(filename);
			Scriptable scope = ScriptableObject.GetTopLevelScope(thisObj);
			ObjectInputStream @in = new ScriptableInputStream(fis, scope);
			object deserialized = @in.ReadObject();
			@in.Close();
			return Context.ToObject(deserialized, scope);
		}

		public virtual string[] GetPrompts(Context cx)
		{
			if (ScriptableObject.HasProperty(this, "prompts"))
			{
				object promptsJS = ScriptableObject.GetProperty(this, "prompts");
				if (promptsJS is Scriptable)
				{
					Scriptable s = (Scriptable)promptsJS;
					if (ScriptableObject.HasProperty(s, 0) && ScriptableObject.HasProperty(s, 1))
					{
						object elem0 = ScriptableObject.GetProperty(s, 0);
						if (elem0 is Function)
						{
							elem0 = ((Function)elem0).Call(cx, this, s, new object[0]);
						}
						prompts[0] = Context.ToString(elem0);
						object elem1 = ScriptableObject.GetProperty(s, 1);
						if (elem1 is Function)
						{
							elem1 = ((Function)elem1).Call(cx, this, s, new object[0]);
						}
						prompts[1] = Context.ToString(elem1);
					}
				}
			}
			return prompts;
		}

		/// <summary>
		/// Example: doctest("js&gt; function f() {\n  &gt;   return 3;\n  &gt; }\njs&gt; f();\n3\n"); returns 2
		/// (since 2 tests were executed).
		/// </summary>
		/// <remarks>
		/// Example: doctest("js&gt; function f() {\n  &gt;   return 3;\n  &gt; }\njs&gt; f();\n3\n"); returns 2
		/// (since 2 tests were executed).
		/// </remarks>
		public static object Doctest(Context cx, Scriptable thisObj, object[] args, Function funObj)
		{
			if (args.Length == 0)
			{
				return false;
			}
			string session = Context.ToString(args[0]);
			Rhino.Tools.Shell.Global global = GetInstance(funObj);
			return global.RunDoctest(cx, global, session, null, 0);
		}

		public virtual int RunDoctest(Context cx, Scriptable scope, string session, string sourceName, int lineNumber)
		{
			doctestCanonicalizations = new Dictionary<string, string>();
			string[] lines = session.Split("[\n\r]+");
			string prompt0 = this.prompts[0].Trim();
			string prompt1 = this.prompts[1].Trim();
			int testCount = 0;
			int i = 0;
			while (i < lines.Length && !lines[i].Trim().StartsWith(prompt0))
			{
				i++;
			}
			// skip lines that don't look like shell sessions
			while (i < lines.Length)
			{
				string inputString = Sharpen.Runtime.Substring(lines[i].Trim(), prompt0.Length);
				inputString += "\n";
				i++;
				while (i < lines.Length && lines[i].Trim().StartsWith(prompt1))
				{
					inputString += Sharpen.Runtime.Substring(lines[i].Trim(), prompt1.Length);
					inputString += "\n";
					i++;
				}
				string expectedString = string.Empty;
				while (i < lines.Length && !lines[i].Trim().StartsWith(prompt0))
				{
					expectedString += lines[i] + "\n";
					i++;
				}
				TextWriter savedOut = this.GetOut();
				TextWriter savedErr = this.GetErr();
				MemoryStream @out = new MemoryStream();
				MemoryStream err = new MemoryStream();
				this.SetOut(new TextWriter(@out));
				this.SetErr(new TextWriter(err));
				string resultString = string.Empty;
				ErrorReporter savedErrorReporter = cx.GetErrorReporter();
				cx.SetErrorReporter(new ToolErrorReporter(false, this.GetErr()));
				try
				{
					testCount++;
					object result = cx.EvaluateString(scope, inputString, "doctest input", 1, null);
					if (result != Context.GetUndefinedValue() && !(result is Function && inputString.Trim().StartsWith("function")))
					{
						resultString = Context.ToString(result);
					}
				}
				catch (RhinoException e)
				{
					ToolErrorReporter.ReportException(cx.GetErrorReporter(), e);
				}
				finally
				{
					this.SetOut(savedOut);
					this.SetErr(savedErr);
					cx.SetErrorReporter(savedErrorReporter);
					resultString += err.ToString() + @out.ToString();
				}
				if (!DoctestOutputMatches(expectedString, resultString))
				{
					string message = "doctest failure running:\n" + inputString + "expected: " + expectedString + "actual: " + resultString + "\n";
					if (sourceName != null)
					{
						throw Context.ReportRuntimeError(message, sourceName, lineNumber + i - 1, null, 0);
					}
					else
					{
						throw Context.ReportRuntimeError(message);
					}
				}
			}
			return testCount;
		}

		/// <summary>
		/// Compare actual result of doctest to expected, modulo some
		/// acceptable differences.
		/// </summary>
		/// <remarks>
		/// Compare actual result of doctest to expected, modulo some
		/// acceptable differences. Currently just trims the strings
		/// before comparing, but should ignore differences in line numbers
		/// for error messages for example.
		/// </remarks>
		/// <param name="expected">the expected string</param>
		/// <param name="actual">the actual string</param>
		/// <returns>
		/// true iff actual matches expected modulo some acceptable
		/// differences
		/// </returns>
		private bool DoctestOutputMatches(string expected, string actual)
		{
			expected = expected.Trim();
			actual = actual.Trim().Replace("\r\n", "\n");
			if (expected.Equals(actual))
			{
				return true;
			}
			foreach (KeyValuePair<string, string> entry in doctestCanonicalizations.EntrySet())
			{
				expected = expected.Replace(entry.Key, entry.Value);
			}
			if (expected.Equals(actual))
			{
				return true;
			}
			// java.lang.Object.toString() prints out a unique hex number associated
			// with each object. This number changes from run to run, so we want to
			// ignore differences between these numbers in the output. We search for a
			// regexp that matches the hex number preceded by '@', then enter mappings into
			// "doctestCanonicalizations" so that we ensure that the mappings are
			// consistent within a session.
			Sharpen.Pattern p = Sharpen.Pattern.Compile("@[0-9a-fA-F]+");
			Matcher expectedMatcher = p.Matcher(expected);
			Matcher actualMatcher = p.Matcher(actual);
			for (; ; )
			{
				if (!expectedMatcher.Find())
				{
					return false;
				}
				if (!actualMatcher.Find())
				{
					return false;
				}
				if (actualMatcher.Start() != expectedMatcher.Start())
				{
					return false;
				}
				int start = expectedMatcher.Start();
				if (!Sharpen.Runtime.Substring(expected, 0, start).Equals(Sharpen.Runtime.Substring(actual, 0, start)))
				{
					return false;
				}
				string expectedGroup = expectedMatcher.Group();
				string actualGroup = actualMatcher.Group();
				string mapping = doctestCanonicalizations.Get(expectedGroup);
				if (mapping == null)
				{
					doctestCanonicalizations.Put(expectedGroup, actualGroup);
					expected = expected.Replace(expectedGroup, actualGroup);
				}
				else
				{
					if (!actualGroup.Equals(mapping))
					{
						return false;
					}
				}
				// wrong object!
				if (expected.Equals(actual))
				{
					return true;
				}
			}
		}

		/// <summary>
		/// The spawn function runs a given function or script in a different
		/// thread.
		/// </summary>
		/// <remarks>
		/// The spawn function runs a given function or script in a different
		/// thread.
		/// js&gt; function g() { a = 7; }
		/// js&gt; a = 3;
		/// 3
		/// js&gt; spawn(g)
		/// Thread[Thread-1,5,main]
		/// js&gt; a
		/// 3
		/// </remarks>
		public static object Spawn(Context cx, Scriptable thisObj, object[] args, Function funObj)
		{
			Scriptable scope = funObj.GetParentScope();
			Runner runner;
			if (args.Length != 0 && args[0] is Function)
			{
				object[] newArgs = null;
				if (args.Length > 1 && args[1] is Scriptable)
				{
					newArgs = cx.GetElements((Scriptable)args[1]);
				}
				if (newArgs == null)
				{
					newArgs = ScriptRuntime.emptyArgs;
				}
				runner = new Runner(scope, (Function)args[0], newArgs);
			}
			else
			{
				if (args.Length != 0 && args[0] is Script)
				{
					runner = new Runner(scope, (Script)args[0]);
				}
				else
				{
					throw ReportRuntimeError("msg.spawn.args");
				}
			}
			runner.factory = cx.GetFactory();
			Sharpen.Thread thread = new Sharpen.Thread(runner);
			thread.Start();
			return thread;
		}

		/// <summary>
		/// The sync function creates a synchronized function (in the sense
		/// of a Java synchronized method) from an existing function.
		/// </summary>
		/// <remarks>
		/// The sync function creates a synchronized function (in the sense
		/// of a Java synchronized method) from an existing function. The
		/// new function synchronizes on the the second argument if it is
		/// defined, or otherwise the <code>this</code> object of
		/// its invocation.
		/// js&gt; var o = { f : sync(function(x) {
		/// print("entry");
		/// Packages.java.lang.Thread.sleep(x*1000);
		/// print("exit");
		/// })};
		/// js&gt; spawn(function() {o.f(5);});
		/// Thread[Thread-0,5,main]
		/// entry
		/// js&gt; spawn(function() {o.f(5);});
		/// Thread[Thread-1,5,main]
		/// js&gt;
		/// exit
		/// entry
		/// exit
		/// </remarks>
		public static object Sync(Context cx, Scriptable thisObj, object[] args, Function funObj)
		{
			if (args.Length >= 1 && args.Length <= 2 && args[0] is Function)
			{
				object syncObject = null;
				if (args.Length == 2 && args[1] != Undefined.instance)
				{
					syncObject = args[1];
				}
				return new Synchronizer((Function)args[0], syncObject);
			}
			else
			{
				throw ReportRuntimeError("msg.sync.args");
			}
		}

		/// <summary>
		/// Execute the specified command with the given argument and options
		/// as a separate process and return the exit status of the process.
		/// </summary>
		/// <remarks>
		/// Execute the specified command with the given argument and options
		/// as a separate process and return the exit status of the process.
		/// <p>
		/// Usage:
		/// <pre>
		/// runCommand(command)
		/// runCommand(command, arg1, ..., argN)
		/// runCommand(command, arg1, ..., argN, options)
		/// </pre>
		/// All except the last arguments to runCommand are converted to strings
		/// and denote command name and its arguments. If the last argument is a
		/// JavaScript object, it is an option object. Otherwise it is converted to
		/// string denoting the last argument and options objects assumed to be
		/// empty.
		/// The following properties of the option object are processed:
		/// <ul>
		/// <li><tt>args</tt> - provides an array of additional command arguments
		/// <li><tt>env</tt> - explicit environment object. All its enumerable
		/// properties define the corresponding environment variable names.
		/// <li><tt>input</tt> - the process input. If it is not
		/// java.io.InputStream, it is converted to string and sent to the process
		/// as its input. If not specified, no input is provided to the process.
		/// <li><tt>output</tt> - the process output instead of
		/// java.lang.System.out. If it is not instance of java.io.OutputStream,
		/// the process output is read, converted to a string, appended to the
		/// output property value converted to string and put as the new value of
		/// the output property.
		/// <li><tt>err</tt> - the process error output instead of
		/// java.lang.System.err. If it is not instance of java.io.OutputStream,
		/// the process error output is read, converted to a string, appended to
		/// the err property value converted to string and put as the new
		/// value of the err property.
		/// </ul>
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public static object RunCommand(Context cx, Scriptable thisObj, object[] args, Function funObj)
		{
			int L = args.Length;
			if (L == 0 || (L == 1 && args[0] is Scriptable))
			{
				throw ReportRuntimeError("msg.runCommand.bad.args");
			}
			Stream @in = null;
			Stream @out = null;
			Stream err = null;
			MemoryStream outBytes = null;
			MemoryStream errBytes = null;
			object outObj = null;
			object errObj = null;
			string[] environment = null;
			Scriptable @params = null;
			object[] addArgs = null;
			if (args[L - 1] is Scriptable)
			{
				@params = (Scriptable)args[L - 1];
				--L;
				object envObj = ScriptableObject.GetProperty(@params, "env");
				if (envObj != ScriptableConstants.NOT_FOUND)
				{
					if (envObj == null)
					{
						environment = new string[0];
					}
					else
					{
						if (!(envObj is Scriptable))
						{
							throw ReportRuntimeError("msg.runCommand.bad.env");
						}
						Scriptable envHash = (Scriptable)envObj;
						object[] ids = ScriptableObject.GetPropertyIds(envHash);
						environment = new string[ids.Length];
						for (int i = 0; i != ids.Length; ++i)
						{
							object keyObj = ids[i];
							object val;
							string key;
							if (keyObj is string)
							{
								key = (string)keyObj;
								val = ScriptableObject.GetProperty(envHash, key);
							}
							else
							{
								int ikey = System.Convert.ToInt32(keyObj);
							    key = ikey.ToString();
							    val = ScriptableObject.GetProperty(envHash, ikey);
							}
							if (val == ScriptableObject.NOT_FOUND)
							{
								val = Undefined.instance;
							}
							environment[i] = key + '=' + ScriptRuntime.ToString(val);
						}
					}
				}
				object inObj = ScriptableObject.GetProperty(@params, "input");
				if (inObj != ScriptableConstants.NOT_FOUND)
				{
					@in = ToInputStream(inObj);
				}
				outObj = ScriptableObject.GetProperty(@params, "output");
				if (outObj != ScriptableConstants.NOT_FOUND)
				{
					@out = ToOutputStream(outObj);
					if (@out == null)
					{
						outBytes = new MemoryStream();
						@out = outBytes;
					}
				}
				errObj = ScriptableObject.GetProperty(@params, "err");
				if (errObj != ScriptableConstants.NOT_FOUND)
				{
					err = ToOutputStream(errObj);
					if (err == null)
					{
						errBytes = new MemoryStream();
						err = errBytes;
					}
				}
				object addArgsObj = ScriptableObject.GetProperty(@params, "args");
				if (addArgsObj != ScriptableConstants.NOT_FOUND)
				{
					Scriptable s = Context.ToObject(addArgsObj, GetTopLevelScope(thisObj));
					addArgs = cx.GetElements(s);
				}
			}
			Rhino.Tools.Shell.Global global = GetInstance(funObj);
			if (@out == null)
			{
				@out = (global != null) ? global.GetOut() : System.Console.Out;
			}
			if (err == null)
			{
				err = (global != null) ? global.GetErr() : System.Console.Error;
			}
			// If no explicit input stream, do not send any input to process,
			// in particular, do not use System.in to avoid deadlocks
			// when waiting for user input to send to process which is already
			// terminated as it is not always possible to interrupt read method.
			string[] cmd = new string[(addArgs == null) ? L : L + addArgs.Length];
			for (int i_1 = 0; i_1 != L; ++i_1)
			{
				cmd[i_1] = ScriptRuntime.ToString(args[i_1]);
			}
			if (addArgs != null)
			{
				for (int i = 0; i_1 != addArgs.Length; ++i_1)
				{
					cmd[L + i_1] = ScriptRuntime.ToString(addArgs[i_1]);
				}
			}
			int exitCode = RunProcess(cmd, environment, @in, @out, err);
			if (outBytes != null)
			{
				string s = ScriptRuntime.ToString(outObj) + outBytes.ToString();
				ScriptableObject.PutProperty(@params, "output", s);
			}
			if (errBytes != null)
			{
				string s = ScriptRuntime.ToString(errObj) + errBytes.ToString();
				ScriptableObject.PutProperty(@params, "err", s);
			}
			return exitCode;
		}

		/// <summary>The seal function seals all supplied arguments.</summary>
		/// <remarks>The seal function seals all supplied arguments.</remarks>
		public static void Seal(Context cx, Scriptable thisObj, object[] args, Function funObj)
		{
			for (int i = 0; i != args.Length; ++i)
			{
				object arg = args[i];
				if (!(arg is ScriptableObject) || arg == Undefined.instance)
				{
					if (!(arg is Scriptable) || arg == Undefined.instance)
					{
						throw ReportRuntimeError("msg.shell.seal.not.object");
					}
					else
					{
						throw ReportRuntimeError("msg.shell.seal.not.scriptable");
					}
				}
			}
			for (int i_1 = 0; i_1 != args.Length; ++i_1)
			{
				object arg = args[i_1];
				((ScriptableObject)arg).SealObject();
			}
		}

		/// <summary>
		/// The readFile reads the given file content and convert it to a string
		/// using the specified character coding or default character coding if
		/// explicit coding argument is not given.
		/// </summary>
		/// <remarks>
		/// The readFile reads the given file content and convert it to a string
		/// using the specified character coding or default character coding if
		/// explicit coding argument is not given.
		/// <p>
		/// Usage:
		/// <pre>
		/// readFile(filePath)
		/// readFile(filePath, charCoding)
		/// </pre>
		/// The first form converts file's context to string using the default
		/// character coding.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public static object ReadFile(Context cx, Scriptable thisObj, object[] args, Function funObj)
		{
			if (args.Length == 0)
			{
				throw ReportRuntimeError("msg.shell.readFile.bad.args");
			}
			string path = ScriptRuntime.ToString(args[0]);
			string charCoding = null;
			if (args.Length >= 2)
			{
				charCoding = ScriptRuntime.ToString(args[1]);
			}
			return ReadUrl(path, charCoding, true);
		}

		/// <summary>
		/// The readUrl opens connection to the given URL, read all its data
		/// and converts them to a string
		/// using the specified character coding or default character coding if
		/// explicit coding argument is not given.
		/// </summary>
		/// <remarks>
		/// The readUrl opens connection to the given URL, read all its data
		/// and converts them to a string
		/// using the specified character coding or default character coding if
		/// explicit coding argument is not given.
		/// <p>
		/// Usage:
		/// <pre>
		/// readUrl(url)
		/// readUrl(url, charCoding)
		/// </pre>
		/// The first form converts file's context to string using the default
		/// charCoding.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public static object ReadUrl(Context cx, Scriptable thisObj, object[] args, Function funObj)
		{
			if (args.Length == 0)
			{
				throw ReportRuntimeError("msg.shell.readUrl.bad.args");
			}
			string url = ScriptRuntime.ToString(args[0]);
			string charCoding = null;
			if (args.Length >= 2)
			{
				charCoding = ScriptRuntime.ToString(args[1]);
			}
			return ReadUrl(url, charCoding, false);
		}

		/// <summary>Convert the argument to int32 number.</summary>
		/// <remarks>Convert the argument to int32 number.</remarks>
		public static object Toint32(Context cx, Scriptable thisObj, object[] args, Function funObj)
		{
			object arg = (args.Length != 0 ? args[0] : Undefined.instance);
			if (arg is int)
			{
				return arg;
			}
			return ScriptRuntime.WrapInt(ScriptRuntime.ToInt32(arg));
		}

		private bool LoadJLine(Encoding cs)
		{
			if (!attemptedJLineLoad)
			{
				// Check if we can use JLine for better command line handling
				attemptedJLineLoad = true;
				console = ShellConsole.GetConsole(this, cs);
			}
			return console != null;
		}

		public virtual ShellConsole GetConsole(Encoding cs)
		{
			if (!LoadJLine(cs))
			{
				console = ShellConsole.GetConsole(GetIn(), GetErr(), cs);
			}
			return console;
		}

		public virtual Stream GetIn()
		{
			if (inStream == null && !attemptedJLineLoad)
			{
				if (LoadJLine(Encoding.Default))
				{
					inStream = console.GetIn();
				}
			}
			return inStream == null ? Runtime.@in : inStream;
		}

		public virtual void SetIn(Stream @in)
		{
			inStream = @in;
		}

		public virtual TextWriter GetOut()
		{
			return outStream == null ? System.Console.Out : outStream;
		}

		public virtual void SetOut(TextWriter @out)
		{
			outStream = @out;
		}

		public virtual TextWriter GetErr()
		{
			return errStream == null ? System.Console.Error : errStream;
		}

		public virtual void SetErr(TextWriter err)
		{
			errStream = err;
		}

		public virtual void SetSealedStdLib(bool value)
		{
			sealedStdLib = value;
		}

		private static Rhino.Tools.Shell.Global GetInstance(Function function)
		{
			Scriptable scope = function.GetParentScope();
			if (!(scope is Rhino.Tools.Shell.Global))
			{
				throw ReportRuntimeError("msg.bad.shell.function.scope", scope.ToString());
			}
			return (Rhino.Tools.Shell.Global)scope;
		}

		/// <summary>Runs the given process using Runtime.exec().</summary>
		/// <remarks>
		/// Runs the given process using Runtime.exec().
		/// If any of in, out, err is null, the corresponding process stream will
		/// be closed immediately, otherwise it will be closed as soon as
		/// all data will be read from/written to process
		/// </remarks>
		/// <returns>Exit value of process.</returns>
		/// <exception cref="System.IO.IOException">If there was an error executing the process.</exception>
		private static int RunProcess(string[] cmd, string[] environment, Stream @in, Stream @out, Stream err)
		{
			SystemProcess p;
			if (environment == null)
			{
				p = Runtime.GetRuntime().Exec(cmd);
			}
			else
			{
				p = Runtime.GetRuntime().Exec(cmd, environment);
			}
			try
			{
				PipeThread inThread = null;
				if (@in != null)
				{
					inThread = new PipeThread(false, @in, p.GetOutputStream());
					inThread.Start();
				}
				else
				{
					p.GetOutputStream().Close();
				}
				PipeThread outThread = null;
				if (@out != null)
				{
					outThread = new PipeThread(true, p.GetInputStream(), @out);
					outThread.Start();
				}
				else
				{
					p.GetInputStream().Close();
				}
				PipeThread errThread = null;
				if (err != null)
				{
					errThread = new PipeThread(true, p.GetErrorStream(), err);
					errThread.Start();
				}
				else
				{
					p.GetErrorStream().Close();
				}
				// wait for process completion
				for (; ; )
				{
					try
					{
						p.WaitFor();
						if (outThread != null)
						{
							outThread.Join();
						}
						if (inThread != null)
						{
							inThread.Join();
						}
						if (errThread != null)
						{
							errThread.Join();
						}
						break;
					}
					catch (Exception)
					{
					}
				}
				return p.ExitValue();
			}
			finally
			{
				p.Destroy();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal static void Pipe(bool fromProcess, Stream from, Stream to)
		{
			try
			{
				int SIZE = 4096;
				byte[] buffer = new byte[SIZE];
				for (; ; )
				{
					int n;
					if (!fromProcess)
					{
						n = from.Read(buffer, 0, SIZE);
					}
					else
					{
						try
						{
							n = from.Read(buffer, 0, SIZE);
						}
						catch (IOException)
						{
							// Ignore exception as it can be cause by closed pipe
							break;
						}
					}
					if (n < 0)
					{
						break;
					}
					if (fromProcess)
					{
						to.Write(buffer, 0, n);
						to.Flush();
					}
					else
					{
						try
						{
							to.Write(buffer, 0, n);
							to.Flush();
						}
						catch (IOException)
						{
							// Ignore exception as it can be cause by closed pipe
							break;
						}
					}
				}
			}
			finally
			{
				try
				{
					if (fromProcess)
					{
						from.Close();
					}
					else
					{
						to.Close();
					}
				}
				catch (IOException)
				{
				}
			}
		}

		// Ignore errors on close. On Windows JVM may throw invalid
		// refrence exception if process terminates too fast.
		/// <exception cref="System.IO.IOException"></exception>
		private static Stream ToInputStream(object value)
		{
			Stream @is = null;
			string s = null;
			if (value is Wrapper)
			{
				object unwrapped = ((Wrapper)value).Unwrap();
				if (unwrapped is Stream)
				{
					@is = (Stream)unwrapped;
				}
				else
				{
					if (unwrapped is byte[])
					{
						@is = new MemoryStream((byte[])unwrapped);
					}
					else
					{
						if (unwrapped is TextReader)
						{
							s = ReadReader((TextReader)unwrapped);
						}
						else
						{
							if (unwrapped is char[])
							{
								s = new string((char[])unwrapped);
							}
						}
					}
				}
			}
			if (@is == null)
			{
				if (s == null)
				{
					s = ScriptRuntime.ToString(value);
				}
				@is = new MemoryStream(Sharpen.Runtime.GetBytesForString(s));
			}
			return @is;
		}

		private static Stream ToOutputStream(object value)
		{
			Stream os = null;
			if (value is Wrapper)
			{
				object unwrapped = ((Wrapper)value).Unwrap();
				if (unwrapped is Stream)
				{
					os = (Stream)unwrapped;
				}
			}
			return os;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static string ReadUrl(string filePath, string charCoding, bool urlIsFile)
		{
			int chunkLength;
			Stream @is = null;
			try
			{
				if (!urlIsFile)
				{
					Uri urlObj = new Uri(filePath);
					URLConnection uc = urlObj.OpenConnection();
					@is = uc.GetInputStream();
					chunkLength = uc.GetContentLength();
					if (chunkLength <= 0)
					{
						chunkLength = 1024;
					}
					if (charCoding == null)
					{
						string type = uc.GetContentType();
						if (type != null)
						{
							charCoding = GetCharCodingFromType(type);
						}
					}
				}
				else
				{
					FilePath f = new FilePath(filePath);
					if (!f.Exists())
					{
						throw new FileNotFoundException("File not found: " + filePath);
					}
					else
					{
						if (!f.CanRead())
						{
							throw new IOException("Cannot read file: " + filePath);
						}
					}
					long length = f.Length();
					chunkLength = (int)length;
					if (chunkLength != length)
					{
						throw new IOException("Too big file size: " + length);
					}
					if (chunkLength == 0)
					{
						return string.Empty;
					}
					@is = new FileInputStream(f);
				}
				TextReader r;
				if (charCoding == null)
				{
					r = new StreamReader(@is);
				}
				else
				{
					r = new StreamReader(@is, charCoding);
				}
				return ReadReader(r, chunkLength);
			}
			finally
			{
				if (@is != null)
				{
					@is.Close();
				}
			}
		}

		private static string GetCharCodingFromType(string type)
		{
			int i = type.IndexOf(';');
			if (i >= 0)
			{
				int end = type.Length;
				++i;
				while (i != end && type[i] <= ' ')
				{
					++i;
				}
				string charset = "charset";
				if (charset.RegionMatches(true, 0, type, i, charset.Length))
				{
					i += charset.Length;
					while (i != end && type[i] <= ' ')
					{
						++i;
					}
					if (i != end && type[i] == '=')
					{
						++i;
						while (i != end && type[i] <= ' ')
						{
							++i;
						}
						if (i != end)
						{
							// i is at the start of non-empty
							// charCoding spec
							while (type[end - 1] <= ' ')
							{
								--end;
							}
							return Sharpen.Runtime.Substring(type, i, end);
						}
					}
				}
			}
			return null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static string ReadReader(TextReader reader)
		{
			return ReadReader(reader, 4096);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static string ReadReader(TextReader reader, int initialBufferSize)
		{
			char[] buffer = new char[initialBufferSize];
			int offset = 0;
			for (; ; )
			{
				int n = reader.Read(buffer, offset, buffer.Length - offset);
				if (n < 0)
				{
					break;
				}
				offset += n;
				if (offset == buffer.Length)
				{
					char[] tmp = new char[buffer.Length * 2];
					System.Array.Copy(buffer, 0, tmp, 0, offset);
					buffer = tmp;
				}
			}
			return new string(buffer, 0, offset);
		}

		internal static Exception ReportRuntimeError(string msgId)
		{
			string message = ToolErrorReporter.GetMessage(msgId);
			return Context.ReportRuntimeError(message);
		}

		internal static Exception ReportRuntimeError(string msgId, string msgArg)
		{
			string message = ToolErrorReporter.GetMessage(msgId, msgArg);
			return Context.ReportRuntimeError(message);
		}
	}

	internal class Runner : Runnable
	{
		internal Runner(Scriptable scope, Function func, object[] args)
		{
			this.scope = scope;
			f = func;
			this.args = args;
		}

		internal Runner(Scriptable scope, Script script)
		{
			this.scope = scope;
			s = script;
		}

		public virtual void Run()
		{
			factory.Call(cx =>
			{
				if (f != null)
				{
					return f.Call(cx, scope, scope, args);
				}
				else
				{
					return s.Exec(cx, scope);
				}
			});
		}

		internal ContextFactory factory;

		private Scriptable scope;

		private Function f;

		private Script s;

		private object[] args;
	}

	internal class PipeThread : Sharpen.Thread
	{
		internal PipeThread(bool fromProcess, Stream from, Stream to)
		{
			SetDaemon(true);
			this.fromProcess = fromProcess;
			this.from = from;
			this.to = to;
		}

		public override void Run()
		{
			try
			{
				Global.Pipe(fromProcess, from, to);
			}
			catch (IOException ex)
			{
				throw Context.ThrowAsScriptRuntimeEx(ex);
			}
		}

		private bool fromProcess;

		private Stream from;

		private Stream to;
	}
}
