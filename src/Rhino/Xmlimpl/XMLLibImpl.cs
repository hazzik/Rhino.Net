/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
#if XML

using System;
using Rhino;
using Rhino.Xml;
using Rhino.XmlImpl;
using Sharpen;

namespace Rhino.XmlImpl
{
	[System.Serializable]
	public sealed class XMLLibImpl : XMLLib
	{
		//
		//    EXPERIMENTAL Java interface
		//
		/// <summary>This experimental interface is undocumented.</summary>
		/// <remarks>This experimental interface is undocumented.</remarks>
		public static System.Xml.XmlNode ToDomNode(object xmlObject)
		{
			//    Could return DocumentFragment for XMLList
			//    Probably a single node for XMLList with one element
			if (xmlObject is XML)
			{
				return ((XML)xmlObject).ToDomNode();
			}
			else
			{
				throw new ArgumentException("xmlObject is not an XML object in JavaScript.");
			}
		}

		public static void Init(Context cx, Scriptable scope, bool @sealed)
		{
			XMLLibImpl lib = new XMLLibImpl(scope);
			XMLLib bound = lib.BindToScope(scope);
			if (bound == lib)
			{
				lib.ExportToScope(@sealed);
			}
		}

		public override void SetIgnoreComments(bool b)
		{
			options.SetIgnoreComments(b);
		}

		public override void SetIgnoreWhitespace(bool b)
		{
			options.SetIgnoreWhitespace(b);
		}

		public override void SetIgnoreProcessingInstructions(bool b)
		{
			options.SetIgnoreProcessingInstructions(b);
		}

		public override void SetPrettyPrinting(bool b)
		{
			options.SetPrettyPrinting(b);
		}

		public override void SetPrettyIndent(int i)
		{
			options.SetPrettyIndent(i);
		}

		public override bool IsIgnoreComments()
		{
			return options.IsIgnoreComments();
		}

		public override bool IsIgnoreProcessingInstructions()
		{
			return options.IsIgnoreProcessingInstructions();
		}

		public override bool IsIgnoreWhitespace()
		{
			return options.IsIgnoreWhitespace();
		}

		public override bool IsPrettyPrinting()
		{
			return options.IsPrettyPrinting();
		}

		public override int GetPrettyIndent()
		{
			return options.GetPrettyIndent();
		}

		private Scriptable globalScope;

		private XML xmlPrototype;

		private XMLList xmlListPrototype;

		private Namespace namespacePrototype;

		private QName qnamePrototype;

		private XmlProcessor options = new XmlProcessor();

		private XMLLibImpl(Scriptable globalScope)
		{
			this.globalScope = globalScope;
		}

		internal XmlProcessor GetProcessor()
		{
			return options;
		}

		private void ExportToScope(bool @sealed)
		{
			xmlPrototype = NewXML(XmlNode.CreateText(options, string.Empty));
			xmlListPrototype = NewXMLList();
			namespacePrototype = Namespace.Create(this.globalScope, null, XmlNode.Namespace.GLOBAL);
			qnamePrototype = QName.Create(this, this.globalScope, null, XmlNode.QName.Create(XmlNode.Namespace.Create(string.Empty), string.Empty));
			xmlPrototype.ExportAsJSClass(@sealed);
			xmlListPrototype.ExportAsJSClass(@sealed);
			namespacePrototype.ExportAsJSClass(@sealed);
			qnamePrototype.ExportAsJSClass(@sealed);
		}

		[Obsolete(@"")]
		internal XMLName ToAttributeName(Context cx, object nameValue)
		{
			if (nameValue is XMLName)
			{
				//    TODO    Will this always be an XMLName of type attribute name?
				return (XMLName)nameValue;
			}
			else
			{
				if (nameValue is QName)
				{
					return XMLName.Create(((QName)nameValue).GetDelegate(), true, false);
				}
				else
				{
					if (nameValue is bool || nameValue.IsNumber()|| nameValue == Undefined.instance || nameValue == null)
					{
						throw BadXMLName(nameValue);
					}
					else
					{
						//    TODO    Not 100% sure that putting these in global namespace is the right thing to do
						string localName = null;
						if (nameValue is string)
						{
							localName = (string)nameValue;
						}
						else
						{
							localName = ScriptRuntime.ToString(nameValue);
						}
						if (localName != null && localName.Equals("*"))
						{
							localName = null;
						}
						return XMLName.Create(XmlNode.QName.Create(XmlNode.Namespace.Create(string.Empty), localName), true, false);
					}
				}
			}
		}

