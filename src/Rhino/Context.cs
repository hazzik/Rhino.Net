/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security;
using Rhino.Annotations;
using Rhino.Ast;
using Rhino.Debug;
using Rhino.Optimizer;
using Rhino.Utils;
using Rhino.Xml;
using Sharpen;

namespace Rhino
{
	/// <summary>This class represents the runtime context of an executing script.</summary>
	/// <remarks>
	/// This class represents the runtime context of an executing script.
	/// Before executing a script, an instance of Context must be created
	/// and associated with the thread that will be executing the script.
	/// The Context will be used to store information about the executing
	/// of the script such as the call stack. Contexts are associated with
	/// the current thread  using the
	/// <see cref="Call(ContextAction)">Call(ContextAction)</see>
	/// or
	/// <see cref="Enter()">Enter()</see>
	/// methods.<p>
	/// Different forms of script execution are supported. Scripts may be
	/// evaluated from the source directly, or first compiled and then later
	/// executed. Interactive execution is also supported.<p>
	/// Some aspects of script execution, such as type conversions and
	/// object creation, may be accessed directly through methods of
	/// Context.
	/// </remarks>
	/// <seealso cref="Scriptable">Scriptable</seealso>
	/// <author>Norris Boyd</author>
	/// <author>Brendan Eich</author>
	public class Context
	{
		public const string languageVersionProperty = "language version";

		public const string errorReporterProperty = "error reporter";

		/// <summary>Convenient value to use as zero-length array of objects.</summary>
		/// <remarks>Convenient value to use as zero-length array of objects.</remarks>
		public static readonly object[] emptyArgs = ScriptRuntime.emptyArgs;

		/// <summary>Creates a new context.</summary>
		/// <remarks>
		/// Creates a new context. Provided as a preferred super constructor for
		/// subclasses in place of the deprecated default public constructor.
		/// </remarks>
		/// <param name="factory">
		/// the context factory associated with this context (most
		/// likely, the one that created the context). Can not be null. The context
		/// features are inherited from the factory, and the context will also
		/// otherwise use its factory's services.
		/// </param>
		/// <exception cref="System.ArgumentException">if factory parameter is null.</exception>
		protected internal Context(ContextFactory factory)
		{
			// API class
			if (factory == null)
			{
				throw new ArgumentException("factory == null");
			}
			this.factory = factory;
			version = LanguageVersion.VERSION_DEFAULT;
			optimizationLevel = codegenClass != null ? 0 : -1;
			maximumInterpreterStackDepth = int.MaxValue;
		}

		/// <summary>Get the current Context.</summary>
		/// <remarks>
		/// Get the current Context.
		/// The current Context is per-thread; this method looks up
		/// the Context associated with the current thread. <p>
		/// </remarks>
		/// <returns>
		/// the Context associated with the current thread, or
		/// null if no context is associated with the current
		/// thread.
		/// </returns>
		/// <seealso cref="ContextFactory.EnterContext()">ContextFactory.EnterContext()</seealso>
		/// <seealso cref="ContextFactory.Call(ContextAction)">ContextFactory.Call(ContextAction)</seealso>
		public static Context GetCurrentContext()
		{
			return VMBridge.Context;
		}

		/// <summary>
		/// Same as calling
		/// <see cref="ContextFactory.EnterContext()">ContextFactory.EnterContext()</see>
		/// on the global
		/// ContextFactory instance.
		/// </summary>
		/// <returns>a Context associated with the current thread</returns>
		/// <seealso cref="GetCurrentContext()">GetCurrentContext()</seealso>
		/// <seealso cref="Exit()">Exit()</seealso>
		/// <seealso cref="Call(ContextAction)">Call(ContextAction)</seealso>
		public static Rhino.Context Enter()
		{
			return Enter(null);
		}

		/// <summary>
		/// Get a Context associated with the current thread, using
		/// the given Context if need be.
		/// </summary>
		/// <remarks>
		/// Get a Context associated with the current thread, using
		/// the given Context if need be.
		/// <p>
		/// The same as <code>enter()</code> except that <code>cx</code>
		/// is associated with the current thread and returned if
		/// the current thread has no associated context and <code>cx</code>
		/// is not associated with any other thread.
		/// </remarks>
		/// <param name="cx">a Context to associate with the thread if possible</param>
		/// <returns>a Context associated with the current thread</returns>
		/// <seealso cref="ContextFactory.EnterContext(Context)">ContextFactory.EnterContext(Context)</seealso>
		/// <seealso cref="ContextFactory.Call(ContextAction)">ContextFactory.Call(ContextAction)</seealso>
		[System.ObsoleteAttribute(@"use ContextFactory.EnterContext(Context) instead as this method relies on usage of a static singleton ""global"" ContextFactory.")]
		public static Rhino.Context Enter(Rhino.Context cx)
		{
			return Enter(cx, ContextFactory.GetGlobal());
		}

		internal static Rhino.Context Enter(Rhino.Context cx, ContextFactory factory)
		{
			Rhino.Context old = VMBridge.Context;
			if (old != null)
			{
				cx = old;
			}
			else
			{
				if (cx == null)
				{
					cx = factory.MakeContext();
					if (cx.enterCount != 0)
					{
						throw new InvalidOperationException("factory.makeContext() returned Context instance already associated with some thread");
					}
					factory.OnContextCreated(cx);
					if (factory.IsSealed() && !cx.IsSealed())
					{
						cx.Seal(null);
					}
				}
				else
				{
					if (cx.enterCount != 0)
					{
						throw new InvalidOperationException("can not use Context instance already associated with some thread");
					}
				}
				VMBridge.Context = cx;
			}
			++cx.enterCount;
			return cx;
		}

		/// <summary>Exit a block of code requiring a Context.</summary>
		/// <remarks>
		/// Exit a block of code requiring a Context.
		/// Calling <code>exit()</code> will remove the association between
		/// the current thread and a Context if the prior call to
		/// <see cref="ContextFactory.EnterContext()">ContextFactory.EnterContext()</see>
		/// on this thread newly associated a
		/// Context with this thread. Once the current thread no longer has an
		/// associated Context, it cannot be used to execute JavaScript until it is
		/// again associated with a Context.
		/// </remarks>
		/// <seealso cref="ContextFactory.EnterContext()">ContextFactory.EnterContext()</seealso>
		//TODO: Use as dispose.
		public static void Exit()
		{
			Rhino.Context cx = VMBridge.Context;
			if (cx == null)
			{
				throw new InvalidOperationException("Calling Context.exit without previous Context.enter");
			}
			if (cx.enterCount < 1)
			{
				Kit.CodeBug();
			}
			if (--cx.enterCount == 0)
			{
				VMBridge.Context = null;
				cx.factory.OnContextReleased(cx);
			}
		}

		/// <summary>
		/// Call
		/// <see cref="Callable.Call(Context, Scriptable, Scriptable, object[])">Callable.Call(Context, Scriptable, Scriptable, object[])</see>
		/// using the Context instance associated with the current thread.
		/// If no Context is associated with the thread, then
		/// <see cref="ContextFactory.MakeContext()">ContextFactory.MakeContext()</see>
		/// will be called to construct
		/// new Context instance. The instance will be temporary associated
		/// with the thread during call to
		/// <see cref="ContextAction.Run(Context)">ContextAction.Run(Context)</see>
		/// .
		/// <p>
		/// It is allowed but not advisable to use null for <tt>factory</tt>
		/// argument in which case the global static singleton ContextFactory
		/// instance will be used to create new context instances.
		/// </summary>
		/// <seealso cref="ContextFactory.Call(ContextAction)">ContextFactory.Call(ContextAction)</seealso>
		public static object Call(ContextFactory factory, Callable callable, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (factory == null)
			{
				factory = ContextFactory.GetGlobal();
			}
			return Call(factory, cx => callable.Call(cx, scope, thisObj, args));
		}

		/// <summary>
		/// The method implements
		/// <see cref="ContextFactory.Call(ContextAction)">ContextFactory.Call(ContextAction)</see>
		/// logic.
		/// </summary>
		internal static object Call(ContextFactory factory, ContextAction action)
		{
			Rhino.Context cx = Enter(null, factory);
			try
			{
				return action(cx);
			}
			finally
			{
				Exit();
			}
		}

		/// <summary>
		/// Return
		/// <see cref="ContextFactory">ContextFactory</see>
		/// instance used to create this Context.
		/// </summary>
		public ContextFactory GetFactory()
		{
			return factory;
		}

		/// <summary>Checks if this is a sealed Context.</summary>
		/// <remarks>
		/// Checks if this is a sealed Context. A sealed Context instance does not
		/// allow to modify any of its properties and will throw an exception
		/// on any such attempt.
		/// </remarks>
		/// <seealso cref="Seal(object)">Seal(object)</seealso>
		public bool IsSealed()
		{
			return @sealed;
		}

		/// <summary>
		/// Seal this Context object so any attempt to modify any of its properties
		/// including calling
		/// <see cref="Enter()">Enter()</see>
		/// and
		/// <see cref="Exit()">Exit()</see>
		/// methods will
		/// throw an exception.
		/// <p>
		/// If <tt>sealKey</tt> is not null, calling
		/// <see cref="Unseal(object)">Unseal(object)</see>
		/// with the same key unseals
		/// the object. If <tt>sealKey</tt> is null, unsealing is no longer possible.
		/// </summary>
		/// <seealso cref="IsSealed()">IsSealed()</seealso>
		/// <seealso cref="Unseal(object)">Unseal(object)</seealso>
		public void Seal(object sealKey)
		{
			if (@sealed)
			{
				OnSealedMutation();
			}
			@sealed = true;
			this.sealKey = sealKey;
		}

		/// <summary>Unseal previously sealed Context object.</summary>
		/// <remarks>
		/// Unseal previously sealed Context object.
		/// The <tt>sealKey</tt> argument should not be null and should match
		/// <tt>sealKey</tt> suplied with the last call to
		/// <see cref="Seal(object)">Seal(object)</see>
		/// or an exception will be thrown.
		/// </remarks>
		/// <seealso cref="IsSealed()">IsSealed()</seealso>
		/// <seealso cref="Seal(object)">Seal(object)</seealso>
		public void Unseal(object sealKey)
		{
			if (sealKey == null)
			{
				throw new ArgumentException();
			}
			if (this.sealKey != sealKey)
			{
				throw new ArgumentException();
			}
			if (!@sealed)
			{
				throw new InvalidOperationException();
			}
			@sealed = false;
			this.sealKey = null;
		}

		internal static void OnSealedMutation()
		{
			throw new InvalidOperationException();
		}

		/// <summary>Get the current language version.</summary>
		/// <remarks>
		/// Get the current language version.
		/// <p>
		/// The language version number affects JavaScript semantics as detailed
		/// in the overview documentation.
		/// </remarks>
		/// <returns>an integer that is one of VERSION_1_0, VERSION_1_1, etc.</returns>
		public LanguageVersion GetLanguageVersion()
		{
			return version;
		}

