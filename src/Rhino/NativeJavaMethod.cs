/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Reflection;
using System.Text;
using Rhino;
using Rhino.Utils;
using Sharpen;

namespace Rhino
{
	/// <summary>
	/// This class reflects Java methods into the JavaScript environment and
	/// handles overloading of methods.
	/// </summary>
	/// <remarks>
	/// This class reflects Java methods into the JavaScript environment and
	/// handles overloading of methods.
	/// </remarks>
	/// <author>Mike Shaver</author>
	/// <seealso cref="NativeJavaArray">NativeJavaArray</seealso>
	/// <seealso cref="NativeJavaPackage">NativeJavaPackage</seealso>
	/// <seealso cref="NativeJavaClass">NativeJavaClass</seealso>
	[Serializable]
	public class NativeJavaMethod : BaseFunction
	{
		internal NativeJavaMethod(MemberBox[] methods)
		{
			functionName = methods[0].GetName();
			this.methods = methods;
		}

		internal NativeJavaMethod(MemberBox[] methods, string name)
		{
			functionName = name;
			this.methods = methods;
		}

		internal NativeJavaMethod(MemberBox method, string name)
		{
			functionName = name;
			methods = new MemberBox[] { method };
		}

		public NativeJavaMethod(MethodInfo method, string name) : this(new MemberBox(method), name)
		{
		}

		public override string GetFunctionName()
		{
			return functionName;
		}

		internal static string ScriptSignature(object[] values)
		{
			StringBuilder sig = new StringBuilder();
			for (int i = 0; i != values.Length; ++i)
			{
				object value = values[i];
				string s;
				if (value == null)
				{
					s = "null";
				}
				else
				{
					if (value is bool)
					{
						s = "boolean";
					}
					else
					{
						if (value is string)
						{
							s = "string";
						}
						else
						{
							if (value.IsNumber())
							{
								s = "number";
							}
							else
							{
								if (value is Scriptable)
								{
									if (value is Undefined)
									{
										s = "undefined";
									}
									else
									{
										if (value is Wrapper)
										{
											object wrapped = ((Wrapper)value).Unwrap();
											s = wrapped.GetType().FullName;
										}
										else
										{
											if (value is Function)
											{
												s = "function";
											}
											else
											{
												s = "object";
											}
										}
									}
								}
								else
								{
									s = JavaMembers.JavaSignature(value.GetType());
								}
							}
						}
					}
				}
				if (i != 0)
				{
					sig.Append(',');
				}
				sig.Append(s);
			}
			return sig.ToString();
		}

		internal override string Decompile(int indent, int flags)
		{
			StringBuilder sb = new StringBuilder();
			bool justbody = (0 != (flags & Decompiler.ONLY_BODY_FLAG));
			if (!justbody)
			{
				sb.Append("function ");
				sb.Append(GetFunctionName());
				sb.Append("() {");
			}
			sb.Append("/*\n");
			sb.Append(ToString());
			sb.Append(justbody ? "*/\n" : "*/}\n");
			return sb.ToString();
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0, N = methods.Length; i != N; ++i)
			{
				// Check member type, we also use this for overloaded constructors
				if (methods[i].IsMethod())
				{
					MethodInfo method = methods[i].Method();
					sb.Append(JavaMembers.JavaSignature(method.ReturnType));
					sb.Append(' ');
					sb.Append(method.Name);
				}
				else
				{
					sb.Append(methods[i].GetName());
				}
				sb.Append(JavaMembers.LiveConnectSignature(methods[i].argTypes));
				sb.Append('\n');
			}
			return sb.ToString();
		}

