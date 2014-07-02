/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using Rhino.Debug;
using Rhino.Utils;
using Sharpen;

namespace Rhino.Tools.Debugger
{
	/// <summary>Dim or Debugger Implementation for Rhino.</summary>
	/// <remarks>Dim or Debugger Implementation for Rhino.</remarks>
	public sealed class Dim : IDisposable
	{
		public const int STEP_OVER = 0;

		public const int STEP_INTO = 1;

		public const int STEP_OUT = 2;

		public const int GO = 3;

		public const int BREAK = 4;

		public const int EXIT = 5;

		/// <summary>Interface to the debugger GUI.</summary>
		/// <remarks>Interface to the debugger GUI.</remarks>
		private GuiCallback callback;

		/// <summary>Whether the debugger should break.</summary>
		/// <remarks>Whether the debugger should break.</remarks>
		internal bool breakFlag;

		/// <summary>
		/// The ScopeProvider object that provides the scope in which to
		/// evaluate script.
		/// </summary>
		/// <remarks>
		/// The ScopeProvider object that provides the scope in which to
		/// evaluate script.
		/// </remarks>
		private ScopeProvider scopeProvider;

		/// <summary>The SourceProvider object that provides the source of evaluated scripts.</summary>
		/// <remarks>The SourceProvider object that provides the source of evaluated scripts.</remarks>
		private SourceProvider sourceProvider;

		/// <summary>The index of the current stack frame.</summary>
		/// <remarks>The index of the current stack frame.</remarks>
		private int frameIndex = -1;

		/// <summary>Information about the current stack at the point of interruption.</summary>
		/// <remarks>Information about the current stack at the point of interruption.</remarks>
		private volatile ContextData interruptedContextData;

		/// <summary>The ContextFactory to listen to for debugging information.</summary>
		/// <remarks>The ContextFactory to listen to for debugging information.</remarks>
		private ContextFactory contextFactory;

		/// <summary>
		/// Synchronization object used to allow script evaluations to
		/// happen when a thread is resumed.
		/// </summary>
		/// <remarks>
		/// Synchronization object used to allow script evaluations to
		/// happen when a thread is resumed.
		/// </remarks>
		private readonly object monitor = new object();

		/// <summary>
		/// Synchronization object used to wait for valid
		/// <see cref="interruptedContextData">interruptedContextData</see>
		/// .
		/// </summary>
		private readonly object eventThreadMonitor = new object();

		/// <summary>The action to perform to end the interruption loop.</summary>
		/// <remarks>The action to perform to end the interruption loop.</remarks>
		private volatile int returnValue = -1;

		/// <summary>Whether the debugger is inside the interruption loop.</summary>
		/// <remarks>Whether the debugger is inside the interruption loop.</remarks>
		private bool insideInterruptLoop;

		/// <summary>
		/// The requested script string to be evaluated when the thread
		/// has been resumed.
		/// </summary>
		/// <remarks>
		/// The requested script string to be evaluated when the thread
		/// has been resumed.
		/// </remarks>
		private string evalRequest;

		/// <summary>
		/// The stack frame in which to evaluate
		/// <see cref="evalRequest">evalRequest</see>
		/// .
		/// </summary>
		private StackFrame evalFrame;

		/// <summary>
		/// The result of evaluating
		/// <see cref="evalRequest">evalRequest</see>
		/// .
		/// </summary>
		private string evalResult;

		/// <summary>Whether the debugger should break when a script exception is thrown.</summary>
		/// <remarks>Whether the debugger should break when a script exception is thrown.</remarks>
		private bool breakOnExceptions;

		/// <summary>Whether the debugger should break when a script function is entered.</summary>
		/// <remarks>Whether the debugger should break when a script function is entered.</remarks>
		internal bool breakOnEnter;

		/// <summary>
		/// Whether the debugger should break when a script function is returned
		/// from.
		/// </summary>
		/// <remarks>
		/// Whether the debugger should break when a script function is returned
		/// from.
		/// </remarks>
		internal bool breakOnReturn;

