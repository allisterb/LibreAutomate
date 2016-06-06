﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using Catkeys;
using static Catkeys.NoClass;
using Util = Catkeys.Util;
using static Catkeys.Util.NoClass;
using Catkeys.Winapi;

namespace SdkConverter
{
	unsafe partial class Converter
	{
		/// <summary>
		/// Converts simple typedef or function typedef declaration.
		/// _i must be at 'typedef'. Finally it will be at semicolon.
		/// </summary>
		void _DeclareTypedef()
		{
			//detect typedef type
			int typedefType = 0;
			//0 - typedef [struct|enum|etc] X Y...
			//1 - typedef ... (FUNCTYPE)(...)
			//2 - typedef struct|enum|etc [X] [:base] {...} Y...
			int i = ++_i;
			for(; ; i++) {
				char c = *T(i);
				if(c == '{') typedefType = 2; else if(c == '(') typedefType = 1; else if(c != ';') continue;
				break;
			}
			//OutList(_tok[_i], typedefType);

			var d = new _FINDTYPEDATA();

			if(typedefType == 2) {
				var k = _FindKeyword(_i, _KeywordT.TypeDecl);

				//find alias name. _DeclareType() will use it.
				i = _SkipEnclosed(i) + 1;
				if(_TokIsIdent(i) && _TokIsChar(i + 1, ';', ',')) d.inTypedefNameToken = i;

				_DeclareType(ref d);
			} else {
				_FindTypename(false, ref d);
				int iTypeName = _i++;

				if(typedefType == 1) {
					int rtPtr = 0;
					for(; _TokIsChar(_i, '*', '&'); _i++) rtPtr++;

					_DeclareTypedefFunc(d.outSym, rtPtr, d.outIsConst, iTypeName);
					return;
				}
			}

			_Symbol aliasOf = d.outSym;
			int ptr = 0; //0 - start or after comma, >0 pointer level, <0 after identifier
			int ptrBase = 0;

			//don't declare typedef to typedef
			if(typedefType == 0 && aliasOf is _Typedef) {
				var t = aliasOf as _Typedef;
				//Out($"unaliased: {_tok[_i-1]} -> {t.aliasOf.csTypename}");
				aliasOf = t.aliasOf;
				ptrBase = t.ptr;
			}

			for(; ; _i++) {
				if(_TokIsIdent(_i)) {
					if(ptr < 0) goto ge;
					if(_TokIsChar(_i + 1, '[')) { //typedef X Y[n];
						__DeclareTypedef_Array(_ConvertTypeName(aliasOf, ref ptr, false, false, 0));
					} else {
						bool defined = false;
						if(aliasOf is _CppType) {
						} else if(_TokIs(_i, aliasOf.csTypename)) {
							defined = true;
						} else if(_nsCurrent == 0 && aliasOf.forwardDecl && ptrBase + ptr == 0) {
							string alias = _TokToString(_i);
							//if(!(aliasOf is _Struct)) Out(aliasOf); //0 in SDK
							//OutList(aliasOf.csTypename, alias, ptr != 0);
							try { _forwardDeclTypedefs.Add(aliasOf.csTypename, _i); } catch { } //info: try/catch because SDK contains 1 duplicate typedef
						}

						if(!defined) _AddSymbol(_i, new _Typedef(aliasOf, ptr + ptrBase, d.outIsConst));
					}
					ptr = -1;
				} else {
					switch(*T(_i)) {
					case '*':
					case '&':
						if(ptr < 0) goto ge;
						ptr++;
						break;
					case ',':
						if(ptr >= 0) goto ge;
						ptr = 0;
						break;
					case ';':
						if(ptr >= 0) goto ge;
						return;
					default: goto ge;
					}
				}
			}
			ge:
			_Err(_i, "unexpected");
		}

		void __DeclareTypedef_Array(string type)
		{
			int iName = _i++;
			string name = _TokToString(iName);
			string attr = _CArrayToMarshalAsAttribute(); _i--;
			string indexer = "public int this[int i] { get { return a[i]; } set { _Init(); a[i] = value; } }";
			int j = attr.IndexOf('=') + 2; string nElem = attr.Substring(j, attr.Length - j - 2);
			_sbType.AppendFormat("\r\npublic struct {0} {{\r\n"
				+ "\t{1} public {2}[] a;"
				+ " {3}"
				+ " void _Init() {{ if(a == null) a = new {2}[{4}]; }}"
				+ "\r\n}}\r\n",
				name, attr, type, indexer, nElem);
			_AddSymbol(iName, new _Struct(name, false));
			//info:
			//SDK has 6 such typedefs, 2 of them are [1] ie used as variable-length array.
			//We convert to struct, because would be difficult to have this as typedef.
		}

