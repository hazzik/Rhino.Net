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
using Rhino.Xml;
using Rhino.Xmlimpl;
using Sharpen;

namespace Rhino.Xmlimpl
{
	[System.Serializable]
	internal class XML : XMLObjectImpl
	{
		internal const long serialVersionUID = -630969919086449092L;

		private Rhino.Xmlimpl.XmlNode node;

		internal XML(XMLLibImpl lib, Scriptable scope, XMLObject prototype, Rhino.Xmlimpl.XmlNode node) : base(lib, scope, prototype)
		{
			Initialize(node);
		}

		internal virtual void Initialize(Rhino.Xmlimpl.XmlNode node)
		{
			this.node = node;
			this.node.SetXml(this);
		}

		internal sealed override Rhino.Xmlimpl.XML GetXML()
		{
			return this;
		}

		internal virtual void ReplaceWith(Rhino.Xmlimpl.XML value)
		{
			//    We use the underlying document structure if the node is not
			//    "standalone," but we need to just replace the XmlNode instance
			//    otherwise
			if (this.node.Parent() != null || false)
			{
				this.node.ReplaceWith(value.node);
			}
			else
			{
				this.Initialize(value.node);
			}
		}

		internal virtual Rhino.Xmlimpl.XML MakeXmlFromString(XMLName name, string value)
		{
			try
			{
				return NewTextElementXML(this.node, name.ToQname(), value);
			}
			catch (Exception e)
			{
				throw ScriptRuntime.TypeError(e.Message);
			}
		}

		internal virtual Rhino.Xmlimpl.XmlNode GetAnnotation()
		{
			return node;
		}

		//
		//  Methods from ScriptableObject
		//
		//    TODO Either cross-reference this next comment with the specification or delete it and change the behavior
		//    The comment: XML[0] should return this, all other indexes are Undefined
		public override object Get(int index, Scriptable start)
		{
			if (index == 0)
			{
				return this;
			}
			else
			{
				return ScriptableConstants.NOT_FOUND;
			}
		}

		public override bool Has(int index, Scriptable start)
		{
			return (index == 0);
		}

		public override void Put(int index, Scriptable start, object value)
		{
			//    TODO    Clarify the following comment and add a reference to the spec
			//    The comment: Spec says assignment to indexed XML object should return type error
			throw ScriptRuntime.TypeError("Assignment to indexed XML is not allowed");
		}

		public override object[] GetIds()
		{
			if (IsPrototype())
			{
				return new object[0];
			}
			else
			{
				return new object[] { Sharpen.Extensions.ValueOf(0) };
			}
		}

		//    TODO    This is how I found it but I am not sure it makes sense
		public override void Delete(int index)
		{
			if (index == 0)
			{
				this.Remove();
			}
		}

		//
		//    Methods from XMLObjectImpl
		//
		internal override bool HasXMLProperty(XMLName xmlName)
		{
			return (GetPropertyList(xmlName).Length() > 0);
		}

		internal override object GetXMLProperty(XMLName xmlName)
		{
			return GetPropertyList(xmlName);
		}

		//
		//
		//    Methods that merit further review
		//
		//
		internal virtual Rhino.Xmlimpl.XmlNode.QName GetNodeQname()
		{
			return this.node.GetQname();
		}

		internal virtual Rhino.Xmlimpl.XML[] GetChildren()
		{
			if (!IsElement())
			{
				return null;
			}
			Rhino.Xmlimpl.XmlNode[] children = this.node.GetMatchingChildren(Rhino.Xmlimpl.XmlNode.Filter.TRUE);
			Rhino.Xmlimpl.XML[] rv = new Rhino.Xmlimpl.XML[children.Length];
			for (int i = 0; i < rv.Length; i++)
			{
				rv[i] = ToXML(children[i]);
			}
			return rv;
		}

		internal virtual Rhino.Xmlimpl.XML[] GetAttributes()
		{
			Rhino.Xmlimpl.XmlNode[] attributes = this.node.GetAttributes();
			Rhino.Xmlimpl.XML[] rv = new Rhino.Xmlimpl.XML[attributes.Length];
			for (int i = 0; i < rv.Length; i++)
			{
				rv[i] = ToXML(attributes[i]);
			}
			return rv;
		}

