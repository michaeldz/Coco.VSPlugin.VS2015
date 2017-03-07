using System.Collections.Generic;

namespace at.jku.ssw.Coco.VSPlugin.Language {
    /// <summary>
    /// This class is used to call the customized Coco-parser.
    /// </summary>
    public class CocoParserProxy {
        #region Variables
        private static object locker = new object();        
        #endregion

        #region Methods
        /// <summary>
        /// Parses the input text and provides possible tokens (which are displayed in code-completion requests as declarations).
        /// </summary>
        /// <param name="currentText">The full source text.</param>
        /// <param name="line">The line of the code-completion position (zero-based).</param>
        /// <param name="col">The column of the code-completion position (zero-based).</param>
        /// <param name="filePath">The path to the source-file.</param>
        public static CocoParseResult Parse(string currentText, string filePath) {
            return Parse(currentText, -1, -1, filePath); //not interested in code-completion
        }
        /// <summary>
        /// Parses the input text and provides possible tokens (which are displayed in code-completion requests as declarations).
        /// </summary>
        /// <param name="currentText">The full source text.</param>
        /// <param name="line">The line of the code-completion position (zero-based).</param>
        /// <param name="col">The column of the code-completion position (zero-based).</param>
        /// <param name="filePath">The path to the source-file.</param>
        public static CocoParseResult Parse(string currentText, int line, int col, string filePath) {
            //because coco uses files (and writes to them), parse-operations mustn't run in parallel
            lock (locker) {                
                //using is necessary because a memorystream is an IDisposeable
                using (System.IO.MemoryStream buffer = new System.IO.MemoryStream(System.Text.Encoding.Default.GetBytes(currentText))) {
                    Scanner cocoScanner = new Scanner(buffer);
                    using (Parser cocoParser = new Parser(cocoScanner)) { //the parser was modified to be IDisposeable in order to Dispose the trace-stream inside the parser!
                        cocoParser.InitateCustomParseRequest(filePath, line + 1, col + 1); //+1 because coco line/col count is 1-base, we are zero-based
                        cocoParser.Parse();
                        
                        return new CocoParseResult(cocoParser.PossibleTokens,
                            cocoParser.GrammarName,
                            cocoParser.UserCharSets,
                            cocoParser.UserTokens,
                            cocoParser.UserProductions,
                            cocoParser.References);
                    }
                }
            }
        }
        #endregion

        #region CocoParseResult
        public class CocoParseResult {
            public CocoParseResult(at.jku.ssw.Coco.Parser.CocoToken possibleTokens, string grammarName, IEnumerable<AddTokenInfo> charsets, IEnumerable<AddTokenInfo> tokens, IEnumerable<AddTokenInfo> productions, IDictionary<string, List<AddTokenInfo>> references) {
                PossibleTokens = possibleTokens;
                GrammarName = grammarName;
                CharSets = charsets;
                Tokens = tokens;
                Productions = productions;
                References = references;
            }
            public at.jku.ssw.Coco.Parser.CocoToken PossibleTokens { get; set; }
            public string GrammarName { get; set; }
            public IEnumerable<AddTokenInfo> Tokens { get; set; }
            public IEnumerable<AddTokenInfo> Productions { get; set; }
            public IEnumerable<AddTokenInfo> CharSets { get; set; }
            public IDictionary<string, List<AddTokenInfo>> References { get; set; }
        }
        #endregion
    }
}
