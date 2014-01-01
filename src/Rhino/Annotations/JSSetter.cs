using System;

namespace Rhino.Annotations
{
	/// <summary>An annotation that marks a Java method as JavaScript setter.</summary>
	/// <remarks>
	/// An annotation that marks a Java method as JavaScript setter. This can
	/// be used as an alternative to the <code>jsSet_</code> prefix desribed in
	/// <see cref="ScriptableObject.DefineClass{T}(Scriptable)">ScriptableObject.DefineClass&lt;T&gt;(Scriptable, Type&lt;T&gt;)</see>
	/// .
	/// </remarks>
	public sealed class JSSetter : Attribute
	{
		public string Value { get; set; }
	}
}