		_Symbol _CopyStruct(_Symbol xFrom, string name)
		{
			var t = _Struct.Copy(xFrom as _Struct, name);
			try { _AddSymbol(_i, t); } catch { return xFrom; } //info: 3 in SDK already defined
			_FormatStruct(t);
			return t;
		}

		_Symbol _CopyEnum(_Symbol xFrom, string name)
		{
			var t = _Enum.Copy(xFrom as _Enum, name);
			try { _AddSymbol(_i, t); } catch { return xFrom; } //info: 0 in SDK already defined
			_FormatEnum(t);
			return t;
		}

		//When 'typedef struct X Y;', and X is forward declaration, we add X=Y to this map. Then, when defining X, we swap names Y with X.
		//Without this, everywhere would be used X, which usually is a tag name such as tagVARIANT.
		//Dictionary<string, List<_Token>> _forwardTypedeclMap = new Dictionary<string, List<_Token>>();
		Dictionary<string, int> _forwardDeclTypedefs = new Dictionary<string, int>();

		/// <summary>
		/// Converts function typedef to C# delegate.
		/// _i must be at the first '(' (after typename and * etc). Finally it will be at semicolon.
		/// Info: script replaced 'typedef TYPE [callConv] FUNCTYPE(' to 'typedef TYPE ([callConv]*FUNCTYPE)('.
		/// </summary>
		/// <param name="t">Return type.</param>
		/// <param name="ptr">Pointer level.</param>
		/// <param name="isConst">Is const.</param>
		/// <param name="iTokTypename">Type name token index.</param>
		/// <param name="inl">Use for inline function type definition.</param>
		void _DeclareTypedefFunc(_Symbol t, int ptr, bool isConst, int iTokTypename, _INLINEFUNCTYPEDATA inl = null)
		{
			//return type
			string returnType = _ConvertTypeName(t, ref ptr, false, isConst, iTokTypename);

			Debug.Assert(_TokIsChar(_i, '('));
			_i++;

			//calling convention
			int iCallConv = 0;
			if(_TokIsIdent(_i)) iCallConv = _i++;
			if(!_TokIsChar(_i, '*')) _Err(_i, "unexpected"); //todo: maybe also can be & (use _TokIsPtr)
			_i++;
			string callConv = iCallConv != 0 ? _ConvertCallConv(iCallConv) : "Cdecl";

			//name
			int iName = _i;
			string name = null;
			if(inl == null) {
				if(_SymbolExists(_i, false)) _Err(_i, "name already exists");
				name = _TokToString(_i);
			} else {
				if(_TokIsIdent(_i)) inl.paramName = _TokToString(_i);
				else { _i--; inl.paramName = "param" + inl.paramIndex; }

				inl.typeName = name = (inl.parentName + "_" + inl.paramName);
			}
			if(!_TokIsChar(++_i, ')')) _Err(_i, "unexpected");
			_i++;

			//start formatting
			StringBuilder sb = inl != null ? _sbInlineDelegate : _sbType;
			sb.AppendLine();
			if(callConv != null) sb.AppendFormat("[UnmanagedFunctionPointer(CallingConvention.{0})]\r\n", callConv);
			sb.AppendFormat("public delegate {0} {1}", returnType, name);

			//parameters
			_ConvertParameters(sb, name);

			var x = new _Callback(name);
			if(inl == null) _AddSymbol(iName, x);
			else _AddSymbol(name, x, iName);

			if(inl != null) {
				inl.t = x;
			}
		}

		class _INLINEFUNCTYPEDATA
		{
			public readonly string parentName; //in
			public int paramIndex; //in
			public string typeName; //out
			public string paramName; //out
			public _Callback t; //out

			public _INLINEFUNCTYPEDATA(string parentName, int paramIndex)
			{
				this.parentName = parentName;
				this.paramIndex = paramIndex;
			}
		}