		//    Used only by XML, XMLList
		internal virtual XMLList GetPropertyList(XMLName name)
		{
			return name.GetMyValueOn(this);
		}

		internal override void DeleteXMLProperty(XMLName name)
		{
			XMLList list = GetPropertyList(name);
			for (int i = 0; i < list.Length(); i++)
			{
				list.Item(i).node.DeleteMe();
			}
		}

		internal override void PutXMLProperty(XMLName xmlName, object value)
		{
			if (IsPrototype())
			{
			}
			else
			{
				//    TODO    Is this really a no-op?  Check the spec to be sure
				xmlName.SetMyValueOn(this, value);
			}
		}

		internal override bool HasOwnProperty(XMLName xmlName)
		{
			bool hasProperty = false;
			if (IsPrototype())
			{
				string property = xmlName.LocalName();
				hasProperty = (0 != FindPrototypeId(property));
			}
			else
			{
				hasProperty = (GetPropertyList(xmlName).Length() > 0);
			}
			return hasProperty;
		}

		protected internal override object JsConstructor(Context cx, bool inNewExpr, object[] args)
		{
			if (args.Length == 0 || args[0] == null || args[0] == Undefined.instance)
			{
				args = new object[] { string.Empty };
			}
			//    ECMA 13.4.2 does not appear to specify what to do if multiple arguments are sent.
			Rhino.Xmlimpl.XML toXml = EcmaToXml(args[0]);
			if (inNewExpr)
			{
				return toXml.Copy();
			}
			else
			{
				return toXml;
			}
		}

		//    See ECMA 357, 11_2_2_1, Semantics, 3_f.
		public override Scriptable GetExtraMethodSource(Context cx)
		{
			if (HasSimpleContent())
			{
				string src = ToString();
				return ScriptRuntime.ToObjectOrNull(cx, src);
			}
			return null;
		}

		//
		//    TODO    Miscellaneous methods not yet grouped
		//
		internal virtual void RemoveChild(int index)
		{
			this.node.RemoveChild(index);
		}

		internal override void Normalize()
		{
			this.node.Normalize();
		}

		private Rhino.Xmlimpl.XML ToXML(Rhino.Xmlimpl.XmlNode node)
		{
			if (node.GetXml() == null)
			{
				node.SetXml(NewXML(node));
			}
			return node.GetXml();
		}

		internal virtual void SetAttribute(XMLName xmlName, object value)
		{
			if (!IsElement())
			{
				throw new InvalidOperationException("Can only set attributes on elements.");
			}
			//    TODO    Is this legal, but just not "supported"?  If so, support it.
			if (xmlName.Uri() == null && xmlName.LocalName().Equals("*"))
			{
				throw ScriptRuntime.TypeError("@* assignment not supported.");
			}
			this.node.SetAttribute(xmlName.ToQname(), ScriptRuntime.ToString(value));
		}

		internal virtual void Remove()
		{
			this.node.DeleteMe();
		}

		internal override void AddMatches(XMLList rv, XMLName name)
		{
			name.AddMatches(rv, this);
		}

		internal override XMLList Elements(XMLName name)
		{
			XMLList rv = NewXMLList();
			rv.SetTargets(this, name.ToQname());
			//    TODO    Should have an XMLNode.Filter implementation based on XMLName
			Rhino.Xmlimpl.XmlNode[] elements = this.node.GetMatchingChildren(Rhino.Xmlimpl.XmlNode.Filter.ELEMENT);
			for (int i = 0; i < elements.Length; i++)
			{
				if (name.Matches(ToXML(elements[i])))
				{
					rv.AddToList(ToXML(elements[i]));
				}
			}
			return rv;
		}