		/// <summary>Table mapping URLs to information about the script source.</summary>
		/// <remarks>Table mapping URLs to information about the script source.</remarks>
		private readonly IDictionary<string, SourceInfo> urlToSourceInfo = new ConcurrentDictionary<string, SourceInfo>();

		/// <summary>Table mapping function names to information about the function.</summary>
		/// <remarks>Table mapping function names to information about the function.</remarks>
		private readonly IDictionary<string, FunctionSource> functionNames = new ConcurrentDictionary<string, FunctionSource>();

		/// <summary>Table mapping functions to information about the function.</summary>
		/// <remarks>Table mapping functions to information about the function.</remarks>
		private readonly IDictionary<DebuggableScript, FunctionSource> functionToSource = new ConcurrentDictionary<DebuggableScript, FunctionSource>();

		/// <summary>
		/// ContextFactory.Listener instance attached to
		/// <see cref="contextFactory">contextFactory</see>
		/// .
		/// </summary>
		private ContextFactory.Listener listener;

		// Constants for instructing the debugger what action to perform
		// to end interruption.  Used by 'returnValue'.
		// Constants for the DimIProxy interface implementation class.
		/// <summary>Sets the GuiCallback object to use.</summary>
		/// <remarks>Sets the GuiCallback object to use.</remarks>
		public void SetGuiCallback(GuiCallback callback)
		{
			this.callback = callback;
		}

		/// <summary>Tells the debugger to break at the next opportunity.</summary>
		/// <remarks>Tells the debugger to break at the next opportunity.</remarks>
		public void SetBreak()
		{
			this.breakFlag = true;
		}

		/// <summary>Sets the ScopeProvider to be used.</summary>
		/// <remarks>Sets the ScopeProvider to be used.</remarks>
		public void SetScopeProvider(ScopeProvider scopeProvider)
		{
			this.scopeProvider = scopeProvider;
		}

		/// <summary>Sets the ScopeProvider to be used.</summary>
		/// <remarks>Sets the ScopeProvider to be used.</remarks>
		public void SetSourceProvider(SourceProvider sourceProvider)
		{
			this.sourceProvider = sourceProvider;
		}

		/// <summary>Switches context to the stack frame with the given index.</summary>
		/// <remarks>Switches context to the stack frame with the given index.</remarks>
		public void ContextSwitch(int frameIndex)
		{
			this.frameIndex = frameIndex;
		}

		/// <summary>Sets whether the debugger should break on exceptions.</summary>
		/// <remarks>Sets whether the debugger should break on exceptions.</remarks>
		public void SetBreakOnExceptions(bool breakOnExceptions)
		{
			this.breakOnExceptions = breakOnExceptions;
		}

		/// <summary>Sets whether the debugger should break on function entering.</summary>
		/// <remarks>Sets whether the debugger should break on function entering.</remarks>
		public void SetBreakOnEnter(bool breakOnEnter)
		{
			this.breakOnEnter = breakOnEnter;
		}

		/// <summary>Sets whether the debugger should break on function return.</summary>
		/// <remarks>Sets whether the debugger should break on function return.</remarks>
		public void SetBreakOnReturn(bool breakOnReturn)
		{
			this.breakOnReturn = breakOnReturn;
		}

		/// <summary>Attaches the debugger to the given ContextFactory.</summary>
		/// <remarks>Attaches the debugger to the given ContextFactory.</remarks>
		public void AttachTo(ContextFactory factory)
		{
			Detach();
			this.contextFactory = factory;
			this.listener = new ListenerImpl(this);
			factory.AddListener(this.listener);
		}

		/// <summary>Detaches the debugger from the current ContextFactory.</summary>
		/// <remarks>Detaches the debugger from the current ContextFactory.</remarks>
		public void Detach()
		{
			if (listener != null)
			{
				contextFactory.RemoveListener(listener);
				contextFactory = null;
				listener = null;
			}
		}

		/// <summary>Releases resources associated with this debugger.</summary>
		/// <remarks>Releases resources associated with this debugger.</remarks>
		public void Dispose()
		{
			Detach();
		}

