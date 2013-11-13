/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Rhino;
using Sharpen;

namespace Rhino
{
	/// <summary>This class implements the JavaScript scanner.</summary>
	/// <remarks>
	/// This class implements the JavaScript scanner.
	/// It is based on the C source files jsscan.c and jsscan.h
	/// in the jsref package.
	/// </remarks>
	/// <seealso cref="Parser">Parser</seealso>
	/// <author>Mike McCabe</author>
	/// <author>Brendan Eich</author>
	public class Token
	{
		public enum CommentType
		{
			LINE,
			BLOCK_COMMENT,
			JSDOC,
			HTML
		}

		public const bool printTrees = false;

		internal const bool printICode = false;

		internal const bool printNames = printTrees || printICode;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int ERROR = -1;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int EOF = 0;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int EOL = 1;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int FIRST_BYTECODE_TOKEN = 2;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int ENTERWITH = 2;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int LEAVEWITH = 3;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int RETURN = 4;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int GOTO = 5;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int IFEQ = 6;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int IFNE = 7;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int SETNAME = 8;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int BITOR = 9;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int BITXOR = 10;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int BITAND = 11;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int EQ = 12;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int NE = 13;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int LT = 14;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int LE = 15;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int GT = 16;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int GE = 17;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int LSH = 18;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int RSH = 19;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int URSH = 20;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int ADD = 21;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int SUB = 22;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int MUL = 23;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int DIV = 24;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int MOD = 25;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int NOT = 26;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int BITNOT = 27;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int POS = 28;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int NEG = 29;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int NEW = 30;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int DELPROP = 31;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int TYPEOF = 32;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int GETPROP = 33;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int GETPROPNOWARN = 34;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int SETPROP = 35;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int GETELEM = 36;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int SETELEM = 37;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int CALL = 38;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int NAME = 39;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int NUMBER = 40;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int STRING = 41;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int NULL = 42;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int THIS = 43;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int FALSE = 44;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int TRUE = 45;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int SHEQ = 46;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int SHNE = 47;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int REGEXP = 48;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int BINDNAME = 49;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int THROW = 50;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int RETHROW = 51;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int IN = 52;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int INSTANCEOF = 53;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int LOCAL_LOAD = 54;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int GETVAR = 55;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int SETVAR = 56;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int CATCH_SCOPE = 57;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int ENUM_INIT_KEYS = 58;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int ENUM_INIT_VALUES = 59;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int ENUM_INIT_ARRAY = 60;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int ENUM_NEXT = 61;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int ENUM_ID = 62;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int THISFN = 63;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int RETURN_RESULT = 64;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int ARRAYLIT = 65;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int OBJECTLIT = 66;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int GET_REF = 67;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int SET_REF = 68;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int DEL_REF = 69;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int REF_CALL = 70;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int REF_SPECIAL = 71;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int YIELD = 72;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int STRICT_SETNAME = 73;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int DEFAULTNAMESPACE = 74;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int ESCXMLATTR = 75;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int ESCXMLTEXT = 76;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int REF_MEMBER = 77;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int REF_NS_MEMBER = 78;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int REF_NAME = 79;

		/// <summary>Token types.</summary>
		/// <remarks>
		/// Token types.  These values correspond to JSTokenType values in
		/// jsscan.c.
		/// </remarks>
		public const int REF_NS_NAME = 80;

		public const int LAST_BYTECODE_TOKEN = REF_NS_NAME;

		public const int TRY = 81;

		public const int SEMI = 82;

		public const int LB = 83;

		public const int RB = 84;

		public const int LC = 85;

		public const int RC = 86;

		public const int LP = 87;

		public const int RP = 88;

		public const int COMMA = 89;

		public const int ASSIGN = 90;

		public const int ASSIGN_BITOR = 91;

		public const int ASSIGN_BITXOR = 92;

		public const int ASSIGN_BITAND = 93;

		public const int ASSIGN_LSH = 94;

		public const int ASSIGN_RSH = 95;

		public const int ASSIGN_URSH = 96;

		public const int ASSIGN_ADD = 97;

		public const int ASSIGN_SUB = 98;

		public const int ASSIGN_MUL = 99;

		public const int ASSIGN_DIV = 100;

		public const int ASSIGN_MOD = 101;

		public const int FIRST_ASSIGN = ASSIGN;

		public const int LAST_ASSIGN = ASSIGN_MOD;

		public const int HOOK = 102;

		public const int COLON = 103;

		public const int OR = 104;

		public const int AND = 105;

		public const int INC = 106;

		public const int DEC = 107;

		public const int DOT = 108;

