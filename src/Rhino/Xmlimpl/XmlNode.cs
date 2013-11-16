/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Xml;
using Org.W3c.Dom;
using Rhino;
using Rhino.Xmlimpl;
using Sharpen;

namespace Rhino.Xmlimpl
{
	[System.Serializable]
	internal class XmlNode
	{
		private const string XML_NAMESPACES_NAMESPACE_URI = "http://www.w3.org/2000/xmlns/";

		private static readonly string USER_DATA_XMLNODE_KEY = typeof(Rhino.Xmlimpl.XmlNode).FullName;

		private const bool DOM_LEVEL_3 = true;

		private static Rhino.Xmlimpl.XmlNode GetUserData(System.Xml.XmlNode node)
		{
			return (Rhino.Xmlimpl.XmlNode)node.GetUserData(USER_DATA_XMLNODE_KEY);
			return null;
		}

		private static void SetUserData(System.Xml.XmlNode node, Rhino.Xmlimpl.XmlNode wrap)
		{
			node.SetUserData(USER_DATA_XMLNODE_KEY, wrap, wrap.events);
		}

		private static Rhino.Xmlimpl.XmlNode CreateImpl(System.Xml.XmlNode node)
		{
			if (node is XmlDocument)
			{
				throw new ArgumentException();
			}
			Rhino.Xmlimpl.XmlNode rv = null;
			if (GetUserData(node) == null)
			{
				rv = new Rhino.Xmlimpl.XmlNode();
				rv.dom = node;
				SetUserData(node, rv);
			}
			else
			{
				rv = GetUserData(node);
			}
			return rv;
		}

		internal static Rhino.Xmlimpl.XmlNode NewElementWithText(XmlProcessor processor, Rhino.Xmlimpl.XmlNode reference, Rhino.Xmlimpl.XmlNode.QName qname, string value)
		{
			if (reference is XmlDocument)
			{
				throw new ArgumentException("Cannot use Document node as reference");
			}
			XmlDocument document = null;
			if (reference != null)
			{
				document = reference.dom.OwnerDocument;
			}
			else
			{
				document = processor.NewDocument();
			}
			System.Xml.XmlNode referenceDom = (reference != null) ? reference.dom : null;
			Rhino.Xmlimpl.XmlNode.Namespace ns = qname.GetNamespace();
			XmlElement e = (ns == null || ns.GetUri().Length == 0) ? document.CreateElementNS(null, qname.GetLocalName()) : document.CreateElementNS(ns.GetUri(), qname.Qualify(referenceDom));
			if (value != null)
			{
				e.AppendChild(document.CreateTextNode(value));
			}
			return Rhino.Xmlimpl.XmlNode.CreateImpl(e);
		}

		internal static Rhino.Xmlimpl.XmlNode CreateText(XmlProcessor processor, string value)
		{
			return CreateImpl(processor.NewDocument().CreateTextNode(value));
		}

		internal static Rhino.Xmlimpl.XmlNode CreateElementFromNode(System.Xml.XmlNode node)
		{
			if (node is XmlDocument)
			{
				node = ((XmlDocument)node).DocumentElement;
			}
			return CreateImpl(node);
		}

		/// <exception cref="Org.Xml.Sax.SAXException"></exception>
		internal static Rhino.Xmlimpl.XmlNode CreateElement(XmlProcessor processor, string namespaceUri, string xml)
		{
			return CreateImpl(processor.ToXml(namespaceUri, xml));
		}

		internal static Rhino.Xmlimpl.XmlNode CreateEmpty(XmlProcessor processor)
		{
			return CreateText(processor, string.Empty);
		}

		private static Rhino.Xmlimpl.XmlNode Copy(Rhino.Xmlimpl.XmlNode other)
		{
			return CreateImpl(other.dom.CloneNode(true));
		}

		private const long serialVersionUID = 1L;

		private UserDataHandler events = new Rhino.Xmlimpl.XmlNode.XmlNodeUserDataHandler();

		private System.Xml.XmlNode dom;

		private XML xml;

		private XmlNode()
		{
		}

		internal virtual string Debug()
		{
			XmlProcessor raw = new XmlProcessor();
			raw.SetIgnoreComments(false);
			raw.SetIgnoreProcessingInstructions(false);
			raw.SetIgnoreWhitespace(false);
			raw.SetPrettyPrinting(false);
			return raw.EcmaToXmlString(this.dom);
		}

		public override string ToString()
		{
			return "XmlNode: type=" + dom.NodeType + " dom=" + dom.ToString();
		}

		internal virtual XML GetXml()
		{
			return xml;
		}

		internal virtual void SetXml(XML xml)
		{
			this.xml = xml;
		}

