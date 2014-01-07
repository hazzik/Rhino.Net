namespace Rhino
{
    public enum LanguageFeatures
    {
        /// <summary>Controls behaviour of <tt>Date.prototype.getYear()</tt>.</summary>
        /// <remarks>
        /// Controls behaviour of <tt>Date.prototype.getYear()</tt>.
        /// If <tt>hasFeature(NON_ECMA_GET_YEAR)</tt> returns true,
        /// Date.prototype.getYear subtructs 1900 only if 1900 &lt,= date &lt, 2000.
        /// The default behavior of
        /// <see cref="Context.HasFeature">HasFeature(int)</see>
        /// is always to subtruct
        /// 1900 as rquired by ECMAScript B.2.4.
        /// </remarks>
        NonEcmaGetYear = 1,

        /// <summary>Control if member expression as function name extension is available.</summary>
        /// <remarks>
        /// Control if member expression as function name extension is available.
        /// If <tt>hasFeature(FEATURE_MEMBER_EXPR_AS_FUNCTION_NAME)</tt> returns
        /// true, allow <tt>function memberExpression(args) { body }</tt> to be
        /// syntax sugar for <tt>memberExpression = function(args) { body }</tt>,
        /// when memberExpression is not a simple identifier.
        /// See ECMAScript-262, section 11.2 for definition of memberExpression.
        /// By default
        /// <see cref="Context.HasFeature">HasFeature(int)</see>
        /// returns false.
        /// </remarks>
        MemberExprAsFunctionName = 2,

        /// <summary>Control if reserved keywords are treated as identifiers.</summary>
        /// <remarks>
        /// Control if reserved keywords are treated as identifiers.
        /// If <tt>Context.HasFeature(RESERVED_KEYWORD_AS_IDENTIFIER)</tt> returns true,
        /// treat future reserved keyword (see  Ecma-262, section 7.5.3) as ordinary
        /// identifiers but warn about this usage.
        /// By default
        /// <see cref="Context.HasFeature">HasFeature(int)</see>
        /// returns false.
        /// </remarks>
        ReservedKeywordAsIdentifier = 3,

        /// <summary>
        /// Control if <tt>toString()</tt> should returns the same result
        /// as  <tt>toSource()</tt> when applied to objects and arrays.
        /// </summary>
        /// <remarks>
        /// Control if <tt>toString()</tt> should returns the same result
        /// as  <tt>toSource()</tt> when applied to objects and arrays.
        /// If <tt>hasFeature(FEATURE_TO_STRING_AS_SOURCE)</tt> returns true,
        /// calling <tt>toString()</tt> on JS objects gives the same result as
        /// calling <tt>toSource()</tt>. That is it returns JS source with code
        /// to create an object with all enumeratable fields of the original object
        /// instead of printing <tt>[object <i>result of
        /// <see cref="Scriptable.GetClassName()">Scriptable.GetClassName()</see>
        /// </i>]</tt>.
        /// <p>
        /// By default
        /// <see cref="Context.HasFeature">HasFeature(int)</see>
        /// returns true only if
        /// the current JS version is set to
        /// <see cref="LanguageVersion.VERSION_1_2">VERSION_1_2</see>
        /// .
        /// </remarks>
        ToStringAsSource = 4,

        /// <summary>
        /// Control if properties <tt>__proto__</tt> and <tt>__parent__</tt>
        /// are treated specially.
        /// </summary>
        /// <remarks>
        /// Control if properties <tt>__proto__</tt> and <tt>__parent__</tt>
        /// are treated specially.
        /// If <tt>hasFeature(FEATURE_PARENT_PROTO_PROPERTIES)</tt> returns true,
        /// treat <tt>__parent__</tt> and <tt>__proto__</tt> as special properties.
        /// <p>
        /// The properties allow to query and set scope and prototype chains for the
        /// objects. The special meaning of the properties is available
        /// only when they are used as the right hand side of the dot operator.
        /// For example, while <tt>x.__proto__ = y</tt> changes the prototype
        /// chain of the object <tt>x</tt> to point to <tt>y</tt>,
        /// <tt>x["__proto__"] = y</tt> simply assigns a new value to the property
        /// <tt>__proto__</tt> in <tt>x</tt> even when the feature is on.
        /// By default
        /// <see cref="Context.HasFeature">HasFeature(int)</see>
        /// returns true.
        /// </remarks>
        ParentProtoProperties = 5,

        /// <summary>Control if support for E4X(ECMAScript for XML) extension is available.</summary>
        /// <remarks>
        /// Control if support for E4X(ECMAScript for XML) extension is available.
        /// If hasFeature(FEATURE_E4X) returns true, the XML syntax is available.
        /// <p>
        /// By default
        /// <see cref="Context.HasFeature">HasFeature(int)</see>
        /// returns true if
        /// the current JS version is set to
        /// <see cref="LanguageVersion.VERSION_DEFAULT">VERSION_DEFAULT</see>
        /// or is at least
        /// <see cref="LanguageVersion.VERSION_1_6">VERSION_1_6</see>
        /// .
        /// </remarks>
        /// <since>1.6 Release 1</since>
        E4X = 6,

        /// <summary>Control if dynamic scope should be used for name access.</summary>
        /// <remarks>
        /// Control if dynamic scope should be used for name access.
        /// If hasFeature(DynamicScope) returns true, then the name lookup
        /// during name resolution will use the top scope of the script or function
        /// which is at the top of JS execution stack instead of the top scope of the
        /// script or function from the current stack frame if the top scope of
        /// the top stack frame contains the top scope of the current stack frame
        /// on its prototype chain.
        /// <p>
        /// This is useful to define shared scope containing functions that can
        /// be called from scripts and functions using private scopes.
        /// <p>
        /// By default
        /// <see cref="Context.HasFeature">HasFeature(int)</see>
        /// returns false.
        /// </remarks>
        /// <since>1.6 Release 1</since>
        DynamicScope = 7,

        /// <summary>Control if strict variable mode is enabled.</summary>
        /// <remarks>
        /// Control if strict variable mode is enabled.
        /// When the feature is on Rhino reports runtime errors if assignment
        /// to a global variable that does not exist is executed. When the feature
        /// is off such assignments create a new variable in the global scope as
        /// required by ECMA 262.
        /// <p>
        /// By default
        /// <see cref="Context.HasFeature">HasFeature(int)</see>
        /// returns false.
        /// </remarks>
        /// <since>1.6 Release 1</since>
        StrictVars = 8,

        /// <summary>Control if strict eval mode is enabled.</summary>
        /// <remarks>
        /// Control if strict eval mode is enabled.
        /// When the feature is on Rhino reports runtime errors if non-string
        /// argument is passed to the eval function. When the feature is off
        /// eval simply return non-string argument as is without performing any
        /// evaluation as required by ECMA 262.
        /// <p>
        /// By default
        /// <see cref="Context.HasFeature">HasFeature(int)</see>
        /// returns false.
        /// </remarks>
        /// <since>1.6 Release 1</since>
        StrictEval = 9,

        /// <summary>
        /// When the feature is on Rhino will add a "fileName" and "lineNumber"
        /// properties to Error objects automatically.
        /// </summary>
        /// <remarks>
        /// When the feature is on Rhino will add a "fileName" and "lineNumber"
        /// properties to Error objects automatically. When the feature is off, you
        /// have to explicitly pass them as the second and third argument to the
        /// Error constructor. Note that neither behavior is fully ECMA 262
        /// compliant (as 262 doesn't specify a three-arg constructor), but keeping
        /// the feature off results in Error objects that don't have
        /// additional non-ECMA properties when constructed using the ECMA-defined
        /// single-arg constructor and is thus desirable if a stricter ECMA
        /// compliance is desired, specifically adherence to the point 15.11.5. of
        /// the standard.
        /// <p>
        /// By default
        /// <see cref="Context.HasFeature">HasFeature(int)</see>
        /// returns false.
        /// </remarks>
        /// <since>1.6 Release 6</since>
        LocationInformationInError = 10,

        /// <summary>Controls whether JS 1.5 'strict mode' is enabled.</summary>
        /// <remarks>
        /// Controls whether JS 1.5 'strict mode' is enabled.
        /// When the feature is on, Rhino reports more than a dozen different
        /// warnings.  When the feature is off, these warnings are not generated.
        /// StrictMode implies StrictVars and StrictEval.
        /// <p>
        /// By default
        /// <see cref="Context.HasFeature">HasFeature(int)</see>
        /// returns false.
        /// </remarks>
        /// <since>1.6 Release 6</since>
        StrictMode = 11,

        /// <summary>Controls whether a warning should be treated as an error.</summary>
        /// <remarks>Controls whether a warning should be treated as an error.</remarks>
        /// <since>1.6 Release 6</since>
        WarningAsError = 12,

        /// <summary>Enables enhanced access to Java.</summary>
        /// <remarks>
        /// Enables enhanced access to Java.
        /// Specifically, controls whether private and protected members can be
        /// accessed, and whether scripts can catch all Java exceptions.
        /// <p>
        /// Note that this feature should only be enabled for trusted scripts.
        /// <p>
        /// By default
        /// <see cref="Context.HasFeature">HasFeature(int)</see>
        /// returns false.
        /// </remarks>
        /// <since>1.7 Release 1</since>
        EnhancedJavaAccess = 13,
    }
}