		/// <summary>Returns the FunctionSource object for the given script or function.</summary>
		/// <remarks>Returns the FunctionSource object for the given script or function.</remarks>
		internal FunctionSource GetFunctionSource(DebuggableScript fnOrScript)
		{
			FunctionSource fsource = FunctionSource(fnOrScript);
			if (fsource == null)
			{
				string url = GetNormalizedUrl(fnOrScript);
				SourceInfo si = SourceInfo(url);
				if (si == null)
				{
					if (!fnOrScript.IsGeneratedScript())
					{
						// Not eval or Function, try to load it from URL
						string source = LoadSource(url);
						if (source != null)
						{
							DebuggableScript top = fnOrScript;
							for (; ; )
							{
								DebuggableScript parent = top.GetParent();
								if (parent == null)
								{
									break;
								}
								top = parent;
							}
							RegisterTopScript(top, source);
							fsource = FunctionSource(fnOrScript);
						}
					}
				}
			}
			return fsource;
		}

		/// <summary>Loads the script at the given URL.</summary>
		/// <remarks>Loads the script at the given URL.</remarks>
		private string LoadSource(string sourceUrl)
		{
			string source = null;
			int hash = sourceUrl.IndexOf('#');
			if (hash >= 0)
			{
				sourceUrl = sourceUrl.Substring(0, hash);
			}
			try
			{
				Stream @is;
				if (sourceUrl.IndexOf(':') < 0)
				{
					// Can be a file name
					try
					{
						if (sourceUrl.StartsWith("~/"))
						{
							string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
							if (home != null)
							{
								string pathFromHome = sourceUrl.Substring(2);
								FilePath f = new FilePath(home, pathFromHome);
								if (f.Exists())
								{
									@is = new FileInputStream(f);
									goto openStream_break;
								}
							}
						}
						FilePath f_1 = new FilePath(sourceUrl);
						if (f_1.Exists())
						{
							@is = new FileInputStream(f_1);
							goto openStream_break;
						}
					}
					catch (SecurityException)
					{
					}
					// No existing file, assume missed http://
					if (sourceUrl.StartsWith("//"))
					{
						sourceUrl = "http:" + sourceUrl;
					}
					else
					{
						if (sourceUrl.StartsWith("/"))
						{
							sourceUrl = "http://127.0.0.1" + sourceUrl;
						}
						else
						{
							sourceUrl = "http://" + sourceUrl;
						}
					}
				}
				@is = File.OpenRead(sourceUrl);
openStream_break: ;
				try
				{
					source = new StreamReader(@is).ReadToEnd();
				}
				finally
				{
					@is.Close();
				}
			}
			catch (IOException ex)
			{
				System.Console.Error.WriteLine("Failed to load source from " + sourceUrl + ": " + ex);
			}
			return source;
		}

		/// <summary>Registers the given script as a top-level script in the debugger.</summary>
		/// <remarks>Registers the given script as a top-level script in the debugger.</remarks>
		internal void RegisterTopScript(DebuggableScript topScript, string source)
		{
			if (!topScript.IsTopLevel())
			{
				throw new ArgumentException();
			}
			string url = GetNormalizedUrl(topScript);
			DebuggableScript[] functions = GetAllFunctions(topScript);
			if (sourceProvider != null)
			{
				string providedSource = sourceProvider.GetSource(topScript);
				if (providedSource != null)
				{
					source = providedSource;
				}
			}
			SourceInfo sourceInfo = new SourceInfo(source, functions, url);
			lock (urlToSourceInfo)
			{
				SourceInfo old = urlToSourceInfo.GetValueOrDefault(url);
				if (old != null)
				{
					sourceInfo.CopyBreakpointsFrom(old);
				}
				urlToSourceInfo [url] = sourceInfo;
				for (int i = 0; i != sourceInfo.FunctionSourcesTop(); ++i)
				{
					FunctionSource fsource = sourceInfo.GetFunctionSource(i);
					string name = fsource.Name;
					if (name.Length != 0)
					{
						functionNames [name] = fsource;
					}
				}
			}
			lock (functionToSource)
			{
				for (int i = 0; i != functions.Length; ++i)
				{
					FunctionSource fsource = sourceInfo.GetFunctionSource(i);
					functionToSource [functions[i]] = fsource;
				}
			}
			callback.UpdateSourceText(sourceInfo);
		}