		internal override XMLList Child(XMLName xmlName)
		{
			//    TODO    Right now I think this method would allow child( "@xxx" ) to return the xxx attribute, which is wrong
			XMLList rv = NewXMLList();
			//    TODO    Should this also match processing instructions?  If so, we have to change the filter and also the XMLName
			//            class to add an acceptsProcessingInstruction() method
			Rhino.Xmlimpl.XmlNode[] elements = this.node.GetMatchingChildren(Rhino.Xmlimpl.XmlNode.Filter.ELEMENT);
			for (int i = 0; i < elements.Length; i++)
			{
				if (xmlName.MatchesElement(elements[i].GetQname()))
				{
					rv.AddToList(ToXML(elements[i]));
				}
			}
			rv.SetTargets(this, xmlName.ToQname());
			return rv;
		}

		internal virtual Rhino.Xmlimpl.XML Replace(XMLName xmlName, object xml)
		{
			PutXMLProperty(xmlName, xml);
			return this;
		}

		internal override XMLList Children()
		{
			XMLList rv = NewXMLList();
			XMLName all = XMLName.FormStar();
			rv.SetTargets(this, all.ToQname());
			Rhino.Xmlimpl.XmlNode[] children = this.node.GetMatchingChildren(Rhino.Xmlimpl.XmlNode.Filter.TRUE);
			for (int i = 0; i < children.Length; i++)
			{
				rv.AddToList(ToXML(children[i]));
			}
			return rv;
		}

		internal override XMLList Child(int index)
		{
			//    ECMA357 13.4.4.6 (numeric case)
			XMLList result = NewXMLList();
			result.SetTargets(this, null);
			if (index >= 0 && index < this.node.GetChildCount())
			{
				result.AddToList(GetXmlChild(index));
			}
			return result;
		}

		internal virtual Rhino.Xmlimpl.XML GetXmlChild(int index)
		{
			Rhino.Xmlimpl.XmlNode child = this.node.GetChild(index);
			if (child.GetXml() == null)
			{
				child.SetXml(NewXML(child));
			}
			return child.GetXml();
		}

		internal virtual Rhino.Xmlimpl.XML GetLastXmlChild()
		{
			int pos = this.node.GetChildCount() - 1;
			if (pos < 0)
			{
				return null;
			}
			return GetXmlChild(pos);
		}

		internal virtual int ChildIndex()
		{
			return this.node.GetChildIndex();
		}

		internal override bool Contains(object xml)
		{
			if (xml is Rhino.Xmlimpl.XML)
			{
				return EquivalentXml(xml);
			}
			else
			{
				return false;
			}
		}

		//    Method overriding XMLObjectImpl
		internal override bool EquivalentXml(object target)
		{
			bool result = false;
			if (target is Rhino.Xmlimpl.XML)
			{
				//    TODO    This is a horrifyingly inefficient way to do this so we should make it better.  It may also not work.
				return this.node.ToXmlString(GetProcessor()).Equals(((Rhino.Xmlimpl.XML)target).node.ToXmlString(GetProcessor()));
			}
			else
			{
				if (target is XMLList)
				{
					//    TODO    Is this right?  Check the spec ...
					XMLList otherList = (XMLList)target;
					if (otherList.Length() == 1)
					{
						result = EquivalentXml(otherList.GetXML());
					}
				}
				else
				{
					if (HasSimpleContent())
					{
						string otherStr = ScriptRuntime.ToString(target);
						result = ToString().Equals(otherStr);
					}
				}
			}
			return result;
		}

		internal override XMLObjectImpl Copy()
		{
			return NewXML(this.node.Copy());
		}

		internal override bool HasSimpleContent()
		{
			if (IsComment() || IsProcessingInstruction())
			{
				return false;
			}
			if (IsText() || this.node.IsAttributeType())
			{
				return true;
			}
			return !this.node.HasChildElement();
		}

		internal override bool HasComplexContent()
		{
			return !HasSimpleContent();
		}

		//    TODO Cross-reference comment below with spec
		//    Comment is: Length of an XML object is always 1, it's a list of XML objects of size 1.
		internal override int Length()
		{
			return 1;
		}

		//    TODO    it is not clear what this method was for ...
		internal virtual bool Is(Rhino.Xmlimpl.XML other)
		{
			return this.node.IsSameNode(other.node);
		}

