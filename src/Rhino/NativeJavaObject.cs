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
using System.Reflection;
using System.Security;
using Rhino;
using Rhino.Utils;
using Sharpen;

namespace Rhino
{
	/// <summary>This class reflects non-Array Java objects into the JavaScript environment.</summary>
	/// <remarks>
	/// This class reflects non-Array Java objects into the JavaScript environment.  It
	/// reflect fields directly, and uses NativeJavaMethod objects to reflect (possibly
	/// overloaded) methods.<p>
	/// </remarks>
	/// <author>Mike Shaver</author>
	/// <seealso cref="NativeJavaArray">NativeJavaArray</seealso>
	/// <seealso cref="NativeJavaPackage">NativeJavaPackage</seealso>
	/// <seealso cref="NativeJavaClass">NativeJavaClass</seealso>
	[System.Serializable]
	public class NativeJavaObject : Scriptable, Wrapper
	{
		internal const long serialVersionUID = -6948590651130498591L;

		public NativeJavaObject()
		{
		}

		public NativeJavaObject(Scriptable scope, object javaObject, Type staticType) : this(scope, javaObject, staticType, false)
		{
		}

		public NativeJavaObject(Scriptable scope, object javaObject, Type staticType, bool isAdapter)
		{
			this.parent = scope;
			this.javaObject = javaObject;
			this.staticType = staticType;
			this.isAdapter = isAdapter;
			InitMembers();
		}

		protected internal virtual void InitMembers()
		{
			Type dynamicType;
			if (javaObject != null)
			{
				dynamicType = javaObject.GetType();
			}
			else
			{
				dynamicType = staticType;
			}
			members = JavaMembers.LookupClass(parent, dynamicType, staticType, isAdapter);
			fieldAndMethods = members.GetFieldAndMethodsObjects(this, javaObject, false);
		}

		public virtual bool Has(string name, Scriptable start)
		{
			return members.Has(name, false);
		}

		public virtual bool Has(int index, Scriptable start)
		{
			return false;
		}

		public virtual object Get(string name, Scriptable start)
		{
			if (fieldAndMethods != null)
			{
				object result = fieldAndMethods.Get(name);
				if (result != null)
				{
					return result;
				}
			}
			// TODO: passing 'this' as the scope is bogus since it has
			//  no parent scope
			return members.Get(this, name, javaObject, false);
		}

		public virtual object Get(int index, Scriptable start)
		{
			throw members.ReportMemberNotFound(index.ToString());
		}

		public virtual void Put(string name, Scriptable start, object value)
		{
			// We could be asked to modify the value of a property in the
			// prototype. Since we can't add a property to a Java object,
			// we modify it in the prototype rather than copy it down.
			if (prototype == null || members.Has(name, false))
			{
				members.Put(this, name, javaObject, value, false);
			}
			else
			{
				prototype.Put(name, prototype, value);
			}
		}

		public virtual void Put(int index, Scriptable start, object value)
		{
			throw members.ReportMemberNotFound(index.ToString());
		}

		public virtual bool HasInstance(Scriptable value)
		{
			// This is an instance of a Java class, so always return false
			return false;
		}

		public virtual void Delete(string name)
		{
		}

		public virtual void Delete(int index)
		{
		}

		public virtual Scriptable GetPrototype()
		{
			if (prototype == null && javaObject is string)
			{
				return TopLevel.GetBuiltinPrototype(ScriptableObject.GetTopLevelScope(parent), TopLevel.Builtins.String);
			}
			return prototype;
		}

		/// <summary>Sets the prototype of the object.</summary>
		/// <remarks>Sets the prototype of the object.</remarks>
		public virtual void SetPrototype(Scriptable m)
		{
			prototype = m;
		}

		/// <summary>Returns the parent (enclosing) scope of the object.</summary>
		/// <remarks>Returns the parent (enclosing) scope of the object.</remarks>
		public virtual Scriptable GetParentScope()
		{
			return parent;
		}

		/// <summary>Sets the parent (enclosing) scope of the object.</summary>
		/// <remarks>Sets the parent (enclosing) scope of the object.</remarks>
		public virtual void SetParentScope(Scriptable m)
		{
			parent = m;
		}