		private static Exception BadXMLName(object value)
		{
			string msg;
			if (value.IsNumber())
			{
				msg = "Can not construct XML name from number: ";
			}
			else
			{
				if (value is bool)
				{
					msg = "Can not construct XML name from boolean: ";
				}
				else
				{
					if (value == Undefined.instance || value == null)
					{
						msg = "Can not construct XML name from ";
					}
					else
					{
						throw new ArgumentException(value.ToString());
					}
				}
			}
			return ScriptRuntime.TypeError(msg + ScriptRuntime.ToString(value));
		}

		internal XMLName ToXMLNameFromString(Context cx, string name)
		{
			return XMLName.Create(GetDefaultNamespaceURI(cx), name);
		}

		internal XMLName ToXMLName(Context cx, object nameValue)
		{
			XMLName result;
			if (nameValue is XMLName)
			{
				result = (XMLName)nameValue;
			}
			else
			{
				if (nameValue is QName)
				{
					QName qname = (QName)nameValue;
					result = XMLName.FormProperty(qname.Uri(), qname.LocalName());
				}
				else
				{
					if (nameValue is string)
					{
						result = ToXMLNameFromString(cx, (string)nameValue);
					}
					else
					{
						if (nameValue is bool || nameValue.IsNumber()|| nameValue == Undefined.instance || nameValue == null)
						{
							throw BadXMLName(nameValue);
						}
						else
						{
							string name = ScriptRuntime.ToString(nameValue);
							result = ToXMLNameFromString(cx, name);
						}
					}
				}
			}
			return result;
		}

		/// <summary>
		/// If value represents Uint32 index, make it available through
		/// ScriptRuntime.lastUint32Result(cx) and return null.
		/// </summary>
		/// <remarks>
		/// If value represents Uint32 index, make it available through
		/// ScriptRuntime.lastUint32Result(cx) and return null.
		/// Otherwise return the same value as toXMLName(cx, value).
		/// </remarks>
		internal XMLName ToXMLNameOrIndex(Context cx, object value)
		{
			XMLName result;
			if (value is XMLName)
			{
				result = (XMLName)value;
			}
			else
			{
				if (value is string)
				{
					string str = (string)value;
					long test = ScriptRuntime.TestUint32String(str);
					if (test >= 0)
					{
						ScriptRuntime.StoreUint32Result(cx, test);
						result = null;
					}
					else
					{
						result = ToXMLNameFromString(cx, str);
					}
				}
				else
				{
					if (value.IsNumber())
					{
						double d = System.Convert.ToDouble(value);
						long l = (long)d;
						if (l == d && 0 <= l && l <= unchecked((long)(0xFFFFFFFFL)))
						{
							ScriptRuntime.StoreUint32Result(cx, l);
							result = null;
						}
						else
						{
							throw BadXMLName(value);
						}
					}
					else
					{
						if (value is QName)
						{
							QName qname = (QName)value;
							string uri = qname.Uri();
							bool number = false;
							result = null;
							if (uri != null && uri.Length == 0)
							{
								// Only in this case qname.toString() can resemble uint32
								long test = ScriptRuntime.TestUint32String(uri);
								if (test >= 0)
								{
									ScriptRuntime.StoreUint32Result(cx, test);
									number = true;
								}
							}
							if (!number)
							{
								result = XMLName.FormProperty(uri, qname.LocalName());
							}
						}
						else
						{
							if (value is bool || value == Undefined.instance || value == null)
							{
								throw BadXMLName(value);
							}
							else
							{
								string str = ScriptRuntime.ToString(value);
								long test = ScriptRuntime.TestUint32String(str);
								if (test >= 0)
								{
									ScriptRuntime.StoreUint32Result(cx, test);
									result = null;
								}
								else
								{
									result = ToXMLNameFromString(cx, str);
								}
							}
						}
					}
				}
			}
			return result;
		}

