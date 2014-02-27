/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using Sharpen;

namespace Rhino
{
	/// <summary>
	/// Wrappper class for Method and Constructor instances to cache
	/// getParameterTypes() results, recover from IllegalAccessException
	/// in some cases and provide serialization support.
	/// </summary>
	/// <remarks>
	/// Wrappper class for Method and Constructor instances to cache
	/// getParameterTypes() results, recover from IllegalAccessException
	/// in some cases and provide serialization support.
	/// </remarks>
	/// <author>Igor Bukanov</author>
	[Serializable]
	public sealed class MemberBox
	{
		[NonSerialized]
		internal MethodBase method;

		[NonSerialized]
		internal Type[] argTypes;

		[NonSerialized]
		internal object delegateTo;

		[NonSerialized]
		internal bool vararg;

		internal MemberBox(MethodBase m)
		{
			Init(m);
		}

		private void Init(MethodBase m)
		{
			method = m;
			argTypes = m.GetParameters().Select(p => p.ParameterType).ToArray();
			vararg = IsVarArgs(m);
		}

		internal MethodInfo Method()
		{
			return (MethodInfo)method;
		}

		internal ConstructorInfo Ctor()
		{
			return (ConstructorInfo)method;
		}

		internal MethodBase Member()
		{
			return method;
		}

		internal bool IsMethod()
		{
			return !method.IsConstructor;
		}

		internal bool IsCtor()
		{
			return method.IsConstructor;
		}

		internal bool IsStatic()
		{
			return method.IsStatic;
		}

		internal string GetName()
		{
			return method.Name;
		}

		internal Type GetDeclaringClass()
		{
			return method.DeclaringType;
		}

		internal string ToJavaDeclaration()
		{
			StringBuilder sb = new StringBuilder();
			if (IsMethod())
			{
				MethodInfo m = Method();
				sb.Append(m.ReturnType);
				sb.Append(' ');
				sb.Append(m.Name);
			}
			else
			{
				ConstructorInfo ctor = Ctor();
				string name = ctor.DeclaringType.FullName;
				int lastDot = name.LastIndexOf('.');
				if (lastDot >= 0)
				{
					name = name.Substring(lastDot + 1);
				}
				sb.Append(name);
			}
			sb.Append(JavaMembers.LiveConnectSignature(argTypes));
			return sb.ToString();
		}

		public override string ToString()
		{
			return method.ToString();
		}

		internal object Invoke(object target, object[] args)
		{
			MethodInfo m = Method();
			try
			{
				try
				{
					return m.Invoke(target, args);
				}
				catch (MemberAccessException ex)
				{
					MethodInfo accessible = SearchAccessibleMethod(m, argTypes);
					if (accessible != null)
					{
						method = accessible;
						m = accessible;
					}
					// Retry after recovery
					return m.Invoke(target, args);
				}
			}
			catch (TargetInvocationException ite)
			{
				// Must allow ContinuationPending exceptions to propagate unhindered
				Exception e = ite;
				do
				{
					e = e.InnerException;
				}
				while ((e is TargetInvocationException));
				if (e is ContinuationPending)
				{
					throw (ContinuationPending)e;
				}
				throw Context.ThrowAsScriptRuntimeEx(e);
			}
			catch (Exception ex)
			{
				throw Context.ThrowAsScriptRuntimeEx(ex);
			}
		}

		internal object NewInstance(object[] args)
		{
			ConstructorInfo ctor = Ctor();
			try
			{
				try
				{
					return ctor.NewInstance(args);
				}
				catch (MemberAccessException ex)
				{
				}
				return ctor.NewInstance(args);
			}
			catch (Exception ex)
			{
				throw Context.ThrowAsScriptRuntimeEx(ex);
			}
		}