		public virtual object[] GetIds()
		{
			return members.GetIds(false);
		}

		public virtual object Unwrap()
		{
			return javaObject;
		}

		public virtual string GetClassName()
		{
			return "JavaObject";
		}

		public virtual object GetDefaultValue(Type hint)
		{
			object value;
			if (hint == null)
			{
				if (javaObject is bool)
				{
					hint = ScriptRuntime.BooleanClass;
				}
			}
			if (hint == null || hint == ScriptRuntime.StringClass)
			{
				value = javaObject.ToString();
			}
			else
			{
				string converterName;
				if (hint == ScriptRuntime.BooleanClass)
				{
					converterName = "booleanValue";
				}
				else
				{
					if (hint == ScriptRuntime.NumberClass)
					{
						converterName = "doubleValue";
					}
					else
					{
						throw Context.ReportRuntimeError0("msg.default.value");
					}
				}
				object converterObject = Get(converterName, this);
				if (converterObject is Function)
				{
					Function f = (Function)converterObject;
					value = f.Call(Context.GetContext(), f.GetParentScope(), this, ScriptRuntime.emptyArgs);
				}
				else
				{
					if (hint == ScriptRuntime.NumberClass && javaObject is bool)
					{
						bool b = ((bool)javaObject);
						value = ScriptRuntime.WrapNumber(b ? 1.0 : 0.0);
					}
					else
					{
						value = javaObject.ToString();
					}
				}
			}
			return value;
		}

		/// <summary>
		/// Determine whether we can/should convert between the given type and the
		/// desired one.
		/// </summary>
		/// <remarks>
		/// Determine whether we can/should convert between the given type and the
		/// desired one.  This should be superceded by a conversion-cost calculation
		/// function, but for now I'll hide behind precedent.
		/// </remarks>
		public static bool CanConvert(object fromObj, Type to)
		{
			int weight = GetConversionWeight(fromObj, to);
			return (weight < CONVERSION_NONE);
		}

		private const int JSTYPE_UNDEFINED = 0;

		private const int JSTYPE_NULL = 1;

		private const int JSTYPE_BOOLEAN = 2;

		private const int JSTYPE_NUMBER = 3;

		private const int JSTYPE_STRING = 4;

		private const int JSTYPE_JAVA_CLASS = 5;

		private const int JSTYPE_JAVA_OBJECT = 6;

		private const int JSTYPE_JAVA_ARRAY = 7;

		private const int JSTYPE_OBJECT = 8;

		internal const byte CONVERSION_TRIVIAL = 1;

		internal const byte CONVERSION_NONTRIVIAL = 0;

		internal const byte CONVERSION_NONE = 99;