		public override object Call(Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			// Find a method that matches the types given.
			if (methods.Length == 0)
			{
				throw new Exception("No methods defined for call");
			}
			int index = FindCachedFunction(cx, args);
			if (index < 0)
			{
				Type c = methods[0].Method().DeclaringType;
				string sig = c.FullName + '.' + GetFunctionName() + '(' + ScriptSignature(args) + ')';
				throw Context.ReportRuntimeError1("msg.java.no_such_method", sig);
			}
			MemberBox meth = methods[index];
			Type[] argTypes = meth.argTypes;
			if (meth.vararg)
			{
				// marshall the explicit parameters
				object[] newArgs = new object[argTypes.Length];
				for (int i = 0; i < argTypes.Length - 1; i++)
				{
					newArgs[i] = Context.JsToJava(args[i], argTypes[i]);
				}
				Array varArgs;
				// Handle special situation where a single variable parameter
				// is given and it is a Java or ECMA array or is null.
				if (args.Length == argTypes.Length && (args[args.Length - 1] == null || args[args.Length - 1] is NativeArray || args[args.Length - 1] is NativeJavaArray))
				{
					// convert the ECMA array into a native array
					varArgs = (Array) Context.JsToJava(args[args.Length - 1], argTypes[argTypes.Length - 1]);
				}
				else
				{
					// marshall the variable parameters
					Type componentType = argTypes[argTypes.Length - 1].GetElementType();
					varArgs = Array.CreateInstance(componentType, args.Length - argTypes.Length + 1);
					for (int i = 0; i < varArgs.Length; i++)
					{
						object value = Context.JsToJava(args[argTypes.Length - 1 + i], componentType);
						varArgs.SetValue(value, i);
					}
				}
				// add varargs
				newArgs[argTypes.Length - 1] = varArgs;
				// replace the original args with the new one
				args = newArgs;
			}
			else
			{
				// First, we marshall the args.
				object[] origArgs = args;
				for (int i = 0; i < args.Length; i++)
				{
					object arg = args[i];
					object coerced = Context.JsToJava(arg, argTypes[i]);
					if (coerced != arg)
					{
						if (origArgs == args)
						{
							args = (object[]) args.Clone();
						}
						args[i] = coerced;
					}
				}
			}
			object javaObject;
			if (meth.IsStatic())
			{
				javaObject = null;
			}
			else
			{
				// don't need an object
				Scriptable o = thisObj;
				Type c = meth.GetDeclaringClass();
				for (; ; )
				{
					if (o == null)
					{
						throw Context.ReportRuntimeError3("msg.nonjava.method", GetFunctionName(), ScriptRuntime.ToString(thisObj), c.FullName);
					}
					if (o is Wrapper)
					{
						javaObject = ((Wrapper)o).Unwrap();
						if (c.IsInstanceOfType(javaObject))
						{
							break;
						}
					}
					o = o.GetPrototype();
				}
			}
			object retval = meth.Invoke(javaObject, args);
			Type staticType = meth.Method().ReturnType;
			object wrapped = cx.GetWrapFactory().Wrap(cx, scope, retval, staticType);
			if (wrapped == null && staticType == typeof(void))
			{
				wrapped = Undefined.instance;
			}
			return wrapped;
		}

		internal virtual int FindCachedFunction(Context cx, object[] args)
		{
			if (methods.Length > 1)
			{
				if (overloadCache != null)
				{
					foreach (ResolvedOverload ovl in overloadCache)
					{
						if (ovl.Matches(args))
						{
							return ovl.index;
						}
					}
				}
				else
				{
					overloadCache = new CopyOnWriteArrayList<ResolvedOverload>();
				}
				int index = FindFunction(cx, methods, args);
				// As a sanity measure, don't let the lookup cache grow longer
				// than twice the number of overloaded methods
				if (overloadCache.Count < methods.Length * 2)
				{
					lock (overloadCache)
					{
						ResolvedOverload ovl = new ResolvedOverload(args, index);
						if (!overloadCache.Contains(ovl))
						{
							overloadCache.Add(0, ovl);
						}
					}
				}
				return index;
			}
			return FindFunction(cx, methods, args);
		}