		internal virtual object NodeKind()
		{
			return EcmaClass();
		}

		internal override object Parent()
		{
			Rhino.Xmlimpl.XmlNode parent = this.node.Parent();
			if (parent == null)
			{
				return null;
			}
			return NewXML(this.node.Parent());
		}

		internal override bool PropertyIsEnumerable(object name)
		{
			bool result;
			if (name is int)
			{
				result = (System.Convert.ToInt32(((int)name)) == 0);
			}
			else
			{
				if (name.IsNumber())
				{
					double x = System.Convert.ToDouble(name);
					// Check that number is positive 0
					result = (x == 0.0 && 1.0 / x > 0);
				}
				else
				{
					result = ScriptRuntime.ToString(name).Equals("0");
				}
			}
			return result;
		}

		internal override object ValueOf()
		{
			return this;
		}

		//
		//    Selection of children
		//
		internal override XMLList Comments()
		{
			XMLList rv = NewXMLList();
			this.node.AddMatchingChildren(rv, Rhino.Xmlimpl.XmlNode.Filter.COMMENT);
			return rv;
		}

		internal override XMLList Text()
		{
			XMLList rv = NewXMLList();
			this.node.AddMatchingChildren(rv, Rhino.Xmlimpl.XmlNode.Filter.TEXT);
			return rv;
		}

		internal override XMLList ProcessingInstructions(XMLName xmlName)
		{
			XMLList rv = NewXMLList();
			this.node.AddMatchingChildren(rv, Rhino.Xmlimpl.XmlNode.Filter.PROCESSING_INSTRUCTION(xmlName));
			return rv;
		}

		//
		//    Methods relating to modification of child nodes
		//
		//    We create all the nodes we are inserting before doing the insert to
		//    avoid nasty cycles caused by mutability of these objects.  For example,
		//    what if the toString() method of value modifies the XML object we were
		//    going to insert into?  insertAfter might get confused about where to
		//    insert.  This actually came up with SpiderMonkey, leading to a (very)
		//    long discussion.  See bug #354145.
		private Rhino.Xmlimpl.XmlNode[] GetNodesForInsert(object value)
		{
			if (value is Rhino.Xmlimpl.XML)
			{
				return new Rhino.Xmlimpl.XmlNode[] { ((Rhino.Xmlimpl.XML)value).node };
			}
			else
			{
				if (value is XMLList)
				{
					XMLList list = (XMLList)value;
					Rhino.Xmlimpl.XmlNode[] rv = new Rhino.Xmlimpl.XmlNode[list.Length()];
					for (int i = 0; i < list.Length(); i++)
					{
						rv[i] = list.Item(i).node;
					}
					return rv;
				}
				else
				{
					return new Rhino.Xmlimpl.XmlNode[] { Rhino.Xmlimpl.XmlNode.CreateText(GetProcessor(), ScriptRuntime.ToString(value)) };
				}
			}
		}

		internal virtual Rhino.Xmlimpl.XML Replace(int index, object xml)
		{
			XMLList xlChildToReplace = Child(index);
			if (xlChildToReplace.Length() > 0)
			{
				// One exists an that index
				Rhino.Xmlimpl.XML childToReplace = xlChildToReplace.Item(0);
				InsertChildAfter(childToReplace, xml);
				RemoveChild(index);
			}
			return this;
		}

		internal virtual Rhino.Xmlimpl.XML PrependChild(object xml)
		{
			if (this.node.IsParentType())
			{
				this.node.InsertChildrenAt(0, GetNodesForInsert(xml));
			}
			return this;
		}

		internal virtual Rhino.Xmlimpl.XML AppendChild(object xml)
		{
			if (this.node.IsParentType())
			{
				Rhino.Xmlimpl.XmlNode[] nodes = GetNodesForInsert(xml);
				this.node.InsertChildrenAt(this.node.GetChildCount(), nodes);
			}
			return this;
		}

		private int GetChildIndexOf(Rhino.Xmlimpl.XML child)
		{
			for (int i = 0; i < this.node.GetChildCount(); i++)
			{
				if (this.node.GetChild(i).IsSameNode(child.node))
				{
					return i;
				}
			}
			return -1;
		}