		// undefined type
		// null
		// boolean
		// number
		// string
		// JavaClass
		// JavaObject
		// JavaArray
		// Scriptable
		/// <summary>Derive a ranking based on how "natural" the conversion is.</summary>
		/// <remarks>
		/// Derive a ranking based on how "natural" the conversion is.
		/// The special value CONVERSION_NONE means no conversion is possible,
		/// and CONVERSION_NONTRIVIAL signals that more type conformance testing
		/// is required.
		/// Based on
		/// <a href="http://www.mozilla.org/js/liveconnect/lc3_method_overloading.html">
		/// "preferred method conversions" from Live Connect 3</a>
		/// </remarks>
		internal static int GetConversionWeight(object fromObj, Type to)
		{
			int fromCode = GetJSTypeCode(fromObj);
			switch (fromCode)
			{
				case JSTYPE_UNDEFINED:
				{
					if (to == ScriptRuntime.StringClass || to == ScriptRuntime.ObjectClass)
					{
						return 1;
					}
					break;
				}

				case JSTYPE_NULL:
				{
					if (!to.IsPrimitive)
					{
						return 1;
					}
					break;
				}

				case JSTYPE_BOOLEAN:
				{
					// "boolean" is #1
					if (to == typeof(bool))
					{
						return 1;
					}
					else
					{
						if (to == ScriptRuntime.BooleanClass)
						{
							return 2;
						}
						else
						{
							if (to == ScriptRuntime.ObjectClass)
							{
								return 3;
							}
							else
							{
								if (to == ScriptRuntime.StringClass)
								{
									return 4;
								}
							}
						}
					}
					break;
				}

				case JSTYPE_NUMBER:
				{
					if (to.IsPrimitive)
					{
						if (to == typeof(double))
						{
							return 1;
						}
						else
						{
							if (to != typeof(bool))
							{
								return 1 + GetSizeRank(to);
							}
						}
					}
					else
					{
						if (to == ScriptRuntime.StringClass)
						{
							// native numbers are #1-8
							return 9;
						}
						else
						{
							if (to == ScriptRuntime.ObjectClass)
							{
								return 10;
							}
							else
							{
								if (ScriptRuntime.NumberClass.IsAssignableFrom(to))
								{
									// "double" is #1
									return 2;
								}
							}
						}
					}
					break;
				}

				case JSTYPE_STRING:
				{
					if (to == ScriptRuntime.StringClass)
					{
						return 1;
					}
					else
					{
						if (to.IsInstanceOfType(fromObj))
						{
							return 2;
						}
						else
						{
							if (to.IsPrimitive)
							{
								if (to == typeof(char))
								{
									return 3;
								}
								else
								{
									if (to != typeof(bool))
									{
										return 4;
									}
								}
							}
						}
					}
					break;
				}

				case JSTYPE_JAVA_CLASS:
				{
					if (to == ScriptRuntime.ClassClass)
					{
						return 1;
					}
					else
					{
						if (to == ScriptRuntime.ObjectClass)
						{
							return 3;
						}
						else
						{
							if (to == ScriptRuntime.StringClass)
							{
								return 4;
							}
						}
					}
					break;
				}

				case JSTYPE_JAVA_OBJECT:
				case JSTYPE_JAVA_ARRAY:
				{
					object javaObj = fromObj;
					if (javaObj is Wrapper)
					{
						javaObj = ((Wrapper)javaObj).Unwrap();
					}
					if (to.IsInstanceOfType(javaObj))
					{
						return CONVERSION_NONTRIVIAL;
					}
					if (to == ScriptRuntime.StringClass)
					{
						return 2;
					}
					else
					{
						if (to.IsPrimitive && to != typeof(bool))
						{
							return (fromCode == JSTYPE_JAVA_ARRAY) ? CONVERSION_NONE : 2 + GetSizeRank(to);
						}
					}
					break;
				}

				case JSTYPE_OBJECT:
				{
					// Other objects takes #1-#3 spots
					if (to != ScriptRuntime.ObjectClass && to.IsInstanceOfType(fromObj))
					{
						// No conversion required, but don't apply for java.lang.Object
						return 1;
					}
					if (to.IsArray)
					{
						if (fromObj is NativeArray)
						{
							// This is a native array conversion to a java array
							// Array conversions are all equal, and preferable to object
							// and string conversion, per LC3.
							return 2;
						}
					}
					else
					{
						if (to == ScriptRuntime.ObjectClass)
						{
							return 3;
						}
						else
						{
							if (to == ScriptRuntime.StringClass)
							{
								return 4;
							}
							else
							{
								if (to == ScriptRuntime.DateClass)
								{
									if (fromObj is NativeDate)
									{
										// This is a native date to java date conversion
										return 1;
									}
								}
								else
								{
									if (to.IsInterface)
									{
										if (fromObj is NativeObject || fromObj is NativeFunction)
										{
											// See comments in createInterfaceAdapter
											return 1;
										}
										return 12;
									}
									else
									{
										if (to.IsPrimitive && to != typeof(bool))
										{
											return 4 + GetSizeRank(to);
										}
									}
								}
							}
						}
					}
					break;
				}
			}
			return CONVERSION_NONE;
		}

		internal static int GetSizeRank(Type aType)
		{
			if (aType == typeof(double))
			{
				return 1;
			}
			else
			{
				if (aType == typeof(float))
				{
					return 2;
				}
				else
				{
					if (aType == typeof(long))
					{
						return 3;
					}
					else
					{
						if (aType == typeof(int))
						{
							return 4;
						}
						else
						{
							if (aType == typeof(short))
							{
								return 5;
							}
							else
							{
								if (aType == typeof(char))
								{
									return 6;
								}
								else
								{
									if (aType == typeof(byte))
									{
										return 7;
									}
									else
									{
										if (aType == typeof(bool))
										{
											return CONVERSION_NONE;
										}
										else
										{
											return 8;
										}
									}
								}
							}
						}
					}
				}
			}
		}

