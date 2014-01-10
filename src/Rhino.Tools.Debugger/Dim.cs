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
using Rhino.Debug;
using Rhino.Tools.Debugger;
using Sharpen;

namespace Rhino.Tools.Debugger
{
	/// <summary>Dim or Debugger Implementation for Rhino.</summary>
	/// <remarks>Dim or Debugger Implementation for Rhino.</remarks>
	public class Dim
	{
		public const int STEP_OVER = 0;

		public const int STEP_INTO = 1;

		public const int STEP_OUT = 2;

		public const int GO = 3;

		public const int BREAK = 4;

		public const int EXIT = 5;

		private const int IPROXY_DEBUG = 0;

		private const int IPROXY_LISTEN = 1;

		private const int IPROXY_COMPILE_SCRIPT = 2;

		private const int IPROXY_EVAL_SCRIPT = 3;

		private const int IPROXY_STRING_IS_COMPILABLE = 4;

		private const int IPROXY_OBJECT_TO_STRING = 5;

		private const int IPROXY_OBJECT_PROPERTY = 6;

		private const int IPROXY_OBJECT_IDS = 7;

		/// <summary>Interface to the debugger GUI.</summary>
		/// <remarks>Interface to the debugger GUI.</remarks>
		private GuiCallback callback;

		/// <summary>Whether the debugger should break.</summary>
		/// <remarks>Whether the debugger should break.</remarks>
		private bool breakFlag;

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
		private volatile Dim.ContextData interruptedContextData;

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
		private object monitor = new object();

		/// <summary>
		/// Synchronization object used to wait for valid
		/// <see cref="interruptedContextData">interruptedContextData</see>
		/// .
		/// </summary>
		private object eventThreadMonitor = new object();

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
		private Dim.StackFrame evalFrame;

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
		private bool breakOnEnter;

		/// <summary>
		/// Whether the debugger should break when a script function is returned
		/// from.
		/// </summary>
		/// <remarks>
		/// Whether the debugger should break when a script function is returned
		/// from.
		/// </remarks>
		private bool breakOnReturn;

		/// <summary>Table mapping URLs to information about the script source.</summary>
		/// <remarks>Table mapping URLs to information about the script source.</remarks>
		private readonly IDictionary<string, Dim.SourceInfo> urlToSourceInfo = Sharpen.Collections.SynchronizedMap(new Dictionary<string, Dim.SourceInfo>());

		/// <summary>Table mapping function names to information about the function.</summary>
		/// <remarks>Table mapping function names to information about the function.</remarks>
		private readonly IDictionary<string, Dim.FunctionSource> functionNames = Sharpen.Collections.SynchronizedMap(new Dictionary<string, Dim.FunctionSource>());

		/// <summary>Table mapping functions to information about the function.</summary>
		/// <remarks>Table mapping functions to information about the function.</remarks>
		private readonly IDictionary<DebuggableScript, Dim.FunctionSource> functionToSource = Sharpen.Collections.SynchronizedMap(new Dictionary<DebuggableScript, Dim.FunctionSource>());

		/// <summary>
		/// ContextFactory.Listener instance attached to
		/// <see cref="contextFactory">contextFactory</see>
		/// .
		/// </summary>
		private Dim.DimIProxy listener;

		// Constants for instructing the debugger what action to perform
		// to end interruption.  Used by 'returnValue'.
		// Constants for the DimIProxy interface implementation class.
		/// <summary>Sets the GuiCallback object to use.</summary>
		/// <remarks>Sets the GuiCallback object to use.</remarks>
		public virtual void SetGuiCallback(GuiCallback callback)
		{
			this.callback = callback;
		}

		/// <summary>Tells the debugger to break at the next opportunity.</summary>
		/// <remarks>Tells the debugger to break at the next opportunity.</remarks>
		public virtual void SetBreak()
		{
			this.breakFlag = true;
		}

