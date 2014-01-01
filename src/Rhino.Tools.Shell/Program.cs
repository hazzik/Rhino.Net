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
using System.Security;
using System.Text;
using Rhino;
using Rhino.Commonjs.Module;
using Rhino.Tools;
using Rhino.Tools.Shell;
using Sharpen;

namespace Rhino.Tools.Shell
{
	/// <summary>The shell program.</summary>
	/// <remarks>
	/// The shell program.
	/// Can execute scripts interactively or in batch mode at the command line.
	/// An example of controlling the JavaScript engine.
	/// </remarks>
	/// <author>Norris Boyd</author>
	public class Program
	{
		public static ShellContextFactory shellContextFactory = new ShellContextFactory();

		public static Global global = new Global();

		protected internal static ToolErrorReporter errorReporter;

		protected internal static int exitCode = 0;

		private const int EXITCODE_RUNTIME_ERROR = 3;

		private const int EXITCODE_FILE_NOT_FOUND = 4;

		internal static bool processStdin = true;

		internal static IList<string> fileList = new List<string>();

		internal static IList<string> modulePath;

		internal static string mainModule;

		internal static bool sandboxed = false;

		internal static bool useRequire = false;

		internal static Require require;

		private static SecurityProxy securityImpl;

		private static readonly Main.ScriptCache scriptCache = new Main.ScriptCache(32);

		static Program()
		{
			global.InitQuitAction(new Main.IProxy(Main.IProxy.SYSTEM_EXIT));
		}

		/// <summary>Proxy class to avoid proliferation of anonymous classes.</summary>
		/// <remarks>Proxy class to avoid proliferation of anonymous classes.</remarks>
		private class IProxy : ContextAction, QuitAction
		{
			private const int PROCESS_FILES = 1;

			private const int EVAL_INLINE_SCRIPT = 2;

			private const int SYSTEM_EXIT = 3;

			private int type;

			internal string[] args;

			internal string scriptText;

			internal IProxy(int type)
			{
				this.type = type;
			}

			public virtual object Run(Context cx)
			{
				if (useRequire)
				{
					require = global.InstallRequire(cx, modulePath, sandboxed);
				}
				if (type == PROCESS_FILES)
				{
					ProcessFiles(cx, args);
				}
				else
				{
					if (type == EVAL_INLINE_SCRIPT)
					{
						EvalInlineScript(cx, scriptText);
					}
					else
					{
						throw Kit.CodeBug();
					}
				}
				return null;
			}

			public virtual void Quit(Context cx, int exitCode)
			{
				if (type == SYSTEM_EXIT)
				{
					System.Environment.Exit(exitCode);
					return;
				}
				throw Kit.CodeBug();
			}
		}

		/// <summary>Main entry point.</summary>
		/// <remarks>
		/// Main entry point.
		/// Process arguments as would a normal Java program. Also
		/// create a new Context and associate it with the current thread.
		/// Then set up the execution environment and begin to
		/// execute scripts.
		/// </remarks>
		public static void Main(string[] args)
		{
			try
			{
				if (bool.GetBoolean("rhino.use_java_policy_security"))
				{
					InitJavaPolicySecuritySupport();
				}
			}
			catch (SecurityException ex)
			{
				Sharpen.Runtime.PrintStackTrace(ex, System.Console.Error);
			}
			int result = Exec(args);
			if (result != 0)
			{
				System.Environment.Exit(result);
			}
		}

		/// <summary>Execute the given arguments, but don't System.exit at the end.</summary>
		/// <remarks>Execute the given arguments, but don't System.exit at the end.</remarks>
		public static int Exec(string[] origArgs)
		{
			errorReporter = new ToolErrorReporter(false, global.GetErr());
			shellContextFactory.SetErrorReporter(errorReporter);
			string[] args = ProcessOptions(origArgs);
			if (processStdin)
			{
				fileList.Add(null);
			}
			if (!global.initialized)
			{
				global.Init(shellContextFactory);
			}
			Main.IProxy iproxy = new Main.IProxy(Main.IProxy.PROCESS_FILES);
			iproxy.args = args;
			shellContextFactory.Call(iproxy);
			return exitCode;
		}

