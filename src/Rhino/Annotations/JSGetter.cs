/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Rhino;
using Rhino.Annotations;
using Sharpen;

namespace Rhino.Annotations
{
	/// <summary>An annotation that marks a Java method as JavaScript getter.</summary>
	/// <remarks>
	/// An annotation that marks a Java method as JavaScript getter. This can
	/// be used as an alternative to the <code>jsGet_</code> prefix desribed in
	/// <see cref="ScriptableObject.DefineClass{T}(Scriptable, Type{T})">ScriptableObject.DefineClass&lt;T&gt;(Scriptable, Type&lt;T&gt;)</see>
	/// .
	/// </remarks>
	public sealed class JSGetter : System.Attribute
	{
		public string Value;
	}
}
