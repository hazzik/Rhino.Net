/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
#if XML

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Sharpen;

namespace Rhino.XmlImpl
{
	[Serializable]
	internal class XmlNode
	{
		private const string XML_NAMESPACES_NAMESPACE_URI = "http://www.w3.org/2000/xmlns/";

		private static readonly string USER_DATA_XMLNODE_KEY = typeof(XmlNode).FullName;

		private const bool DOM_LEVEL_3 = true;

		private static XmlNode GetUserData(System.Xml.XmlNode node)
		{
			return (XmlNode)node.GetUserData(USER_DATA_XMLNODE_KEY);
			return null;
		}

		private static void SetUserData(System.Xml.XmlNode node, XmlNode wrap)
		{
			node.SetUserData(USER_DATA_XMLNODE_KEY, wrap, wrap.events);
		}

		private static XmlNode CreateImpl(System.Xml.XmlNode node)
		{
			if (node is XmlDocument)
			{
				throw new ArgumentException();
			}
			XmlNode rv = null;
			if (GetUserData(node) == null)
			{
				rv = new XmlNode();
				rv.dom = node;
				SetUserData(node, rv);
			}
			else
			{
				rv = GetUserData(node);
			}
			return rv;
		}

		internal static XmlNode NewElementWithText(XmlProcessor processor, XmlNode reference, QName qname, string value)
		{
			if (reference is XmlDocument)
			{
				throw new ArgumentException("Cannot use Document node as reference");
			}
			XmlDocument document = reference != null
				? reference.dom.OwnerDocument
				: processor.NewDocument();
			System.Xml.XmlNode referenceDom = (reference != null) ? reference.dom : null;
			Namespace ns = qname.GetNamespace();
			XmlElement e = (ns == null || ns.GetUri().Length == 0) ? document.CreateElement(qname.GetLocalName(), null) : document.CreateElement(qname.Qualify(referenceDom), ns.GetUri());
			if (value != null)
			{
				e.AppendChild(document.CreateTextNode(value));
			}
			return CreateImpl(e);
		}

		internal static XmlNode CreateText(XmlProcessor processor, string value)
		{
			return CreateImpl(processor.NewDocument().CreateTextNode(value));
		}

		internal static XmlNode CreateElementFromNode(System.Xml.XmlNode node)
		{
			if (node is XmlDocument)
			{
				node = ((XmlDocument)node).DocumentElement;
			}
			return CreateImpl(node);
		}

		/// <exception cref="Org.Xml.Sax.SAXException"></exception>
		internal static XmlNode CreateElement(XmlProcessor processor, string namespaceUri, string xml)
		{
			return CreateImpl(processor.ToXml(namespaceUri, xml));
		}

		internal static XmlNode CreateEmpty(XmlProcessor processor)
		{
			return CreateText(processor, string.Empty);
		}

		private static XmlNode Copy(XmlNode other)
		{
			return CreateImpl(other.dom.CloneNode(true));
		}

		private XmlNodeUserDataHandler events = new XmlNodeUserDataHandler();

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
			return raw.EcmaToXmlString(dom);
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
			return dom.ChildNodes.Count;
		}