		private static int GetJSTypeCode(object value)
		{
			if (value == null)
			{
				return JSTYPE_NULL;
			}
			else
			{
				if (value == Undefined.instance)
				{
					return JSTYPE_UNDEFINED;
				}
				else
				{
					if (value is string)
					{
						return JSTYPE_STRING;
					}
					else
					{
						if (value.IsNumber())
						{
							return JSTYPE_NUMBER;
						}
						else
						{
							if (value is bool)
							{
								return JSTYPE_BOOLEAN;
							}
							else
							{
								if (value is Scriptable)
								{
									if (value is NativeJavaClass)
									{
										return JSTYPE_JAVA_CLASS;
									}
									else
									{
										if (value is NativeJavaArray)
										{
											return JSTYPE_JAVA_ARRAY;
										}
										else
										{
											if (value is Wrapper)
											{
												return JSTYPE_JAVA_OBJECT;
											}
											else
											{
												return JSTYPE_OBJECT;
											}
										}
									}
								}
								else
								{
									if (value is Type)
									{
										return JSTYPE_JAVA_CLASS;
									}
									else
									{
										Type valueClass = value.GetType();
										if (valueClass.IsArray)
										{
											return JSTYPE_JAVA_ARRAY;
										}
										else
										{
											return JSTYPE_JAVA_OBJECT;
										}
									}
								}
							}
						}
					}
				}
			}
		}

