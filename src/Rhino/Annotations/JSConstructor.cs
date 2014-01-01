using System;

namespace Rhino.Annotations
{
	/// <summary>An annotation that marks a Java method as JavaScript constructor.</summary>
	/// <remarks>
	/// An annotation that marks a Java method as JavaScript constructor. This can
	/// be used as an alternative to the <code>jsConstructor</code> naming convention desribed in
	/// <see cref="ScriptableObject.DefineClass{T}(Scriptable)">ScriptableObject.DefineClass&lt;T&gt;(Scriptable, Type&lt;T&gt;)</see>
	/// .
	/// </remarks>
	public sealed class JSConstructor : Attribute
	{
	}
}