		/// <summary>Set the language version.</summary>
		/// <remarks>
		/// Set the language version.
		/// <p>
		/// Setting the language version will affect functions and scripts compiled
		/// subsequently. See the overview documentation for version-specific
		/// behavior.
		/// </remarks>
		/// <param name="version">the version as specified by VERSION_1_0, VERSION_1_1, etc.</param>
		public virtual void SetLanguageVersion(LanguageVersion version)
		{
			if (@sealed)
			{
				OnSealedMutation();
			}
			CheckLanguageVersion(version);
			object listeners = propertyListeners;
			if (listeners != null && version != this.version)
			{
				FirePropertyChangeImpl(listeners, languageVersionProperty, this.version, version);
			}
			this.version = version;
		}

		public static bool IsValidLanguageVersion(LanguageVersion version)
		{
			switch (version)
			{
				case LanguageVersion.VERSION_DEFAULT:
				case LanguageVersion.VERSION_1_0:
				case LanguageVersion.VERSION_1_1:
				case LanguageVersion.VERSION_1_2:
				case LanguageVersion.VERSION_1_3:
				case LanguageVersion.VERSION_1_4:
				case LanguageVersion.VERSION_1_5:
				case LanguageVersion.VERSION_1_6:
				case LanguageVersion.VERSION_1_7:
				case LanguageVersion.VERSION_1_8:
				{
					return true;
				}
			}
			return false;
		}

		public static void CheckLanguageVersion(LanguageVersion version)
		{
			if (IsValidLanguageVersion(version))
			{
				return;
			}
			throw new ArgumentException("Bad language version: " + version);
		}

		/// <summary>Get the implementation version.</summary>
		/// <remarks>
		/// Get the implementation version.
		/// <p>
		/// The implementation version is of the form
		/// <pre>
		/// "<i>name langVer</i> <code>release</code> <i>relNum date</i>"
		/// </pre>
		/// where <i>name</i> is the name of the product, <i>langVer</i> is
		/// the language version, <i>relNum</i> is the release number, and
		/// <i>date</i> is the release date for that specific
		/// release in the form "yyyy mm dd".
		/// </remarks>
		/// <returns>
		/// a string that encodes the product, language version, release
		/// number, and date.
		/// </returns>
		public string GetImplementationVersion()
		{
			// XXX Probably it would be better to embed this directly into source
			// with special build preprocessing but that would require some ant
			// tweaking and then replacing token in resource files was simpler
			if (implementationVersion == null)
			{
				implementationVersion = ScriptRuntime.GetMessage0("implementation.version");
			}
			return implementationVersion;
		}

		/// <summary>Get the current error reporter.</summary>
		/// <remarks>Get the current error reporter.</remarks>
		/// <seealso cref="ErrorReporter">ErrorReporter</seealso>
		public ErrorReporter GetErrorReporter()
		{
			if (errorReporter == null)
			{
				return DefaultErrorReporter.instance;
			}
			return errorReporter;
		}

		/// <summary>Change the current error reporter.</summary>
		/// <remarks>Change the current error reporter.</remarks>
		/// <returns>the previous error reporter</returns>
		/// <seealso cref="ErrorReporter">ErrorReporter</seealso>
		public ErrorReporter SetErrorReporter(ErrorReporter reporter)
		{
			if (@sealed)
			{
				OnSealedMutation();
			}
			if (reporter == null)
			{
				throw new ArgumentException();
			}
			ErrorReporter old = GetErrorReporter();
			if (reporter == old)
			{
				return old;
			}
			object listeners = propertyListeners;
			if (listeners != null)
			{
				FirePropertyChangeImpl(listeners, errorReporterProperty, old, reporter);
			}
			this.errorReporter = reporter;
			return old;
		}

		/// <summary>Get the current locale.</summary>
		/// <remarks>
		/// Get the current locale.  Returns the default locale if none has
		/// been set.
		/// </remarks>
		/// <seealso cref="System.Globalization.CultureInfo">System.Globalization.CultureInfo</seealso>
		public CultureInfo GetLocale()
		{
			if (locale == null)
			{
				locale = CultureInfo.CurrentCulture;
			}
			return locale;
		}

		/// <summary>Set the current locale.</summary>
		/// <remarks>Set the current locale.</remarks>
		/// <seealso cref="System.Globalization.CultureInfo">System.Globalization.CultureInfo</seealso>
		public CultureInfo SetLocale(CultureInfo loc)
		{
			if (@sealed)
			{
				OnSealedMutation();
			}
			CultureInfo result = locale;
			locale = loc;
			return result;
		}

		/// <summary>
		/// Register an object to receive notifications when a bound property
		/// has changed
		/// </summary>
		/// <seealso cref="Java.Beans.PropertyChangeEvent">Java.Beans.PropertyChangeEvent</seealso>
		/// <seealso cref="RemovePropertyChangeListener(Java.Beans.PropertyChangeListener)">RemovePropertyChangeListener(Java.Beans.PropertyChangeListener)</seealso>
		/// <param name="l">the listener</param>
		public void AddPropertyChangeListener(PropertyChangeListener l)
		{
			if (@sealed)
			{
				OnSealedMutation();
			}
			propertyListeners = Kit.AddListener(propertyListeners, l);
		}

		/// <summary>
		/// Remove an object from the list of objects registered to receive
		/// notification of changes to a bounded property
		/// </summary>
		/// <seealso cref="Java.Beans.PropertyChangeEvent">Java.Beans.PropertyChangeEvent</seealso>
		/// <seealso cref="AddPropertyChangeListener(Java.Beans.PropertyChangeListener)">AddPropertyChangeListener(Java.Beans.PropertyChangeListener)</seealso>
		/// <param name="l">the listener</param>
		public void RemovePropertyChangeListener(PropertyChangeListener l)
		{
			if (@sealed)
			{
				OnSealedMutation();
			}
			propertyListeners = Kit.RemoveListener(propertyListeners, l);
		}

		/// <summary>Notify any registered listeners that a bounded property has changed</summary>
		/// <seealso cref="AddPropertyChangeListener(Java.Beans.PropertyChangeListener)">AddPropertyChangeListener(Java.Beans.PropertyChangeListener)</seealso>
		/// <seealso cref="RemovePropertyChangeListener(Java.Beans.PropertyChangeListener)">RemovePropertyChangeListener(Java.Beans.PropertyChangeListener)</seealso>
		/// <seealso cref="Java.Beans.PropertyChangeListener">Java.Beans.PropertyChangeListener</seealso>
		/// <seealso cref="Java.Beans.PropertyChangeEvent">Java.Beans.PropertyChangeEvent</seealso>
		/// <param name="property">the bound property</param>
		/// <param name="oldValue">the old value</param>
		/// <param name="newValue">the new value</param>
		internal void FirePropertyChange(string property, object oldValue, object newValue)
		{
			object listeners = propertyListeners;
			if (listeners != null)
			{
				FirePropertyChangeImpl(listeners, property, oldValue, newValue);
			}
		}

		private void FirePropertyChangeImpl(object listeners, string property, object oldValue, object newValue)
		{
			for (int i = 0; ; ++i)
			{
				object l = Kit.GetListener(listeners, i);
				if (l == null)
				{
					break;
				}
				var pcl = l as PropertyChangeListener;
				if (pcl != null)
				{
					pcl.PropertyChange(new PropertyChangeEvent(this, property, oldValue, newValue));
				}
			}
		}

		/// <summary>Report a warning using the error reporter for the current thread.</summary>
		/// <remarks>Report a warning using the error reporter for the current thread.</remarks>
		/// <param name="message">the warning message to report</param>
		/// <param name="sourceName">a string describing the source, such as a filename</param>
		/// <param name="lineno">the starting line number</param>
		/// <param name="lineSource">the text of the line (may be null)</param>
		/// <param name="lineOffset">the offset into lineSource where problem was detected</param>
		/// <seealso cref="ErrorReporter">ErrorReporter</seealso>
		public static void ReportWarning(string message, string sourceName, int lineno, string lineSource, int lineOffset)
		{
			Rhino.Context cx = Rhino.Context.GetContext();
			if (cx.HasFeature(LanguageFeatures.WarningAsError))
			{
				ReportError(message, sourceName, lineno, lineSource, lineOffset);
			}
			else
			{
				cx.GetErrorReporter().Warning(message, sourceName, lineno, lineSource, lineOffset);
			}
		}

		/// <summary>Report a warning using the error reporter for the current thread.</summary>
		/// <remarks>Report a warning using the error reporter for the current thread.</remarks>
		/// <param name="message">the warning message to report</param>
		/// <seealso cref="ErrorReporter">ErrorReporter</seealso>
		public static void ReportWarning(string message)
		{
			int[] linep = new int[] { 0 };
			string filename = GetSourcePositionFromStack(linep);
			Rhino.Context.ReportWarning(message, filename, linep[0], null, 0);
		}

		public static void ReportWarning(string message, Exception t)
		{
			int[] linep = new int[] { 0 };
			string filename = GetSourcePositionFromStack(linep);
			TextWriter sw = new StringWriter();
			sw.WriteLine(message);
			sw.WriteLine(t);
			sw.Flush();
			Rhino.Context.ReportWarning(sw.ToString(), filename, linep[0], null, 0);
		}

		/// <summary>Report an error using the error reporter for the current thread.</summary>
		/// <remarks>Report an error using the error reporter for the current thread.</remarks>
		/// <param name="message">the error message to report</param>
		/// <param name="sourceName">a string describing the source, such as a filename</param>
		/// <param name="lineno">the starting line number</param>
		/// <param name="lineSource">the text of the line (may be null)</param>
		/// <param name="lineOffset">the offset into lineSource where problem was detected</param>
		/// <seealso cref="ErrorReporter">ErrorReporter</seealso>
		public static void ReportError(string message, string sourceName, int lineno, string lineSource, int lineOffset)
		{
			Rhino.Context cx = GetCurrentContext();
			if (cx != null)
			{
				cx.GetErrorReporter().Error(message, sourceName, lineno, lineSource, lineOffset);
			}
			else
			{
				throw new EvaluatorException(message, sourceName, lineno, lineSource, lineOffset);
			}
		}

		/// <summary>Report an error using the error reporter for the current thread.</summary>
		/// <remarks>Report an error using the error reporter for the current thread.</remarks>
		/// <param name="message">the error message to report</param>
		/// <seealso cref="ErrorReporter">ErrorReporter</seealso>
		public static void ReportError(string message)
		{
			int[] linep = new int[] { 0 };
			string filename = GetSourcePositionFromStack(linep);
			Rhino.Context.ReportError(message, filename, linep[0], null, 0);
		}