		/// <summary>Type-munging for field setting and method invocation.</summary>
		/// <remarks>
		/// Type-munging for field setting and method invocation.
		/// Conforms to LC3 specification
		/// </remarks>
		internal static object CoerceTypeImpl(Type type, object value)
		{
			if (value != null && value.GetType() == type)
			{
				return value;
			}
			switch (GetJSTypeCode(value))
			{
				case JSTYPE_NULL:
				{
					// raise error if type.isPrimitive()
					if (type.IsPrimitive)
					{
						ReportConversionError(value, type);
					}
					return null;
				}

				case JSTYPE_UNDEFINED:
				{
					if (type == ScriptRuntime.StringClass || type == ScriptRuntime.ObjectClass)
					{
						return "undefined";
					}
					else
					{
						ReportConversionError("undefined", type);
					}
					break;
				}

				case JSTYPE_BOOLEAN:
				{
					// Under LC3, only JS Booleans can be coerced into a Boolean value
					if (type == typeof(bool) || type == ScriptRuntime.BooleanClass || type == ScriptRuntime.ObjectClass)
					{
						return value;
					}
					else
					{
						if (type == ScriptRuntime.StringClass)
						{
							return value.ToString();
						}
						else
						{
							ReportConversionError(value, type);
						}
					}
					break;
				}

				case JSTYPE_NUMBER:
				{
					if (type == ScriptRuntime.StringClass)
					{
						return ScriptRuntime.ToString(value);
					}
					else
					{
						if (type == ScriptRuntime.ObjectClass)
						{
							return CoerceToNumber(typeof(double), value);
						}
						else
						{
							if ((type.IsPrimitive && type != typeof(bool)) || ScriptRuntime.NumberClass.IsAssignableFrom(type))
							{
								return CoerceToNumber(type, value);
							}
							else
							{
								ReportConversionError(value, type);
							}
						}
					}
					break;
				}

				case JSTYPE_STRING:
				{
					if (type == ScriptRuntime.StringClass || type.IsInstanceOfType(value))
					{
						return value.ToString();
					}
					else
					{
						if (type == typeof(char) || type == ScriptRuntime.CharacterClass)
						{
							// Special case for converting a single char string to a
							// character
							// Placed here because it applies *only* to JS strings,
							// not other JS objects converted to strings
							if (((string)value).Length == 1)
							{
								return ((string)value)[0];
							}
							else
							{
								return CoerceToNumber(type, value);
							}
						}
						else
						{
							if ((type.IsPrimitive && type != typeof(bool)) || ScriptRuntime.NumberClass.IsAssignableFrom(type))
							{
								return CoerceToNumber(type, value);
							}
							else
							{
								ReportConversionError(value, type);
							}
						}
					}
					break;
				}

				case JSTYPE_JAVA_CLASS:
				{
					if (value is Wrapper)
					{
						value = ((Wrapper)value).Unwrap();
					}
					if (type == ScriptRuntime.ClassClass || type == ScriptRuntime.ObjectClass)
					{
						return value;
					}
					else
					{
						if (type == ScriptRuntime.StringClass)
						{
							return value.ToString();
						}
						else
						{
							ReportConversionError(value, type);
						}
					}
					break;
				}

				case JSTYPE_JAVA_OBJECT:
				case JSTYPE_JAVA_ARRAY:
				{
					if (value is Wrapper)
					{
						value = ((Wrapper)value).Unwrap();
					}
					if (type.IsPrimitive)
					{
						if (type == typeof(bool))
						{
							ReportConversionError(value, type);
						}
						return CoerceToNumber(type, value);
					}
					else
					{
						if (type == ScriptRuntime.StringClass)
						{
							return value.ToString();
						}
						else
						{
							if (type.IsInstanceOfType(value))
							{
								return value;
							}
							else
							{
								ReportConversionError(value, type);
							}
						}
					}
					break;
				}

				case JSTYPE_OBJECT:
				{
					if (type == ScriptRuntime.StringClass)
					{
						return ScriptRuntime.ToString(value);
					}
					else
					{
						if (type.IsPrimitive)
						{
							if (type == typeof(bool))
							{
								ReportConversionError(value, type);
							}
							return CoerceToNumber(type, value);
						}
						else
						{
							if (type.IsInstanceOfType(value))
							{
								return value;
							}
							else
							{
								if (type == ScriptRuntime.DateClass && value is NativeDate)
								{
									double time = ((NativeDate)value).GetJSTimeValue();
									// XXX: This will replace NaN by 0
									return Sharpen.Extensions.CreateDate((long)time);
								}
								else
								{
									if (type.IsArray && value is NativeArray)
									{
										// Make a new java array, and coerce the JS array components
										// to the target (component) type.
										NativeArray array = (NativeArray)value;
										long length = array.GetLength();
										Type arrayType = type.GetElementType();
										Array result = System.Array.CreateInstance(arrayType, (int)length);
										for (int i = 0; i < length; ++i)
										{
											try
											{
												result.SetValue(CoerceTypeImpl(arrayType, array.Get(i, array)), i);
											}
											catch (EvaluatorException)
											{
												ReportConversionError(value, type);
											}
										}
										return result;
									}
									else
									{
										if (value is Wrapper)
										{
											value = ((Wrapper)value).Unwrap();
											if (type.IsInstanceOfType(value))
											{
												return value;
											}
											ReportConversionError(value, type);
										}
										else
										{
#if INTERFACE_ADAPTER
											if (type.IsInterface && (value is NativeObject || value is NativeFunction))
											{
												// Try to use function/object as implementation of Java interface.
												return CreateInterfaceAdapter(type, (ScriptableObject)value);
											}
											else
#endif
											{
												ReportConversionError(value, type);
											}
										}
									}
								}
							}
						}
					}
					break;
				}
			}
			return value;
		}

#if INTERFACE_ADAPTER
		protected internal static object CreateInterfaceAdapter(Type type, ScriptableObject so)
		{
			// XXX: Currently only instances of ScriptableObject are
			// supported since the resulting interface proxies should
			// be reused next time conversion is made and generic
			// Callable has no storage for it. Weak references can
			// address it but for now use this restriction.
			object key = Kit.MakeHashKeyFromPair(COERCED_INTERFACE_KEY, type);
			object old = so.GetAssociatedValue(key);
			if (old != null)
			{
				// Function was already wrapped
				return old;
			}
			Context cx = Context.GetContext();
			object glue = InterfaceAdapter.Create(cx, type, so);
			// Store for later retrieval
			glue = so.AssociateValue(key, glue);
			return glue;
		}
#endif

