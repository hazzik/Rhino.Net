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
	/// <summary>Class QName</summary>
	[System.Serializable]
	internal sealed class QName : IdScriptableObject
	{
		internal const long serialVersionUID = 416745167693026750L;

		private static readonly object QNAME_TAG = "QName";

		private XMLLibImpl lib;

		private Rhino.Xmlimpl.QName prototype;

		private Rhino.Xmlimpl.XmlNode.QName delegate_;

		private QName()
		{
		}

		internal static Rhino.Xmlimpl.QName Create(XMLLibImpl lib, Scriptable scope, Rhino.Xmlimpl.QName prototype, Rhino.Xmlimpl.XmlNode.QName delegate_)
		{
			Rhino.Xmlimpl.QName rv = new Rhino.Xmlimpl.QName();
			rv.lib = lib;
			rv.SetParentScope(scope);
			rv.prototype = prototype;
			rv.SetPrototype(prototype);
			rv.delegate_ = delegate_;
			return rv;
		}

		//    /** @deprecated */
		//    static QName create(XMLLibImpl lib, XmlNode.QName nodeQname) {
		//        return create(lib, lib.globalScope(), lib.qnamePrototype(), nodeQname);
		//    }
		internal void ExportAsJSClass(bool @sealed)
		{
			ExportAsJSClass(MAX_PROTOTYPE_ID, GetParentScope(), @sealed);
		}

		public override string ToString()
		{
			//    ECMA357 13.3.4.2
			if (delegate_.GetNamespace() == null)
			{
				return "*::" + LocalName();
			}
			else
			{
				if (delegate_.GetNamespace().IsGlobal())
				{
					//    leave as empty
					return LocalName();
				}
				else
				{
					return Uri() + "::" + LocalName();
				}
			}
		}

		public string LocalName()
		{
			if (delegate_.GetLocalName() == null)
			{
				return "*";
			}
			return delegate_.GetLocalName();
		}

		internal string Prefix()
		{
			if (delegate_.GetNamespace() == null)
			{
				return null;
			}
			return delegate_.GetNamespace().GetPrefix();
		}

		internal string Uri()
		{
			if (delegate_.GetNamespace() == null)
			{
				return null;
			}
			return delegate_.GetNamespace().GetUri();
		}

		[System.ObsoleteAttribute(@"")]
		internal Rhino.Xmlimpl.XmlNode.QName ToNodeQname()
		{
			return delegate_;
		}

		internal Rhino.Xmlimpl.XmlNode.QName GetDelegate()
		{
			return delegate_;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Rhino.Xmlimpl.QName))
			{
				return false;
			}
			return Equals((Rhino.Xmlimpl.QName)obj);
		}

		public override int GetHashCode()
		{
			return delegate_.GetHashCode();
		}

		protected internal override object EquivalentValues(object value)
		{
			if (!(value is Rhino.Xmlimpl.QName))
			{
				return ScriptableConstants.NOT_FOUND;
			}
			bool result = Equals((Rhino.Xmlimpl.QName)value);
			return result ? true : false;
		}

		private bool Equals(Rhino.Xmlimpl.QName q)
		{
			return this.delegate_.Equals(q.delegate_);
		}

		public override string GetClassName()
		{
			return "QName";
		}

		public override object GetDefaultValue(Type hint)
		{
			return ToString();
		}

		private const int Id_localName = 1;

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
			// #generated# Last update: 2007-08-20 08:21:41 EDT
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
				if (s_length == 9)
				{
					X = "localName";
					id = Id_localName;
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
				case Id_localName:
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
				case Id_localName:
				{
					return "localName";
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
				case Id_localName:
				{
					return LocalName();
				}

				case Id_uri:
				{
					return Uri();
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
			// #generated# Last update: 2007-08-20 08:21:41 EDT
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
			InitPrototypeMethod(QNAME_TAG, id, s, arity);
		}

		public override object ExecIdCall(IdFunctionObject f, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (!f.HasTag(QNAME_TAG))
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

		private Rhino.Xmlimpl.QName RealThis(Scriptable thisObj, IdFunctionObject f)
		{
			if (!(thisObj is Rhino.Xmlimpl.QName))
			{
				throw IncompatibleCallError(f);
			}
			return (Rhino.Xmlimpl.QName)thisObj;
		}

		internal Rhino.Xmlimpl.QName NewQName(XMLLibImpl lib, string q_uri, string q_localName, string q_prefix)
		{
			Rhino.Xmlimpl.QName prototype = this.prototype;
			if (prototype == null)
			{
				prototype = this;
			}
			Rhino.Xmlimpl.XmlNode.Namespace ns = null;
			if (q_prefix != null)
			{
				ns = Rhino.Xmlimpl.XmlNode.Namespace.Create(q_prefix, q_uri);
			}
			else
			{
				if (q_uri != null)
				{
					ns = Rhino.Xmlimpl.XmlNode.Namespace.Create(q_uri);
				}
				else
				{
					ns = null;
				}
			}
			if (q_localName != null && q_localName.Equals("*"))
			{
				q_localName = null;
			}
			return Create(lib, this.GetParentScope(), prototype, Rhino.Xmlimpl.XmlNode.QName.Create(ns, q_localName));
		}

		//    See ECMA357 13.3.2
		internal Rhino.Xmlimpl.QName ConstructQName(XMLLibImpl lib, Context cx, object @namespace, object name)
		{
			string nameString = null;
			if (name is Rhino.Xmlimpl.QName)
			{
				if (@namespace == Undefined.instance)
				{
					return (Rhino.Xmlimpl.QName)name;
				}
				else
				{
					nameString = ((Rhino.Xmlimpl.QName)name).LocalName();
				}
			}
			if (name == Undefined.instance)
			{
				nameString = string.Empty;
			}
			else
			{
				nameString = ScriptRuntime.ToString(name);
			}
			if (@namespace == Undefined.instance)
			{
				if ("*".Equals(nameString))
				{
					@namespace = null;
				}
				else
				{
					@namespace = lib.GetDefaultNamespace(cx);
				}
			}
			Namespace namespaceNamespace = null;
			if (@namespace == null)
			{
			}
			else
			{
				//    leave as null
				if (@namespace is Namespace)
				{
					namespaceNamespace = (Namespace)@namespace;
				}
				else
				{
					namespaceNamespace = lib.NewNamespace(ScriptRuntime.ToString(@namespace));
				}
			}
			string q_localName = nameString;
			string q_uri;
			string q_prefix;
			if (@namespace == null)
			{
				q_uri = null;
				q_prefix = null;
			}
			else
			{
				//    corresponds to undefined; see QName class
				q_uri = namespaceNamespace.Uri();
				q_prefix = namespaceNamespace.Prefix();
			}
			return NewQName(lib, q_uri, q_localName, q_prefix);
		}

		internal Rhino.Xmlimpl.QName ConstructQName(XMLLibImpl lib, Context cx, object nameValue)
		{
			return ConstructQName(lib, cx, Undefined.instance, nameValue);
		}

		internal Rhino.Xmlimpl.QName CastToQName(XMLLibImpl lib, Context cx, object qnameValue)
		{
			if (qnameValue is Rhino.Xmlimpl.QName)
			{
				return (Rhino.Xmlimpl.QName)qnameValue;
			}
			return ConstructQName(lib, cx, qnameValue);
		}

		private object JsConstructor(Context cx, bool inNewExpr, object[] args)
		{
			//    See ECMA357 13.3.2
			if (!inNewExpr && args.Length == 1)
			{
				return CastToQName(lib, cx, args[0]);
			}
			if (args.Length == 0)
			{
				return ConstructQName(lib, cx, Undefined.instance);
			}
			else
			{
				if (args.Length == 1)
				{
					return ConstructQName(lib, cx, args[0]);
				}
				else
				{
					return ConstructQName(lib, cx, args[0], args[1]);
				}
			}
		}

		private string Js_toSource()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append('(');
			ToSourceImpl(Uri(), LocalName(), Prefix(), sb);
			sb.Append(')');
			return sb.ToString();
		}

		private static void ToSourceImpl(string uri, string localName, string prefix, StringBuilder sb)
		{
			sb.Append("new QName(");
			if (uri == null && prefix == null)
			{
				if (!"*".Equals(localName))
				{
					sb.Append("null, ");
				}
			}
			else
			{
				Namespace.ToSourceImpl(prefix, uri, sb);
				sb.Append(", ");
			}
			sb.Append('\'');
			sb.Append(ScriptRuntime.EscapeString(localName, '\''));
			sb.Append("')");
		}
	}
}