		/// <summary>Returns the FunctionSource object for the given function or script.</summary>
		/// <remarks>Returns the FunctionSource object for the given function or script.</remarks>
		private FunctionSource FunctionSource(DebuggableScript fnOrScript)
		{
			return functionToSource.GetValueOrDefault(fnOrScript);
		}

		/// <summary>Returns an array of all function names.</summary>
		/// <remarks>Returns an array of all function names.</remarks>
		public string[] FunctionNames()
		{
			lock (urlToSourceInfo)
			{
				return functionNames.Keys.ToArray();
			}
		}

		/// <summary>Returns the FunctionSource object for the function with the given name.</summary>
		/// <remarks>Returns the FunctionSource object for the function with the given name.</remarks>
		public FunctionSource FunctionSourceByName(string functionName)
		{
			return functionNames.GetValueOrDefault(functionName);
		}

		/// <summary>Returns the SourceInfo object for the given URL.</summary>
		/// <remarks>Returns the SourceInfo object for the given URL.</remarks>
		public SourceInfo SourceInfo(string url)
		{
			return urlToSourceInfo.GetValueOrDefault(url);
		}

		/// <summary>Returns the source URL for the given script or function.</summary>
		/// <remarks>Returns the source URL for the given script or function.</remarks>
		private string GetNormalizedUrl(DebuggableScript fnOrScript)
		{
			string url = fnOrScript.GetSourceName();
			if (url == null)
			{
				url = "<stdin>";
			}
			else
			{
				// Not to produce window for eval from different lines,
				// strip line numbers, i.e. replace all #[0-9]+\(eval\) by
				// (eval)
				// Option: similar teatment for Function?
				char evalSeparator = '#';
				StringBuilder sb = null;
				int urlLength = url.Length;
				int cursor = 0;
				for (; ; )
				{
					int searchStart = url.IndexOf(evalSeparator, cursor);
					if (searchStart < 0)
					{
						break;
					}
					string replace = null;
					int i = searchStart + 1;
					while (i != urlLength)
					{
						int c = url[i];
						if (!('0' <= c && c <= '9'))
						{
							break;
						}
						++i;
					}
					if (i != searchStart + 1)
					{
						// i points after #[0-9]+
						if ("(eval)".RegionMatches(0, url, i, 6))
						{
							cursor = i + 6;
							replace = "(eval)";
						}
					}
					if (replace == null)
					{
						break;
					}
					if (sb == null)
					{
						sb = new StringBuilder();
						sb.Append(url.Substring(0, searchStart));
					}
					sb.Append(replace);
				}
				if (sb != null)
				{
					if (cursor != urlLength)
					{
						sb.Append(url.Substring(cursor));
					}
					url = sb.ToString();
				}
			}
			return url;
		}

		/// <summary>Returns an array of all functions in the given script.</summary>
		/// <remarks>Returns an array of all functions in the given script.</remarks>
		private static DebuggableScript[] GetAllFunctions(DebuggableScript function)
		{
			var functions = new List<DebuggableScript>();
			CollectFunctions_r(function, functions);
			return functions.ToArray();
		}

		/// <summary>
		/// Helper function for
		/// <see cref="GetAllFunctions(Rhino.Debug.DebuggableScript)">GetAllFunctions(Rhino.Debug.DebuggableScript)</see>
		/// .
		/// </summary>
		private static void CollectFunctions_r(DebuggableScript function, ICollection<DebuggableScript> array)
		{
			array.Add(function);
			for (int i = 0; i != function.GetFunctionCount(); ++i)
			{
				CollectFunctions_r(function.GetFunction(i), array);
			}
		}