		internal static void ProcessFiles(Context cx, string[] args)
		{
			// define "arguments" array in the top-level object:
			// need to allocate new array since newArray requires instances
			// of exactly Object[], not ObjectSubclass[]
			object[] array = new object[args.Length];
			System.Array.Copy(args, 0, array, 0, args.Length);
			Scriptable argsObj = cx.NewArray(global, array);
			global.DefineProperty("arguments", argsObj, ScriptableObject.DONTENUM);
			foreach (string file in fileList)
			{
				try
				{
					ProcessSource(cx, file);
				}
				catch (IOException ioex)
				{
					Context.ReportError(ToolErrorReporter.GetMessage("msg.couldnt.read.source", file, ioex.Message));
					exitCode = EXITCODE_FILE_NOT_FOUND;
				}
				catch (RhinoException rex)
				{
					ToolErrorReporter.ReportException(cx.GetErrorReporter(), rex);
					exitCode = EXITCODE_RUNTIME_ERROR;
				}
				catch (VirtualMachineError ex)
				{
					// Treat StackOverflow and OutOfMemory as runtime errors
					Sharpen.Runtime.PrintStackTrace(ex);
					string msg = ToolErrorReporter.GetMessage("msg.uncaughtJSException", ex.ToString());
					Context.ReportError(msg);
					exitCode = EXITCODE_RUNTIME_ERROR;
				}
			}
		}

		internal static void EvalInlineScript(Context cx, string scriptText)
		{
			try
			{
				Script script = cx.CompileString(scriptText, "<command>", 1, null);
				if (script != null)
				{
					script.Exec(cx, GetShellScope());
				}
			}
			catch (RhinoException rex)
			{
				ToolErrorReporter.ReportException(cx.GetErrorReporter(), rex);
				exitCode = EXITCODE_RUNTIME_ERROR;
			}
			catch (VirtualMachineError ex)
			{
				// Treat StackOverflow and OutOfMemory as runtime errors
				Sharpen.Runtime.PrintStackTrace(ex);
				string msg = ToolErrorReporter.GetMessage("msg.uncaughtJSException", ex.ToString());
				Context.ReportError(msg);
				exitCode = EXITCODE_RUNTIME_ERROR;
			}
		}

		public static Global GetGlobal()
		{
			return global;
		}

		internal static Scriptable GetShellScope()
		{
			return GetScope(null);
		}

		internal static Scriptable GetScope(string path)
		{
			if (useRequire)
			{
				// If CommonJS modules are enabled use a module scope that resolves
				// relative ids relative to the current URL, file or working directory.
				Uri uri;
				if (path == null)
				{
					// use current directory for shell and -e switch
					uri = new FilePath(Runtime.GetProperty("user.dir")).ToURI();
				}
				else
				{
					// find out whether this is a file path or a URL
					if (SourceReader.ToUrl(path) != null)
					{
						try
						{
							uri = new Uri(path);
						}
						catch (URISyntaxException)
						{
							// fall back to file uri
							uri = new FilePath(path).ToURI();
						}
					}
					else
					{
						uri = new FilePath(path).ToURI();
					}
				}
				return new ModuleScope(global, uri, null);
			}
			else
			{
				return global;
			}
		}

