/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Rhino;
using Rhino.Xml;
using Sharpen;

namespace Rhino
{
	/// <summary>
	/// Factory class that Rhino runtime uses to create new
	/// <see cref="Context">Context</see>
	/// instances.  A <code>ContextFactory</code> can also notify listeners
	/// about context creation and release.
	/// <p>
	/// When the Rhino runtime needs to create new
	/// <see cref="Context">Context</see>
	/// instance during
	/// execution of
	/// <see cref="Context.Enter()">Context.Enter()</see>
	/// or
	/// <see cref="Context">Context</see>
	/// , it will call
	/// <see cref="MakeContext()">MakeContext()</see>
	/// of the current global ContextFactory.
	/// See
	/// <see cref="GetGlobal()">GetGlobal()</see>
	/// and
	/// <see cref="InitGlobal(ContextFactory)">InitGlobal(ContextFactory)</see>
	/// .
	/// <p>
	/// It is also possible to use explicit ContextFactory instances for Context
	/// creation. This is useful to have a set of independent Rhino runtime
	/// instances under single JVM. See
	/// <see cref="Call(ContextAction)">Call(ContextAction)</see>
	/// .
	/// <p>
	/// The following example demonstrates Context customization to terminate
	/// scripts running more then 10 seconds and to provide better compatibility
	/// with JavaScript code using MSIE-specific features.
	/// <pre>
	/// import Rhino.*;
	/// class MyFactory extends ContextFactory
	/// {
	/// // Custom
	/// <see cref="Context">Context</see>
	/// to store execution time.
	/// private static class MyContext extends Context
	/// {
	/// long startTime;
	/// }
	/// static {
	/// // Initialize GlobalFactory with custom factory
	/// ContextFactory.initGlobal(new MyFactory());
	/// }
	/// // Override
	/// <see cref="MakeContext()">MakeContext()</see>
	/// protected Context makeContext()
	/// {
	/// MyContext cx = new MyContext();
	/// // Make Rhino runtime to call observeInstructionCount
	/// // each 10000 bytecode instructions
	/// cx.setInstructionObserverThreshold(10000);
	/// return cx;
	/// }
	/// // Override
	/// <see cref="HasFeature">HasFeature(Context, int)</see>
	/// public boolean hasFeature(Context cx, int featureIndex)
	/// {
	/// // Turn on maximum compatibility with MSIE scripts
	/// switch (featureIndex) {
	/// case
	/// <see cref="LanguageFeatures.NonEcmaGetYear">Context.NON_ECMA_GET_YEAR</see>
	/// :
	/// return true;
	/// case
	/// <see cref="LanguageFeatures.MemberExprAsFunctionName">Context.FEATURE_MEMBER_EXPR_AS_FUNCTION_NAME</see>
	/// :
	/// return true;
	/// case
	/// <see cref="LanguageFeatures.ReservedKeywordAsIdentifier">Context.FEATURE_RESERVED_KEYWORD_AS_IDENTIFIER</see>
	/// :
	/// return true;
	/// case
	/// <see cref="LanguageFeatures.ParentProtoProperties">Context.FEATURE_PARENT_PROTO_PROPERTIES</see>
	/// :
	/// return false;
	/// }
	/// return super.hasFeature(cx, featureIndex);
	/// }
	/// // Override
	/// <see cref="ObserveInstructionCount(Context, int)">ObserveInstructionCount(Context, int)</see>
	/// protected void observeInstructionCount(Context cx, int instructionCount)
	/// {
	/// MyContext mcx = (MyContext)cx;
	/// long currentTime = System.currentTimeMillis();
	/// if (currentTime - mcx.startTime &gt; 10*1000) {
	/// // More then 10 seconds from Context creation time:
	/// // it is time to stop the script.
	/// // Throw Error instance to ensure that script will never
	/// // get control back through catch or finally.
	/// throw new Error();
	/// }
	/// }
	/// // Override
	/// <see cref="DoTopCall(Callable, Context, Scriptable, Scriptable, object[])">DoTopCall(Callable, Context, Scriptable, Scriptable, object[])</see>
	/// protected Object doTopCall(Callable callable,
	/// Context cx, Scriptable scope,
	/// Scriptable thisObj, Object[] args)
	/// {
	/// MyContext mcx = (MyContext)cx;
	/// mcx.startTime = System.currentTimeMillis();
	/// return super.doTopCall(callable, cx, scope, thisObj, args);
	/// }
	/// }
	/// </pre>
	/// </summary>
	public class ContextFactory
	{
		private static volatile bool hasCustomGlobal;