		internal virtual int GetChildCount()
		{
			return this.dom.ChildNodes.Count;
		}

		internal virtual Rhino.Xmlimpl.XmlNode Parent()
		{
			System.Xml.XmlNode domParent = dom.ParentNode;
			if (domParent is XmlDocument)
			{
				return null;
			}
			if (domParent == null)
			{
				return null;
			}
			return CreateImpl(domParent);
		}

		internal virtual int GetChildIndex()
		{
			if (this.IsAttributeType())
			{
				return -1;
			}
			if (Parent() == null)
			{
				return -1;
			}
			XmlNodeList siblings = this.dom.ParentNode.ChildNodes;
			for (int i = 0; i < siblings.Count; i++)
			{
				if (siblings.Item(i) == dom)
				{
					return i;
				}
			}
			//    Either the parent is -1 or one of the this node's parent's children is this node.
			throw new Exception("Unreachable.");
		}

		internal virtual void RemoveChild(int index)
		{
			this.dom.RemoveChild(this.dom.ChildNodes.Item(index));
		}

		internal virtual string ToXmlString(XmlProcessor processor)
		{
			return processor.EcmaToXmlString(this.dom);
		}

		internal virtual string EcmaValue()
		{
			//    TODO    See ECMA 357 Section 9.1
			if (IsTextType())
			{
				return ((XmlText)dom).GetData();
			}
			else
			{
				if (IsAttributeType())
				{
					return ((Attr)dom).GetValue();
				}
				else
				{
					if (IsProcessingInstructionType())
					{
						return ((ProcessingInstruction)dom).GetData();
					}
					else
					{
						if (IsCommentType())
						{
							return ((XmlComment)dom).GetNodeValue();
						}
						else
						{
							if (IsElementType())
							{
								throw new Exception("Unimplemented ecmaValue() for elements.");
							}
							else
							{
								throw new Exception("Unimplemented for node " + dom);
							}
						}
					}
				}
			}
		}

		internal virtual void DeleteMe()
		{
			if (dom is Attr)
			{
				Attr attr = (Attr)this.dom;
				attr.GetOwnerElement().Attributes.RemoveNamedItemNS(attr.NamespaceURI, attr.LocalName);
			}
			else
			{
				if (this.dom.ParentNode != null)
				{
					this.dom.ParentNode.RemoveChild(this.dom);
				}
			}
		}

		//    This case can be exercised at least when executing the regression
		//    tests under https://bugzilla.mozilla.org/show_bug.cgi?id=354145
		internal virtual void Normalize()
		{
			this.dom.Normalize();
		}

		internal virtual void InsertChildAt(int index, Rhino.Xmlimpl.XmlNode node)
		{
			System.Xml.XmlNode parent = this.dom;
			System.Xml.XmlNode child = parent.OwnerDocument.ImportNode(node.dom, true);
			if (parent.ChildNodes.Count < index)
			{
				//    TODO    Check ECMA for what happens here
				throw new ArgumentException("index=" + index + " length=" + parent.ChildNodes.Count);
			}
			if (parent.ChildNodes.Count == index)
			{
				parent.AppendChild(child);
			}
			else
			{
				parent.InsertBefore(child, parent.ChildNodes.Item(index));
			}
		}

		internal virtual void InsertChildrenAt(int index, Rhino.Xmlimpl.XmlNode[] nodes)
		{
			for (int i = 0; i < nodes.Length; i++)
			{
				InsertChildAt(index + i, nodes[i]);
			}
		}

		internal virtual Rhino.Xmlimpl.XmlNode GetChild(int index)
		{
			System.Xml.XmlNode child = dom.ChildNodes.Item(index);
			return CreateImpl(child);
		}

		//    Helper method for XML.hasSimpleContent()
		internal virtual bool HasChildElement()
		{
			XmlNodeList nodes = this.dom.ChildNodes;
			for (int i = 0; i < nodes.Count; i++)
			{
				if (nodes.Item(i).NodeType == NodeConstants.ELEMENT_NODE)
				{
					return true;
				}
			}
			return false;
		}

		internal virtual bool IsSameNode(Rhino.Xmlimpl.XmlNode other)
		{
			//    TODO    May need to be changed if we allow XmlNode to refer to several Node objects
			return this.dom == other.dom;
		}

		private string ToUri(string ns)
		{
			return (ns == null) ? string.Empty : ns;
		}