		internal virtual Rhino.Xmlimpl.XML InsertChildBefore(Rhino.Xmlimpl.XML child, object xml)
		{
			if (child == null)
			{
				// Spec says inserting before nothing is the same as appending
				AppendChild(xml);
			}
			else
			{
				Rhino.Xmlimpl.XmlNode[] toInsert = GetNodesForInsert(xml);
				int index = GetChildIndexOf(child);
				if (index != -1)
				{
					this.node.InsertChildrenAt(index, toInsert);
				}
			}
			return this;
		}

		internal virtual Rhino.Xmlimpl.XML InsertChildAfter(Rhino.Xmlimpl.XML child, object xml)
		{
			if (child == null)
			{
				// Spec says inserting after nothing is the same as prepending
				PrependChild(xml);
			}
			else
			{
				Rhino.Xmlimpl.XmlNode[] toInsert = GetNodesForInsert(xml);
				int index = GetChildIndexOf(child);
				if (index != -1)
				{
					this.node.InsertChildrenAt(index + 1, toInsert);
				}
			}
			return this;
		}

		internal virtual Rhino.Xmlimpl.XML SetChildren(object xml)
		{
			//    TODO    Have not carefully considered the spec but it seems to call for this
			if (!IsElement())
			{
				return this;
			}
			while (this.node.GetChildCount() > 0)
			{
				this.node.RemoveChild(0);
			}
			Rhino.Xmlimpl.XmlNode[] toInsert = GetNodesForInsert(xml);
			// append new children
			this.node.InsertChildrenAt(0, toInsert);
			return this;
		}

		//
		//    Name and namespace-related methods
		//
		private void AddInScopeNamespace(Rhino.Xmlimpl.Namespace ns)
		{
			if (!IsElement())
			{
				return;
			}
			//    See ECMA357 9.1.1.13
			//    in this implementation null prefix means ECMA undefined
			if (ns.Prefix() != null)
			{
				if (ns.Prefix().Length == 0 && ns.Uri().Length == 0)
				{
					return;
				}
				if (node.GetQname().GetNamespace().GetPrefix().Equals(ns.Prefix()))
				{
					node.InvalidateNamespacePrefix();
				}
				node.DeclareNamespace(ns.Prefix(), ns.Uri());
			}
			else
			{
				return;
			}
		}

		internal virtual Rhino.Xmlimpl.Namespace[] InScopeNamespaces()
		{
			Rhino.Xmlimpl.XmlNode.Namespace[] inScope = this.node.GetInScopeNamespaces();
			return CreateNamespaces(inScope);
		}

		private Rhino.Xmlimpl.XmlNode.Namespace Adapt(Rhino.Xmlimpl.Namespace ns)
		{
			if (ns.Prefix() == null)
			{
				return Rhino.Xmlimpl.XmlNode.Namespace.Create(ns.Uri());
			}
			else
			{
				return Rhino.Xmlimpl.XmlNode.Namespace.Create(ns.Prefix(), ns.Uri());
			}
		}

		internal virtual Rhino.Xmlimpl.XML RemoveNamespace(Rhino.Xmlimpl.Namespace ns)
		{
			if (!IsElement())
			{
				return this;
			}
			this.node.RemoveNamespace(Adapt(ns));
			return this;
		}

		internal virtual Rhino.Xmlimpl.XML AddNamespace(Rhino.Xmlimpl.Namespace ns)
		{
			AddInScopeNamespace(ns);
			return this;
		}

		internal virtual QName Name()
		{
			if (IsText() || IsComment())
			{
				return null;
			}
			if (IsProcessingInstruction())
			{
				return NewQName(string.Empty, this.node.GetQname().GetLocalName(), null);
			}
			return NewQName(node.GetQname());
		}

		internal virtual Rhino.Xmlimpl.Namespace[] NamespaceDeclarations()
		{
			Rhino.Xmlimpl.XmlNode.Namespace[] declarations = node.GetNamespaceDeclarations();
			return CreateNamespaces(declarations);
		}