		private static ContextFactory global = new ContextFactory();

		private volatile bool @sealed;

		private readonly object listenersLock = new object();

		private volatile object listeners;

		private bool disabledListening;

		private ClassLoader applicationClassLoader;

		/// <summary>
		/// Listener of
		/// <see cref="Context">Context</see>
		/// creation and release events.
		/// </summary>
		public interface Listener
		{
			// API class
			/// <summary>
			/// Notify about newly created
			/// <see cref="Context">Context</see>
			/// object.
			/// </summary>
			void ContextCreated(Context cx);

			/// <summary>
			/// Notify that the specified
			/// <see cref="Context">Context</see>
			/// instance is no longer
			/// associated with the current thread.
			/// </summary>
			void ContextReleased(Context cx);
		}

		/// <summary>Get global ContextFactory.</summary>
		/// <remarks>Get global ContextFactory.</remarks>
		/// <seealso cref="HasExplicitGlobal()">HasExplicitGlobal()</seealso>
		/// <seealso cref="InitGlobal(ContextFactory)">InitGlobal(ContextFactory)</seealso>
		public static ContextFactory GetGlobal()
		{
			return global;
		}

		/// <summary>Check if global factory was set.</summary>
		/// <remarks>
		/// Check if global factory was set.
		/// Return true to indicate that
		/// <see cref="InitGlobal(ContextFactory)">InitGlobal(ContextFactory)</see>
		/// was
		/// already called and false to indicate that the global factory was not
		/// explicitly set.
		/// </remarks>
		/// <seealso cref="GetGlobal()">GetGlobal()</seealso>
		/// <seealso cref="InitGlobal(ContextFactory)">InitGlobal(ContextFactory)</seealso>
		public static bool HasExplicitGlobal()
		{
			return hasCustomGlobal;
		}

		/// <summary>Set global ContextFactory.</summary>
		/// <remarks>
		/// Set global ContextFactory.
		/// The method can only be called once.
		/// </remarks>
		/// <seealso cref="GetGlobal()">GetGlobal()</seealso>
		/// <seealso cref="HasExplicitGlobal()">HasExplicitGlobal()</seealso>
		public static void InitGlobal(ContextFactory factory)
		{
			lock (typeof(ContextFactory))
			{
				if (factory == null)
				{
					throw new ArgumentException();
				}
				if (hasCustomGlobal)
				{
					throw new InvalidOperationException();
				}
				hasCustomGlobal = true;
				global = factory;
			}
		}

		public interface GlobalSetter
		{
			void SetContextFactoryGlobal(ContextFactory factory);

			ContextFactory GetContextFactoryGlobal();
		}

		public static ContextFactory.GlobalSetter GetGlobalSetter()
		{
			lock (typeof(ContextFactory))
			{
				if (hasCustomGlobal)
				{
					throw new InvalidOperationException();
				}
				hasCustomGlobal = true;
				return new GlobalSetterImpl();
			}
		}

		private sealed class GlobalSetterImpl : GlobalSetter
		{
			public void SetContextFactoryGlobal(ContextFactory factory)
			{
				global = factory ?? new ContextFactory();
			}

			public ContextFactory GetContextFactoryGlobal()
			{
				return global;
			}
		}

		/// <summary>
		/// Create new
		/// <see cref="Context">Context</see>
		/// instance to be associated with the current
		/// thread.
		/// This is a callback method used by Rhino to create
		/// <see cref="Context">Context</see>
		/// instance when it is necessary to associate one with the current
		/// execution thread. <tt>makeContext()</tt> is allowed to call
		/// <see cref="Context.Seal(object)">Context.Seal(object)</see>
		/// on the result to prevent
		/// <see cref="Context">Context</see>
		/// changes by hostile scripts or applets.
		/// </summary>
		protected internal virtual Context MakeContext()
		{
			return new Context(this);
		}