		private void AddNamespaces(Rhino.Xmlimpl.XmlNode.Namespaces rv, XmlElement element)
		{
			if (element == null)
			{
				throw new Exception("element must not be null");
			}
			string myDefaultNamespace = ToUri(element.LookupNamespaceURI(null));
			string parentDefaultNamespace = string.Empty;
			if (element.ParentNode != null)
			{
				parentDefaultNamespace = ToUri(element.ParentNode.LookupNamespaceURI(null));
			}
			if (!myDefaultNamespace.Equals(parentDefaultNamespace) || !(element.ParentNode is XmlElement))
			{
				rv.Declare(Rhino.Xmlimpl.XmlNode.Namespace.Create(string.Empty, myDefaultNamespace));
			}
			XmlNamedNodeMap attributes = element.Attributes;
			for (int i = 0; i < attributes.Count; i++)
			{
				Attr attr = (Attr)attributes.Item(i);
				if (attr.Prefix != null && attr.Prefix.Equals("xmlns"))
				{
					rv.Declare(Rhino.Xmlimpl.XmlNode.Namespace.Create(attr.LocalName, attr.GetValue()));
				}
			}
		}

		private Rhino.Xmlimpl.XmlNode.Namespaces GetAllNamespaces()
		{
			Rhino.Xmlimpl.XmlNode.Namespaces rv = new Rhino.Xmlimpl.XmlNode.Namespaces();
			System.Xml.XmlNode target = this.dom;
			if (target is Attr)
			{
				target = ((Attr)target).GetOwnerElement();
			}
			while (target != null)
			{
				if (target is XmlElement)
				{
					AddNamespaces(rv, (XmlElement)target);
				}
				target = target.ParentNode;
			}
			//    Fallback in case no namespace was declared
			rv.Declare(Rhino.Xmlimpl.XmlNode.Namespace.Create(string.Empty, string.Empty));
			return rv;
		}

		internal virtual Rhino.Xmlimpl.XmlNode.Namespace[] GetInScopeNamespaces()
		{
			Rhino.Xmlimpl.XmlNode.Namespaces rv = GetAllNamespaces();
			return rv.GetNamespaces();
		}

		internal virtual Rhino.Xmlimpl.XmlNode.Namespace[] GetNamespaceDeclarations()
		{
			//    ECMA357 13.4.4.24
			if (this.dom is XmlElement)
			{
				Rhino.Xmlimpl.XmlNode.Namespaces rv = new Rhino.Xmlimpl.XmlNode.Namespaces();
				AddNamespaces(rv, (XmlElement)this.dom);
				return rv.GetNamespaces();
			}
			else
			{
				return new Rhino.Xmlimpl.XmlNode.Namespace[0];
			}
		}

		internal virtual Rhino.Xmlimpl.XmlNode.Namespace GetNamespaceDeclaration(string prefix)
		{
			if (prefix.Equals(string.Empty) && dom is Attr)
			{
				//    Default namespaces do not apply to attributes; see XML Namespaces section 5.2
				return Rhino.Xmlimpl.XmlNode.Namespace.Create(string.Empty, string.Empty);
			}
			Rhino.Xmlimpl.XmlNode.Namespaces rv = GetAllNamespaces();
			return rv.GetNamespace(prefix);
		}

		internal virtual Rhino.Xmlimpl.XmlNode.Namespace GetNamespaceDeclaration()
		{
			if (dom.Prefix == null)
			{
				return GetNamespaceDeclaration(string.Empty);
			}
			return GetNamespaceDeclaration(dom.Prefix);
		}

		[System.Serializable]
		internal class XmlNodeUserDataHandler : UserDataHandler
		{
			private const long serialVersionUID = 4666895518900769588L;

			public virtual void Handle(short operation, string key, object data, System.Xml.XmlNode src, System.Xml.XmlNode dest)
			{
			}
		}

		private class Namespaces
		{
			private IDictionary<string, string> map = new Dictionary<string, string>();

			private IDictionary<string, string> uriToPrefix = new Dictionary<string, string>();

			internal Namespaces()
			{
			}

			internal virtual void Declare(Rhino.Xmlimpl.XmlNode.Namespace n)
			{
				if (map.Get(n.prefix) == null)
				{
					map.Put(n.prefix, n.uri);
				}
				//    TODO    I think this is analogous to the other way, but have not really thought it through ... should local scope
				//            matter more than outer scope?
				if (uriToPrefix.Get(n.uri) == null)
				{
					uriToPrefix.Put(n.uri, n.prefix);
				}
			}

			internal virtual Rhino.Xmlimpl.XmlNode.Namespace GetNamespaceByUri(string uri)
			{
				if (uriToPrefix.Get(uri) == null)
				{
					return null;
				}
				return Rhino.Xmlimpl.XmlNode.Namespace.Create(uri, uriToPrefix.Get(uri));
			}