		/// <summary>
		/// Converts enum definition.
		/// _i must be at 'enum'. Finally it will be at semicolon.
		/// </summary>
		void __DeclareEnum(ref _FINDTYPEDATA d)
		{
			//scoped?
			if(_TokIs(++_i, "class") || _TokIs(_i, "struct")) _i++;

			_Enum x = null;

			//name
			string name = null;
			int iName = 0;
			if(_TokIsIdent(_i)) {
				bool wasForwardDecl;
				if(__MustSwapTagWithAlias(d.inTypedefNameToken, out wasForwardDecl)) {
					iName = d.inTypedefNameToken;
					name = _TokToString(iName);
					_AddSymbol(iName, x = new _Enum(name, false));
					_AddSymbol(_i++, new _Typedef(x, 0, false, wasForwardDecl));
				} else {
					iName = _i++;
					name = _TokToString(iName);

					//is forward declaration? Rare but allowed.
					if(_TokIsChar(_i, ';')) {
						_AddSymbol(iName, d.outSym = new _Enum(name, true));
						//info: don't need name in this case
						return;
					}
				}
			}

			_ns[_nsCurrent + 1].Clear();
			StringBuilder sb = _ns[_nsCurrent + 1].sb;

			//base type
			string sBaseType = null;
			if(_TokIsChar(_i, ':')) {
				_Symbol baseType = _FindType(++_i, true);
				if(0 != _Unalias(_i, ref baseType) || !(baseType is _CppType)) _Err(_i, "unexpected");
				sBaseType = baseType.csTypename;
				_i++;
			}

			if(!_TokIsChar(_i, '{')) _Err(_i, "unexpected");
			int iBodyStart = _i++;
			bool isFlags = false;

			if(iName == 0) {
				iName = d.inTypedefNameToken;
				if(iName == 0) {
					//borrow enum name from first member. Benefits: easier for users to find the enum; will not be a name conflict; easy.
					if(!_TokIsIdent(_i)) _Err(_i, "unexpected");
					iName = _i;
					//Out(_TokToString(iName));
				}
				name = _TokToString(iName);
			}

			//body
			sb.AppendLine(" {");
			int iMember = 0, iPrevMember = 0;
			for(; ; _i++) {
				char* s = T(_i);
				if(*s == '}') break;
				if(*s == '\0') _Err(iBodyStart, "no }");

				if(iMember == 0) {
					if(!_TokIsIdent(_i)) _Err(_i, "unexpected");
					iMember = _i;
					sb.Append('\t'); sb.Append(_TokToString(iMember));
					if(_TokIsChar(_i + 1, '=')) {
						_i++;

						if(_TokIsChar(_i + 1, '(')) {
							if(_TokIsChar(_i + 3, '+') && _tok[_i + 2].Equals(_tok[iPrevMember]) && _TokIs(_i + 4, "1") && _TokIsChar(_i + 5, ')') && _TokIsChar(_i + 6, ',', '}')) {
								//remove value if Member = (PrevMember+1)
								_i += 5;
								continue;
							} else {
								//remove cast from '(int)0x...'
								if(_TokIsChar(_i + 3, ')') && _TokIs(_i + 2, "int") && _TokStarts(_i + 4, "0x")) {
									_i += 3;
								}
								//Out(_tok[iMember]);
							}
						}

						sb.Append(" =");
					} else if(!_TokIsChar(_i + 1, ',', '}')) _Err(_i, "unexpected");
				} else if(*s == ',') {
					iPrevMember = iMember;
					iMember = 0;
					if(!_TokIsChar(_i + 1, '}')) sb.AppendLine(",");
				} else {
					if(sBaseType == null && *s == '0' && (s[1] == 'x' || s[1] == 'X')) { isFlags = true; sBaseType = "uint"; }

					//sb.Append(' '); //splits '<<' etc
					if(_i == iMember + 2) sb.Append(' ');
					sb.Append(_TokToString(_i));
				}
			}
			sb.AppendLine("\r\n}");
			int iBodyEnd = ++_i;

			if(sBaseType != null) sb.Insert(0, " :" + sBaseType);

			if(x == null) {
				if(_TryFindSymbolAs(iName, out x, false) && x.forwardDecl == true) x.forwardDecl = false;
				else _AddSymbol(iName, x = new _Enum(name, false));
			}

			sb.CopyTo(0, x.defAfterName = new char[sb.Length], 0, sb.Length);
			x.isFlags = isFlags;

			d.outSym = x;

			_FormatEnum(x);
		}