		/// <summary>
		/// Implementation of
		/// <see cref="Context.HasFeature">Context.HasFeature(int)</see>
		/// .
		/// This can be used to customize
		/// <see cref="Context">Context</see>
		/// without introducing
		/// additional subclasses.
		/// </summary>
		protected internal virtual bool HasFeature(Context cx, LanguageFeatures featureIndex)
		{
			LanguageVersion version;
			switch (featureIndex)
			{
				case LanguageFeatures.NonEcmaGetYear:
				{
					version = cx.GetLanguageVersion();
					return (version == LanguageVersion.VERSION_1_0 || version == LanguageVersion.VERSION_1_1 || version == LanguageVersion.VERSION_1_2);
				}

				case LanguageFeatures.MemberExprAsFunctionName:
				{
					return false;
				}

				case LanguageFeatures.ReservedKeywordAsIdentifier:
				{
					return true;
				}

				case LanguageFeatures.ToStringAsSource:
				{
					version = cx.GetLanguageVersion();
					return version == LanguageVersion.VERSION_1_2;
				}

				case LanguageFeatures.ParentProtoProperties:
				{
					return true;
				}

				case LanguageFeatures.E4X:
				{
					version = cx.GetLanguageVersion();
					return (version == LanguageVersion.VERSION_DEFAULT || version >= LanguageVersion.VERSION_1_6);
				}

				case LanguageFeatures.DynamicScope:
				{
					return false;
				}

				case LanguageFeatures.StrictVars:
				{
					return false;
				}

				case LanguageFeatures.StrictEval:
				{
					return false;
				}

				case LanguageFeatures.LocationInformationInError:
				{
					return false;
				}

				case LanguageFeatures.StrictMode:
				{
					return false;
				}

				case LanguageFeatures.WarningAsError:
				{
					return false;
				}

				case LanguageFeatures.EnhancedJavaAccess:
				{
					return false;
				}
			}
			// It is a bug to call the method with unknown featureIndex
			throw new ArgumentException(featureIndex.ToString());
		}

		private bool IsDom3Present()
		{
			Type nodeClass = Kit.ClassOrNull("org.w3c.dom.Node");
			if (nodeClass == null)
			{
				return false;
			}
			// Check to see whether DOM3 is present; use a new method defined in
			// DOM3 that is vital to our implementation
			try
			{
				nodeClass.GetMethod("getUserData", new Type[] { typeof(string) });
				return true;
			}
			catch (MissingMethodException)
			{
				return false;
			}
		}

		/// <summary>
		/// Provides a default
		/// <see cref="Rhino.Xml.XMLLib.Factory">XMLLib.Factory</see>
		/// to be used by the <code>Context</code> instances produced by this
		/// factory. See
		/// <see cref="Context.GetE4xImplementationFactory()">Context.GetE4xImplementationFactory()</see>
		/// for details.
		/// May return null, in which case E4X functionality is not supported in
		/// Rhino.
		/// The default implementation now prefers the DOM3 E4X implementation.
		/// </summary>
		protected internal virtual XMLLib.Factory GetE4xImplementationFactory()
		{
			// Must provide default implementation, rather than abstract method,
			// so that past implementors of ContextFactory do not fail at runtime
			// upon invocation of this method.
			// Note that the default implementation returns null if we
			// neither have XMLBeans nor a DOM3 implementation present.
			if (IsDom3Present())
			{
				return XMLLib.Factory.Create("Rhino.XmlImpl.XMLLibImpl");
			}
			else
			{
				if (Kit.ClassOrNull("org.apache.xmlbeans.XmlCursor") != null)
				{
					return XMLLib.Factory.Create("Rhino.Xml.impl.xmlbeans.XMLLibImpl");
				}
				else
				{
					return null;
				}
			}
		}

#if ENHANCED_SECURITY
		/// <summary>Create class loader for generated classes.</summary>
		/// <remarks>
		/// Create class loader for generated classes.
		/// This method creates an instance of the default implementation
		/// of
		/// <see cref="GeneratedClassLoader">GeneratedClassLoader</see>
		/// . Rhino uses this interface to load
		/// generated JVM classes when no
		/// <see cref="SecurityController">SecurityController</see>
		/// is installed.
		/// Application can override the method to provide custom class loading.
		/// </remarks>
		protected internal virtual GeneratedClassLoader CreateClassLoader(ClassLoader parent)
		{
			return AccessController.DoPrivileged(new _PrivilegedAction_344(parent));
		}
		