		public const int FUNCTION = 109;

		public const int EXPORT = 110;

		public const int IMPORT = 111;

		public const int IF = 112;

		public const int ELSE = 113;

		public const int SWITCH = 114;

		public const int CASE = 115;

		public const int DEFAULT = 116;

		public const int WHILE = 117;

		public const int DO = 118;

		public const int FOR = 119;

		public const int BREAK = 120;

		public const int CONTINUE = 121;

		public const int VAR = 122;

		public const int WITH = 123;

		public const int CATCH = 124;

		public const int FINALLY = 125;

		public const int VOID = 126;

		public const int RESERVED = 127;

		public const int EMPTY = 128;

		public const int BLOCK = 129;

		public const int LABEL = 130;

		public const int TARGET = 131;

		public const int LOOP = 132;

		public const int EXPR_VOID = 133;

		public const int EXPR_RESULT = 134;

		public const int JSR = 135;

		public const int SCRIPT = 136;

		public const int TYPEOFNAME = 137;

		public const int USE_STACK = 138;

		public const int SETPROP_OP = 139;

		public const int SETELEM_OP = 140;

		public const int LOCAL_BLOCK = 141;

		public const int SET_REF_OP = 142;

		public const int DOTDOT = 143;

		public const int COLONCOLON = 144;

		public const int XML = 145;

		public const int DOTQUERY = 146;

		public const int XMLATTR = 147;

		public const int XMLEND = 148;

		public const int TO_OBJECT = 149;

		public const int TO_DOUBLE = 150;

		public const int GET = 151;

		public const int SET = 152;

		public const int LET = 153;

		public const int CONST = 154;

		public const int SETCONST = 155;

		public const int SETCONSTVAR = 156;

		public const int ARRAYCOMP = 157;

		public const int LETEXPR = 158;

		public const int WITHEXPR = 159;

		public const int DEBUGGER = 160;

		public const int COMMENT = 161;

		public const int GENEXPR = 162;

		public const int LAST_TOKEN = 163;

		// debug flags
		// start enum
		// well-known as the only code < EOF
		// end of file token - (not EOF_CHAR)
		// end of line
		// Interpreter reuses the following as bytecodes
		// shallow equality (===)
		// shallow inequality (!==)
		// rethrow caught exception: catch (e if ) use it
		// to return previously stored return result
		// array literal
		// object literal
		// *reference
		// *reference    = something
		// delete reference
		// f(args)    = something or f(args)++
		// reference for special properties like __proto
		// JS 1.7 yield pseudo keyword
		// For XML support:
		// default xml namespace =
		// Reference for x.@y, x..y etc.
		// Reference for x.ns::y, x..ns::y etc.
		// Reference for @y, @[y] etc.
		// Reference for ns::y, @ns::y@[y] etc.
		// End of interpreter bytecodes
		// semicolon
		// left and right brackets
		// left and right curlies (braces)
		// left and right parentheses
		// comma operator
		// simple assignment  (=)
		// |=
		// ^=
		// |=
		// <<=
		// >>=
		// >>>=
		// +=
		// -=
		// *=
		// /=
		// %=
		// conditional (?:)
		// logical or (||)
		// logical and (&&)
		// increment/decrement (++ --)
		// member operator (.)
		// function keyword
		// export keyword
		// import keyword
		// if keyword
		// else keyword
		// switch keyword
		// case keyword
		// default keyword
		// while keyword
		// do keyword
		// for keyword
		// break keyword
		// continue keyword
		// var keyword
		// with keyword
		// catch keyword
		// finally keyword
		// void keyword
		// reserved keywords
		// statement block
		// label
		// expression statement in functions
		// expression statement in scripts
		// top-level node for entire script
		// for typeof(simple-name)
		// x.y op= something
		// x[y] op= something
		// *reference op= something
		// For XML support:
		// member operator (..)
		// namespace::name
		// XML type
		// .() -- e.g., x.emps.emp.(name == "terry")
		// @
		// Optimizer-only-tokens
		// JS 1.5 get pseudo keyword
		// JS 1.5 set pseudo keyword
		// JS 1.7 let pseudo keyword
		// array comprehension
		/// <summary>Returns a name for the token.</summary>
		/// <remarks>
		/// Returns a name for the token.  If Rhino is compiled with certain
		/// hardcoded debugging flags in this file, it calls
		/// <code>#typeToName</code>
		/// ;
		/// otherwise it returns a string whose value is the token number.
		/// </remarks>
		public static string Name(int token)
		{
			return token.ToString();
			return TypeToName(token);
		}