		/// <summary>Clears all breakpoints.</summary>
		/// <remarks>Clears all breakpoints.</remarks>
		public void ClearAllBreakpoints()
		{
			foreach (SourceInfo si in urlToSourceInfo.Values)
			{
				si.RemoveAllBreakpoints();
			}
		}

		/// <summary>Called when a breakpoint has been hit.</summary>
		/// <remarks>Called when a breakpoint has been hit.</remarks>
		internal void HandleBreakpointHit(StackFrame frame, Context cx)
		{
			breakFlag = false;
			Interrupted(cx, frame, null);
		}

		/// <summary>Called when a script exception has been thrown.</summary>
		/// <remarks>Called when a script exception has been thrown.</remarks>
		internal void HandleExceptionThrown(Context cx, Exception ex, StackFrame frame)
		{
			if (breakOnExceptions)
			{
				ContextData cd = frame.ContextData();
				if (cd.lastProcessedException != ex)
				{
					Interrupted(cx, frame, ex);
					cd.lastProcessedException = ex;
				}
			}
		}

		/// <summary>Returns the current ContextData object.</summary>
		/// <remarks>Returns the current ContextData object.</remarks>
		public ContextData CurrentContextData()
		{
			return interruptedContextData;
		}

		/// <summary>Sets the action to perform to end interruption.</summary>
		/// <remarks>Sets the action to perform to end interruption.</remarks>
		public void SetReturnValue(int returnValue)
		{
			lock (monitor)
			{
				this.returnValue = returnValue;
				Monitor.Pulse(monitor);
			}
		}

		/// <summary>Resumes execution of script.</summary>
		/// <remarks>Resumes execution of script.</remarks>
		public void Go()
		{
			lock (monitor)
			{
				this.returnValue = GO;
				Monitor.PulseAll(monitor);
			}
		}

		/// <summary>Evaluates the given script.</summary>
		/// <remarks>Evaluates the given script.</remarks>
		public string Eval(string expr)
		{
			string result = "undefined";
			if (expr == null)
			{
				return result;
			}
			ContextData contextData = CurrentContextData();
			if (contextData == null || frameIndex >= contextData.FrameCount())
			{
				return result;
			}
			StackFrame frame = contextData.GetFrame(frameIndex);
			if (contextData.eventThreadFlag)
			{
				Context cx = Context.GetCurrentContext();
				result = Do_eval(cx, frame, expr);
			}
			else
			{
				lock (monitor)
				{
					if (insideInterruptLoop)
					{
						evalRequest = expr;
						evalFrame = frame;
						Monitor.Pulse(monitor);
						do
						{
							try
							{
								Monitor.Wait(monitor);
							}
							catch (Exception)
							{
								System.Threading.Thread.CurrentThread.Interrupt();
								break;
							}
						}
						while (evalRequest != null);
						result = evalResult;
					}
				}
			}
			return result;
		}

		/// <summary>Compiles the given script.</summary>
		/// <remarks>Compiles the given script.</remarks>
		public void CompileScript(string url, string text)
		{
			contextFactory.Call(cx => cx.CompileString(text, url, 1, null));
		}

		/// <summary>Evaluates the given script.</summary>
		public void EvalScript(string url, string text)
		{
			contextFactory.Call(cx =>
			{
				Scriptable scope = null;
				if (scopeProvider != null)
				{
					scope = scopeProvider.GetScope();
				}
				if (scope == null)
				{
					scope = new ImporterTopLevel(cx);
				}
				cx.EvaluateString(scope, text, url, 1, null);
				return null;
			});
		}

		/// <summary>Converts the given script object to a string.</summary>
		public string ObjectToString(object @object)
		{
			return (string) contextFactory.Call(cx =>
			{
				if (@object == Undefined.instance)
					return "undefined";

				if (@object == null)
					return "null";

				if (@object is NativeCall)
					return "[object Call]";

				return Context.ToString(@object);
			});
		}

		/// <summary>Returns whether the given string is syntactically valid script.</summary>
		/// <remarks>Returns whether the given string is syntactically valid script.</remarks>
		public bool StringIsCompilableUnit(string str)
		{
			return (bool) contextFactory.Call(cx => cx.StringIsCompilableUnit(str));
		}