		/// <summary>
		/// Find the index of the correct function to call given the set of methods
		/// or constructors and the arguments.
		/// </summary>
		/// <remarks>
		/// Find the index of the correct function to call given the set of methods
		/// or constructors and the arguments.
		/// If no function can be found to call, return -1.
		/// </remarks>
		internal static int FindFunction(Context cx, MemberBox[] methodsOrCtors, object[] args)
		{
			if (methodsOrCtors.Length == 0)
			{
				return -1;
			}
			else
			{
				if (methodsOrCtors.Length == 1)
				{
					MemberBox member = methodsOrCtors[0];
					Type[] argTypes = member.argTypes;
					int alength = argTypes.Length;
					if (member.vararg)
					{
						alength--;
						if (alength > args.Length)
						{
							return -1;
						}
					}
					else
					{
						if (alength != args.Length)
						{
							return -1;
						}
					}
					for (int j = 0; j != alength; ++j)
					{
						if (!NativeJavaObject.CanConvert(args[j], argTypes[j]))
						{
							return -1;
						}
					}
					return 0;
				}
			}
			int firstBestFit = -1;
			int[] extraBestFits = null;
			int extraBestFitsCount = 0;
			for (int i = 0; i < methodsOrCtors.Length; i++)
			{
				MemberBox member = methodsOrCtors[i];
				Type[] argTypes = member.argTypes;
				int alength = argTypes.Length;
				if (member.vararg)
				{
					alength--;
					if (alength > args.Length)
					{
						goto search_continue;
					}
				}
				else
				{
					if (alength != args.Length)
					{
						goto search_continue;
					}
				}
				for (int j = 0; j < alength; j++)
				{
					if (!NativeJavaObject.CanConvert(args[j], argTypes[j]))
					{
						goto search_continue;
					}
				}
				if (firstBestFit < 0)
				{
					firstBestFit = i;
				}
				else
				{
					// Compare with all currently fit methods.
					// The loop starts from -1 denoting firstBestFit and proceed
					// until extraBestFitsCount to avoid extraBestFits allocation
					// in the most common case of no ambiguity
					int betterCount = 0;
					// number of times member was prefered over
					// best fits
					int worseCount = 0;
					// number of times best fits were prefered
					// over member
					for (int j_1 = -1; j_1 != extraBestFitsCount; ++j_1)
					{
						int bestFitIndex;
						if (j_1 == -1)
						{
							bestFitIndex = firstBestFit;
						}
						else
						{
							bestFitIndex = extraBestFits[j_1];
						}
						MemberBox bestFit = methodsOrCtors[bestFitIndex];
						if (cx.HasFeature(LanguageFeatures.EnhancedJavaAccess) && (bestFit.Member().Attributes & MethodAttributes.Public) != (member.Member().Attributes & MethodAttributes.Public))
						{
							// When EnhancedJavaAccess gives us access
							// to non-public members, continue to prefer public
							// methods in overloading
							if ((bestFit.Member().Attributes & MethodAttributes.Public) == 0)
							{
								++betterCount;
							}
							else
							{
								++worseCount;
							}
						}
						else
						{
							int preference = PreferSignature(args, argTypes, member.vararg, bestFit.argTypes, bestFit.vararg);
							if (preference == PREFERENCE_AMBIGUOUS)
							{
								break;
							}
							else
							{
								if (preference == PREFERENCE_FIRST_ARG)
								{
									++betterCount;
								}
								else
								{
									if (preference == PREFERENCE_SECOND_ARG)
									{
										++worseCount;
									}
									else
									{
										if (preference != PREFERENCE_EQUAL)
										{
											Kit.CodeBug();
										}
										// This should not happen in theory
										// but on some JVMs, Class.getMethods will return all
										// static methods of the class hierarchy, even if
										// a derived class's parameters match exactly.
										// We want to call the derived class's method.
										if (bestFit.IsStatic() && bestFit.GetDeclaringClass().IsAssignableFrom(member.GetDeclaringClass()))
										{
											// On some JVMs, Class.getMethods will return all
											// static methods of the class hierarchy, even if
											// a derived class's parameters match exactly.
											// We want to call the derived class's method.
											if (j_1 == -1)
											{
												firstBestFit = i;
											}
											else
											{
												extraBestFits[j_1] = i;
											}
										}
										goto search_continue;
									}
								}
							}
						}
					}
					if (betterCount == 1 + extraBestFitsCount)
					{
						// member was prefered over all best fits
						firstBestFit = i;
						extraBestFitsCount = 0;
					}
					else
					{
						if (worseCount == 1 + extraBestFitsCount)
						{
						}
						else
						{
							// all best fits were prefered over member, ignore it
							// some ambiguity was present, add member to best fit set
							if (extraBestFits == null)
							{
								// Allocate maximum possible array
								extraBestFits = new int[methodsOrCtors.Length - 1];
							}
							extraBestFits[extraBestFitsCount] = i;
							++extraBestFitsCount;
						}
					}
				}
search_continue: ;
			}
search_break: ;
			if (firstBestFit < 0)
			{
				// Nothing was found
				return -1;
			}
			else
			{
				if (extraBestFitsCount == 0)
				{
					// single best fit
					return firstBestFit;
				}
			}
			// report remaining ambiguity
			StringBuilder buf = new StringBuilder();
			for (int j_2 = -1; j_2 != extraBestFitsCount; ++j_2)
			{
				int bestFitIndex;
				if (j_2 == -1)
				{
					bestFitIndex = firstBestFit;
				}
				else
				{
					bestFitIndex = extraBestFits[j_2];
				}
				buf.Append("\n    ");
				buf.Append(methodsOrCtors[bestFitIndex].ToJavaDeclaration());
			}
			MemberBox firstFitMember = methodsOrCtors[firstBestFit];
			string memberName = firstFitMember.GetName();
			string memberClass = firstFitMember.GetDeclaringClass().FullName;
			if (methodsOrCtors[0].IsCtor())
			{
				throw Context.ReportRuntimeError3("msg.constructor.ambiguous", memberName, ScriptSignature(args), buf.ToString());
			}
			else
			{
				throw Context.ReportRuntimeError4("msg.method.ambiguous", memberClass, memberName, ScriptSignature(args), buf.ToString());
			}
		}

