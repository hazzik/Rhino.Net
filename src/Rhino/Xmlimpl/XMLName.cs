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
	[System.Serializable]
	internal class XMLName : Ref
	{
		internal const long serialVersionUID = 3832176310755686977L;

		private static bool IsNCNameStartChar(int c)
		{
			if ((c & ~unchecked((int)(0x7F))) == 0)
			{
				// Optimize for ASCII and use A..Z < _ < a..z
				if (c >= 'a')
				{
					return c <= 'z';
				}
				else
				{
					if (c >= 'A')
					{
						if (c <= 'Z')
						{
							return true;
						}
						return c == '_';
					}
				}
			}
			else
			{
				if ((c & ~unchecked((int)(0x1FFF))) == 0)
				{
					return (unchecked((int)(0xC0)) <= c && c <= unchecked((int)(0xD6))) || (unchecked((int)(0xD8)) <= c && c <= unchecked((int)(0xF6))) || (unchecked((int)(0xF8)) <= c && c <= unchecked((int)(0x2FF))) || (unchecked((int)(0x370)) <= c && c <= unchecked((int)(0x37D))) || unchecked((int)(0x37F)) <= c;
				}
			}
			return (unchecked((int)(0x200C)) <= c && c <= unchecked((int)(0x200D))) || (unchecked((int)(0x2070)) <= c && c <= unchecked((int)(0x218F))) || (unchecked((int)(0x2C00)) <= c && c <= unchecked((int)(0x2FEF))) || (unchecked((int)(0x3001)) <= c && c <= unchecked((int)(0xD7FF))) || (unchecked((int)(0xF900)) <= c && c <= unchecked((int)(0xFDCF))) || (unchecked((int)(0xFDF0)) <= c && c <= unchecked((int)
				(0xFFFD))) || (unchecked((int)(0x10000)) <= c && c <= unchecked((int)(0xEFFFF)));
		}

		private static bool IsNCNameChar(int c)
		{
			if ((c & ~unchecked((int)(0x7F))) == 0)
			{
				// Optimize for ASCII and use - < . < 0..9 < A..Z < _ < a..z
				if (c >= 'a')
				{
					return c <= 'z';
				}
				else
				{
					if (c >= 'A')
					{
						if (c <= 'Z')
						{
							return true;
						}
						return c == '_';
					}
					else
					{
						if (c >= '0')
						{
							return c <= '9';
						}
						else
						{
							return c == '-' || c == '.';
						}
					}
				}
			}
			else
			{
				if ((c & ~unchecked((int)(0x1FFF))) == 0)
				{
					return IsNCNameStartChar(c) || c == unchecked((int)(0xB7)) || (unchecked((int)(0x300)) <= c && c <= unchecked((int)(0x36F)));
				}
			}
			return IsNCNameStartChar(c) || (unchecked((int)(0x203F)) <= c && c <= unchecked((int)(0x2040)));
		}

		//    This means "accept" in the parsing sense
		//    See ECMA357 13.1.2.1
		internal static bool Accept(object nameObj)
		{
			string name;
			try
			{
				name = ScriptRuntime.ToString(nameObj);
			}
			catch (EcmaError ee)
			{
				if ("TypeError".Equals(ee.GetName()))
				{
					return false;
				}
				throw;
			}
			// See http://w3.org/TR/xml-names11/#NT-NCName
			int length = name.Length;
			if (length != 0)
			{
				if (IsNCNameStartChar(name[0]))
				{
					for (int i = 1; i != length; ++i)
					{
						if (!IsNCNameChar(name[i]))
						{
							return false;
						}
					}
					return true;
				}
			}
			return false;
		}

		private Rhino.Xmlimpl.XmlNode.QName qname;

		private bool isAttributeName;

		private bool isDescendants;

		private XMLObjectImpl xmlObject;

		private XMLName()
		{
		}

		internal static Rhino.Xmlimpl.XMLName FormStar()
		{
			Rhino.Xmlimpl.XMLName rv = new Rhino.Xmlimpl.XMLName();
			rv.qname = Rhino.Xmlimpl.XmlNode.QName.Create(null, null);
			return rv;
		}

		[System.ObsoleteAttribute(@"")]
		internal static Rhino.Xmlimpl.XMLName FormProperty(Rhino.Xmlimpl.XmlNode.Namespace @namespace, string localName)
		{
			if (localName != null && localName.Equals("*"))
			{
				localName = null;
			}
			Rhino.Xmlimpl.XMLName rv = new Rhino.Xmlimpl.XMLName();
			rv.qname = Rhino.Xmlimpl.XmlNode.QName.Create(@namespace, localName);
			return rv;
		}

		/// <summary>TODO: marked deprecated by original author</summary>
		internal static Rhino.Xmlimpl.XMLName FormProperty(string uri, string localName)
		{
			return FormProperty(Rhino.Xmlimpl.XmlNode.Namespace.Create(uri), localName);
		}

		/// <summary>TODO: marked deprecated by original implementor</summary>
		internal static Rhino.Xmlimpl.XMLName Create(string defaultNamespaceUri, string name)
		{
			if (name == null)
			{
				throw new ArgumentException();
			}
			int l = name.Length;
			if (l != 0)
			{
				char firstChar = name[0];
				if (firstChar == '*')
				{
					if (l == 1)
					{
						return Rhino.Xmlimpl.XMLName.FormStar();
					}
				}
				else
				{
					if (firstChar == '@')
					{
						Rhino.Xmlimpl.XMLName xmlName = Rhino.Xmlimpl.XMLName.FormProperty(string.Empty, Sharpen.Runtime.Substring(name, 1));
						xmlName.SetAttributeName();
						return xmlName;
					}
				}
			}
			return Rhino.Xmlimpl.XMLName.FormProperty(defaultNamespaceUri, name);
		}

		internal static Rhino.Xmlimpl.XMLName Create(Rhino.Xmlimpl.XmlNode.QName qname, bool attribute, bool descendants)
		{
			Rhino.Xmlimpl.XMLName rv = new Rhino.Xmlimpl.XMLName();
			rv.qname = qname;
			rv.isAttributeName = attribute;
			rv.isDescendants = descendants;
			return rv;
		}

		[System.ObsoleteAttribute(@"")]
		internal static Rhino.Xmlimpl.XMLName Create(Rhino.Xmlimpl.XmlNode.QName qname)
		{
			return Create(qname, false, false);
		}

		internal virtual void InitXMLObject(XMLObjectImpl xmlObject)
		{
			if (xmlObject == null)
			{
				throw new ArgumentException();
			}
			if (this.xmlObject != null)
			{
				throw new InvalidOperationException();
			}
			this.xmlObject = xmlObject;
		}

		internal virtual string Uri()
		{
			if (qname.GetNamespace() == null)
			{
				return null;
			}
			return qname.GetNamespace().GetUri();
		}

		internal virtual string LocalName()
		{
			if (qname.GetLocalName() == null)
			{
				return "*";
			}
			return qname.GetLocalName();
		}

		private void AddDescendantChildren(XMLList list, XML target)
		{
			Rhino.Xmlimpl.XMLName xmlName = this;
			if (target.IsElement())
			{
				XML[] children = target.GetChildren();
				for (int i = 0; i < children.Length; i++)
				{
					if (xmlName.Matches(children[i]))
					{
						list.AddToList(children[i]);
					}
					AddDescendantChildren(list, children[i]);
				}
			}
		}

		internal virtual void AddMatchingAttributes(XMLList list, XML target)
		{
			Rhino.Xmlimpl.XMLName name = this;
			if (target.IsElement())
			{
				XML[] attributes = target.GetAttributes();
				for (int i = 0; i < attributes.Length; i++)
				{
					if (name.Matches(attributes[i]))
					{
						list.AddToList(attributes[i]);
					}
				}
			}
		}

		private void AddDescendantAttributes(XMLList list, XML target)
		{
			if (target.IsElement())
			{
				AddMatchingAttributes(list, target);
				XML[] children = target.GetChildren();
				for (int i = 0; i < children.Length; i++)
				{
					AddDescendantAttributes(list, children[i]);
				}
			}
		}

		internal virtual XMLList MatchDescendantAttributes(XMLList rv, XML target)
		{
			rv.SetTargets(target, null);
			AddDescendantAttributes(rv, target);
			return rv;
		}

		internal virtual XMLList MatchDescendantChildren(XMLList rv, XML target)
		{
			rv.SetTargets(target, null);
			AddDescendantChildren(rv, target);
			return rv;
		}

		internal virtual void AddDescendants(XMLList rv, XML target)
		{
			Rhino.Xmlimpl.XMLName xmlName = this;
			if (xmlName.IsAttributeName())
			{
				MatchDescendantAttributes(rv, target);
			}
			else
			{
				MatchDescendantChildren(rv, target);
			}
		}

		private void AddAttributes(XMLList rv, XML target)
		{
			AddMatchingAttributes(rv, target);
		}

		internal virtual void AddMatches(XMLList rv, XML target)
		{
			if (IsDescendants())
			{
				AddDescendants(rv, target);
			}
			else
			{
				if (IsAttributeName())
				{
					AddAttributes(rv, target);
				}
				else
				{
					XML[] children = target.GetChildren();
					if (children != null)
					{
						for (int i = 0; i < children.Length; i++)
						{
							if (this.Matches(children[i]))
							{
								rv.AddToList(children[i]);
							}
						}
					}
					rv.SetTargets(target, this.ToQname());
				}
			}
		}

		internal virtual XMLList GetMyValueOn(XML target)
		{
			XMLList rv = target.NewXMLList();
			AddMatches(rv, target);
			return rv;
		}

		internal virtual void SetMyValueOn(XML target, object value)
		{
			// Special-case checks for undefined and null
			if (value == null)
			{
				value = "null";
			}
			else
			{
				if (value is Undefined)
				{
					value = "undefined";
				}
			}
			Rhino.Xmlimpl.XMLName xmlName = this;
			// Get the named property
			if (xmlName.IsAttributeName())
			{
				target.SetAttribute(xmlName, value);
			}
			else
			{
				if (xmlName.Uri() == null && xmlName.LocalName().Equals("*"))
				{
					target.SetChildren(value);
				}
				else
				{
					// Convert text into XML if needed.
					XMLObjectImpl xmlValue = null;
					if (value is XMLObjectImpl)
					{
						xmlValue = (XMLObjectImpl)value;
						// Check for attribute type and convert to textNode
						if (xmlValue is XML)
						{
							if (((XML)xmlValue).IsAttribute())
							{
								xmlValue = target.MakeXmlFromString(xmlName, xmlValue.ToString());
							}
						}
						if (xmlValue is XMLList)
						{
							for (int i = 0; i < xmlValue.Length(); i++)
							{
								XML xml = ((XMLList)xmlValue).Item(i);
								if (xml.IsAttribute())
								{
									((XMLList)xmlValue).Replace(i, target.MakeXmlFromString(xmlName, xml.ToString()));
								}
							}
						}
					}
					else
					{
						xmlValue = target.MakeXmlFromString(xmlName, ScriptRuntime.ToString(value));
					}
					XMLList matches = target.GetPropertyList(xmlName);
					if (matches.Length() == 0)
					{
						target.AppendChild(xmlValue);
					}
					else
					{
						// Remove all other matches
						for (int i = 1; i < matches.Length(); i++)
						{
							target.RemoveChild(matches.Item(i).ChildIndex());
						}
						// Replace first match with new value.
						XML firstMatch = matches.Item(0);
						target.Replace(firstMatch.ChildIndex(), xmlValue);
					}
				}
			}
		}

		public override bool Has(Context cx)
		{
			if (xmlObject == null)
			{
				return false;
			}
			return xmlObject.HasXMLProperty(this);
		}

		public override object Get(Context cx)
		{
			if (xmlObject == null)
			{
				throw ScriptRuntime.UndefReadError(Undefined.instance, ToString());
			}
			return xmlObject.GetXMLProperty(this);
		}

		public override object Set(Context cx, object value)
		{
			if (xmlObject == null)
			{
				throw ScriptRuntime.UndefWriteError(Undefined.instance, ToString(), value);
			}
			// Assignment to descendants causes parse error on bad reference
			// and this should not be called
			if (isDescendants)
			{
				throw Kit.CodeBug();
			}
			xmlObject.PutXMLProperty(this, value);
			return value;
		}

		public override bool Delete(Context cx)
		{
			if (xmlObject == null)
			{
				return true;
			}
			xmlObject.DeleteXMLProperty(this);
			return !xmlObject.HasXMLProperty(this);
		}

		public override string ToString()
		{
			//return qname.localName();
			StringBuilder buff = new StringBuilder();
			if (isDescendants)
			{
				buff.Append("..");
			}
			if (isAttributeName)
			{
				buff.Append('@');
			}
			if (Uri() == null)
			{
				buff.Append('*');
				if (LocalName().Equals("*"))
				{
					return buff.ToString();
				}
			}
			else
			{
				buff.Append('"').Append(Uri()).Append('"');
			}
			buff.Append(':').Append(LocalName());
			return buff.ToString();
		}

		internal Rhino.Xmlimpl.XmlNode.QName ToQname()
		{
			return this.qname;
		}

		internal bool MatchesLocalName(string localName)
		{
			return LocalName().Equals("*") || LocalName().Equals(localName);
		}

		internal bool MatchesElement(Rhino.Xmlimpl.XmlNode.QName qname)
		{
			if (this.Uri() == null || this.Uri().Equals(qname.GetNamespace().GetUri()))
			{
				if (this.LocalName().Equals("*") || this.LocalName().Equals(qname.GetLocalName()))
				{
					return true;
				}
			}
			return false;
		}

		internal bool Matches(XML node)
		{
			Rhino.Xmlimpl.XmlNode.QName qname = node.GetNodeQname();
			string nodeUri = null;
			if (qname.GetNamespace() != null)
			{
				nodeUri = qname.GetNamespace().GetUri();
			}
			if (isAttributeName)
			{
				if (node.IsAttribute())
				{
					if (this.Uri() == null || this.Uri().Equals(nodeUri))
					{
						if (this.LocalName().Equals("*") || this.LocalName().Equals(qname.GetLocalName()))
						{
							return true;
						}
					}
					return false;
				}
				else
				{
					//    TODO    Could throw exception maybe, should not call this method on attribute name with arbitrary node type
					//            unless we traverse all attributes and children habitually
					return false;
				}
			}
			else
			{
				if (this.Uri() == null || ((node.IsElement()) && this.Uri().Equals(nodeUri)))
				{
					if (LocalName().Equals("*"))
					{
						return true;
					}
					if (node.IsElement())
					{
						if (LocalName().Equals(qname.GetLocalName()))
						{
							return true;
						}
					}
				}
				return false;
			}
		}

		internal virtual bool IsAttributeName()
		{
			return isAttributeName;
		}

		// TODO Fix whether this is an attribute XMLName at construction?
		// Marked deprecated by original author
		internal virtual void SetAttributeName()
		{
			//        if (isAttributeName) throw new IllegalStateException();
			isAttributeName = true;
		}

		internal virtual bool IsDescendants()
		{
			return isDescendants;
		}

		//    TODO    Fix whether this is an descendant XMLName at construction?
		[System.ObsoleteAttribute(@"")]
		internal virtual void SetIsDescendants()
		{
			//        if (isDescendants) throw new IllegalStateException();
			isDescendants = true;
		}
	}
}
