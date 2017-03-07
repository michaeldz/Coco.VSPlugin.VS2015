using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Linq;
using System.Collections.Generic;
using CocoToken = at.jku.ssw.Coco.Parser.CocoToken;
using CocoParseResult = at.jku.ssw.Coco.VSPlugin.Language.CocoParserProxy.CocoParseResult;

namespace at.jku.ssw.Coco.VSPlugin.Language
{
    /// <summary>
    /// Used to implement code-completion, method-selection, member-selection or go-to (needed when "Find all references is clicked") functionality.
    /// Only code-completion is suported so far.
    /// </summary>
    public class CocoAuthoringScope : AuthoringScope {
        public static int tempGlyph = 0;

        #region Constructors
        public CocoAuthoringScope(CocoSource source, ParseRequest req) {
            Source = source;

            HandleRequest(req);
        }
        #endregion

        #region Properties
        protected CocoSource Source { get; set; }
        private CocoDeclarations Declarations { get; set; }
        private TextSpan SourceSpan { get; set; }
        private bool SourceSpanValid { get; set; }
        #endregion

        #region Methods

        #region HandleRequest
        /// <summary>
        /// Handles the causing parse-request.
        /// </summary>
        /// <param name="req">The parse-request.</param>
        protected virtual void HandleRequest(ParseRequest req) {
            // use full source text was in e.g. productions, we wan't to suggest productions which were specfied after the current position            
            string sourceText = req.Text;

            if (string.IsNullOrEmpty(sourceText)) //HACK: empty source text makes problems for coco, so supply at least a whitespace
                sourceText = " ";

            CocoParseResult parseResult;
            // we could do something like auto-parse when the source is parsed, highlight matched braces, get members, get quick info
            // but we are only interested in code completions
            switch (req.Reason) {
                case ParseReason.CompleteWord:                    
                    parseResult = CocoParserProxy.Parse(sourceText, req.Line, req.Col, Source.GetFilePath());

                    //this sets the possible declarations                         
                    CreateDeclarations(parseResult);
                    break;
                case ParseReason.Goto:                    
                    parseResult = CocoParserProxy.Parse(sourceText, Source.GetFilePath());

                    //get selected text (text of selected token)
                    TokenInfo info = Source.GetTokenInfo(req.Line, req.Col);

                    string selectedText = Source.GetText(req.Line, info.StartIndex, req.Line, info.EndIndex + 1);
                    
                    //look if there is a tokeninfo for this selected text
                    AddTokenInfo addInfo = FindTokenInfo(parseResult, selectedText);

                    TextSpan span = new TextSpan();
                    
                    //when there is a tokeninfo for it, set the span where to navigate within the current file
                    if (addInfo != null) {
                        int length = addInfo.Name == null ? 0 : addInfo.Name.Length;
                        span = new TextSpan();
                        span.iStartLine = span.iEndLine = addInfo.Line - 1;
                        span.iStartIndex = addInfo.Col - 1;
                        span.iEndIndex = addInfo.Col - 1 + length;
                        SourceSpanValid = true;
                    }
                    else {
                        SourceSpanValid = false;
                    }
                    
                    SourceSpan = span;
                                       
                    break;                    
                default:
                    break;
            }
        }
        #endregion

        #region FindTokenInfo
        private AddTokenInfo FindTokenInfo(CocoParserProxy.CocoParseResult result, string text) {
            AddTokenInfo info = null;
            //search first in productions, then in tokens, then in charsets
            info = result.Productions.FirstOrDefault(n => n.Name == text);
            if (info != null)
                return info;
                        
            info = result.Tokens.FirstOrDefault(n => n.Name == text);
            if (info != null)
                return info;
            
            info = result.CharSets.FirstOrDefault(n => n.Name == text);
            return info;                
        }
        #endregion

