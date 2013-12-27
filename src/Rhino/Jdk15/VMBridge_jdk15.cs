/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Rhino;
using Rhino.Jdk13;
using Sharpen;

namespace Rhino.Jdk15
{
	public class VMBridge_jdk15 : VMBridge_jdk13
	{
		/// <exception cref="System.Security.SecurityException"></exception>
		/// <exception cref="Sharpen.InstantiationException"></exception>
		public VMBridge_jdk15()
		{
			try
			{
				// Just try and see if we can access the isVarArgs method.
				// We want to fail loading if the method does not exist
				// so that we can load a bridge to an older JDK instead.
				typeof(MethodInfo).GetMethod("isVarArgs", (Type[])null);
			}
			catch (MissingMethodException e)
			{
				// Throw a fitting exception that is handled by
				// Rhino.Kit.newInstanceOrNull:
				throw new InstantiationException(e.Message);
			}
		}

		protected internal override bool IsVarArgs(MemberInfo member)
		{
			if (member is MethodInfo)
			{
				return ((MethodInfo)member).IsVarArgs();
			}
			else
			{
				if (member is ConstructorInfo)
				{
					return ((ConstructorInfo)member).IsVarArgs();
				}
				else
				{
					return false;
				}
			}
		}

		/// <summary>
		/// If "obj" is a java.util.Iterator or a java.lang.Iterable, return a
		/// wrapping as a JavaScript Iterator.
		/// </summary>
		/// <remarks>
		/// If "obj" is a java.util.Iterator or a java.lang.Iterable, return a
		/// wrapping as a JavaScript Iterator. Otherwise, return null.
		/// This method is in VMBridge since Iterable is a JDK 1.5 addition.
		/// </remarks>
		public override IEnumerator<object> GetJavaIterator(Context cx, Scriptable scope, object obj)
		{
			if (obj is Wrapper)
			{
				object unwrapped = ((Wrapper)obj).Unwrap();
				IEnumerator<object> iterator = null;
				if (unwrapped is IEnumerator)
				{
					iterator = (IEnumerator<object>)unwrapped;
				}
				if (unwrapped is IEnumerable)
				{
					iterator = ((IEnumerable<object>)unwrapped).GetEnumerator();
				}
				return iterator;
			}
			return null;
		}
	}
}
