/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System.Collections.Generic;
using CocoToken = at.jku.ssw.Coco.Parser.CocoToken;

namespace at.jku.ssw.Coco.VSPlugin.Language {
    /// <summary>
    /// This class (contained in VS SDK 2010\VisualStudioIntegration\Common\Source\CSharp\Babel) represents a list of delcarations used in code-completion.
    /// </summary>
    public class CocoDeclarations : Microsoft.VisualStudio.Package.Declarations {
        #region Constants
        /// <summary>
        /// The glyph-index is used in declarations for code-completions.
        /// The Language-Service has the derived method GetImageList, which returns a list of images which are also used in code-completions (see CocoDeclarations).
        /// This constant was obtained by going through this image list and look for an image which fits our wishes best (it's the same image as used in C# for code-snippets -> e.g. "namespace", "foreach").
        /// </summary>
        public const int GLYPH_SNIPPET = 205;
        /// <summary>
        /// The glyph-index is used in declarations for code-completions.
        /// The Language-Service has the derived method GetImageList, which returns a list of images which are also used in code-completions (see CocoDeclarations).
        /// This constant was obtained by going through this image list and look for an image which fits our wishes best (it's the same image as used in C# for local variables).
        /// </summary>
        public const int GLYPH_VAR = 42;
        /// <summary>
        /// The glyph-index is used in declarations for code-completions.
        /// The Language-Service has the derived method GetImageList, which returns a list of images which are also used in code-completions (see CocoDeclarations).
        /// This constant was obtained by going through this image list and look for an image which fits our wishes best (it's the same image as used for results displayed in the 'find references window').
        /// </summary>
        public const int GLYPH_USAGE = 208;
        #endregion

        #region Variables
        //list of all possible declarations (dynamic declarations like user-specified tokens are not handled here)
        private static IDictionary<CocoToken, CocoDeclaration> predefinedDecls;
        #endregion

        #region Static Initializers
        static CocoDeclarations() {
            //add all static declarations
            predefinedDecls = new Dictionary<CocoToken, CocoDeclaration>(34);
            AddDeclaration(CocoToken._EMPTY);
            AddDeclaration(CocoToken.Compiler);
            AddDeclaration(CocoToken.IgnoreCase);
            AddDeclaration(CocoToken.Characters);
            AddDeclaration(CocoToken.Tokens);
            AddDeclaration(CocoToken.Pragmas);
            AddDeclaration(CocoToken.Comments);
            AddDeclaration(CocoToken.Comments_From);
            AddDeclaration(CocoToken.Comments_To);
            AddDeclaration(CocoToken.Comments_Nested);
            AddDeclaration(CocoToken.Context);
            AddDeclaration(CocoToken.Ignore);
            AddDeclaration(CocoToken.Any);
            AddDeclaration(CocoToken.Plus);
            AddDeclaration(CocoToken.Minus);
            AddDeclaration(CocoToken.TokenRange);
            AddDeclaration(CocoToken.Productions);
            AddDeclaration(CocoToken.Period);
            AddDeclaration(CocoToken.Existing_Character_Decl);
            AddDeclaration(CocoToken.Existing_Token_Decl);
            AddDeclaration(CocoToken.LPar);
            AddDeclaration(CocoToken.RPar);
            AddDeclaration(CocoToken.LBrack);
            AddDeclaration(CocoToken.RBrack);
            AddDeclaration(CocoToken.LBrace);
            AddDeclaration(CocoToken.RBrace);
            AddDeclaration(CocoToken.Alternative);
            AddDeclaration(CocoToken.SemanticActionStart);
            AddDeclaration(CocoToken.AttributeStart_1);
            AddDeclaration(CocoToken.AttributeStart_2);
            AddDeclaration(CocoToken.Sync);
            AddDeclaration(CocoToken.Weak);
            AddDeclaration(CocoToken.If);
            AddDeclaration(CocoToken.End);
        }

        private static void AddDeclaration(CocoToken type) {
            predefinedDecls.Add(new KeyValuePair<CocoToken, CocoDeclaration>(type, CreateDeclaration(type)));
        }

        private static CocoDeclaration CreateDelcarationKW(string description, string keyword) {
            return new CocoDeclaration(description, keyword, GLYPH_SNIPPET, keyword);
        }