		/// <summary>Always returns a human-readable string for the token name.</summary>
		/// <remarks>
		/// Always returns a human-readable string for the token name.
		/// For instance,
		/// <see cref="FINALLY">FINALLY</see>
		/// has the name "FINALLY".
		/// </remarks>
		/// <param name="token">the token code</param>
		/// <returns>the actual name for the token code</returns>
		public static string TypeToName(int token)
		{
			switch (token)
			{
				case ERROR:
				{
					return "ERROR";
				}

				case EOF:
				{
					return "EOF";
				}

				case EOL:
				{
					return "EOL";
				}

				case ENTERWITH:
				{
					return "ENTERWITH";
				}

				case LEAVEWITH:
				{
					return "LEAVEWITH";
				}

				case RETURN:
				{
					return "RETURN";
				}

				case GOTO:
				{
					return "GOTO";
				}

				case IFEQ:
				{
					return "IFEQ";
				}

				case IFNE:
				{
					return "IFNE";
				}

				case SETNAME:
				{
					return "SETNAME";
				}

				case BITOR:
				{
					return "BITOR";
				}

				case BITXOR:
				{
					return "BITXOR";
				}

				case BITAND:
				{
					return "BITAND";
				}

				case EQ:
				{
					return "EQ";
				}

				case NE:
				{
					return "NE";
				}

				case LT:
				{
					return "LT";
				}

				case LE:
				{
					return "LE";
				}

				case GT:
				{
					return "GT";
				}

				case GE:
				{
					return "GE";
				}

				case LSH:
				{
					return "LSH";
				}

				case RSH:
				{
					return "RSH";
				}

				case URSH:
				{
					return "URSH";
				}

				case ADD:
				{
					return "ADD";
				}

				case SUB:
				{
					return "SUB";
				}

				case MUL:
				{
					return "MUL";
				}

				case DIV:
				{
					return "DIV";
				}

				case MOD:
				{
					return "MOD";
				}

				case NOT:
				{
					return "NOT";
				}

				case BITNOT:
				{
					return "BITNOT";
				}

				case POS:
				{
					return "POS";
				}

				case NEG:
				{
					return "NEG";
				}

				case NEW:
				{
					return "NEW";
				}

				case DELPROP:
				{
					return "DELPROP";
				}

				case TYPEOF:
				{
					return "TYPEOF";
				}

				case GETPROP:
				{
					return "GETPROP";
				}

				case GETPROPNOWARN:
				{
					return "GETPROPNOWARN";
				}

				case SETPROP:
				{
					return "SETPROP";
				}

				case GETELEM:
				{
					return "GETELEM";
				}

				case SETELEM:
				{
					return "SETELEM";
				}

				case CALL:
				{
					return "CALL";
				}

				case NAME:
				{
					return "NAME";
				}

				case NUMBER:
				{
					return "NUMBER";
				}

				case STRING:
				{
					return "STRING";
				}

				case NULL:
				{
					return "NULL";
				}

				case THIS:
				{
					return "THIS";
				}

				case FALSE:
				{
					return "FALSE";
				}

				case TRUE:
				{
					return "TRUE";
				}

				case SHEQ:
				{
					return "SHEQ";
				}

				case SHNE:
				{
					return "SHNE";
				}

				case REGEXP:
				{
					return "REGEXP";
				}

				case BINDNAME:
				{
					return "BINDNAME";
				}

				case THROW:
				{
					return "THROW";
				}

				case RETHROW:
				{
					return "RETHROW";
				}

				case IN:
				{
					return "IN";
				}

				case INSTANCEOF:
				{
					return "INSTANCEOF";
				}

				case LOCAL_LOAD:
				{
					return "LOCAL_LOAD";
				}

				case GETVAR:
				{
					return "GETVAR";
				}

				case SETVAR:
				{
					return "SETVAR";
				}

				case CATCH_SCOPE:
				{
					return "CATCH_SCOPE";
				}

				case ENUM_INIT_KEYS:
				{
					return "ENUM_INIT_KEYS";
				}

				case ENUM_INIT_VALUES:
				{
					return "ENUM_INIT_VALUES";
				}

				case ENUM_INIT_ARRAY:
				{
					return "ENUM_INIT_ARRAY";
				}

				case ENUM_NEXT:
				{
					return "ENUM_NEXT";
				}

				case ENUM_ID:
				{
					return "ENUM_ID";
				}

				case THISFN:
				{
					return "THISFN";
				}

				case RETURN_RESULT:
				{
					return "RETURN_RESULT";
				}

				case ARRAYLIT:
				{
					return "ARRAYLIT";
				}

				case OBJECTLIT:
				{
					return "OBJECTLIT";
				}

				case GET_REF:
				{
					return "GET_REF";
				}

				case SET_REF:
				{
					return "SET_REF";
				}

				case DEL_REF:
				{
					return "DEL_REF";
				}

				case REF_CALL:
				{
					return "REF_CALL";
				}

				case REF_SPECIAL:
				{
					return "REF_SPECIAL";
				}

				case DEFAULTNAMESPACE:
				{
					return "DEFAULTNAMESPACE";
				}

				case ESCXMLTEXT:
				{
					return "ESCXMLTEXT";
				}

				case ESCXMLATTR:
				{
					return "ESCXMLATTR";
				}

				case REF_MEMBER:
				{
					return "REF_MEMBER";
				}

				case REF_NS_MEMBER:
				{
					return "REF_NS_MEMBER";
				}

				case REF_NAME:
				{
					return "REF_NAME";
				}

				case REF_NS_NAME:
				{
					return "REF_NS_NAME";
				}

				case TRY:
				{
					return "TRY";
				}

				case SEMI:
				{
					return "SEMI";
				}

				case LB:
				{
					return "LB";
				}

				case RB:
				{
					return "RB";
				}

				case LC:
				{
					return "LC";
				}

				case RC:
				{
					return "RC";
				}

				case LP:
				{
					return "LP";
				}

				case RP:
				{
					return "RP";
				}

				case COMMA:
				{
					return "COMMA";
				}

				case ASSIGN:
				{
					return "ASSIGN";
				}

				case ASSIGN_BITOR:
				{
					return "ASSIGN_BITOR";
				}

				case ASSIGN_BITXOR:
				{
					return "ASSIGN_BITXOR";
				}

				case ASSIGN_BITAND:
				{
					return "ASSIGN_BITAND";
				}

				case ASSIGN_LSH:
				{
					return "ASSIGN_LSH";
				}

				case ASSIGN_RSH:
				{
					return "ASSIGN_RSH";
				}

				case ASSIGN_URSH:
				{
					return "ASSIGN_URSH";
				}

				case ASSIGN_ADD:
				{
					return "ASSIGN_ADD";
				}

				case ASSIGN_SUB:
				{
					return "ASSIGN_SUB";
				}

				case ASSIGN_MUL:
				{
					return "ASSIGN_MUL";
				}

				case ASSIGN_DIV:
				{
					return "ASSIGN_DIV";
				}

				case ASSIGN_MOD:
				{
					return "ASSIGN_MOD";
				}

				case HOOK:
				{
					return "HOOK";
				}

				case COLON:
				{
					return "COLON";
				}

				case OR:
				{
					return "OR";
				}

				case AND:
				{
					return "AND";
				}

				case INC:
				{
					return "INC";
				}

				case DEC:
				{
					return "DEC";
				}

				case DOT:
				{
					return "DOT";
				}

				case FUNCTION:
				{
					return "FUNCTION";
				}

				case EXPORT:
				{
					return "EXPORT";
				}

				case IMPORT:
				{
					return "IMPORT";
				}

				case IF:
				{
					return "IF";
				}

				case ELSE:
				{
					return "ELSE";
				}

				case SWITCH:
				{
					return "SWITCH";
				}

				case CASE:
				{
					return "CASE";
				}

				case DEFAULT:
				{
					return "DEFAULT";
				}

				case WHILE:
				{
					return "WHILE";
				}

				case DO:
				{
					return "DO";
				}

				case FOR:
				{
					return "FOR";
				}

				case BREAK:
				{
					return "BREAK";
				}

				case CONTINUE:
				{
					return "CONTINUE";
				}

				case VAR:
				{
					return "VAR";
				}

				case WITH:
				{
					return "WITH";
				}

				case CATCH:
				{
					return "CATCH";
				}

				case FINALLY:
				{
					return "FINALLY";
				}

				case VOID:
				{
					return "VOID";
				}

				case RESERVED:
				{
					return "RESERVED";
				}

				case EMPTY:
				{
					return "EMPTY";
				}

				case BLOCK:
				{
					return "BLOCK";
				}

				case LABEL:
				{
					return "LABEL";
				}

				case TARGET:
				{
					return "TARGET";
				}

				case LOOP:
				{
					return "LOOP";
				}

				case EXPR_VOID:
				{
					return "EXPR_VOID";
				}

				case EXPR_RESULT:
				{
					return "EXPR_RESULT";
				}

				case JSR:
				{
					return "JSR";
				}

				case SCRIPT:
				{
					return "SCRIPT";
				}

				case TYPEOFNAME:
				{
					return "TYPEOFNAME";
				}

				case USE_STACK:
				{
					return "USE_STACK";
				}

				case SETPROP_OP:
				{
					return "SETPROP_OP";
				}

				case SETELEM_OP:
				{
					return "SETELEM_OP";
				}

				case LOCAL_BLOCK:
				{
					return "LOCAL_BLOCK";
				}

				case SET_REF_OP:
				{
					return "SET_REF_OP";
				}

				case DOTDOT:
				{
					return "DOTDOT";
				}

				case COLONCOLON:
				{
					return "COLONCOLON";
				}

				case XML:
				{
					return "XML";
				}

				case DOTQUERY:
				{
					return "DOTQUERY";
				}

				case XMLATTR:
				{
					return "XMLATTR";
				}

				case XMLEND:
				{
					return "XMLEND";
				}

				case TO_OBJECT:
				{
					return "TO_OBJECT";
				}

				case TO_DOUBLE:
				{
					return "TO_DOUBLE";
				}

				case GET:
				{
					return "GET";
				}

				case SET:
				{
					return "SET";
				}

				case LET:
				{
					return "LET";
				}

				case YIELD:
				{
					return "YIELD";
				}

				case CONST:
				{
					return "CONST";
				}

				case SETCONST:
				{
					return "SETCONST";
				}

				case ARRAYCOMP:
				{
					return "ARRAYCOMP";
				}

				case WITHEXPR:
				{
					return "WITHEXPR";
				}

				case LETEXPR:
				{
					return "LETEXPR";
				}

				case DEBUGGER:
				{
					return "DEBUGGER";
				}

				case COMMENT:
				{
					return "COMMENT";
				}

				case GENEXPR:
				{
					return "GENEXPR";
				}
			}
			// Token without name
			throw new InvalidOperationException(token.ToString());
		}