		internal object AddXMLObjects(Context cx, XMLObject obj1, XMLObject obj2)
		{
			XMLList listToAdd = NewXMLList();
			if (obj1 is XMLList)
			{
				XMLList list1 = (XMLList)obj1;
				if (list1.Length() == 1)
				{
					listToAdd.AddToList(list1.Item(0));
				}
				else
				{
					// Might be xmlFragment + xmlFragment + xmlFragment + ...;
					// then the result will be an XMLList which we want to be an
					// rValue and allow it to be assigned to an lvalue.
					listToAdd = NewXMLListFrom(obj1);
				}
			}
			else
			{
				listToAdd.AddToList(obj1);
			}
			if (obj2 is XMLList)
			{
				XMLList list2 = (XMLList)obj2;
				for (int i = 0; i < list2.Length(); i++)
				{
					listToAdd.AddToList(list2.Item(i));
				}
			}
			else
			{
				if (obj2 is XML)
				{
					listToAdd.AddToList(obj2);
				}
			}
			return listToAdd;
		}

		private Ref XmlPrimaryReference(Context cx, XMLName xmlName, Scriptable scope)
		{
			XMLObjectImpl xmlObj;
			XMLObjectImpl firstXml = null;
			for (; ; )
			{
				// XML object can only present on scope chain as a wrapper
				// of XMLWithScope
				if (scope is XMLWithScope)
				{
					xmlObj = (XMLObjectImpl)scope.GetPrototype();
					if (xmlObj.HasXMLProperty(xmlName))
					{
						break;
					}
					if (firstXml == null)
					{
						firstXml = xmlObj;
					}
				}
				scope = scope.ParentScope;
				if (scope == null)
				{
					xmlObj = firstXml;
					break;
				}
			}
			// xmlObj == null corresponds to undefined as the target of
			// the reference
			if (xmlObj != null)
			{
				xmlName.InitXMLObject(xmlObj);
			}
			return xmlName;
		}

		internal Namespace CastToNamespace(Context cx, object namespaceObj)
		{
			return this.namespacePrototype.CastToNamespace(namespaceObj);
		}

		private string GetDefaultNamespaceURI(Context cx)
		{
			return GetDefaultNamespace(cx).Uri();
		}

		internal Namespace NewNamespace(string uri)
		{
			return this.namespacePrototype.NewNamespace(uri);
		}

		internal Namespace GetDefaultNamespace(Context cx)
		{
			if (cx == null)
			{
				cx = Context.GetCurrentContext();
				if (cx == null)
				{
					return namespacePrototype;
				}
			}
			object ns = ScriptRuntime.SearchDefaultNamespace(cx);
			if (ns == null)
			{
				return namespacePrototype;
			}
			else
			{
				if (ns is Namespace)
				{
					return (Namespace)ns;
				}
				else
				{
					//    TODO    Clarify or remove the following comment
					// Should not happen but for now it could
					// due to bad searchDefaultNamespace implementation.
					return namespacePrototype;
				}
			}
		}

		internal Namespace[] CreateNamespaces(XmlNode.Namespace[] declarations)
		{
			Namespace[] rv = new Namespace[declarations.Length];
			for (int i = 0; i < declarations.Length; i++)
			{
				rv[i] = this.namespacePrototype.NewNamespace(declarations[i].GetPrefix(), declarations[i].GetUri());
			}
			return rv;
		}

		//    See ECMA357 13.3.2
		internal QName ConstructQName(Context cx, object @namespace, object name)
		{
			return this.qnamePrototype.ConstructQName(this, cx, @namespace, name);
		}

		internal QName NewQName(string uri, string localName, string prefix)
		{
			return this.qnamePrototype.NewQName(this, uri, localName, prefix);
		}

		internal QName ConstructQName(Context cx, object nameValue)
		{
			//        return constructQName(cx, Undefined.instance, nameValue);
			return this.qnamePrototype.ConstructQName(this, cx, nameValue);
		}

		internal QName CastToQName(Context cx, object qnameValue)
		{
			return this.qnamePrototype.CastToQName(this, cx, qnameValue);
		}

		internal QName NewQName(XmlNode.QName qname)
		{
			return QName.Create(this, this.globalScope, this.qnamePrototype, qname);
		}