		private sealed class _PrivilegedAction_344 : PrivilegedAction<DefiningClassLoader>
		{
			public _PrivilegedAction_344(ClassLoader parent)
			{
				this.parent = parent;
			}

			public DefiningClassLoader Run()
			{
				return new DefiningClassLoader(parent);
			}

			private readonly ClassLoader parent;
		}
#endif

		/// <summary>Get ClassLoader to use when searching for Java classes.</summary>
		/// <remarks>
		/// Get ClassLoader to use when searching for Java classes.
		/// Unless it was explicitly initialized with
		/// <see cref="InitApplicationClassLoader(ClassLoader)">InitApplicationClassLoader(Sharpen.ClassLoader)</see>
		/// the method returns
		/// null to indicate that Thread.getContextClassLoader() should be used.
		/// </remarks>
		public ClassLoader GetApplicationClassLoader()
		{
			return applicationClassLoader;
		}

		/// <summary>Set explicit class loader to use when searching for Java classes.</summary>
		/// <remarks>Set explicit class loader to use when searching for Java classes.</remarks>
		/// <seealso cref="GetApplicationClassLoader()">GetApplicationClassLoader()</seealso>
		public void InitApplicationClassLoader(ClassLoader loader)
		{
			if (loader == null)
			{
				throw new ArgumentException("loader is null");
			}
			if (!Kit.TestIfCanLoadRhinoClasses(loader))
			{
				throw new ArgumentException("Loader can not resolve Rhino classes");
			}
			if (this.applicationClassLoader != null)
			{
				throw new InvalidOperationException("applicationClassLoader can only be set once");
			}
			CheckNotSealed();
			this.applicationClassLoader = loader;
		}

		/// <summary>Execute top call to script or function.</summary>
		/// <remarks>
		/// Execute top call to script or function.
		/// When the runtime is about to execute a script or function that will
		/// create the first stack frame with scriptable code, it calls this method
		/// to perform the real call. In this way execution of any script
		/// happens inside this function.
		/// </remarks>
		protected internal virtual object DoTopCall(Callable callable, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			object result = callable.Call(cx, scope, thisObj, args);
			return result;
		}

		/// <summary>
		/// Implementation of
		/// <see cref="Context.ObserveInstructionCount(int)">Context.ObserveInstructionCount(int)</see>
		/// .
		/// This can be used to customize
		/// <see cref="Context">Context</see>
		/// without introducing
		/// additional subclasses.
		/// </summary>
		protected internal virtual void ObserveInstructionCount(Context cx, int instructionCount)
		{
		}

		protected internal virtual void OnContextCreated(Context cx)
		{
			object listeners = this.listeners;
			for (int i = 0; ; ++i)
			{
				ContextFactory.Listener l = (ContextFactory.Listener)Kit.GetListener(listeners, i);
				if (l == null)
				{
					break;
				}
				l.ContextCreated(cx);
			}
		}

		protected internal virtual void OnContextReleased(Context cx)
		{
			object listeners = this.listeners;
			for (int i = 0; ; ++i)
			{
				ContextFactory.Listener l = (ContextFactory.Listener)Kit.GetListener(listeners, i);
				if (l == null)
				{
					break;
				}
				l.ContextReleased(cx);
			}
		}

		public void AddListener(ContextFactory.Listener listener)
		{
			CheckNotSealed();
			lock (listenersLock)
			{
				if (disabledListening)
				{
					throw new InvalidOperationException();
				}
				listeners = Kit.AddListener(listeners, listener);
			}
		}

		public void RemoveListener(ContextFactory.Listener listener)
		{
			CheckNotSealed();
			lock (listenersLock)
			{
				if (disabledListening)
				{
					throw new InvalidOperationException();
				}
				listeners = Kit.RemoveListener(listeners, listener);
			}
		}

		/// <summary>
		/// The method is used only to implement
		/// Context.disableStaticContextListening()
		/// </summary>
		internal void DisableContextListening()
		{
			CheckNotSealed();
			lock (listenersLock)
			{
				disabledListening = true;
				listeners = null;
			}
		}

		/// <summary>Checks if this is a sealed ContextFactory.</summary>
		/// <remarks>Checks if this is a sealed ContextFactory.</remarks>
		/// <seealso cref="Seal()">Seal()</seealso>
		public bool IsSealed()
		{
			return @sealed;
		}