        /// <summary>
        /// Creates the declaration for a token.
        /// </summary>
        /// <param name="type">The token.</param>
        /// <returns>The declaration for the given token.</returns>
        public static CocoDeclaration CreateDeclaration(CocoToken type) {                        
            switch (type) {
                case CocoToken.Compiler:
                    return CreateDelcarationKW(Resources.KW_Compiler, "COMPILER");                    
                case CocoToken.IgnoreCase:
                    return CreateDelcarationKW(Resources.KW_IgnoreCase, "IGNORECASE");                    
                case CocoToken.Characters:
                    return CreateDelcarationKW(Resources.KW_Characters, "CHARACTERS");                    
                case CocoToken.Tokens:
                    return CreateDelcarationKW(Resources.KW_Tokens, "TOKENS");                    
                case CocoToken.Pragmas:
                    return CreateDelcarationKW(Resources.KW_Pragmas, "PRAGMAS");                    
                case CocoToken.Comments:
                    return CreateDelcarationKW(Resources.KW_Comments, "COMMENTS");                    
                case CocoToken.Comments_From:
                    return CreateDelcarationKW(Resources.KW_From, "FROM");                    
                case CocoToken.Comments_To:
                    return CreateDelcarationKW(Resources.KW_To, "TO");                    
                case CocoToken.Comments_Nested:
                    return CreateDelcarationKW(Resources.KW_Nested, "NESTED");                    
                case CocoToken.Context:
                    return CreateDelcarationKW(Resources.KW_Context, "CONTEXT");                    
                case CocoToken.Ignore:
                    return CreateDelcarationKW(Resources.KW_Ignore, "IGNORE");                    
                case CocoToken.Any:
                    return CreateDelcarationKW(Resources.KW_Any, "ANY");                    
                case CocoToken.Plus:
                    return CreateDelcarationKW(Resources.KW_Plus, "+");                    
                case CocoToken.Minus:
                    return CreateDelcarationKW(Resources.KW_Minus, "-");                    
                case CocoToken.TokenRange:
                    return CreateDelcarationKW(Resources.KW_TokenRange, "..");                    
                case CocoToken.Productions:
                    return CreateDelcarationKW(Resources.KW_Productions, "PRODUCTIONS");                    
                case CocoToken.Period:
                    return CreateDelcarationKW(Resources.KW_Period, ".");                    
                case CocoToken.LPar:
                    return CreateDelcarationKW(Resources.KW_LPar, "(");                    
                case CocoToken.RPar:
                    return CreateDelcarationKW(Resources.KW_RPar, ")");                    
                case CocoToken.LBrack:
                    return CreateDelcarationKW(Resources.KW_LBrack, "[");                    
                case CocoToken.RBrack:
                    return CreateDelcarationKW(Resources.KW_RBrack, "]");                    
                case CocoToken.LBrace:
                    return CreateDelcarationKW(Resources.KW_LBrace, "{");                    
                case CocoToken.RBrace:
                    return CreateDelcarationKW(Resources.KW_RBrace, "}");                    
                case CocoToken.Alternative:
                    return CreateDelcarationKW(Resources.KW_Alternative, "|");                    
                case CocoToken.SemanticActionStart:
                    return CreateDelcarationKW(Resources.KW_SemActionStart, "(.");                    
                case CocoToken.AttributeStart_1:
                    return CreateDelcarationKW(Resources.KW_AttributeStart1, "<");                    
                case CocoToken.AttributeStart_2:
                    return CreateDelcarationKW(Resources.KW_AttributeStart2, "<.");                    
                case CocoToken.Sync:
                    return CreateDelcarationKW(Resources.KW_Sync, "SYNC");                    
                case CocoToken.Weak:
                    return CreateDelcarationKW(Resources.KW_Weak, "WEAK");                    
                case CocoToken.If:
                    return CreateDelcarationKW(Resources.KW_If, "IF");                    
                case CocoToken.End:
                    return CreateDelcarationKW(Resources.KW_End, "END");                    
                default:
                    return new CocoDeclaration("", "", 0, "");
            }            
        }
        #endregion

        #region CreateDeclaration
        /// <summary>
        /// Creates a declaration.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <param name="name">The name to use.</param>
        /// <param name="glyph">The glyph-index.</param>
        /// <returns>A new declaration.</returns>
        public static CocoDeclaration CreateDeclaration(string description, string name, int glyph) {
            return new CocoDeclaration(description, name, glyph, name);
        }
        #endregion

        #region GetPredefinedDeclaration
        /// <summary>
        /// Gets a predefined CocoDeclaration from the static dictionary.
        /// </summary>
        /// <param name="type">The type of the declaration to get.</param>
        /// <returns></returns>
        public static CocoDeclaration GetPredefinedDeclaration(CocoToken type) {
            if (predefinedDecls.ContainsKey(type))
                return predefinedDecls[type];

            return predefinedDecls[CocoToken._EMPTY];
        }
        #endregion

        #region Original source
        IList<CocoDeclaration> declarations;
        public CocoDeclarations(IList<CocoDeclaration> declarations) {
            this.declarations = declarations;
        }

        public override int GetCount() {
            return declarations.Count;
        }

        public override string GetDescription(int index) {
            return declarations[index].Description;
        }

        public override string GetDisplayText(int index) {
            return declarations[index].DisplayText;
        }

        public override int GetGlyph(int index) {
            return declarations[index].Glyph;
        }

        public override string GetName(int index) {
            if (index >= 0)
                return declarations[index].Name;

            return null;
        }
        #endregion
    }
}