		/// <summary>Report a runtime error using the error reporter for the current thread.</summary>
		/// <remarks>Report a runtime error using the error reporter for the current thread.</remarks>
		/// <param name="message">the error message to report</param>
		/// <param name="sourceName">a string describing the source, such as a filename</param>
		/// <param name="lineno">the starting line number</param>
		/// <param name="lineSource">the text of the line (may be null)</param>
		/// <param name="lineOffset">the offset into lineSource where problem was detected</param>
		/// <returns>
		/// a runtime exception that will be thrown to terminate the
		/// execution of the script
		/// </returns>
		/// <seealso cref="ErrorReporter">ErrorReporter</seealso>
		public static EvaluatorException ReportRuntimeError(string message, string sourceName, int lineno, string lineSource, int lineOffset)
		{
			Rhino.Context cx = GetCurrentContext();
			if (cx != null)
			{
				return cx.GetErrorReporter().RuntimeError(message, sourceName, lineno, lineSource, lineOffset);
			}
			else
			{
				throw new EvaluatorException(message, sourceName, lineno, lineSource, lineOffset);
			}
		}

		internal static EvaluatorException ReportRuntimeError0(string messageId)
		{
			string msg = ScriptRuntime.GetMessage0(messageId);
			return ReportRuntimeError(msg);
		}

		internal static EvaluatorException ReportRuntimeError1(string messageId, object arg1)
		{
			string msg = ScriptRuntime.GetMessage1(messageId, arg1);
			return ReportRuntimeError(msg);
		}

		internal static EvaluatorException ReportRuntimeError2(string messageId, object arg1, object arg2)
		{
			string msg = ScriptRuntime.GetMessage2(messageId, arg1, arg2);
			return ReportRuntimeError(msg);
		}

		internal static EvaluatorException ReportRuntimeError3(string messageId, object arg1, object arg2, object arg3)
		{
			string msg = ScriptRuntime.GetMessage3(messageId, arg1, arg2, arg3);
			return ReportRuntimeError(msg);
		}

		internal static EvaluatorException ReportRuntimeError4(string messageId, object arg1, object arg2, object arg3, object arg4)
		{
			string msg = ScriptRuntime.GetMessage4(messageId, arg1, arg2, arg3, arg4);
			return ReportRuntimeError(msg);
		}

		/// <summary>Report a runtime error using the error reporter for the current thread.</summary>
		/// <remarks>Report a runtime error using the error reporter for the current thread.</remarks>
		/// <param name="message">the error message to report</param>
		/// <seealso cref="ErrorReporter">ErrorReporter</seealso>
		public static EvaluatorException ReportRuntimeError(string message)
		{
			int[] linep = new int[] { 0 };
			string filename = GetSourcePositionFromStack(linep);
			return Rhino.Context.ReportRuntimeError(message, filename, linep[0], null, 0);
		}

		/// <summary>Initialize the standard objects.</summary>
		/// <remarks>
		/// Initialize the standard objects.
		/// Creates instances of the standard objects and their constructors
		/// (Object, String, Number, Date, etc.), setting up 'scope' to act
		/// as a global object as in ECMA 15.1.<p>
		/// This method must be called to initialize a scope before scripts
		/// can be evaluated in that scope.<p>
		/// This method does not affect the Context it is called upon.
		/// </remarks>
		/// <returns>the initialized scope</returns>
		public ScriptableObject InitStandardObjects()
		{
			return InitStandardObjects(null, false);
		}

		/// <summary>Initialize the standard objects.</summary>
		/// <remarks>
		/// Initialize the standard objects.
		/// Creates instances of the standard objects and their constructors
		/// (Object, String, Number, Date, etc.), setting up 'scope' to act
		/// as a global object as in ECMA 15.1.<p>
		/// This method must be called to initialize a scope before scripts
		/// can be evaluated in that scope.<p>
		/// This method does not affect the Context it is called upon.
		/// </remarks>
		/// <param name="scope">
		/// the scope to initialize, or null, in which case a new
		/// object will be created to serve as the scope
		/// </param>
		/// <returns>
		/// the initialized scope. The method returns the value of the scope
		/// argument if it is not null or newly allocated scope object which
		/// is an instance
		/// <see cref="ScriptableObject">ScriptableObject</see>
		/// .
		/// </returns>
		public Scriptable InitStandardObjects(ScriptableObject scope)
		{
			return InitStandardObjects(scope, false);
		}

		/// <summary>Initialize the standard objects.</summary>
		/// <remarks>
		/// Initialize the standard objects.
		/// Creates instances of the standard objects and their constructors
		/// (Object, String, Number, Date, etc.), setting up 'scope' to act
		/// as a global object as in ECMA 15.1.<p>
		/// This method must be called to initialize a scope before scripts
		/// can be evaluated in that scope.<p>
		/// This method does not affect the Context it is called upon.<p>
		/// This form of the method also allows for creating "sealed" standard
		/// objects. An object that is sealed cannot have properties added, changed,
		/// or removed. This is useful to create a "superglobal" that can be shared
		/// among several top-level objects. Note that sealing is not allowed in
		/// the current ECMA/ISO language specification, but is likely for
		/// the next version.
		/// </remarks>
		/// <param name="scope">
		/// the scope to initialize, or null, in which case a new
		/// object will be created to serve as the scope
		/// </param>
		/// <param name="sealed">
		/// whether or not to create sealed standard objects that
		/// cannot be modified.
		/// </param>
		/// <returns>
		/// the initialized scope. The method returns the value of the scope
		/// argument if it is not null or newly allocated scope object.
		/// </returns>
		/// <since>1.4R3</since>
		public virtual ScriptableObject InitStandardObjects(ScriptableObject scope, bool @sealed)
		{
			return ScriptRuntime.InitStandardObjects(this, scope, @sealed);
		}

		/// <summary>Get the singleton object that represents the JavaScript Undefined value.</summary>
		/// <remarks>Get the singleton object that represents the JavaScript Undefined value.</remarks>
		public static object GetUndefinedValue()
		{
			return Undefined.instance;
		}

		/// <summary>Evaluate a JavaScript source string.</summary>
		/// <remarks>
		/// Evaluate a JavaScript source string.
		/// The provided source name and line number are used for error messages
		/// and for producing debug information.
		/// </remarks>
		/// <param name="scope">the scope to execute in</param>
		/// <param name="source">the JavaScript source</param>
		/// <param name="sourceName">a string describing the source, such as a filename</param>
		/// <param name="lineno">the starting line number</param>
		/// <param name="securityDomain">
		/// an arbitrary object that specifies security
		/// information about the origin or owner of the script. For
		/// implementations that don't care about security, this value
		/// may be null.
		/// </param>
		/// <returns>the result of evaluating the string</returns>
		/// <seealso cref="SecurityController">SecurityController</seealso>
		public object EvaluateString(Scriptable scope, string source, string sourceName, int lineno, object securityDomain)
		{
			Script script = CompileString(source, sourceName, lineno, securityDomain);
			if (script != null)
			{
				return script.Exec(this, scope);
			}
			else
			{
				return null;
			}
		}

		/// <summary>Evaluate a reader as JavaScript source.</summary>
		/// <remarks>
		/// Evaluate a reader as JavaScript source.
		/// All characters of the reader are consumed.
		/// </remarks>
		/// <param name="scope">the scope to execute in</param>
		/// <param name="in">the Reader to get JavaScript source from</param>
		/// <param name="sourceName">a string describing the source, such as a filename</param>
		/// <param name="lineno">the starting line number</param>
		/// <param name="securityDomain">
		/// an arbitrary object that specifies security
		/// information about the origin or owner of the script. For
		/// implementations that don't care about security, this value
		/// may be null.
		/// </param>
		/// <returns>the result of evaluating the source</returns>
		/// <exception>
		/// IOException
		/// if an IOException was generated by the Reader
		/// </exception>
		/// <exception cref="System.IO.IOException"></exception>
		public object EvaluateReader(Scriptable scope, TextReader @in, string sourceName, int lineno, object securityDomain)
		{
			Script script = CompileReader(@in, sourceName, lineno, securityDomain);
			if (script != null)
			{
				return script.Exec(this, scope);
			}
			else
			{
				return null;
			}
		}

		/// <summary>Execute script that may pause execution by capturing a continuation.</summary>
		/// <remarks>
		/// Execute script that may pause execution by capturing a continuation.
		/// Caller must be prepared to catch a ContinuationPending exception
		/// and resume execution by calling
		/// <see cref="ResumeContinuation(object, Scriptable, object)">ResumeContinuation(object, Scriptable, object)</see>
		/// .
		/// </remarks>
		/// <param name="script">
		/// The script to execute. Script must have been compiled
		/// with interpreted mode (optimization level -1)
		/// </param>
		/// <param name="scope">The scope to execute the script against</param>
		/// <exception cref="ContinuationPending">
		/// if the script calls a function that results
		/// in a call to
		/// <see cref="CaptureContinuation()">CaptureContinuation()</see>
		/// </exception>
		/// <since>1.7 Release 2</since>
		/// <exception cref="Rhino.ContinuationPending"></exception>
		public virtual object ExecuteScriptWithContinuations(Script script, Scriptable scope)
		{
			var function = script as InterpretedFunction;
			if (function == null || !function.IsScript())
			{
				throw new ArgumentException("Script argument was not a script or was not created by interpreted mode ");
			}
			// Can only be applied to scripts
			return CallFunctionWithContinuations(function, scope, ScriptRuntime.emptyArgs);
		}

		/// <summary>Call function that may pause execution by capturing a continuation.</summary>
		/// <remarks>
		/// Call function that may pause execution by capturing a continuation.
		/// Caller must be prepared to catch a ContinuationPending exception
		/// and resume execution by calling
		/// <see cref="ResumeContinuation(object, Scriptable, object)">ResumeContinuation(object, Scriptable, object)</see>
		/// .
		/// </remarks>
		/// <param name="function">
		/// The function to call. The function must have been
		/// compiled with interpreted mode (optimization level -1)
		/// </param>
		/// <param name="scope">The scope to execute the script against</param>
		/// <param name="args">The arguments for the function</param>
		/// <exception cref="ContinuationPending">
		/// if the script calls a function that results
		/// in a call to
		/// <see cref="CaptureContinuation()">CaptureContinuation()</see>
		/// </exception>
		/// <since>1.7 Release 2</since>
		/// <exception cref="Rhino.ContinuationPending"></exception>
		public virtual object CallFunctionWithContinuations(Callable function, Scriptable scope, object[] args)
		{
			if (!(function is InterpretedFunction))
			{
				// Can only be applied to scripts
				throw new ArgumentException("Function argument was not" + " created by interpreted mode ");
			}
			if (ScriptRuntime.HasTopCall(this))
			{
				throw new InvalidOperationException("Cannot have any pending top " + "calls when executing a script with continuations");
			}
			// Annotate so we can check later to ensure no java code in
			// intervening frames
			isContinuationsTopCall = true;
			return ScriptRuntime.DoTopCall(function, this, scope, scope, args);
		}