			internal virtual Rhino.Xmlimpl.XmlNode.Namespace GetNamespace(string prefix)
			{
				if (map.Get(prefix) == null)
				{
					return null;
				}
				return Rhino.Xmlimpl.XmlNode.Namespace.Create(prefix, map.Get(prefix));
			}

			internal virtual Rhino.Xmlimpl.XmlNode.Namespace[] GetNamespaces()
			{
				AList<Rhino.Xmlimpl.XmlNode.Namespace> rv = new AList<Rhino.Xmlimpl.XmlNode.Namespace>();
				foreach (string prefix in map.Keys)
				{
					string uri = map.Get(prefix);
					Rhino.Xmlimpl.XmlNode.Namespace n = Rhino.Xmlimpl.XmlNode.Namespace.Create(prefix, uri);
					if (!n.IsEmpty())
					{
						rv.AddItem(n);
					}
				}
				return Sharpen.Collections.ToArray(rv, new Rhino.Xmlimpl.XmlNode.Namespace[rv.Count]);
			}
		}

		internal Rhino.Xmlimpl.XmlNode Copy()
		{
			return Copy(this);
		}

		//    Returns whether this node is capable of being a parent
		internal bool IsParentType()
		{
			return IsElementType();
		}

		internal bool IsTextType()
		{
			return dom.NodeType == NodeConstants.TEXT_NODE || dom.NodeType == NodeConstants.CDATA_SECTION_NODE;
		}

		internal bool IsAttributeType()
		{
			return dom.NodeType == NodeConstants.ATTRIBUTE_NODE;
		}

		internal bool IsProcessingInstructionType()
		{
			return dom.NodeType == NodeConstants.PROCESSING_INSTRUCTION_NODE;
		}

		internal bool IsCommentType()
		{
			return dom.NodeType == NodeConstants.COMMENT_NODE;
		}

		internal bool IsElementType()
		{
			return dom.NodeType == NodeConstants.ELEMENT_NODE;
		}

		internal void RenameNode(Rhino.Xmlimpl.XmlNode.QName qname)
		{
			this.dom = dom.OwnerDocument.RenameNode(dom, qname.GetNamespace().GetUri(), qname.Qualify(dom));
		}

		internal virtual void InvalidateNamespacePrefix()
		{
			if (!(dom is XmlElement))
			{
				throw new InvalidOperationException();
			}
			string prefix = this.dom.Prefix;
			Rhino.Xmlimpl.XmlNode.QName after = Rhino.Xmlimpl.XmlNode.QName.Create(this.dom.NamespaceURI, this.dom.LocalName, null);
			RenameNode(after);
			XmlNamedNodeMap attrs = this.dom.Attributes;
			for (int i = 0; i < attrs.Count; i++)
			{
				if (attrs.Item(i).Prefix.Equals(prefix))
				{
					CreateImpl(attrs.Item(i)).RenameNode(Rhino.Xmlimpl.XmlNode.QName.Create(attrs.Item(i).NamespaceURI, attrs.Item(i).LocalName, null));
				}
			}
		}

		private void DeclareNamespace(XmlElement e, string prefix, string uri)
		{
			if (prefix.Length > 0)
			{
				e.SetAttributeNS(XML_NAMESPACES_NAMESPACE_URI, "xmlns:" + prefix, uri);
			}
			else
			{
				e.SetAttribute("xmlns", uri);
			}
		}

		internal virtual void DeclareNamespace(string prefix, string uri)
		{
			if (!(dom is XmlElement))
			{
				throw new InvalidOperationException();
			}
			if (dom.LookupNamespaceURI(uri) != null && dom.LookupNamespaceURI(uri).Equals(prefix))
			{
			}
			else
			{
				//    do nothing
				XmlElement e = (XmlElement)dom;
				DeclareNamespace(e, prefix, uri);
			}
		}

		private Rhino.Xmlimpl.XmlNode.Namespace GetDefaultNamespace()
		{
			string prefix = string.Empty;
			string uri = (dom.LookupNamespaceURI(null) == null) ? string.Empty : dom.LookupNamespaceURI(null);
			return Rhino.Xmlimpl.XmlNode.Namespace.Create(prefix, uri);
		}

		private string GetExistingPrefixFor(Rhino.Xmlimpl.XmlNode.Namespace @namespace)
		{
			if (GetDefaultNamespace().GetUri().Equals(@namespace.GetUri()))
			{
				return string.Empty;
			}
			return dom.LookupPrefix(@namespace.GetUri());
		}