		internal XML NewXML(XmlNode node)
		{
			return new XML(this, this.globalScope, this.xmlPrototype, node);
		}

		internal XML NewXMLFromJs(object inputObject)
		{
			string frag;
			if (inputObject == null || inputObject == Undefined.instance)
			{
				frag = string.Empty;
			}
			else
			{
				if (inputObject is XMLObjectImpl)
				{
					// todo: faster way for XMLObjects?
					frag = ((XMLObjectImpl)inputObject).ToXMLString();
				}
				else
				{
					frag = ScriptRuntime.ToString(inputObject);
				}
			}
			if (frag.Trim().StartsWith("<>"))
			{
				throw ScriptRuntime.TypeError("Invalid use of XML object anonymous tags <></>.");
			}
			if (frag.IndexOf("<") == -1)
			{
				//    Solo text node
				return NewXML(XmlNode.CreateText(options, frag));
			}
			return Parse(frag);
		}

		private XML Parse(string frag)
		{
			try
			{
				return NewXML(XmlNode.CreateElement(options, GetDefaultNamespaceURI(Context.GetCurrentContext()), frag));
			}
			catch (Exception e)
			{
				throw ScriptRuntime.TypeError("Cannot parse XML: " + e.Message);
			}
		}

		internal XML EcmaToXml(object @object)
		{
			//    See ECMA357 10.3
			if (@object == null || @object == Undefined.instance)
			{
				throw ScriptRuntime.TypeError("Cannot convert " + @object + " to XML");
			}
			if (@object is XML)
			{
				return (XML)@object;
			}
			if (@object is XMLList)
			{
				XMLList list = (XMLList)@object;
				if (list.GetXML() != null)
				{
					return list.GetXML();
				}
				else
				{
					throw ScriptRuntime.TypeError("Cannot convert list of >1 element to XML");
				}
			}
			//    TODO    Technically we should fail on anything except a String, Number or Boolean
			//            See ECMA357 10.3
			// Extension: if object is a DOM node, use that to construct the XML
			// object.
			if (@object is Wrapper)
			{
				@object = ((Wrapper)@object).Unwrap();
			}
			if (@object is System.Xml.XmlNode)
			{
				System.Xml.XmlNode node = (System.Xml.XmlNode)@object;
				return NewXML(XmlNode.CreateElementFromNode(node));
			}
			//    Instead we just blindly cast to a String and let them convert anything.
			string s = ScriptRuntime.ToString(@object);
			//    TODO    Could this get any uglier?
			if (s.Length > 0 && s[0] == '<')
			{
				return Parse(s);
			}
			else
			{
				return NewXML(XmlNode.CreateText(options, s));
			}
		}

		internal XML NewTextElementXML(XmlNode reference, XmlNode.QName qname, string value)
		{
			return NewXML(XmlNode.NewElementWithText(options, reference, qname, value));
		}

		internal XMLList NewXMLList()
		{
			return new XMLList(this, this.globalScope, this.xmlListPrototype);
		}

		internal XMLList NewXMLListFrom(object inputObject)
		{
			XMLList rv = NewXMLList();
			if (inputObject == null || inputObject is Undefined)
			{
				return rv;
			}
			else
			{
				if (inputObject is XML)
				{
					XML xml = (XML)inputObject;
					rv.GetNodeList().Add(xml);
					return rv;
				}
				else
				{
					if (inputObject is XMLList)
					{
						XMLList xmll = (XMLList)inputObject;
						rv.GetNodeList().Add(xmll.GetNodeList());
						return rv;
					}
					else
					{
						string frag = ScriptRuntime.ToString(inputObject).Trim();
						if (!frag.StartsWith("<>"))
						{
							frag = "<>" + frag + "</>";
						}
						frag = "<fragment>" + frag.Substring(2);
						if (!frag.EndsWith("</>"))
						{
							throw ScriptRuntime.TypeError("XML with anonymous tag missing end anonymous tag");
						}
						frag = frag.Substring(0, frag.Length - 3) + "</fragment>";
						XML orgXML = NewXMLFromJs(frag);
						// Now orphan the children and add them to our XMLList.
						XMLList children = orgXML.Children();
						for (int i = 0; i < children.GetNodeList().Length(); i++)
						{
							// Copy here is so that they'll be orphaned (parent() will be undefined)
							rv.GetNodeList().Add(((XML)children.Item(i).Copy()));
						}
						return rv;
					}
				}
			}
		}

