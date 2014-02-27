/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using Rhino;
using Rhino.Utils;
using Sharpen;

namespace Rhino
{
	/// <author>Mike Shaver</author>
	/// <author>Norris Boyd</author>
	/// <seealso cref="NativeJavaObject">NativeJavaObject</seealso>
	/// <seealso cref="NativeJavaClass">NativeJavaClass</seealso>
	public class JavaMembers
	{
		internal JavaMembers(Scriptable scope, Type cl) : this(scope, cl, false)
		{
		}

		internal JavaMembers(Scriptable scope, Type cl, bool includeProtected)
		{
			try
			{
				Context cx = ContextFactory.GetGlobal().EnterContext();
				ClassShutter shutter = cx.GetClassShutter();
				if (shutter != null && !shutter.VisibleToScripts(cl.FullName))
				{
					throw Context.ReportRuntimeError1("msg.access.prohibited", cl.FullName);
				}
				members = new Dictionary<string, object>();
				staticMembers = new Dictionary<string, object>();
				this.cl = cl;
				bool includePrivate = cx.HasFeature(LanguageFeatures.EnhancedJavaAccess);
				Reflect(scope, includeProtected, includePrivate);
			}
			finally
			{
				Context.Exit();
			}
		}

		internal virtual bool Has(string name, bool isStatic)
		{
			IDictionary<string, object> ht = isStatic ? staticMembers : members;
			object obj = ht.Get(name);
			if (obj != null)
			{
				return true;
			}
			return FindExplicitFunction(name, isStatic) != null;
		}

		internal virtual object Get(Scriptable scope, string name, object javaObject, bool isStatic)
		{
			IDictionary<string, object> ht = isStatic ? staticMembers : members;
			object member = ht.Get(name);
			if (!isStatic && member == null)
			{
				// Try to get static member from instance (LC3)
				member = staticMembers.Get(name);
			}
			if (member == null)
			{
				member = GetExplicitFunction(scope, name, javaObject, isStatic);
				if (member == null)
				{
					return ScriptableConstants.NOT_FOUND;
				}
			}
			if (member is Scriptable)
			{
				return member;
			}
			Context cx = Context.GetContext();
			object rval;
			Type type;
			try
			{
				var property = member as BeanProperty;
				if (property != null)
				{
					if (property.getter == null)
					{
						return ScriptableConstants.NOT_FOUND;
					}
					rval = property.getter.Invoke(javaObject, Context.emptyArgs);
					type = property.getter.Method().ReturnType;
				}
				else
				{
					FieldInfo field = (FieldInfo)member;
					rval = field.GetValue(isStatic ? null : javaObject);
					type = field.FieldType;
				}
			}
			catch (Exception ex)
			{
				throw Context.ThrowAsScriptRuntimeEx(ex);
			}
			// Need to wrap the object before we return it.
			scope = ScriptableObject.GetTopLevelScope(scope);
			return cx.GetWrapFactory().Wrap(cx, scope, rval, type);
		}

