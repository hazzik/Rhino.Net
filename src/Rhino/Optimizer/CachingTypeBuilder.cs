using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Rhino.Optimizer
{
	public class CachingTypeBuilder : TypeDelegator
	{
		private readonly IList<ConstructorBuilder> constructors = new List<ConstructorBuilder>();
		private readonly IDictionary<string, FieldBuilder> fields = new Dictionary<string, FieldBuilder>();
		private readonly IList<MethodBuilder> methods = new List<MethodBuilder>();
		private readonly TypeBuilder tb;

		public CachingTypeBuilder(TypeBuilder tb)
			: base(tb)
		{
			this.tb = tb;
		}

		protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
		{
			foreach (var method in methods)
			{
				if (method.Name == name/* && method.GetParameters().Length == types.Length*/)
				{
					return method;
				}
			}
			return base.GetMethodImpl(name, bindingAttr, binder, callConvention, types, modifiers);
		}

		public ConstructorBuilder DefineConstructor(MethodAttributes attributes, CallingConventions callingConvention, Type[] parameterTypes)
		{
			ConstructorBuilder constructor = tb.DefineConstructor(attributes, callingConvention, parameterTypes);
			constructors.Add(constructor);
			return constructor;
		}

		public void AddInterfaceImplementation(Type interfaceType)
		{
			tb.AddInterfaceImplementation(interfaceType);
		}

		public ConstructorBuilder DefineConstructor(MethodAttributes attributes, CallingConventions callingConvention, Type[] parameterTypes, Type[][] requiredCustomModifiers, Type[][] optionalCustomModifiers)
		{
			ConstructorBuilder constructor = tb.DefineConstructor(attributes, callingConvention, parameterTypes, requiredCustomModifiers, optionalCustomModifiers);
			constructors.Add(constructor);
			return constructor;
		}

		public FieldBuilder DefineField(string fieldName, Type type, FieldAttributes attributes)
		{
			FieldBuilder field = tb.DefineField(fieldName, type, attributes);
			fields.Add(fieldName, field);
			return field;
		}

		public FieldBuilder DefineField(string fieldName, Type type, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers, FieldAttributes attributes)
		{
			FieldBuilder field = tb.DefineField(fieldName, type, requiredCustomModifiers, optionalCustomModifiers, attributes);
			fields.Add(fieldName, field);
			return field;
		}

		public override FieldInfo GetField(string name, BindingFlags bindingAttr)
		{
			FieldBuilder field;
			if (fields.TryGetValue(name, out field))
				return field;

			return base.GetField(name, bindingAttr);
		}

		public Type CreateType()
		{
			return tb.CreateType();
		}

		public MethodBuilder DefineMethod(string name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers)
		{
			MethodBuilder method = tb.DefineMethod(name, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);
			methods.Add(method);
			return method;
		}

		public MethodBuilder DefineMethod(string name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
		{
			MethodBuilder method = tb.DefineMethod(name, attributes, callingConvention, returnType, parameterTypes);
			methods.Add(method);
			return method;
		}

		public MethodBuilder DefineMethod(string name, MethodAttributes attributes, CallingConventions callingConvention)
		{
			MethodBuilder method = tb.DefineMethod(name, attributes, callingConvention);
			methods.Add(method);
			return method;
		}

		public MethodBuilder DefineMethod(string name, MethodAttributes attributes)
		{
			MethodBuilder method = tb.DefineMethod(name, attributes);
			methods.Add(method);
			return method;
		}

		public MethodBuilder DefineMethod(string name, MethodAttributes attributes, Type returnType, Type[] parameterTypes)
		{
			MethodBuilder method = tb.DefineMethod(name, attributes, returnType, parameterTypes);
			methods.Add(method);
			return method;
		}

		public void DefineMethodOverride(MethodInfo methodInfoBody, MethodInfo methodInfoDeclaration)
		{
			tb.DefineMethodOverride(methodInfoBody, methodInfoDeclaration);
		}
	}
}