		internal XmlNode.QName ToNodeQName(Context cx, object namespaceValue, object nameValue)
		{
			// This is duplication of constructQName(cx, namespaceValue, nameValue)
			// but for XMLName
			string localName;
			if (nameValue is QName)
			{
				QName qname = (QName)nameValue;
				localName = qname.LocalName();
			}
			else
			{
				localName = ScriptRuntime.ToString(nameValue);
			}
			XmlNode.Namespace ns;
			if (namespaceValue == Undefined.instance)
			{
				if ("*".Equals(localName))
				{
					ns = null;
				}
				else
				{
					ns = GetDefaultNamespace(cx).GetDelegate();
				}
			}
			else
			{
				if (namespaceValue == null)
				{
					ns = null;
				}
				else
				{
					if (namespaceValue is Namespace)
					{
						ns = ((Namespace)namespaceValue).GetDelegate();
					}
					else
					{
						ns = this.namespacePrototype.ConstructNamespace(namespaceValue).GetDelegate();
					}
				}
			}
			if (localName != null && localName.Equals("*"))
			{
				localName = null;
			}
			return XmlNode.QName.Create(ns, localName);
		}

		internal XmlNode.QName ToNodeQName(Context cx, string name, bool attribute)
		{
			XmlNode.Namespace defaultNamespace = GetDefaultNamespace(cx).GetDelegate();
			if (name != null && name.Equals("*"))
			{
				return XmlNode.QName.Create(null, null);
			}
			else
			{
				if (attribute)
				{
					return XmlNode.QName.Create(XmlNode.Namespace.GLOBAL, name);
				}
				else
				{
					return XmlNode.QName.Create(defaultNamespace, name);
				}
			}
		}

		internal XmlNode.QName ToNodeQName(Context cx, object nameValue, bool attribute)
		{
			if (nameValue is XMLName)
			{
				return ((XMLName)nameValue).ToQname();
			}
			else
			{
				if (nameValue is QName)
				{
					QName qname = (QName)nameValue;
					return qname.GetDelegate();
				}
				else
				{
					if (nameValue is bool || nameValue.IsNumber()|| nameValue == Undefined.instance || nameValue == null)
					{
						throw BadXMLName(nameValue);
					}
					else
					{
						string local = null;
						if (nameValue is string)
						{
							local = (string)nameValue;
						}
						else
						{
							local = ScriptRuntime.ToString(nameValue);
						}
						return ToNodeQName(cx, local, attribute);
					}
				}
			}
		}

		//
		//    Override methods from XMLLib
		//
		public override bool IsXMLName(Context _cx, object nameObj)
		{
			return XMLName.Accept(nameObj);
		}

		public override object ToDefaultXmlNamespace(Context cx, object uriValue)
		{
			return this.namespacePrototype.ConstructNamespace(uriValue);
		}

		public override string EscapeTextValue(object o)
		{
			return options.EscapeTextValue(o);
		}

		public override string EscapeAttributeValue(object o)
		{
			return options.EscapeAttributeValue(o);
		}

		public override Ref NameRef(Context cx, object name, Scriptable scope, int memberTypeFlags)
		{
			if ((memberTypeFlags & Node.ATTRIBUTE_FLAG) == 0)
			{
				// should only be called for cases like @name or @[expr]
				throw Kit.CodeBug();
			}
			XMLName xmlName = ToAttributeName(cx, name);
			return XmlPrimaryReference(cx, xmlName, scope);
		}

		public override Ref NameRef(Context cx, object @namespace, object name, Scriptable scope, int memberTypeFlags)
		{
			XMLName xmlName = XMLName.Create(ToNodeQName(cx, @namespace, name), false, false);
			//    No idea what is coming in from the parser in this case; is it detecting the "@"?
			if ((memberTypeFlags & Node.ATTRIBUTE_FLAG) != 0)
			{
				if (!xmlName.IsAttributeName())
				{
					xmlName.SetAttributeName();
				}
			}
			return XmlPrimaryReference(cx, xmlName, scope);
		}
	}
}
#endif
