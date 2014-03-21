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
using System.Linq;
using System.Security;
using System.Text;
using Rhino.Debug;
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
		private readonly IDictionary<string, SourceInfo> urlToSourceInfo = Sharpen.Collections.SynchronizedMap(new Dictionary<string, SourceInfo>());

		/// <summary>Table mapping function names to information about the function.</summary>
		/// <remarks>Table mapping function names to information about the function.</remarks>
		private readonly IDictionary<string, FunctionSource> functionNames = Sharpen.Collections.SynchronizedMap(new Dictionary<string, FunctionSource>());

		/// <summary>Table mapping functions to information about the function.</summary>
		/// <remarks>Table mapping functions to information about the function.</remarks>
		private readonly IDictionary<DebuggableScript, FunctionSource> functionToSource = Sharpen.Collections.SynchronizedMap(new Dictionary<DebuggableScript, FunctionSource>());

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
							string home = SecurityUtilities.GetSystemProperty("user.home");
							if (home != null)
							{
								string pathFromHome = sourceUrl.Substring(2);
								FilePath f = new FilePath(new FilePath(home), pathFromHome);
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
				@is = (new Uri(sourceUrl)).OpenStream();
openStream_break: ;
				try
				{
					source = Kit.ReadReader(new StreamReader(@is));
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
				SourceInfo old = urlToSourceInfo.Get(url);
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
			return functionToSource.Get(fnOrScript);
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
			return functionNames.Get(functionName);
		}

		/// <summary>Returns the SourceInfo object for the given URL.</summary>
		/// <remarks>Returns the SourceInfo object for the given URL.</remarks>
		public SourceInfo SourceInfo(string url)
		{
			return urlToSourceInfo.Get(url);
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
			ObjArray functions = new ObjArray();
			CollectFunctions_r(function, functions);
			DebuggableScript[] result = new DebuggableScript[functions.Size()];
			functions.ToArray(result);
			return result;
		}

		/// <summary>
		/// Helper function for
		/// <see cref="GetAllFunctions(Rhino.Debug.DebuggableScript)">GetAllFunctions(Rhino.Debug.DebuggableScript)</see>
		/// .
		/// </summary>
		private static void CollectFunctions_r(DebuggableScript function, ObjArray array)
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
				Sharpen.Runtime.Notify(monitor);
			}
		}

		/// <summary>Resumes execution of script.</summary>
		/// <remarks>Resumes execution of script.</remarks>
		public void Go()
		{
			lock (monitor)
			{
				this.returnValue = GO;
				Sharpen.Runtime.NotifyAll(monitor);
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
						Sharpen.Runtime.Notify(monitor);
						do
						{
							try
							{
								Sharpen.Runtime.Wait(monitor);
							}
							catch (Exception)
							{
								Sharpen.Thread.CurrentThread().Interrupt();
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
			return contextFactory.Call(cx => GetObjectPropertyImpl(cx, @object, id));
		}

		/// <summary>Returns an array of the property names on the given script object.</summary>
		/// <remarks>Returns an array of the property names on the given script object.</remarks>
		public object[] GetObjectIds(object @object)
		{
			return (object[]) contextFactory.Call(cx => GetObjectIdsImpl(cx, @object));
		}

		/// <summary>Returns the value of a property on the given script object.</summary>
		/// <remarks>Returns the value of a property on the given script object.</remarks>
		private object GetObjectPropertyImpl(Context cx, object @object, object id)
		{
			Scriptable scriptable = (Scriptable)@object;
			object result;
			if (id is string)
			{
				string name = (string)id;
				if (name.Equals("this"))
				{
					result = scriptable;
				}
				else
				{
					if (name.Equals("__proto__"))
					{
						result = scriptable.Prototype;
					}
					else
					{
						if (name.Equals("__parent__"))
						{
							result = scriptable.ParentScope;
						}
						else
						{
							result = ScriptableObject.GetProperty(scriptable, name);
							if (result == ScriptableConstants.NOT_FOUND)
							{
								result = Undefined.instance;
							}
						}
					}
				}
			}
			else
			{
				int index = System.Convert.ToInt32(((int)id));
				result = ScriptableObject.GetProperty(scriptable, index);
				if (result == ScriptableConstants.NOT_FOUND)
				{
					result = Undefined.instance;
				}
			}
			return result;
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
							Sharpen.Runtime.Wait(eventThreadMonitor);
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
					string threadTitle = Sharpen.Thread.CurrentThread().ToString();
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
										Sharpen.Runtime.Wait(monitor);
									}
									catch (Exception)
									{
										Sharpen.Thread.CurrentThread().Interrupt();
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
											Sharpen.Runtime.Notify(monitor);
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
					Sharpen.Runtime.NotifyAll(eventThreadMonitor);
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

	internal sealed class DebuggerImpl : Debug.Debugger
	{
		/// <summary>The debugger.</summary>
		private readonly Dim dim;

		internal DebuggerImpl(Dim dim)
		{
			this.dim = dim;
		}

		/// <summary>Returns a StackFrame for the given function or script.</summary>
		/// <remarks>Returns a StackFrame for the given function or script.</remarks>
		public DebugFrame GetFrame(Context cx, DebuggableScript fnOrScript)
		{
			var item = dim.GetFunctionSource(fnOrScript);
			if (item == null) // Can not debug if source is not available
				return null;

			return new StackFrame(cx, dim, item);
		}

		/// <summary>Called when compilation is finished.</summary>
		/// <remarks>Called when compilation is finished.</remarks>
		public void HandleCompilationDone(Context cx, DebuggableScript fnOrScript, string source)
		{
			if (!fnOrScript.IsTopLevel())
				return;
				
			dim.RegisterTopScript(fnOrScript, source);
		}
	}

	internal sealed class ListenerImpl : ContextFactory.Listener
	{
		internal ListenerImpl(Dim dim)
		{
			this.dim = dim;
		}

		/// <summary>The debugger.</summary>
		private readonly Dim dim;

		/// <summary>Called when a Context is created.</summary>
		public void ContextCreated(Context cx)
		{
			cx.SetDebugger(new DebuggerImpl(dim), new ContextData());
			cx.SetGeneratingDebug(true);
			cx.SetOptimizationLevel(-1);
		}

		/// <summary>Called when a Context is destroyed.</summary>
		public void ContextReleased(Context cx)
		{
		}
	}

	/// <summary>Class to store information about a stack.</summary>
	/// <remarks>Class to store information about a stack.</remarks>
	public sealed class ContextData
	{
		/// <summary>The stack frames.</summary>
		/// <remarks>The stack frames.</remarks>
		private readonly ObjArray frameStack = new ObjArray();

		/// <summary>Whether the debugger should break at the next line in this context.</summary>
		/// <remarks>Whether the debugger should break at the next line in this context.</remarks>
		internal bool breakNextLine;

		/// <summary>The frame depth the debugger should stop at.</summary>
		/// <remarks>
		/// The frame depth the debugger should stop at.  Used to implement
		/// "step over" and "step out".
		/// </remarks>
		internal int stopAtFrameDepth = -1;

		/// <summary>Whether this context is in the event thread.</summary>
		/// <remarks>Whether this context is in the event thread.</remarks>
		internal bool eventThreadFlag;

		/// <summary>The last exception that was processed.</summary>
		/// <remarks>The last exception that was processed.</remarks>
		internal Exception lastProcessedException;

		/// <summary>Returns the ContextData for the given Context.</summary>
		/// <remarks>Returns the ContextData for the given Context.</remarks>
		public static ContextData Get(Context cx)
		{
			return (ContextData)cx.GetDebuggerContextData();
		}

		/// <summary>Returns the number of stack frames.</summary>
		/// <remarks>Returns the number of stack frames.</remarks>
		public int FrameCount()
		{
			return frameStack.Size();
		}

		/// <summary>Returns the stack frame with the given index.</summary>
		/// <remarks>Returns the stack frame with the given index.</remarks>
		public StackFrame GetFrame(int frameNumber)
		{
			int num = frameStack.Size() - frameNumber - 1;
			return (StackFrame)frameStack.Get(num);
		}

		/// <summary>Pushes a stack frame on to the stack.</summary>
		/// <remarks>Pushes a stack frame on to the stack.</remarks>
		internal void PushFrame(StackFrame frame)
		{
			frameStack.Push(frame);
		}

		/// <summary>Pops a stack frame from the stack.</summary>
		/// <remarks>Pops a stack frame from the stack.</remarks>
		internal void PopFrame()
		{
			frameStack.Pop();
		}
	}

	/// <summary>Object to represent one stack frame.</summary>
	/// <remarks>Object to represent one stack frame.</remarks>
	public sealed class StackFrame : DebugFrame
	{
		/// <summary>The debugger.</summary>
		/// <remarks>The debugger.</remarks>
		private Dim dim;

		/// <summary>The ContextData for the Context being debugged.</summary>
		/// <remarks>The ContextData for the Context being debugged.</remarks>
		private ContextData contextData;

		/// <summary>The scope.</summary>
		/// <remarks>The scope.</remarks>
		public Scriptable scope;

		/// <summary>The 'this' object.</summary>
		/// <remarks>The 'this' object.</remarks>
		public Scriptable thisObj;

		/// <summary>Information about the function.</summary>
		/// <remarks>Information about the function.</remarks>
		private FunctionSource fsource;

		/// <summary>Array of breakpoint state for each source line.</summary>
		/// <remarks>Array of breakpoint state for each source line.</remarks>
		private bool[] breakpoints;

		/// <summary>Current line number.</summary>
		/// <remarks>Current line number.</remarks>
		private int lineNumber;

		/// <summary>Creates a new StackFrame.</summary>
		/// <remarks>Creates a new StackFrame.</remarks>
		internal StackFrame(Context cx, Dim dim, FunctionSource fsource)
		{
			this.dim = dim;
			this.contextData = Debugger.ContextData.Get(cx);
			this.fsource = fsource;
			this.breakpoints = fsource.SourceInfo.breakpoints;
			this.lineNumber = fsource.FirstLine;
		}

		/// <summary>Called when the stack frame is entered.</summary>
		/// <remarks>Called when the stack frame is entered.</remarks>
		public void OnEnter(Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			contextData.PushFrame(this);
			this.scope = scope;
			this.thisObj = thisObj;
			if (dim.breakOnEnter)
			{
				dim.HandleBreakpointHit(this, cx);
			}
		}

		/// <summary>Called when the current position has changed.</summary>
		/// <remarks>Called when the current position has changed.</remarks>
		public void OnLineChange(Context cx, int lineno)
		{
			this.lineNumber = lineno;
			if (!breakpoints[lineno] && !dim.breakFlag)
			{
				bool lineBreak = contextData.breakNextLine;
				if (lineBreak && contextData.stopAtFrameDepth >= 0)
				{
					lineBreak = (contextData.FrameCount() <= contextData.stopAtFrameDepth);
				}
				if (!lineBreak)
				{
					return;
				}
				contextData.stopAtFrameDepth = -1;
				contextData.breakNextLine = false;
			}
			dim.HandleBreakpointHit(this, cx);
		}

		/// <summary>Called when an exception has been thrown.</summary>
		/// <remarks>Called when an exception has been thrown.</remarks>
		public void OnExceptionThrown(Context cx, Exception exception)
		{
			dim.HandleExceptionThrown(cx, exception, this);
		}

		/// <summary>Called when the stack frame has been left.</summary>
		/// <remarks>Called when the stack frame has been left.</remarks>
		public void OnExit(Context cx, bool byThrow, object resultOrException)
		{
			if (dim.breakOnReturn && !byThrow)
			{
				dim.HandleBreakpointHit(this, cx);
			}
			contextData.PopFrame();
		}

		/// <summary>Called when a 'debugger' statement is executed.</summary>
		/// <remarks>Called when a 'debugger' statement is executed.</remarks>
		public void OnDebuggerStatement(Context cx)
		{
			dim.HandleBreakpointHit(this, cx);
		}

		/// <summary>Returns the SourceInfo object for the function.</summary>
		/// <remarks>Returns the SourceInfo object for the function.</remarks>
		public SourceInfo SourceInfo()
		{
			return fsource.SourceInfo;
		}

		/// <summary>Returns the ContextData object for the Context.</summary>
		/// <remarks>Returns the ContextData object for the Context.</remarks>
		public ContextData ContextData()
		{
			return contextData;
		}

		/// <summary>Returns the scope object for this frame.</summary>
		/// <remarks>Returns the scope object for this frame.</remarks>
		public object Scope()
		{
			return scope;
		}

		/// <summary>Returns the 'this' object for this frame.</summary>
		/// <remarks>Returns the 'this' object for this frame.</remarks>
		public object ThisObj()
		{
			return thisObj;
		}

		/// <summary>Returns the source URL.</summary>
		/// <remarks>Returns the source URL.</remarks>
		public string GetUrl()
		{
			return fsource.SourceInfo.Url();
		}

		/// <summary>Returns the current line number.</summary>
		/// <remarks>Returns the current line number.</remarks>
		public int GetLineNumber()
		{
			return lineNumber;
		}

		/// <summary>Returns the current function name.</summary>
		/// <remarks>Returns the current function name.</remarks>
		public string GetFunctionName()
		{
			return fsource.Name;
		}
	}

	/// <summary>Class to store information about a script source.</summary>
	/// <remarks>Class to store information about a script source.</remarks>
	public sealed class SourceInfo
	{
		/// <summary>An empty array of booleans.</summary>
		/// <remarks>An empty array of booleans.</remarks>
		private static readonly bool[] EMPTY_BOOLEAN_ARRAY = new bool[0];

		/// <summary>The script.</summary>
		/// <remarks>The script.</remarks>
		private readonly string source;

		/// <summary>The URL of the script.</summary>
		/// <remarks>The URL of the script.</remarks>
		private readonly string url;

		/// <summary>Array indicating which lines can have breakpoints set.</summary>
		/// <remarks>Array indicating which lines can have breakpoints set.</remarks>
		private readonly bool[] breakableLines;

		/// <summary>Array indicating whether a breakpoint is set on the line.</summary>
		/// <remarks>Array indicating whether a breakpoint is set on the line.</remarks>
		internal readonly bool[] breakpoints;

		/// <summary>Array of FunctionSource objects for the functions in the script.</summary>
		/// <remarks>Array of FunctionSource objects for the functions in the script.</remarks>
		private readonly FunctionSource[] functionSources;

		/// <summary>Creates a new SourceInfo object.</summary>
		/// <remarks>Creates a new SourceInfo object.</remarks>
		internal SourceInfo(string source, DebuggableScript[] functions, string normilizedUrl)
		{
			this.source = source;
			this.url = normilizedUrl;
			int N = functions.Length;
			int[][] lineArrays = new int[N][];
			for (int i = 0; i != N; ++i)
			{
				lineArrays[i] = functions[i].GetLineNumbers();
			}
			int minAll = 0;
			int maxAll = -1;
			int[] firstLines = new int[N];
			for (int i_1 = 0; i_1 != N; ++i_1)
			{
				int[] lines = lineArrays[i_1];
				if (lines == null || lines.Length == 0)
				{
					firstLines[i_1] = -1;
				}
				else
				{
					int max;
					int min = max = lines[0];
					for (int j = 1; j != lines.Length; ++j)
					{
						int line = lines[j];
						if (line < min)
						{
							min = line;
						}
						else
						{
							if (line > max)
							{
								max = line;
							}
						}
					}
					firstLines[i_1] = min;
					if (minAll > maxAll)
					{
						minAll = min;
						maxAll = max;
					}
					else
					{
						if (min < minAll)
						{
							minAll = min;
						}
						if (max > maxAll)
						{
							maxAll = max;
						}
					}
				}
			}
			if (minAll > maxAll)
			{
				// No line information
				this.breakableLines = EMPTY_BOOLEAN_ARRAY;
				this.breakpoints = EMPTY_BOOLEAN_ARRAY;
			}
			else
			{
				if (minAll < 0)
				{
					// Line numbers can not be negative
					throw new InvalidOperationException(minAll.ToString());
				}
				int linesTop = maxAll + 1;
				this.breakableLines = new bool[linesTop];
				this.breakpoints = new bool[linesTop];
				for (int i_2 = 0; i_2 != N; ++i_2)
				{
					int[] lines = lineArrays[i_2];
					if (lines != null && lines.Length != 0)
					{
						for (int j = 0; j != lines.Length; ++j)
						{
							int line = lines[j];
							this.breakableLines[line] = true;
						}
					}
				}
			}
			this.functionSources = new FunctionSource[N];
			for (int i_3 = 0; i_3 != N; ++i_3)
			{
				string name = functions[i_3].GetFunctionName();
				if (name == null)
				{
					name = String.Empty;
				}
				this.functionSources[i_3] = new FunctionSource(this, firstLines[i_3], name);
			}
		}

		/// <summary>Returns the source text.</summary>
		/// <remarks>Returns the source text.</remarks>
		public string Source()
		{
			return source;
		}

		/// <summary>Returns the script's origin URL.</summary>
		/// <remarks>Returns the script's origin URL.</remarks>
		public string Url()
		{
			return url;
		}

		/// <summary>Returns the number of FunctionSource objects stored in this object.</summary>
		/// <remarks>Returns the number of FunctionSource objects stored in this object.</remarks>
		public int FunctionSourcesTop()
		{
			return functionSources.Length;
		}

		/// <summary>Returns the FunctionSource object with the given index.</summary>
		/// <remarks>Returns the FunctionSource object with the given index.</remarks>
		public FunctionSource GetFunctionSource(int i)
		{
			return functionSources[i];
		}

		/// <summary>
		/// Copies the breakpoints from the given SourceInfo object into this
		/// one.
		/// </summary>
		/// <remarks>
		/// Copies the breakpoints from the given SourceInfo object into this
		/// one.
		/// </remarks>
		internal void CopyBreakpointsFrom(SourceInfo old)
		{
			int end = old.breakpoints.Length;
			if (end > breakpoints.Length)
			{
				end = breakpoints.Length;
			}
			for (int line = 0; line != end; ++line)
			{
				if (old.breakpoints[line])
				{
					breakpoints[line] = true;
				}
			}
		}

		/// <summary>
		/// Returns whether the given line number can have a breakpoint set on
		/// it.
		/// </summary>
		/// <remarks>
		/// Returns whether the given line number can have a breakpoint set on
		/// it.
		/// </remarks>
		public bool BreakableLine(int line)
		{
			return (line < breakableLines.Length) && breakableLines[line];
		}

		/// <summary>Returns whether there is a breakpoint set on the given line.</summary>
		/// <remarks>Returns whether there is a breakpoint set on the given line.</remarks>
		public bool Breakpoint(int line)
		{
			if (!BreakableLine(line))
			{
				throw new ArgumentException(line.ToString());
			}
			return line < breakpoints.Length && breakpoints[line];
		}

		/// <summary>Sets or clears the breakpoint flag for the given line.</summary>
		/// <remarks>Sets or clears the breakpoint flag for the given line.</remarks>
		public bool Breakpoint(int line, bool value)
		{
			if (!BreakableLine(line))
			{
				throw new ArgumentException(line.ToString());
			}
			bool changed;
			lock (breakpoints)
			{
				if (breakpoints[line] != value)
				{
					breakpoints[line] = value;
					changed = true;
				}
				else
				{
					changed = false;
				}
			}
			return changed;
		}

		/// <summary>Removes all breakpoints from the script.</summary>
		/// <remarks>Removes all breakpoints from the script.</remarks>
		public void RemoveAllBreakpoints()
		{
			lock (breakpoints)
			{
				for (int line = 0; line != breakpoints.Length; ++line)
				{
					breakpoints[line] = false;
				}
			}
		}
	}

	/// <summary>Class to store information about a function.</summary>
	public sealed class FunctionSource
	{
		/// <summary>Creates a new FunctionSource.</summary>
		internal FunctionSource(SourceInfo sourceInfo, int firstLine, string name)
		{
			if (name == null)
				throw new ArgumentException();
			SourceInfo = sourceInfo;
			FirstLine = firstLine;
			Name = name;
		}

		/// <summary> Returns the SourceInfo object that describes the source of the function. </summary>
		public SourceInfo SourceInfo { get; private set; }

		/// <summary>Returns the line number of the first line of the function.</summary>
		public int FirstLine { get; private set; }

		/// <summary>Returns the name of the function.</summary>
		public string Name { get; private set; }
	}
}