		/// <summary>Capture a continuation from the current execution.</summary>
		/// <remarks>
		/// Capture a continuation from the current execution. The execution must
		/// have been started via a call to
		/// <see cref="ExecuteScriptWithContinuations(Script, Scriptable)">ExecuteScriptWithContinuations(Script, Scriptable)</see>
		/// or
		/// <see cref="CallFunctionWithContinuations(Callable, Scriptable, object[])">CallFunctionWithContinuations(Callable, Scriptable, object[])</see>
		/// .
		/// This implies that the code calling
		/// this method must have been called as a function from the
		/// JavaScript script. Also, there cannot be any non-JavaScript code
		/// between the JavaScript frames (e.g., a call to eval()). The
		/// ContinuationPending exception returned must be thrown.
		/// </remarks>
		/// <returns>A ContinuationPending exception that must be thrown</returns>
		/// <since>1.7 Release 2</since>
		public virtual ContinuationPending CaptureContinuation()
		{
			return new ContinuationPending(Interpreter.CaptureContinuation(this));
		}

		/// <summary>
		/// Restarts execution of the JavaScript suspended at the call
		/// to
		/// <see cref="CaptureContinuation()">CaptureContinuation()</see>
		/// . Execution of the code will resume
		/// with the functionResult as the result of the call that captured the
		/// continuation.
		/// Execution of the script will either conclude normally and the
		/// result returned, another continuation will be captured and
		/// thrown, or the script will terminate abnormally and throw an exception.
		/// </summary>
		/// <param name="continuation">
		/// The value returned by
		/// <see cref="ContinuationPending.GetContinuation()">ContinuationPending.GetContinuation()</see>
		/// </param>
		/// <param name="functionResult">
		/// This value will appear to the code being resumed
		/// as the result of the function that captured the continuation
		/// </param>
		/// <exception cref="ContinuationPending">
		/// if another continuation is captured before
		/// the code terminates
		/// </exception>
		/// <since>1.7 Release 2</since>
		/// <exception cref="Rhino.ContinuationPending"></exception>
		public virtual object ResumeContinuation(object continuation, Scriptable scope, object functionResult)
		{
			object[] args = new object[] { functionResult };
			return Interpreter.RestartContinuation((NativeContinuation)continuation, this, scope, args);
		}

		/// <summary>Check whether a string is ready to be compiled.</summary>
		/// <remarks>
		/// Check whether a string is ready to be compiled.
		/// <p>
		/// stringIsCompilableUnit is intended to support interactive compilation of
		/// JavaScript.  If compiling the string would result in an error
		/// that might be fixed by appending more source, this method
		/// returns false.  In every other case, it returns true.
		/// <p>
		/// Interactive shells may accumulate source lines, using this
		/// method after each new line is appended to check whether the
		/// statement being entered is complete.
		/// </remarks>
		/// <param name="source">the source buffer to check</param>
		/// <returns>whether the source is ready for compilation</returns>
		/// <since>1.4 Release 2</since>
		public bool StringIsCompilableUnit(string source)
		{
			bool errorseen = false;
			var compilerEnv = new CompilerEnvirons(this)
			{
				// no source name or source text manager, because we're just
				// going to throw away the result.
				GeneratingSource = false
			};
			Parser p = new Parser(compilerEnv, DefaultErrorReporter.instance);
			try
			{
				p.Parse(source, null, 1);
			}
			catch (EvaluatorException)
			{
				errorseen = true;
			}
			// Return false only if an error occurred as a result of reading past
			// the end of the file, i.e. if the source could be fixed by
			// appending more source.
			if (errorseen && p.Eof())
			{
				return false;
			}
			else
			{
				return true;
			}
		}

		/// <summary>Compiles the source in the given reader.</summary>
		/// <remarks>
		/// Compiles the source in the given reader.
		/// <p>
		/// Returns a script that may later be executed.
		/// Will consume all the source in the reader.
		/// </remarks>
		/// <param name="in">the input reader</param>
		/// <param name="sourceName">a string describing the source, such as a filename</param>
		/// <param name="lineno">the starting line number for reporting errors</param>
		/// <param name="securityDomain">
		/// an arbitrary object that specifies security
		/// information about the origin or owner of the script. For
		/// implementations that don't care about security, this value
		/// may be null.
		/// </param>
		/// <returns>a script that may later be executed</returns>
		/// <exception>
		/// IOException
		/// if an IOException was generated by the Reader
		/// </exception>
		/// <seealso cref="Script">Script</seealso>
		/// <exception cref="System.IO.IOException"></exception>
		public Script CompileReader(TextReader @in, string sourceName, int lineno, object securityDomain)
		{
			if (lineno < 0)
			{
				// For compatibility IllegalArgumentException can not be thrown here
				lineno = 0;
			}
			return (Script)CompileImpl(null, @in, null, sourceName, lineno, securityDomain, false, null, null);
		}

		/// <summary>Compiles the source in the given string.</summary>
		/// <remarks>
		/// Compiles the source in the given string.
		/// <p>
		/// Returns a script that may later be executed.
		/// </remarks>
		/// <param name="source">the source string</param>
		/// <param name="sourceName">a string describing the source, such as a filename</param>
		/// <param name="lineno">
		/// the starting line number for reporting errors. Use
		/// 0 if the line number is unknown.
		/// </param>
		/// <param name="securityDomain">
		/// an arbitrary object that specifies security
		/// information about the origin or owner of the script. For
		/// implementations that don't care about security, this value
		/// may be null.
		/// </param>
		/// <returns>a script that may later be executed</returns>
		/// <seealso cref="Script">Script</seealso>
		public Script CompileString(string source, string sourceName, int lineno, object securityDomain)
		{
			if (lineno < 0)
			{
				// For compatibility IllegalArgumentException can not be thrown here
				lineno = 0;
			}
			return CompileString(source, null, null, sourceName, lineno, securityDomain);
		}

		internal Script CompileString(string source, Evaluator compiler, ErrorReporter compilationErrorReporter, string sourceName, int lineno, object securityDomain)
		{
			try
			{
				return (Script)CompileImpl(null, null, source, sourceName, lineno, securityDomain, false, compiler, compilationErrorReporter);
			}
			catch (IOException)
			{
				// Should not happen when dealing with source as string
				throw new Exception();
			}
		}

		/// <summary>Compile a JavaScript function.</summary>
		/// <remarks>
		/// Compile a JavaScript function.
		/// <p>
		/// The function source must be a function definition as defined by
		/// ECMA (e.g., "function f(a) { return a; }").
		/// </remarks>
		/// <param name="scope">the scope to compile relative to</param>
		/// <param name="source">the function definition source</param>
		/// <param name="sourceName">a string describing the source, such as a filename</param>
		/// <param name="lineno">the starting line number</param>
		/// <param name="securityDomain">
		/// an arbitrary object that specifies security
		/// information about the origin or owner of the script. For
		/// implementations that don't care about security, this value
		/// may be null.
		/// </param>
		/// <returns>a Function that may later be called</returns>
		/// <seealso cref="Function">Function</seealso>
		public Function CompileFunction(Scriptable scope, string source, string sourceName, int lineno, object securityDomain)
		{
			return CompileFunction(scope, source, null, null, sourceName, lineno, securityDomain);
		}

		internal Function CompileFunction(Scriptable scope, string source, Evaluator compiler, ErrorReporter compilationErrorReporter, string sourceName, int lineno, object securityDomain)
		{
			try
			{
				return (Function)CompileImpl(scope, null, source, sourceName, lineno, securityDomain, true, compiler, compilationErrorReporter);
			}
			catch (IOException)
			{
				// Should never happen because we just made the reader
				// from a String
				throw new Exception();
			}
		}

		/// <summary>Decompile the script.</summary>
		/// <remarks>
		/// Decompile the script.
		/// <p>
		/// The canonical source of the script is returned.
		/// </remarks>
		/// <param name="script">the script to decompile</param>
		/// <param name="indent">the number of spaces to indent the result</param>
		/// <returns>a string representing the script source</returns>
		public string DecompileScript(Script script, int indent)
		{
			NativeFunction scriptImpl = (NativeFunction)script;
			return scriptImpl.Decompile(indent, 0);
		}

		/// <summary>Decompile a JavaScript Function.</summary>
		/// <remarks>
		/// Decompile a JavaScript Function.
		/// <p>
		/// Decompiles a previously compiled JavaScript function object to
		/// canonical source.
		/// <p>
		/// Returns function body of '[native code]' if no decompilation
		/// information is available.
		/// </remarks>
		/// <param name="fun">the JavaScript function to decompile</param>
		/// <param name="indent">the number of spaces to indent the result</param>
		/// <returns>a string representing the function source</returns>
		public string DecompileFunction(Function fun, int indent)
		{
			var baseFunction = fun as BaseFunction;
			if (baseFunction != null)
			{
				return baseFunction.Decompile(indent, 0);
			}
			else
			{
				return "function " + fun.GetClassName() + "() {\n\t[native code]\n}\n";
			}
		}

		/// <summary>Decompile the body of a JavaScript Function.</summary>
		/// <remarks>
		/// Decompile the body of a JavaScript Function.
		/// <p>
		/// Decompiles the body a previously compiled JavaScript Function
		/// object to canonical source, omitting the function header and
		/// trailing brace.
		/// Returns '[native code]' if no decompilation information is available.
		/// </remarks>
		/// <param name="fun">the JavaScript function to decompile</param>
		/// <param name="indent">the number of spaces to indent the result</param>
		/// <returns>a string representing the function body source.</returns>
		public string DecompileFunctionBody(Function fun, int indent)
		{
			var bf = fun as BaseFunction;
			if (bf != null)
			{
				return bf.Decompile(indent, Decompiler.ONLY_BODY_FLAG);
			}
			// ALERT: not sure what the right response here is.
			return "[native code]\n";
		}

		/// <summary>Create a new JavaScript object.</summary>
		/// <remarks>
		/// Create a new JavaScript object.
		/// Equivalent to evaluating "new Object()".
		/// </remarks>
		/// <param name="scope">
		/// the scope to search for the constructor and to evaluate
		/// against
		/// </param>
		/// <returns>the new object</returns>
		public virtual Scriptable NewObject(Scriptable scope)
		{
			NativeObject result = new NativeObject();
			ScriptRuntime.SetBuiltinProtoAndParent(result, scope, TopLevel.Builtins.Object);
			return result;
		}

		/// <summary>Create a new JavaScript object by executing the named constructor.</summary>
		/// <remarks>
		/// Create a new JavaScript object by executing the named constructor.
		/// The call <code>newObject(scope, "Foo")</code> is equivalent to
		/// evaluating "new Foo()".
		/// </remarks>
		/// <param name="scope">the scope to search for the constructor and to evaluate against</param>
		/// <param name="constructorName">the name of the constructor to call</param>
		/// <returns>the new object</returns>
		public virtual Scriptable NewObject(Scriptable scope, string constructorName)
		{
			return NewObject(scope, constructorName, ScriptRuntime.emptyArgs);
		}