		internal virtual void Put(Scriptable scope, string name, object javaObject, object value, bool isStatic)
		{
			IDictionary<string, object> ht = isStatic ? staticMembers : members;
			object member = ht.Get(name);
			if (!isStatic && member == null)
			{
				// Try to get static member from instance (LC3)
				member = staticMembers.Get(name);
			}
			if (member == null)
			{
				throw ReportMemberNotFound(name);
			}
			if (member is FieldAndMethods)
			{
				FieldAndMethods fam = (FieldAndMethods)ht.Get(name);
				member = fam.field;
			}
			// Is this a bean property "set"?
			var bp = member as BeanProperty;
			if (bp != null)
			{
				if (bp.setter == null)
				{
					throw ReportMemberNotFound(name);
				}
				// If there's only one setter or if the value is null, use the
				// main setter. Otherwise, let the NativeJavaMethod decide which
				// setter to use:
				if (bp.setters == null || value == null)
				{
					Type setType = bp.setter.argTypes[0];
					object[] args = new object[] { Context.JsToJava(value, setType) };
					try
					{
						bp.setter.Invoke(javaObject, args);
					}
					catch (Exception ex)
					{
						throw Context.ThrowAsScriptRuntimeEx(ex);
					}
				}
				else
				{
					object[] args = new object[] { value };
					bp.setters.Call(Context.GetContext(), ScriptableObject.GetTopLevelScope(scope), scope, args);
				}
			}
			else
			{
				if (!(member is FieldInfo))
				{
					string str = (member == null) ? "msg.java.internal.private" : "msg.java.method.assign";
					throw Context.ReportRuntimeError1(str, name);
				}
				FieldInfo field = (FieldInfo)member;
				object javaValue = Context.JsToJava(value, field.FieldType);
				try
				{
					field.SetValue(javaObject, javaValue);
				}
				catch (MemberAccessException accessEx)
				{
					if (field.IsInitOnly || field.IsLiteral)
					{
						// treat Java final the same as JavaScript [[READONLY]]
						return;
					}
					throw Context.ThrowAsScriptRuntimeEx(accessEx);
				}
				catch (ArgumentException)
				{
					throw Context.ReportRuntimeError3("msg.java.internal.field.type", value.GetType().FullName, field, javaObject.GetType().FullName);
				}
			}
		}

		internal virtual object[] GetIds(bool isStatic)
		{
			IDictionary<string, object> map = isStatic ? staticMembers : members;
			return map.Keys.Cast<object>().ToArray();
		}

		internal static string JavaSignature(Type type)
		{
			if (!type.IsArray)
			{
				return type.FullName;
			}
			else
			{
				int arrayDimension = 0;
				do
				{
					++arrayDimension;
					type = type.GetElementType();
				}
				while (type.IsArray);
				string name = type.FullName;
				string suffix = "[]";
				if (arrayDimension == 1)
				{
					return String.Concat(name, suffix);
				}
				else
				{
					int length = name.Length + arrayDimension * suffix.Length;
					StringBuilder sb = new StringBuilder(length);
					sb.Append(name);
					while (arrayDimension != 0)
					{
						--arrayDimension;
						sb.Append(suffix);
					}
					return sb.ToString();
				}
			}
		}

		internal static string LiveConnectSignature(Type[] argTypes)
		{
			int N = argTypes.Length;
			if (N == 0)
			{
				return "()";
			}
			StringBuilder sb = new StringBuilder();
			sb.Append('(');
			for (int i = 0; i != N; ++i)
			{
				if (i != 0)
				{
					sb.Append(',');
				}
				sb.Append(JavaSignature(argTypes[i]));
			}
			sb.Append(')');
			return sb.ToString();
		}

		private MemberBox FindExplicitFunction(string name, bool isStatic)
		{
			int sigStart = name.IndexOf('(');
			if (sigStart < 0)
			{
				return null;
			}
			IDictionary<string, object> ht = isStatic ? staticMembers : members;
			MemberBox[] methodsOrCtors = null;
			bool isCtor = (isStatic && sigStart == 0);
			if (isCtor)
			{
				// Explicit request for an overloaded constructor
				methodsOrCtors = ctors.methods;
			}
			else
			{
				// Explicit request for an overloaded method
				string trueName = name.Substring(0, sigStart);
				object obj = ht.Get(trueName);
				if (!isStatic && obj == null)
				{
					// Try to get static member from instance (LC3)
					obj = staticMembers.Get(trueName);
				}
				var njm = obj as NativeJavaMethod;
				if (njm != null)
				{
					methodsOrCtors = njm.methods;
				}
			}
			if (methodsOrCtors != null)
			{
				foreach (MemberBox methodsOrCtor in methodsOrCtors)
				{
					Type[] type = methodsOrCtor.argTypes;
					string sig = LiveConnectSignature(type);
					if (sigStart + sig.Length == name.Length && name.RegionMatches(sigStart, sig, 0, sig.Length))
					{
						return methodsOrCtor;
					}
				}
			}
			return null;
		}

