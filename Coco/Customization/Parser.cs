using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace at.jku.ssw.Coco {
    /// <summary>
    /// Additional (plugin-specifiy) things which need to be handled inside the parsing-process are handled by this class.
    /// This classes is mainly used to implement code-completion.
    /// </summary>
    public partial class Parser : IDisposable {
        #region Enumerations
        /// <summary>
        /// Represents coco tokens (keywords and placeholders for user-declared tokens, user-declared productions or grammar name).
        /// </summary>
        public enum CocoToken : long {
            _EMPTY = 0,
            Compiler = 2,
            IgnoreCase = 4,
            Characters = 8,
            Tokens = 16,
            Pragmas = 32,
            Comments = 64,
            Comments_From = 128,
            Comments_To = 256,
            Comments_Nested = 512,
            Context = 1024,
            Ignore = 2048,
            Any = 4096,
            Plus = 8192,
            Minus = 16384,
            TokenRange = 32768, // '..'
            Productions = 65536,
            Period = 131072,
            Existing_Character_Decl = 262144,
            Existing_Token_Decl = 524288,
            LPar = 1048576,
            RPar = 2097152,
            LBrack = 4194304,
            RBrack = 8388608,
            LBrace = 16777216,
            RBrace = 33554432,
            Alternative = 67108864,
            SemanticActionStart = 134217728,
            AttributeStart_1 = 268435456,
            AttributeStart_2 = 536870912,
            Existing_Production_Decl = 1073741824,
            Sync = 2147483648,
            Weak = 4294967296,
            If = 8589934592,
            GrammarName = 17179869184,
            End = 34359738368
        }
        #endregion

        #region Variables
        private bool generateCode = true; // code generation can be disabled (in order to only create symbol-table) -> this flag needs to be handled via the orginal Parser-class!
        private bool customizedParsing = false; //indicates that the actual purpose is a custom parse-process (e.g. code completion)
        private string currentContext; //this is the name of the current production/token/charset
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the determined grammar name.
        /// </summary>
        public virtual string GrammarName { get; protected set; }
        /// <summary>
        /// Gets or sets all charsets which were specified in the grammar file.
        /// </summary>
        public virtual IList<AddTokenInfo> UserCharSets { get; protected set; }
        /// <summary>
        /// Gets or sets all tokens which were specified in the grammar file.
        /// </summary>
        public virtual IList<AddTokenInfo> UserTokens { get; protected set; }
        /// <summary>
        /// Gets or sets all productions which were specified in the grammar file.
        /// </summary>
        public virtual IList<AddTokenInfo> UserProductions { get; protected set; }
        /// <summary>
        /// Gets or sets the list of opened braces. The values in this list represent the long-representation of the brace (of the corresponding CocoToken).
        /// </summary>
        protected List<long> OpenedBraces { get; set; }
        /// <summary>
        /// Gets or sets the determined position in the grammar ifle where we should provide possible tokens.
        /// This property is modified by the Notify-method.
        /// </summary>        
        protected int DeterminedGrammarPos { get; set; }
        /// <summary>
        /// Gets or sets the target line for which to get possible tokens.
        /// </summary>
        protected int TargetLine { get; set; }
        /// <summary>
        /// Gets or sets the target column for which to get possible tokens.
        /// </summary>
        protected int TargetCol { get; set; }
        /// <summary>
        /// Gets the tokens, possible at the place TargetLine, TargetCol.
        /// </summary>
        /// <see cref="TargetLine"/>
        /// /// <see cref="TargetLine"/>
        /// /// <see cref="TargetCol"/>
        /// /// <see cref="InitateCustomParseRequest"/>
        public CocoToken PossibleTokens {
            get {
                return GetTokensForPosition(DeterminedGrammarPos);
            }
        }
        /// <summary>
        /// Gets a dictionary of references there the key is the symbol and the value is a list of additional token infos where this symbol is referenced in the file.        
        /// </summary>
        public IDictionary<string, List<AddTokenInfo>> References { get; private set; } //not fully consistant, as the key is duplicated in every add token info, but do this due to perfromance (but avoid separate class which is very close to AddTokenInfo)
        #endregion
        
        #region Methods

        #region Notify
        /// <summary>
        /// Handles parse-notification. This method is called during the parsing-process when reaching specified positions in the grammar file.
        /// </summary>
        /// <param name="position">The reached position.</param>
        /// <param name="addInfo">Additional information about the reached position.</param>        
        partial void Notify(int position) {
            //do nothing when it's not needed (normal coco-codegeneration, no code-completion request)
            if (!customizedParsing)
                return;

            //if it was a charset, token, productioni or the name of the grammar, store the additional information
            if (position == 2 && !string.IsNullOrEmpty(la.val))
                GrammarName = la.val;
            else if (position == 5 && !string.IsNullOrEmpty(la.val)) {
                UserCharSets.Add(new AddTokenInfo(la.val, la.line, la.col));
                currentContext = la.val;
            }
            else if (position == 10 && !string.IsNullOrEmpty(t.val)) {
                UserTokens.Add(new AddTokenInfo(t.val, t.line, t.col));
                currentContext = t.val;
            }
            else if (position == 25 && !string.IsNullOrEmpty(la.val)) {
                UserProductions.Add(new AddTokenInfo(la.val, la.line, la.col));
                currentContext = la.val;
            }
            else if (position == 50 && !string.IsNullOrEmpty(la.val))
                AddReference(la.val, la.line, la.col, currentContext);
            
            if (errors.count > 0) {
                //grammar must be free of errors until target point (otherwise, it's dangerous that a wrong completion-point gets detected)
                return;
            }

            if (t.line < TargetLine || (t.line == TargetLine && t.col < TargetCol)) {
                //add opened brace (only as long as the current position is before the target position)
                if (position == 12 || position == 30)
                    AddOpenBrace((long)CocoToken.LPar);
                else if (position == 13 || position == 33)
                    RemoveOpenBrace((long)CocoToken.LPar);
                else if (position == 14 || position == 31)
                    AddOpenBrace((long)CocoToken.LBrack);
                else if (position == 15 || position == 34)
                    RemoveOpenBrace((long)CocoToken.LBrack);
                else if (position == 16 || position == 32)
                    AddOpenBrace((long)CocoToken.LBrace);
                else if (position == 17 || position == 35)
                    RemoveOpenBrace((long)CocoToken.LBrace);


                DeterminedGrammarPos = position;
            }
        }
        #endregion

        #region InitateCustomParseRequest
        /// <summary>
        /// Sets up the parser to the purpose of a custom parse request (e.g. to support code-completion).
        /// </summary>
        /// <param name="srcPath">The path of the source-file.</param>
        /// <param name="frameDir">The directory where the frames are stored.</param>
        /// <param name="outDir">The output directory.</param>
        /// <param name="line">The line (one-based) of the target position.</param>
        /// <param name="col">The column (one-based) of the target position.</param>
        public virtual void InitateCustomParseRequest(string srcPath) {
            InitateCustomParseRequest(srcPath, -1, -1); //-1 indicates that we are not interested in code-completion (property 'PossibleTokens' will not be set)
        }
        /// <summary>
        /// Sets up the parser to the purpose of a custom parse request (e.g. to support code-completion).
        /// </summary>
        /// <param name="srcPath">The path of the source-file.</param>
        /// <param name="frameDir">The directory where the frames are stored.</param>
        /// <param name="outDir">The output directory.</param>
        /// <param name="line">The line (one-based) of the target position.</param>
        /// <param name="col">The column (one-based) of the target position.</param>
        public virtual void InitateCustomParseRequest(string srcPath, int line, int col) {            
            generateCode = false; //suppose is to build symbol-table
                        
            trace = new StreamWriter(new MemoryStream()); //we are not interested            
            tab = new Tab(this);
            dfa = new DFA(this);
            pgen = new ParserGen(this);

            tab.srcName = srcPath;
            tab.srcDir = Path.GetDirectoryName(srcPath);
            tab.nsName = "at.jku.ssw.Coco.VSPlugin.TempNS"; //doesn't matter, no code is generated
            tab.frameDir = string.Empty; //won't be needed because no code-generation is done
            tab.outDir = string.Empty; // wont't be needed because no code-generation is done

            //indicate that initialization occured
            customizedParsing = true;

            //set the target position
            TargetLine = line;
            TargetCol = col;

            DeterminedGrammarPos = -1; //initialize to error position

            //clear (initialize) collected lists of charsets, tokens, productions, braces
            UserCharSets = new List<AddTokenInfo>();
            UserTokens = new List<AddTokenInfo>();
            UserProductions = new List<AddTokenInfo>();
            OpenedBraces = new List<long>();

            References = new Dictionary<string, List<AddTokenInfo>>();
        }
        #endregion

        #region GetTokensForPosition
        /// <summary>
        /// Gets a list of all possible tokens based on a given position in the grammar.
        /// </summary>
        /// <param name="state">The position in the grammar.</param>
        /// <returns>Allpossible tokens.</returns>
        protected virtual CocoToken GetTokensForPosition(int position) {
            CocoToken tokens = CocoToken._EMPTY;

            switch (position) {
                case -1:
                    //error state, keep _EMPTY symbol                    
                    break;
                case 0:
                    tokens |= CocoToken.Compiler;
                    break;
                case 1:
                    tokens |= CocoToken.Compiler;
                    break;
                case 2:
                    break;
                case 3:
                    tokens |= CocoToken.IgnoreCase;
                    tokens |= CocoToken.Characters;
                    tokens |= CocoToken.Tokens;
                    tokens |= CocoToken.Pragmas;
                    tokens |= CocoToken.Comments;
                    tokens |= CocoToken.Ignore;
                    tokens |= CocoToken.Productions;
                    break;
                case 4:
                    tokens |= CocoToken.Characters;
                    tokens |= CocoToken.Tokens;
                    tokens |= CocoToken.Pragmas;
                    tokens |= CocoToken.Comments;
                    tokens |= CocoToken.Ignore;
                    tokens |= CocoToken.Productions;
                    break;
                case 5:
                    break;
                case 6:
                    break;
                case 7:
                    tokens |= CocoToken.Plus;
                    tokens |= CocoToken.Minus;
                    tokens |= CocoToken.TokenRange;                    
                    break;
                case 8:
                    break;
                case 9:
                    tokens |= CocoToken.Tokens;
                    tokens |= CocoToken.Pragmas;
                    tokens |= CocoToken.Comments;
                    tokens |= CocoToken.Ignore;
                    tokens |= CocoToken.Productions;
                    break;
                case 10:
                    break;
                case 11:
                case 12:
                case 13:
                case 14:
                case 15:
                case 16:
                case 17:
                    tokens |= CocoToken.Existing_Character_Decl;
                    tokens |= CocoToken.LPar;
                    tokens |= CocoToken.LBrack;
                    tokens |= CocoToken.LBrace;
                    tokens |= CocoToken.Context;
                    tokens |= CocoToken.Alternative;

                    //add the currently opened brace (only right pendant of the last opened brace will be added)
                    if (IsBraceOpen((long)CocoToken.LPar))
                        tokens |= CocoToken.RPar;
                    else if (IsBraceOpen((long)CocoToken.LBrack))
                        tokens |= CocoToken.RBrack;
                    else if (IsBraceOpen((long)CocoToken.LBrace))
                        tokens |= CocoToken.RBrace;
                    break;
                case 18:
                    tokens |= CocoToken.Pragmas;
                    tokens |= CocoToken.Comments;
                    tokens |= CocoToken.Ignore;
                    tokens |= CocoToken.Productions;
                    break;
                case 19:
                    tokens |= CocoToken.Comments;
                    tokens |= CocoToken.Ignore;
                    tokens |= CocoToken.Productions;
                    break;
                case 20:
                    tokens |= CocoToken.Comments_From;
                    break;
                case 21:
                    tokens |= CocoToken.Comments_To;
                    break;
                case 22:
                    tokens |= CocoToken.Comments_Nested;
                    tokens |= CocoToken.Comments;
                    tokens |= CocoToken.Ignore;
                    tokens |= CocoToken.Productions;
                    break;
                case 23:
                    tokens |= CocoToken.Ignore;
                    tokens |= CocoToken.Productions;
                    break;
                case 25:
                    break;
                case 26:
                    tokens |= CocoToken.AttributeStart_1;
                    tokens |= CocoToken.AttributeStart_2;
                    tokens |= CocoToken.SemanticActionStart;                    
                    break;
                case 27:
                    tokens |= CocoToken.Existing_Token_Decl;
                    tokens |= CocoToken.Existing_Production_Decl;
                    tokens |= CocoToken.Weak;
                    tokens |= CocoToken.LPar;
                    tokens |= CocoToken.LBrack;
                    tokens |= CocoToken.LBrace;
                    tokens |= CocoToken.Any;
                    tokens |= CocoToken.Sync;
                    tokens |= CocoToken.SemanticActionStart;
                    
                    //add the currently opened brace (only right pendant of the last opened brace will be added)
                    if (IsBraceOpen((long)CocoToken.LPar))
                        tokens |= CocoToken.RPar;
                    else if (IsBraceOpen((long)CocoToken.LBrack))
                        tokens |= CocoToken.RBrack;
                    else if (IsBraceOpen((long)CocoToken.LBrace))
                        tokens |= CocoToken.RBrace;
                    break;
                case 28:
                    tokens |= CocoToken.LPar;
                    break;
                case 29:
                case 30:
                case 31:
                case 32:                
                    tokens |= CocoToken.Existing_Token_Decl;
                    tokens |= CocoToken.Existing_Production_Decl;
                    tokens |= CocoToken.Weak;
                    tokens |= CocoToken.LPar;
                    tokens |= CocoToken.LBrack;
                    tokens |= CocoToken.LBrace;
                    tokens |= CocoToken.Any;
                    tokens |= CocoToken.Sync;
                    tokens |= CocoToken.SemanticActionStart;
                    tokens |= CocoToken.If;

                    //add the currently opened brace (only right pendant of the last opened brace will be added)
                    if (IsBraceOpen((long)CocoToken.LPar))
                        tokens |= CocoToken.RPar;
                    else if (IsBraceOpen((long)CocoToken.LBrack))
                        tokens |= CocoToken.RBrack;
                    else if (IsBraceOpen((long)CocoToken.LBrace))
                        tokens |= CocoToken.RBrace;
                    break;
                case 33:
                case 34:
                case 35:
                case 36:
                    tokens |= CocoToken.Existing_Token_Decl;
                    tokens |= CocoToken.Existing_Production_Decl;
                    tokens |= CocoToken.Weak;
                    tokens |= CocoToken.LPar;
                    tokens |= CocoToken.LBrack;
                    tokens |= CocoToken.LBrace;
                    tokens |= CocoToken.Any;
                    tokens |= CocoToken.Sync;
                    tokens |= CocoToken.SemanticActionStart;
                    tokens |= CocoToken.Alternative;

                    //add the currently opened brace (only right pendant of the last opened brace will be added)
                    if (IsBraceOpen((long)CocoToken.LPar))
                        tokens |= CocoToken.RPar;
                    else if (IsBraceOpen((long)CocoToken.LBrack))
                        tokens |= CocoToken.RBrack;
                    else if (IsBraceOpen((long)CocoToken.LBrace))
                        tokens |= CocoToken.RBrace;
                    break;                
                case 37:
                    tokens |= CocoToken.Existing_Token_Decl;
                    tokens |= CocoToken.Existing_Production_Decl;                    
                    tokens |= CocoToken.LPar;
                    tokens |= CocoToken.LBrack;
                    tokens |= CocoToken.LBrace;
                    tokens |= CocoToken.Any;
                    tokens |= CocoToken.Sync;
                    tokens |= CocoToken.SemanticActionStart;

                    //add the currently opened brace (only right pendant of the last opened brace will be added)
                    if (IsBraceOpen((long)CocoToken.LPar))
                        tokens |= CocoToken.RPar;
                    else if (IsBraceOpen((long)CocoToken.LBrack))
                        tokens |= CocoToken.RBrack;
                    else if (IsBraceOpen((long)CocoToken.LBrace))
                        tokens |= CocoToken.RBrace;
                    break;               
                case 38:               
                    tokens |= CocoToken.Existing_Token_Decl;
                    tokens |= CocoToken.Existing_Production_Decl;
                    tokens |= CocoToken.Weak;
                    tokens |= CocoToken.LPar;
                    tokens |= CocoToken.LBrack;
                    tokens |= CocoToken.LBrace;
                    tokens |= CocoToken.Any;
                    tokens |= CocoToken.Sync;
                    tokens |= CocoToken.SemanticActionStart;
                    tokens |= CocoToken.Alternative;

                    //add the currently opened brace (only right pendant of the last opened brace will be added)
                    if (IsBraceOpen((long)CocoToken.LPar))
                        tokens |= CocoToken.RPar;
                    else if (IsBraceOpen((long)CocoToken.LBrack))
                        tokens |= CocoToken.RBrack;
                    else if (IsBraceOpen((long)CocoToken.LBrace))
                        tokens |= CocoToken.RBrace;

                    tokens |= CocoToken.AttributeStart_1;
                    tokens |= CocoToken.AttributeStart_2;
                    break;
                case 39:
                    break;
                case 40:
                    break;
                case 41:
                    tokens |= CocoToken.End;
                    break;
                case 42:
                    tokens |= CocoToken.GrammarName;
                    break;
                case 43:
                    tokens |= CocoToken.Period;
                    break;
                case 44:
                    tokens |= CocoToken.SemanticActionStart;
                    break;
                case 45:
                    //eof
                    break;
                case 46:
                    break;
                default:
                    break;
            }

            return tokens;
        }
        #endregion

        #region Handle Braces
        protected void AddOpenBrace(long brace) {
            OpenedBraces.Add(brace);
        }
        protected void RemoveOpenBrace(long brace) {
            if (IsBraceOpen(brace)) {
                OpenedBraces.RemoveAt(OpenedBraces.Count - 1);
            }
        }
        protected bool IsBraceOpen(long brace) {
            return OpenedBraces.Count > 0 && OpenedBraces.Last() == brace;
        }
        #endregion

        #region HandleReferences
        protected void AddReference(string ident, int line, int col, string additionalInfo) {
            if (!References.ContainsKey(ident))
                References.Add(ident, new List<AddTokenInfo>());
            References[ident].Add(new AddTokenInfo(ident, line, col, additionalInfo));
        }
        #endregion

        #region Dispose
        public void Dispose() {
            //release memory stream which was used for tracings
            this.trace.Dispose();
        }
        #endregion

        #endregion
    }
}