		private Rhino.Xmlimpl.XmlNode.Namespace GetNodeNamespace()
		{
			string uri = dom.NamespaceURI;
			string prefix = dom.Prefix;
			if (uri == null)
			{
				uri = string.Empty;
			}
			if (prefix == null)
			{
				prefix = string.Empty;
			}
			return Rhino.Xmlimpl.XmlNode.Namespace.Create(prefix, uri);
		}

		internal virtual Rhino.Xmlimpl.XmlNode.Namespace GetNamespace()
		{
			return GetNodeNamespace();
		}

		internal virtual void RemoveNamespace(Rhino.Xmlimpl.XmlNode.Namespace @namespace)
		{
			Rhino.Xmlimpl.XmlNode.Namespace current = GetNodeNamespace();
			//    Do not remove in-use namespace
			if (@namespace.Is(current))
			{
				return;
			}
			XmlNamedNodeMap attrs = this.dom.Attributes;
			for (int i = 0; i < attrs.Count; i++)
			{
				Rhino.Xmlimpl.XmlNode attr = Rhino.Xmlimpl.XmlNode.CreateImpl(attrs.Item(i));
				if (@namespace.Is(attr.GetNodeNamespace()))
				{
					return;
				}
			}
			//    TODO    I must confess I am not sure I understand the spec fully.  See ECMA357 13.4.4.31
			string existingPrefix = GetExistingPrefixFor(@namespace);
			if (existingPrefix != null)
			{
				if (@namespace.IsUnspecifiedPrefix())
				{
					//    we should remove any namespace with this URI from scope; we do this by declaring a namespace with the same
					//    prefix as the existing prefix and setting its URI to the default namespace
					DeclareNamespace(existingPrefix, GetDefaultNamespace().GetUri());
				}
				else
				{
					if (existingPrefix.Equals(@namespace.GetPrefix()))
					{
						DeclareNamespace(existingPrefix, GetDefaultNamespace().GetUri());
					}
				}
			}
		}

		//    the argument namespace is not declared in this scope, so do nothing.
		private void SetProcessingInstructionName(string localName)
		{
			ProcessingInstruction pi = (ProcessingInstruction)this.dom;
			//    We cannot set the node name; Document.renameNode() only supports elements and attributes.  So we replace it
			pi.ParentNode.ReplaceChild(pi, pi.OwnerDocument.CreateProcessingInstruction(localName, pi.GetData()));
		}

		internal void SetLocalName(string localName)
		{
			if (dom is ProcessingInstruction)
			{
				SetProcessingInstructionName(localName);
			}
			else
			{
				string prefix = dom.Prefix;
				if (prefix == null)
				{
					prefix = string.Empty;
				}
				this.dom = dom.OwnerDocument.RenameNode(dom, dom.NamespaceURI, Rhino.Xmlimpl.XmlNode.QName.Qualify(prefix, localName));
			}
		}

		internal Rhino.Xmlimpl.XmlNode.QName GetQname()
		{
			string uri = (dom.NamespaceURI) == null ? string.Empty : dom.NamespaceURI;
			string prefix = (dom.Prefix == null) ? string.Empty : dom.Prefix;
			return Rhino.Xmlimpl.XmlNode.QName.Create(uri, dom.LocalName, prefix);
		}

		internal virtual void AddMatchingChildren(XMLList result, Rhino.Xmlimpl.XmlNode.Filter filter)
		{
			System.Xml.XmlNode node = this.dom;
			XmlNodeList children = node.ChildNodes;
			for (int i = 0; i < children.Count; i++)
			{
				System.Xml.XmlNode childnode = children.Item(i);
				Rhino.Xmlimpl.XmlNode child = Rhino.Xmlimpl.XmlNode.CreateImpl(childnode);
				if (filter.Accept(childnode))
				{
					result.AddToList(child);
				}
			}
		}

		internal virtual Rhino.Xmlimpl.XmlNode[] GetMatchingChildren(Rhino.Xmlimpl.XmlNode.Filter filter)
		{
			AList<Rhino.Xmlimpl.XmlNode> rv = new AList<Rhino.Xmlimpl.XmlNode>();
			XmlNodeList nodes = this.dom.ChildNodes;
			for (int i = 0; i < nodes.Count; i++)
			{
				System.Xml.XmlNode node = nodes.Item(i);
				if (filter.Accept(node))
				{
					rv.AddItem(CreateImpl(node));
				}
			}
			return Sharpen.Collections.ToArray(rv, new Rhino.Xmlimpl.XmlNode[rv.Count]);
		}

		internal virtual Rhino.Xmlimpl.XmlNode[] GetAttributes()
		{
			XmlNamedNodeMap attrs = this.dom.Attributes;
			//    TODO    Or could make callers handle null?
			if (attrs == null)
			{
				throw new InvalidOperationException("Must be element.");
			}
			Rhino.Xmlimpl.XmlNode[] rv = new Rhino.Xmlimpl.XmlNode[attrs.Count];
			for (int i = 0; i < attrs.Count; i++)
			{
				rv[i] = CreateImpl(attrs.Item(i));
			}
			return rv;
		}