		void _FormatEnum(_Enum x)
		{
			StringBuilder sb = _sbType;
			sb.AppendLine();
			if(x.isFlags) sb.AppendLine("[Flags]");
			sb.Append("public enum ");
			sb.Append(x.csTypename);
			sb.Append(x.defAfterName);
		}

		/// <summary>
		/// Converts struct/union/class/__interface/enum/typedef definition or forward declaration.
		/// _i must be at 'struct' etc. Finally it will be after }. If skipVariablesUntilSemicolon - at ;.
		/// </summary>
		void _DeclareType(ref _FINDTYPEDATA d, bool skipVariablesUntilSemicolon = false)
		{
			//struct, union, class, or __interface?
			bool isUnion = false, isInterface = false, isClass = false;
			string access = "public";
			char keyword = *T(_i);
			switch(keyword) {
			case 't': _DeclareTypedef(); return;
			case 'e': __DeclareEnum(ref d); return;
			case 'u': isUnion = true; break;
			case 'c': isClass = true; access = "private"; break;
			case '_': isInterface = true; break;
			}
			_i++;

			//uuid (was '__declspec(uuid', replaced in script)
			string uuid = null;
			if(_TokIs(_i, "uuid")) {
				if(!_TokIsChar(++_i, '(') || !_TokIsChar(++_i, '\"') || !_TokIsChar(_i + 1, ')')) _Err(_i, "unexpected");
				uuid = _TokToString(_i);
				_i += 2;
			}

			//name
			_Struct x = null;
			int iName = 0;
			string name = null;
			if(_TokIsIdent(_i)) {
				bool isThisForwardDecl = _TokIsChar(_i + 1, ';');

				//look in _forwardDeclTypedefs, maybe need to swap this tag with previously declared alias
				if(d.inTypedefNameToken == 0 && _nsCurrent == 0 && !isThisForwardDecl) {
					string tag = _TokToString(_i); int k;
					if(_forwardDeclTypedefs.TryGetValue(tag, out k)) {
						//Out($"<><c 0xff0000>{tag}, {_TokToString(k)}</c>");
						d.inTypedefNameToken = k;
						_forwardDeclTypedefs.Remove(tag);
					}
				}

				bool wasForwardDecl;
				if(!isThisForwardDecl && __MustSwapTagWithAlias(d.inTypedefNameToken, out wasForwardDecl)) {
					iName = d.inTypedefNameToken;
					name = _TokToString(iName);
					_AddSymbol(iName, x = new _Struct(name, false));
					_AddSymbol(_i++, new _Typedef(x, 0, false, wasForwardDecl));
				} else {
					iName = _i++;
					name = _TokToString(iName);

					//forward-declare this because may contain a member of this pointer type
					if(!(_TryFindSymbolAs(iName, out x, false) && x.forwardDecl)) //info: if !x.forwardDecl _AddSymbol will throw
						_AddSymbol(iName, x = new _Struct(name, isThisForwardDecl));

					if(isThisForwardDecl) {
						d.outSym = x;
						x.isInterface = isInterface;
						x.isClass = isClass;
						//info: don't need name in this case
						//if(uuid != null) Out(isClass); //all True
						if(isClass && uuid != null && _nsCurrent == 0) { //coclass
																		 //OutList(keyword, name, uuid);
							_DeclareGuid("CLSID_", name, uuid);
							_sbInterface.AppendFormat(
								"\r\n[ComImport, Guid({0}), ClassInterface(ClassInterfaceType.None)]"
								+ "\r\npublic class {1} {{}}\r\n"
								, uuid, name);
						}
						return;
					}
				}
			} else if(d.inTypedefNameToken != 0) {
				iName = d.inTypedefNameToken;
				name = _TokToString(iName);
			} //later will get name from variable

			//skip IUnknown
			if(!__haveIUnknown && (isInterface || uuid != null) && name == "IUnknown") {
				__haveIUnknown = true;
				_SkipStatement(true);
				x.forwardDecl = false;
				_DeclareGuid("IID_", name, uuid);
				return;
			}

			//is interface?
			if(__haveIUnknown && (isInterface || uuid != null) && _TokIsChar(_i, ':')) {
				//info: SDK has only few non-interface struct that have a base, and they don't have uuid.
				isInterface = true;
				_DeclareGuid("IID_", name, uuid);
			}

			//get empty namespace (symbols + StringBuilder) from stack
			_ns[_nsCurrent + 1].Clear();
			StringBuilder sb = _ns[_nsCurrent + 1].sb;

			//base type
			if(_TokIsChar(_i, ':')) {
				_i++;
				if(_TokIsIdent(_i + 1) && _FindKeyword(_i).kwType == _KeywordT.PubPrivProt) _i++;

				bool addedBaseMembers = false;
				for(; ; _i++) { //support multiple inheritance
					_Symbol baseType = _FindType(_i, true);
					int ptr = _Unalias(_i, ref baseType);
					var bt = baseType as _Struct;
					if(bt == null || bt.forwardDecl || ptr != 0) _Err(_i, "unexpected");
					if(bt.members != null) { //null if IUnknown or no members
						if(bt.members[0] != '/') {
							sb.Append("// ");
							sb.AppendLine(bt.csTypename);
						}
						sb.Append(bt.members);
						addedBaseMembers = true;
					}
					if(!_TokIsChar(++_i, ',')) break;
				}
				if(addedBaseMembers) { sb.Append("// "); sb.AppendLine(name); }
			}

			//members
			if(!_TokIsChar(_i, '{')) _Err(_i, "unexpected");
			int iBodyStart = _i++;
			string memberAttr = isUnion ? "[FieldOffset(0)] " : null;
			bool hasBitfields = false;

			if(_nsCurrent == 0) _anonymousStructSuffixCounter = 0;
			if(!isInterface) _sbType = _ns[++_nsCurrent].sb;

			for(int iMember = 1, iBitfields = 1, iInterfaceFunc=1; !_TokIsChar(_i, '}'); _i++, iMember++) {

				//public/private/protected?
				if(_TokIsChar(_i + 1, ':') && 0 != _TokIs(_i, "public", "private", "protected")) {
					access = _TokToString(_i);
					_i++;
					continue;
				}

				//template etc?
				if(_TokIs(_i, "template")) {
					_SkipStatement();
					continue;
				}

				//interface function?
				if(isInterface) {
					_InterfaceFunction(sb, iInterfaceFunc++);
					continue;
				}

				//bitfield?
				if(__IsBitfield()) {
					_Bitfields(ref iBitfields, memberAttr);
					hasBitfields = true;
					continue;
				}

				//type
				_PARAMDATA t;
				_ParseParamOrMember(true, out t, "TYPEOF", iMember);

				if(t.isNestedTypeDefinitionWithoutVariables) {
					//if anonymous type definition without a variable, its members are part of parent in C++, therefore in C# need to add a variable of that type
					if(t.isAnonymousTypeDefinitionWithoutVariables) {
						sb.AppendFormat("{3}{2} {0} {1};\r\n", t.typeName, "_" + iMember, access, memberAttr);
					}
					continue;
				}

				//member variable
				for(;;) { //support TYPE a, b;
					switch(*T(_i)) {
					case ';': case ',': break;
					case '\0': _Err(iBodyStart, "no }"); break;
					default: _Err(_i, "unexpected"); break;
					}

					if(t.attributes != null) { sb.Append(t.attributes); sb.Append(' '); }
					sb.AppendFormat("{3}{2} {0} {1};\r\n", t.typeName, t.name, access, memberAttr);
					if(!_TokIsChar(_i, ',')) break;
					_i++;

					//ignore *. Assume never will be 'TYPE *a, b;' or 'TYPE a, *b;.
					while(_TokIsChar(_i, '*', '&')) _i++;
					//int ptr = 0;
					//while(_TokIsChar(_i, '*', '&')) { _i++; ptr++; }
					//Out(ptr);

					if(!_TokIsIdent(_i)) _Err(_i, "unexpected");
					t.name = _TokToString(_i++);

					if(_TokIsChar(_i, '[')) {
						t.attributes = _CArrayToMarshalAsAttribute();
						if(!t.typeName.EndsWith_("[]")) t.typeName += "[]";
					} else if(t.attributes != null) {
						t.attributes = null;
						if(t.typeName.EndsWith_("[]")) t.typeName = t.typeName.Remove(t.typeName.Length - 2);
					}
				}
			}

			if(!isInterface) _sbType = _ns[--_nsCurrent].sb;

			_i++; //skip }
			if(skipVariablesUntilSemicolon) {
				//skip variables after }
				while(!_TokIsChar(_i, ';', '\0')) _i++;
			}

			if(iName == 0) {
				if(skipVariablesUntilSemicolon) return; //skip this struct/var definition (it is global 'struct {...} var;')

				//use variable name from 'struct{...}var;', or _anonymousStructSuffixCounter if just 'struct{...};'
				bool isVar = _TokIsIdent(_i);
				name = isVar ? ("TYPEOF_" + _TokToString(_i)) : ("TYPE_" + (++_anonymousStructSuffixCounter).ToString());
				_AddSymbol(name, x = new _Struct(name, false), _i);

				d.outIsAnonymousTypeDefinition = true;
			} else {
				if(x == null) _AddSymbol(name, x = new _Struct(name, false), iName); //typedef struct {...} name;
				else x.forwardDecl = false;
			}

			d.outSym = x;

			if(sb.Length > 0) {
				sb.CopyTo(0, x.members = new char[sb.Length], 0, sb.Length);
				sb.Clear();
			}

			//attributes
			if(isInterface) {
				sb.AppendFormat("[ComImport, Guid({0}), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]\r\n", uuid, name);
				x.isInterface = true;
			} else {
				if(hasBitfields) sb.AppendLine("[DebuggerStepThrough]");
				if(_pack != 8) sb.AppendFormat("[StructLayout(LayoutKind.{0}, Pack={1})]\r\n", isUnion ? "Explicit" : "Sequential", _pack);
				else if(isUnion) sb.AppendLine("[StructLayout(LayoutKind.Explicit)]");
			}
			if(sb.Length > 0) sb.CopyTo(0, x.attributes = new char[sb.Length], 0, sb.Length);

			_FormatStruct(x);
		}