		internal virtual XmlNode Parent()
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
			if (IsAttributeType())
			{
				return -1;
			}
			if (Parent() == null)
			{
				return -1;
			}
			XmlNodeList siblings = dom.ParentNode.ChildNodes;
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
			dom.RemoveChild(dom.ChildNodes.Item(index));
		}

		internal virtual string ToXmlString(XmlProcessor processor)
		{
			return processor.EcmaToXmlString(dom);
		}

		internal virtual string EcmaValue()
		{
			//    TODO    See ECMA 357 Section 9.1
			if (IsTextType())
			{
				return ((XmlText)dom).Data;
			}
			else
			{
				if (IsAttributeType())
				{
					return dom.Value;
				}
				else
				{
					if (IsProcessingInstructionType())
					{
						return ((XmlProcessingInstruction)dom).Data;
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
			if (dom is XmlAttribute)
			{
				XmlAttribute attr = (XmlAttribute)dom;
				attr.OwnerElement.Attributes.RemoveNamedItemNS(attr.NamespaceURI, attr.LocalName);
			}
			else
			{
				if (dom.ParentNode != null)
				{
					dom.ParentNode.RemoveChild(dom);
				}
			}
		}

		//    This case can be exercised at least when executing the regression
		//    tests under https://bugzilla.mozilla.org/show_bug.cgi?id=354145
		internal virtual void Normalize()
		{
			dom.Normalize();
		}

		internal virtual void InsertChildAt(int index, XmlNode node)
		{
			System.Xml.XmlNode parent = dom;
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

		internal virtual void InsertChildrenAt(int index, XmlNode[] nodes)
		{
			for (int i = 0; i < nodes.Length; i++)
			{
				InsertChildAt(index + i, nodes[i]);
			}
		}

		internal virtual XmlNode GetChild(int index)
		{
			System.Xml.XmlNode child = dom.ChildNodes.Item(index);
			return CreateImpl(child);
		}

		//    Helper method for XML.hasSimpleContent()
		internal virtual bool HasChildElement()
		{
			XmlNodeList nodes = dom.ChildNodes;
			for (int i = 0; i < nodes.Count; i++)
			{
				if (nodes.Item(i).NodeType == XmlNodeType.Element)
				{
					return true;
				}
			}
			return false;
		}

		internal virtual bool IsSameNode(XmlNode other)
		{
			//    TODO    May need to be changed if we allow XmlNode to refer to several Node objects
			return dom == other.dom;
		}

		private string ToUri(string ns)
		{
			return (ns == null) ? string.Empty : ns;
		}

		private void AddNamespaces(Namespaces rv, XmlElement element)
		{
			if (element == null)
			{
				throw new Exception("element must not be null");
			}
			string myDefaultNamespace = ToUri(element.GetPrefixOfNamespace(null));
			string parentDefaultNamespace = string.Empty;
			if (element.ParentNode != null)
			{
				parentDefaultNamespace = ToUri(element.ParentNode.GetPrefixOfNamespace(null));
			}
			if (!myDefaultNamespace.Equals(parentDefaultNamespace) || !(element.ParentNode is XmlElement))
			{
				rv.Declare(Namespace.Create(string.Empty, myDefaultNamespace));
			}
			XmlNamedNodeMap attributes = element.Attributes;
			for (int i = 0; i < attributes.Count; i++)
			{
				XmlAttribute attr = (XmlAttribute)attributes.Item(i);
				if (attr.Prefix != null && attr.Prefix.Equals("xmlns"))
				{
					rv.Declare(Namespace.Create(attr.LocalName, attr.Value));
				}
			}
		}

		private Namespaces GetAllNamespaces()
		{
			Namespaces rv = new Namespaces();
			System.Xml.XmlNode target = dom;
			if (target is XmlAttribute)
			{
				target = ((XmlAttribute)target).OwnerElement;
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
			rv.Declare(Namespace.Create(string.Empty, string.Empty));
			return rv;
		}

		internal virtual Namespace[] GetInScopeNamespaces()
		{
			Namespaces rv = GetAllNamespaces();
			return rv.GetNamespaces();
		}

		internal virtual Namespace[] GetNamespaceDeclarations()
		{
			//    ECMA357 13.4.4.24
			if (dom is XmlElement)
			{
				Namespaces rv = new Namespaces();
				AddNamespaces(rv, (XmlElement)dom);
				return rv.GetNamespaces();
			}
			else
			{
				return new Namespace[0];
			}
		}

		internal virtual Namespace GetNamespaceDeclaration(string prefix)
		{
			if (prefix.Equals(string.Empty) && dom is XmlAttribute)
			{
				//    Default namespaces do not apply to attributes; see XML Namespaces section 5.2
				return Namespace.Create(string.Empty, string.Empty);
			}
			Namespaces rv = GetAllNamespaces();
			return rv.GetNamespace(prefix);
		}

		internal virtual Namespace GetNamespaceDeclaration()
		{
			if (dom.Prefix == null)
			{
				return GetNamespaceDeclaration(string.Empty);
			}
			return GetNamespaceDeclaration(dom.Prefix);
		}

		[Serializable]
		internal class XmlNodeUserDataHandler : UserDataHandler
		{
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

			internal virtual void Declare(Namespace n)
			{
				if (map.Get(n.prefix) == null)
				{
					map[n.prefix] = n.uri;
				}
				//    TODO    I think this is analogous to the other way, but have not really thought it through ... should local scope
				//            matter more than outer scope?
				if (uriToPrefix.Get(n.uri) == null)
				{
					uriToPrefix[n.uri] = n.prefix;
				}
			}

			internal virtual Namespace GetNamespaceByUri(string uri)
			{
				if (uriToPrefix.Get(uri) == null)
				{
					return null;
				}
				return Namespace.Create(uri, uriToPrefix.Get(uri));
			}

			internal virtual Namespace GetNamespace(string prefix)
			{
				if (map.Get(prefix) == null)
				{
					return null;
				}
				return Namespace.Create(prefix, map.Get(prefix));
			}

			internal virtual Namespace[] GetNamespaces()
			{
				return (from prefix in map.Keys
					let uri = map.Get(prefix)
					select Namespace.Create(prefix, uri)
					into n
					where !n.IsEmpty()
					select n).ToArray();
			}
		}

		internal XmlNode Copy()
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
			return dom.NodeType == XmlNodeType.Text || dom.NodeType == XmlNodeType.CDATA;
		}

		internal bool IsAttributeType()
		{
			return dom.NodeType == XmlNodeType.Attribute;
		}

		internal bool IsProcessingInstructionType()
		{
			return dom.NodeType == XmlNodeType.ProcessingInstruction;
		}

		internal bool IsCommentType()
		{
			return dom.NodeType == XmlNodeType.Comment;
		}

		internal bool IsElementType()
		{
			return dom.NodeType == XmlNodeType.Element;
		}

		internal void RenameNode(QName qname)
		{
			dom = dom.OwnerDocument.RenameNode(dom, qname.GetNamespace().GetUri(), qname.Qualify(dom));
		}

		internal virtual void InvalidateNamespacePrefix()
		{
			if (!(dom is XmlElement))
			{
				throw new InvalidOperationException();
			}
			string prefix = dom.Prefix;
			QName after = QName.Create(dom.NamespaceURI, dom.LocalName, null);
			RenameNode(after);
			XmlNamedNodeMap attrs = dom.Attributes;
			for (int i = 0; i < attrs.Count; i++)
			{
				if (attrs.Item(i).Prefix.Equals(prefix))
				{
					CreateImpl(attrs.Item(i)).RenameNode(QName.Create(attrs.Item(i).NamespaceURI, attrs.Item(i).LocalName, null));
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
			if (dom.GetPrefixOfNamespace(uri) != null && dom.GetPrefixOfNamespace(uri).Equals(prefix))
			{
			}
			else
			{
				//    do nothing
				XmlElement e = (XmlElement)dom;
				DeclareNamespace(e, prefix, uri);
			}
		}

		private Namespace GetDefaultNamespace()
		{
			string prefix = string.Empty;
			string uri = (dom.GetPrefixOfNamespace(null) == null) ? string.Empty : dom.GetPrefixOfNamespace(null);
			return Namespace.Create(prefix, uri);
		}

		private string GetExistingPrefixFor(Namespace @namespace)
		{
			if (GetDefaultNamespace().GetUri().Equals(@namespace.GetUri()))
			{
				return string.Empty;
			}
			return dom.LookupPrefix(@namespace.GetUri());
		}

		private Namespace GetNodeNamespace()
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
			return Namespace.Create(prefix, uri);
		}

		internal virtual Namespace GetNamespace()
		{
			return GetNodeNamespace();
		}

		internal virtual void RemoveNamespace(Namespace @namespace)
		{
			Namespace current = GetNodeNamespace();
			//    Do not remove in-use namespace
			if (@namespace.Is(current))
			{
				return;
			}
			XmlNamedNodeMap attrs = dom.Attributes;
			for (int i = 0; i < attrs.Count; i++)
			{
				XmlNode attr = CreateImpl(attrs.Item(i));
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
			XmlProcessingInstruction pi = (XmlProcessingInstruction)dom;
			//    We cannot set the node name; Document.renameNode() only supports elements and attributes.  So we replace it
			pi.ParentNode.ReplaceChild(pi, pi.OwnerDocument.CreateProcessingInstruction(localName, pi.Data));
		}

		internal void SetLocalName(string localName)
		{
			if (dom is XmlProcessingInstruction)
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
				dom = dom.OwnerDocument.RenameNode(dom, dom.NamespaceURI, QName.Qualify(prefix, localName));
			}
		}

		internal QName GetQname()
		{
			string uri = (dom.NamespaceURI) == null ? string.Empty : dom.NamespaceURI;
			string prefix = (dom.Prefix == null) ? string.Empty : dom.Prefix;
			return QName.Create(uri, dom.LocalName, prefix);
		}

		internal virtual void AddMatchingChildren(XMLList result, Filter filter)
		{
			System.Xml.XmlNode node = dom;
			XmlNodeList children = node.ChildNodes;
			for (int i = 0; i < children.Count; i++)
			{
				System.Xml.XmlNode childnode = children.Item(i);
				XmlNode child = CreateImpl(childnode);
				if (filter.Accept(childnode))
				{
					result.AddToList(child);
				}
			}
		}

		internal virtual XmlNode[] GetMatchingChildren(Filter filter)
		{
			List<XmlNode> rv = new List<XmlNode>();
			XmlNodeList nodes = dom.ChildNodes;
			for (int i = 0; i < nodes.Count; i++)
			{
				System.Xml.XmlNode node = nodes.Item(i);
				if (filter.Accept(node))
				{
					rv.Add (CreateImpl(node));
				}
			}
			return rv.ToArray();
		}

		internal virtual XmlNode[] GetAttributes()
		{
			XmlNamedNodeMap attrs = dom.Attributes;
			//    TODO    Or could make callers handle null?
			if (attrs == null)
			{
				throw new InvalidOperationException("Must be element.");
			}
			XmlNode[] rv = new XmlNode[attrs.Count];
			for (int i = 0; i < attrs.Count; i++)
			{
				rv[i] = CreateImpl(attrs.Item(i));
			}
			return rv;
		}

		internal virtual string GetAttributeValue()
		{
			return ((XmlAttribute)dom).Value;
		}

		internal virtual void SetAttribute(QName name, string value)
		{
			if (!(dom is XmlElement))
			{
				throw new InvalidOperationException("Can only set attribute on elements.");
			}
			name.SetAttribute((XmlElement)dom, value);
		}

		internal virtual void ReplaceWith(XmlNode other)
		{
			System.Xml.XmlNode replacement = other.dom;
			if (replacement.OwnerDocument != dom.OwnerDocument)
			{
				replacement = dom.OwnerDocument.ImportNode(replacement, true);
			}
			dom.ParentNode.ReplaceChild(replacement, dom);
		}

		internal virtual string EcmaToXMLString(XmlProcessor processor)
		{
			if (IsElementType())
			{
				XmlElement copy = (XmlElement)dom.CloneNode(true);
				Namespace[] inScope = GetInScopeNamespaces();
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

		[Serializable]
		internal class Namespace
		{
			internal static Namespace Create(string prefix, string uri)
			{
				if (prefix == null)
				{
					throw new ArgumentException("Empty string represents default namespace prefix");
				}
				if (uri == null)
				{
					throw new ArgumentException("Namespace may not lack a URI");
				}
				Namespace rv = new Namespace();
				rv.prefix = prefix;
				rv.uri = uri;
				return rv;
			}

			internal static Namespace Create(string uri)
			{
				Namespace rv = new Namespace();
				rv.uri = uri;
				// Avoid null prefix for "" namespace
				if (uri == null || uri.Length == 0)
				{
					rv.prefix = string.Empty;
				}
				return rv;
			}

			internal static readonly Namespace GLOBAL = Create(string.Empty, string.Empty);

			internal string prefix;

			internal string uri;

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

			internal virtual bool Is(Namespace other)
			{
				return prefix != null && other.prefix != null && prefix.Equals(other.prefix) && uri.Equals(other.uri);
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
			internal void SetPrefix(string prefix)
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

		[Serializable]
		internal class QName
		{
			//    TODO    Where is this class used?  No longer using it in QName implementation
			internal static QName Create(Namespace @namespace, string localName)
			{
				//    A null namespace indicates a wild-card match for any namespace
				//    A null localName indicates "*" from the point of view of ECMA357
				if (localName != null && localName.Equals("*"))
				{
					throw new Exception("* is not valid localName");
				}
				QName rv = new QName();
				rv.@namespace = @namespace;
				rv.localName = localName;
				return rv;
			}

			[Obsolete(@"")]
			internal static QName Create(string uri, string localName, string prefix)
			{
				return Create(Namespace.Create(prefix, uri), localName);
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

			private Namespace @namespace;

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

			private bool NamespacesEqual(Namespace one, Namespace two)
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

			internal bool Equals(QName other)
			{
				if (!NamespacesEqual(@namespace, other.@namespace))
				{
					return false;
				}
				if (!Equals(localName, other.localName))
				{
					return false;
				}
				return true;
			}

			public override bool Equals(object obj)
			{
				if (!(obj is QName))
				{
					return false;
				}
				return Equals((QName)obj);
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
					string defaultNamespace = node.GetPrefixOfNamespace(null);
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
					string generatedUri = node.GetPrefixOfNamespace(generatedPrefix);
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

			internal virtual Namespace GetNamespace()
			{
				return @namespace;
			}

			internal virtual string GetLocalName()
			{
				return localName;
			}
		}

		[Serializable]
		internal class InternalList
		{
			private IList<XmlNode> list;

			internal InternalList()
			{
				list = new List<XmlNode>();
			}

			private void _add(XmlNode n)
			{
				list.Add(n);
			}

			internal virtual XmlNode Item(int index)
			{
				return list[index];
			}

			internal virtual void Remove(int index)
			{
				list.Remove(index);
			}

			internal virtual void Add(InternalList other)
			{
				for (int i = 0; i < other.Length(); i++)
				{
					_add(other.Item(i));
				}
			}

			internal virtual void Add(InternalList from, int startInclusive, int endExclusive)
			{
				for (int i = startInclusive; i < endExclusive; i++)
				{
					_add(from.Item(i));
				}
			}

			internal virtual void Add(XmlNode node)
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
						_add((xmlSrc.Item(i)).GetAnnotation());
					}
				}
				else
				{
					if (toAdd is XML)
					{
						_add(((XML)(toAdd)).GetAnnotation());
					}
					else
					{
						if (toAdd is XmlNode)
						{
							_add((XmlNode)toAdd);
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
			private sealed class _Filter_839 : Filter
			{
				public _Filter_839()
				{
				}

				internal override bool Accept(System.Xml.XmlNode node)
				{
					return node.NodeType == XmlNodeType.Comment;
				}
			}

			internal static readonly Filter COMMENT = new _Filter_839();

			private sealed class _Filter_845 : Filter
			{
				public _Filter_845()
				{
				}

				internal override bool Accept(System.Xml.XmlNode node)
				{
					return node.NodeType == XmlNodeType.Text;
				}
			}

			internal static readonly Filter TEXT = new _Filter_845();

			internal static Filter PROCESSING_INSTRUCTION(XMLName name)
			{
				return new _Filter_852(name);
			}

			private sealed class _Filter_852 : Filter
			{
				public _Filter_852(XMLName name)
				{
					this.name = name;
				}

				internal override bool Accept(System.Xml.XmlNode node)
				{
					if (node.NodeType == XmlNodeType.ProcessingInstruction)
					{
						XmlProcessingInstruction pi = (XmlProcessingInstruction)node;
						return name.MatchesLocalName(pi.Target);
					}
					return false;
				}

				private readonly XMLName name;
			}

			private sealed class _Filter_863 : Filter
			{
				public _Filter_863()
				{
				}

				internal override bool Accept(System.Xml.XmlNode node)
				{
					return node.NodeType == XmlNodeType.Element;
				}
			}

			internal static Filter ELEMENT = new _Filter_863();

			private sealed class _Filter_869 : Filter
			{
				public _Filter_869()
				{
				}

				internal override bool Accept(System.Xml.XmlNode node)
				{
					return true;
				}
			}

			internal static Filter TRUE = new _Filter_869();

			internal abstract bool Accept(System.Xml.XmlNode node);
		}

		//    Support experimental Java interface
		internal virtual System.Xml.XmlNode ToDomNode()
		{
			return dom;
		}
	}
}
#endif