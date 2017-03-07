using System;
using System.Collections;
using Microsoft.VisualStudio.Package;

namespace at.jku.ssw.Coco.VSPlugin.Language {
    /// <summary>
    /// The scanner is used to do the highlighting of coco (and also C#) keywords, strings and comments
    /// </summary>
    internal class CocoScanner : IScanner {
        private string source;
        private int offset;
        private readonly Hashtable keywords;

        public CocoScanner() {            
            string[] kwarr = {
                  "COMPILER", "IGNORECASE", "CHARACTERS", "TOKENS",
                  "PRAGMAS", "COMMENTS", "FROM", "TO", "NESTED", "IGNORE",
                  "PRODUCTIONS", "END", "ANY", "WEAK", "SYNC", "IF", "CONTEXT",
                  "using", "abstract", "event", "new", "struct", "as", "explicit",
                  "null", "switch", "base", "extern", "object", "this", "bool",
                  "false", "operator", "throw", "break", "finally", "out", "true",
                  "byte", "fixed", "override", "try", "case", "float", "params",
                  "typeof", "catch", "for", "private", "uint", "char", "foreach",
                  "protected", "ulong", "checked", "goto", "public", "unchecked",
                  "class", "if", "readonly", "unsafe", "const", "implicit", "ref",
                  "ushort", "continue", "in", "return", "decimal", "string", "int",
                  "sbyte", "virtual", "default", "interface", "sealed", "volatile",
                  "delegate", "internal", "short", "void", "do", "is", "sizeof",
                  "while", "double", "lock", "stackalloc", "else", "long", "static"
                  , "enum", "namespace" };

            keywords = new Hashtable();
            foreach (string kw in kwarr) {
                keywords.Add(kw, null);
            }
        }

        #region IScanner Members

        public bool ScanTokenAndProvideInfoAboutIt(TokenInfo tokenInfo,
            ref int state) {
            bool fFoundToken = GetNextToken(offset, tokenInfo, ref state);
            if (fFoundToken) {
                offset = tokenInfo.EndIndex + 1;
            }
            return fFoundToken;
        }

        public void SetSource(string source, int offset) {
            this.source = source;
            this.offset = offset;
        }

        #endregion

        private bool GetNextToken(int startIndex, TokenInfo tokenInfo,
            ref int state) {
            bool bFoundToken = false; // Assume we are done with this line.
            int index = startIndex;

            if (index < source.Length) {
                bFoundToken = true; // We are not done with this line.
                tokenInfo.StartIndex = index;
                char c = source[index];
                char nextc = (index + 1 < source.Length) ? source[index + 1] : '\0';

                if (c == '/' && nextc == '*' || state == 1) {
                    tokenInfo.Type = TokenType.Comment;
                    tokenInfo.Color = TokenColor.Comment;
                    state = 1;
                    index += 2;

                    while (index + 1 < source.Length
                        && (source[index] != '*' || source[index + 1] != '/'))
                        index++;

                    if (index + 1 < source.Length) {
                        tokenInfo.EndIndex = ++index;
                        state = 0;
                    }
                    else {
                        tokenInfo.EndIndex = ++index;
                    }
                }
                else if (c == '/' && nextc == '/') {
                    tokenInfo.Type = TokenType.Comment;
                    tokenInfo.Color = TokenColor.Comment;
                    tokenInfo.EndIndex = source.Length - 1;
                }
                else if (c == '\'' || c == '"') {
                    tokenInfo.Type = TokenType.String;
                    tokenInfo.Color = TokenColor.String;
                    index++;
                    while (index < source.Length && source[index] != c) {
                        if (source[index] == '\\') index++;
                        index++;
                    }
                    tokenInfo.EndIndex = index;
                }
                else if (Char.IsNumber(c)) {
                    tokenInfo.Type = TokenType.Text;
                    tokenInfo.Color = TokenColor.Number;
                    tokenInfo.EndIndex = index;
                }
                else if (Char.IsWhiteSpace(c)) {
                    do {
                        ++index;
                    } while (index < source.Length
                        && Char.IsWhiteSpace(source[index]));
                    tokenInfo.Type = TokenType.WhiteSpace;
                    tokenInfo.Color = TokenColor.Text;
                    tokenInfo.EndIndex = index - 1;
                }
                else if (Char.IsLetter(c)) {
                    do {
                        ++index;
                    } while (index < source.Length
                        && Char.IsLetterOrDigit(source[index]));
                    
                    string token = source.Substring(tokenInfo.StartIndex,
                        index - tokenInfo.StartIndex);
                    if (keywords.ContainsKey(token)) {
                        tokenInfo.Type = TokenType.Keyword;
                        tokenInfo.Color = TokenColor.Keyword;
                    }
                    else {
                        tokenInfo.Type = TokenType.Identifier;
                        tokenInfo.Color = TokenColor.Identifier;
                    }
                    tokenInfo.EndIndex = index - 1; //changed
                }                
                else {
                    tokenInfo.Type = TokenType.Operator;
                    tokenInfo.Color = TokenColor.Text;
                    tokenInfo.EndIndex = index;
                }
            }
            return bFoundToken;
        }
    }
}