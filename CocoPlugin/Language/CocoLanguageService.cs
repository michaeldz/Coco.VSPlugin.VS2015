using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio;

namespace at.jku.ssw.Coco.VSPlugin.Language {
    /// <summary>
    /// The CocoLanguageService provides support that belong to the custom language.
    /// </summary>
    [ComVisible(true)]
    [Guid(GuidList.guidAttributedGrammarServiceString)]
    public partial class CocoLanguageService : LanguageService {
        #region Constants
        public const string EXTENSION = ".atg";
        public const string NAME = "Coco/R Grammar";
        public const string FORMATLIST = "Coco/R Grammar File (*.atg)\n*.atg";

        #endregion

        #region Variables
        private LanguagePreferences m_preferences;
        private IScanner m_scanner;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the name for the language service.
        /// </summary>
        public override string Name {
            get {
                return NAME;
            }
        }

        #endregion
        
        #region Methods
        public override Source CreateSource(IVsTextLines buffer) {
            return new CocoSource(this, buffer, GetColorizer(buffer));
        }

        public override LanguagePreferences GetLanguagePreferences() {
            if (m_preferences == null) {
                m_preferences = new LanguagePreferences(Site,
                    typeof(CocoLanguageService).GUID, Name);
                m_preferences.Init();
                m_preferences.EnableCommenting = true;
                m_preferences.LineNumbers = true;                
            }
            return m_preferences;
        }

        public override string GetFormatFilterList() {
            return FORMATLIST;
        }
                
        public override Microsoft.VisualStudio.Package.AuthoringScope ParseSource(Microsoft.VisualStudio.Package.ParseRequest req) {                            
            return new CocoAuthoringScope(GetSource(req.View) as CocoSource, req);
        }

        public override IScanner GetScanner(IVsTextLines buffer) {
            if (m_scanner == null) {
                m_scanner = new CocoScanner();
            }
            return m_scanner;
        }

        public override ViewFilter CreateViewFilter(CodeWindowManager mgr, IVsTextView newView) {
            return new CocoViewFilter(mgr, newView);
        }

        public override int GetItemCount(out int piCount) {            
            piCount = 0;
            return VSConstants.E_NOTIMPL;
        }

        public override int GetColorableItem(int iIndex, out IVsColorableItem ppItem) {
            ppItem = null;
            return VSConstants.E_INVALIDARG;
        }

        #endregion     
    }
}