		private static object CoerceToNumber(Type type, object value)
		{
			Type valueClass = value.GetType();
			// Character
			if (type == typeof(char) || type == ScriptRuntime.CharacterClass)
			{
				if (valueClass == ScriptRuntime.CharacterClass)
				{
					return value;
				}
				return (char)ToInteger(value, ScriptRuntime.CharacterClass, char.MinValue, char.MaxValue);
			}
			// Double, Float
			if (type == ScriptRuntime.ObjectClass || type == ScriptRuntime.DoubleClass || type == typeof(double))
			{
				return valueClass == ScriptRuntime.DoubleClass ? value : ToDouble(value);
			}
			if (type == ScriptRuntime.FloatClass || type == typeof(float))
			{
				if (valueClass == ScriptRuntime.FloatClass)
				{
					return value;
				}
				else
				{
					double number = ToDouble(value);
					if (System.Double.IsInfinity(number) || double.IsNaN(number) || number == 0.0)
					{
						return (float)number;
					}
					else
					{
						double absNumber = Math.Abs(number);
						if (absNumber < float.MinValue)
						{
							return System.Convert.ToSingle((number > 0.0) ? +0.0 : -0.0);
						}
						else
						{
							if (absNumber > float.MaxValue)
							{
								return (number > 0.0) ? float.PositiveInfinity : float.NegativeInfinity;
							}
							else
							{
								return (float)number;
							}
						}
					}
				}
			}
			// Integer, Long, Short, Byte
			if (type == ScriptRuntime.IntegerClass || type == typeof(int))
			{
				if (valueClass == ScriptRuntime.IntegerClass)
				{
					return value;
				}
				else
				{
					return (int)ToInteger(value, ScriptRuntime.IntegerClass, int.MinValue, int.MaxValue);
				}
			}
			if (type == ScriptRuntime.LongClass || type == typeof(long))
			{
				if (valueClass == ScriptRuntime.LongClass)
				{
					return value;
				}
				else
				{
					double max = System.BitConverter.Int64BitsToDouble(unchecked((long)(0x43dfffffffffffffL)));
					double min = System.BitConverter.Int64BitsToDouble(unchecked((long)(0xc3e0000000000000L)));
					return ToInteger(value, ScriptRuntime.LongClass, min, max);
				}
			}
			if (type == ScriptRuntime.ShortClass || type == typeof(short))
			{
				if (valueClass == ScriptRuntime.ShortClass)
				{
					return value;
				}
				else
				{
					return (short)ToInteger(value, ScriptRuntime.ShortClass, short.MinValue, short.MaxValue);
				}
			}
			if (type == ScriptRuntime.ByteClass || type == typeof(byte))
			{
				if (valueClass == ScriptRuntime.ByteClass)
				{
					return value;
				}
				else
				{
					return unchecked((byte)ToInteger(value, ScriptRuntime.ByteClass, byte.MinValue, byte.MaxValue));
				}
			}
			return ToDouble(value);
		}

		private static double ToDouble(object value)
		{
			if (value.IsNumber())
			{
				return System.Convert.ToDouble(value);
			}
			else
			{
				if (value is string)
				{
					return ScriptRuntime.ToNumber((string)value);
				}
				else
				{
					if (value is Scriptable)
					{
						if (value is Wrapper)
						{
							// XXX: optimize tail-recursion?
							return ToDouble(((Wrapper)value).Unwrap());
						}
						else
						{
							return ScriptRuntime.ToNumber(value);
						}
					}
					else
					{
						MethodInfo meth;
						try
						{
							meth = value.GetType().GetMethod("doubleValue", (Type[])null);
						}
						catch (MissingMethodException)
						{
							meth = null;
						}
						catch (SecurityException)
						{
							meth = null;
						}
						if (meth != null)
						{
							try
							{
								return System.Convert.ToDouble(meth.Invoke(value, (object[])null));
							}
							catch (MemberAccessException)
							{
								// XXX: ignore, or error message?
								ReportConversionError(value, typeof(double));
							}
							catch (TargetInvocationException)
							{
								// XXX: ignore, or error message?
								ReportConversionError(value, typeof(double));
							}
						}
						return ScriptRuntime.ToNumber(value.ToString());
					}
				}
			}
		}

