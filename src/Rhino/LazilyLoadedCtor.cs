/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Reflection;
using System.Security;
using Rhino;
using Sharpen;

namespace Rhino
{
	/// <summary>Avoid loading classes unless they are used.</summary>
	/// <remarks>
	/// Avoid loading classes unless they are used.
	/// <p> This improves startup time and average memory usage.
	/// </remarks>
	[System.Serializable]
	public sealed class LazilyLoadedCtor
	{
		private const int STATE_BEFORE_INIT = 0;

		private const int STATE_INITIALIZING = 1;

		private const int STATE_WITH_VALUE = 2;

		private readonly ScriptableObject scope;

		private readonly string propertyName;

		private readonly string className;

		private readonly bool @sealed;

		private readonly bool privileged;

		private object initializedValue;

		private int state;

		public LazilyLoadedCtor(ScriptableObject scope, string propertyName, string className, bool @sealed) : this(scope, propertyName, className, @sealed, false)
		{
		}

		internal LazilyLoadedCtor(ScriptableObject scope, string propertyName, string className, bool @sealed, bool privileged)
		{
			this.scope = scope;
			this.propertyName = propertyName;
			this.className = className;
			this.@sealed = @sealed;
			this.privileged = privileged;
			this.state = STATE_BEFORE_INIT;
			scope.AddLazilyInitializedValue(propertyName, 0, this, PropertyAttributes.DONTENUM);
		}

		internal void Init()
		{
			lock (this)
			{
				if (state == STATE_INITIALIZING)
				{
					throw new InvalidOperationException("Recursive initialization for " + propertyName);
				}
				if (state == STATE_BEFORE_INIT)
				{
					state = STATE_INITIALIZING;
					// Set value now to have something to set in finally block if
					// buildValue throws.
					object value = ScriptableConstants.NOT_FOUND;
					try
					{
						value = BuildValue();
					}
					finally
					{
						initializedValue = value;
						state = STATE_WITH_VALUE;
					}
				}
			}
		}

		internal object GetValue()
		{
			if (state != STATE_WITH_VALUE)
			{
				throw new InvalidOperationException(propertyName);
			}
			return initializedValue;
		}

		private object BuildValue()
		{
			if (privileged)
			{
				return AccessController.DoPrivileged(new _PrivilegedAction_87(this));
			}
			else
			{
				return BuildValue0();
			}
		}

		private sealed class _PrivilegedAction_87 : PrivilegedAction<object>
		{
			public _PrivilegedAction_87(LazilyLoadedCtor _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public object Run()
			{
				return this._enclosing.BuildValue0();
			}

			private readonly LazilyLoadedCtor _enclosing;
		}

		private object BuildValue0()
		{
			Type cl = Cast(Kit.ClassOrNull(className));
			if (cl != null)
			{
				try
				{
					object value = ScriptableObject.BuildClassCtor(scope, cl, @sealed, false);
					if (value != null)
					{
						return value;
					}
					else
					{
						// cl has own static initializer which is expected
						// to set the property on its own.
						value = scope.Get(propertyName, scope);
						if (value != ScriptableConstants.NOT_FOUND)
						{
							return value;
						}
					}
				}
				catch (TargetInvocationException ex)
				{
					Exception target = ex.InnerException;
					if (target is Exception)
					{
						throw (Exception)target;
					}
				}
				catch (RhinoException)
				{
				}
				catch (InstantiationException)
				{
				}
				catch (MemberAccessException)
				{
				}
				catch (SecurityException)
				{
				}
			}
			return ScriptableConstants.NOT_FOUND;
		}

		private Type Cast(Type cl)
		{
			return (Type)cl;
		}
	}
}