		/// <summary>Creates a new JavaScript object by executing the named constructor.</summary>
		/// <remarks>
		/// Creates a new JavaScript object by executing the named constructor.
		/// Searches <code>scope</code> for the named constructor, calls it with
		/// the given arguments, and returns the result.<p>
		/// The code
		/// <pre>
		/// Object[] args = { "a", "b" };
		/// newObject(scope, "Foo", args)</pre>
		/// is equivalent to evaluating "new Foo('a', 'b')", assuming that the Foo
		/// constructor has been defined in <code>scope</code>.
		/// </remarks>
		/// <param name="scope">
		/// The scope to search for the constructor and to evaluate
		/// against
		/// </param>
		/// <param name="constructorName">the name of the constructor to call</param>
		/// <param name="args">the array of arguments for the constructor</param>
		/// <returns>the new object</returns>
		public virtual Scriptable NewObject(Scriptable scope, string constructorName, object[] args)
		{
			scope = ScriptableObject.GetTopLevelScope(scope);
			Function ctor = ScriptRuntime.GetExistingCtor(this, scope, constructorName);
			if (args == null)
			{
				args = ScriptRuntime.emptyArgs;
			}
			return ctor.Construct(this, scope, args);
		}

		/// <summary>Create an array with a specified initial length.</summary>
		/// <remarks>
		/// Create an array with a specified initial length.
		/// <p>
		/// </remarks>
		/// <param name="scope">the scope to create the object in</param>
		/// <param name="length">
		/// the initial length (JavaScript arrays may have
		/// additional properties added dynamically).
		/// </param>
		/// <returns>the new array object</returns>
		public virtual Scriptable NewArray(Scriptable scope, int length)
		{
			NativeArray result = new NativeArray(length);
			ScriptRuntime.SetBuiltinProtoAndParent(result, scope, TopLevel.Builtins.Array);
			return result;
		}

		/// <summary>Create an array with a set of initial elements.</summary>
		/// <remarks>Create an array with a set of initial elements.</remarks>
		/// <param name="scope">the scope to create the object in.</param>
		/// <param name="elements">
		/// the initial elements. Each object in this array
		/// must be an acceptable JavaScript type and type
		/// of array should be exactly Object[], not
		/// SomeObjectSubclass[].
		/// </param>
		/// <returns>the new array object.</returns>
		public virtual Scriptable NewArray(Scriptable scope, object[] elements)
		{
			if (elements.GetType().GetElementType() != ScriptRuntime.ObjectClass)
			{
				throw new ArgumentException();
			}
			NativeArray result = new NativeArray(elements);
			ScriptRuntime.SetBuiltinProtoAndParent(result, scope, TopLevel.Builtins.Array);
			return result;
		}

		/// <summary>Get the elements of a JavaScript array.</summary>
		/// <remarks>
		/// Get the elements of a JavaScript array.
		/// <p>
		/// If the object defines a length property convertible to double number,
		/// then the number is converted Uint32 value as defined in Ecma 9.6
		/// and Java array of that size is allocated.
		/// The array is initialized with the values obtained by
		/// calling get() on object for each value of i in [0,length-1]. If
		/// there is not a defined value for a property the Undefined value
		/// is used to initialize the corresponding element in the array. The
		/// Java array is then returned.
		/// If the object doesn't define a length property or it is not a number,
		/// empty array is returned.
		/// </remarks>
		/// <param name="object">the JavaScript array or array-like object</param>
		/// <returns>a Java array of objects</returns>
		/// <since>1.4 release 2</since>
		public object[] GetElements(Scriptable @object)
		{
			return ScriptRuntime.GetArrayElements(@object);
		}

		/// <summary>Convert the value to a JavaScript boolean value.</summary>
		/// <remarks>
		/// Convert the value to a JavaScript boolean value.
		/// <p>
		/// See ECMA 9.2.
		/// </remarks>
		/// <param name="value">a JavaScript value</param>
		/// <returns>
		/// the corresponding boolean value converted using
		/// the ECMA rules
		/// </returns>
		[UsedImplicitly]
		public static bool ToBoolean(object value)
		{
			return ScriptRuntime.ToBoolean(value);
		}

		/// <summary>Convert the value to a JavaScript Number value.</summary>
		/// <remarks>
		/// Convert the value to a JavaScript Number value.
		/// <p>
		/// Returns a Java double for the JavaScript Number.
		/// <p>
		/// See ECMA 9.3.
		/// </remarks>
		/// <param name="value">a JavaScript value</param>
		/// <returns>
		/// the corresponding double value converted using
		/// the ECMA rules
		/// </returns>
		public static double ToNumber(object value)
		{
			return ScriptRuntime.ToNumber(value);
		}

		/// <summary>Convert the value to a JavaScript String value.</summary>
		/// <remarks>
		/// Convert the value to a JavaScript String value.
		/// <p>
		/// See ECMA 9.8.
		/// <p>
		/// </remarks>
		/// <param name="value">a JavaScript value</param>
		/// <returns>
		/// the corresponding String value converted using
		/// the ECMA rules
		/// </returns>
		public static string ToString(object value)
		{
			return ScriptRuntime.ToString(value);
		}

		/// <summary>Convert the value to an JavaScript object value.</summary>
		/// <remarks>
		/// Convert the value to an JavaScript object value.
		/// <p>
		/// Note that a scope must be provided to look up the constructors
		/// for Number, Boolean, and String.
		/// <p>
		/// See ECMA 9.9.
		/// <p>
		/// Additionally, arbitrary Java objects and classes will be
		/// wrapped in a Scriptable object with its Java fields and methods
		/// reflected as JavaScript properties of the object.
		/// </remarks>
		/// <param name="value">any Java object</param>
		/// <param name="scope">
		/// global scope containing constructors for Number,
		/// Boolean, and String
		/// </param>
		/// <returns>new JavaScript object</returns>
		public static Scriptable ToObject(object value, Scriptable scope)
		{
			return ScriptRuntime.ToObject(scope, value);
		}

		/// <summary>
		/// Convenient method to convert java value to its closest representation
		/// in JavaScript.
		/// </summary>
		/// <remarks>
		/// Convenient method to convert java value to its closest representation
		/// in JavaScript.
		/// <p>
		/// If value is an instance of String, Number, Boolean, Function or
		/// Scriptable, it is returned as it and will be treated as the corresponding
		/// JavaScript type of string, number, boolean, function and object.
		/// <p>
		/// Note that for Number instances during any arithmetic operation in
		/// JavaScript the engine will always use the result of
		/// <tt>Number.doubleValue()</tt> resulting in a precision loss if
		/// the number can not fit into double.
		/// <p>
		/// If value is an instance of Character, it will be converted to string of
		/// length 1 and its JavaScript type will be string.
		/// <p>
		/// The rest of values will be wrapped as LiveConnect objects
		/// by calling
		/// <see cref="WrapFactory.Wrap(Context, Scriptable, object, System.Type{T})">WrapFactory.Wrap(Context, Scriptable, object, System.Type&lt;T&gt;)</see>
		/// as in:
		/// <pre>
		/// Context cx = Context.getCurrentContext();
		/// return cx.getWrapFactory().wrap(cx, scope, value, null);
		/// </pre>
		/// </remarks>
		/// <param name="value">any Java object</param>
		/// <param name="scope">top scope object</param>
		/// <returns>value suitable to pass to any API that takes JavaScript values.</returns>
		public static object JavaToJS(object value, Scriptable scope)
		{
			if (value is string || value.IsNumber() || value is bool || value is Scriptable)
			{
				return value;
			}
			else
			{
				if (value is char)
				{
					return ((char)value).ToString();
				}
				else
				{
					Rhino.Context cx = Rhino.Context.GetContext();
					return cx.GetWrapFactory().Wrap(cx, scope, value, null);
				}
			}
		}

		/// <summary>Convert a JavaScript value into the desired type.</summary>
		/// <remarks>
		/// Convert a JavaScript value into the desired type.
		/// Uses the semantics defined with LiveConnect3 and throws an
		/// Illegal argument exception if the conversion cannot be performed.
		/// </remarks>
		/// <param name="value">the JavaScript value to convert</param>
		/// <param name="desiredType">
		/// the Java type to convert to. Primitive Java
		/// types are represented using the TYPE fields in the corresponding
		/// wrapper class in java.lang.
		/// </param>
		/// <returns>the converted value</returns>
		/// <exception cref="EvaluatorException">if the conversion cannot be performed</exception>
		/// <exception cref="Rhino.EvaluatorException"></exception>
		public static object JsToJava(object value, Type desiredType)
		{
			return NativeJavaObject.CoerceTypeImpl(desiredType, value);
		}

		/// <summary>Rethrow the exception wrapping it as the script runtime exception.</summary>
		/// <remarks>
		/// Rethrow the exception wrapping it as the script runtime exception.
		/// Unless the exception is instance of
		/// <see cref="EcmaError">EcmaError</see>
		/// or
		/// <see cref="EvaluatorException">EvaluatorException</see>
		/// it will be wrapped as
		/// <see cref="WrappedException">WrappedException</see>
		/// , a subclass of
		/// <see cref="EvaluatorException">EvaluatorException</see>
		/// .
		/// The resulting exception object always contains
		/// source name and line number of script that triggered exception.
		/// <p>
		/// This method always throws an exception, its return value is provided
		/// only for convenience to allow a usage like:
		/// <pre>
		/// throw Context.throwAsScriptRuntimeEx(ex);
		/// </pre>
		/// to indicate that code after the method is unreachable.
		/// </remarks>
		/// <exception cref="EvaluatorException">EvaluatorException</exception>
		/// <exception cref="EcmaError">EcmaError</exception>
		public static Exception ThrowAsScriptRuntimeEx(Exception e)
		{
			while ((e is TargetInvocationException))
			{
				e = ((TargetInvocationException)e).InnerException;
			}
			// special handling of Error so scripts would not catch them
			if (e is Exception)
			{
				Rhino.Context cx = GetContext();
				if (cx == null || !cx.HasFeature(LanguageFeatures.EnhancedJavaAccess))
				{
					throw (Exception)e;
				}
			}
			var rhinoException = e as RhinoException;
			if (rhinoException != null)
			{
				throw rhinoException;
			}
			throw new WrappedException(e);
		}

		/// <summary>Tell whether debug information is being generated.</summary>
		/// <remarks>Tell whether debug information is being generated.</remarks>
		/// <since>1.3</since>
		public bool IsGeneratingDebug()
		{
			return generatingDebug;
		}

		/// <summary>Specify whether or not debug information should be generated.</summary>
		/// <remarks>
		/// Specify whether or not debug information should be generated.
		/// <p>
		/// Setting the generation of debug information on will set the
		/// optimization level to zero.
		/// </remarks>
		/// <since>1.3</since>
		public void SetGeneratingDebug(bool generatingDebug)
		{
			if (@sealed)
			{
				OnSealedMutation();
			}
			generatingDebugChanged = true;
			if (generatingDebug && GetOptimizationLevel() > 0)
			{
				SetOptimizationLevel(0);
			}
			this.generatingDebug = generatingDebug;
		}