		/// <summary>Sets the ScopeProvider to be used.</summary>
		/// <remarks>Sets the ScopeProvider to be used.</remarks>
		public virtual void SetScopeProvider(ScopeProvider scopeProvider)
		{
			this.scopeProvider = scopeProvider;
		}

		/// <summary>Sets the ScopeProvider to be used.</summary>
		/// <remarks>Sets the ScopeProvider to be used.</remarks>
		public virtual void SetSourceProvider(SourceProvider sourceProvider)
		{
			this.sourceProvider = sourceProvider;
		}

		/// <summary>Switches context to the stack frame with the given index.</summary>
		/// <remarks>Switches context to the stack frame with the given index.</remarks>
		public virtual void ContextSwitch(int frameIndex)
		{
			this.frameIndex = frameIndex;
		}

		/// <summary>Sets whether the debugger should break on exceptions.</summary>
		/// <remarks>Sets whether the debugger should break on exceptions.</remarks>
		public virtual void SetBreakOnExceptions(bool breakOnExceptions)
		{
			this.breakOnExceptions = breakOnExceptions;
		}

		/// <summary>Sets whether the debugger should break on function entering.</summary>
		/// <remarks>Sets whether the debugger should break on function entering.</remarks>
		public virtual void SetBreakOnEnter(bool breakOnEnter)
		{
			this.breakOnEnter = breakOnEnter;
		}

		/// <summary>Sets whether the debugger should break on function return.</summary>
		/// <remarks>Sets whether the debugger should break on function return.</remarks>
		public virtual void SetBreakOnReturn(bool breakOnReturn)
		{
			this.breakOnReturn = breakOnReturn;
		}

		/// <summary>Attaches the debugger to the given ContextFactory.</summary>
		/// <remarks>Attaches the debugger to the given ContextFactory.</remarks>
		public virtual void AttachTo(ContextFactory factory)
		{
			Detach();
			this.contextFactory = factory;
			this.listener = new Dim.DimIProxy(this, IPROXY_LISTEN);
			factory.AddListener(this.listener);
		}

		/// <summary>Detaches the debugger from the current ContextFactory.</summary>
		/// <remarks>Detaches the debugger from the current ContextFactory.</remarks>
		public virtual void Detach()
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
		public virtual void Dispose()
		{
			Detach();
		}