		/// <summary>Types are equal</summary>
		private const int PREFERENCE_EQUAL = 0;

		private const int PREFERENCE_FIRST_ARG = 1;

		private const int PREFERENCE_SECOND_ARG = 2;

		/// <summary>No clear "easy" conversion</summary>
		private const int PREFERENCE_AMBIGUOUS = 3;

		/// <summary>Determine which of two signatures is the closer fit.</summary>
		/// <remarks>
		/// Determine which of two signatures is the closer fit.
		/// Returns one of PREFERENCE_EQUAL, PREFERENCE_FIRST_ARG,
		/// PREFERENCE_SECOND_ARG, or PREFERENCE_AMBIGUOUS.
		/// </remarks>
		private static int PreferSignature(object[] args, Type[] sig1, bool vararg1, Type[] sig2, bool vararg2)
		{
			int totalPreference = 0;
			for (int j = 0; j < args.Length; j++)
			{
				Type type1 = vararg1 && j >= sig1.Length ? sig1[sig1.Length - 1] : sig1[j];
				Type type2 = vararg2 && j >= sig2.Length ? sig2[sig2.Length - 1] : sig2[j];
				if (type1 == type2)
				{
					continue;
				}
				object arg = args[j];
				// Determine which of type1, type2 is easier to convert from arg.
				int rank1 = NativeJavaObject.GetConversionWeight(arg, type1);
				int rank2 = NativeJavaObject.GetConversionWeight(arg, type2);
				int preference;
				if (rank1 < rank2)
				{
					preference = PREFERENCE_FIRST_ARG;
				}
				else
				{
					if (rank1 > rank2)
					{
						preference = PREFERENCE_SECOND_ARG;
					}
					else
					{
						// Equal ranks
						if (rank1 == NativeJavaObject.CONVERSION_NONTRIVIAL)
						{
							if (type1.IsAssignableFrom(type2))
							{
								preference = PREFERENCE_SECOND_ARG;
							}
							else
							{
								if (type2.IsAssignableFrom(type1))
								{
									preference = PREFERENCE_FIRST_ARG;
								}
								else
								{
									preference = PREFERENCE_AMBIGUOUS;
								}
							}
						}
						else
						{
							preference = PREFERENCE_AMBIGUOUS;
						}
					}
				}
				totalPreference |= preference;
				if (totalPreference == PREFERENCE_AMBIGUOUS)
				{
					break;
				}
			}
			return totalPreference;
		}

		private const bool debug = false;

		private static void PrintDebug(string msg, MemberBox member, object[] args)
		{
		}

		internal MemberBox[] methods;

		private string functionName;

		[NonSerialized]
		private CopyOnWriteArrayList<ResolvedOverload> overloadCache;
	}

	internal class ResolvedOverload
	{
		internal readonly Type[] types;

		internal readonly int index;

		internal ResolvedOverload(object[] args, int index)
		{
			this.index = index;
			types = new Type[args.Length];
			for (int i = 0, l = args.Length; i < l; i++)
			{
				object arg = args[i];
				if (arg is Wrapper)
				{
					arg = ((Wrapper)arg).Unwrap();
				}
				types[i] = arg == null ? null : arg.GetType();
			}
		}

		internal virtual bool Matches(object[] args)
		{
			if (args.Length != types.Length)
			{
				return false;
			}
			for (int i = 0, l = args.Length; i < l; i++)
			{
				object arg = args[i];
				if (arg is Wrapper)
				{
					arg = ((Wrapper)arg).Unwrap();
				}
				if (arg == null)
				{
					if (types[i] != null)
					{
						return false;
					}
				}
				else
				{
					if (arg.GetType() != types[i])
					{
						return false;
					}
				}
			}
			return true;
		}

		public override bool Equals(object other)
		{
			if (!(other is ResolvedOverload))
			{
				return false;
			}
			ResolvedOverload ovl = (ResolvedOverload)other;
			return Arrays.Equals(types, ovl.types) && index == ovl.index;
		}

		public override int GetHashCode()
		{
			return Arrays.HashCode(types);
		}
	}
}