		/// <summary>Tell whether source information is being generated.</summary>
		/// <remarks>Tell whether source information is being generated.</remarks>
		/// <since>1.3</since>
		public bool IsGeneratingSource()
		{
			return generatingSource;
		}

		/// <summary>Specify whether or not source information should be generated.</summary>
		/// <remarks>
		/// Specify whether or not source information should be generated.
		/// <p>
		/// Without source information, evaluating the "toString" method
		/// on JavaScript functions produces only "[native code]" for
		/// the body of the function.
		/// Note that code generated without source is not fully ECMA
		/// conformant.
		/// </remarks>
		/// <since>1.3</since>
		public void SetGeneratingSource(bool generatingSource)
		{
			if (@sealed)
			{
				OnSealedMutation();
			}
			this.generatingSource = generatingSource;
		}

		/// <summary>Get the current optimization level.</summary>
		/// <remarks>
		/// Get the current optimization level.
		/// <p>
		/// The optimization level is expressed as an integer between -1 and
		/// 9.
		/// </remarks>
		/// <since>1.3</since>
		public int GetOptimizationLevel()
		{
			return optimizationLevel;
		}

		/// <summary>Set the current optimization level.</summary>
		/// <remarks>
		/// Set the current optimization level.
		/// <p>
		/// The optimization level is expected to be an integer between -1 and
		/// 9. Any negative values will be interpreted as -1, and any values
		/// greater than 9 will be interpreted as 9.
		/// An optimization level of -1 indicates that interpretive mode will
		/// always be used. Levels 0 through 9 indicate that class files may
		/// be generated. Higher optimization levels trade off compile time
		/// performance for runtime performance.
		/// The optimizer level can't be set greater than -1 if the optimizer
		/// package doesn't exist at run time.
		/// </remarks>
		/// <param name="optimizationLevel">
		/// an integer indicating the level of
		/// optimization to perform
		/// </param>
		/// <since>1.3</since>
		public void SetOptimizationLevel(int optimizationLevel)
		{
			if (@sealed)
			{
				OnSealedMutation();
			}
			if (optimizationLevel == -2)
			{
				// To be compatible with Cocoon fork
				optimizationLevel = -1;
			}
			CheckOptimizationLevel(optimizationLevel);
			if (codegenClass == null)
			{
				optimizationLevel = -1;
			}
			this.optimizationLevel = optimizationLevel;
		}

		public static bool IsValidOptimizationLevel(int optimizationLevel)
		{
			return -1 <= optimizationLevel && optimizationLevel <= 9;
		}

		public static void CheckOptimizationLevel(int optimizationLevel)
		{
			if (IsValidOptimizationLevel(optimizationLevel))
			{
				return;
			}
			throw new ArgumentException("Optimization level outside [-1..9]: " + optimizationLevel);
		}

		/// <summary>
		/// Returns the maximum stack depth (in terms of number of call frames)
		/// allowed in a single invocation of interpreter.
		/// </summary>
		/// <remarks>
		/// Returns the maximum stack depth (in terms of number of call frames)
		/// allowed in a single invocation of interpreter. If the set depth would be
		/// exceeded, the interpreter will throw an EvaluatorException in the script.
		/// Defaults to Integer.MAX_VALUE. The setting only has effect for
		/// interpreted functions (those compiled with optimization level set to -1).
		/// As the interpreter doesn't use the Java stack but rather manages its own
		/// stack in the heap memory, a runaway recursion in interpreted code would
		/// eventually consume all available memory and cause OutOfMemoryError
		/// instead of a StackOverflowError limited to only a single thread. This
		/// setting helps prevent such situations.
		/// </remarks>
		/// <returns>The current maximum interpreter stack depth.</returns>
		public int GetMaximumInterpreterStackDepth()
		{
			return maximumInterpreterStackDepth;
		}

		/// <summary>
		/// Sets the maximum stack depth (in terms of number of call frames)
		/// allowed in a single invocation of interpreter.
		/// </summary>
		/// <remarks>
		/// Sets the maximum stack depth (in terms of number of call frames)
		/// allowed in a single invocation of interpreter. If the set depth would be
		/// exceeded, the interpreter will throw an EvaluatorException in the script.
		/// Defaults to Integer.MAX_VALUE. The setting only has effect for
		/// interpreted functions (those compiled with optimization level set to -1).
		/// As the interpreter doesn't use the Java stack but rather manages its own
		/// stack in the heap memory, a runaway recursion in interpreted code would
		/// eventually consume all available memory and cause OutOfMemoryError
		/// instead of a StackOverflowError limited to only a single thread. This
		/// setting helps prevent such situations.
		/// </remarks>
		/// <param name="max">the new maximum interpreter stack depth</param>
		/// <exception cref="System.InvalidOperationException">
		/// if this context's optimization level is not
		/// -1
		/// </exception>
		/// <exception cref="System.ArgumentException">if the new depth is not at least 1</exception>
		public void SetMaximumInterpreterStackDepth(int max)
		{
			if (@sealed)
			{
				OnSealedMutation();
			}
			if (optimizationLevel != -1)
			{
				throw new InvalidOperationException("Cannot set maximumInterpreterStackDepth when optimizationLevel != -1");
			}
			if (max < 1)
			{
				throw new ArgumentException("Cannot set maximumInterpreterStackDepth to less than 1");
			}
			maximumInterpreterStackDepth = max;
		}

#if ENHANCED_SECURITY
		/// <summary>Set the security controller for this context.</summary>
		/// <remarks>
		/// Set the security controller for this context.
		/// <p> SecurityController may only be set if it is currently null
		/// and
		/// <see cref="SecurityController.HasGlobal()">SecurityController.HasGlobal()</see>
		/// is <tt>false</tt>.
		/// Otherwise a SecurityException is thrown.
		/// </remarks>
		/// <param name="controller">a SecurityController object</param>
		/// <exception cref="System.Security.SecurityException">
		/// if there is already a SecurityController
		/// object for this Context or globally installed.
		/// </exception>
		/// <seealso cref="SecurityController.InitGlobal(SecurityController)">SecurityController.InitGlobal(SecurityController)</seealso>
		/// <seealso cref="SecurityController.HasGlobal()">SecurityController.HasGlobal()</seealso>
		public void SetSecurityController(SecurityController controller)
		{
			if (@sealed)
			{
				OnSealedMutation();
			}
			if (controller == null)
			{
				throw new ArgumentException();
			}
			if (securityController != null)
			{
				throw new SecurityException("Can not overwrite existing SecurityController object");
			}
			if (SecurityController.HasGlobal())
			{
				throw new SecurityException("Can not overwrite existing global SecurityController object");
			}
			securityController = controller;
		}
#endif

		/// <summary>Set the LiveConnect access filter for this context.</summary>
		/// <remarks>
		/// Set the LiveConnect access filter for this context.
		/// <p>
		/// <see cref="ClassShutter">ClassShutter</see>
		/// may only be set if it is currently null.
		/// Otherwise a SecurityException is thrown.
		/// </remarks>
		/// <param name="shutter">a ClassShutter object</param>
		/// <exception cref="System.Security.SecurityException">
		/// if there is already a ClassShutter
		/// object for this Context
		/// </exception>
		public void SetClassShutter(ClassShutter shutter)
		{
			lock (this)
			{
				if (@sealed)
				{
					OnSealedMutation();
				}
				if (shutter == null)
				{
					throw new ArgumentException();
				}
				if (hasClassShutter)
				{
					throw new SecurityException("Cannot overwrite existing " + "ClassShutter object");
				}
				classShutter = shutter;
				hasClassShutter = true;
			}
		}

		internal ClassShutter GetClassShutter()
		{
			lock (this)
			{
				return classShutter;
			}
		}

		public interface ClassShutterSetter
		{
			void SetClassShutter(ClassShutter shutter);

			ClassShutter GetClassShutter();
		}

		public Context.ClassShutterSetter GetClassShutterSetter()
		{
			lock (this)
			{
				if (hasClassShutter)
				{
					return null;
				}
				hasClassShutter = true;
				return new _ClassShutterSetter_1970(this);
			}
		}

		private sealed class _ClassShutterSetter_1970 : Context.ClassShutterSetter
		{
			public _ClassShutterSetter_1970(Context _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public void SetClassShutter(ClassShutter shutter)
			{
				this._enclosing.classShutter = shutter;
			}

			public ClassShutter GetClassShutter()
			{
				return this._enclosing.classShutter;
			}

			private readonly Context _enclosing;
		}

		/// <summary>Get a value corresponding to a key.</summary>
		/// <remarks>
		/// Get a value corresponding to a key.
		/// <p>
		/// Since the Context is associated with a thread it can be
		/// used to maintain values that can be later retrieved using
		/// the current thread.
		/// <p>
		/// Note that the values are maintained with the Context, so
		/// if the Context is disassociated from the thread the values
		/// cannot be retrieved. Also, if private data is to be maintained
		/// in this manner the key should be a java.lang.Object
		/// whose reference is not divulged to untrusted code.
		/// </remarks>
		/// <param name="key">the key used to lookup the value</param>
		/// <returns>a value previously stored using putThreadLocal.</returns>
		public object GetThreadLocal(object key)
		{
			if (threadLocalMap == null)
			{
				return null;
			}
			return threadLocalMap.GetValueOrDefault(key);
		}

		/// <summary>Put a value that can later be retrieved using a given key.</summary>
		/// <remarks>
		/// Put a value that can later be retrieved using a given key.
		/// <p>
		/// </remarks>
		/// <param name="key">the key used to index the value</param>
		/// <param name="value">the value to save</param>
		public void PutThreadLocal(object key, object value)
		{
			lock (this)
			{
				if (@sealed)
				{
					OnSealedMutation();
				}
				if (threadLocalMap == null)
				{
					threadLocalMap = new Dictionary<object, object>();
				}
				threadLocalMap[key] = value;
			}
		}

		/// <summary>Remove values from thread-local storage.</summary>
		/// <remarks>Remove values from thread-local storage.</remarks>
		/// <param name="key">the key for the entry to remove.</param>
		/// <since>1.5 release 2</since>
		public void RemoveThreadLocal(object key)
		{
			if (@sealed)
			{
				OnSealedMutation();
			}
			if (threadLocalMap == null)
			{
				return;
			}
			threadLocalMap.Remove(key);
		}

		/// <summary>Set a WrapFactory for this Context.</summary>
		/// <remarks>
		/// Set a WrapFactory for this Context.
		/// <p>
		/// The WrapFactory allows custom object wrapping behavior for
		/// Java object manipulated with JavaScript.
		/// </remarks>
		/// <seealso cref="WrapFactory">WrapFactory</seealso>
		/// <since>1.5 Release 4</since>
		public void SetWrapFactory(WrapFactory wrapFactory)
		{
			if (@sealed)
			{
				OnSealedMutation();
			}
			if (wrapFactory == null)
			{
				throw new ArgumentException();
			}
			this.wrapFactory = wrapFactory;
		}

