/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.IO;
using System.Reflection;
using System.Security;
using System.Text;
using Rhino;
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
	[System.Serializable]
	internal sealed class MemberBox
	{
		internal const long serialVersionUID = 6358550398665688245L;

		[System.NonSerialized]
		private MemberInfo memberObject;

		[System.NonSerialized]
		internal Type[] argTypes;

		[System.NonSerialized]
		internal object delegateTo;

		[System.NonSerialized]
		internal bool vararg;

		internal MemberBox(MethodInfo method)
		{
			Init(method);
		}

		internal MemberBox(ConstructorInfo<object> constructor)
		{
			Init(constructor);
		}

		private void Init(MethodInfo method)
		{
			this.memberObject = method;
			this.argTypes = Sharpen.Runtime.GetParameterTypes(method);
			this.vararg = VMBridge.instance.IsVarArgs(method);
		}

		private void Init<_T0>(ConstructorInfo<_T0> constructor)
		{
			this.memberObject = constructor;
			this.argTypes = constructor.GetParameterTypes();
			this.vararg = VMBridge.instance.IsVarArgs(constructor);
		}

		internal MethodInfo Method()
		{
			return (MethodInfo)memberObject;
		}

		internal ConstructorInfo<object> Ctor()
		{
			return (ConstructorInfo<object>)memberObject;
		}

		internal MemberInfo Member()
		{
			return memberObject;
		}

		internal bool IsMethod()
		{
			return memberObject is MethodInfo;
		}

		internal bool IsCtor()
		{
			return memberObject is ConstructorInfo;
		}

		internal bool IsStatic()
		{
			return Modifier.IsStatic(memberObject.Attributes);
		}

		internal string GetName()
		{
			return memberObject.Name;
		}

		internal Type GetDeclaringClass()
		{
			return memberObject.DeclaringType;
		}

		internal string ToJavaDeclaration()
		{
			StringBuilder sb = new StringBuilder();
			if (IsMethod())
			{
				MethodInfo method = Method();
				sb.Append(method.ReturnType);
				sb.Append(' ');
				sb.Append(method.Name);
			}
			else
			{
				ConstructorInfo<object> ctor = Ctor();
				string name = ((Type)ctor.DeclaringType).FullName;
				int lastDot = name.LastIndexOf('.');
				if (lastDot >= 0)
				{
					name = Sharpen.Runtime.Substring(name, lastDot + 1);
				}
				sb.Append(name);
			}
			sb.Append(JavaMembers.LiveConnectSignature(argTypes));
			return sb.ToString();
		}

		public override string ToString()
		{
			return memberObject.ToString();
		}

		internal object Invoke(object target, object[] args)
		{
			MethodInfo method = Method();
			try
			{
				try
				{
					return method.Invoke(target, args);
				}
				catch (MemberAccessException ex)
				{
					MethodInfo accessible = SearchAccessibleMethod(method, argTypes);
					if (accessible != null)
					{
						memberObject = accessible;
						method = accessible;
					}
					else
					{
						if (!VMBridge.instance.TryToMakeAccessible(method))
						{
							throw Context.ThrowAsScriptRuntimeEx(ex);
						}
					}
					// Retry after recovery
					return method.Invoke(target, args);
				}
			}
			catch (TargetInvocationException ite)
			{
				// Must allow ContinuationPending exceptions to propagate unhindered
				Exception e = ite;
				do
				{
					e = ((TargetInvocationException)e).InnerException;
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
			ConstructorInfo<object> ctor = Ctor();
			try
			{
				try
				{
					return ctor.NewInstance(args);
				}
				catch (MemberAccessException ex)
				{
					if (!VMBridge.instance.TryToMakeAccessible(ctor))
					{
						throw Context.ThrowAsScriptRuntimeEx(ex);
					}
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
			int modifiers = method.Attributes;
			if (Modifier.IsPublic(modifiers) && !Modifier.IsStatic(modifiers))
			{
				Type c = method.DeclaringType;
				if (!Modifier.IsPublic(c.Attributes))
				{
					string name = method.Name;
					Type[] intfs = c.GetInterfaces();
					for (int i = 0, N = intfs.Length; i != N; ++i)
					{
						Type intf = intfs[i];
						if (Modifier.IsPublic(intf.Attributes))
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
						if (Modifier.IsPublic(c.Attributes))
						{
							try
							{
								MethodInfo m = c.GetMethod(name, @params);
								int mModifiers = m.Attributes;
								if (Modifier.IsPublic(mModifiers) && !Modifier.IsStatic(mModifiers))
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
			MemberInfo member = ReadMember(@in);
			if (member is MethodInfo)
			{
				Init((MethodInfo)member);
			}
			else
			{
				Init((ConstructorInfo<object>)member);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteObject(ObjectOutputStream @out)
		{
			@out.DefaultWriteObject();
			WriteMember(@out, memberObject);
		}

		/// <summary>Writes a Constructor or Method object.</summary>
		/// <remarks>
		/// Writes a Constructor or Method object.
		/// Methods and Constructors are not serializable, so we must serialize
		/// information about the class, the name, and the parameters and
		/// recreate upon deserialization.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private static void WriteMember(ObjectOutputStream @out, MemberInfo member)
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
			if (member is MethodInfo)
			{
				WriteParameters(@out, Sharpen.Runtime.GetParameterTypes(((MethodInfo)member)));
			}
			else
			{
				WriteParameters(@out, ((ConstructorInfo<object>)member).GetParameterTypes());
			}
		}

		/// <summary>Reads a Method or a Constructor from the stream.</summary>
		/// <remarks>Reads a Method or a Constructor from the stream.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.TypeLoadException"></exception>
		private static MemberInfo ReadMember(ObjectInputStream @in)
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
			for (int i = 0; i < parms.Length; i++)
			{
				Type parm = parms[i];
				bool primitive = parm.IsPrimitive;
				@out.WriteBoolean(primitive);
				if (!primitive)
				{
					@out.WriteObject(parm);
					continue;
				}
				for (int j = 0; j < primitives.Length; j++)
				{
					if (parm.Equals(primitives[j]))
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
	}
}