		internal virtual string GetAttributeValue()
		{
			return ((Attr)dom).GetValue();
		}

		internal virtual void SetAttribute(Rhino.Xmlimpl.XmlNode.QName name, string value)
		{
			if (!(dom is XmlElement))
			{
				throw new InvalidOperationException("Can only set attribute on elements.");
			}
			name.SetAttribute((XmlElement)dom, value);
		}

		internal virtual void ReplaceWith(Rhino.Xmlimpl.XmlNode other)
		{
			System.Xml.XmlNode replacement = other.dom;
			if (replacement.OwnerDocument != this.dom.OwnerDocument)
			{
				replacement = this.dom.OwnerDocument.ImportNode(replacement, true);
			}
			this.dom.ParentNode.ReplaceChild(replacement, this.dom);
		}

		internal virtual string EcmaToXMLString(XmlProcessor processor)
		{
			if (this.IsElementType())
			{
				XmlElement copy = (XmlElement)this.dom.CloneNode(true);
				Rhino.Xmlimpl.XmlNode.Namespace[] inScope = this.GetInScopeNamespaces();
				for (int i = 0; i < inScope.Length; i++)
				{
					DeclareNamespace(copy, inScope[i].GetPrefix(), inScope[i].GetUri());
				}
				return processor.EcmaToXmlString(copy);
			}
			else
			{
				return processor.EcmaToXmlString(dom);
			}
		}

		[System.Serializable]
		internal class Namespace
		{
			/// <summary>Serial version id for Namespace with fields prefix and uri</summary>
			private const long serialVersionUID = 4073904386884677090L;

			internal static Rhino.Xmlimpl.XmlNode.Namespace Create(string prefix, string uri)
			{
				if (prefix == null)
				{
					throw new ArgumentException("Empty string represents default namespace prefix");
				}
				if (uri == null)
				{
					throw new ArgumentException("Namespace may not lack a URI");
				}
				Rhino.Xmlimpl.XmlNode.Namespace rv = new Rhino.Xmlimpl.XmlNode.Namespace();
				rv.prefix = prefix;
				rv.uri = uri;
				return rv;
			}

			internal static Rhino.Xmlimpl.XmlNode.Namespace Create(string uri)
			{
				Rhino.Xmlimpl.XmlNode.Namespace rv = new Rhino.Xmlimpl.XmlNode.Namespace();
				rv.uri = uri;
				// Avoid null prefix for "" namespace
				if (uri == null || uri.Length == 0)
				{
					rv.prefix = string.Empty;
				}
				return rv;
			}

			internal static readonly Rhino.Xmlimpl.XmlNode.Namespace GLOBAL = Create(string.Empty, string.Empty);

			private string prefix;

			private string uri;

			private Namespace()
			{
			}

			public override string ToString()
			{
				if (prefix == null)
				{
					return "XmlNode.Namespace [" + uri + "]";
				}
				return "XmlNode.Namespace [" + prefix + "{" + uri + "}]";
			}

			internal virtual bool IsUnspecifiedPrefix()
			{
				return prefix == null;
			}

			internal virtual bool Is(Rhino.Xmlimpl.XmlNode.Namespace other)
			{
				return this.prefix != null && other.prefix != null && this.prefix.Equals(other.prefix) && this.uri.Equals(other.uri);
			}

			internal virtual bool IsEmpty()
			{
				return prefix != null && prefix.Equals(string.Empty) && uri.Equals(string.Empty);
			}

			internal virtual bool IsDefault()
			{
				return prefix != null && prefix.Equals(string.Empty);
			}

			internal virtual bool IsGlobal()
			{
				return uri != null && uri.Equals(string.Empty);
			}

			//    Called by QName
			//    TODO    Move functionality from QName lookupPrefix to here
			private void SetPrefix(string prefix)
			{
				if (prefix == null)
				{
					throw new ArgumentException();
				}
				this.prefix = prefix;
			}

			internal virtual string GetPrefix()
			{
				return prefix;
			}

			internal virtual string GetUri()
			{
				return uri;
			}
		}

		[System.Serializable]
		internal class QName
		{
			private const long serialVersionUID = -6587069811691451077L;