		internal virtual Rhino.Xmlimpl.Namespace Namespace(string prefix)
		{
			if (prefix == null)
			{
				return CreateNamespace(this.node.GetNamespaceDeclaration());
			}
			else
			{
				return CreateNamespace(this.node.GetNamespaceDeclaration(prefix));
			}
		}

		internal virtual string LocalName()
		{
			if (Name() == null)
			{
				return null;
			}
			return Name().LocalName();
		}

		internal virtual void SetLocalName(string localName)
		{
			//    ECMA357 13.4.4.34
			if (IsText() || IsComment())
			{
				return;
			}
			this.node.SetLocalName(localName);
		}

		internal virtual void SetName(QName name)
		{
			//    See ECMA357 13.4.4.35
			if (IsText() || IsComment())
			{
				return;
			}
			if (IsProcessingInstruction())
			{
				//    Spec says set the name URI to empty string and then set the [[Name]] property, but I understand this to do the same
				//    thing, unless we allow colons in processing instruction targets, which I think we do not.
				this.node.SetLocalName(name.LocalName());
				return;
			}
			node.RenameNode(name.GetDelegate());
		}

		internal virtual void SetNamespace(Rhino.Xmlimpl.Namespace ns)
		{
			//    See ECMA357 13.4.4.36
			if (IsText() || IsComment() || IsProcessingInstruction())
			{
				return;
			}
			SetName(NewQName(ns.Uri(), LocalName(), ns.Prefix()));
		}

		internal string EcmaClass()
		{
			//    See ECMA357 9.1
			//    TODO    See ECMA357 9.1.1 last paragraph for what defaults should be
			if (node.IsTextType())
			{
				return "text";
			}
			else
			{
				if (node.IsAttributeType())
				{
					return "attribute";
				}
				else
				{
					if (node.IsCommentType())
					{
						return "comment";
					}
					else
					{
						if (node.IsProcessingInstructionType())
						{
							return "processing-instruction";
						}
						else
						{
							if (node.IsElementType())
							{
								return "element";
							}
							else
							{
								throw new Exception("Unrecognized type: " + node);
							}
						}
					}
				}
			}
		}

		public override string GetClassName()
		{
			//    TODO:    This appears to confuse the interpreter if we use the "real" class property from ECMA.  Otherwise this code
			//    would be:
			//    return ecmaClass();
			return "XML";
		}

		private string EcmaValue()
		{
			return node.EcmaValue();
		}

		private string EcmaToString()
		{
			//    See ECMA357 10.1.1
			if (IsAttribute() || IsText())
			{
				return EcmaValue();
			}
			if (this.HasSimpleContent())
			{
				StringBuilder rv = new StringBuilder();
				for (int i = 0; i < this.node.GetChildCount(); i++)
				{
					Rhino.Xmlimpl.XmlNode child = this.node.GetChild(i);
					if (!child.IsProcessingInstructionType() && !child.IsCommentType())
					{
						// TODO: Probably inefficient; taking clean non-optimized
						// solution for now
						Rhino.Xmlimpl.XML x = new Rhino.Xmlimpl.XML(GetLib(), GetParentScope(), (XMLObject)GetPrototype(), child);
						rv.Append(x.ToString());
					}
				}
				return rv.ToString();
			}
			return ToXMLString();
		}

		public override string ToString()
		{
			return EcmaToString();
		}

		internal override string ToSource(int indent)
		{
			return ToXMLString();
		}

		internal override string ToXMLString()
		{
			return this.node.EcmaToXMLString(GetProcessor());
		}

		internal bool IsAttribute()
		{
			return node.IsAttributeType();
		}

		internal bool IsComment()
		{
			return node.IsCommentType();
		}

		internal bool IsText()
		{
			return node.IsTextType();
		}

		internal bool IsElement()
		{
			return node.IsElementType();
		}

		internal bool IsProcessingInstruction()
		{
			return node.IsProcessingInstructionType();
		}

		//    Support experimental Java interface
		internal virtual System.Xml.XmlNode ToDomNode()
		{
			return node.ToDomNode();
		}
	}
}