		private static MethodInfo SearchAccessibleMethod(MethodInfo method, Type[] @params)
		{
			if (method.IsPublic && !method.IsStatic)
			{
				Type c = method.DeclaringType;
				if (!c.IsPublic)
				{
					string name = method.Name;
					Type[] intfs = c.GetInterfaces();
					for (int i = 0, N = intfs.Length; i != N; ++i)
					{
						Type intf = intfs[i];
						if (intf.IsPublic)
						{
							try
							{
								return intf.GetMethod(name, @params);
							}
							catch (MissingMethodException)
							{
							}
							catch (SecurityException)
							{
							}
						}
					}
					for (; ; )
					{
						c = c.BaseType;
						if (c == null)
						{
							break;
						}
						if (c.IsPublic)
						{
							try
							{
								MethodInfo m = c.GetMethod(name, @params);
								if (m.IsPublic && !m.IsStatic)
								{
									return m;
								}
							}
							catch (MissingMethodException)
							{
							}
							catch (SecurityException)
							{
							}
						}
					}
				}
			}
			return null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.TypeLoadException"></exception>
		private void ReadObject(ObjectInputStream @in)
		{
			@in.DefaultReadObject();
			var member = (MethodBase) ReadMember(@in);
			Init(member);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteObject(ObjectOutputStream @out)
		{
			@out.DefaultWriteObject();
			WriteMember(@out, method);
		}

		/// <summary>Writes a Constructor or Method object.</summary>
		/// <remarks>
		/// Writes a Constructor or Method object.
		/// Methods and Constructors are not serializable, so we must serialize
		/// information about the class, the name, and the parameters and
		/// recreate upon deserialization.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private static void WriteMember(ObjectOutputStream @out, MethodBase member)
		{
		    if (member == null)
		    {
		        @out.WriteBoolean(false);
		        return;
		    }
		    @out.WriteBoolean(true);
		    if (!(member is MethodInfo || member is ConstructorInfo))
		    {
		        throw new ArgumentException("not Method or Constructor");
		    }
		    @out.WriteBoolean(member is MethodInfo);
		    @out.WriteObject(member.Name);
		    @out.WriteObject(member.DeclaringType);
		    WriteParameters(@out, member.GetParameterTypes());
		}

	    /// <summary>Reads a Method or a Constructor from the stream.</summary>
		/// <remarks>Reads a Method or a Constructor from the stream.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.TypeLoadException"></exception>
		private static MethodBase ReadMember(ObjectInputStream @in)
		{
			if (!@in.ReadBoolean())
			{
				return null;
			}
			bool isMethod = @in.ReadBoolean();
			string name = (string)@in.ReadObject();
			Type declaring = (Type)@in.ReadObject();
			Type[] parms = ReadParameters(@in);
			try
			{
				if (isMethod)
				{
					return declaring.GetMethod(name, parms);
				}
				else
				{
					return declaring.GetConstructor(parms);
				}
			}
			catch (MissingMethodException e)
			{
				throw new IOException("Cannot find member: " + e);
			}
		}

		private static readonly Type[] primitives = new Type[] { typeof(bool), typeof(byte), typeof(char), typeof(double), typeof(float), typeof(int), typeof(long), typeof(short), typeof(void) };

		/// <summary>Writes an array of parameter types to the stream.</summary>
		/// <remarks>
		/// Writes an array of parameter types to the stream.
		/// Requires special handling because primitive types cannot be
		/// found upon deserialization by the default Java implementation.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private static void WriteParameters(ObjectOutputStream @out, Type[] parms)
		{
			@out.WriteShort(parms.Length);
			foreach (var parm in parms)
			{
				bool primitive = parm.IsPrimitive;
				@out.WriteBoolean(primitive);
				if (!primitive)
				{
					@out.WriteObject(parm);
					continue;
				}
				for (int j = 0; j < primitives.Length; j++)
				{
					if (parm == primitives[j])
					{
						@out.WriteByte(j);
						goto outer_continue;
					}
				}
				throw new ArgumentException("Primitive " + parm + " not found");
				outer_continue: ;
			}
outer_break: ;
		}

		/// <summary>Reads an array of parameter types from the stream.</summary>
		/// <remarks>Reads an array of parameter types from the stream.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.TypeLoadException"></exception>
		private static Type[] ReadParameters(ObjectInputStream @in)
		{
			Type[] result = new Type[@in.ReadShort()];
			for (int i = 0; i < result.Length; i++)
			{
				if (!@in.ReadBoolean())
				{
					result[i] = (Type)@in.ReadObject();
					continue;
				}
				result[i] = primitives[@in.ReadByte()];
			}
			return result;
		}

		private static bool IsVarArgs(MethodBase member)
		{
			foreach (ParameterInfo parameterInfo in member.GetParameters().Reverse())
			{
				var attributes = parameterInfo.GetCustomAttributes(typeof (ParamArrayAttribute));
				return attributes != null && attributes.Any();
			}
			return false;
		}
	}
}