		/// <summary>Returns the FunctionSource object for the given script or function.</summary>
		/// <remarks>Returns the FunctionSource object for the given script or function.</remarks>
		private Dim.FunctionSource GetFunctionSource(DebuggableScript fnOrScript)
		{
			Dim.FunctionSource fsource = FunctionSource(fnOrScript);
			if (fsource == null)
			{
				string url = GetNormalizedUrl(fnOrScript);
				Dim.SourceInfo si = SourceInfo(url);
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
				sourceUrl = Sharpen.Runtime.Substring(sourceUrl, 0, hash);
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
								string pathFromHome = Sharpen.Runtime.Substring(sourceUrl, 2);
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
		private void RegisterTopScript(DebuggableScript topScript, string source)
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
			Dim.SourceInfo sourceInfo = new Dim.SourceInfo(source, functions, url);
			lock (urlToSourceInfo)
			{
				Dim.SourceInfo old = urlToSourceInfo.Get(url);
				if (old != null)
				{
					sourceInfo.CopyBreakpointsFrom(old);
				}
				urlToSourceInfo [url] = sourceInfo;
				for (int i = 0; i != sourceInfo.FunctionSourcesTop(); ++i)
				{
					Dim.FunctionSource fsource = sourceInfo.FunctionSource(i);
					string name = fsource.Name();
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
					Dim.FunctionSource fsource = sourceInfo.FunctionSource(i);
					functionToSource [functions[i]] = fsource;
				}
			}
			callback.UpdateSourceText(sourceInfo);
		}

		/// <summary>Returns the FunctionSource object for the given function or script.</summary>
		/// <remarks>Returns the FunctionSource object for the given function or script.</remarks>
		private Dim.FunctionSource FunctionSource(DebuggableScript fnOrScript)
		{
			return functionToSource.Get(fnOrScript);
		}

		/// <summary>Returns an array of all function names.</summary>
		/// <remarks>Returns an array of all function names.</remarks>
		public virtual string[] FunctionNames()
		{
			lock (urlToSourceInfo)
			{
				return Sharpen.Collections.ToArray(functionNames.Keys, new string[functionNames.Count]);
			}
		}

		/// <summary>Returns the FunctionSource object for the function with the given name.</summary>
		/// <remarks>Returns the FunctionSource object for the function with the given name.</remarks>
		public virtual Dim.FunctionSource FunctionSourceByName(string functionName)
		{
			return functionNames.Get(functionName);
		}

		/// <summary>Returns the SourceInfo object for the given URL.</summary>
		/// <remarks>Returns the SourceInfo object for the given URL.</remarks>
		public virtual Dim.SourceInfo SourceInfo(string url)
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
						sb.Append(Sharpen.Runtime.Substring(url, 0, searchStart));
					}
					sb.Append(replace);
				}
				if (sb != null)
				{
					if (cursor != urlLength)
					{
						sb.Append(Sharpen.Runtime.Substring(url, cursor));
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
		public virtual void ClearAllBreakpoints()
		{
			foreach (Dim.SourceInfo si in urlToSourceInfo.Values)
			{
				si.RemoveAllBreakpoints();
			}
		}

		/// <summary>Called when a breakpoint has been hit.</summary>
		/// <remarks>Called when a breakpoint has been hit.</remarks>
		private void HandleBreakpointHit(Dim.StackFrame frame, Context cx)
		{
			breakFlag = false;
			Interrupted(cx, frame, null);
		}

		/// <summary>Called when a script exception has been thrown.</summary>
		/// <remarks>Called when a script exception has been thrown.</remarks>
		private void HandleExceptionThrown(Context cx, Exception ex, Dim.StackFrame frame)
		{
			if (breakOnExceptions)
			{
				Dim.ContextData cd = frame.ContextData();
				if (cd.lastProcessedException != ex)
				{
					Interrupted(cx, frame, ex);
					cd.lastProcessedException = ex;
				}
			}
		}

		/// <summary>Returns the current ContextData object.</summary>
		/// <remarks>Returns the current ContextData object.</remarks>
		public virtual Dim.ContextData CurrentContextData()
		{
			return interruptedContextData;
		}

		/// <summary>Sets the action to perform to end interruption.</summary>
		/// <remarks>Sets the action to perform to end interruption.</remarks>
		public virtual void SetReturnValue(int returnValue)
		{
			lock (monitor)
			{
				this.returnValue = returnValue;
				Sharpen.Runtime.Notify(monitor);
			}
		}

		/// <summary>Resumes execution of script.</summary>
		/// <remarks>Resumes execution of script.</remarks>
		public virtual void Go()
		{
			lock (monitor)
			{
				this.returnValue = GO;
				Sharpen.Runtime.NotifyAll(monitor);
			}
		}

		/// <summary>Evaluates the given script.</summary>
		/// <remarks>Evaluates the given script.</remarks>
		public virtual string Eval(string expr)
		{
			string result = "undefined";
			if (expr == null)
			{
				return result;
			}
			Dim.ContextData contextData = CurrentContextData();
			if (contextData == null || frameIndex >= contextData.FrameCount())
			{
				return result;
			}
			Dim.StackFrame frame = contextData.GetFrame(frameIndex);
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
		public virtual void CompileScript(string url, string text)
		{
			Dim.DimIProxy action = new Dim.DimIProxy(this, IPROXY_COMPILE_SCRIPT);
			action.url = url;
			action.text = text;
			action.WithContext();
		}

		/// <summary>Evaluates the given script.</summary>
		/// <remarks>Evaluates the given script.</remarks>
		public virtual void EvalScript(string url, string text)
		{
			Dim.DimIProxy action = new Dim.DimIProxy(this, IPROXY_EVAL_SCRIPT);
			action.url = url;
			action.text = text;
			action.WithContext();
		}

		/// <summary>Converts the given script object to a string.</summary>
		/// <remarks>Converts the given script object to a string.</remarks>
		public virtual string ObjectToString(object @object)
		{
			Dim.DimIProxy action = new Dim.DimIProxy(this, IPROXY_OBJECT_TO_STRING);
			action.@object = @object;
			action.WithContext();
			return action.stringResult;
		}

		/// <summary>Returns whether the given string is syntactically valid script.</summary>
		/// <remarks>Returns whether the given string is syntactically valid script.</remarks>
		public virtual bool StringIsCompilableUnit(string str)
		{
			Dim.DimIProxy action = new Dim.DimIProxy(this, IPROXY_STRING_IS_COMPILABLE);
			action.text = str;
			action.WithContext();
			return action.booleanResult;
		}

		/// <summary>Returns the value of a property on the given script object.</summary>
		/// <remarks>Returns the value of a property on the given script object.</remarks>
		public virtual object GetObjectProperty(object @object, object id)
		{
			Dim.DimIProxy action = new Dim.DimIProxy(this, IPROXY_OBJECT_PROPERTY);
			action.@object = @object;
			action.id = id;
			action.WithContext();
			return action.objectResult;
		}

		/// <summary>Returns an array of the property names on the given script object.</summary>
		/// <remarks>Returns an array of the property names on the given script object.</remarks>
		public virtual object[] GetObjectIds(object @object)
		{
			Dim.DimIProxy action = new Dim.DimIProxy(this, IPROXY_OBJECT_IDS);
			action.@object = @object;
			action.WithContext();
			return action.objectArrayResult;
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
							if (result == ScriptableObject.NOT_FOUND)
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
				if (result == ScriptableObject.NOT_FOUND)
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
		private void Interrupted(Context cx, Dim.StackFrame frame, Exception scriptException)
		{
			Dim.ContextData contextData = frame.ContextData();
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
		private static string Do_eval(Context cx, Dim.StackFrame frame, string expr)
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
			if (resultString == null)
			{
				resultString = "null";
			}
			return resultString;
		}

		/// <summary>
		/// Proxy class to implement debug interfaces without bloat of class
		/// files.
		/// </summary>
		/// <remarks>
		/// Proxy class to implement debug interfaces without bloat of class
		/// files.
		/// </remarks>
		private class DimIProxy : ContextAction, ContextFactory.Listener, Rhino.Debug.Debugger
		{
			/// <summary>The debugger.</summary>
			/// <remarks>The debugger.</remarks>
			private Dim dim;

			/// <summary>The interface implementation type.</summary>
			/// <remarks>
			/// The interface implementation type.  One of the IPROXY_* constants
			/// defined in
			/// <see cref="Dim">Dim</see>
			/// .
			/// </remarks>
			private int type;

			/// <summary>The URL origin of the script to compile or evaluate.</summary>
			/// <remarks>The URL origin of the script to compile or evaluate.</remarks>
			private string url;

			/// <summary>The text of the script to compile, evaluate or test for compilation.</summary>
			/// <remarks>The text of the script to compile, evaluate or test for compilation.</remarks>
			private string text;

			/// <summary>The object to convert, get a property from or enumerate.</summary>
			/// <remarks>The object to convert, get a property from or enumerate.</remarks>
			private object @object;

			/// <summary>
			/// The property to look up in
			/// <see cref="@object">@object</see>
			/// .
			/// </summary>
			private object id;

			/// <summary>The boolean result of the action.</summary>
			/// <remarks>The boolean result of the action.</remarks>
			private bool booleanResult;

			/// <summary>The String result of the action.</summary>
			/// <remarks>The String result of the action.</remarks>
			private string stringResult;

			/// <summary>The Object result of the action.</summary>
			/// <remarks>The Object result of the action.</remarks>
			private object objectResult;

			/// <summary>The Object[] result of the action.</summary>
			/// <remarks>The Object[] result of the action.</remarks>
			private object[] objectArrayResult;

			/// <summary>Creates a new DimIProxy.</summary>
			/// <remarks>Creates a new DimIProxy.</remarks>
			private DimIProxy(Dim dim, int type)
			{
				this.dim = dim;
				this.type = type;
			}

			// ContextAction
			/// <summary>
			/// Performs the action given by
			/// <see cref="type">type</see>
			/// .
			/// </summary>
			public virtual object Run(Context cx)
			{
				switch (type)
				{
					case IPROXY_COMPILE_SCRIPT:
					{
						cx.CompileString(text, url, 1, null);
						break;
					}

					case IPROXY_EVAL_SCRIPT:
					{
						Scriptable scope = null;
						if (dim.scopeProvider != null)
						{
							scope = dim.scopeProvider.GetScope();
						}
						if (scope == null)
						{
							scope = new ImporterTopLevel(cx);
						}
						cx.EvaluateString(scope, text, url, 1, null);
						break;
					}

					case IPROXY_STRING_IS_COMPILABLE:
					{
						booleanResult = cx.StringIsCompilableUnit(text);
						break;
					}

					case IPROXY_OBJECT_TO_STRING:
					{
						if (@object == Undefined.instance)
						{
							stringResult = "undefined";
						}
						else
						{
							if (@object == null)
							{
								stringResult = "null";
							}
							else
							{
								if (@object is NativeCall)
								{
									stringResult = "[object Call]";
								}
								else
								{
									stringResult = Context.ToString(@object);
								}
							}
						}
						break;
					}

					case IPROXY_OBJECT_PROPERTY:
					{
						objectResult = dim.GetObjectPropertyImpl(cx, @object, id);
						break;
					}

					case IPROXY_OBJECT_IDS:
					{
						objectArrayResult = dim.GetObjectIdsImpl(cx, @object);
						break;
					}

					default:
					{
						throw Kit.CodeBug();
					}
				}
				return null;
			}

			/// <summary>
			/// Performs the action given by
			/// <see cref="type">type</see>
			/// with the attached
			/// <see cref="Rhino.ContextFactory">Rhino.ContextFactory</see>
			/// .
			/// </summary>
			private void WithContext()
			{
				dim.contextFactory.Call(this);
			}

			// ContextFactory.Listener
			/// <summary>Called when a Context is created.</summary>
			/// <remarks>Called when a Context is created.</remarks>
			public virtual void ContextCreated(Context cx)
			{
				if (type != IPROXY_LISTEN)
				{
					Kit.CodeBug();
				}
				Dim.ContextData contextData = new Dim.ContextData();
				Rhino.Debug.Debugger debugger = new Dim.DimIProxy(dim, IPROXY_DEBUG);
				cx.SetDebugger(debugger, contextData);
				cx.SetGeneratingDebug(true);
				cx.SetOptimizationLevel(-1);
			}

			/// <summary>Called when a Context is destroyed.</summary>
			/// <remarks>Called when a Context is destroyed.</remarks>
			public virtual void ContextReleased(Context cx)
			{
				if (type != IPROXY_LISTEN)
				{
					Kit.CodeBug();
				}
			}

			// Debugger
			/// <summary>Returns a StackFrame for the given function or script.</summary>
			/// <remarks>Returns a StackFrame for the given function or script.</remarks>
			public virtual DebugFrame GetFrame(Context cx, DebuggableScript fnOrScript)
			{
				if (type != IPROXY_DEBUG)
				{
					Kit.CodeBug();
				}
				Dim.FunctionSource item = dim.GetFunctionSource(fnOrScript);
				if (item == null)
				{
					// Can not debug if source is not available
					return null;
				}
				return new Dim.StackFrame(cx, dim, item);
			}

			/// <summary>Called when compilation is finished.</summary>
			/// <remarks>Called when compilation is finished.</remarks>
			public virtual void HandleCompilationDone(Context cx, DebuggableScript fnOrScript, string source)
			{
				if (type != IPROXY_DEBUG)
				{
					Kit.CodeBug();
				}
				if (!fnOrScript.IsTopLevel())
				{
					return;
				}
				dim.RegisterTopScript(fnOrScript, source);
			}
		}

		/// <summary>Class to store information about a stack.</summary>
		/// <remarks>Class to store information about a stack.</remarks>
		public class ContextData
		{
			/// <summary>The stack frames.</summary>
			/// <remarks>The stack frames.</remarks>
			private ObjArray frameStack = new ObjArray();

			/// <summary>Whether the debugger should break at the next line in this context.</summary>
			/// <remarks>Whether the debugger should break at the next line in this context.</remarks>
			private bool breakNextLine;

			/// <summary>The frame depth the debugger should stop at.</summary>
			/// <remarks>
			/// The frame depth the debugger should stop at.  Used to implement
			/// "step over" and "step out".
			/// </remarks>
			private int stopAtFrameDepth = -1;

			/// <summary>Whether this context is in the event thread.</summary>
			/// <remarks>Whether this context is in the event thread.</remarks>
			private bool eventThreadFlag;

			/// <summary>The last exception that was processed.</summary>
			/// <remarks>The last exception that was processed.</remarks>
			private Exception lastProcessedException;

			/// <summary>Returns the ContextData for the given Context.</summary>
			/// <remarks>Returns the ContextData for the given Context.</remarks>
			public static Dim.ContextData Get(Context cx)
			{
				return (Dim.ContextData)cx.GetDebuggerContextData();
			}

			/// <summary>Returns the number of stack frames.</summary>
			/// <remarks>Returns the number of stack frames.</remarks>
			public virtual int FrameCount()
			{
				return frameStack.Size();
			}

			/// <summary>Returns the stack frame with the given index.</summary>
			/// <remarks>Returns the stack frame with the given index.</remarks>
			public virtual Dim.StackFrame GetFrame(int frameNumber)
			{
				int num = frameStack.Size() - frameNumber - 1;
				return (Dim.StackFrame)frameStack.Get(num);
			}

			/// <summary>Pushes a stack frame on to the stack.</summary>
			/// <remarks>Pushes a stack frame on to the stack.</remarks>
			private void PushFrame(Dim.StackFrame frame)
			{
				frameStack.Push(frame);
			}

			/// <summary>Pops a stack frame from the stack.</summary>
			/// <remarks>Pops a stack frame from the stack.</remarks>
			private void PopFrame()
			{
				frameStack.Pop();
			}
		}

		/// <summary>Object to represent one stack frame.</summary>
		/// <remarks>Object to represent one stack frame.</remarks>
		public class StackFrame : DebugFrame
		{
			/// <summary>The debugger.</summary>
			/// <remarks>The debugger.</remarks>
			private Dim dim;

			/// <summary>The ContextData for the Context being debugged.</summary>
			/// <remarks>The ContextData for the Context being debugged.</remarks>
			private Dim.ContextData contextData;

			/// <summary>The scope.</summary>
			/// <remarks>The scope.</remarks>
			private Scriptable scope;

			/// <summary>The 'this' object.</summary>
			/// <remarks>The 'this' object.</remarks>
			private Scriptable thisObj;

			/// <summary>Information about the function.</summary>
			/// <remarks>Information about the function.</remarks>
			private Dim.FunctionSource fsource;

			/// <summary>Array of breakpoint state for each source line.</summary>
			/// <remarks>Array of breakpoint state for each source line.</remarks>
			private bool[] breakpoints;

			/// <summary>Current line number.</summary>
			/// <remarks>Current line number.</remarks>
			private int lineNumber;

			/// <summary>Creates a new StackFrame.</summary>
			/// <remarks>Creates a new StackFrame.</remarks>
			private StackFrame(Context cx, Dim dim, Dim.FunctionSource fsource)
			{
				this.dim = dim;
				this.contextData = Dim.ContextData.Get(cx);
				this.fsource = fsource;
				this.breakpoints = fsource.SourceInfo().breakpoints;
				this.lineNumber = fsource.FirstLine();
			}

			/// <summary>Called when the stack frame is entered.</summary>
			/// <remarks>Called when the stack frame is entered.</remarks>
			public virtual void OnEnter(Context cx, Scriptable scope, Scriptable thisObj, object[] args)
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
			public virtual void OnLineChange(Context cx, int lineno)
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
			public virtual void OnExceptionThrown(Context cx, Exception exception)
			{
				dim.HandleExceptionThrown(cx, exception, this);
			}

			/// <summary>Called when the stack frame has been left.</summary>
			/// <remarks>Called when the stack frame has been left.</remarks>
			public virtual void OnExit(Context cx, bool byThrow, object resultOrException)
			{
				if (dim.breakOnReturn && !byThrow)
				{
					dim.HandleBreakpointHit(this, cx);
				}
				contextData.PopFrame();
			}

			/// <summary>Called when a 'debugger' statement is executed.</summary>
			/// <remarks>Called when a 'debugger' statement is executed.</remarks>
			public virtual void OnDebuggerStatement(Context cx)
			{
				dim.HandleBreakpointHit(this, cx);
			}

			/// <summary>Returns the SourceInfo object for the function.</summary>
			/// <remarks>Returns the SourceInfo object for the function.</remarks>
			public virtual Dim.SourceInfo SourceInfo()
			{
				return fsource.SourceInfo();
			}

			/// <summary>Returns the ContextData object for the Context.</summary>
			/// <remarks>Returns the ContextData object for the Context.</remarks>
			public virtual Dim.ContextData ContextData()
			{
				return contextData;
			}

			/// <summary>Returns the scope object for this frame.</summary>
			/// <remarks>Returns the scope object for this frame.</remarks>
			public virtual object Scope()
			{
				return scope;
			}

			/// <summary>Returns the 'this' object for this frame.</summary>
			/// <remarks>Returns the 'this' object for this frame.</remarks>
			public virtual object ThisObj()
			{
				return thisObj;
			}

			/// <summary>Returns the source URL.</summary>
			/// <remarks>Returns the source URL.</remarks>
			public virtual string GetUrl()
			{
				return fsource.SourceInfo().Url();
			}

			/// <summary>Returns the current line number.</summary>
			/// <remarks>Returns the current line number.</remarks>
			public virtual int GetLineNumber()
			{
				return lineNumber;
			}

			/// <summary>Returns the current function name.</summary>
			/// <remarks>Returns the current function name.</remarks>
			public virtual string GetFunctionName()
			{
				return fsource.Name();
			}
		}

		/// <summary>Class to store information about a function.</summary>
		/// <remarks>Class to store information about a function.</remarks>
		public class FunctionSource
		{
			/// <summary>Information about the source of the function.</summary>
			/// <remarks>Information about the source of the function.</remarks>
			private Dim.SourceInfo sourceInfo;

			/// <summary>Line number of the first line of the function.</summary>
			/// <remarks>Line number of the first line of the function.</remarks>
			private int firstLine;

			/// <summary>The function name.</summary>
			/// <remarks>The function name.</remarks>
			private string name;

			/// <summary>Creates a new FunctionSource.</summary>
			/// <remarks>Creates a new FunctionSource.</remarks>
			private FunctionSource(Dim.SourceInfo sourceInfo, int firstLine, string name)
			{
				if (name == null)
				{
					throw new ArgumentException();
				}
				this.sourceInfo = sourceInfo;
				this.firstLine = firstLine;
				this.name = name;
			}

			/// <summary>
			/// Returns the SourceInfo object that describes the source of the
			/// function.
			/// </summary>
			/// <remarks>
			/// Returns the SourceInfo object that describes the source of the
			/// function.
			/// </remarks>
			public virtual Dim.SourceInfo SourceInfo()
			{
				return sourceInfo;
			}

			/// <summary>Returns the line number of the first line of the function.</summary>
			/// <remarks>Returns the line number of the first line of the function.</remarks>
			public virtual int FirstLine()
			{
				return firstLine;
			}

			/// <summary>Returns the name of the function.</summary>
			/// <remarks>Returns the name of the function.</remarks>
			public virtual string Name()
			{
				return name;
			}
		}

		/// <summary>Class to store information about a script source.</summary>
		/// <remarks>Class to store information about a script source.</remarks>
		public class SourceInfo
		{
			/// <summary>An empty array of booleans.</summary>
			/// <remarks>An empty array of booleans.</remarks>
			private static readonly bool[] EMPTY_BOOLEAN_ARRAY = new bool[0];

			/// <summary>The script.</summary>
			/// <remarks>The script.</remarks>
			private string source;

			/// <summary>The URL of the script.</summary>
			/// <remarks>The URL of the script.</remarks>
			private string url;

			/// <summary>Array indicating which lines can have breakpoints set.</summary>
			/// <remarks>Array indicating which lines can have breakpoints set.</remarks>
			private bool[] breakableLines;

			/// <summary>Array indicating whether a breakpoint is set on the line.</summary>
			/// <remarks>Array indicating whether a breakpoint is set on the line.</remarks>
			private bool[] breakpoints;

			/// <summary>Array of FunctionSource objects for the functions in the script.</summary>
			/// <remarks>Array of FunctionSource objects for the functions in the script.</remarks>
			private Dim.FunctionSource[] functionSources;

			/// <summary>Creates a new SourceInfo object.</summary>
			/// <remarks>Creates a new SourceInfo object.</remarks>
			private SourceInfo(string source, DebuggableScript[] functions, string normilizedUrl)
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
						int min;
						int max;
						min = max = lines[0];
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
				this.functionSources = new Dim.FunctionSource[N];
				for (int i_3 = 0; i_3 != N; ++i_3)
				{
					string name = functions[i_3].GetFunctionName();
					if (name == null)
					{
						name = string.Empty;
					}
					this.functionSources[i_3] = new Dim.FunctionSource(this, firstLines[i_3], name);
				}
			}

			/// <summary>Returns the source text.</summary>
			/// <remarks>Returns the source text.</remarks>
			public virtual string Source()
			{
				return this.source;
			}

			/// <summary>Returns the script's origin URL.</summary>
			/// <remarks>Returns the script's origin URL.</remarks>
			public virtual string Url()
			{
				return this.url;
			}

			/// <summary>Returns the number of FunctionSource objects stored in this object.</summary>
			/// <remarks>Returns the number of FunctionSource objects stored in this object.</remarks>
			public virtual int FunctionSourcesTop()
			{
				return functionSources.Length;
			}

			/// <summary>Returns the FunctionSource object with the given index.</summary>
			/// <remarks>Returns the FunctionSource object with the given index.</remarks>
			public virtual Dim.FunctionSource FunctionSource(int i)
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
			private void CopyBreakpointsFrom(Dim.SourceInfo old)
			{
				int end = old.breakpoints.Length;
				if (end > this.breakpoints.Length)
				{
					end = this.breakpoints.Length;
				}
				for (int line = 0; line != end; ++line)
				{
					if (old.breakpoints[line])
					{
						this.breakpoints[line] = true;
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
			public virtual bool BreakableLine(int line)
			{
				return (line < this.breakableLines.Length) && this.breakableLines[line];
			}

			/// <summary>Returns whether there is a breakpoint set on the given line.</summary>
			/// <remarks>Returns whether there is a breakpoint set on the given line.</remarks>
			public virtual bool Breakpoint(int line)
			{
				if (!BreakableLine(line))
				{
					throw new ArgumentException(line.ToString());
				}
				return line < this.breakpoints.Length && this.breakpoints[line];
			}

			/// <summary>Sets or clears the breakpoint flag for the given line.</summary>
			/// <remarks>Sets or clears the breakpoint flag for the given line.</remarks>
			public virtual bool Breakpoint(int line, bool value)
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
			public virtual void RemoveAllBreakpoints()
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
	}
}