		/// <summary>
		/// Seal this ContextFactory so any attempt to modify it like to add or
		/// remove its listeners will throw an exception.
		/// </summary>
		/// <remarks>
		/// Seal this ContextFactory so any attempt to modify it like to add or
		/// remove its listeners will throw an exception.
		/// </remarks>
		/// <seealso cref="IsSealed()">IsSealed()</seealso>
		public void Seal()
		{
			CheckNotSealed();
			@sealed = true;
		}

		protected internal void CheckNotSealed()
		{
			if (@sealed)
			{
				throw new InvalidOperationException();
			}
		}

		/// <summary>
		/// Call
		/// <see cref="ContextAction.Run(Context)">ContextAction.Run(Context)</see>
		/// using the
		/// <see cref="Context">Context</see>
		/// instance associated with the current thread.
		/// If no Context is associated with the thread, then
		/// <see cref="MakeContext()">MakeContext()</see>
		/// will be called to construct
		/// new Context instance. The instance will be temporary associated
		/// with the thread during call to
		/// <see cref="ContextAction.Run(Context)">ContextAction.Run(Context)</see>
		/// .
		/// </summary>
		/// <seealso cref="Call(ContextAction)">Call(ContextAction)</seealso>
		/// <seealso cref="Context.Call(ContextFactory, Callable, Scriptable, Scriptable, object[])">Context.Call(ContextFactory, Callable, Scriptable, Scriptable, object[])</seealso>
		public object Call(ContextAction action)
		{
			return Context.Call(this, action);
		}

		/// <summary>
		/// Get a context associated with the current thread, creating one if need
		/// be.
		/// </summary>
		/// <remarks>
		/// Get a context associated with the current thread, creating one if need
		/// be. The Context stores the execution state of the JavaScript engine, so
		/// it is required that the context be entered before execution may begin.
		/// Once a thread has entered a Context, then getCurrentContext() may be
		/// called to find the context that is associated with the current thread.
		/// <p>
		/// Calling <code>enterContext()</code> will return either the Context
		/// currently associated with the thread, or will create a new context and
		/// associate it with the current thread. Each call to
		/// <code>enterContext()</code> must have a matching call to
		/// <see cref="Context.Exit()">Context.Exit()</see>
		/// .
		/// <pre>
		/// Context cx = contextFactory.enterContext();
		/// try {
		/// ...
		/// cx.evaluateString(...);
		/// } finally {
		/// Context.exit();
		/// }
		/// </pre>
		/// Instead of using <tt>enterContext()</tt>, <tt>exit()</tt> pair consider
		/// using
		/// <see cref="Call(ContextAction)">Call(ContextAction)</see>
		/// which guarantees proper association
		/// of Context instances with the current thread.
		/// With this method the above example becomes:
		/// <pre>
		/// ContextFactory.call(new ContextAction() {
		/// public Object run(Context cx) {
		/// ...
		/// cx.evaluateString(...);
		/// return null;
		/// }
		/// });
		/// </pre>
		/// </remarks>
		/// <returns>a Context associated with the current thread</returns>
		/// <seealso cref="Context.GetCurrentContext()">Context.GetCurrentContext()</seealso>
		/// <seealso cref="Context.Exit()">Context.Exit()</seealso>
		/// <seealso cref="Call(ContextAction)">Call(ContextAction)</seealso>
		public virtual Context EnterContext()
		{
			return EnterContext(null);
		}

		/// <summary>
		/// Get a Context associated with the current thread, using the given
		/// Context if need be.
		/// </summary>
		/// <remarks>
		/// Get a Context associated with the current thread, using the given
		/// Context if need be.
		/// <p>
		/// The same as <code>enterContext()</code> except that <code>cx</code>
		/// is associated with the current thread and returned if the current thread
		/// has no associated context and <code>cx</code> is not associated with any
		/// other thread.
		/// </remarks>
		/// <param name="cx">a Context to associate with the thread if possible</param>
		/// <returns>a Context associated with the current thread</returns>
		/// <seealso cref="EnterContext()">EnterContext()</seealso>
		/// <seealso cref="Call(ContextAction)">Call(ContextAction)</seealso>
		/// <exception cref="System.InvalidOperationException">
		/// if <code>cx</code> is already associated
		/// with a different thread
		/// </exception>
		public Context EnterContext(Context cx)
		{
			return Context.Enter(cx, this);
		}
	}
}