		private object GetExplicitFunction(Scriptable scope, string name, object javaObject, bool isStatic)
		{
			IDictionary<string, object> ht = isStatic ? staticMembers : members;
			object member = null;
			MemberBox methodOrCtor = FindExplicitFunction(name, isStatic);
			if (methodOrCtor != null)
			{
				Scriptable prototype = ScriptableObject.GetFunctionPrototype(scope);
				if (methodOrCtor.IsCtor())
				{
					NativeJavaConstructor fun = new NativeJavaConstructor(methodOrCtor);
					fun.Prototype = prototype;
					member = fun;
					ht[name] = fun;
				}
				else
				{
					string trueName = methodOrCtor.GetName();
					member = ht.Get(trueName);
					if (member is NativeJavaMethod && ((NativeJavaMethod)member).methods.Length > 1)
					{
						NativeJavaMethod fun = new NativeJavaMethod(methodOrCtor, name);
						fun.Prototype = prototype;
						ht[name] = fun;
						member = fun;
					}
				}
			}
			return member;
		}

		/// <summary>Retrieves mapping of methods to accessible methods for a class.</summary>
		/// <remarks>
		/// Retrieves mapping of methods to accessible methods for a class.
		/// In case the class is not public, retrieves methods with same
		/// signature as its public methods from public superclasses and
		/// interfaces (if they exist). Basically upcasts every method to the
		/// nearest accessible method.
		/// </remarks>
		private static MethodInfo[] DiscoverAccessibleMethods(Type clazz, bool includeProtected, bool includePrivate)
		{
			IDictionary<MethodSignature, MethodInfo> map = new Dictionary<MethodSignature, MethodInfo>();
			DiscoverAccessibleMethods(clazz, map, includeProtected, includePrivate);
			return map.Values.ToArray();
		}

		private static void DiscoverAccessibleMethods(Type clazz, IDictionary<MethodSignature, MethodInfo> map, bool includeProtected, bool includePrivate)
		{
			if (clazz.IsPublic || includePrivate)
			{
				try
				{
					if (includeProtected || includePrivate)
					{
						while (clazz != null)
						{
							try
							{
								MethodInfo[] methods = Runtime.GetDeclaredMethods(clazz);
								foreach (MethodInfo method in methods)
								{
									if (method.IsPublic || method.IsFamily || includePrivate)
									{
										MethodSignature sig = new MethodSignature(method);
										if (!map.ContainsKey(sig))
										{
											map[sig] = method;
										}
									}
								}
								clazz = clazz.BaseType;
							}
							catch (SecurityException)
							{
								// Some security settings (i.e., applets) disallow
								// access to Class.getDeclaredMethods. Fall back to
								// Class.getMethods.
								MethodInfo[] methods = clazz.GetMethods();
								foreach (MethodInfo method in methods)
								{
									MethodSignature sig = new MethodSignature(method);
									if (!map.ContainsKey(sig))
									{
										map[sig] = method;
									}
								}
								break;
							}
						}
					}
					else
					{
						// getMethods gets superclass methods, no
						// need to loop any more
						MethodInfo[] methods = clazz.GetMethods();
						foreach (MethodInfo method in methods)
						{
							MethodSignature sig = new MethodSignature(method);
							// Array may contain methods with same signature but different return value!
							if (!map.ContainsKey(sig))
							{
								map[sig] = method;
							}
						}
					}
					return;
				}
				catch (SecurityException)
				{
					Context.ReportWarning("Could not discover accessible methods of class " + clazz.FullName + " due to lack of privileges, " + "attemping superclasses/interfaces.");
				}
			}
			// Fall through and attempt to discover superclass/interface
			// methods
			Type[] interfaces = clazz.GetInterfaces();
			foreach (Type intface in interfaces)
			{
				DiscoverAccessibleMethods(intface, map, includeProtected, includePrivate);
			}
			Type superclass = clazz.BaseType;
			if (superclass != null)
			{
				DiscoverAccessibleMethods(superclass, map, includeProtected, includePrivate);
			}
		}

		private sealed class MethodSignature
		{
			private readonly string name;

			private readonly Type[] args;

			private MethodSignature(string name, Type[] args)
			{
				this.name = name;
				this.args = args;
			}

			internal MethodSignature(MethodInfo method) : this(method.Name, method.GetParameterTypes())
			{
			}