		/// <summary>Returns the value of a property on the given script object.</summary>
		/// <remarks>Returns the value of a property on the given script object.</remarks>
		public object GetObjectProperty(object @object, object id)
		{
			return contextFactory.Call(cx => GetObjectPropertyImpl(@object, id));
		}

		/// <summary>Returns an array of the property names on the given script object.</summary>
		/// <remarks>Returns an array of the property names on the given script object.</remarks>
		public object[] GetObjectIds(object @object)
		{
			return (object[]) contextFactory.Call(cx => GetObjectIdsImpl(cx, @object));
		}

		/// <summary>Returns the value of a property on the given script object.</summary>
		private static object GetObjectPropertyImpl(object @object, object id)
		{
			var scriptable = (Scriptable)@object;
			var name = id as string;
			if (name != null)
			{
				switch (name)
				{
					case "this":
						return scriptable;

					case "__proto__":
						return scriptable.Prototype;

					case "__parent__":
						return scriptable.ParentScope;
					
					default:
					{
						object result = ScriptableObject.GetProperty(scriptable, name);
						if (result == ScriptableConstants.NOT_FOUND)
							return Undefined.instance;
						
						return result;
					}
				}
			}
			else
			{
				int index = (int) id;
				object result = ScriptableObject.GetProperty(scriptable, index);
				if (result == ScriptableConstants.NOT_FOUND)
					return Undefined.instance;
				
				return result;
			}
		}

		/// <summary>Returns an array of the property names on the given script object.</summary>
		/// <remarks>Returns an array of the property names on the given script object.</remarks>
		private object[] GetObjectIdsImpl(Context cx, object @object)
		{
			if (!(@object is Scriptable) || @object == Undefined.instance)
			{
				return Context.emptyArgs;
			}
			object[] ids;
			Scriptable scriptable = (Scriptable)@object;
			if (scriptable is DebuggableObject)
			{
				ids = ((DebuggableObject)scriptable).GetAllIds();
			}
			else
			{
				ids = scriptable.GetIds();
			}
			Scriptable proto = scriptable.Prototype;
			Scriptable parent = scriptable.ParentScope;
			int extra = 0;
			if (proto != null)
			{
				++extra;
			}
			if (parent != null)
			{
				++extra;
			}
			if (extra != 0)
			{
				object[] tmp = new object[extra + ids.Length];
				System.Array.Copy(ids, 0, tmp, extra, ids.Length);
				ids = tmp;
				extra = 0;
				if (proto != null)
				{
					ids[extra++] = "__proto__";
				}
				if (parent != null)
				{
					ids[extra++] = "__parent__";
				}
			}
			return ids;
		}