		/// <summary>Return the current WrapFactory, or null if none is defined.</summary>
		/// <remarks>Return the current WrapFactory, or null if none is defined.</remarks>
		/// <seealso cref="WrapFactory">WrapFactory</seealso>
		/// <since>1.5 Release 4</since>
		public WrapFactory GetWrapFactory()
		{
			if (wrapFactory == null)
			{
				wrapFactory = new WrapFactory();
			}
			return wrapFactory;
		}

		/// <summary>Return the current debugger.</summary>
		/// <remarks>Return the current debugger.</remarks>
		/// <returns>the debugger, or null if none is attached.</returns>
		public Debugger GetDebugger()
		{
			return debugger;
		}

		/// <summary>Return the debugger context data associated with current context.</summary>
		/// <remarks>Return the debugger context data associated with current context.</remarks>
		/// <returns>the debugger data, or null if debugger is not attached</returns>
		public object GetDebuggerContextData()
		{
			return debuggerData;
		}

		/// <summary>Set the associated debugger.</summary>
		/// <remarks>Set the associated debugger.</remarks>
		/// <param name="debugger">
		/// the debugger to be used on callbacks from
		/// the engine.
		/// </param>
		/// <param name="contextData">
		/// arbitrary object that debugger can use to store
		/// per Context data.
		/// </param>
		public void SetDebugger(Debugger debugger, object contextData)
		{
			if (@sealed)
			{
				OnSealedMutation();
			}
			this.debugger = debugger;
			debuggerData = contextData;
		}

		/// <summary>Return DebuggableScript instance if any associated with the script.</summary>
		/// <remarks>
		/// Return DebuggableScript instance if any associated with the script.
		/// If callable supports DebuggableScript implementation, the method
		/// returns it. Otherwise null is returned.
		/// </remarks>
		public static DebuggableScript GetDebuggableView(Script script)
		{
			var nativeFunction = script as NativeFunction;
			if (nativeFunction != null)
			{
				return nativeFunction.GetDebuggableView();
			}
			return null;
		}

		/// <summary>Controls certain aspects of script semantics.</summary>
		/// <remarks>
		/// Controls certain aspects of script semantics.
		/// Should be overwritten to alter default behavior.
		/// <p>
		/// The default implementation calls
		/// <see cref="ContextFactory.HasFeature">ContextFactory.HasFeature(Context, int)</see>
		/// that allows to customize Context behavior without introducing
		/// Context subclasses.
		/// <see cref="ContextFactory">ContextFactory</see>
		/// documentation gives
		/// an example of hasFeature implementation.
		/// </remarks>
		/// <param name="featureIndex">feature index to check</param>
		/// <returns>true if the <code>featureIndex</code> feature is turned on</returns>
		/// <seealso cref="LanguageFeatures.NonEcmaGetYear">NON_ECMA_GET_YEAR</seealso>
		/// <seealso cref="LanguageFeatures.MemberExprAsFunctionName">FEATURE_MEMBER_EXPR_AS_FUNCTION_NAME</seealso>
		/// <seealso cref="LanguageFeatures.ReservedKeywordAsIdentifier">FEATURE_RESERVED_KEYWORD_AS_IDENTIFIER</seealso>
		/// <seealso cref="LanguageFeatures.ToStringAsSource">FEATURE_TO_STRING_AS_SOURCE</seealso>
		/// <seealso cref="LanguageFeatures.ParentProtoProperties">FEATURE_PARENT_PROTO_PROPERTIES</seealso>
		/// <seealso cref="LanguageFeatures.E4X">FEATURE_E4X</seealso>
		/// <seealso cref="LanguageFeatures.DynamicScope">DynamicScope</seealso>
		/// <seealso cref="LanguageFeatures.StrictVars">StrictVars</seealso>
		/// <seealso cref="LanguageFeatures.StrictEval">StrictEval</seealso>
		/// <seealso cref="LanguageFeatures.LocationInformationInError">LocationInformationInError</seealso>
		/// <seealso cref="LanguageFeatures.StrictMode">StrictMode</seealso>
		/// <seealso cref="LanguageFeatures.WarningAsError">WarningAsError</seealso>
		/// <seealso cref="LanguageFeatures.EnhancedJavaAccess">EnhancedJavaAccess</seealso>
		public virtual bool HasFeature(LanguageFeatures featureIndex)
		{
			ContextFactory f = GetFactory();
			return f.HasFeature(this, featureIndex);
		}

		/// <summary>
		/// Returns an object which specifies an E4X implementation to use within
		/// this <code>Context</code>.
		/// </summary>
		/// <remarks>
		/// Returns an object which specifies an E4X implementation to use within
		/// this <code>Context</code>. Note that the XMLLib.Factory interface should
		/// be considered experimental.
		/// The default implementation uses the implementation provided by this
		/// <code>Context</code>'s
		/// <see cref="ContextFactory">ContextFactory</see>
		/// .
		/// </remarks>
		/// <returns>
		/// An XMLLib.Factory. Should not return <code>null</code> if
		/// <see cref="LanguageFeatures.E4X">FEATURE_E4X</see>
		/// is enabled. See
		/// <see cref="HasFeature">HasFeature(int)</see>
		/// .
		/// </returns>
		public virtual XMLLib.Factory GetE4xImplementationFactory()
		{
			return GetFactory().GetE4xImplementationFactory();
		}

		/// <summary>
		/// Get threshold of executed instructions counter that triggers call to
		/// <code>observeInstructionCount()</code>.
		/// </summary>
		/// <remarks>
		/// Get threshold of executed instructions counter that triggers call to
		/// <code>observeInstructionCount()</code>.
		/// When the threshold is zero, instruction counting is disabled,
		/// otherwise each time the run-time executes at least the threshold value
		/// of script instructions, <code>observeInstructionCount()</code> will
		/// be called.
		/// </remarks>
		public int GetInstructionObserverThreshold()
		{
			return instructionThreshold;
		}

		/// <summary>
		/// Set threshold of executed instructions counter that triggers call to
		/// <code>observeInstructionCount()</code>.
		/// </summary>
		/// <remarks>
		/// Set threshold of executed instructions counter that triggers call to
		/// <code>observeInstructionCount()</code>.
		/// When the threshold is zero, instruction counting is disabled,
		/// otherwise each time the run-time executes at least the threshold value
		/// of script instructions, <code>observeInstructionCount()</code> will
		/// be called.<p/>
		/// Note that the meaning of "instruction" is not guaranteed to be
		/// consistent between compiled and interpretive modes: executing a given
		/// script or function in the different modes will result in different
		/// instruction counts against the threshold.
		/// <see cref="SetGenerateObserverCount(bool)">SetGenerateObserverCount(bool)</see>
		/// is called with true if
		/// <code>threshold</code> is greater than zero, false otherwise.
		/// </remarks>
		/// <param name="threshold">The instruction threshold</param>
		public void SetInstructionObserverThreshold(int threshold)
		{
			if (@sealed)
			{
				OnSealedMutation();
			}
			if (threshold < 0)
			{
				throw new ArgumentException();
			}
			instructionThreshold = threshold;
			SetGenerateObserverCount(threshold > 0);
		}

		/// <summary>
		/// Turn on or off generation of code with callbacks to
		/// track the count of executed instructions.
		/// </summary>
		/// <remarks>
		/// Turn on or off generation of code with callbacks to
		/// track the count of executed instructions.
		/// Currently only affects JVM byte code generation: this slows down the
		/// generated code, but code generated without the callbacks will not
		/// be counted toward instruction thresholds. Rhino's interpretive
		/// mode does instruction counting without inserting callbacks, so
		/// there is no requirement to compile code differently.
		/// </remarks>
		/// <param name="generateObserverCount">
		/// if true, generated code will contain
		/// calls to accumulate an estimate of the instructions executed.
		/// </param>
		public virtual void SetGenerateObserverCount(bool generateObserverCount)
		{
			this.generateObserverCount = generateObserverCount;
		}

		/// <summary>
		/// Allow application to monitor counter of executed script instructions
		/// in Context subclasses.
		/// </summary>
		/// <remarks>
		/// Allow application to monitor counter of executed script instructions
		/// in Context subclasses.
		/// Run-time calls this when instruction counting is enabled and the counter
		/// reaches limit set by <code>setInstructionObserverThreshold()</code>.
		/// The method is useful to observe long running scripts and if necessary
		/// to terminate them.
		/// <p>
		/// The default implementation calls
		/// <see cref="ContextFactory.ObserveInstructionCount(Context, int)">ContextFactory.ObserveInstructionCount(Context, int)</see>
		/// that allows to customize Context behavior without introducing
		/// Context subclasses.
		/// </remarks>
		/// <param name="instructionCount">
		/// amount of script instruction executed since
		/// last call to <code>observeInstructionCount</code>
		/// </param>
		/// <exception cref="System.Exception">to terminate the script</exception>
		/// <seealso cref="SetOptimizationLevel(int)">SetOptimizationLevel(int)</seealso>
		protected internal virtual void ObserveInstructionCount(int instructionCount)
		{
			ContextFactory f = GetFactory();
			f.ObserveInstructionCount(this, instructionCount);
		}

#if ENHANCED_SECURITY
		/// <summary>Create class loader for generated classes.</summary>
		/// <remarks>
		/// Create class loader for generated classes.
		/// The method calls
		/// <see cref="ContextFactory.CreateClassLoader(ClassLoader)">ContextFactory.CreateClassLoader(Sharpen.ClassLoader)</see>
		/// using the result of
		/// <see cref="GetFactory()">GetFactory()</see>
		/// .
		/// </remarks>
		public virtual GeneratedClassLoader CreateClassLoader(ClassLoader parent)
		{
			ContextFactory f = GetFactory();
			return f.CreateClassLoader(parent);
		}

		public ClassLoader GetApplicationClassLoader()
		{
			if (applicationClassLoader == null)
			{
				ContextFactory f = GetFactory();
				ClassLoader loader = f.GetApplicationClassLoader();
				if (loader == null)
				{
					ClassLoader threadLoader = VMBridge.GetCurrentThreadClassLoader();
					if (threadLoader != null && Kit.TestIfCanLoadRhinoClasses(threadLoader))
					{
						// Thread.getContextClassLoader is not cached since
						// its caching prevents it from GC which may lead to
						// a memory leak and hides updates to
						// Thread.getContextClassLoader
						return threadLoader;
					}
					// Thread.getContextClassLoader can not load Rhino classes,
					// try to use the loader of ContextFactory or Context
					// subclasses.
					Type fClass = f.GetType();
					if (fClass != ScriptRuntime.ContextFactoryClass)
					{
						loader = fClass.GetClassLoader();
					}
					else
					{
						loader = GetType().GetClassLoader();
					}
				}
				applicationClassLoader = loader;
			}
			return applicationClassLoader;
		}