		/// <summary>Parse arguments.</summary>
		/// <remarks>Parse arguments.</remarks>
		public static string[] ProcessOptions(string[] args)
		{
			string usageError;
			for (int i = 0; ; ++i)
			{
				if (i == args.Length)
				{
					return new string[0];
				}
				string arg = args[i];
				if (!arg.StartsWith("-"))
				{
					processStdin = false;
					fileList.Add(arg);
					mainModule = arg;
					string[] result = new string[args.Length - i - 1];
					System.Array.Copy(args, i + 1, result, 0, args.Length - i - 1);
					return result;
				}
				if (arg.Equals("-version"))
				{
					if (++i == args.Length)
					{
						usageError = arg;
						goto goodUsage_break;
					}
					int version;
					try
					{
						version = System.Convert.ToInt32(args[i]);
					}
					catch (FormatException)
					{
						usageError = args[i];
						goto goodUsage_break;
					}
					if (!Context.IsValidLanguageVersion(version))
					{
						usageError = args[i];
						goto goodUsage_break;
					}
					shellContextFactory.SetLanguageVersion(version);
					continue;
				}
				if (arg.Equals("-opt") || arg.Equals("-O"))
				{
					if (++i == args.Length)
					{
						usageError = arg;
						goto goodUsage_break;
					}
					int opt;
					try
					{
						opt = System.Convert.ToInt32(args[i]);
					}
					catch (FormatException)
					{
						usageError = args[i];
						goto goodUsage_break;
					}
					if (opt == -2)
					{
						// Compatibility with Cocoon Rhino fork
						opt = -1;
					}
					else
					{
						if (!Context.IsValidOptimizationLevel(opt))
						{
							usageError = args[i];
							goto goodUsage_break;
						}
					}
					shellContextFactory.SetOptimizationLevel(opt);
					continue;
				}
				if (arg.Equals("-encoding"))
				{
					if (++i == args.Length)
					{
						usageError = arg;
						goto goodUsage_break;
					}
					string enc = args[i];
					shellContextFactory.SetCharacterEncoding(enc);
					continue;
				}
				if (arg.Equals("-strict"))
				{
					shellContextFactory.SetStrictMode(true);
					shellContextFactory.SetAllowReservedKeywords(false);
					errorReporter.SetIsReportingWarnings(true);
					continue;
				}
				if (arg.Equals("-fatal-warnings"))
				{
					shellContextFactory.SetWarningAsError(true);
					continue;
				}
				if (arg.Equals("-e"))
				{
					processStdin = false;
					if (++i == args.Length)
					{
						usageError = arg;
						goto goodUsage_break;
					}
					if (!global.initialized)
					{
						global.Init(shellContextFactory);
					}
					Main.IProxy iproxy = new Main.IProxy(Main.IProxy.EVAL_INLINE_SCRIPT);
					iproxy.scriptText = args[i];
					shellContextFactory.Call(iproxy);
					continue;
				}
				if (arg.Equals("-require"))
				{
					useRequire = true;
					continue;
				}
				if (arg.Equals("-sandbox"))
				{
					sandboxed = true;
					useRequire = true;
					continue;
				}
				if (arg.Equals("-modules"))
				{
					if (++i == args.Length)
					{
						usageError = arg;
						goto goodUsage_break;
					}
					if (modulePath == null)
					{
						modulePath = new List<string>();
					}
					modulePath.Add(args[i]);
					useRequire = true;
					continue;
				}
				if (arg.Equals("-w"))
				{
					errorReporter.SetIsReportingWarnings(true);
					continue;
				}
				if (arg.Equals("-f"))
				{
					processStdin = false;
					if (++i == args.Length)
					{
						usageError = arg;
						goto goodUsage_break;
					}
					if (args[i].Equals("-"))
					{
						fileList.Add(null);
					}
					else
					{
						fileList.Add(args[i]);
						mainModule = args[i];
					}
					continue;
				}
				if (arg.Equals("-sealedlib"))
				{
					global.SetSealedStdLib(true);
					continue;
				}
				if (arg.Equals("-debug"))
				{
					shellContextFactory.SetGeneratingDebug(true);
					continue;
				}
				if (arg.Equals("-?") || arg.Equals("-help"))
				{
					// print usage message
					global.GetOut().WriteLine(ToolErrorReporter.GetMessage("msg.shell.usage", typeof(Program).FullName));
					System.Environment.Exit(1);
				}
				usageError = arg;
				goto goodUsage_break;
goodUsage_continue: ;
			}
goodUsage_break: ;
			// print error and usage message
			global.GetOut().WriteLine(ToolErrorReporter.GetMessage("msg.shell.invalid", usageError));
			global.GetOut().WriteLine(ToolErrorReporter.GetMessage("msg.shell.usage", typeof(Program).FullName));
			System.Environment.Exit(1);
			return null;
		}

