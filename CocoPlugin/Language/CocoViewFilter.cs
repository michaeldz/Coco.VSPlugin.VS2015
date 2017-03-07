using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Package;
using VsCommands = Microsoft.VisualStudio.VSConstants.VSStd97CmdID;
using VsCommands2K = Microsoft.VisualStudio.VSConstants.VSStd2KCmdID;
using VsCommands2010 = Microsoft.VisualStudio.VSConstants.VSStd2010CmdID;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Shell.Interop;

namespace at.jku.ssw.Coco.VSPlugin.Language {
    /// <summary>
    /// A custom viewfilter implementation. It is used to filter the available commands and partially handles the commands.
    /// </summary>
    public class CocoViewFilter : ViewFilter {
        #region Variables
        //we want our contextmenu do not display the "Insert Intellitrace point"-command
        //but there is NOTHING documentated about the corresponding cmdgroup and commandid, so we had to find out ourselfes
        //however, we don't know which type this is, we just know the guid-val and the uinit value for the command-id, so we can not cast to a specifiy enum
        private static readonly Guid guidCmdGroupIntelliTrace = new Guid("c9dd4a59-47fb-11d2-83e7-00c04f9902c1");
        private static readonly int nCmdIdInsertIntellitrace = 65;        
        #endregion

        #region Constructor
        /// <summary>
        /// The default constructor.
        /// </summary>
        /// <param name="mgr">The codewindowmanager to use.</param>
        /// <param name="view">The textview.</param>
        public CocoViewFilter(CodeWindowManager mgr, IVsTextView view)
            : base(mgr, view) {
        }
        #endregion

        #region Methods
        protected override int ExecCommand(ref Guid guidCmdGroup, uint nCmdId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) {
            //find references is not handled by VS automatically, so we have to do that
            if (guidCmdGroup == typeof(VsCommands).GUID && (VsCommands)nCmdId == VsCommands.FindReferences) {                
                HandleFindReferences();
                return VSConstants.S_OK;
            }
            return base.ExecCommand(ref guidCmdGroup, nCmdId, nCmdexecopt, pvaIn, pvaOut);
        }
       
        protected virtual void HandleFindReferences()  {
            int line, col;

            // Get the caret position
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(TextView.GetCaretPos(out line, out col));

            //get selected text (text of selected token)
            TokenInfo info = Source.GetTokenInfo(line, col);            
            string selectedText = Source.GetText(line, info.StartIndex, line, info.EndIndex + 1);
                
            //set the search-criteria            
            VSOBSEARCHCRITERIA2 criteria = new VSOBSEARCHCRITERIA2();            
            criteria.szName = String.Format("{0}/{1}", Source.GetFilePath(),selectedText);
            criteria.dwCustom = Library.Library.DWCUSTOM_FINDREFSEARCH; //indicate that we want to find also references

            //perform the search
            IVsFindSymbol findSymbol = Source.LanguageService.GetService(typeof(SVsObjectSearch)) as IVsFindSymbol;            

            Guid guidCocoLibrary = new Guid(GuidList.guidLibraryString);
            VSOBSEARCHCRITERIA2[] searchCriterias = new VSOBSEARCHCRITERIA2[] { criteria };

            //the search will be handled by the Library-object
            findSymbol.DoSearch(ref guidCocoLibrary, searchCriterias);
        }
        /// <summary>
        /// Handles the Goto-command.
        /// We have to do that synchronous as the default async-operation SOMETIMES doesn't work due to internal error (Win32Exception: "Invalid window handle")
        /// </summary>
        /// <param name="cmd">The command id.</param>
        public override void HandleGoto(VsCommands cmd) {
            //we handle only th goto-definition command
            if (cmd != VsCommands.GotoDefn) {
                base.HandleGoto(cmd);
                return;
            }

            int line, col;

            // Get the caret position
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(TextView.GetCaretPos(out line, out col));

            //parse the source synchronous
            AuthoringScope scope = Source.LanguageService.ParseSource(new ParseRequest(line, col, new TokenInfo(), Source.GetText(), Source.GetFilePath(), ParseReason.Goto, TextView, Source.CreateAuthoringSink(ParseReason.Goto, line, col), true)); 
            
            //navigate to the found position

            string url = null;
            TextSpan span;
            
            if (scope != null) {
                url = scope.Goto(cmd, TextView, line, col, out span);
            }
            else {
                return;
            }
            if (url == null || url.Trim().Length == 0) { // nothing to show
                return;
            }

            // Open the referenced document, and scroll to the given location.
            IVsUIHierarchy hierarchy;
            uint itemID;
            IVsWindowFrame frame;
            IVsTextView view;

            Microsoft.VisualStudio.Shell.VsShellUtilities.OpenDocument(base.Source.LanguageService.Site, url, VSConstants.LOGVIEWID_Code, out hierarchy, out itemID, out frame, out view);
            if (view != null) {
                TextSpanHelper.MakePositive(ref span);
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(view.EnsureSpanVisible(span));
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(view.SetSelection(span.iStartLine, span.iStartIndex, span.iEndLine, span.iEndIndex));
            }
        }
       
        /// <summary>
        /// Gets the status of specific commands.
        /// </summary>
        /// <param name="guidCmdGroup">The guid of the command group.</param>
        /// <param name="nCmdId">The int value of the corresponding command-enum.</param>
        /// <returns>The status of the command.</returns>
        protected override int QueryCommandStatus(ref Guid guidCmdGroup, uint nCmdId) {
            if (guidCmdGroup == typeof(VsCommands).GUID) {
                switch ((VsCommands)nCmdId) {
                    case VsCommands.GotoDecl:
                    case VsCommands.GotoRef:
                    case VsCommands.InsertBreakpoint:
                    case VsCommands.RunToCursor:
                    case VsCommands.EnableBreakpoint:

                        // Hide these commands from context menu
                        //return (int)OLECMDF.OLECMDF_INVISIBLE;
                        //return VSConstants.E_FAIL;
                        return (int)OLECMDF.OLECMDF_SUPPORTED
                        | (int)OLECMDF.OLECMDF_INVISIBLE;
                    case VsCommands.FindReferences:
                    case VsCommands.ObjectSearch:
                    case VsCommands.ObjectSearchResults:
                        // Show the command in context menu
                        return (int)OLECMDF.OLECMDF_SUPPORTED
                        | (int)OLECMDF.OLECMDF_ENABLED;
                }
            }
            //hide 'insert intelli-trace point'-command (values found out by debuggint, NOTHING documentated in MSDN)
            else if (guidCmdGroup == guidCmdGroupIntelliTrace && nCmdId == nCmdIdInsertIntellitrace) {
                return (int)OLECMDF.OLECMDF_SUPPORTED
                        | (int)OLECMDF.OLECMDF_INVISIBLE;
            }
                        
            return base.QueryCommandStatus(ref guidCmdGroup, nCmdId);
        }
        #endregion
    }
}