		/// <summary>Interrupts script execution.</summary>
		/// <remarks>Interrupts script execution.</remarks>
		private void Interrupted(Context cx, StackFrame frame, Exception scriptException)
		{
			ContextData contextData = frame.ContextData();
			bool eventThreadFlag = callback.IsGuiEventThread();
			contextData.eventThreadFlag = eventThreadFlag;
			bool recursiveEventThreadCall = false;
			lock (eventThreadMonitor)
			{
				if (eventThreadFlag)
				{
					if (interruptedContextData != null)
					{
						recursiveEventThreadCall = true;
						goto interruptedCheck_break;
					}
				}
				else
				{
					while (interruptedContextData != null)
					{
						try
						{
							Monitor.Wait(eventThreadMonitor);
						}
						catch (Exception)
						{
							return;
						}
					}
				}
				interruptedContextData = contextData;
			}
interruptedCheck_break: ;
			if (recursiveEventThreadCall)
			{
				// XXX: For now the following is commented out as on Linux
				// too deep recursion of dispatchNextGuiEvent causes GUI lockout.
				// Note: it can make GUI unresponsive if long-running script
				// will be called on GUI thread while processing another interrupt
				if (false)
				{
					// Run event dispatch until gui sets a flag to exit the initial
					// call to interrupted.
					while (this.returnValue == -1)
					{
						try
						{
							callback.DispatchNextGuiEvent();
						}
						catch (Exception)
						{
						}
					}
				}
				return;
			}
			if (interruptedContextData == null)
			{
				Kit.CodeBug();
			}
			try
			{
				do
				{
					int frameCount = contextData.FrameCount();
					this.frameIndex = frameCount - 1;
					string threadTitle = System.Threading.Thread.CurrentThread.Name;
					string alertMessage;
					if (scriptException == null)
					{
						alertMessage = null;
					}
					else
					{
						alertMessage = scriptException.ToString();
					}
					int returnValue = -1;
					if (!eventThreadFlag)
					{
						lock (monitor)
						{
							if (insideInterruptLoop)
							{
								Kit.CodeBug();
							}
							this.insideInterruptLoop = true;
							this.evalRequest = null;
							this.returnValue = -1;
							callback.EnterInterrupt(frame, threadTitle, alertMessage);
							try
							{
								for (; ; )
								{
									try
									{
										Monitor.Wait(monitor);
									}
									catch (Exception)
									{
										System.Threading.Thread.CurrentThread.Interrupt();
										break;
									}
									if (evalRequest != null)
									{
										this.evalResult = null;
										try
										{
											evalResult = Do_eval(cx, evalFrame, evalRequest);
										}
										finally
										{
											evalRequest = null;
											evalFrame = null;
											Monitor.Pulse(monitor);
										}
										continue;
									}
									if (this.returnValue != -1)
									{
										returnValue = this.returnValue;
										break;
									}
								}
							}
							finally
							{
								insideInterruptLoop = false;
							}
						}
					}
					else
					{
						this.returnValue = -1;
						callback.EnterInterrupt(frame, threadTitle, alertMessage);
						while (this.returnValue == -1)
						{
							try
							{
								callback.DispatchNextGuiEvent();
							}
							catch (Exception)
							{
							}
						}
						returnValue = this.returnValue;
					}
					switch (returnValue)
					{
						case STEP_OVER:
						{
							contextData.breakNextLine = true;
							contextData.stopAtFrameDepth = contextData.FrameCount();
							break;
						}

						case STEP_INTO:
						{
							contextData.breakNextLine = true;
							contextData.stopAtFrameDepth = -1;
							break;
						}

						case STEP_OUT:
						{
							if (contextData.FrameCount() > 1)
							{
								contextData.breakNextLine = true;
								contextData.stopAtFrameDepth = contextData.FrameCount() - 1;
							}
							break;
						}
					}
				}
				while (false);
			}
			finally
			{
				lock (eventThreadMonitor)
				{
					interruptedContextData = null;
					Monitor.PulseAll(eventThreadMonitor);
				}
			}
		}

		/// <summary>Evaluates script in the given stack frame.</summary>
		/// <remarks>Evaluates script in the given stack frame.</remarks>
		private static string Do_eval(Context cx, StackFrame frame, string expr)
		{
			string resultString;
			Rhino.Debug.Debugger saved_debugger = cx.GetDebugger();
			object saved_data = cx.GetDebuggerContextData();
			int saved_level = cx.GetOptimizationLevel();
			cx.SetDebugger(null, null);
			cx.SetOptimizationLevel(-1);
			cx.SetGeneratingDebug(false);
			try
			{
				Callable script = (Callable)cx.CompileString(expr, string.Empty, 0, null);
				object result = script.Call(cx, frame.scope, frame.thisObj, ScriptRuntime.emptyArgs);
				if (result == Undefined.instance)
				{
					resultString = string.Empty;
				}
				else
				{
					resultString = ScriptRuntime.ToString(result);
				}
			}
			catch (Exception exc)
			{
				resultString = exc.Message;
			}
			finally
			{
				cx.SetGeneratingDebug(true);
				cx.SetOptimizationLevel(saved_level);
				cx.SetDebugger(saved_debugger, saved_data);
			}
			return resultString ?? "null";
		}
	}
}