		private static void InitJavaPolicySecuritySupport()
		{
			Exception exObj;
			try
			{
				Type cl = Sharpen.Runtime.GetType("org.mozilla.javascript.tools.shell.JavaPolicySecurity");
				securityImpl = (SecurityProxy)System.Activator.CreateInstance(cl);
				SecurityController.InitGlobal(securityImpl);
				return;
			}
			catch (TypeLoadException ex)
			{
				exObj = ex;
			}
			catch (MemberAccessException ex)
			{
				exObj = ex;
			}
			catch (InstantiationException ex)
			{
				exObj = ex;
			}
			catch (LinkageError ex)
			{
				exObj = ex;
			}
			throw Kit.InitCause(new InvalidOperationException("Can not load security support: " + exObj), exObj);
		}

		/// <summary>Evaluate JavaScript source.</summary>
		/// <remarks>Evaluate JavaScript source.</remarks>
		/// <param name="cx">the current context</param>
		/// <param name="filename">
		/// the name of the file to compile, or null
		/// for interactive mode.
		/// </param>
		/// <exception cref="System.IO.IOException">if the source could not be read</exception>
		/// <exception cref="Rhino.RhinoException">thrown during evaluation of source</exception>
		public static void ProcessSource(Context cx, string filename)
		{
			if (filename == null || filename.Equals("-"))
			{
				Scriptable scope = GetShellScope();
				Encoding cs;
				string charEnc = shellContextFactory.GetCharacterEncoding();
				if (charEnc != null)
				{
					cs = Sharpen.Extensions.GetEncoding(charEnc);
				}
				else
				{
					cs = Encoding.Default;
				}
				ShellConsole console = global.GetConsole(cs);
				if (filename == null)
				{
					// print implementation version
					console.Println(cx.GetImplementationVersion());
				}
				int lineno = 1;
				bool hitEOF = false;
				while (!hitEOF)
				{
					string[] prompts = global.GetPrompts(cx);
					string prompt = null;
					if (filename == null)
					{
						prompt = prompts[0];
					}
					console.Flush();
					string source = string.Empty;
					// Collect lines of source to compile.
					while (true)
					{
						string newline;
						try
						{
							newline = console.ReadLine(prompt);
						}
						catch (IOException ioe)
						{
							console.Println(ioe.ToString());
							break;
						}
						if (newline == null)
						{
							hitEOF = true;
							break;
						}
						source = source + newline + "\n";
						lineno++;
						if (cx.StringIsCompilableUnit(source))
						{
							break;
						}
						prompt = prompts[1];
					}
					try
					{
						Script script = cx.CompileString(source, "<stdin>", lineno, null);
						if (script != null)
						{
							object result = script.Exec(cx, scope);
							// Avoid printing out undefined or function definitions.
							if (result != Context.GetUndefinedValue() && !(result is Function && source.Trim().StartsWith("function")))
							{
								try
								{
									console.Println(Context.ToString(result));
								}
								catch (RhinoException rex)
								{
									ToolErrorReporter.ReportException(cx.GetErrorReporter(), rex);
								}
							}
							NativeArray h = global.history;
							h.Put((int)h.GetLength(), h, source);
						}
					}
					catch (RhinoException rex)
					{
						ToolErrorReporter.ReportException(cx.GetErrorReporter(), rex);
						exitCode = EXITCODE_RUNTIME_ERROR;
					}
					catch (VirtualMachineError ex)
					{
						// Treat StackOverflow and OutOfMemory as runtime errors
						Sharpen.Runtime.PrintStackTrace(ex);
						string msg = ToolErrorReporter.GetMessage("msg.uncaughtJSException", ex.ToString());
						Context.ReportError(msg);
						exitCode = EXITCODE_RUNTIME_ERROR;
					}
				}
				console.Println();
				console.Flush();
			}
			else
			{
				if (useRequire && filename.Equals(mainModule))
				{
					require.RequireMain(cx, filename);
				}
				else
				{
					ProcessFile(cx, GetScope(filename), filename);
				}
			}
		}