		void _FormatStruct(_Struct x)
		{
			StringBuilder sb = x.isInterface ? _sbInterface : _sbType;
			sb.AppendLine();
			if(x.attributes != null) sb.Append(x.attributes);
			sb.Append(x.isInterface ? "public interface " : "public struct ");
			sb.Append(x.csTypename);
			if(x.members != null) {
				sb.Append(" {\r\n");
				int offs = sb.Length;
				sb.Append(x.members);
				sb.Replace("\n", "\n\t", offs - 2, x.members.Length); //tab-indent members
				sb.AppendLine("}");
			} else sb.AppendLine(" { }");
		}

		//Returns true if must declare aliasToken as this struct|enum, and this struct|enum as aliasToken.
		//For example, it changes 'typedef struct tagX{...} X;' to 'typedef struct X{...} tagX;'.
		bool __MustSwapTagWithAlias(int aliasToken, out bool wasForwardDecl)
		{
			wasForwardDecl = false;
			if(aliasToken == 0) return false;
			if(_nsCurrent != 0) return false;
			if(_tok[_i].Equals(_tok[aliasToken])) return false; //typedef struct X{...}X

			__RemoveForwardDeclIfExists(aliasToken, false);
			wasForwardDecl=__RemoveForwardDeclIfExists(_i, true);

			return true;
		}