			//    TODO    Where is this class used?  No longer using it in QName implementation
			internal static Rhino.Xmlimpl.XmlNode.QName Create(Rhino.Xmlimpl.XmlNode.Namespace @namespace, string localName)
			{
				//    A null namespace indicates a wild-card match for any namespace
				//    A null localName indicates "*" from the point of view of ECMA357
				if (localName != null && localName.Equals("*"))
				{
					throw new Exception("* is not valid localName");
				}
				Rhino.Xmlimpl.XmlNode.QName rv = new Rhino.Xmlimpl.XmlNode.QName();
				rv.@namespace = @namespace;
				rv.localName = localName;
				return rv;
			}

			[System.ObsoleteAttribute(@"")]
			internal static Rhino.Xmlimpl.XmlNode.QName Create(string uri, string localName, string prefix)
			{
				return Create(Rhino.Xmlimpl.XmlNode.Namespace.Create(prefix, uri), localName);
			}

			internal static string Qualify(string prefix, string localName)
			{
				if (prefix == null)
				{
					throw new ArgumentException("prefix must not be null");
				}
				if (prefix.Length > 0)
				{
					return prefix + ":" + localName;
				}
				return localName;
			}

			private Rhino.Xmlimpl.XmlNode.Namespace @namespace;

			private string localName;

			private QName()
			{
			}

			public override string ToString()
			{
				return "XmlNode.QName [" + localName + "," + @namespace + "]";
			}

			private bool Equals(string one, string two)
			{
				if (one == null && two == null)
				{
					return true;
				}
				if (one == null || two == null)
				{
					return false;
				}
				return one.Equals(two);
			}

			private bool NamespacesEqual(Rhino.Xmlimpl.XmlNode.Namespace one, Rhino.Xmlimpl.XmlNode.Namespace two)
			{
				if (one == null && two == null)
				{
					return true;
				}
				if (one == null || two == null)
				{
					return false;
				}
				return Equals(one.GetUri(), two.GetUri());
			}

			internal bool Equals(Rhino.Xmlimpl.XmlNode.QName other)
			{
				if (!NamespacesEqual(this.@namespace, other.@namespace))
				{
					return false;
				}
				if (!Equals(this.localName, other.localName))
				{
					return false;
				}
				return true;
			}

			public override bool Equals(object obj)
			{
				if (!(obj is Rhino.Xmlimpl.XmlNode.QName))
				{
					return false;
				}
				return Equals((Rhino.Xmlimpl.XmlNode.QName)obj);
			}

			public override int GetHashCode()
			{
				return localName == null ? 0 : localName.GetHashCode();
			}

			internal virtual void LookupPrefix(System.Xml.XmlNode node)
			{
				if (node == null)
				{
					throw new ArgumentException("node must not be null");
				}
				string prefix = node.LookupPrefix(@namespace.GetUri());
				if (prefix == null)
				{
					//    check to see if we match the default namespace
					string defaultNamespace = node.LookupNamespaceURI(null);
					if (defaultNamespace == null)
					{
						defaultNamespace = string.Empty;
					}
					string nodeNamespace = @namespace.GetUri();
					if (nodeNamespace.Equals(defaultNamespace))
					{
						prefix = string.Empty;
					}
				}
				int i = 0;
				while (prefix == null)
				{
					string generatedPrefix = "e4x_" + i++;
					string generatedUri = node.LookupNamespaceURI(generatedPrefix);
					if (generatedUri == null)
					{
						prefix = generatedPrefix;
						System.Xml.XmlNode top = node;
						while (top.ParentNode != null && top.ParentNode is XmlElement)
						{
							top = top.ParentNode;
						}
						((XmlElement)top).SetAttributeNS("http://www.w3.org/2000/xmlns/", "xmlns:" + prefix, @namespace.GetUri());
					}
				}
				@namespace.SetPrefix(prefix);
			}

			internal virtual string Qualify(System.Xml.XmlNode node)
			{
				if (@namespace.GetPrefix() == null)
				{
					if (node != null)
					{
						LookupPrefix(node);
					}
					else
					{
						if (@namespace.GetUri().Equals(string.Empty))
						{
							@namespace.SetPrefix(string.Empty);
						}
						else
						{
							//    TODO    I am not sure this is right, but if we are creating a standalone node, I think we can set the
							//            default namespace on the node itself and not worry about setting a prefix for that namespace.
							@namespace.SetPrefix(string.Empty);
						}
					}
				}
				return Qualify(@namespace.GetPrefix(), localName);
			}

			internal virtual void SetAttribute(XmlElement element, string value)
			{
				if (@namespace.GetPrefix() == null)
				{
					LookupPrefix(element);
				}
				element.SetAttributeNS(@namespace.GetUri(), Qualify(@namespace.GetPrefix(), localName), value);
			}

			internal virtual Rhino.Xmlimpl.XmlNode.Namespace GetNamespace()
			{
				return @namespace;
			}

