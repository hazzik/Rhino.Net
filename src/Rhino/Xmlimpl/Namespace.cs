/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Text;
using Rhino;
using Rhino.Xmlimpl;
using Sharpen;

namespace Rhino.Xmlimpl
{
	/// <summary>Class Namespace</summary>
	[System.Serializable]
	internal class Namespace : IdScriptableObject
	{
		internal const long serialVersionUID = -5765755238131301744L;

		private static readonly object NAMESPACE_TAG = "Namespace";

		private Rhino.Xmlimpl.Namespace prototype;

		private XmlNode.Namespace ns;

		private Namespace()
		{
		}

		internal static Rhino.Xmlimpl.Namespace Create(Scriptable scope, Rhino.Xmlimpl.Namespace prototype, XmlNode.Namespace @namespace)
		{
			Rhino.Xmlimpl.Namespace rv = new Rhino.Xmlimpl.Namespace();
			rv.SetParentScope(scope);
			rv.prototype = prototype;
			rv.SetPrototype(prototype);
			rv.ns = @namespace;
			return rv;
		}

		internal XmlNode.Namespace GetDelegate()
		{
			return ns;
		}

		public virtual void ExportAsJSClass(bool @sealed)
		{
			ExportAsJSClass(MAX_PROTOTYPE_ID, this.GetParentScope(), @sealed);
		}

		public virtual string Uri()
		{
			return ns.GetUri();
		}

		public virtual string Prefix()
		{
			return ns.GetPrefix();
		}

		public override string ToString()
		{
			return Uri();
		}

		public virtual string ToLocaleString()
		{
			return ToString();
		}