			public override bool Equals(object o)
			{
				var ms = o as MethodSignature;
				if (ms != null)
				{
					return ms.name.Equals(name) && Arrays.Equals(args, ms.args);
				}
				return false;
			}

			public override int GetHashCode()
			{
				return name.GetHashCode() ^ args.Length;
			}
		}

		private void Reflect(Scriptable scope, bool includeProtected, bool includePrivate)
		{
			// We reflect methods first, because we want overloaded field/method
			// names to be allocated to the NativeJavaMethod before the field
			// gets in the way.
			MethodInfo[] methods = DiscoverAccessibleMethods(cl, includeProtected, includePrivate);
			foreach (MethodInfo method in methods)
			{
				bool isStatic = method.IsStatic;
				IDictionary<string, object> ht = isStatic ? staticMembers : members;
				string name = method.Name;
				object value = ht.Get(name);
				if (value == null)
				{
					ht[name] = method;
				}
				else
				{
					ObjArray overloadedMethods;
					var objArray = value as ObjArray;
					if (objArray != null)
					{
						overloadedMethods = objArray;
					}
					else
					{
						if (!(value is MethodInfo))
						{
							Kit.CodeBug();
						}
						// value should be instance of Method as at this stage
						// staticMembers and members can only contain methods
						overloadedMethods = new ObjArray();
						overloadedMethods.Add(value);
						ht[name] = overloadedMethods;
					}
					overloadedMethods.Add(method);
				}
			}
			// replace Method instances by wrapped NativeJavaMethod objects
			// first in staticMembers and then in members
			for (int tableCursor = 0; tableCursor != 2; ++tableCursor)
			{
				bool isStatic = (tableCursor == 0);
				IDictionary<string, object> ht = isStatic ? staticMembers : members;
				foreach (var entry in ht.ToArray())
				{
					MemberBox[] methodBoxes;
					object value = entry.Value;
					var methodInfo = value as MethodInfo;
					if (methodInfo != null)
					{
						methodBoxes = new MemberBox[1];
						methodBoxes[0] = new MemberBox(methodInfo);
					}
					else
					{
						ObjArray overloadedMethods = (ObjArray)value;
						int N = overloadedMethods.Size();
						if (N < 2)
						{
							Kit.CodeBug();
						}
						methodBoxes = new MemberBox[N];
						for (int i = 0; i < N; i++)
						{
							MethodInfo method_1 = (MethodInfo) overloadedMethods.Get(i);
							methodBoxes[i] = new MemberBox(method_1);
						}
					}
					NativeJavaMethod fun = new NativeJavaMethod(methodBoxes);
					if (scope != null)
					{
						ScriptRuntime.SetFunctionProtoAndParent(fun, scope);
					}
					ht[entry.Key] = fun;
				}
			}
			// Reflect fields.
			FieldInfo[] fields = GetAccessibleFields(includeProtected, includePrivate);
			foreach (FieldInfo field in fields)
			{
				string name = field.Name;
				try
				{
					bool isStatic = (field).IsStatic;
					IDictionary<string, object> ht = isStatic ? staticMembers : members;
					object member = ht.Get(name);
					if (member == null)
					{
						ht[name] = field;
					}
					else
					{
						var method = member as NativeJavaMethod;
						if (method != null)
						{
							FieldAndMethods fam = new FieldAndMethods(scope, method.methods, field);
							IDictionary<string, FieldAndMethods> fmht = isStatic ? staticFieldAndMethods : fieldAndMethods;
							if (fmht == null)
							{
								fmht = new Dictionary<string, FieldAndMethods>();
								if (isStatic)
								{
									staticFieldAndMethods = fmht;
								}
								else
								{
									fieldAndMethods = fmht;
								}
							}
							fmht[name] = fam;
							ht[name] = fam;
						}
						else
						{
							var oldField = member as FieldInfo;
							if (oldField != null)
							{
								// If this newly reflected field shadows an inherited field,
								// then replace it. Otherwise, since access to the field
								// would be ambiguous from Java, no field should be
								// reflected.
								// For now, the first field found wins, unless another field
								// explicitly shadows it.
								if (oldField.DeclaringType.IsAssignableFrom(field.DeclaringType))
								{
									ht[name] = field;
								}
							}
							else
							{
								// "unknown member type"
								Kit.CodeBug();
							}
						}
					}
				}
				catch (SecurityException)
				{
					// skip this field
					Context.ReportWarning("Could not access field " + name + " of class " + cl.FullName + " due to lack of privileges.");
				}
			}
			// Create bean properties from corresponding get/set methods first for
			// static members and then for instance members
			for (int tableCursor_1 = 0; tableCursor_1 != 2; ++tableCursor_1)
			{
				bool isStatic = (tableCursor_1 == 0);
				IDictionary<string, object> ht = isStatic ? staticMembers : members;
				IDictionary<string, BeanProperty> toAdd = new Dictionary<string, BeanProperty>();
				// Now, For each member, make "bean" properties.
				foreach (string name in ht.Keys)
				{
					// Is this a getter?
					bool memberIsGetMethod = name.StartsWith("get");
					bool memberIsSetMethod = name.StartsWith("set");
					bool memberIsIsMethod = name.StartsWith("is");
					if (memberIsGetMethod || memberIsIsMethod || memberIsSetMethod)
					{
						// Double check name component.
						string nameComponent = name.Substring(memberIsIsMethod ? 2 : 3);
						if (nameComponent.Length == 0)
						{
							continue;
						}
						// Make the bean property name.
						string beanPropertyName = nameComponent;
						char ch0 = nameComponent[0];
						if (Char.IsUpper(ch0))
						{
							if (nameComponent.Length == 1)
							{
								beanPropertyName = nameComponent.ToLower();
							}
							else
							{
								char ch1 = nameComponent[1];
								if (!Char.IsUpper(ch1))
								{
									beanPropertyName = Char.ToLower(ch0) + nameComponent.Substring(1);
								}
							}
						}
						// If we already have a member by this name, don't do this
						// property.
						if (toAdd.ContainsKey(beanPropertyName))
						{
							continue;
						}
						object v = ht.Get(beanPropertyName);
						if (v != null)
						{
							// A private field shouldn't mask a public getter/setter
							if (!includePrivate || !(v is MemberInfo) || !((MemberInfo) v).IsPrivate())
							{
								continue;
							}
						}
						// Find the getter method, or if there is none, the is-
						// method.
						MemberBox getter = null;
						getter = FindGetter(isStatic, ht, "get", nameComponent);
						// If there was no valid getter, check for an is- method.
						if (getter == null)
						{
							getter = FindGetter(isStatic, ht, "is", nameComponent);
						}
						// setter
						MemberBox setter = null;
						NativeJavaMethod setters = null;
						string setterName = String.Concat("set", nameComponent);
						if (ht.ContainsKey(setterName))
						{
							// Is this value a method?
							object member = ht.Get(setterName);
							var njmSet = member as NativeJavaMethod;
							if (njmSet != null)
							{
								if (getter != null)
								{
									// We have a getter. Now, do we have a matching
									// setter?
									Type type = getter.Method().ReturnType;
									setter = ExtractSetMethod(type, njmSet.methods, isStatic);
								}
								else
								{
									// No getter, find any set method
									setter = ExtractSetMethod(njmSet.methods, isStatic);
								}
								if (njmSet.methods.Length > 1)
								{
									setters = njmSet;
								}
							}
						}
						// Make the property.
						BeanProperty bp = new BeanProperty(getter, setter, setters);
						toAdd[beanPropertyName] = bp;
					}
				}
				// Add the new bean properties.
				foreach (string key in toAdd.Keys)
				{
					object value = toAdd.Get(key);
					ht[key] = value;
				}
			}
			// Reflect constructors
			ConstructorInfo[] constructors = GetAccessibleConstructors(includePrivate);
			MemberBox[] ctorMembers = new MemberBox[constructors.Length];
			for (int i_1 = 0; i_1 != constructors.Length; ++i_1)
			{
				ctorMembers[i_1] = new MemberBox(constructors[i_1]);
			}
			ctors = new NativeJavaMethod(ctorMembers, cl.Name);
		}

