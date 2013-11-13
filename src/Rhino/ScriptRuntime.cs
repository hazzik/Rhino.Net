/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using Rhino;
using Rhino.Ast;
using Rhino.V8dtoa;
using Rhino.Xml;
using Sharpen;

namespace Rhino
{
	/// <summary>This is the class that implements the runtime.</summary>
	/// <remarks>This is the class that implements the runtime.</remarks>
	/// <author>Norris Boyd</author>
	public class ScriptRuntime
	{
		/// <summary>No instances should be created.</summary>
		/// <remarks>No instances should be created.</remarks>
		protected internal ScriptRuntime()
		{
		}

		/// <summary>Returns representation of the [[ThrowTypeError]] object.</summary>
		/// <remarks>
		/// Returns representation of the [[ThrowTypeError]] object.
		/// See ECMA 5 spec, 13.2.3
		/// </remarks>
		public static BaseFunction TypeErrorThrower()
		{
			if (THROW_TYPE_ERROR == null)
			{
				BaseFunction thrower = new _BaseFunction_41();
				thrower.PreventExtensions();
				THROW_TYPE_ERROR = thrower;
			}
			return THROW_TYPE_ERROR;
		}

		private sealed class _BaseFunction_41 : BaseFunction
		{
			public _BaseFunction_41()
			{
				this.serialVersionUID = -5891740962154902286L;
			}

			internal const long serialVersionUID;

			public override object Call(Context cx, Scriptable scope, Scriptable thisObj, object[] args)
			{
				throw Rhino.ScriptRuntime.TypeError0("msg.op.not.allowed");
			}

			public override int GetLength()
			{
				return 0;
			}
		}

		private static BaseFunction THROW_TYPE_ERROR = null;

		internal class NoSuchMethodShim : Callable
		{
			internal string methodName;

			internal Callable noSuchMethodMethod;

			internal NoSuchMethodShim(Callable noSuchMethodMethod, string methodName)
			{
				this.noSuchMethodMethod = noSuchMethodMethod;
				this.methodName = methodName;
			}

			/// <summary>Perform the call.</summary>
			/// <remarks>Perform the call.</remarks>
			/// <param name="cx">the current Context for this thread</param>
			/// <param name="scope">the scope to use to resolve properties.</param>
			/// <param name="thisObj">the JavaScript <code>this</code> object</param>
			/// <param name="args">the array of arguments</param>
			/// <returns>the result of the call</returns>
			public virtual object Call(Context cx, Scriptable scope, Scriptable thisObj, object[] args)
			{
				object[] nestedArgs = new object[2];
				nestedArgs[0] = methodName;
				nestedArgs[1] = NewArrayLiteral(args, null, cx, scope);
				return noSuchMethodMethod.Call(cx, scope, thisObj, nestedArgs);
			}
		}

		public static readonly Type BooleanClass = Kit.ClassOrNull("java.lang.Boolean");

		public static readonly Type ByteClass = Kit.ClassOrNull("java.lang.Byte");

		public static readonly Type CharacterClass = Kit.ClassOrNull("java.lang.Character");

		public static readonly Type ClassClass = Kit.ClassOrNull("java.lang.Class");

		public static readonly Type DoubleClass = Kit.ClassOrNull("java.lang.Double");

		public static readonly Type FloatClass = Kit.ClassOrNull("java.lang.Float");

		public static readonly Type IntegerClass = Kit.ClassOrNull("java.lang.Integer");

		public static readonly Type LongClass = Kit.ClassOrNull("java.lang.Long");

		public static readonly Type NumberClass = Kit.ClassOrNull("java.lang.Number");

		public static readonly Type ObjectClass = Kit.ClassOrNull("java.lang.Object");

		public static readonly Type ShortClass = Kit.ClassOrNull("java.lang.Short");

		public static readonly Type StringClass = Kit.ClassOrNull("java.lang.String");

		public static readonly Type DateClass = Kit.ClassOrNull("java.util.Date");

		public static readonly Type ContextClass = Kit.ClassOrNull("org.mozilla.javascript.Context");

		public static readonly Type ContextFactoryClass = Kit.ClassOrNull("org.mozilla.javascript.ContextFactory");

		public static readonly Type FunctionClass = Kit.ClassOrNull("org.mozilla.javascript.Function");

		public static readonly Type ScriptableObjectClass = Kit.ClassOrNull("org.mozilla.javascript.ScriptableObject");

		public static readonly Type ScriptableClass = typeof(Scriptable);

		public static CultureInfo ROOT_LOCALE = new CultureInfo(string.Empty);

		private static readonly object LIBRARY_SCOPE_KEY = "LIBRARY_SCOPE";

		// Locale object used to request locale-neutral operations.
		public static bool IsRhinoRuntimeType(Type cl)
		{
			if (cl.IsPrimitive)
			{
				return (cl != typeof(char));
			}
			else
			{
				return (cl == StringClass || cl == BooleanClass || NumberClass.IsAssignableFrom(cl) || ScriptableClass.IsAssignableFrom(cl));
			}
		}

		public static ScriptableObject InitStandardObjects(Context cx, ScriptableObject scope, bool @sealed)
		{
			if (scope == null)
			{
				scope = new NativeObject();
			}
			scope.AssociateValue(LIBRARY_SCOPE_KEY, scope);
			(new ClassCache()).Associate(scope);
			BaseFunction.Init(scope, @sealed);
			NativeObject.Init(scope, @sealed);
			Scriptable objectProto = ScriptableObject.GetObjectPrototype(scope);
			// Function.prototype.__proto__ should be Object.prototype
			Scriptable functionProto = ScriptableObject.GetClassPrototype(scope, "Function");
			functionProto.SetPrototype(objectProto);
			// Set the prototype of the object passed in if need be
			if (scope.GetPrototype() == null)
			{
				scope.SetPrototype(objectProto);
			}
			// must precede NativeGlobal since it's needed therein
			NativeError.Init(scope, @sealed);
			NativeGlobal.Init(cx, scope, @sealed);
			NativeArray.Init(scope, @sealed);
			if (cx.GetOptimizationLevel() > 0)
			{
				// When optimizing, attempt to fulfill all requests for new Array(N)
				// with a higher threshold before switching to a sparse
				// representation
				NativeArray.SetMaximumInitialCapacity(200000);
			}
			NativeString.Init(scope, @sealed);
			NativeBoolean.Init(scope, @sealed);
			NativeNumber.Init(scope, @sealed);
			NativeDate.Init(scope, @sealed);
			NativeMath.Init(scope, @sealed);
			NativeJSON.Init(scope, @sealed);
			NativeWith.Init(scope, @sealed);
			NativeCall.Init(scope, @sealed);
			NativeScript.Init(scope, @sealed);
			NativeIterator.Init(scope, @sealed);
			// Also initializes NativeGenerator
			bool withXml = cx.HasFeature(Context.FEATURE_E4X) && cx.GetE4xImplementationFactory() != null;
			// define lazy-loaded properties using their class name
			new LazilyLoadedCtor(scope, "RegExp", "org.mozilla.javascript.regexp.NativeRegExp", @sealed, true);
			new LazilyLoadedCtor(scope, "Packages", "org.mozilla.javascript.NativeJavaTopPackage", @sealed, true);
			new LazilyLoadedCtor(scope, "getClass", "org.mozilla.javascript.NativeJavaTopPackage", @sealed, true);
			new LazilyLoadedCtor(scope, "JavaAdapter", "org.mozilla.javascript.JavaAdapter", @sealed, true);
			new LazilyLoadedCtor(scope, "JavaImporter", "org.mozilla.javascript.ImporterTopLevel", @sealed, true);
			new LazilyLoadedCtor(scope, "Continuation", "org.mozilla.javascript.NativeContinuation", @sealed, true);
			foreach (string packageName in GetTopPackageNames())
			{
				new LazilyLoadedCtor(scope, packageName, "org.mozilla.javascript.NativeJavaTopPackage", @sealed, true);
			}
			if (withXml)
			{
				string xmlImpl = cx.GetE4xImplementationFactory().GetImplementationClassName();
				new LazilyLoadedCtor(scope, "XML", xmlImpl, @sealed, true);
				new LazilyLoadedCtor(scope, "XMLList", xmlImpl, @sealed, true);
				new LazilyLoadedCtor(scope, "Namespace", xmlImpl, @sealed, true);
				new LazilyLoadedCtor(scope, "QName", xmlImpl, @sealed, true);
			}
			if (scope is TopLevel)
			{
				((TopLevel)scope).CacheBuiltins();
			}
			return scope;
		}

		internal static string[] GetTopPackageNames()
		{
			// Include "android" top package if running on Android
			return "Dalvik".Equals(Runtime.GetProperty("java.vm.name")) ? new string[] { "java", "javax", "org", "com", "edu", "net", "android" } : new string[] { "java", "javax", "org", "com", "edu", "net" };
		}

		public static ScriptableObject GetLibraryScopeOrNull(Scriptable scope)
		{
			ScriptableObject libScope;
			libScope = (ScriptableObject)ScriptableObject.GetTopScopeValue(scope, LIBRARY_SCOPE_KEY);
			return libScope;
		}

		// It is public so NativeRegExp can access it.
		public static bool IsJSLineTerminator(int c)
		{
			// Optimization for faster check for eol character:
			// they do not have 0xDFD0 bits set
			if ((c & unchecked((int)(0xDFD0))) != 0)
			{
				return false;
			}
			return c == '\n' || c == '\r' || c == unchecked((int)(0x2028)) || c == unchecked((int)(0x2029));
		}

		public static bool IsJSWhitespaceOrLineTerminator(int c)
		{
			return (IsStrWhiteSpaceChar(c) || IsJSLineTerminator(c));
		}

		/// <summary>
		/// Indicates if the character is a Str whitespace char according to ECMA spec:
		/// StrWhiteSpaceChar :::
		/// <TAB>
		/// <SP>
		/// <NBSP>
		/// <FF>
		/// <VT>
		/// <CR>
		/// <LF>
		/// <LS>
		/// <PS>
		/// <USP>
		/// <BOM>
		/// </summary>
		internal static bool IsStrWhiteSpaceChar(int c)
		{
			switch (c)
			{
				case ' ':
				case '\n':
				case '\r':
				case '\t':
				case '\u00A0':
				case '\u000C':
				case '\u000B':
				case '\u2028':
				case '\u2029':
				case '\uFEFF':
				{
					// <SP>
					// <LF>
					// <CR>
					// <TAB>
					// <NBSP>
					// <FF>
					// <VT>
					// <LS>
					// <PS>
					// <BOM>
					return true;
				}

				default:
				{
					return char.GetType(c) == char.SPACE_SEPARATOR;
				}
			}
		}

		public static bool WrapBoolean(bool b)
		{
			return b ? true : false;
		}

		public static int WrapInt(int i)
		{
			return Sharpen.Extensions.ValueOf(i);
		}

		public static Number WrapNumber(double x)
		{
			if (x != x)
			{
				return ScriptRuntime.NaNobj;
			}
			return x;
		}

		/// <summary>Convert the value to a boolean.</summary>
		/// <remarks>
		/// Convert the value to a boolean.
		/// See ECMA 9.2.
		/// </remarks>
		public static bool ToBoolean(object val)
		{
			for (; ; )
			{
				if (val is bool)
				{
					return ((bool)val);
				}
				if (val == null || val == Undefined.instance)
				{
					return false;
				}
				if (val is CharSequence)
				{
					return ((CharSequence)val).Length != 0;
				}
				if (val is Number)
				{
					double d = System.Convert.ToDouble(((Number)val));
					return (d == d && d != 0.0);
				}
				if (val is Scriptable)
				{
					if (val is ScriptableObject && ((ScriptableObject)val).AvoidObjectDetection())
					{
						return false;
					}
					if (Context.GetContext().IsVersionECMA1())
					{
						// pure ECMA
						return true;
					}
					// ECMA extension
					val = ((Scriptable)val).GetDefaultValue(BooleanClass);
					if (val is Scriptable)
					{
						throw ErrorWithClassName("msg.primitive.expected", val);
					}
					continue;
				}
				WarnAboutNonJSObject(val);
				return true;
			}
		}

		/// <summary>Convert the value to a number.</summary>
		/// <remarks>
		/// Convert the value to a number.
		/// See ECMA 9.3.
		/// </remarks>
		public static double ToNumber(object val)
		{
			for (; ; )
			{
				if (val is Number)
				{
					return System.Convert.ToDouble(((Number)val));
				}
				if (val == null)
				{
					return +0.0;
				}
				if (val == Undefined.instance)
				{
					return NaN;
				}
				if (val is string)
				{
					return ToNumber((string)val);
				}
				if (val is CharSequence)
				{
					return ToNumber(val.ToString());
				}
				if (val is bool)
				{
					return ((bool)val) ? 1 : +0.0;
				}
				if (val is Scriptable)
				{
					val = ((Scriptable)val).GetDefaultValue(NumberClass);
					if (val is Scriptable)
					{
						throw ErrorWithClassName("msg.primitive.expected", val);
					}
					continue;
				}
				WarnAboutNonJSObject(val);
				return NaN;
			}
		}

		public static double ToNumber(object[] args, int index)
		{
			return (index < args.Length) ? ToNumber(args[index]) : NaN;
		}

		public static readonly double NaN = System.BitConverter.Int64BitsToDouble(unchecked((long)(0x7ff8000000000000L)));

		public static readonly double negativeZero = System.BitConverter.Int64BitsToDouble(unchecked((long)(0x8000000000000000L)));

		public static readonly double NaNobj = NaN;

		// Can not use Double.NaN defined as 0.0d / 0.0 as under the Microsoft VM,
		// versions 2.01 and 3.0P1, that causes some uses (returns at least) of
		// Double.NaN to be converted to 1.0.
		// So we use ScriptRuntime.NaN instead of Double.NaN.
		// A similar problem exists for negative zero.
		internal static double StringToNumber(string s, int start, int radix)
		{
			char digitMax = '9';
			char lowerCaseBound = 'a';
			char upperCaseBound = 'A';
			int len = s.Length;
			if (radix < 10)
			{
				digitMax = (char)('0' + radix - 1);
			}
			if (radix > 10)
			{
				lowerCaseBound = (char)('a' + radix - 10);
				upperCaseBound = (char)('A' + radix - 10);
			}
			int end;
			double sum = 0.0;
			for (end = start; end < len; end++)
			{
				char c = s[end];
				int newDigit;
				if ('0' <= c && c <= digitMax)
				{
					newDigit = c - '0';
				}
				else
				{
					if ('a' <= c && c < lowerCaseBound)
					{
						newDigit = c - 'a' + 10;
					}
					else
					{
						if ('A' <= c && c < upperCaseBound)
						{
							newDigit = c - 'A' + 10;
						}
						else
						{
							break;
						}
					}
				}
				sum = sum * radix + newDigit;
			}
			if (start == end)
			{
				return NaN;
			}
			if (sum >= 9007199254740992.0)
			{
				if (radix == 10)
				{
					try
					{
						return System.Double.Parse(Sharpen.Runtime.Substring(s, start, end));
					}
					catch (FormatException)
					{
						return NaN;
					}
				}
				else
				{
					if (radix == 2 || radix == 4 || radix == 8 || radix == 16 || radix == 32)
					{
						int bitShiftInChar = 1;
						int digit = 0;
						int SKIP_LEADING_ZEROS = 0;
						int FIRST_EXACT_53_BITS = 1;
						int AFTER_BIT_53 = 2;
						int ZEROS_AFTER_54 = 3;
						int MIXED_AFTER_54 = 4;
						int state = SKIP_LEADING_ZEROS;
						int exactBitsLimit = 53;
						double factor = 0.0;
						bool bit53 = false;
						// bit54 is the 54th bit (the first dropped from the mantissa)
						bool bit54 = false;
						for (; ; )
						{
							if (bitShiftInChar == 1)
							{
								if (start == end)
								{
									break;
								}
								digit = s[start++];
								if ('0' <= digit && digit <= '9')
								{
									digit -= '0';
								}
								else
								{
									if ('a' <= digit && digit <= 'z')
									{
										digit -= 'a' - 10;
									}
									else
									{
										digit -= 'A' - 10;
									}
								}
								bitShiftInChar = radix;
							}
							bitShiftInChar >>= 1;
							bool bit = (digit & bitShiftInChar) != 0;
							switch (state)
							{
								case SKIP_LEADING_ZEROS:
								{
									if (bit)
									{
										--exactBitsLimit;
										sum = 1.0;
										state = FIRST_EXACT_53_BITS;
									}
									break;
								}

								case FIRST_EXACT_53_BITS:
								{
									sum *= 2.0;
									if (bit)
									{
										sum += 1.0;
									}
									--exactBitsLimit;
									if (exactBitsLimit == 0)
									{
										bit53 = bit;
										state = AFTER_BIT_53;
									}
									break;
								}

								case AFTER_BIT_53:
								{
									bit54 = bit;
									factor = 2.0;
									state = ZEROS_AFTER_54;
									break;
								}

								case ZEROS_AFTER_54:
								{
									if (bit)
									{
										state = MIXED_AFTER_54;
									}
									goto case MIXED_AFTER_54;
								}

								case MIXED_AFTER_54:
								{
									// fallthrough
									factor *= 2;
									break;
								}
							}
						}
						switch (state)
						{
							case SKIP_LEADING_ZEROS:
							{
								sum = 0.0;
								break;
							}

							case FIRST_EXACT_53_BITS:
							case AFTER_BIT_53:
							{
								// do nothing
								break;
							}

							case ZEROS_AFTER_54:
							{
								// x1.1 -> x1 + 1 (round up)
								// x0.1 -> x0 (round down)
								if (bit54 & bit53)
								{
									sum += 1.0;
								}
								sum *= factor;
								break;
							}

							case MIXED_AFTER_54:
							{
								// x.100...1.. -> x + 1 (round up)
								// x.0anything -> x (round down)
								if (bit54)
								{
									sum += 1.0;
								}
								sum *= factor;
								break;
							}
						}
					}
				}
			}
			return sum;
		}