		bool __RemoveForwardDeclIfExists(int iTok, bool tag)
		{
			Debug.Assert(_nsCurrent == 0);
			var d = _ns[0].sym;
			_Token token = _tok[iTok];
			_Symbol x;
			if(!d.TryGetValue(token, out x)) return false;
			int ptr = _Unalias(iTok, ref x);
			//OutList(tag, x.GetType(), _TokToString(iTok), x.csTypename, ptr, x.forwardDecl);
			if(!x.forwardDecl || ptr != 0) _Err(iTok, "already exists");
			d.Remove(token);
			return true;
		}

		#region bitfields

		void _Bitfields(ref int iBitfields, string memberAttr)
		{
			int nBitsType = 0, nBitsTypePrev = 0, fieldOffset = 0;
			string csTypeName = null, bitHolderVar = null;
			bool isUnsigned;

			for(;;) {
				//type
				nBitsType = __BitfieldGetType(out csTypeName, out isUnsigned);
				if(nBitsType != nBitsTypePrev) { nBitsTypePrev = nBitsType; bitHolderVar = null; }
				_i++;
				g1:
				//name
				string varName = _TokIsChar(_i, ':') ? null : _TokToString(_i++);
				_i++;
				//field size
				int fieldSize = Api.strtoi(T(_i));
				if(fieldSize < 1 || fieldSize > nBitsType) _Err(_i, "unexpected");
				_i++;
				//add private member variable that holds the bitfields
				if(bitHolderVar == null || fieldOffset + fieldSize > nBitsType) {
					fieldOffset = 0;
					bitHolderVar = "__bf_" + iBitfields++;
					_sbType.AppendFormat("{2}private {0} {1};\r\n", csTypeName, bitHolderVar, memberAttr);
				}
				//add property that get/set the bitfield
				if(varName != null) {
					ulong mask = (1UL << fieldSize) - 1;
					ulong maskWithOffset = mask << fieldOffset;

					//work around C# problems with signed/unsigned conversion
					string op = "~";
					if(nBitsType > 32) {
						if(maskWithOffset > long.MaxValue) { maskWithOffset = ~maskWithOffset; op = null; }
					} else {
						if(maskWithOffset > int.MaxValue) { maskWithOffset = ~maskWithOffset & 0xffffffff; op = null; }
					}

					_sbType.AppendFormat(
						"public {0} {1} {{ get {{ return {8}({2} >> {3} & 0x{4:X}{7}); }} " +
						"set {{ {2} = {8}(({2} & {6}0x{5:X}{7}) | ((value & 0x{4:X}{7}) << {3})); }} }}\r\n",
						csTypeName, varName, bitHolderVar, fieldOffset, mask, maskWithOffset,
						op, isUnsigned ? "U" : "", nBitsType < 32 ? "(" + csTypeName + ")" : "");
				}

				fieldOffset += fieldSize;

				//next bitfield?
				if(_TokIsChar(_i++, ',')) {
					if(!(_TokIsChar(_i + 1, ':') && _TokIsIdent(_i) && _TokIsChar(_i + 3, ';', ','))) _Err(_i, "unexpected");
					goto g1;
				}
				if(!__IsBitfield()) break;
			}

			_i--;
		}