		private ConstructorInfo[] GetAccessibleConstructors(bool includePrivate)
		{
			// The JVM currently doesn't allow changing access on java.lang.Class
			// constructors, so don't try
			if (includePrivate && cl != ScriptRuntime.ClassClass)
			{
				try
				{
					ConstructorInfo[] cons = cl.GetDeclaredConstructors();
					return cons;
				}
				catch (SecurityException)
				{
					// Fall through to !includePrivate case
					Context.ReportWarning("Could not access constructor " + " of class " + cl.FullName + " due to lack of privileges.");
				}
			}
			return cl.GetConstructors();
		}

		private FieldInfo[] GetAccessibleFields(bool includeProtected, bool includePrivate)
		{
			if (includePrivate || includeProtected)
			{
				try
				{
					IList<FieldInfo> fieldsList = new List<FieldInfo>();
					Type currentClass = cl;
					while (currentClass != null)
					{
						// get all declared fields in this class, make them
						// accessible, and save
						FieldInfo[] declared = Runtime.GetDeclaredFields(currentClass);
						foreach (FieldInfo field in declared)
						{
							if (includePrivate || field.IsPublic || field.IsFamily)
							{
								fieldsList.Add(field);
							}
						}
						// walk up superclass chain.  no need to deal specially with
						// interfaces, since they can't have fields
						currentClass = currentClass.BaseType;
					}
					return fieldsList.ToArray();
				}
				catch (SecurityException)
				{
				}
			}
			// fall through to !includePrivate case
			return cl.GetFields();
		}