		private static long ToInteger(object value, Type type, double min, double max)
		{
			double d = ToDouble(value);
			if (System.Double.IsInfinity(d) || double.IsNaN(d))
			{
				// Convert to string first, for more readable message
				ReportConversionError(ScriptRuntime.ToString(value), type);
			}
			if (d > 0.0)
			{
				d = Math.Floor(d);
			}
			else
			{
				d = System.Math.Ceiling(d);
			}
			if (d < min || d > max)
			{
				// Convert to string first, for more readable message
				ReportConversionError(ScriptRuntime.ToString(value), type);
			}
			return (long)d;
		}

		internal static void ReportConversionError(object value, Type type)
		{
			// It uses String.valueOf(value), not value.toString() since
			// value can be null, bug 282447.
			throw Context.ReportRuntimeError2("msg.conversion.not.allowed", value.ToString(), JavaMembers.JavaSignature(type));
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteObject(ObjectOutputStream @out)
		{
			@out.DefaultWriteObject();
			@out.WriteBoolean(isAdapter);
			if (isAdapter)
			{
				if (adapter_writeAdapterObject == null)
				{
					throw new IOException();
				}
				object[] args = new object[] { javaObject, @out };
				try
				{
					adapter_writeAdapterObject.Invoke(null, args);
				}
				catch (Exception)
				{
					throw new IOException();
				}
			}
			else
			{
				@out.WriteObject(javaObject);
			}
			if (staticType != null)
			{
				@out.WriteObject(staticType.GetType().FullName);
			}
			else
			{
				@out.WriteObject(null);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.TypeLoadException"></exception>
		private void ReadObject(ObjectInputStream @in)
		{
			@in.DefaultReadObject();
			isAdapter = @in.ReadBoolean();
			if (isAdapter)
			{
				if (adapter_readAdapterObject == null)
				{
					throw new TypeLoadException();
				}
				object[] args = new object[] { this, @in };
				try
				{
					javaObject = adapter_readAdapterObject.Invoke(null, args);
				}
				catch (Exception)
				{
					throw new IOException();
				}
			}
			else
			{
				javaObject = @in.ReadObject();
			}
			string className = (string)@in.ReadObject();
			if (className != null)
			{
				staticType = Sharpen.Runtime.GetType(className);
			}
			else
			{
				staticType = null;
			}
			InitMembers();
		}

		/// <summary>The prototype of this object.</summary>
		/// <remarks>The prototype of this object.</remarks>
		protected internal Scriptable prototype;

		/// <summary>The parent scope of this object.</summary>
		/// <remarks>The parent scope of this object.</remarks>
		protected internal Scriptable parent;

		[System.NonSerialized]
		protected internal object javaObject;

		[System.NonSerialized]
		protected internal Type staticType;

		[System.NonSerialized]
		protected internal JavaMembers members;

		[System.NonSerialized]
		private IDictionary<string, FieldAndMethods> fieldAndMethods;

		[System.NonSerialized]
		protected internal bool isAdapter;

		private static readonly object COERCED_INTERFACE_KEY = "Coerced Interface";

		private static MethodInfo adapter_writeAdapterObject;

		private static MethodInfo adapter_readAdapterObject;

		static NativeJavaObject()
		{
			// Reflection in java is verbose
			Type[] sig2 = new Type[2];
			Type cl = Kit.ClassOrNull("Rhino.JavaAdapter");
			if (cl != null)
			{
				try
				{
					sig2[0] = ScriptRuntime.ObjectClass;
					sig2[1] = Kit.ClassOrNull("Sharpen.ObjectOutputStream");
					adapter_writeAdapterObject = cl.GetMethod("writeAdapterObject", sig2);
					sig2[0] = ScriptRuntime.ScriptableClass;
					sig2[1] = Kit.ClassOrNull("Sharpen.ObjectInputStream");
					adapter_readAdapterObject = cl.GetMethod("readAdapterObject", sig2);
				}
				catch (MissingMethodException)
				{
					adapter_writeAdapterObject = null;
					adapter_readAdapterObject = null;
				}
			}
		}
	}
}