		bool __IsBitfield()
		{
			int i = _i;
			if(_TokIsChar(i + 2, ':')) {
				if(!_TokIsIdent(i++)) return false;
			} else if(!_TokIsChar(i + 1, ':')) return false; //nameless?

			return _TokIsIdent(i) && _IsCharDigit(*T(i + 2)) && _TokIsChar(i + 3, ';', ',');
		}

		/// <summary>
		/// Returns bitfield size in bits. Gets C# type name.
		/// </summary>
		int __BitfieldGetType(out string csTypename, out bool isUnsigned)
		{
			csTypename = null;
			isUnsigned = false;
			_Symbol t = _FindType(_i, true);
			if(0 == _Unalias(_i, ref t)) {
				var ct = t as _CppType;
				if(ct != null && ct.sizeBytesCpp != 0) {
					csTypename = ct.csTypename;
					isUnsigned = ct.isUnsigned;
					return ct.sizeBytesCpp * 8;
				} else if(_TokIs(_i, "IntLong")) {
					csTypename = "long";
					return 64;
					//SDK has 1 such struct (union PSAPI_WORKING_SET_BLOCK), and there the bitfield is at the end, so in most cases it is safe to use C# long instead. Using IntLong is difficult because of its variable size.
				}
			}
			_Err(_i, "unexpected");
			return 0;
		}

		#endregion

		bool __haveIUnknown;

		void _DeclareGuid(string prefix, string name, string uuid)
		{
			if(uuid == null) return;
			_sbInterface.AppendFormat("\r\npublic static Guid {0}{1} = new Guid({2});\r\n", prefix, name, uuid);
			//note: not 'readonly' because then could not be passed as ref.
		}

		void _InterfaceFunction(StringBuilder sb, int index)
		{
			if(_TokIs(_i, "virtual")) _i++;

			var d = new _FINDTYPEDATA();
			_FindTypename(false, ref d);
			_i++;

			//pointer
			int ptr = 0;
			while(_TokIsChar(_i, '*', '&')) { _i++; ptr++; }

			if(_TokIs(_i, "__stdcall")) _i++;

			string name = _TokToString(_i++);

			//some interfaces have IUnknown memners
			bool skip = false;
			if(index<=3) {
				if(index == 1 && name == "QueryInterface") skip = true;
				if(index == 2 && name == "AddRef") skip = true;
				if(index == 3 && name == "Release") skip = true;
			}
			if(skip) {
				_SkipEnclosed();
				_i++;
			} else {
				string returnType = _ConvertTypeName(d.outSym, ref ptr, false, d.outIsConst, d.outTypenameToken);

				//start formatting
				sb.Append("[PreserveSig] ");
				sb.Append(returnType); sb.Append(' '); sb.Append(name);

				//parameters
				_ConvertParameters(sb, name);
			}

			//skip '= 0'
			if(_TokIsChar(_i, '=')) _i += 2;
		}