		private MemberBox FindGetter(bool isStatic, IDictionary<string, object> ht, string prefix, string propertyName)
		{
			string getterName = String.Concat(prefix, propertyName);
			if (ht.ContainsKey(getterName))
			{
				// Check that the getter is a method.
				var njmGet = ht.Get(getterName) as NativeJavaMethod;
				if (njmGet != null)
				{
					return ExtractGetMethod(njmGet.methods, isStatic);
				}
			}
			return null;
		}

		private static MemberBox ExtractGetMethod(MemberBox[] methods, bool isStatic)
		{
			// Inspect the list of all MemberBox for the only one having no
			// parameters
			foreach (MemberBox method in methods)
			{
				// Does getter method have an empty parameter list with a return
				// value (eg. a getSomething() or isSomething())?
				if (method.argTypes.Length == 0 && (!isStatic || method.IsStatic()))
				{
					Type type = method.Method().ReturnType;
					if (type != typeof(void))
					{
						return method;
					}
					break;
				}
			}
			return null;
		}

		private static MemberBox ExtractSetMethod(Type type, MemberBox[] methods, bool isStatic)
		{
			//
			// Note: it may be preferable to allow NativeJavaMethod.findFunction()
			//       to find the appropriate setter; unfortunately, it requires an
			//       instance of the target arg to determine that.
			//
			// Make two passes: one to find a method with direct type assignment,
			// and one to find a widening conversion.
			for (int pass = 1; pass <= 2; ++pass)
			{
				foreach (MemberBox method in methods)
				{
					if (!isStatic || method.IsStatic())
					{
						Type[] @params = method.argTypes;
						if (@params.Length == 1)
						{
							if (pass == 1)
							{
								if (@params[0] == type)
								{
									return method;
								}
							}
							else
							{
								if (pass != 2)
								{
									Kit.CodeBug();
								}
								if (@params[0].IsAssignableFrom(type))
								{
									return method;
								}
							}
						}
					}
				}
			}
			return null;
		}

		private static MemberBox ExtractSetMethod(MemberBox[] methods, bool isStatic)
		{
			foreach (MemberBox method in methods)
			{
				if (!isStatic || method.IsStatic())
				{
					if (method.Method().ReturnType == typeof(void))
					{
						if (method.argTypes.Length == 1)
						{
							return method;
						}
					}
				}
			}
			return null;
		}