		public static void ProcessFileNoThrow(Context cx, Scriptable scope, string filename)
		{
			try
			{
				ProcessFile(cx, scope, filename);
			}
			catch (IOException ioex)
			{
				Context.ReportError(ToolErrorReporter.GetMessage("msg.couldnt.read.source", filename, ioex.Message));
				exitCode = EXITCODE_FILE_NOT_FOUND;
			}
			catch (RhinoException rex)
			{
				ToolErrorReporter.ReportException(cx.GetErrorReporter(), rex);
				exitCode = EXITCODE_RUNTIME_ERROR;
			}
			catch (VirtualMachineError ex)
			{
				// Treat StackOverflow and OutOfMemory as runtime errors
				Sharpen.Runtime.PrintStackTrace(ex);
				string msg = ToolErrorReporter.GetMessage("msg.uncaughtJSException", ex.ToString());
				Context.ReportError(msg);
				exitCode = EXITCODE_RUNTIME_ERROR;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static void ProcessFile(Context cx, Scriptable scope, string filename)
		{
			if (securityImpl == null)
			{
				ProcessFileSecure(cx, scope, filename, null);
			}
			else
			{
				securityImpl.CallProcessFileSecure(cx, scope, filename);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal static void ProcessFileSecure(Context cx, Scriptable scope, string path, object securityDomain)
		{
			bool isClass = path.EndsWith(".class");
			object source = ReadFileOrUrl(path, !isClass);
			byte[] digest = GetDigest(source);
			string key = path + "_" + cx.GetOptimizationLevel();
			Main.ScriptReference @ref = scriptCache.Get(key, digest);
			Script script = @ref != null ? @ref.Get() : null;
			if (script == null)
			{
				if (isClass)
				{
					script = LoadCompiledScript(cx, path, (byte[])source, securityDomain);
				}
				else
				{
					string strSrc = (string)source;
					// Support the executable script #! syntax:  If
					// the first line begins with a '#', treat the whole
					// line as a comment.
					if (strSrc.Length > 0 && strSrc[0] == '#')
					{
						for (int i = 1; i != strSrc.Length; ++i)
						{
							int c = strSrc[i];
							if (c == '\n' || c == '\r')
							{
								strSrc = Sharpen.Runtime.Substring(strSrc, i);
								break;
							}
						}
					}
					script = cx.CompileString(strSrc, path, 1, securityDomain);
				}
				scriptCache.Put(key, digest, script);
			}
			if (script != null)
			{
				script.Exec(cx, scope);
			}
		}

		private static byte[] GetDigest(object source)
		{
			byte[] bytes;
			byte[] digest = null;
			if (source != null)
			{
				if (source is string)
				{
					try
					{
						bytes = Sharpen.Runtime.GetBytesForString(((string)source), "UTF-8");
					}
					catch (UnsupportedEncodingException)
					{
						bytes = Sharpen.Runtime.GetBytesForString(((string)source));
					}
				}
				else
				{
					bytes = (byte[])source;
				}
				try
				{
					MessageDigest md = MessageDigest.GetInstance("MD5");
					digest = md.Digest(bytes);
				}
				catch (NoSuchAlgorithmException nsa)
				{
					// Should not happen
					throw new Exception(nsa);
				}
			}
			return digest;
		}

		/// <exception cref="System.IO.FileNotFoundException"></exception>
		private static Script LoadCompiledScript(Context cx, string path, byte[] data, object securityDomain)
		{
			if (data == null)
			{
				throw new FileNotFoundException(path);
			}
			// XXX: For now extract class name of compiled Script from path
			// instead of parsing class bytes
			int nameStart = path.LastIndexOf('/');
			if (nameStart < 0)
			{
				nameStart = 0;
			}
			else
			{
				++nameStart;
			}
			int nameEnd = path.LastIndexOf('.');
			if (nameEnd < nameStart)
			{
				// '.' does not exist in path (nameEnd < 0)
				// or it comes before nameStart
				nameEnd = path.Length;
			}
			string name = Sharpen.Runtime.Substring(path, nameStart, nameEnd);
			try
			{
				GeneratedClassLoader loader = SecurityController.CreateLoader(cx.GetApplicationClassLoader(), securityDomain);
				Type clazz = loader.DefineClass(name, data);
				loader.LinkClass(clazz);
				if (!typeof(Script).IsAssignableFrom(clazz))
				{
					throw Context.ReportRuntimeError("msg.must.implement.Script");
				}
				return (Script)System.Activator.CreateInstance(clazz);
			}
			catch (MemberAccessException iaex)
			{
				Context.ReportError(iaex.ToString());
				throw new Exception(iaex);
			}
			catch (InstantiationException inex)
			{
				Context.ReportError(inex.ToString());
				throw new Exception(inex);
			}
		}

		public static InputStream GetIn()
		{
			return GetGlobal().GetIn();
		}

		public static void SetIn(InputStream @in)
		{
			GetGlobal().SetIn(@in);
		}

		public static TextWriter GetOut()
		{
			return GetGlobal().GetOut();
		}

		public static void SetOut(TextWriter @out)
		{
			GetGlobal().SetOut(@out);
		}

		public static TextWriter GetErr()
		{
			return GetGlobal().GetErr();
		}

		public static void SetErr(TextWriter err)
		{
			GetGlobal().SetErr(err);
		}

		/// <summary>Read file or url specified by <tt>path</tt>.</summary>
		/// <remarks>Read file or url specified by <tt>path</tt>.</remarks>
		/// <returns>
		/// file or url content as <tt>byte[]</tt> or as <tt>String</tt> if
		/// <tt>convertToString</tt> is true.
		/// </returns>
		/// <exception cref="System.IO.IOException"></exception>
		private static object ReadFileOrUrl(string path, bool convertToString)
		{
			return SourceReader.ReadFileOrUrl(path, convertToString, shellContextFactory.GetCharacterEncoding());
		}

		internal class ScriptReference : SoftReference<Script>
		{
			internal string path;

			internal byte[] digest;

			internal ScriptReference(string path, byte[] digest, Script script, ReferenceQueue<Script> queue) : base(script, queue)
			{
				this.path = path;
				this.digest = digest;
			}
		}

		[System.Serializable]
		internal class ScriptCache : LinkedHashMap<string, Main.ScriptReference>
		{
			internal ReferenceQueue<Script> queue;

			internal int capacity;

			internal ScriptCache(int capacity) : base(capacity + 1, 2f, true)
			{
				this.capacity = capacity;
				queue = new ReferenceQueue<Script>();
			}

			protected override bool RemoveEldestEntry(KeyValuePair<string, Main.ScriptReference> eldest)
			{
				return Count > capacity;
			}

			internal virtual Main.ScriptReference Get(string path, byte[] digest)
			{
				Main.ScriptReference @ref;
				while ((@ref = (Main.ScriptReference)queue.Poll()) != null)
				{
					Sharpen.Collections.Remove(this, @ref.path);
				}
				@ref = Get(path);
				if (@ref != null && !Arrays.Equals(digest, @ref.digest))
				{
					Sharpen.Collections.Remove(this, @ref.path);
					@ref = null;
				}
				return @ref;
			}

			internal virtual void Put(string path, byte[] digest, Script script)
			{
				Put(path, new Main.ScriptReference(path, digest, script, queue));
			}
		}
	}
}