		private bool Equals(Rhino.Xmlimpl.Namespace n)
		{
			return Uri().Equals(n.Uri());
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Rhino.Xmlimpl.Namespace))
			{
				return false;
			}
			return Equals((Rhino.Xmlimpl.Namespace)obj);
		}

		public override int GetHashCode()
		{
			return Uri().GetHashCode();
		}

		protected internal override object EquivalentValues(object value)
		{
			if (!(value is Rhino.Xmlimpl.Namespace))
			{
				return ScriptableConstants.NOT_FOUND;
			}
			bool result = Equals((Rhino.Xmlimpl.Namespace)value);
			return result ? true : false;
		}

		public override string GetClassName()
		{
			return "Namespace";
		}

		public override object GetDefaultValue(Type hint)
		{
			return Uri();
		}

		private const int Id_prefix = 1;

		private const int Id_uri = 2;

		private const int MAX_INSTANCE_ID = 2;

		// #string_id_map#
		protected internal override int GetMaxInstanceId()
		{
			return base.GetMaxInstanceId() + MAX_INSTANCE_ID;
		}

		protected internal override int FindInstanceIdInfo(string s)
		{
			int id;
			// #generated# Last update: 2007-08-20 08:23:22 EDT
			id = 0;
			string X = null;
			int s_length = s.Length;
			if (s_length == 3)
			{
				X = "uri";
				id = Id_uri;
			}
			else
			{
				if (s_length == 6)
				{
					X = "prefix";
					id = Id_prefix;
				}
			}
			if (X != null && X != s && !X.Equals(s))
			{
				id = 0;
			}
			goto L0_break;
L0_break: ;
			// #/generated#
			if (id == 0)
			{
				return base.FindInstanceIdInfo(s);
			}
			int attr;
			switch (id)
			{
				case Id_prefix:
				case Id_uri:
				{
					attr = PERMANENT | READONLY;
					break;
				}

				default:
				{
					throw new InvalidOperationException();
				}
			}
			return InstanceIdInfo(attr, base.GetMaxInstanceId() + id);
		}

		// #/string_id_map#
		protected internal override string GetInstanceIdName(int id)
		{
			switch (id - base.GetMaxInstanceId())
			{
				case Id_prefix:
				{
					return "prefix";
				}

				case Id_uri:
				{
					return "uri";
				}
			}
			return base.GetInstanceIdName(id);
		}

		protected internal override object GetInstanceIdValue(int id)
		{
			switch (id - base.GetMaxInstanceId())
			{
				case Id_prefix:
				{
					if (ns.GetPrefix() == null)
					{
						return Undefined.instance;
					}
					return ns.GetPrefix();
				}

				case Id_uri:
				{
					return ns.GetUri();
				}
			}
			return base.GetInstanceIdValue(id);
		}

		private const int Id_constructor = 1;

		private const int Id_toString = 2;

		private const int Id_toSource = 3;

		private const int MAX_PROTOTYPE_ID = 3;

		// #string_id_map#
		protected internal override int FindPrototypeId(string s)
		{
			int id;
			// #generated# Last update: 2007-08-20 08:23:22 EDT
			id = 0;
			string X = null;
			int c;
			int s_length = s.Length;
			if (s_length == 8)
			{
				c = s[3];
				if (c == 'o')
				{
					X = "toSource";
					id = Id_toSource;
				}
				else
				{
					if (c == 't')
					{
						X = "toString";
						id = Id_toString;
					}
				}
			}
			else
			{
				if (s_length == 11)
				{
					X = "constructor";
					id = Id_constructor;
				}
			}
			if (X != null && X != s && !X.Equals(s))
			{
				id = 0;
			}
			goto L0_break;
L0_break: ;
			// #/generated#
			return id;
		}

		// #/string_id_map#
		protected internal override void InitPrototypeId(int id)
		{
			string s;
			int arity;
			switch (id)
			{
				case Id_constructor:
				{
					arity = 2;
					s = "constructor";
					break;
				}

				case Id_toString:
				{
					arity = 0;
					s = "toString";
					break;
				}

				case Id_toSource:
				{
					arity = 0;
					s = "toSource";
					break;
				}

				default:
				{
					throw new ArgumentException(id.ToString());
				}
			}
			InitPrototypeMethod(NAMESPACE_TAG, id, s, arity);
		}

		public override object ExecIdCall(IdFunctionObject f, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (!f.HasTag(NAMESPACE_TAG))
			{
				return base.ExecIdCall(f, cx, scope, thisObj, args);
			}
			int id = f.MethodId();
			switch (id)
			{
				case Id_constructor:
				{
					return JsConstructor(cx, (thisObj == null), args);
				}

				case Id_toString:
				{
					return RealThis(thisObj, f).ToString();
				}

				case Id_toSource:
				{
					return RealThis(thisObj, f).Js_toSource();
				}
			}
			throw new ArgumentException(id.ToString());
		}

		private Rhino.Xmlimpl.Namespace RealThis(Scriptable thisObj, IdFunctionObject f)
		{
			if (!(thisObj is Rhino.Xmlimpl.Namespace))
			{
				throw IncompatibleCallError(f);
			}
			return (Rhino.Xmlimpl.Namespace)thisObj;
		}

		internal virtual Rhino.Xmlimpl.Namespace NewNamespace(string uri)
		{
			Rhino.Xmlimpl.Namespace prototype = (this.prototype == null) ? this : this.prototype;
			return Create(this.GetParentScope(), prototype, XmlNode.Namespace.Create(uri));
		}

		internal virtual Rhino.Xmlimpl.Namespace NewNamespace(string prefix, string uri)
		{
			if (prefix == null)
			{
				return NewNamespace(uri);
			}
			Rhino.Xmlimpl.Namespace prototype = (this.prototype == null) ? this : this.prototype;
			return Create(this.GetParentScope(), prototype, XmlNode.Namespace.Create(prefix, uri));
		}

		internal virtual Rhino.Xmlimpl.Namespace ConstructNamespace(object uriValue)
		{
			string prefix;
			string uri;
			if (uriValue is Rhino.Xmlimpl.Namespace)
			{
				Rhino.Xmlimpl.Namespace ns = (Rhino.Xmlimpl.Namespace)uriValue;
				prefix = ns.Prefix();
				uri = ns.Uri();
			}
			else
			{
				if (uriValue is QName)
				{
					QName qname = (QName)uriValue;
					uri = qname.Uri();
					if (uri != null)
					{
						//    TODO    Is there a way to push this back into QName so that we can make prefix() private?
						prefix = qname.Prefix();
					}
					else
					{
						uri = qname.ToString();
						prefix = null;
					}
				}
				else
				{
					uri = ScriptRuntime.ToString(uriValue);
					prefix = (uri.Length == 0) ? string.Empty : null;
				}
			}
			return NewNamespace(prefix, uri);
		}

		internal virtual Rhino.Xmlimpl.Namespace CastToNamespace(object namespaceObj)
		{
			if (namespaceObj is Rhino.Xmlimpl.Namespace)
			{
				return (Rhino.Xmlimpl.Namespace)namespaceObj;
			}
			return ConstructNamespace(namespaceObj);
		}

		private Rhino.Xmlimpl.Namespace ConstructNamespace(object prefixValue, object uriValue)
		{
			string prefix;
			string uri;
			if (uriValue is QName)
			{
				QName qname = (QName)uriValue;
				uri = qname.Uri();
				if (uri == null)
				{
					uri = qname.ToString();
				}
			}
			else
			{
				uri = ScriptRuntime.ToString(uriValue);
			}
			if (uri.Length == 0)
			{
				if (prefixValue == Undefined.instance)
				{
					prefix = string.Empty;
				}
				else
				{
					prefix = ScriptRuntime.ToString(prefixValue);
					if (prefix.Length != 0)
					{
						throw ScriptRuntime.TypeError("Illegal prefix '" + prefix + "' for 'no namespace'.");
					}
				}
			}
			else
			{
				if (prefixValue == Undefined.instance)
				{
					prefix = string.Empty;
				}
				else
				{
					if (!XMLName.Accept(prefixValue))
					{
						prefix = string.Empty;
					}
					else
					{
						prefix = ScriptRuntime.ToString(prefixValue);
					}
				}
			}
			return NewNamespace(prefix, uri);
		}

		private Rhino.Xmlimpl.Namespace ConstructNamespace()
		{
			return NewNamespace(string.Empty, string.Empty);
		}

		private object JsConstructor(Context cx, bool inNewExpr, object[] args)
		{
			if (!inNewExpr && args.Length == 1)
			{
				return CastToNamespace(args[0]);
			}
			if (args.Length == 0)
			{
				return ConstructNamespace();
			}
			else
			{
				if (args.Length == 1)
				{
					return ConstructNamespace(args[0]);
				}
				else
				{
					return ConstructNamespace(args[0], args[1]);
				}
			}
		}

		private string Js_toSource()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append('(');
			ToSourceImpl(ns.GetPrefix(), ns.GetUri(), sb);
			sb.Append(')');
			return sb.ToString();
		}

		internal static void ToSourceImpl(string prefix, string uri, StringBuilder sb)
		{
			sb.Append("new Namespace(");
			if (uri.Length == 0)
			{
				if (!string.Empty.Equals(prefix))
				{
					throw new ArgumentException(prefix);
				}
			}
			else
			{
				sb.Append('\'');
				if (prefix != null)
				{
					sb.Append(ScriptRuntime.EscapeString(prefix, '\''));
					sb.Append("', '");
				}
				sb.Append(ScriptRuntime.EscapeString(uri, '\''));
				sb.Append('\'');
			}
			sb.Append(')');
		}
	}
}