		public void SetApplicationClassLoader(ClassLoader loader)
		{
			if (@sealed)
			{
				OnSealedMutation();
			}
			if (loader == null)
			{
				// restore default behaviour
				applicationClassLoader = null;
				return;
			}
			if (!Kit.TestIfCanLoadRhinoClasses(loader))
			{
				throw new ArgumentException("Loader can not resolve Rhino classes");
			}
			applicationClassLoader = loader;
		}
#endif

		/// <summary>
		/// Internal method that reports an error for missing calls to
		/// enter().
		/// </summary>
		/// <remarks>
		/// Internal method that reports an error for missing calls to
		/// enter().
		/// </remarks>
		internal static Context GetContext()
		{
			Context cx = GetCurrentContext();
			if (cx == null)
			{
				throw new Exception("No Context associated with current Thread");
			}
			return cx;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private object CompileImpl(Scriptable scope, TextReader sourceReader, string sourceString, string sourceName, int lineno, object securityDomain, bool returnFunction, Evaluator compiler, ErrorReporter compilationErrorReporter)
		{
			if (sourceName == null)
			{
				sourceName = "unnamed script";
			}
#if ENHANCED_SECURITY
			if (securityDomain != null && GetSecurityController() == null)
			{
				throw new ArgumentException("securityDomain should be null if setSecurityController() was never called");
			}
#endif
			// One of sourceReader or sourceString has to be null
			if (!(sourceReader == null ^ sourceString == null))
			{
				Kit.CodeBug();
			}
			// scope should be given if and only if compiling function
			if (!(scope == null ^ returnFunction))
			{
				Kit.CodeBug();
			}
			var compilerEnv = new CompilerEnvirons(this);
			if (compilationErrorReporter == null)
			{
				compilationErrorReporter = compilerEnv.ErrorReporter;
			}
			if (debugger != null)
			{
				if (sourceReader != null)
				{
					sourceString = sourceReader.ReadToEnd();
					sourceReader = null;
				}
			}
			Parser p = new Parser(compilerEnv, compilationErrorReporter);
			if (returnFunction)
			{
				p.calledByCompileFunction = true;
			}
			
			AstRoot ast = sourceString != null
				? p.Parse(sourceString, sourceName, lineno)
				: p.Parse(sourceReader, sourceName, lineno);

			if (returnFunction)
			{
				// parser no longer adds function to script node
				if (!(ast.GetFirstChild() != null && ast.GetFirstChild().GetType() == Token.FUNCTION))
				{
					// XXX: the check just looks for the first child
					// and allows for more nodes after it for compatibility
					// with sources like function() {};;;
					throw new ArgumentException("compileFunction only accepts source with single JS function: " + sourceString);
				}
			}
			if (compiler == null)
			{
				compiler = CreateCompiler();
			}
			//TODO: It is a dirrrrrty hack as I do not want to rewrite try-catch-finally
			//TODO: Delegate creation of IRFactory to Evaluator OR
			IRFactory irf;
			if (compiler is Codegen)
			{
				irf = new IRFactoryNet(compilerEnv, compilationErrorReporter);
			}
			else
			{
				irf = new IRFactory(compilerEnv, compilationErrorReporter);
			}

			ScriptNode tree = irf.TransformTree(ast);
			
			Action<object> debuggerNotificationAction = o => NotifyDebugger(sourceString, o);
			object result;
			if (returnFunction)
			{
				result = compiler.CreateFunctionObject(compilerEnv, tree, this, scope, securityDomain, debuggerNotificationAction);
			}
			else
			{
				result = compiler.CreateScriptObject(compilerEnv, tree, securityDomain, debuggerNotificationAction);
			}
			return result;
		}

		private void NotifyDebugger(string sourceString, object bytecode)
		{
			if (debugger != null)
			{
				if (sourceString == null)
				{
					Kit.CodeBug();
				}
				var dscript = bytecode as DebuggableScript;
				if (dscript != null)
				{
					NotifyDebugger_r(this, dscript, sourceString);
				}
				else
				{
					throw new Exception("NOT SUPPORTED");
				}
			}
		}

		private static void NotifyDebugger_r(Context cx, DebuggableScript dscript, string debugSource)
		{
			cx.debugger.HandleCompilationDone(cx, dscript, debugSource);
			for (int i = 0; i != dscript.GetFunctionCount(); ++i)
			{
				NotifyDebugger_r(cx, dscript.GetFunction(i), debugSource);
			}
		}

		private static Type codegenClass = Kit.ClassOrNull("Rhino.Optimizer.Codegen");

		private static Type interpreterClass = Kit.ClassOrNull("Rhino.Interpreter");

		private Evaluator CreateCompiler()
		{
			Evaluator result = null;
			if (optimizationLevel >= 0 && codegenClass != null)
			{
				result = (Evaluator)Kit.NewInstanceOrNull(codegenClass);
			}
			if (result == null)
			{
				result = CreateInterpreter();
			}
			return result;
		}

		internal static Evaluator CreateInterpreter()
		{
			return (Evaluator)Kit.NewInstanceOrNull(interpreterClass);
		}

		internal static string GetSourcePositionFromStack(int[] linep)
		{
			Context cx = GetCurrentContext();
			if (cx == null)
			{
				return null;
			}
			if (cx.lastInterpreterFrame != null)
			{
				Evaluator evaluator = CreateInterpreter();
				if (evaluator != null)
				{
					return evaluator.GetSourcePositionFromStack(cx, linep);
				}
			}
			string s = new Exception().ToString();
			int open = -1;
			int close = -1;
			int colon = -1;
			for (int i = 0; i < s.Length; i++)
			{
				char c = s[i];
				if (c == ':')
				{
					colon = i;
				}
				else
				{
					if (c == '(')
					{
						open = i;
					}
					else
					{
						if (c == ')')
						{
							close = i;
						}
						else
						{
							if (c == '\n' && open != -1 && close != -1 && colon != -1 && open < colon && colon < close)
							{
								string fileStr = s.Substring(open + 1, colon - (open + 1));
								if (!fileStr.EndsWith(".java"))
								{
									string lineStr = s.Substring(colon + 1, close - (colon + 1));
									try
									{
										linep[0] = System.Convert.ToInt32(lineStr);
										if (linep[0] < 0)
										{
											linep[0] = 0;
										}
										return fileStr;
									}
									catch (FormatException)
									{
									}
								}
								// fall through
								open = close = colon = -1;
							}
						}
					}
				}
			}
			return null;
		}

		internal virtual RegExpProxy GetRegExpProxy()
		{
			if (regExpProxy == null)
			{
				Type cl = Kit.ClassOrNull("Rhino.RegExp.RegExpImpl");
				if (cl != null)
				{
					regExpProxy = (RegExpProxy)Kit.NewInstanceOrNull(cl);
				}
			}
			return regExpProxy;
		}

		internal bool IsVersionECMA1()
		{
			return version == LanguageVersion.VERSION_DEFAULT || version >= LanguageVersion.VERSION_1_3;
		}

#if ENHANCED_SECURITY
		// The method must NOT be public or protected
		internal virtual SecurityController GetSecurityController()
		{
			SecurityController global = SecurityController.Global();
			if (global != null)
			{
				return global;
			}
			return securityController;
		}
#endif

		public bool IsGeneratingDebugChanged()
		{
			return generatingDebugChanged;
		}

		/// <summary>
		/// Add a name to the list of names forcing the creation of real
		/// activation objects for functions.
		/// </summary>
		/// <remarks>
		/// Add a name to the list of names forcing the creation of real
		/// activation objects for functions.
		/// </remarks>
		/// <param name="name">the name of the object to add to the list</param>
		public virtual void AddActivationName(string name)
		{
			if (@sealed)
			{
				OnSealedMutation();
			}
			if (activationNames == null)
			{
				activationNames = new HashSet<string>();
			}
			activationNames.Add(name);
		}

		/// <summary>
		/// Check whether the name is in the list of names of objects
		/// forcing the creation of activation objects.
		/// </summary>
		/// <remarks>
		/// Check whether the name is in the list of names of objects
		/// forcing the creation of activation objects.
		/// </remarks>
		/// <param name="name">the name of the object to test</param>
		/// <returns>true if an function activation object is needed.</returns>
		public bool IsActivationNeeded(string name)
		{
			return activationNames != null && activationNames.Contains(name);
		}

		/// <summary>
		/// Remove a name from the list of names forcing the creation of real
		/// activation objects for functions.
		/// </summary>
		/// <remarks>
		/// Remove a name from the list of names forcing the creation of real
		/// activation objects for functions.
		/// </remarks>
		/// <param name="name">the name of the object to remove from the list</param>
		public virtual void RemoveActivationName(string name)
		{
			if (@sealed)
			{
				OnSealedMutation();
			}
			if (activationNames != null)
			{
				activationNames.Remove(name);
			}
		}

		private static string implementationVersion;

		private readonly ContextFactory factory;

		private bool @sealed;

		private object sealKey;

		internal Scriptable topCallScope;

		internal bool isContinuationsTopCall;

		internal NativeCall currentActivationCall;

		internal XMLLib cachedXMLLib;

		internal ObjToIntMap iterating;

		internal object interpreterSecurityDomain;

		internal LanguageVersion version;

#if ENHANCED_SECURITY
		private SecurityController securityController;
#endif

		private bool hasClassShutter;

		private ClassShutter classShutter;

		private ErrorReporter errorReporter;

		internal RegExpProxy regExpProxy;

		private CultureInfo locale;

		private bool generatingDebug;

		private bool generatingDebugChanged;

		private bool generatingSource = true;

		internal bool useDynamicScope;

		private int optimizationLevel;

		private int maximumInterpreterStackDepth;

		private WrapFactory wrapFactory;

		internal Debugger debugger;

		private object debuggerData;

		private int enterCount;

		private object propertyListeners;

		private IDictionary<object, object> threadLocalMap;

		private ClassLoader applicationClassLoader;

		/// <summary>
		/// This is the list of names of objects forcing the creation of
		/// function activation records.
		/// </summary>
		/// <remarks>
		/// This is the list of names of objects forcing the creation of
		/// function activation records.
		/// </remarks>
		internal ICollection<string> activationNames;

		internal object lastInterpreterFrame;

		internal ObjArray previousInterpreterInvocations;

		internal int instructionCount;

		internal int instructionThreshold;

		internal int scratchIndex;

		internal long scratchUint32;

		internal Scriptable scratchScriptable;

		public bool generateObserverCount = false;
		// for Objects, Arrays to tag themselves as being printed out,
		// so they don't print themselves out recursively.
		// Use ObjToIntMap instead of java.util.HashSet for JDK 1.1 compatibility
		// For the interpreter to store the last frame for error reports etc.
		// For the interpreter to store information about previous invocations
		// interpreter invocations
		// For instruction counting (interpreter only)
		// It can be used to return the second index-like result from function
		// It can be used to return the second uint32 result from function
		// It can be used to return the second Scriptable result from function
		// Generate an observer count on compiled code
	}
}