		/// <summary>
		/// Convert a keyword token to a name string for use with the
		/// <see cref="Context.FEATURE_RESERVED_KEYWORD_AS_IDENTIFIER">Context.FEATURE_RESERVED_KEYWORD_AS_IDENTIFIER</see>
		/// feature.
		/// </summary>
		/// <param name="token">A token</param>
		/// <returns>the corresponding name string</returns>
		public static string KeywordToName(int token)
		{
			switch (token)
			{
				case Token.BREAK:
				{
					return "break";
				}

				case Token.CASE:
				{
					return "case";
				}

				case Token.CONTINUE:
				{
					return "continue";
				}

				case Token.DEFAULT:
				{
					return "default";
				}

				case Token.DELPROP:
				{
					return "delete";
				}

				case Token.DO:
				{
					return "do";
				}

				case Token.ELSE:
				{
					return "else";
				}

				case Token.FALSE:
				{
					return "false";
				}

				case Token.FOR:
				{
					return "for";
				}

				case Token.FUNCTION:
				{
					return "function";
				}

				case Token.IF:
				{
					return "if";
				}

				case Token.IN:
				{
					return "in";
				}

				case Token.LET:
				{
					return "let";
				}

				case Token.NEW:
				{
					return "new";
				}

				case Token.NULL:
				{
					return "null";
				}

				case Token.RETURN:
				{
					return "return";
				}

				case Token.SWITCH:
				{
					return "switch";
				}

				case Token.THIS:
				{
					return "this";
				}

				case Token.TRUE:
				{
					return "true";
				}

				case Token.TYPEOF:
				{
					return "typeof";
				}

				case Token.VAR:
				{
					return "var";
				}

				case Token.VOID:
				{
					return "void";
				}

				case Token.WHILE:
				{
					return "while";
				}

				case Token.WITH:
				{
					return "with";
				}

				case Token.YIELD:
				{
					return "yield";
				}

				case Token.CATCH:
				{
					return "catch";
				}

				case Token.CONST:
				{
					return "const";
				}

				case Token.DEBUGGER:
				{
					return "debugger";
				}

				case Token.FINALLY:
				{
					return "finally";
				}

				case Token.INSTANCEOF:
				{
					return "instanceof";
				}

				case Token.THROW:
				{
					return "throw";
				}

				case Token.TRY:
				{
					return "try";
				}

				default:
				{
					return null;
				}
			}
		}

		/// <summary>Return true if the passed code is a valid Token constant.</summary>
		/// <remarks>Return true if the passed code is a valid Token constant.</remarks>
		/// <param name="code">a potential token code</param>
		/// <returns>true if it's a known token</returns>
		public static bool IsValidToken(int code)
		{
			return code >= ERROR && code <= LAST_TOKEN;
		}
	}
}