		/// <summary>
		/// Processes inline forward declaration such as 'struct X' in 'struct X * param' or 'struct X * Func(..)'.
		/// _i must be at 'struct' etc. Finally will be after it (_i++).
		/// Returns _Struct or _Enum, unaliased if was _Typedef.
		/// </summary>
		_Symbol _InlineForwardDeclaration()
		{
			bool en = *T(_i++) == 'e';
			_Symbol x;
			if(_TryFindSymbol(_i, out x, true) && 0 == _Unalias(_i, ref x)) {
				if(en) { if(x is _Enum) return x; } else if(x is _Struct) return x;
			}
			string name = _TokToString(_i);
			if(en) x = new _Enum(name, true); else x = new _Struct(name, true);
			_AddSymbol(_i, x, true);
			return x;
		}

		/// <summary>
		/// Joins _sbType, _sbInlineDelegate, _sbFunc, and _sbInterface into single string.
		/// Replaces some identifiers in it etc.
		/// Clears the stringbuilders.
		/// </summary>
		string _PostProcessTypesFunctionsInterfaces()
		{
			//join types etc into single string, for replacements
			var sb = new StringBuilder();
			sb.Append("\r\n// TYPES\r\n");
			sb.Append(_sbType.ToString()); _sbType.Clear();
			sb.Append(_sbInlineDelegate.ToString()); _sbInlineDelegate.Clear();
			sb.Append("\r\n// FUNCTIONS\r\n");
			sb.Append(_sbFunc.ToString()); _sbFunc.Clear();
			sb.Append("\r\n// INTERFACES\r\n");
			sb.Append(_sbInterface.ToString()); _sbInterface.Clear();
			string R = sb.ToString(); sb.Clear();

			//Perf.First();
			foreach(var v in _ns[0].sym) {
				var sym = v.Value;

				//most forward declarations were converted to definitions, but some don't have definitions; replace their pointers to IntPtr
				if(sym.forwardDecl) {
					var ts = sym as _Struct;
					//if(ts==null) Out(v.Key); //0 in SDK
					if(ts != null && ts.isClass) continue; //all in SDK are with GUID, converted to coclass
					string s = v.Key.ToString();

					//Replace all found TYPE* and ref TYPE to IntPtr.
					//Call RegexReplace_ 2 times because regex (a|b) is very slow.
					string repl = "/*( $0 )*/ IntPtr";
					int n;
					if(ts != null && ts.isInterface) n = R.RegexReplace_(out R, $@"\b{s}\b", repl); //decremented pointer level. 0 in SDK
					else n = R.RegexReplace_(out R, $@"\b{s}\*", repl) + R.RegexReplace_(out R, $@"\bref {s}\b", repl);
					if(n != 0) {
						//Out($"<><c 0xff0000>{n}  {s}* = IntPtr</c>");
						continue;
					}

					//if(_FindIdentifierInString(R, s) < 0) { OutList("no", s); continue; } //5 in SDK
					//OutList(sym.csTypename, sym); //0 in SDK
					continue;
				}

				//process typedefs
				var td = sym as _Typedef;
				if(td != null) {
					if(!td.wasForwardDecl) continue; //don't need to replace. Makes this replacement code 10 times faster.
					if(td.ptr != 0) continue; //the type is or will be resolved in some way
					_Symbol t = td.aliasOf;
					if(t.forwardDecl) continue; //the type is already resolved in some way
					if(!((t is _Struct) || (t is _Enum))) continue;
					string s = v.Key.ToString();
					//if(_FindIdentifierInString(R, s) >= 0) { //makes slower
					//if(ts!=null) _FormatStruct(ts); else _FormatEnum(te);
					if(0 != R.RegexReplace_(out R, $@"\b{s}\b", t.csTypename)) {
						//Out($"<><c 0xff0000>{s} = {t.csTypename}    {t}</c>"); //all struct (0 enum) in SDK
						continue;
					}
					//Out($"<><c 0x8000>{s} = {t.csTypename}    {t}</c>"); //OK, just not used between typedef and struct definition
				}
			}
			//Perf.NextWrite();

			return R;
		}
	}
}