		/// <summary>
		/// ToNumber applied to the String type
		/// See ECMA 9.3.1
		/// </summary>
		public static double ToNumber(string s)
		{
			int len = s.Length;
			int start = 0;
			char startChar;
			for (; ; )
			{
				if (start == len)
				{
					// Empty or contains only whitespace
					return +0.0;
				}
				startChar = s[start];
				if (!ScriptRuntime.IsStrWhiteSpaceChar(startChar))
				{
					break;
				}
				start++;
			}
			if (startChar == '0')
			{
				if (start + 2 < len)
				{
					int c1 = s[start + 1];
					if (c1 == 'x' || c1 == 'X')
					{
						// A hexadecimal number
						return StringToNumber(s, start + 2, 16);
					}
				}
			}
			else
			{
				if (startChar == '+' || startChar == '-')
				{
					if (start + 3 < len && s[start + 1] == '0')
					{
						int c2 = s[start + 2];
						if (c2 == 'x' || c2 == 'X')
						{
							// A hexadecimal number with sign
							double val = StringToNumber(s, start + 3, 16);
							return startChar == '-' ? -val : val;
						}
					}
				}
			}
			int end = len - 1;
			char endChar;
			while (ScriptRuntime.IsStrWhiteSpaceChar(endChar = s[end]))
			{
				end--;
			}
			if (endChar == 'y')
			{
				// check for "Infinity"
				if (startChar == '+' || startChar == '-')
				{
					start++;
				}
				if (start + 7 == end && s.RegionMatches(start, "Infinity", 0, 8))
				{
					return startChar == '-' ? double.NegativeInfinity : double.PositiveInfinity;
				}
				return NaN;
			}
			// A non-hexadecimal, non-infinity number:
			// just try a normal floating point conversion
			string sub = Sharpen.Runtime.Substring(s, start, end + 1);
			// Quick test to check string contains only valid characters because
			// Double.parseDouble() can be slow and accept input we want to reject
			for (int i = sub.Length - 1; i >= 0; i--)
			{
				char c = sub[i];
				if (('0' <= c && c <= '9') || c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-')
				{
					continue;
				}
				return NaN;
			}
			try
			{
				return System.Double.Parse(sub);
			}
			catch (FormatException)
			{
				return NaN;
			}
		}

		/// <summary>Helper function for builtin objects that use the varargs form.</summary>
		/// <remarks>
		/// Helper function for builtin objects that use the varargs form.
		/// ECMA function formal arguments are undefined if not supplied;
		/// this function pads the argument array out to the expected
		/// length, if necessary.
		/// </remarks>
		public static object[] PadArguments(object[] args, int count)
		{
			if (count < args.Length)
			{
				return args;
			}
			int i;
			object[] result = new object[count];
			for (i = 0; i < args.Length; i++)
			{
				result[i] = args[i];
			}
			for (; i < count; i++)
			{
				result[i] = Undefined.instance;
			}
			return result;
		}

		public static string EscapeString(string s)
		{
			return EscapeString(s, '"');
		}

		/// <summary>
		/// For escaping strings printed by object and array literals; not quite
		/// the same as 'escape.'
		/// </summary>
		public static string EscapeString(string s, char escapeQuote)
		{
			if (!(escapeQuote == '"' || escapeQuote == '\''))
			{
				Kit.CodeBug();
			}
			StringBuilder sb = null;
			for (int i = 0, L = s.Length; i != L; ++i)
			{
				int c = s[i];
				if (' ' <= c && c <= '~' && c != escapeQuote && c != '\\')
				{
					// an ordinary print character (like C isprint()) and not "
					// or \ .
					if (sb != null)
					{
						sb.Append((char)c);
					}
					continue;
				}
				if (sb == null)
				{
					sb = new StringBuilder(L + 3);
					sb.Append(s);
					sb.Length = i;
				}
				int escape = -1;
				switch (c)
				{
					case '\b':
					{
						escape = 'b';
						break;
					}

					case '\f':
					{
						escape = 'f';
						break;
					}

					case '\n':
					{
						escape = 'n';
						break;
					}

					case '\r':
					{
						escape = 'r';
						break;
					}

					case '\t':
					{
						escape = 't';
						break;
					}

					case unchecked((int)(0xb)):
					{
						escape = 'v';
						break;
					}

					case ' ':
					{
						// Java lacks \v.
						escape = ' ';
						break;
					}

					case '\\':
					{
						escape = '\\';
						break;
					}
				}
				if (escape >= 0)
				{
					// an \escaped sort of character
					sb.Append('\\');
					sb.Append((char)escape);
				}
				else
				{
					if (c == escapeQuote)
					{
						sb.Append('\\');
						sb.Append(escapeQuote);
					}
					else
					{
						int hexSize;
						if (c < 256)
						{
							// 2-digit hex
							sb.Append("\\x");
							hexSize = 2;
						}
						else
						{
							// Unicode.
							sb.Append("\\u");
							hexSize = 4;
						}
						// append hexadecimal form of c left-padded with 0
						for (int shift = (hexSize - 1) * 4; shift >= 0; shift -= 4)
						{
							int digit = unchecked((int)(0xf)) & (c >> shift);
							int hc = (digit < 10) ? '0' + digit : 'a' - 10 + digit;
							sb.Append((char)hc);
						}
					}
				}
			}
			return (sb == null) ? s : sb.ToString();
		}

		internal static bool IsValidIdentifierName(string s)
		{
			int L = s.Length;
			if (L == 0)
			{
				return false;
			}
			if (!char.IsJavaIdentifierStart(s[0]))
			{
				return false;
			}
			for (int i = 1; i != L; ++i)
			{
				if (!char.IsJavaIdentifierPart(s[i]))
				{
					return false;
				}
			}
			return !TokenStream.IsKeyword(s);
		}

		public static CharSequence ToCharSequence(object val)
		{
			if (val is NativeString)
			{
				return ((NativeString)val).ToCharSequence();
			}
			return val is CharSequence ? (CharSequence)val : ToString(val);
		}

		/// <summary>Convert the value to a string.</summary>
		/// <remarks>
		/// Convert the value to a string.
		/// See ECMA 9.8.
		/// </remarks>
		public static string ToString(object val)
		{
			for (; ; )
			{
				if (val == null)
				{
					return "null";
				}
				if (val == Undefined.instance)
				{
					return "undefined";
				}
				if (val is string)
				{
					return (string)val;
				}
				if (val is CharSequence)
				{
					return val.ToString();
				}
				if (val is Number)
				{
					// XXX should we just teach NativeNumber.stringValue()
					// about Numbers?
					return NumberToString(System.Convert.ToDouble(((Number)val)), 10);
				}
				if (val is Scriptable)
				{
					val = ((Scriptable)val).GetDefaultValue(StringClass);
					if (val is Scriptable)
					{
						throw ErrorWithClassName("msg.primitive.expected", val);
					}
					continue;
				}
				return val.ToString();
			}
		}

		internal static string DefaultObjectToString(Scriptable obj)
		{
			return "[object " + obj.GetClassName() + ']';
		}

		public static string ToString(object[] args, int index)
		{
			return (index < args.Length) ? ToString(args[index]) : "undefined";
		}

		/// <summary>Optimized version of toString(Object) for numbers.</summary>
		/// <remarks>Optimized version of toString(Object) for numbers.</remarks>
		public static string ToString(double val)
		{
			return NumberToString(val, 10);
		}

		public static string NumberToString(double d, int @base)
		{
			if (d != d)
			{
				return "NaN";
			}
			if (d == double.PositiveInfinity)
			{
				return "Infinity";
			}
			if (d == double.NegativeInfinity)
			{
				return "-Infinity";
			}
			if (d == 0.0)
			{
				return "0";
			}
			if ((@base < 2) || (@base > 36))
			{
				throw Context.ReportRuntimeError1("msg.bad.radix", Sharpen.Extensions.ToString(@base));
			}
			if (@base != 10)
			{
				return DToA.JS_dtobasestr(@base, d);
			}
			else
			{
				// V8 FastDtoa can't convert all numbers, so try it first but
				// fall back to old DToA in case it fails
				string result = FastDtoa.NumberToString(d);
				if (result != null)
				{
					return result;
				}
				StringBuilder buffer = new StringBuilder();
				DToA.JS_dtostr(buffer, DToA.DTOSTR_STANDARD, 0, d);
				return buffer.ToString();
			}
		}

		internal static string Uneval(Context cx, Scriptable scope, object value)
		{
			if (value == null)
			{
				return "null";
			}
			if (value == Undefined.instance)
			{
				return "undefined";
			}
			if (value is CharSequence)
			{
				string escaped = EscapeString(value.ToString());
				StringBuilder sb = new StringBuilder(escaped.Length + 2);
				sb.Append('\"');
				sb.Append(escaped);
				sb.Append('\"');
				return sb.ToString();
			}
			if (value is Number)
			{
				double d = System.Convert.ToDouble(((Number)value));
				if (d == 0 && 1 / d < 0)
				{
					return "-0";
				}
				return ToString(d);
			}
			if (value is bool)
			{
				return ToString(value);
			}
			if (value is Scriptable)
			{
				Scriptable obj = (Scriptable)value;
				// Wrapped Java objects won't have "toSource" and will report
				// errors for get()s of nonexistent name, so use has() first
				if (ScriptableObject.HasProperty(obj, "toSource"))
				{
					object v = ScriptableObject.GetProperty(obj, "toSource");
					if (v is Function)
					{
						Function f = (Function)v;
						return ToString(f.Call(cx, scope, obj, emptyArgs));
					}
				}
				return ToString(value);
			}
			WarnAboutNonJSObject(value);
			return value.ToString();
		}

		internal static string DefaultObjectToSource(Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			bool toplevel;
			bool iterating;
			if (cx.iterating == null)
			{
				toplevel = true;
				iterating = false;
				cx.iterating = new ObjToIntMap(31);
			}
			else
			{
				toplevel = false;
				iterating = cx.iterating.Has(thisObj);
			}
			StringBuilder result = new StringBuilder(128);
			if (toplevel)
			{
				result.Append("(");
			}
			result.Append('{');
			// Make sure cx.iterating is set to null when done
			// so we don't leak memory
			try
			{
				if (!iterating)
				{
					cx.iterating.Intern(thisObj);
					// stop recursion.
					object[] ids = thisObj.GetIds();
					for (int i = 0; i < ids.Length; i++)
					{
						object id = ids[i];
						object value;
						if (id is int)
						{
							int intId = System.Convert.ToInt32(((int)id));
							value = thisObj.Get(intId, thisObj);
							if (value == ScriptableConstants.NOT_FOUND)
							{
								continue;
							}
							// a property has been removed
							if (i > 0)
							{
								result.Append(", ");
							}
							result.Append(intId);
						}
						else
						{
							string strId = (string)id;
							value = thisObj.Get(strId, thisObj);
							if (value == ScriptableConstants.NOT_FOUND)
							{
								continue;
							}
							// a property has been removed
							if (i > 0)
							{
								result.Append(", ");
							}
							if (ScriptRuntime.IsValidIdentifierName(strId))
							{
								result.Append(strId);
							}
							else
							{
								result.Append('\'');
								result.Append(ScriptRuntime.EscapeString(strId, '\''));
								result.Append('\'');
							}
						}
						result.Append(':');
						result.Append(ScriptRuntime.Uneval(cx, scope, value));
					}
				}
			}
			finally
			{
				if (toplevel)
				{
					cx.iterating = null;
				}
			}
			result.Append('}');
			if (toplevel)
			{
				result.Append(')');
			}
			return result.ToString();
		}

		public static Scriptable ToObject(Scriptable scope, object val)
		{
			if (val is Scriptable)
			{
				return (Scriptable)val;
			}
			return ToObject(Context.GetContext(), scope, val);
		}

		/// <summary>Warning: this doesn't allow to resolve primitive prototype properly when many top scopes are involved</summary>
		public static Scriptable ToObjectOrNull(Context cx, object obj)
		{
			if (obj is Scriptable)
			{
				return (Scriptable)obj;
			}
			else
			{
				if (obj != null && obj != Undefined.instance)
				{
					return ToObject(cx, GetTopCallScope(cx), obj);
				}
			}
			return null;
		}

		/// <param name="scope">the scope that should be used to resolve primitive prototype</param>
		public static Scriptable ToObjectOrNull(Context cx, object obj, Scriptable scope)
		{
			if (obj is Scriptable)
			{
				return (Scriptable)obj;
			}
			else
			{
				if (obj != null && obj != Undefined.instance)
				{
					return ToObject(cx, scope, obj);
				}
			}
			return null;
		}

		[System.ObsoleteAttribute(@"Use ToObject(Scriptable, object) instead.")]
		public static Scriptable ToObject(Scriptable scope, object val, Type staticClass)
		{
			if (val is Scriptable)
			{
				return (Scriptable)val;
			}
			return ToObject(Context.GetContext(), scope, val);
		}

		/// <summary>Convert the value to an object.</summary>
		/// <remarks>
		/// Convert the value to an object.
		/// See ECMA 9.9.
		/// </remarks>
		public static Scriptable ToObject(Context cx, Scriptable scope, object val)
		{
			if (val is Scriptable)
			{
				return (Scriptable)val;
			}
			if (val is CharSequence)
			{
				// FIXME we want to avoid toString() here, especially for concat()
				NativeString result = new NativeString((CharSequence)val);
				SetBuiltinProtoAndParent(result, scope, TopLevel.Builtins.String);
				return result;
			}
			if (val is Number)
			{
				NativeNumber result = new NativeNumber(System.Convert.ToDouble(((Number)val)));
				SetBuiltinProtoAndParent(result, scope, TopLevel.Builtins.Number);
				return result;
			}
			if (val is bool)
			{
				NativeBoolean result = new NativeBoolean(((bool)val));
				SetBuiltinProtoAndParent(result, scope, TopLevel.Builtins.Boolean);
				return result;
			}
			if (val == null)
			{
				throw TypeError0("msg.null.to.object");
			}
			if (val == Undefined.instance)
			{
				throw TypeError0("msg.undef.to.object");
			}
			// Extension: Wrap as a LiveConnect object.
			object wrapped = cx.GetWrapFactory().Wrap(cx, scope, val, null);
			if (wrapped is Scriptable)
			{
				return (Scriptable)wrapped;
			}
			throw ErrorWithClassName("msg.invalid.type", val);
		}

		[System.ObsoleteAttribute(@"Use ToObject(Context, Scriptable, object) instead.")]
		public static Scriptable ToObject(Context cx, Scriptable scope, object val, Type staticClass)
		{
			return ToObject(cx, scope, val);
		}

		[System.ObsoleteAttribute(@"The method is only present for compatibility.")]
		public static object Call(Context cx, object fun, object thisArg, object[] args, Scriptable scope)
		{
			if (!(fun is Function))
			{
				throw NotFunctionError(ToString(fun));
			}
			Function function = (Function)fun;
			Scriptable thisObj = ToObjectOrNull(cx, thisArg);
			if (thisObj == null)
			{
				throw UndefCallError(thisObj, "function");
			}
			return function.Call(cx, scope, thisObj, args);
		}

		public static Scriptable NewObject(Context cx, Scriptable scope, string constructorName, object[] args)
		{
			scope = ScriptableObject.GetTopLevelScope(scope);
			Function ctor = GetExistingCtor(cx, scope, constructorName);
			if (args == null)
			{
				args = ScriptRuntime.emptyArgs;
			}
			return ctor.Construct(cx, scope, args);
		}

		public static Scriptable NewBuiltinObject(Context cx, Scriptable scope, TopLevel.Builtins type, object[] args)
		{
			scope = ScriptableObject.GetTopLevelScope(scope);
			Function ctor = TopLevel.GetBuiltinCtor(cx, scope, type);
			if (args == null)
			{
				args = ScriptRuntime.emptyArgs;
			}
			return ctor.Construct(cx, scope, args);
		}

		/// <summary>See ECMA 9.4.</summary>
		/// <remarks>See ECMA 9.4.</remarks>
		public static double ToInteger(object val)
		{
			return ToInteger(ToNumber(val));
		}

		// convenience method
		public static double ToInteger(double d)
		{
			// if it's NaN
			if (d != d)
			{
				return +0.0;
			}
			if (d == 0.0 || d == double.PositiveInfinity || d == double.NegativeInfinity)
			{
				return d;
			}
			if (d > 0.0)
			{
				return Math.Floor(d);
			}
			else
			{
				return System.Math.Ceiling(d);
			}
		}

		public static double ToInteger(object[] args, int index)
		{
			return (index < args.Length) ? ToInteger(args[index]) : +0.0;
		}

		/// <summary>See ECMA 9.5.</summary>
		/// <remarks>See ECMA 9.5.</remarks>
		public static int ToInt32(object val)
		{
			// short circuit for common integer values
			if (val is int)
			{
				return System.Convert.ToInt32(((int)val));
			}
			return ToInt32(ToNumber(val));
		}

		public static int ToInt32(object[] args, int index)
		{
			return (index < args.Length) ? ToInt32(args[index]) : 0;
		}

		public static int ToInt32(double d)
		{
			int id = (int)d;
			if (id == d)
			{
				// This covers -0.0 as well
				return id;
			}
			if (d != d || d == double.PositiveInfinity || d == double.NegativeInfinity)
			{
				return 0;
			}
			d = (d >= 0) ? Math.Floor(d) : System.Math.Ceiling(d);
			double two32 = 4294967296.0;
			d = System.Math.IEEERemainder(d, two32);
			// (double)(long)d == d should hold here
			long l = (long)d;
			// returning (int)d does not work as d can be outside int range
			// but the result must always be 32 lower bits of l
			return (int)l;
		}

		/// <summary>See ECMA 9.6.</summary>
		/// <remarks>See ECMA 9.6.</remarks>
		/// <returns>long value representing 32 bits unsigned integer</returns>
		public static long ToUint32(double d)
		{
			long l = (long)d;
			if (l == d)
			{
				// This covers -0.0 as well
				return l & unchecked((long)(0xffffffffL));
			}
			if (d != d || d == double.PositiveInfinity || d == double.NegativeInfinity)
			{
				return 0;
			}
			d = (d >= 0) ? Math.Floor(d) : System.Math.Ceiling(d);
			// 0x100000000 gives me a numeric overflow...
			double two32 = 4294967296.0;
			l = (long)System.Math.IEEERemainder(d, two32);
			return l & unchecked((long)(0xffffffffL));
		}

		public static long ToUint32(object val)
		{
			return ToUint32(ToNumber(val));
		}

		/// <summary>See ECMA 9.7.</summary>
		/// <remarks>See ECMA 9.7.</remarks>
		public static char ToUint16(object val)
		{
			double d = ToNumber(val);
			int i = (int)d;
			if (i == d)
			{
				return (char)i;
			}
			if (d != d || d == double.PositiveInfinity || d == double.NegativeInfinity)
			{
				return 0;
			}
			d = (d >= 0) ? Math.Floor(d) : System.Math.Ceiling(d);
			int int16 = unchecked((int)(0x10000));
			i = (int)System.Math.IEEERemainder(d, int16);
			return (char)i;
		}

		private const string DEFAULT_NS_TAG = "__default_namespace__";

		// XXX: this is until setDefaultNamespace will learn how to store NS
		// properly and separates namespace form Scriptable.get etc.
		public static object SetDefaultNamespace(object @namespace, Context cx)
		{
			Scriptable scope = cx.currentActivationCall;
			if (scope == null)
			{
				scope = GetTopCallScope(cx);
			}
			XMLLib xmlLib = CurrentXMLLib(cx);
			object ns = xmlLib.ToDefaultXmlNamespace(cx, @namespace);
			// XXX : this should be in separated namesapce from Scriptable.get/put
			if (!scope.Has(DEFAULT_NS_TAG, scope))
			{
				// XXX: this is racy of cause
				ScriptableObject.DefineProperty(scope, DEFAULT_NS_TAG, ns, ScriptableObject.PERMANENT | ScriptableObject.DONTENUM);
			}
			else
			{
				scope.Put(DEFAULT_NS_TAG, scope, ns);
			}
			return Undefined.instance;
		}

		public static object SearchDefaultNamespace(Context cx)
		{
			Scriptable scope = cx.currentActivationCall;
			if (scope == null)
			{
				scope = GetTopCallScope(cx);
			}
			object nsObject;
			for (; ; )
			{
				Scriptable parent = scope.GetParentScope();
				if (parent == null)
				{
					nsObject = ScriptableObject.GetProperty(scope, DEFAULT_NS_TAG);
					if (nsObject == ScriptableConstants.NOT_FOUND)
					{
						return null;
					}
					break;
				}
				nsObject = scope.Get(DEFAULT_NS_TAG, scope);
				if (nsObject != ScriptableConstants.NOT_FOUND)
				{
					break;
				}
				scope = parent;
			}
			return nsObject;
		}

		public static object GetTopLevelProp(Scriptable scope, string id)
		{
			scope = ScriptableObject.GetTopLevelScope(scope);
			return ScriptableObject.GetProperty(scope, id);
		}

		internal static Function GetExistingCtor(Context cx, Scriptable scope, string constructorName)
		{
			object ctorVal = ScriptableObject.GetProperty(scope, constructorName);
			if (ctorVal is Function)
			{
				return (Function)ctorVal;
			}
			if (ctorVal == ScriptableConstants.NOT_FOUND)
			{
				throw Context.ReportRuntimeError1("msg.ctor.not.found", constructorName);
			}
			else
			{
				throw Context.ReportRuntimeError1("msg.not.ctor", constructorName);
			}
		}

		/// <summary>
		/// Return -1L if str is not an index, or the index value as lower 32
		/// bits of the result.
		/// </summary>
		/// <remarks>
		/// Return -1L if str is not an index, or the index value as lower 32
		/// bits of the result. Note that the result needs to be cast to an int
		/// in order to produce the actual index, which may be negative.
		/// </remarks>
		public static long IndexFromString(string str)
		{
			// The length of the decimal string representation of
			//  Integer.MAX_VALUE, 2147483647
			int MAX_VALUE_LENGTH = 10;
			int len = str.Length;
			if (len > 0)
			{
				int i = 0;
				bool negate = false;
				int c = str[0];
				if (c == '-')
				{
					if (len > 1)
					{
						c = str[1];
						if (c == '0')
						{
							return -1L;
						}
						// "-0" is not an index
						i = 1;
						negate = true;
					}
				}
				c -= '0';
				if (0 <= c && c <= 9 && len <= (negate ? MAX_VALUE_LENGTH + 1 : MAX_VALUE_LENGTH))
				{
					// Use negative numbers to accumulate index to handle
					// Integer.MIN_VALUE that is greater by 1 in absolute value
					// then Integer.MAX_VALUE
					int index = -c;
					int oldIndex = 0;
					i++;
					if (index != 0)
					{
						// Note that 00, 01, 000 etc. are not indexes
						while (i != len && 0 <= (c = str[i] - '0') && c <= 9)
						{
							oldIndex = index;
							index = 10 * index - c;
							i++;
						}
					}
					// Make sure all characters were consumed and that it couldn't
					// have overflowed.
					if (i == len && (oldIndex > (int.MinValue / 10) || (oldIndex == (int.MinValue / 10) && c <= (negate ? -(int.MinValue % 10) : (int.MaxValue % 10)))))
					{
						return unchecked((long)(0xFFFFFFFFL)) & (negate ? index : -index);
					}
				}
			}
			return -1L;
		}

		/// <summary>If str is a decimal presentation of Uint32 value, return it as long.</summary>
		/// <remarks>
		/// If str is a decimal presentation of Uint32 value, return it as long.
		/// Othewise return -1L;
		/// </remarks>
		public static long TestUint32String(string str)
		{
			// The length of the decimal string representation of
			//  UINT32_MAX_VALUE, 4294967296
			int MAX_VALUE_LENGTH = 10;
			int len = str.Length;
			if (1 <= len && len <= MAX_VALUE_LENGTH)
			{
				int c = str[0];
				c -= '0';
				if (c == 0)
				{
					// Note that 00,01 etc. are not valid Uint32 presentations
					return (len == 1) ? 0L : -1L;
				}
				if (1 <= c && c <= 9)
				{
					long v = c;
					for (int i = 1; i != len; ++i)
					{
						c = str[i] - '0';
						if (!(0 <= c && c <= 9))
						{
							return -1;
						}
						v = 10 * v + c;
					}
					// Check for overflow
					if (((long)(((ulong)v) >> 32)) == 0)
					{
						return v;
					}
				}
			}
			return -1;
		}

		/// <summary>
		/// If s represents index, then return index value wrapped as Integer
		/// and othewise return s.
		/// </summary>
		/// <remarks>
		/// If s represents index, then return index value wrapped as Integer
		/// and othewise return s.
		/// </remarks>
		internal static object GetIndexObject(string s)
		{
			long indexTest = IndexFromString(s);
			if (indexTest >= 0)
			{
				return Sharpen.Extensions.ValueOf((int)indexTest);
			}
			return s;
		}

		/// <summary>
		/// If d is exact int value, return its value wrapped as Integer
		/// and othewise return d converted to String.
		/// </summary>
		/// <remarks>
		/// If d is exact int value, return its value wrapped as Integer
		/// and othewise return d converted to String.
		/// </remarks>
		internal static object GetIndexObject(double d)
		{
			int i = (int)d;
			if (i == d)
			{
				return Sharpen.Extensions.ValueOf(i);
			}
			return ToString(d);
		}

		/// <summary>
		/// If toString(id) is a decimal presentation of int32 value, then id
		/// is index.
		/// </summary>
		/// <remarks>
		/// If toString(id) is a decimal presentation of int32 value, then id
		/// is index. In this case return null and make the index available
		/// as ScriptRuntime.lastIndexResult(cx). Otherwise return toString(id).
		/// </remarks>
		internal static string ToStringIdOrIndex(Context cx, object id)
		{
			if (id is Number)
			{
				double d = System.Convert.ToDouble(((Number)id));
				int index = (int)d;
				if (index == d)
				{
					StoreIndexResult(cx, index);
					return null;
				}
				return ToString(id);
			}
			else
			{
				string s;
				if (id is string)
				{
					s = (string)id;
				}
				else
				{
					s = ToString(id);
				}
				long indexTest = IndexFromString(s);
				if (indexTest >= 0)
				{
					StoreIndexResult(cx, (int)indexTest);
					return null;
				}
				return s;
			}
		}

		/// <summary>Call obj.[[Get]](id)</summary>
		public static object GetObjectElem(object obj, object elem, Context cx)
		{
			return GetObjectElem(obj, elem, cx, GetTopCallScope(cx));
		}

		/// <summary>Call obj.[[Get]](id)</summary>
		public static object GetObjectElem(object obj, object elem, Context cx, Scriptable scope)
		{
			Scriptable sobj = ToObjectOrNull(cx, obj, scope);
			if (sobj == null)
			{
				throw UndefReadError(obj, elem);
			}
			return GetObjectElem(sobj, elem, cx);
		}

		public static object GetObjectElem(Scriptable obj, object elem, Context cx)
		{
			object result;
			if (obj is XMLObject)
			{
				result = ((XMLObject)obj).Get(cx, elem);
			}
			else
			{
				string s = ToStringIdOrIndex(cx, elem);
				if (s == null)
				{
					int index = LastIndexResult(cx);
					result = ScriptableObject.GetProperty(obj, index);
				}
				else
				{
					result = ScriptableObject.GetProperty(obj, s);
				}
			}
			if (result == ScriptableConstants.NOT_FOUND)
			{
				result = Undefined.instance;
			}
			return result;
		}

		/// <summary>Version of getObjectElem when elem is a valid JS identifier name.</summary>
		/// <remarks>Version of getObjectElem when elem is a valid JS identifier name.</remarks>
		public static object GetObjectProp(object obj, string property, Context cx)
		{
			Scriptable sobj = ToObjectOrNull(cx, obj);
			if (sobj == null)
			{
				throw UndefReadError(obj, property);
			}
			return GetObjectProp(sobj, property, cx);
		}

		/// <param name="scope">the scope that should be used to resolve primitive prototype</param>
		public static object GetObjectProp(object obj, string property, Context cx, Scriptable scope)
		{
			Scriptable sobj = ToObjectOrNull(cx, obj, scope);
			if (sobj == null)
			{
				throw UndefReadError(obj, property);
			}
			return GetObjectProp(sobj, property, cx);
		}

		public static object GetObjectProp(Scriptable obj, string property, Context cx)
		{
			object result = ScriptableObject.GetProperty(obj, property);
			if (result == ScriptableConstants.NOT_FOUND)
			{
				if (cx.HasFeature(Context.FEATURE_STRICT_MODE))
				{
					Context.ReportWarning(ScriptRuntime.GetMessage1("msg.ref.undefined.prop", property));
				}
				result = Undefined.instance;
			}
			return result;
		}

		public static object GetObjectPropNoWarn(object obj, string property, Context cx)
		{
			Scriptable sobj = ToObjectOrNull(cx, obj);
			if (sobj == null)
			{
				throw UndefReadError(obj, property);
			}
			object result = ScriptableObject.GetProperty(sobj, property);
			if (result == ScriptableConstants.NOT_FOUND)
			{
				return Undefined.instance;
			}
			return result;
		}

		public static object GetObjectIndex(object obj, double dblIndex, Context cx)
		{
			Scriptable sobj = ToObjectOrNull(cx, obj);
			if (sobj == null)
			{
				throw UndefReadError(obj, ToString(dblIndex));
			}
			int index = (int)dblIndex;
			if (index == dblIndex)
			{
				return GetObjectIndex(sobj, index, cx);
			}
			else
			{
				string s = ToString(dblIndex);
				return GetObjectProp(sobj, s, cx);
			}
		}

		public static object GetObjectIndex(Scriptable obj, int index, Context cx)
		{
			object result = ScriptableObject.GetProperty(obj, index);
			if (result == ScriptableConstants.NOT_FOUND)
			{
				result = Undefined.instance;
			}
			return result;
		}

		public static object SetObjectElem(object obj, object elem, object value, Context cx)
		{
			Scriptable sobj = ToObjectOrNull(cx, obj);
			if (sobj == null)
			{
				throw UndefWriteError(obj, elem, value);
			}
			return SetObjectElem(sobj, elem, value, cx);
		}

		public static object SetObjectElem(Scriptable obj, object elem, object value, Context cx)
		{
			if (obj is XMLObject)
			{
				((XMLObject)obj).Put(cx, elem, value);
			}
			else
			{
				string s = ToStringIdOrIndex(cx, elem);
				if (s == null)
				{
					int index = LastIndexResult(cx);
					ScriptableObject.PutProperty(obj, index, value);
				}
				else
				{
					ScriptableObject.PutProperty(obj, s, value);
				}
			}
			return value;
		}

		/// <summary>Version of setObjectElem when elem is a valid JS identifier name.</summary>
		/// <remarks>Version of setObjectElem when elem is a valid JS identifier name.</remarks>
		public static object SetObjectProp(object obj, string property, object value, Context cx)
		{
			Scriptable sobj = ToObjectOrNull(cx, obj);
			if (sobj == null)
			{
				throw UndefWriteError(obj, property, value);
			}
			return SetObjectProp(sobj, property, value, cx);
		}

		public static object SetObjectProp(Scriptable obj, string property, object value, Context cx)
		{
			ScriptableObject.PutProperty(obj, property, value);
			return value;
		}

		public static object SetObjectIndex(object obj, double dblIndex, object value, Context cx)
		{
			Scriptable sobj = ToObjectOrNull(cx, obj);
			if (sobj == null)
			{
				throw UndefWriteError(obj, dblIndex.ToString(), value);
			}
			int index = (int)dblIndex;
			if (index == dblIndex)
			{
				return SetObjectIndex(sobj, index, value, cx);
			}
			else
			{
				string s = ToString(dblIndex);
				return SetObjectProp(sobj, s, value, cx);
			}
		}

		public static object SetObjectIndex(Scriptable obj, int index, object value, Context cx)
		{
			ScriptableObject.PutProperty(obj, index, value);
			return value;
		}

		public static bool DeleteObjectElem(Scriptable target, object elem, Context cx)
		{
			string s = ToStringIdOrIndex(cx, elem);
			if (s == null)
			{
				int index = LastIndexResult(cx);
				target.Delete(index);
				return !target.Has(index, target);
			}
			else
			{
				target.Delete(s);
				return !target.Has(s, target);
			}
		}

		public static bool HasObjectElem(Scriptable target, object elem, Context cx)
		{
			bool result;
			string s = ToStringIdOrIndex(cx, elem);
			if (s == null)
			{
				int index = LastIndexResult(cx);
				result = ScriptableObject.HasProperty(target, index);
			}
			else
			{
				result = ScriptableObject.HasProperty(target, s);
			}
			return result;
		}

		public static object RefGet(Ref @ref, Context cx)
		{
			return @ref.Get(cx);
		}

		public static object RefSet(Ref @ref, object value, Context cx)
		{
			return @ref.Set(cx, value);
		}

		public static object RefDel(Ref @ref, Context cx)
		{
			return WrapBoolean(@ref.Delete(cx));
		}

		internal static bool IsSpecialProperty(string s)
		{
			return s.Equals("__proto__") || s.Equals("__parent__");
		}

		public static Ref SpecialRef(object obj, string specialProperty, Context cx)
		{
			return SpecialRef.CreateSpecial(cx, obj, specialProperty);
		}

		[System.ObsoleteAttribute]
		public static object Delete(object obj, object id, Context cx)
		{
			return Delete(obj, id, cx, false);
		}

		/// <summary>
		/// The delete operator
		/// See ECMA 11.4.1
		/// In ECMA 0.19, the description of the delete operator (11.4.1)
		/// assumes that the [[Delete]] method returns a value.
		/// </summary>
		/// <remarks>
		/// The delete operator
		/// See ECMA 11.4.1
		/// In ECMA 0.19, the description of the delete operator (11.4.1)
		/// assumes that the [[Delete]] method returns a value. However,
		/// the definition of the [[Delete]] operator (8.6.2.5) does not
		/// define a return value. Here we assume that the [[Delete]]
		/// method doesn't return a value.
		/// </remarks>
		public static object Delete(object obj, object id, Context cx, bool isName)
		{
			Scriptable sobj = ToObjectOrNull(cx, obj);
			if (sobj == null)
			{
				if (isName)
				{
					return true;
				}
				throw UndefDeleteError(obj, id);
			}
			bool result = DeleteObjectElem(sobj, id, cx);
			return WrapBoolean(result);
		}

		/// <summary>Looks up a name in the scope chain and returns its value.</summary>
		/// <remarks>Looks up a name in the scope chain and returns its value.</remarks>
		public static object Name(Context cx, Scriptable scope, string name)
		{
			Scriptable parent = scope.GetParentScope();
			if (parent == null)
			{
				object result = TopScopeName(cx, scope, name);
				if (result == ScriptableConstants.NOT_FOUND)
				{
					throw NotFoundError(scope, name);
				}
				return result;
			}
			return NameOrFunction(cx, scope, parent, name, false);
		}

		private static object NameOrFunction(Context cx, Scriptable scope, Scriptable parentScope, string name, bool asFunctionCall)
		{
			object result;
			Scriptable thisObj = scope;
			// It is used only if asFunctionCall==true.
			XMLObject firstXMLObject = null;
			for (; ; )
			{
				if (scope is NativeWith)
				{
					Scriptable withObj = scope.GetPrototype();
					if (withObj is XMLObject)
					{
						XMLObject xmlObj = (XMLObject)withObj;
						if (xmlObj.Has(name, xmlObj))
						{
							// function this should be the target object of with
							thisObj = xmlObj;
							result = xmlObj.Get(name, xmlObj);
							break;
						}
						if (firstXMLObject == null)
						{
							firstXMLObject = xmlObj;
						}
					}
					else
					{
						result = ScriptableObject.GetProperty(withObj, name);
						if (result != ScriptableConstants.NOT_FOUND)
						{
							// function this should be the target object of with
							thisObj = withObj;
							break;
						}
					}
				}
				else
				{
					if (scope is NativeCall)
					{
						// NativeCall does not prototype chain and Scriptable.get
						// can be called directly.
						result = scope.Get(name, scope);
						if (result != ScriptableConstants.NOT_FOUND)
						{
							if (asFunctionCall)
							{
								// ECMA 262 requires that this for nested funtions
								// should be top scope
								thisObj = ScriptableObject.GetTopLevelScope(parentScope);
							}
							break;
						}
					}
					else
					{
						// Can happen if Rhino embedding decided that nested
						// scopes are useful for what ever reasons.
						result = ScriptableObject.GetProperty(scope, name);
						if (result != ScriptableConstants.NOT_FOUND)
						{
							thisObj = scope;
							break;
						}
					}
				}
				scope = parentScope;
				parentScope = parentScope.GetParentScope();
				if (parentScope == null)
				{
					result = TopScopeName(cx, scope, name);
					if (result == ScriptableConstants.NOT_FOUND)
					{
						if (firstXMLObject == null || asFunctionCall)
						{
							throw NotFoundError(scope, name);
						}
						// The name was not found, but we did find an XML
						// object in the scope chain and we are looking for name,
						// not function. The result should be an empty XMLList
						// in name context.
						result = firstXMLObject.Get(name, firstXMLObject);
					}
					// For top scope thisObj for functions is always scope itself.
					thisObj = scope;
					break;
				}
			}
			if (asFunctionCall)
			{
				if (!(result is Callable))
				{
					throw NotFunctionError(result, name);
				}
				StoreScriptable(cx, thisObj);
			}
			return result;
		}

		private static object TopScopeName(Context cx, Scriptable scope, string name)
		{
			if (cx.useDynamicScope)
			{
				scope = CheckDynamicScope(cx.topCallScope, scope);
			}
			return ScriptableObject.GetProperty(scope, name);
		}

		/// <summary>Returns the object in the scope chain that has a given property.</summary>
		/// <remarks>
		/// Returns the object in the scope chain that has a given property.
		/// The order of evaluation of an assignment expression involves
		/// evaluating the lhs to a reference, evaluating the rhs, and then
		/// modifying the reference with the rhs value. This method is used
		/// to 'bind' the given name to an object containing that property
		/// so that the side effects of evaluating the rhs do not affect
		/// which property is modified.
		/// Typically used in conjunction with setName.
		/// See ECMA 10.1.4
		/// </remarks>
		public static Scriptable Bind(Context cx, Scriptable scope, string id)
		{
			Scriptable firstXMLObject = null;
			Scriptable parent = scope.GetParentScope();
			if (parent != null)
			{
				// Check for possibly nested "with" scopes first
				while (scope is NativeWith)
				{
					Scriptable withObj = scope.GetPrototype();
					if (withObj is XMLObject)
					{
						XMLObject xmlObject = (XMLObject)withObj;
						if (xmlObject.Has(cx, id))
						{
							return xmlObject;
						}
						if (firstXMLObject == null)
						{
							firstXMLObject = xmlObject;
						}
					}
					else
					{
						if (ScriptableObject.HasProperty(withObj, id))
						{
							return withObj;
						}
					}
					scope = parent;
					parent = parent.GetParentScope();
					if (parent == null)
					{
						goto childScopesChecks_break;
					}
				}
				for (; ; )
				{
					if (ScriptableObject.HasProperty(scope, id))
					{
						return scope;
					}
					scope = parent;
					parent = parent.GetParentScope();
					if (parent == null)
					{
						goto childScopesChecks_break;
					}
				}
			}
childScopesChecks_break: ;
			// scope here is top scope
			if (cx.useDynamicScope)
			{
				scope = CheckDynamicScope(cx.topCallScope, scope);
			}
			if (ScriptableObject.HasProperty(scope, id))
			{
				return scope;
			}
			// Nothing was found, but since XML objects always bind
			// return one if found
			return firstXMLObject;
		}

		public static object SetName(Scriptable bound, object value, Context cx, Scriptable scope, string id)
		{
			if (bound != null)
			{
				// TODO: we used to special-case XMLObject here, but putProperty
				// seems to work for E4X and it's better to optimize  the common case
				ScriptableObject.PutProperty(bound, id, value);
			}
			else
			{
				// "newname = 7;", where 'newname' has not yet
				// been defined, creates a new property in the
				// top scope unless strict mode is specified.
				if (cx.HasFeature(Context.FEATURE_STRICT_MODE) || cx.HasFeature(Context.FEATURE_STRICT_VARS))
				{
					Context.ReportWarning(ScriptRuntime.GetMessage1("msg.assn.create.strict", id));
				}
				// Find the top scope by walking up the scope chain.
				bound = ScriptableObject.GetTopLevelScope(scope);
				if (cx.useDynamicScope)
				{
					bound = CheckDynamicScope(cx.topCallScope, bound);
				}
				bound.Put(id, bound, value);
			}
			return value;
		}

		public static object StrictSetName(Scriptable bound, object value, Context cx, Scriptable scope, string id)
		{
			if (bound != null)
			{
				// TODO: The LeftHandSide also may not be a reference to a
				// data property with the attribute value {[[Writable]]:false},
				// to an accessor property with the attribute value
				// {[[Put]]:undefined}, nor to a non-existent property of an
				// object whose [[Extensible]] internal property has the value
				// false. In these cases a TypeError exception is thrown (11.13.1).
				// TODO: we used to special-case XMLObject here, but putProperty
				// seems to work for E4X and we should optimize  the common case
				ScriptableObject.PutProperty(bound, id, value);
				return value;
			}
			else
			{
				// See ES5 8.7.2
				string msg = "Assignment to undefined \"" + id + "\" in strict mode";
				throw ConstructError("ReferenceError", msg);
			}
		}

		public static object SetConst(Scriptable bound, object value, Context cx, string id)
		{
			if (bound is XMLObject)
			{
				bound.Put(id, bound, value);
			}
			else
			{
				ScriptableObject.PutConstProperty(bound, id, value);
			}
			return value;
		}

		/// <summary>This is the enumeration needed by the for..in statement.</summary>
		/// <remarks>
		/// This is the enumeration needed by the for..in statement.
		/// See ECMA 12.6.3.
		/// IdEnumeration maintains a ObjToIntMap to make sure a given
		/// id is enumerated only once across multiple objects in a
		/// prototype chain.
		/// XXX - ECMA delete doesn't hide properties in the prototype,
		/// but js/ref does. This means that the js/ref for..in can
		/// avoid maintaining a hash table and instead perform lookups
		/// to see if a given property has already been enumerated.
		/// </remarks>
		[System.Serializable]
		private class IdEnumeration
		{
			private const long serialVersionUID = 1L;

			internal Scriptable obj;

			internal object[] ids;

			internal int index;

			internal ObjToIntMap used;

			internal object currentId;

			internal int enumType;

			internal bool enumNumbers;

			internal Scriptable iterator;
			// if true, integer ids will be returned as numbers rather than strings
		}

		public static Scriptable ToIterator(Context cx, Scriptable scope, Scriptable obj, bool keyOnly)
		{
			if (ScriptableObject.HasProperty(obj, NativeIterator.ITERATOR_PROPERTY_NAME))
			{
				object v = ScriptableObject.GetProperty(obj, NativeIterator.ITERATOR_PROPERTY_NAME);
				if (!(v is Callable))
				{
					throw TypeError0("msg.invalid.iterator");
				}
				Callable f = (Callable)v;
				object[] args = new object[] { keyOnly ? true : false };
				v = f.Call(cx, scope, obj, args);
				if (!(v is Scriptable))
				{
					throw TypeError0("msg.iterator.primitive");
				}
				return (Scriptable)v;
			}
			return null;
		}

		// for backwards compatibility with generated class files
		public static object EnumInit(object value, Context cx, bool enumValues)
		{
			return EnumInit(value, cx, enumValues ? ENUMERATE_VALUES : ENUMERATE_KEYS);
		}

		public const int ENUMERATE_KEYS = 0;

		public const int ENUMERATE_VALUES = 1;

		public const int ENUMERATE_ARRAY = 2;

		public const int ENUMERATE_KEYS_NO_ITERATOR = 3;

		public const int ENUMERATE_VALUES_NO_ITERATOR = 4;

		public const int ENUMERATE_ARRAY_NO_ITERATOR = 5;

		public static object EnumInit(object value, Context cx, int enumType)
		{
			ScriptRuntime.IdEnumeration x = new ScriptRuntime.IdEnumeration();
			x.obj = ToObjectOrNull(cx, value);
			if (x.obj == null)
			{
				// null or undefined do not cause errors but rather lead to empty
				// "for in" loop
				return x;
			}
			x.enumType = enumType;
			x.iterator = null;
			if (enumType != ENUMERATE_KEYS_NO_ITERATOR && enumType != ENUMERATE_VALUES_NO_ITERATOR && enumType != ENUMERATE_ARRAY_NO_ITERATOR)
			{
				x.iterator = ToIterator(cx, x.obj.GetParentScope(), x.obj, enumType == ScriptRuntime.ENUMERATE_KEYS);
			}
			if (x.iterator == null)
			{
				// enumInit should read all initial ids before returning
				// or "for (a.i in a)" would wrongly enumerate i in a as well
				EnumChangeObject(x);
			}
			return x;
		}

		public static void SetEnumNumbers(object enumObj, bool enumNumbers)
		{
			((ScriptRuntime.IdEnumeration)enumObj).enumNumbers = enumNumbers;
		}

		public static bool EnumNext(object enumObj)
		{
			ScriptRuntime.IdEnumeration x = (ScriptRuntime.IdEnumeration)enumObj;
			if (x.iterator != null)
			{
				object v = ScriptableObject.GetProperty(x.iterator, "next");
				if (!(v is Callable))
				{
					return false;
				}
				Callable f = (Callable)v;
				Context cx = Context.GetContext();
				try
				{
					x.currentId = f.Call(cx, x.iterator.GetParentScope(), x.iterator, emptyArgs);
					return true;
				}
				catch (JavaScriptException e)
				{
					if (e.GetValue() is NativeIterator.StopIteration)
					{
						return false;
					}
					throw;
				}
			}
			for (; ; )
			{
				if (x.obj == null)
				{
					return false;
				}
				if (x.index == x.ids.Length)
				{
					x.obj = x.obj.GetPrototype();
					EnumChangeObject(x);
					continue;
				}
				object id = x.ids[x.index++];
				if (x.used != null && x.used.Has(id))
				{
					continue;
				}
				if (id is string)
				{
					string strId = (string)id;
					if (!x.obj.Has(strId, x.obj))
					{
						continue;
					}
					// must have been deleted
					x.currentId = strId;
				}
				else
				{
					int intId = System.Convert.ToInt32(((Number)id));
					if (!x.obj.Has(intId, x.obj))
					{
						continue;
					}
					// must have been deleted
					x.currentId = x.enumNumbers ? (object)(Sharpen.Extensions.ValueOf(intId)) : intId.ToString();
				}
				return true;
			}
		}

		public static object EnumId(object enumObj, Context cx)
		{
			ScriptRuntime.IdEnumeration x = (ScriptRuntime.IdEnumeration)enumObj;
			if (x.iterator != null)
			{
				return x.currentId;
			}
			switch (x.enumType)
			{
				case ENUMERATE_KEYS:
				case ENUMERATE_KEYS_NO_ITERATOR:
				{
					return x.currentId;
				}

				case ENUMERATE_VALUES:
				case ENUMERATE_VALUES_NO_ITERATOR:
				{
					return EnumValue(enumObj, cx);
				}

				case ENUMERATE_ARRAY:
				case ENUMERATE_ARRAY_NO_ITERATOR:
				{
					object[] elements = new object[] { x.currentId, EnumValue(enumObj, cx) };
					return cx.NewArray(ScriptableObject.GetTopLevelScope(x.obj), elements);
				}

				default:
				{
					throw Kit.CodeBug();
				}
			}
		}

		public static object EnumValue(object enumObj, Context cx)
		{
			ScriptRuntime.IdEnumeration x = (ScriptRuntime.IdEnumeration)enumObj;
			object result;
			string s = ToStringIdOrIndex(cx, x.currentId);
			if (s == null)
			{
				int index = LastIndexResult(cx);
				result = x.obj.Get(index, x.obj);
			}
			else
			{
				result = x.obj.Get(s, x.obj);
			}
			return result;
		}

		private static void EnumChangeObject(ScriptRuntime.IdEnumeration x)
		{
			object[] ids = null;
			while (x.obj != null)
			{
				ids = x.obj.GetIds();
				if (ids.Length != 0)
				{
					break;
				}
				x.obj = x.obj.GetPrototype();
			}
			if (x.obj != null && x.ids != null)
			{
				object[] previous = x.ids;
				int L = previous.Length;
				if (x.used == null)
				{
					x.used = new ObjToIntMap(L);
				}
				for (int i = 0; i != L; ++i)
				{
					x.used.Intern(previous[i]);
				}
			}
			x.ids = ids;
			x.index = 0;
		}

		/// <summary>
		/// Prepare for calling name(...): return function corresponding to
		/// name and make current top scope available
		/// as ScriptRuntime.lastStoredScriptable() for consumption as thisObj.
		/// </summary>
		/// <remarks>
		/// Prepare for calling name(...): return function corresponding to
		/// name and make current top scope available
		/// as ScriptRuntime.lastStoredScriptable() for consumption as thisObj.
		/// The caller must call ScriptRuntime.lastStoredScriptable() immediately
		/// after calling this method.
		/// </remarks>
		public static Callable GetNameFunctionAndThis(string name, Context cx, Scriptable scope)
		{
			Scriptable parent = scope.GetParentScope();
			if (parent == null)
			{
				object result = TopScopeName(cx, scope, name);
				if (!(result is Callable))
				{
					if (result == ScriptableConstants.NOT_FOUND)
					{
						throw NotFoundError(scope, name);
					}
					else
					{
						throw NotFunctionError(result, name);
					}
				}
				// Top scope is not NativeWith or NativeCall => thisObj == scope
				Scriptable thisObj = scope;
				StoreScriptable(cx, thisObj);
				return (Callable)result;
			}
			// name will call storeScriptable(cx, thisObj);
			return (Callable)NameOrFunction(cx, scope, parent, name, true);
		}

		/// <summary>
		/// Prepare for calling obj[id](...): return function corresponding to
		/// obj[id] and make obj properly converted to Scriptable available
		/// as ScriptRuntime.lastStoredScriptable() for consumption as thisObj.
		/// </summary>
		/// <remarks>
		/// Prepare for calling obj[id](...): return function corresponding to
		/// obj[id] and make obj properly converted to Scriptable available
		/// as ScriptRuntime.lastStoredScriptable() for consumption as thisObj.
		/// The caller must call ScriptRuntime.lastStoredScriptable() immediately
		/// after calling this method.
		/// </remarks>
		public static Callable GetElemFunctionAndThis(object obj, object elem, Context cx)
		{
			string str = ToStringIdOrIndex(cx, elem);
			if (str != null)
			{
				return GetPropFunctionAndThis(obj, str, cx);
			}
			int index = LastIndexResult(cx);
			Scriptable thisObj = ToObjectOrNull(cx, obj);
			if (thisObj == null)
			{
				throw UndefCallError(obj, index.ToString());
			}
			object value = ScriptableObject.GetProperty(thisObj, index);
			if (!(value is Callable))
			{
				throw NotFunctionError(value, elem);
			}
			StoreScriptable(cx, thisObj);
			return (Callable)value;
		}

		/// <summary>
		/// Prepare for calling obj.property(...): return function corresponding to
		/// obj.property and make obj properly converted to Scriptable available
		/// as ScriptRuntime.lastStoredScriptable() for consumption as thisObj.
		/// </summary>
		/// <remarks>
		/// Prepare for calling obj.property(...): return function corresponding to
		/// obj.property and make obj properly converted to Scriptable available
		/// as ScriptRuntime.lastStoredScriptable() for consumption as thisObj.
		/// The caller must call ScriptRuntime.lastStoredScriptable() immediately
		/// after calling this method.
		/// Warning: this doesn't allow to resolve primitive prototype properly when
		/// many top scopes are involved.
		/// </remarks>
		public static Callable GetPropFunctionAndThis(object obj, string property, Context cx)
		{
			Scriptable thisObj = ToObjectOrNull(cx, obj);
			return GetPropFunctionAndThisHelper(obj, property, cx, thisObj);
		}

		/// <summary>
		/// Prepare for calling obj.property(...): return function corresponding to
		/// obj.property and make obj properly converted to Scriptable available
		/// as ScriptRuntime.lastStoredScriptable() for consumption as thisObj.
		/// </summary>
		/// <remarks>
		/// Prepare for calling obj.property(...): return function corresponding to
		/// obj.property and make obj properly converted to Scriptable available
		/// as ScriptRuntime.lastStoredScriptable() for consumption as thisObj.
		/// The caller must call ScriptRuntime.lastStoredScriptable() immediately
		/// after calling this method.
		/// </remarks>
		public static Callable GetPropFunctionAndThis(object obj, string property, Context cx, Scriptable scope)
		{
			Scriptable thisObj = ToObjectOrNull(cx, obj, scope);
			return GetPropFunctionAndThisHelper(obj, property, cx, thisObj);
		}

		private static Callable GetPropFunctionAndThisHelper(object obj, string property, Context cx, Scriptable thisObj)
		{
			if (thisObj == null)
			{
				throw UndefCallError(obj, property);
			}
			object value = ScriptableObject.GetProperty(thisObj, property);
			if (!(value is Callable))
			{
				object noSuchMethod = ScriptableObject.GetProperty(thisObj, "__noSuchMethod__");
				if (noSuchMethod is Callable)
				{
					value = new ScriptRuntime.NoSuchMethodShim((Callable)noSuchMethod, property);
				}
			}
			if (!(value is Callable))
			{
				throw NotFunctionError(thisObj, value, property);
			}
			StoreScriptable(cx, thisObj);
			return (Callable)value;
		}

		/// <summary>
		/// Prepare for calling <expression>(...): return function corresponding to
		/// <expression> and make parent scope of the function available
		/// as ScriptRuntime.lastStoredScriptable() for consumption as thisObj.
		/// </summary>
		/// <remarks>
		/// Prepare for calling <expression>(...): return function corresponding to
		/// <expression> and make parent scope of the function available
		/// as ScriptRuntime.lastStoredScriptable() for consumption as thisObj.
		/// The caller must call ScriptRuntime.lastStoredScriptable() immediately
		/// after calling this method.
		/// </remarks>
		public static Callable GetValueFunctionAndThis(object value, Context cx)
		{
			if (!(value is Callable))
			{
				throw NotFunctionError(value);
			}
			Callable f = (Callable)value;
			Scriptable thisObj = null;
			if (f is Scriptable)
			{
				thisObj = ((Scriptable)f).GetParentScope();
			}
			if (thisObj == null)
			{
				if (cx.topCallScope == null)
				{
					throw new InvalidOperationException();
				}
				thisObj = cx.topCallScope;
			}
			if (thisObj.GetParentScope() != null)
			{
				if (thisObj is NativeWith)
				{
				}
				else
				{
					// functions defined inside with should have with target
					// as their thisObj
					if (thisObj is NativeCall)
					{
						// nested functions should have top scope as their thisObj
						thisObj = ScriptableObject.GetTopLevelScope(thisObj);
					}
				}
			}
			StoreScriptable(cx, thisObj);
			return f;
		}

		/// <summary>Perform function call in reference context.</summary>
		/// <remarks>
		/// Perform function call in reference context. Should always
		/// return value that can be passed to
		/// <see cref="RefGet(Ref, Context)">RefGet(Ref, Context)</see>
		/// or
		/// <see cref="RefSet(Ref, object, Context)">RefSet(Ref, object, Context)</see>
		/// arbitrary number of times.
		/// The args array reference should not be stored in any object that is
		/// can be GC-reachable after this method returns. If this is necessary,
		/// store args.clone(), not args array itself.
		/// </remarks>
		public static Ref CallRef(Callable function, Scriptable thisObj, object[] args, Context cx)
		{
			if (function is RefCallable)
			{
				RefCallable rfunction = (RefCallable)function;
				Ref @ref = rfunction.RefCall(cx, thisObj, args);
				if (@ref == null)
				{
					throw new InvalidOperationException(rfunction.GetType().FullName + ".refCall() returned null");
				}
				return @ref;
			}
			// No runtime support for now
			string msg = GetMessage1("msg.no.ref.from.function", ToString(function));
			throw ConstructError("ReferenceError", msg);
		}

		/// <summary>Operator new.</summary>
		/// <remarks>
		/// Operator new.
		/// See ECMA 11.2.2
		/// </remarks>
		public static Scriptable NewObject(object fun, Context cx, Scriptable scope, object[] args)
		{
			if (!(fun is Function))
			{
				throw NotFunctionError(fun);
			}
			Function function = (Function)fun;
			return function.Construct(cx, scope, args);
		}

		public static object CallSpecial(Context cx, Callable fun, Scriptable thisObj, object[] args, Scriptable scope, Scriptable callerThis, int callType, string filename, int lineNumber)
		{
			if (callType == Node.SPECIALCALL_EVAL)
			{
				if (thisObj.GetParentScope() == null && NativeGlobal.IsEvalFunction(fun))
				{
					return EvalSpecial(cx, scope, callerThis, args, filename, lineNumber);
				}
			}
			else
			{
				if (callType == Node.SPECIALCALL_WITH)
				{
					if (NativeWith.IsWithFunction(fun))
					{
						throw Context.ReportRuntimeError1("msg.only.from.new", "With");
					}
				}
				else
				{
					throw Kit.CodeBug();
				}
			}
			return fun.Call(cx, scope, thisObj, args);
		}

		public static object NewSpecial(Context cx, object fun, object[] args, Scriptable scope, int callType)
		{
			if (callType == Node.SPECIALCALL_EVAL)
			{
				if (NativeGlobal.IsEvalFunction(fun))
				{
					throw TypeError1("msg.not.ctor", "eval");
				}
			}
			else
			{
				if (callType == Node.SPECIALCALL_WITH)
				{
					if (NativeWith.IsWithFunction(fun))
					{
						return NativeWith.NewWithSpecial(cx, scope, args);
					}
				}
				else
				{
					throw Kit.CodeBug();
				}
			}
			return NewObject(fun, cx, scope, args);
		}

		/// <summary>
		/// Function.prototype.apply and Function.prototype.call
		/// See Ecma 15.3.4.[34]
		/// </summary>
		public static object ApplyOrCall(bool isApply, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			int L = args.Length;
			Callable function = GetCallable(thisObj);
			Scriptable callThis = null;
			if (L != 0)
			{
				callThis = ToObjectOrNull(cx, args[0]);
			}
			if (callThis == null)
			{
				// This covers the case of args[0] == (null|undefined) as well.
				callThis = GetTopCallScope(cx);
			}
			object[] callArgs;
			if (isApply)
			{
				// Follow Ecma 15.3.4.3
				callArgs = L <= 1 ? ScriptRuntime.emptyArgs : GetApplyArguments(cx, args[1]);
			}
			else
			{
				// Follow Ecma 15.3.4.4
				if (L <= 1)
				{
					callArgs = ScriptRuntime.emptyArgs;
				}
				else
				{
					callArgs = new object[L - 1];
					System.Array.Copy(args, 1, callArgs, 0, L - 1);
				}
			}
			return function.Call(cx, scope, callThis, callArgs);
		}

		internal static object[] GetApplyArguments(Context cx, object arg1)
		{
			if (arg1 == null || arg1 == Undefined.instance)
			{
				return ScriptRuntime.emptyArgs;
			}
			else
			{
				if (arg1 is NativeArray || arg1 is Arguments)
				{
					return cx.GetElements((Scriptable)arg1);
				}
				else
				{
					throw ScriptRuntime.TypeError0("msg.arg.isnt.array");
				}
			}
		}

		internal static Callable GetCallable(Scriptable thisObj)
		{
			Callable function;
			if (thisObj is Callable)
			{
				function = (Callable)thisObj;
			}
			else
			{
				object value = thisObj.GetDefaultValue(ScriptRuntime.FunctionClass);
				if (!(value is Callable))
				{
					throw ScriptRuntime.NotFunctionError(value, thisObj);
				}
				function = (Callable)value;
			}
			return function;
		}

		/// <summary>The eval function property of the global object.</summary>
		/// <remarks>
		/// The eval function property of the global object.
		/// See ECMA 15.1.2.1
		/// </remarks>
		public static object EvalSpecial(Context cx, Scriptable scope, object thisArg, object[] args, string filename, int lineNumber)
		{
			if (args.Length < 1)
			{
				return Undefined.instance;
			}
			object x = args[0];
			if (!(x is CharSequence))
			{
				if (cx.HasFeature(Context.FEATURE_STRICT_MODE) || cx.HasFeature(Context.FEATURE_STRICT_EVAL))
				{
					throw Context.ReportRuntimeError0("msg.eval.nonstring.strict");
				}
				string message = ScriptRuntime.GetMessage0("msg.eval.nonstring");
				Context.ReportWarning(message);
				return x;
			}
			if (filename == null)
			{
				int[] linep = new int[1];
				filename = Context.GetSourcePositionFromStack(linep);
				if (filename != null)
				{
					lineNumber = linep[0];
				}
				else
				{
					filename = string.Empty;
				}
			}
			string sourceName = ScriptRuntime.MakeUrlForGeneratedScript(true, filename, lineNumber);
			ErrorReporter reporter;
			reporter = DefaultErrorReporter.ForEval(cx.GetErrorReporter());
			Evaluator evaluator = Context.CreateInterpreter();
			if (evaluator == null)
			{
				throw new JavaScriptException("Interpreter not present", filename, lineNumber);
			}
			// Compile with explicit interpreter instance to force interpreter
			// mode.
			Script script = cx.CompileString(x.ToString(), evaluator, reporter, sourceName, 1, null);
			evaluator.SetEvalScriptFlag(script);
			Callable c = (Callable)script;
			return c.Call(cx, scope, (Scriptable)thisArg, ScriptRuntime.emptyArgs);
		}

		/// <summary>The typeof operator</summary>
		public static string Typeof(object value)
		{
			if (value == null)
			{
				return "object";
			}
			if (value == Undefined.instance)
			{
				return "undefined";
			}
			if (value is ScriptableObject)
			{
				return ((ScriptableObject)value).GetTypeOf();
			}
			if (value is Scriptable)
			{
				return (value is Callable) ? "function" : "object";
			}
			if (value is CharSequence)
			{
				return "string";
			}
			if (value is Number)
			{
				return "number";
			}
			if (value is bool)
			{
				return "boolean";
			}
			throw ErrorWithClassName("msg.invalid.type", value);
		}

		/// <summary>The typeof operator that correctly handles the undefined case</summary>
		public static string TypeofName(Scriptable scope, string id)
		{
			Context cx = Context.GetContext();
			Scriptable val = Bind(cx, scope, id);
			if (val == null)
			{
				return "undefined";
			}
			return Typeof(GetObjectProp(val, id, cx));
		}

		// neg:
		// implement the '-' operator inline in the caller
		// as "-toNumber(val)"
		// not:
		// implement the '!' operator inline in the caller
		// as "!toBoolean(val)"
		// bitnot:
		// implement the '~' operator inline in the caller
		// as "~toInt32(val)"
		public static object Add(object val1, object val2, Context cx)
		{
			if (val1 is Number && val2 is Number)
			{
				return WrapNumber(System.Convert.ToDouble(((Number)val1)) + System.Convert.ToDouble(((Number)val2)));
			}
			if (val1 is XMLObject)
			{
				object test = ((XMLObject)val1).AddValues(cx, true, val2);
				if (test != ScriptableConstants.NOT_FOUND)
				{
					return test;
				}
			}
			if (val2 is XMLObject)
			{
				object test = ((XMLObject)val2).AddValues(cx, false, val1);
				if (test != ScriptableConstants.NOT_FOUND)
				{
					return test;
				}
			}
			if (val1 is Scriptable)
			{
				val1 = ((Scriptable)val1).GetDefaultValue(null);
			}
			if (val2 is Scriptable)
			{
				val2 = ((Scriptable)val2).GetDefaultValue(null);
			}
			if (!(val1 is CharSequence) && !(val2 is CharSequence))
			{
				if ((val1 is Number) && (val2 is Number))
				{
					return WrapNumber(System.Convert.ToDouble(((Number)val1)) + System.Convert.ToDouble(((Number)val2)));
				}
				else
				{
					return WrapNumber(ToNumber(val1) + ToNumber(val2));
				}
			}
			return new ConsString(ToCharSequence(val1), ToCharSequence(val2));
		}

		public static CharSequence Add(CharSequence val1, object val2)
		{
			return new ConsString(val1, ToCharSequence(val2));
		}

		public static CharSequence Add(object val1, CharSequence val2)
		{
			return new ConsString(ToCharSequence(val1), val2);
		}

		[System.ObsoleteAttribute(@"The method is only present for compatibility.")]
		public static object NameIncrDecr(Scriptable scopeChain, string id, int incrDecrMask)
		{
			return NameIncrDecr(scopeChain, id, Context.GetContext(), incrDecrMask);
		}

		public static object NameIncrDecr(Scriptable scopeChain, string id, Context cx, int incrDecrMask)
		{
			Scriptable target;
			object value;
			do
			{
				if (cx.useDynamicScope && scopeChain.GetParentScope() == null)
				{
					scopeChain = CheckDynamicScope(cx.topCallScope, scopeChain);
				}
				target = scopeChain;
				do
				{
					if (target is NativeWith && target.GetPrototype() is XMLObject)
					{
						break;
					}
					value = target.Get(id, scopeChain);
					if (value != ScriptableConstants.NOT_FOUND)
					{
						goto search_break;
					}
					target = target.GetPrototype();
				}
				while (target != null);
				scopeChain = scopeChain.GetParentScope();
			}
			while (scopeChain != null);
			throw NotFoundError(scopeChain, id);
search_break: ;
			return DoScriptableIncrDecr(target, id, scopeChain, value, incrDecrMask);
		}

		public static object PropIncrDecr(object obj, string id, Context cx, int incrDecrMask)
		{
			Scriptable start = ToObjectOrNull(cx, obj);
			if (start == null)
			{
				throw UndefReadError(obj, id);
			}
			Scriptable target = start;
			object value;
			do
			{
				value = target.Get(id, start);
				if (value != ScriptableConstants.NOT_FOUND)
				{
					goto search_break;
				}
				target = target.GetPrototype();
			}
			while (target != null);
			start.Put(id, start, NaNobj);
			return NaNobj;
search_break: ;
			return DoScriptableIncrDecr(target, id, start, value, incrDecrMask);
		}

		private static object DoScriptableIncrDecr(Scriptable target, string id, Scriptable protoChainStart, object value, int incrDecrMask)
		{
			bool post = ((incrDecrMask & Node.POST_FLAG) != 0);
			double number;
			if (value is Number)
			{
				number = System.Convert.ToDouble(((Number)value));
			}
			else
			{
				number = ToNumber(value);
				if (post)
				{
					// convert result to number
					value = WrapNumber(number);
				}
			}
			if ((incrDecrMask & Node.DECR_FLAG) == 0)
			{
				++number;
			}
			else
			{
				--number;
			}
			Number result = WrapNumber(number);
			target.Put(id, protoChainStart, result);
			if (post)
			{
				return value;
			}
			else
			{
				return result;
			}
		}

		public static object ElemIncrDecr(object obj, object index, Context cx, int incrDecrMask)
		{
			object value = GetObjectElem(obj, index, cx);
			bool post = ((incrDecrMask & Node.POST_FLAG) != 0);
			double number;
			if (value is Number)
			{
				number = System.Convert.ToDouble(((Number)value));
			}
			else
			{
				number = ToNumber(value);
				if (post)
				{
					// convert result to number
					value = WrapNumber(number);
				}
			}
			if ((incrDecrMask & Node.DECR_FLAG) == 0)
			{
				++number;
			}
			else
			{
				--number;
			}
			Number result = WrapNumber(number);
			SetObjectElem(obj, index, result, cx);
			if (post)
			{
				return value;
			}
			else
			{
				return result;
			}
		}

		public static object RefIncrDecr(Ref @ref, Context cx, int incrDecrMask)
		{
			object value = @ref.Get(cx);
			bool post = ((incrDecrMask & Node.POST_FLAG) != 0);
			double number;
			if (value is Number)
			{
				number = System.Convert.ToDouble(((Number)value));
			}
			else
			{
				number = ToNumber(value);
				if (post)
				{
					// convert result to number
					value = WrapNumber(number);
				}
			}
			if ((incrDecrMask & Node.DECR_FLAG) == 0)
			{
				++number;
			}
			else
			{
				--number;
			}
			Number result = WrapNumber(number);
			@ref.Set(cx, result);
			if (post)
			{
				return value;
			}
			else
			{
				return result;
			}
		}

		public static object ToPrimitive(object val)
		{
			return ToPrimitive(val, null);
		}

		public static object ToPrimitive(object val, Type typeHint)
		{
			if (!(val is Scriptable))
			{
				return val;
			}
			Scriptable s = (Scriptable)val;
			object result = s.GetDefaultValue(typeHint);
			if (result is Scriptable)
			{
				throw TypeError0("msg.bad.default.value");
			}
			return result;
		}

		/// <summary>
		/// Equality
		/// See ECMA 11.9
		/// </summary>
		public static bool Eq(object x, object y)
		{
			if (x == null || x == Undefined.instance)
			{
				if (y == null || y == Undefined.instance)
				{
					return true;
				}
				if (y is ScriptableObject)
				{
					object test = ((ScriptableObject)y).EquivalentValues(x);
					if (test != ScriptableConstants.NOT_FOUND)
					{
						return ((bool)test);
					}
				}
				return false;
			}
			else
			{
				if (x is Number)
				{
					return EqNumber(System.Convert.ToDouble(((Number)x)), y);
				}
				else
				{
					if (x == y)
					{
						return true;
					}
					else
					{
						if (x is CharSequence)
						{
							return EqString((CharSequence)x, y);
						}
						else
						{
							if (x is bool)
							{
								bool b = ((bool)x);
								if (y is bool)
								{
									return b == ((bool)y);
								}
								if (y is ScriptableObject)
								{
									object test = ((ScriptableObject)y).EquivalentValues(x);
									if (test != ScriptableConstants.NOT_FOUND)
									{
										return ((bool)test);
									}
								}
								return EqNumber(b ? 1.0 : 0.0, y);
							}
							else
							{
								if (x is Scriptable)
								{
									if (y is Scriptable)
									{
										if (x is ScriptableObject)
										{
											object test = ((ScriptableObject)x).EquivalentValues(y);
											if (test != ScriptableConstants.NOT_FOUND)
											{
												return ((bool)test);
											}
										}
										if (y is ScriptableObject)
										{
											object test = ((ScriptableObject)y).EquivalentValues(x);
											if (test != ScriptableConstants.NOT_FOUND)
											{
												return ((bool)test);
											}
										}
										if (x is Wrapper && y is Wrapper)
										{
											// See bug 413838. Effectively an extension to ECMA for
											// the LiveConnect case.
											object unwrappedX = ((Wrapper)x).Unwrap();
											object unwrappedY = ((Wrapper)y).Unwrap();
											return unwrappedX == unwrappedY || (IsPrimitive(unwrappedX) && IsPrimitive(unwrappedY) && Eq(unwrappedX, unwrappedY));
										}
										return false;
									}
									else
									{
										if (y is bool)
										{
											if (x is ScriptableObject)
											{
												object test = ((ScriptableObject)x).EquivalentValues(y);
												if (test != ScriptableConstants.NOT_FOUND)
												{
													return ((bool)test);
												}
											}
											double d = ((bool)y) ? 1.0 : 0.0;
											return EqNumber(d, x);
										}
										else
										{
											if (y is Number)
											{
												return EqNumber(System.Convert.ToDouble(((Number)y)), x);
											}
											else
											{
												if (y is CharSequence)
												{
													return EqString((CharSequence)y, x);
												}
											}
										}
									}
									// covers the case when y == Undefined.instance as well
									return false;
								}
								else
								{
									WarnAboutNonJSObject(x);
									return x == y;
								}
							}
						}
					}
				}
			}
		}

		public static bool IsPrimitive(object obj)
		{
			return obj == null || obj == Undefined.instance || (obj is Number) || (obj is string) || (obj is bool);
		}

		internal static bool EqNumber(double x, object y)
		{
			for (; ; )
			{
				if (y == null || y == Undefined.instance)
				{
					return false;
				}
				else
				{
					if (y is Number)
					{
						return x == System.Convert.ToDouble(((Number)y));
					}
					else
					{
						if (y is CharSequence)
						{
							return x == ToNumber(y);
						}
						else
						{
							if (y is bool)
							{
								return x == (((bool)y) ? 1.0 : +0.0);
							}
							else
							{
								if (y is Scriptable)
								{
									if (y is ScriptableObject)
									{
										object xval = WrapNumber(x);
										object test = ((ScriptableObject)y).EquivalentValues(xval);
										if (test != ScriptableConstants.NOT_FOUND)
										{
											return ((bool)test);
										}
									}
									y = ToPrimitive(y);
								}
								else
								{
									WarnAboutNonJSObject(y);
									return false;
								}
							}
						}
					}
				}
			}
		}

		private static bool EqString(CharSequence x, object y)
		{
			for (; ; )
			{
				if (y == null || y == Undefined.instance)
				{
					return false;
				}
				else
				{
					if (y is CharSequence)
					{
						CharSequence c = (CharSequence)y;
						return x.Length == c.Length && x.ToString().Equals(c.ToString());
					}
					else
					{
						if (y is Number)
						{
							return ToNumber(x.ToString()) == System.Convert.ToDouble(((Number)y));
						}
						else
						{
							if (y is bool)
							{
								return ToNumber(x.ToString()) == (((bool)y) ? 1.0 : 0.0);
							}
							else
							{
								if (y is Scriptable)
								{
									if (y is ScriptableObject)
									{
										object test = ((ScriptableObject)y).EquivalentValues(x.ToString());
										if (test != ScriptableConstants.NOT_FOUND)
										{
											return ((bool)test);
										}
									}
									y = ToPrimitive(y);
									continue;
								}
								else
								{
									WarnAboutNonJSObject(y);
									return false;
								}
							}
						}
					}
				}
			}
		}

		public static bool ShallowEq(object x, object y)
		{
			if (x == y)
			{
				if (!(x is Number))
				{
					return true;
				}
				// NaN check
				double d = System.Convert.ToDouble(((Number)x));
				return d == d;
			}
			if (x == null || x == Undefined.instance)
			{
				return false;
			}
			else
			{
				if (x is Number)
				{
					if (y is Number)
					{
						return System.Convert.ToDouble(((Number)x)) == System.Convert.ToDouble(((Number)y));
					}
				}
				else
				{
					if (x is CharSequence)
					{
						if (y is CharSequence)
						{
							return x.ToString().Equals(y.ToString());
						}
					}
					else
					{
						if (x is bool)
						{
							if (y is bool)
							{
								return x.Equals(y);
							}
						}
						else
						{
							if (x is Scriptable)
							{
								if (x is Wrapper && y is Wrapper)
								{
									return ((Wrapper)x).Unwrap() == ((Wrapper)y).Unwrap();
								}
							}
							else
							{
								WarnAboutNonJSObject(x);
								return x == y;
							}
						}
					}
				}
			}
			return false;
		}

		/// <summary>The instanceof operator.</summary>
		/// <remarks>The instanceof operator.</remarks>
		/// <returns>a instanceof b</returns>
		public static bool InstanceOf(object a, object b, Context cx)
		{
			// Check RHS is an object
			if (!(b is Scriptable))
			{
				throw TypeError0("msg.instanceof.not.object");
			}
			// for primitive values on LHS, return false
			if (!(a is Scriptable))
			{
				return false;
			}
			return ((Scriptable)b).HasInstance((Scriptable)a);
		}

		/// <summary>Delegates to</summary>
		/// <returns>true iff rhs appears in lhs' proto chain</returns>
		public static bool JsDelegatesTo(Scriptable lhs, Scriptable rhs)
		{
			Scriptable proto = lhs.GetPrototype();
			while (proto != null)
			{
				if (proto.Equals(rhs))
				{
					return true;
				}
				proto = proto.GetPrototype();
			}
			return false;
		}

		/// <summary>The in operator.</summary>
		/// <remarks>
		/// The in operator.
		/// This is a new JS 1.3 language feature.  The in operator mirrors
		/// the operation of the for .. in construct, and tests whether the
		/// rhs has the property given by the lhs.  It is different from the
		/// for .. in construct in that:
		/// <BR> - it doesn't perform ToObject on the right hand side
		/// <BR> - it returns true for DontEnum properties.
		/// </remarks>
		/// <param name="a">the left hand operand</param>
		/// <param name="b">the right hand operand</param>
		/// <returns>true if property name or element number a is a property of b</returns>
		public static bool In(object a, object b, Context cx)
		{
			if (!(b is Scriptable))
			{
				throw TypeError0("msg.in.not.object");
			}
			return HasObjectElem((Scriptable)b, a, cx);
		}

		public static bool Cmp_LT(object val1, object val2)
		{
			double d1;
			double d2;
			if (val1 is Number && val2 is Number)
			{
				d1 = System.Convert.ToDouble(((Number)val1));
				d2 = System.Convert.ToDouble(((Number)val2));
			}
			else
			{
				if (val1 is Scriptable)
				{
					val1 = ((Scriptable)val1).GetDefaultValue(NumberClass);
				}
				if (val2 is Scriptable)
				{
					val2 = ((Scriptable)val2).GetDefaultValue(NumberClass);
				}
				if (val1 is CharSequence && val2 is CharSequence)
				{
					return string.CompareOrdinal(val1.ToString(), val2.ToString()) < 0;
				}
				d1 = ToNumber(val1);
				d2 = ToNumber(val2);
			}
			return d1 < d2;
		}

		public static bool Cmp_LE(object val1, object val2)
		{
			double d1;
			double d2;
			if (val1 is Number && val2 is Number)
			{
				d1 = System.Convert.ToDouble(((Number)val1));
				d2 = System.Convert.ToDouble(((Number)val2));
			}
			else
			{
				if (val1 is Scriptable)
				{
					val1 = ((Scriptable)val1).GetDefaultValue(NumberClass);
				}
				if (val2 is Scriptable)
				{
					val2 = ((Scriptable)val2).GetDefaultValue(NumberClass);
				}
				if (val1 is CharSequence && val2 is CharSequence)
				{
					return string.CompareOrdinal(val1.ToString(), val2.ToString()) <= 0;
				}
				d1 = ToNumber(val1);
				d2 = ToNumber(val2);
			}
			return d1 <= d2;
		}

		// ------------------
		// Statements
		// ------------------
		public static ScriptableObject GetGlobal(Context cx)
		{
			string GLOBAL_CLASS = "org.mozilla.javascript.tools.shell.Global";
			Type globalClass = Kit.ClassOrNull(GLOBAL_CLASS);
			if (globalClass != null)
			{
				try
				{
					Type[] parm = new Type[] { ScriptRuntime.ContextClass };
					ConstructorInfo globalClassCtor = globalClass.GetConstructor(parm);
					object[] arg = new object[] { cx };
					return (ScriptableObject)globalClassCtor.NewInstance(arg);
				}
				catch (Exception e)
				{
					throw;
				}
				catch (Exception)
				{
				}
			}
			// fall through...
			return new ImporterTopLevel(cx);
		}

		public static bool HasTopCall(Context cx)
		{
			return (cx.topCallScope != null);
		}

		public static Scriptable GetTopCallScope(Context cx)
		{
			Scriptable scope = cx.topCallScope;
			if (scope == null)
			{
				throw new InvalidOperationException();
			}
			return scope;
		}

		public static object DoTopCall(Callable callable, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (scope == null)
			{
				throw new ArgumentException();
			}
			if (cx.topCallScope != null)
			{
				throw new InvalidOperationException();
			}
			object result;
			cx.topCallScope = ScriptableObject.GetTopLevelScope(scope);
			cx.useDynamicScope = cx.HasFeature(Context.FEATURE_DYNAMIC_SCOPE);
			ContextFactory f = cx.GetFactory();
			try
			{
				result = f.DoTopCall(callable, cx, scope, thisObj, args);
			}
			finally
			{
				cx.topCallScope = null;
				// Cleanup cached references
				cx.cachedXMLLib = null;
				if (cx.currentActivationCall != null)
				{
					// Function should always call exitActivationFunction
					// if it creates activation record
					throw new InvalidOperationException();
				}
			}
			return result;
		}

		/// <summary>
		/// Return <tt>possibleDynamicScope</tt> if <tt>staticTopScope</tt>
		/// is present on its prototype chain and return <tt>staticTopScope</tt>
		/// otherwise.
		/// </summary>
		/// <remarks>
		/// Return <tt>possibleDynamicScope</tt> if <tt>staticTopScope</tt>
		/// is present on its prototype chain and return <tt>staticTopScope</tt>
		/// otherwise.
		/// Should only be called when <tt>staticTopScope</tt> is top scope.
		/// </remarks>
		internal static Scriptable CheckDynamicScope(Scriptable possibleDynamicScope, Scriptable staticTopScope)
		{
			// Return cx.topCallScope if scope
			if (possibleDynamicScope == staticTopScope)
			{
				return possibleDynamicScope;
			}
			Scriptable proto = possibleDynamicScope;
			for (; ; )
			{
				proto = proto.GetPrototype();
				if (proto == staticTopScope)
				{
					return possibleDynamicScope;
				}
				if (proto == null)
				{
					return staticTopScope;
				}
			}
		}

		public static void AddInstructionCount(Context cx, int instructionsToAdd)
		{
			cx.instructionCount += instructionsToAdd;
			if (cx.instructionCount > cx.instructionThreshold)
			{
				cx.ObserveInstructionCount(cx.instructionCount);
				cx.instructionCount = 0;
			}
		}

		public static void InitScript(NativeFunction funObj, Scriptable thisObj, Context cx, Scriptable scope, bool evalScript)
		{
			if (cx.topCallScope == null)
			{
				throw new InvalidOperationException();
			}
			int varCount = funObj.GetParamAndVarCount();
			if (varCount != 0)
			{
				Scriptable varScope = scope;
				// Never define any variables from var statements inside with
				// object. See bug 38590.
				while (varScope is NativeWith)
				{
					varScope = varScope.GetParentScope();
				}
				for (int i = varCount; i-- != 0; )
				{
					string name = funObj.GetParamOrVarName(i);
					bool isConst = funObj.GetParamOrVarConst(i);
					// Don't overwrite existing def if already defined in object
					// or prototypes of object.
					if (!ScriptableObject.HasProperty(scope, name))
					{
						if (isConst)
						{
							ScriptableObject.DefineConstProperty(varScope, name);
						}
						else
						{
							if (!evalScript)
							{
								// Global var definitions are supposed to be DONTDELETE
								ScriptableObject.DefineProperty(varScope, name, Undefined.instance, ScriptableObject.PERMANENT);
							}
							else
							{
								varScope.Put(name, varScope, Undefined.instance);
							}
						}
					}
					else
					{
						ScriptableObject.RedefineProperty(scope, name, isConst);
					}
				}
			}
		}

		public static Scriptable CreateFunctionActivation(NativeFunction funObj, Scriptable scope, object[] args)
		{
			return new NativeCall(funObj, scope, args);
		}

		public static void EnterActivationFunction(Context cx, Scriptable scope)
		{
			if (cx.topCallScope == null)
			{
				throw new InvalidOperationException();
			}
			NativeCall call = (NativeCall)scope;
			call.parentActivationCall = cx.currentActivationCall;
			cx.currentActivationCall = call;
		}

		public static void ExitActivationFunction(Context cx)
		{
			NativeCall call = cx.currentActivationCall;
			cx.currentActivationCall = call.parentActivationCall;
			call.parentActivationCall = null;
		}

		internal static NativeCall FindFunctionActivation(Context cx, Function f)
		{
			NativeCall call = cx.currentActivationCall;
			while (call != null)
			{
				if (call.function == f)
				{
					return call;
				}
				call = call.parentActivationCall;
			}
			return null;
		}

		public static Scriptable NewCatchScope(Exception t, Scriptable lastCatchScope, string exceptionName, Context cx, Scriptable scope)
		{
			object obj;
			bool cacheObj;
			if (t is JavaScriptException)
			{
				cacheObj = false;
				obj = ((JavaScriptException)t).GetValue();
			}
			else
			{
				cacheObj = true;
				// Create wrapper object unless it was associated with
				// the previous scope object
				if (lastCatchScope != null)
				{
					NativeObject last = (NativeObject)lastCatchScope;
					obj = last.GetAssociatedValue(t);
					if (obj == null)
					{
						Kit.CodeBug();
					}
				}
				else
				{
					obj = WrapException(t, scope, cx);
				}
			}
			NativeObject catchScopeObject = new NativeObject();
			// See ECMA 12.4
			catchScopeObject.DefineProperty(exceptionName, obj, ScriptableObject.PERMANENT);
			if (IsVisible(cx, t))
			{
				// Add special Rhino object __exception__ defined in the catch
				// scope that can be used to retrieve the Java exception associated
				// with the JavaScript exception (to get stack trace info, etc.)
				catchScopeObject.DefineProperty("__exception__", Context.JavaToJS(t, scope), ScriptableObject.PERMANENT | ScriptableObject.DONTENUM);
			}
			if (cacheObj)
			{
				catchScopeObject.AssociateValue(t, obj);
			}
			return catchScopeObject;
		}

		public static Scriptable WrapException(Exception t, Scriptable scope, Context cx)
		{
			RhinoException re;
			string errorName;
			string errorMsg;
			Exception javaException = null;
			if (t is EcmaError)
			{
				EcmaError ee = (EcmaError)t;
				re = ee;
				errorName = ee.GetName();
				errorMsg = ee.GetErrorMessage();
			}
			else
			{
				if (t is WrappedException)
				{
					WrappedException we = (WrappedException)t;
					re = we;
					javaException = we.GetWrappedException();
					errorName = "JavaException";
					errorMsg = javaException.GetType().FullName + ": " + javaException.Message;
				}
				else
				{
					if (t is EvaluatorException)
					{
						// Pure evaluator exception, nor WrappedException instance
						EvaluatorException ee = (EvaluatorException)t;
						re = ee;
						errorName = "InternalError";
						errorMsg = ee.Message;
					}
					else
					{
						if (cx.HasFeature(Context.FEATURE_ENHANCED_JAVA_ACCESS))
						{
							// With FEATURE_ENHANCED_JAVA_ACCESS, scripts can catch
							// all exception types
							re = new WrappedException(t);
							errorName = "JavaException";
							errorMsg = t.ToString();
						}
						else
						{
							// Script can catch only instances of JavaScriptException,
							// EcmaError and EvaluatorException
							throw Kit.CodeBug();
						}
					}
				}
			}
			string sourceUri = re.SourceName();
			if (sourceUri == null)
			{
				sourceUri = string.Empty;
			}
			int line = re.LineNumber();
			object[] args;
			if (line > 0)
			{
				args = new object[] { errorMsg, sourceUri, Sharpen.Extensions.ValueOf(line) };
			}
			else
			{
				args = new object[] { errorMsg, sourceUri };
			}
			Scriptable errorObject = cx.NewObject(scope, errorName, args);
			ScriptableObject.PutProperty(errorObject, "name", errorName);
			// set exception in Error objects to enable non-ECMA "stack" property
			if (errorObject is NativeError)
			{
				((NativeError)errorObject).SetStackProvider(re);
			}
			if (javaException != null && IsVisible(cx, javaException))
			{
				object wrap = cx.GetWrapFactory().Wrap(cx, scope, javaException, null);
				ScriptableObject.DefineProperty(errorObject, "javaException", wrap, ScriptableObject.PERMANENT | ScriptableObject.READONLY);
			}
			if (IsVisible(cx, re))
			{
				object wrap = cx.GetWrapFactory().Wrap(cx, scope, re, null);
				ScriptableObject.DefineProperty(errorObject, "rhinoException", wrap, ScriptableObject.PERMANENT | ScriptableObject.READONLY);
			}
			return errorObject;
		}

		private static bool IsVisible(Context cx, object obj)
		{
			ClassShutter shutter = cx.GetClassShutter();
			return shutter == null || shutter.VisibleToScripts(obj.GetType().FullName);
		}

		public static Scriptable EnterWith(object obj, Context cx, Scriptable scope)
		{
			Scriptable sobj = ToObjectOrNull(cx, obj);
			if (sobj == null)
			{
				throw TypeError1("msg.undef.with", ToString(obj));
			}
			if (sobj is XMLObject)
			{
				XMLObject xmlObject = (XMLObject)sobj;
				return xmlObject.EnterWith(scope);
			}
			return new NativeWith(scope, sobj);
		}

		public static Scriptable LeaveWith(Scriptable scope)
		{
			NativeWith nw = (NativeWith)scope;
			return nw.GetParentScope();
		}

		public static Scriptable EnterDotQuery(object value, Scriptable scope)
		{
			if (!(value is XMLObject))
			{
				throw NotXmlError(value);
			}
			XMLObject @object = (XMLObject)value;
			return @object.EnterDotQuery(scope);
		}

		public static object UpdateDotQuery(bool value, Scriptable scope)
		{
			// Return null to continue looping
			NativeWith nw = (NativeWith)scope;
			return nw.UpdateDotQuery(value);
		}

		public static Scriptable LeaveDotQuery(Scriptable scope)
		{
			NativeWith nw = (NativeWith)scope;
			return nw.GetParentScope();
		}

		public static void SetFunctionProtoAndParent(BaseFunction fn, Scriptable scope)
		{
			fn.SetParentScope(scope);
			fn.SetPrototype(ScriptableObject.GetFunctionPrototype(scope));
		}

		public static void SetObjectProtoAndParent(ScriptableObject @object, Scriptable scope)
		{
			// Compared with function it always sets the scope to top scope
			scope = ScriptableObject.GetTopLevelScope(scope);
			@object.SetParentScope(scope);
			Scriptable proto = ScriptableObject.GetClassPrototype(scope, @object.GetClassName());
			@object.SetPrototype(proto);
		}

		public static void SetBuiltinProtoAndParent(ScriptableObject @object, Scriptable scope, TopLevel.Builtins type)
		{
			scope = ScriptableObject.GetTopLevelScope(scope);
			@object.SetParentScope(scope);
			@object.SetPrototype(TopLevel.GetBuiltinPrototype(scope, type));
		}

		public static void InitFunction(Context cx, Scriptable scope, NativeFunction function, int type, bool fromEvalCode)
		{
			if (type == FunctionNode.FUNCTION_STATEMENT)
			{
				string name = function.GetFunctionName();
				if (name != null && name.Length != 0)
				{
					if (!fromEvalCode)
					{
						// ECMA specifies that functions defined in global and
						// function scope outside eval should have DONTDELETE set.
						ScriptableObject.DefineProperty(scope, name, function, ScriptableObject.PERMANENT);
					}
					else
					{
						scope.Put(name, scope, function);
					}
				}
			}
			else
			{
				if (type == FunctionNode.FUNCTION_EXPRESSION_STATEMENT)
				{
					string name = function.GetFunctionName();
					if (name != null && name.Length != 0)
					{
						// Always put function expression statements into initial
						// activation object ignoring the with statement to follow
						// SpiderMonkey
						while (scope is NativeWith)
						{
							scope = scope.GetParentScope();
						}
						scope.Put(name, scope, function);
					}
				}
				else
				{
					throw Kit.CodeBug();
				}
			}
		}

		public static Scriptable NewArrayLiteral(object[] objects, int[] skipIndices, Context cx, Scriptable scope)
		{
			int SKIP_DENSITY = 2;
			int count = objects.Length;
			int skipCount = 0;
			if (skipIndices != null)
			{
				skipCount = skipIndices.Length;
			}
			int length = count + skipCount;
			if (length > 1 && skipCount * SKIP_DENSITY < length)
			{
				// If not too sparse, create whole array for constructor
				object[] sparse;
				if (skipCount == 0)
				{
					sparse = objects;
				}
				else
				{
					sparse = new object[length];
					int skip = 0;
					for (int i = 0, j = 0; i != length; ++i)
					{
						if (skip != skipCount && skipIndices[skip] == i)
						{
							sparse[i] = ScriptableConstants.NOT_FOUND;
							++skip;
							continue;
						}
						sparse[i] = objects[j];
						++j;
					}
				}
				return cx.NewArray(scope, sparse);
			}
			Scriptable array = cx.NewArray(scope, length);
			int skip_1 = 0;
			for (int i_1 = 0, j_1 = 0; i_1 != length; ++i_1)
			{
				if (skip_1 != skipCount && skipIndices[skip_1] == i_1)
				{
					++skip_1;
					continue;
				}
				ScriptableObject.PutProperty(array, i_1, objects[j_1]);
				++j_1;
			}
			return array;
		}

		/// <summary>This method is here for backward compat with existing compiled code.</summary>
		/// <remarks>
		/// This method is here for backward compat with existing compiled code.  It
		/// is called when an object literal is compiled.  The next instance will be
		/// the version called from new code.
		/// </remarks>
		[System.ObsoleteAttribute(@"This method only present for compatibility.")]
		public static Scriptable NewObjectLiteral(object[] propertyIds, object[] propertyValues, Context cx, Scriptable scope)
		{
			// Passing null for getterSetters means no getters or setters
			return NewObjectLiteral(propertyIds, propertyValues, null, cx, scope);
		}

		public static Scriptable NewObjectLiteral(object[] propertyIds, object[] propertyValues, int[] getterSetters, Context cx, Scriptable scope)
		{
			Scriptable @object = cx.NewObject(scope);
			for (int i = 0, end = propertyIds.Length; i != end; ++i)
			{
				object id = propertyIds[i];
				int getterSetter = getterSetters == null ? 0 : getterSetters[i];
				object value = propertyValues[i];
				if (id is string)
				{
					if (getterSetter == 0)
					{
						if (IsSpecialProperty((string)id))
						{
							SpecialRef(@object, (string)id, cx).Set(cx, value);
						}
						else
						{
							@object.Put((string)id, @object, value);
						}
					}
					else
					{
						ScriptableObject so = (ScriptableObject)@object;
						Callable getterOrSetter = (Callable)value;
						bool isSetter = getterSetter == 1;
						so.SetGetterOrSetter((string)id, 0, getterOrSetter, isSetter);
					}
				}
				else
				{
					int index = System.Convert.ToInt32(((int)id));
					@object.Put(index, @object, value);
				}
			}
			return @object;
		}

		public static bool IsArrayObject(object obj)
		{
			return obj is NativeArray || obj is Arguments;
		}

		public static object[] GetArrayElements(Scriptable @object)
		{
			Context cx = Context.GetContext();
			long longLen = NativeArray.GetLengthProperty(cx, @object);
			if (longLen > int.MaxValue)
			{
				// arrays beyond  MAX_INT is not in Java in any case
				throw new ArgumentException();
			}
			int len = (int)longLen;
			if (len == 0)
			{
				return ScriptRuntime.emptyArgs;
			}
			else
			{
				object[] result = new object[len];
				for (int i = 0; i < len; i++)
				{
					object elem = ScriptableObject.GetProperty(@object, i);
					result[i] = (elem == ScriptableConstants.NOT_FOUND) ? Undefined.instance : elem;
				}
				return result;
			}
		}

		internal static void CheckDeprecated(Context cx, string name)
		{
			int version = cx.GetLanguageVersion();
			if (version >= Context.VERSION_1_4 || version == Context.VERSION_DEFAULT)
			{
				string msg = GetMessage1("msg.deprec.ctor", name);
				if (version == Context.VERSION_DEFAULT)
				{
					Context.ReportWarning(msg);
				}
				else
				{
					throw Context.ReportRuntimeError(msg);
				}
			}
		}

		public static string GetMessage0(string messageId)
		{
			return GetMessage(messageId, null);
		}

		public static string GetMessage1(string messageId, object arg1)
		{
			object[] arguments = new object[] { arg1 };
			return GetMessage(messageId, arguments);
		}

		public static string GetMessage2(string messageId, object arg1, object arg2)
		{
			object[] arguments = new object[] { arg1, arg2 };
			return GetMessage(messageId, arguments);
		}

		public static string GetMessage3(string messageId, object arg1, object arg2, object arg3)
		{
			object[] arguments = new object[] { arg1, arg2, arg3 };
			return GetMessage(messageId, arguments);
		}

		public static string GetMessage4(string messageId, object arg1, object arg2, object arg3, object arg4)
		{
			object[] arguments = new object[] { arg1, arg2, arg3, arg4 };
			return GetMessage(messageId, arguments);
		}

		/// <summary>This is an interface defining a message provider.</summary>
		/// <remarks>
		/// This is an interface defining a message provider. Create your
		/// own implementation to override the default error message provider.
		/// </remarks>
		/// <author>Mike Harm</author>
		public interface MessageProvider
		{
			/// <summary>
			/// Returns a textual message identified by the given messageId,
			/// parameterized by the given arguments.
			/// </summary>
			/// <remarks>
			/// Returns a textual message identified by the given messageId,
			/// parameterized by the given arguments.
			/// </remarks>
			/// <param name="messageId">the identifier of the message</param>
			/// <param name="arguments">the arguments to fill into the message</param>
			string GetMessage(string messageId, object[] arguments);
		}

		public static ScriptRuntime.MessageProvider messageProvider = new ScriptRuntime.DefaultMessageProvider();

		public static string GetMessage(string messageId, object[] arguments)
		{
			return messageProvider.GetMessage(messageId, arguments);
		}

		private class DefaultMessageProvider : ScriptRuntime.MessageProvider
		{
			public virtual string GetMessage(string messageId, object[] arguments)
			{
				string defaultResource = "org.mozilla.javascript.resources.Messages";
				Context cx = Context.GetCurrentContext();
				CultureInfo locale = cx != null ? cx.GetLocale() : CultureInfo.CurrentCulture;
				// ResourceBundle does caching.
				ResourceBundle rb = ResourceBundle.GetBundle(defaultResource, locale);
				string formatString;
				try
				{
					formatString = rb.GetString(messageId);
				}
				catch (MissingResourceException)
				{
					throw new Exception("no message resource found for message property " + messageId);
				}
				MessageFormat formatter = new MessageFormat(formatString);
				return formatter.Format(arguments);
			}
		}

		public static EcmaError ConstructError(string error, string message)
		{
			int[] linep = new int[1];
			string filename = Context.GetSourcePositionFromStack(linep);
			return ConstructError(error, message, filename, linep[0], null, 0);
		}

		public static EcmaError ConstructError(string error, string message, int lineNumberDelta)
		{
			int[] linep = new int[1];
			string filename = Context.GetSourcePositionFromStack(linep);
			if (linep[0] != 0)
			{
				linep[0] += lineNumberDelta;
			}
			return ConstructError(error, message, filename, linep[0], null, 0);
		}

		public static EcmaError ConstructError(string error, string message, string sourceName, int lineNumber, string lineSource, int columnNumber)
		{
			return new EcmaError(error, message, sourceName, lineNumber, lineSource, columnNumber);
		}

		public static EcmaError TypeError(string message)
		{
			return ConstructError("TypeError", message);
		}

		public static EcmaError TypeError0(string messageId)
		{
			string msg = GetMessage0(messageId);
			return TypeError(msg);
		}

		public static EcmaError TypeError1(string messageId, string arg1)
		{
			string msg = GetMessage1(messageId, arg1);
			return TypeError(msg);
		}

		public static EcmaError TypeError2(string messageId, string arg1, string arg2)
		{
			string msg = GetMessage2(messageId, arg1, arg2);
			return TypeError(msg);
		}

		public static EcmaError TypeError3(string messageId, string arg1, string arg2, string arg3)
		{
			string msg = GetMessage3(messageId, arg1, arg2, arg3);
			return TypeError(msg);
		}

		public static Exception UndefReadError(object @object, object id)
		{
			return TypeError2("msg.undef.prop.read", ToString(@object), ToString(id));
		}

		public static Exception UndefCallError(object @object, object id)
		{
			return TypeError2("msg.undef.method.call", ToString(@object), ToString(id));
		}

		public static Exception UndefWriteError(object @object, object id, object value)
		{
			return TypeError3("msg.undef.prop.write", ToString(@object), ToString(id), ToString(value));
		}

		private static Exception UndefDeleteError(object @object, object id)
		{
			throw TypeError2("msg.undef.prop.delete", ToString(@object), ToString(id));
		}

		public static Exception NotFoundError(Scriptable @object, string property)
		{
			// XXX: use object to improve the error message
			string msg = GetMessage1("msg.is.not.defined", property);
			throw ConstructError("ReferenceError", msg);
		}

		public static Exception NotFunctionError(object value)
		{
			return NotFunctionError(value, value);
		}

		public static Exception NotFunctionError(object value, object messageHelper)
		{
			// Use value for better error reporting
			string msg = (messageHelper == null) ? "null" : messageHelper.ToString();
			if (value == ScriptableConstants.NOT_FOUND)
			{
				return TypeError1("msg.function.not.found", msg);
			}
			return TypeError2("msg.isnt.function", msg, Typeof(value));
		}

		public static Exception NotFunctionError(object obj, object value, string propertyName)
		{
			// Use obj and value for better error reporting
			string objString = ToString(obj);
			if (obj is NativeFunction)
			{
				// Omit function body in string representations of functions
				int paren = objString.IndexOf(')');
				int curly = objString.IndexOf('{', paren);
				if (curly > -1)
				{
					objString = Sharpen.Runtime.Substring(objString, 0, curly + 1) + "...}";
				}
			}
			if (value == ScriptableConstants.NOT_FOUND)
			{
				return TypeError2("msg.function.not.found.in", propertyName, objString);
			}
			return TypeError3("msg.isnt.function.in", propertyName, objString, Typeof(value));
		}

		private static Exception NotXmlError(object value)
		{
			throw TypeError1("msg.isnt.xml.object", ToString(value));
		}

		private static void WarnAboutNonJSObject(object nonJSObject)
		{
			string message = "RHINO USAGE WARNING: Missed Context.javaToJS() conversion:\n" + "Rhino runtime detected object " + nonJSObject + " of class " + nonJSObject.GetType().FullName + " where it expected String, Number, Boolean or Scriptable instance. Please check your code for missing Context.javaToJS() call.";
			Context.ReportWarning(message);
			// Just to be sure that it would be noticed
			System.Console.Error.WriteLine(message);
		}

		public static RegExpProxy GetRegExpProxy(Context cx)
		{
			return cx.GetRegExpProxy();
		}

		public static void SetRegExpProxy(Context cx, RegExpProxy proxy)
		{
			if (proxy == null)
			{
				throw new ArgumentException();
			}
			cx.regExpProxy = proxy;
		}

		public static RegExpProxy CheckRegExpProxy(Context cx)
		{
			RegExpProxy result = GetRegExpProxy(cx);
			if (result == null)
			{
				throw Context.ReportRuntimeError0("msg.no.regexp");
			}
			return result;
		}

		public static Scriptable WrapRegExp(Context cx, Scriptable scope, object compiled)
		{
			return cx.GetRegExpProxy().WrapRegExp(cx, scope, compiled);
		}

		private static XMLLib CurrentXMLLib(Context cx)
		{
			// Scripts should be running to access this
			if (cx.topCallScope == null)
			{
				throw new InvalidOperationException();
			}
			XMLLib xmlLib = cx.cachedXMLLib;
			if (xmlLib == null)
			{
				xmlLib = XMLLib.ExtractFromScope(cx.topCallScope);
				if (xmlLib == null)
				{
					throw new InvalidOperationException();
				}
				cx.cachedXMLLib = xmlLib;
			}
			return xmlLib;
		}

		/// <summary>Escapes the reserved characters in a value of an attribute</summary>
		/// <param name="value">Unescaped text</param>
		/// <returns>The escaped text</returns>
		public static string EscapeAttributeValue(object value, Context cx)
		{
			XMLLib xmlLib = CurrentXMLLib(cx);
			return xmlLib.EscapeAttributeValue(value);
		}

		/// <summary>Escapes the reserved characters in a value of a text node</summary>
		/// <param name="value">Unescaped text</param>
		/// <returns>The escaped text</returns>
		public static string EscapeTextValue(object value, Context cx)
		{
			XMLLib xmlLib = CurrentXMLLib(cx);
			return xmlLib.EscapeTextValue(value);
		}

		public static Ref MemberRef(object obj, object elem, Context cx, int memberTypeFlags)
		{
			if (!(obj is XMLObject))
			{
				throw NotXmlError(obj);
			}
			XMLObject xmlObject = (XMLObject)obj;
			return xmlObject.MemberRef(cx, elem, memberTypeFlags);
		}

		public static Ref MemberRef(object obj, object @namespace, object elem, Context cx, int memberTypeFlags)
		{
			if (!(obj is XMLObject))
			{
				throw NotXmlError(obj);
			}
			XMLObject xmlObject = (XMLObject)obj;
			return xmlObject.MemberRef(cx, @namespace, elem, memberTypeFlags);
		}

		public static Ref NameRef(object name, Context cx, Scriptable scope, int memberTypeFlags)
		{
			XMLLib xmlLib = CurrentXMLLib(cx);
			return xmlLib.NameRef(cx, name, scope, memberTypeFlags);
		}

		public static Ref NameRef(object @namespace, object name, Context cx, Scriptable scope, int memberTypeFlags)
		{
			XMLLib xmlLib = CurrentXMLLib(cx);
			return xmlLib.NameRef(cx, @namespace, name, scope, memberTypeFlags);
		}

		private static void StoreIndexResult(Context cx, int index)
		{
			cx.scratchIndex = index;
		}

		internal static int LastIndexResult(Context cx)
		{
			return cx.scratchIndex;
		}

		public static void StoreUint32Result(Context cx, long value)
		{
			if (((long)(((ulong)value) >> 32)) != 0)
			{
				throw new ArgumentException();
			}
			cx.scratchUint32 = value;
		}

		public static long LastUint32Result(Context cx)
		{
			long value = cx.scratchUint32;
			if (((long)(((ulong)value) >> 32)) != 0)
			{
				throw new InvalidOperationException();
			}
			return value;
		}

		private static void StoreScriptable(Context cx, Scriptable value)
		{
			// The previously stored scratchScriptable should be consumed
			if (cx.scratchScriptable != null)
			{
				throw new InvalidOperationException();
			}
			cx.scratchScriptable = value;
		}

		public static Scriptable LastStoredScriptable(Context cx)
		{
			Scriptable result = cx.scratchScriptable;
			cx.scratchScriptable = null;
			return result;
		}

		internal static string MakeUrlForGeneratedScript(bool isEval, string masterScriptUrl, int masterScriptLine)
		{
			if (isEval)
			{
				return masterScriptUrl + '#' + masterScriptLine + "(eval)";
			}
			else
			{
				return masterScriptUrl + '#' + masterScriptLine + "(Function)";
			}
		}

		internal static bool IsGeneratedScript(string sourceUrl)
		{
			// ALERT: this may clash with a valid URL containing (eval) or
			// (Function)
			return sourceUrl.IndexOf("(eval)") >= 0 || sourceUrl.IndexOf("(Function)") >= 0;
		}

		private static Exception ErrorWithClassName(string msg, object val)
		{
			return Context.ReportRuntimeError1(msg, val.GetType().FullName);
		}

		/// <summary>Equivalent to executing "new Error(message)" from JavaScript.</summary>
		/// <remarks>Equivalent to executing "new Error(message)" from JavaScript.</remarks>
		/// <param name="cx">the current context</param>
		/// <param name="scope">the current scope</param>
		/// <param name="message">the message</param>
		/// <returns>a JavaScriptException you should throw</returns>
		public static JavaScriptException ThrowError(Context cx, Scriptable scope, string message)
		{
			int[] linep = new int[] { 0 };
			string filename = Context.GetSourcePositionFromStack(linep);
			Scriptable error = NewBuiltinObject(cx, scope, TopLevel.Builtins.Error, new object[] { message, filename, Sharpen.Extensions.ValueOf(linep[0]) });
			return new JavaScriptException(error, filename, linep[0]);
		}

		public static readonly object[] emptyArgs = new object[0];

		public static readonly string[] emptyStrings = new string[0];
	}
}
