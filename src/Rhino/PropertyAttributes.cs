using System;

namespace Rhino
{
	[Flags]
	public enum PropertyAttributes
	{
		/// <summary>The empty property attribute.</summary>
		/// <remarks>
		/// The empty property attribute.
		/// Used by getAttributes() and setAttributes().
		/// </remarks>
		/// <seealso cref="ScriptableObject.GetAttributes(string)">GetAttributes(string)</seealso>
		/// <seealso cref="ScriptableObject.SetAttributes(string,Rhino.PropertyAttributes)">SetAttributes(string, int)</seealso>
		EMPTY = 0,

		/// <summary>Property attribute indicating assignment to this property is ignored.</summary>
		/// <remarks>Property attribute indicating assignment to this property is ignored.</remarks>
		/// <seealso cref="ScriptableObject.Put(string, Scriptable, object)">Put(string, Scriptable, object)</seealso>
		/// <seealso cref="ScriptableObject.GetAttributes(string)">GetAttributes(string)</seealso>
		/// <seealso cref="ScriptableObject.SetAttributes(string,Rhino.PropertyAttributes)">SetAttributes(string, int)</seealso>
		READONLY = 1,

		/// <summary>Property attribute indicating property is not enumerated.</summary>
		/// <remarks>
		/// Property attribute indicating property is not enumerated.
		/// Only enumerated properties will be returned by getIds().
		/// </remarks>
		/// <seealso cref="ScriptableObject.GetIds()">GetIds()</seealso>
		/// <seealso cref="ScriptableObject.GetAttributes(string)">GetAttributes(string)</seealso>
		/// <seealso cref="ScriptableObject.SetAttributes(string,Rhino.PropertyAttributes)">SetAttributes(string, int)</seealso>
		DONTENUM = 2,

		/// <summary>Property attribute indicating property cannot be deleted.</summary>
		/// <remarks>Property attribute indicating property cannot be deleted.</remarks>
		/// <seealso cref="ScriptableObject.Delete(string)">Delete(string)</seealso>
		/// <seealso cref="ScriptableObject.GetAttributes(string)">GetAttributes(string)</seealso>
		/// <seealso cref="ScriptableObject.SetAttributes(string,Rhino.PropertyAttributes)">SetAttributes(string, int)</seealso>
		PERMANENT = 4,

		/// <summary>
		/// Property attribute indicating that this is a const property that has not
		/// been assigned yet.
		/// </summary>
		/// <remarks>
		/// Property attribute indicating that this is a const property that has not
		/// been assigned yet.  The first 'const' assignment to the property will
		/// clear this bit.
		/// </remarks>
		UNINITIALIZED_CONST = 8,

		CONST = PERMANENT | READONLY | UNINITIALIZED_CONST,
	}
}