			internal virtual string GetLocalName()
			{
				return localName;
			}
		}

		[System.Serializable]
		internal class InternalList
		{
			private const long serialVersionUID = -3633151157292048978L;

			private IList<Rhino.Xmlimpl.XmlNode> list;

			internal InternalList()
			{
				list = new AList<Rhino.Xmlimpl.XmlNode>();
			}

			private void _add(Rhino.Xmlimpl.XmlNode n)
			{
				list.AddItem(n);
			}

			internal virtual Rhino.Xmlimpl.XmlNode Item(int index)
			{
				return list[index];
			}

			internal virtual void Remove(int index)
			{
				list.Remove(index);
			}

			internal virtual void Add(Rhino.Xmlimpl.XmlNode.InternalList other)
			{
				for (int i = 0; i < other.Length(); i++)
				{
					_add(other.Item(i));
				}
			}

			internal virtual void Add(Rhino.Xmlimpl.XmlNode.InternalList from, int startInclusive, int endExclusive)
			{
				for (int i = startInclusive; i < endExclusive; i++)
				{
					_add(from.Item(i));
				}
			}

			internal virtual void Add(Rhino.Xmlimpl.XmlNode node)
			{
				_add(node);
			}

			internal virtual void Add(XML xml)
			{
				_add(xml.GetAnnotation());
			}

			internal virtual void AddToList(object toAdd)
			{
				if (toAdd is Undefined)
				{
					// Missing argument do nothing...
					return;
				}
				if (toAdd is XMLList)
				{
					XMLList xmlSrc = (XMLList)toAdd;
					for (int i = 0; i < xmlSrc.Length(); i++)
					{
						this._add((xmlSrc.Item(i)).GetAnnotation());
					}
				}
				else
				{
					if (toAdd is XML)
					{
						this._add(((XML)(toAdd)).GetAnnotation());
					}
					else
					{
						if (toAdd is Rhino.Xmlimpl.XmlNode)
						{
							this._add((Rhino.Xmlimpl.XmlNode)toAdd);
						}
					}
				}
			}

			internal virtual int Length()
			{
				return list.Count;
			}
		}

		internal abstract class Filter
		{
			private sealed class _Filter_839 : Rhino.Xmlimpl.XmlNode.Filter
			{
				public _Filter_839()
				{
				}

				internal override bool Accept(System.Xml.XmlNode node)
				{
					return node.NodeType == NodeConstants.COMMENT_NODE;
				}
			}

			internal static readonly Rhino.Xmlimpl.XmlNode.Filter COMMENT = new _Filter_839();

			private sealed class _Filter_845 : Rhino.Xmlimpl.XmlNode.Filter
			{
				public _Filter_845()
				{
				}

				internal override bool Accept(System.Xml.XmlNode node)
				{
					return node.NodeType == NodeConstants.TEXT_NODE;
				}
			}

			internal static readonly Rhino.Xmlimpl.XmlNode.Filter TEXT = new _Filter_845();

			internal static Rhino.Xmlimpl.XmlNode.Filter PROCESSING_INSTRUCTION(XMLName name)
			{
				return new _Filter_852(name);
			}

			private sealed class _Filter_852 : Rhino.Xmlimpl.XmlNode.Filter
			{
				public _Filter_852(XMLName name)
				{
					this.name = name;
				}

				internal override bool Accept(System.Xml.XmlNode node)
				{
					if (node.NodeType == NodeConstants.PROCESSING_INSTRUCTION_NODE)
					{
						ProcessingInstruction pi = (ProcessingInstruction)node;
						return name.MatchesLocalName(pi.GetTarget());
					}
					return false;
				}

				private readonly XMLName name;
			}

			private sealed class _Filter_863 : Rhino.Xmlimpl.XmlNode.Filter
			{
				public _Filter_863()
				{
				}

				internal override bool Accept(System.Xml.XmlNode node)
				{
					return node.NodeType == NodeConstants.ELEMENT_NODE;
				}
			}

			internal static Rhino.Xmlimpl.XmlNode.Filter ELEMENT = new _Filter_863();

			private sealed class _Filter_869 : Rhino.Xmlimpl.XmlNode.Filter
			{
				public _Filter_869()
				{
				}

				internal override bool Accept(System.Xml.XmlNode node)
				{
					return true;
				}
			}

			internal static Rhino.Xmlimpl.XmlNode.Filter TRUE = new _Filter_869();

			internal abstract bool Accept(System.Xml.XmlNode node);
		}

		//    Support experimental Java interface
		internal virtual System.Xml.XmlNode ToDomNode()
		{
			return this.dom;
		}
	}
}
