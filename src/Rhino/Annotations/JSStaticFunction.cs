using System;

namespace Rhino.Annotations
{
	/// <summary>An annotation that marks a Java method as JavaScript static function.</summary>
	/// <remarks>
	/// An annotation that marks a Java method as JavaScript static function. This can
	/// be used as an alternative to the <code>jsStaticFunction_</code> prefix desribed in
	/// <see cref="ScriptableObject.DefineClass{T}(Scriptable)">ScriptableObject.DefineClass&lt;T&gt;(Scriptable, Type&lt;T&gt;)</see>
	/// .
	/// </remarks>
	public sealed class JSStaticFunction : Attribute
	{
		public string Value { get; set; }
	}
}