        #region CreateDeclarations
        /// <summary>
        /// Creates the declarations and stores them to the property Declarations
        /// </summary>        
        /// <param name="parseResult">The result of the parsing-process.</param>
        /// <exception cref="ArgumentNullException">parseResult</exception>
        protected virtual void CreateDeclarations(CocoParseResult parseResult) {
            if (parseResult == null)
                throw new System.ArgumentNullException("parseResult");

            List<CocoDeclaration> decls = new List<CocoDeclaration>();

            IEnumerable<string> charsets = parseResult.CharSets.Select<AddTokenInfo, string>(n => n.Name);
            IEnumerable<string> tokens = parseResult.Tokens.Select<AddTokenInfo, string>(n => n.Name);
            IEnumerable<string> productions = parseResult.Productions.Select<AddTokenInfo, string>(n => n.Name);

            #region Add charsets, tokens and productions which were specified by the user
            if ((parseResult.PossibleTokens & CocoToken.Existing_Token_Decl) > 0) {
                parseResult.PossibleTokens ^= CocoToken.Existing_Token_Decl;
                foreach (string userToken in tokens) {
                    decls.Add(CocoDeclarations.CreateDeclaration("Token", userToken, CocoDeclarations.GLYPH_VAR));                 
                }
            }
            if ((parseResult.PossibleTokens & CocoToken.Existing_Character_Decl) > 0) {
                parseResult.PossibleTokens ^= CocoToken.Existing_Character_Decl;
                foreach (string userCharSet in charsets) {
                    decls.Add(CocoDeclarations.CreateDeclaration("Charset", userCharSet, CocoDeclarations.GLYPH_VAR));
                }
            }
            if ((parseResult.PossibleTokens & CocoToken.Existing_Production_Decl) > 0) {
                parseResult.PossibleTokens ^= CocoToken.Existing_Production_Decl;
                foreach (string userProduction in productions) {
                    decls.Add(CocoDeclarations.CreateDeclaration("Production", userProduction, CocoDeclarations.GLYPH_VAR));
                }
            }
            if ((parseResult.PossibleTokens & CocoToken.GrammarName) > 0) {
                parseResult.PossibleTokens ^= CocoToken.GrammarName;
                decls.Add(CocoDeclarations.CreateDeclaration("Grammar name", parseResult.GrammarName, CocoDeclarations.GLYPH_VAR));
            }
            #endregion

            //add all other tokens which are possible at the current position
            foreach (long lv in System.Enum.GetValues(typeof(CocoToken))) {
                if ((parseResult.PossibleTokens & (CocoToken)lv) > 0)
                    decls.Add(CocoDeclarations.GetPredefinedDeclaration((CocoToken)lv));
            }

            //sort the list
            decls.Sort();
            
            Declarations = new CocoDeclarations(decls);
        }
        #endregion
                
        #region AuthoringScope members
        public override string GetDataTipText(int line, int col,
          out TextSpan span) {
            span = new TextSpan();
            return null;
        }

        public override Microsoft.VisualStudio.Package.Declarations GetDeclarations(IVsTextView view, int line,
            int col, TokenInfo info, ParseReason reason) {
            return Declarations;
        }

        public override Methods GetMethods(int line, int col, string name) {
            return null;
        }

        //public override string Goto(Microsoft.VisualStudio.VSConstants cmd, IVsTextView textView, int line, int col, out TextSpan span)
        //{
        //    span = SourceSpan;

        //    //when it's a valid sourcespan, return filepath, otherwise return nothing
        //    if (SourceSpanValid)
        //        return Source.GetFilePath();

        //    //returning nothing doesn't change the caret-position, returning the filename for an invalid source-span would the caret to be set to the beginning of the file
        //    return string.Empty;
        //}

        public override string Goto(VSConstants.VSStd97CmdID cmd, IVsTextView textView, int line, int col, out TextSpan span)
        {
            span = SourceSpan;

            //when it's a valid sourcespan, return filepath, otherwise return nothing
            if (SourceSpanValid)
                return Source.GetFilePath();

            //returning nothing doesn't change the caret-position, returning the filename for an invalid source-span would the caret to be set to the beginning of the file
            return string.Empty;
        }

        #endregion

        #endregion
    }
}