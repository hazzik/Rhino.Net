/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
#if XML

using Rhino;
using Rhino.Xml;
using Rhino.XmlImpl;
using Sharpen;

namespace Rhino.XmlImpl
{
	[System.Serializable]
	internal sealed class XMLWithScope : NativeWith
	{
		private XMLLibImpl lib;

		private int _currIndex;

		private XMLList _xmlList;

		private XMLObject _dqPrototype;

		internal XMLWithScope(XMLLibImpl lib, Scriptable parent, XMLObject prototype) : base(parent, prototype)
		{
			this.lib = lib;
		}

		internal void InitAsDotQuery()
		{
			XMLObject prototype = (XMLObject)Prototype;
			// XMLWithScope also handles the .(xxx) DotQuery for XML
			// basically DotQuery is a for/in/with statement and in
			// the following 3 statements we setup to signal it's
			// DotQuery,
			// the index and the object being looped over.  The
			// xws.setPrototype is the scope of the object which is
			// is a element of the lhs (XMLList).
			_currIndex = 0;
			_dqPrototype = prototype;
			if (prototype is XMLList)
			{
				XMLList xl = (XMLList)prototype;
				if (xl.Length() > 0)
				{
					Prototype = (Scriptable)(xl.Get(0, null));
				}
			}
			// Always return the outer-most type of XML lValue of
			// XML to left of dotQuery.
			_xmlList = lib.NewXMLList();
		}

		protected internal override object UpdateDotQuery(bool value)
		{
			// Return null to continue looping
			XMLObject seed = _dqPrototype;
			XMLList xmlL = _xmlList;
			if (seed is XMLList)
			{
				// We're a list so keep testing each element of the list if the
				// result on the top of stack is true then that element is added
				// to our result list.  If false, we try the next element.
				XMLList orgXmlL = (XMLList)seed;
				int idx = _currIndex;
				if (value)
				{
					xmlL.AddToList(orgXmlL.Get(idx, null));
				}
				// More elements to test?
				if (++idx < orgXmlL.Length())
				{
					// Yes, set our new index, get the next element and
					// reset the expression to run with this object as
					// the WITH selector.
					_currIndex = idx;
					Prototype = (Scriptable)(orgXmlL.Get(idx, null));
					// continue looping
					return null;
				}
			}
			else
			{
				// If we're not a XMLList then there's no looping
				// just return DQPrototype if the result is true.
				if (value)
				{
					xmlL.AddToList(seed);
				}
			}
			return xmlL;
		}
	}
}
#endif