		internal virtual IDictionary<string, FieldAndMethods> GetFieldAndMethodsObjects(Scriptable scope, object javaObject, bool isStatic)
		{
			IDictionary<string, FieldAndMethods> ht = isStatic ? staticFieldAndMethods : fieldAndMethods;
			if (ht == null)
			{
				return null;
			}
			int len = ht.Count;
			IDictionary<string, FieldAndMethods> result = new Dictionary<string, FieldAndMethods>(len);
			foreach (FieldAndMethods fam in ht.Values)
			{
				FieldAndMethods famNew = new FieldAndMethods(scope, fam.methods, fam.field);
				famNew.javaObject = javaObject;
				result[fam.field.Name] = famNew;
			}
			return result;
		}

		internal static JavaMembers LookupClass(Scriptable scope, Type dynamicType, Type staticType, bool includeProtected)
		{
			JavaMembers members;
			ClassCache cache = ClassCache.Get(scope);
			IDictionary<Type, JavaMembers> ct = cache.GetClassCacheMap();
			Type cl = dynamicType;
			for (; ; )
			{
				members = ct.Get(cl);
				if (members != null)
				{
					if (cl != dynamicType)
					{
						// member lookup for the original class failed because of
						// missing privileges, cache the result so we don't try again
						ct[dynamicType] = members;
					}
					return members;
				}
				try
				{
					members = new JavaMembers(cache.GetAssociatedScope(), cl, includeProtected);
					break;
				}
				catch (SecurityException e)
				{
					// Reflection may fail for objects that are in a restricted
					// access package (e.g. sun.*).  If we get a security
					// exception, try again with the static type if it is interface.
					// Otherwise, try superclass
					if (staticType != null && staticType.IsInterface)
					{
						cl = staticType;
						staticType = null;
					}
					else
					{
						// try staticType only once
						Type parent = cl.BaseType;
						if (parent == null)
						{
							if (cl.IsInterface)
							{
								// last resort after failed staticType interface
								parent = ScriptRuntime.ObjectClass;
							}
							else
							{
								throw;
							}
						}
						cl = parent;
					}
				}
			}
			if (cache.IsCachingEnabled())
			{
				ct[cl] = members;
				if (cl != dynamicType)
				{
					// member lookup for the original class failed because of
					// missing privileges, cache the result so we don't try again
					ct[dynamicType] = members;
				}
			}
			return members;
		}

		internal virtual Exception ReportMemberNotFound(string memberName)
		{
			return Context.ReportRuntimeError2("msg.java.member.not.found", cl.FullName, memberName);
		}

		private Type cl;

		private IDictionary<string, object> members;

		private IDictionary<string, FieldAndMethods> fieldAndMethods;

		private IDictionary<string, object> staticMembers;

		private IDictionary<string, FieldAndMethods> staticFieldAndMethods;

		internal NativeJavaMethod ctors;
		// we use NativeJavaMethod for ctor overload resolution
	}

	internal class BeanProperty
	{
		internal BeanProperty(MemberBox getter, MemberBox setter, NativeJavaMethod setters)
		{
			this.getter = getter;
			this.setter = setter;
			this.setters = setters;
		}

		internal MemberBox getter;

		internal MemberBox setter;

		internal NativeJavaMethod setters;
	}

	[Serializable]
	internal class FieldAndMethods : NativeJavaMethod
	{
		internal FieldAndMethods(Scriptable scope, MemberBox[] methods, FieldInfo field) : base(methods)
		{
			this.field = field;
			ParentScope = scope;
			Prototype = GetFunctionPrototype(scope);
		}

		public override object GetDefaultValue(Type hint)
		{
			if (hint == ScriptRuntime.FunctionClass)
			{
				return this;
			}
			object rval;
			Type type;
			try
			{
				rval = field.GetValue(javaObject);
				type = field.FieldType;
			}
			catch (MemberAccessException)
			{
				throw Context.ReportRuntimeError1("msg.java.internal.private", field.Name);
			}
			Context cx = Context.GetContext();
			rval = cx.GetWrapFactory().Wrap(cx, this, rval, type);
			if (rval is Scriptable)
			{
				rval = ((Scriptable)rval).GetDefaultValue(hint);
			}
			return rval;
		}

		internal FieldInfo field;

		internal object javaObject;
	}
}
