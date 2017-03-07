/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/
using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;
using VSConstants = Microsoft.VisualStudio.VSConstants;

//NOTE: This class is from the VS SDK 2008 (with minor changes)
namespace at.jku.ssw.Coco.VSPlugin.Library {

    /// <summary>
    /// This is a specialized version of the LibraryNode that handles the Coco items. 
    /// The main difference from the generic one is that it supports navigation
    /// to the location inside the source code where the element is defined.
    /// </summary>
    internal class CocoLibraryNode : LibraryNode {
        #region Variables
        private IVsHierarchy ownerHierarchy;
        private uint fileId;
        private TextSpan sourceSpan;
        private string fileMoniker;
        #endregion

        #region Constructors
        internal CocoLibraryNode(string name, int line, int startCol, int endCol, IVsHierarchy hierarchy, uint itemId, LibraryNodeType nodeType) :
            base(name) {
            
            this.NodeType = nodeType;
            
            this.ownerHierarchy = hierarchy;
            this.fileId = itemId;

            // Now check if we have all the information to navigate to the source location.
            if ((null != ownerHierarchy) && (VSConstants.VSITEMID_NIL != fileId)) {
                sourceSpan = new TextSpan();
                sourceSpan.iStartIndex = startCol - 1;
                sourceSpan.iStartLine = sourceSpan.iEndLine = line - 1;                
                sourceSpan.iEndIndex = endCol - 1;
                
                this.CanGoToSource = true;
            }

            SetDisplayData();
        }
              
        internal CocoLibraryNode(CocoLibraryNode node) :
            base(node) {
            this.fileId = node.fileId;
            this.ownerHierarchy = node.ownerHierarchy;
            this.fileMoniker = node.fileMoniker;
            this.sourceSpan = node.sourceSpan;
        }
        #endregion

        #region Properties
        public override string UniqueName {
            get {
                if (string.IsNullOrEmpty(fileMoniker)) {
                    ErrorHandler.ThrowOnFailure(ownerHierarchy.GetCanonicalName(fileId, out fileMoniker));
                }
                return string.Format(CultureInfo.InvariantCulture, "{0}/{1}", fileMoniker, Name);
            }
        }

        #endregion

        #region Methods
        protected override void SetDisplayData() {
            displayData = new VSTREEDISPLAYDATA();
            ushort imageIndex = 0;
            switch (NodeType) {
                case LibraryNodeType.Members:
                    imageIndex = at.jku.ssw.Coco.VSPlugin.Language.CocoDeclarations.GLYPH_VAR;
                    break;
                case LibraryNodeType.References:
                    imageIndex = at.jku.ssw.Coco.VSPlugin.Language.CocoDeclarations.GLYPH_USAGE;
                    break;
                //add further image indizes here
                default:
                    //leave 0 as index
                    break;
            }
            displayData.Image = displayData.SelectedImage = imageIndex;
        }

        protected override uint CategoryField(LIB_CATEGORY category) {
            switch (category) {
                case (LIB_CATEGORY)_LIB_CATEGORY2.LC_MEMBERINHERITANCE:
                    if (this.NodeType == LibraryNodeType.Members) {
                        return (uint)_LIBCAT_MEMBERINHERITANCE.LCMI_IMMEDIATE;
                    }
                    break;
                case LIB_CATEGORY.LC_MEMBERTYPE:
                    return (uint)_LIBCAT_MEMBERTYPE.LCMT_VARIABLE;
                case LIB_CATEGORY.LC_MEMBERACCESS:
                    return (uint)_LIBCAT_MEMBERACCESS.LCMA_PRIVATE;
                case LIB_CATEGORY.LC_CLASSTYPE:
                    return (uint)_LIBCAT_CLASSTYPE.LCCT_CLASS;
                case LIB_CATEGORY.LC_CLASSACCESS:
                    return (uint)_LIBCAT_CLASSACCESS.LCCA_PUBLIC;
                case LIB_CATEGORY.LC_VISIBILITY:
                    if(Visible)
                        return (uint)_LIBCAT_VISIBILITY.LCV_VISIBLE;
                    return (uint)_LIBCAT_VISIBILITY.LCV_HIDDEN;
                case LIB_CATEGORY.LC_MODIFIER:
                    return (uint)_LIBCAT_MODIFIERTYPE.LCMDT_NONVIRTUAL;
            }
            return base.CategoryField(category);
        }

        protected override LibraryNode Clone() {
            return new CocoLibraryNode(this);
        }

        protected override void GotoSource(VSOBJGOTOSRCTYPE gotoType) {
            // We do not support the "Goto Reference"
            if (VSOBJGOTOSRCTYPE.GS_REFERENCE == gotoType) {
                return;
            }

            // There is no difference between definition and declaration, so here we
            // don't check for the other flags.

            IVsWindowFrame frame = null;
            IntPtr documentData = FindDocDataFromRDT();
            try {
                // Now we can try to open the editor. We assume that the owner hierarchy is
                // a project and we want to use its OpenItem method.
                IVsProject3 project = ownerHierarchy as IVsProject3;
                if (null == project) {
                    return;
                }
                Guid viewGuid = VSConstants.LOGVIEWID_Code;
                ErrorHandler.ThrowOnFailure(project.OpenItem(fileId, ref viewGuid, documentData, out frame));
            } finally {
                if (IntPtr.Zero != documentData) {
                    Marshal.Release(documentData);
                    documentData = IntPtr.Zero;
                }
            }

            // Make sure that the document window is visible.
            ErrorHandler.ThrowOnFailure(frame.Show());

            // Get the code window from the window frame.
            object docView;
            ErrorHandler.ThrowOnFailure(frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out docView));
            IVsCodeWindow codeWindow = docView as IVsCodeWindow;
            if (null == codeWindow) {
                object docData;
                ErrorHandler.ThrowOnFailure(frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out docData));
                codeWindow = docData as IVsCodeWindow;
                if (null == codeWindow) {
                    return;
                }
            }

            // Get the primary view from the code window.
            IVsTextView textView;
            ErrorHandler.ThrowOnFailure(codeWindow.GetPrimaryView(out textView));

            // Set the cursor at the beginning of the declaration.
            ErrorHandler.ThrowOnFailure(textView.SetCaretPos(sourceSpan.iStartLine, sourceSpan.iStartIndex));
            // Make sure that the text is visible.
            TextSpan visibleSpan = new TextSpan();
            visibleSpan.iStartLine = sourceSpan.iStartLine;
            visibleSpan.iStartIndex = sourceSpan.iStartIndex;
            visibleSpan.iEndLine = sourceSpan.iStartLine;
            visibleSpan.iEndIndex = sourceSpan.iStartIndex + 1;
            ErrorHandler.ThrowOnFailure(textView.EnsureSpanVisible(visibleSpan));

        }

        protected override void SourceItems(out IVsHierarchy hierarchy, out uint itemId, out uint itemsCount) {
            hierarchy = ownerHierarchy;
            itemId = fileId;
            itemsCount = 1;
        }
               
        private IntPtr FindDocDataFromRDT() {
            // Get a reference to the RDT.
            IVsRunningDocumentTable rdt = CocoPluginPackage.GetGlobalService(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
            if (null == rdt) {
                return IntPtr.Zero;
            }

            // Get the enumeration of the running documents.
            IEnumRunningDocuments documents;
            ErrorHandler.ThrowOnFailure(rdt.GetRunningDocumentsEnum(out documents));

            IntPtr documentData = IntPtr.Zero;
            uint[] docCookie = new uint[1];
            uint fetched;
            while ((VSConstants.S_OK == documents.Next(1, docCookie, out fetched)) && (1 == fetched)) {
                uint flags;
                uint editLocks;
                uint readLocks;
                string moniker;
                IVsHierarchy docHierarchy;
                uint docId;
                IntPtr docData = IntPtr.Zero;
                try {
                    ErrorHandler.ThrowOnFailure(
                        rdt.GetDocumentInfo(docCookie[0], out flags, out readLocks, out editLocks, out moniker, out docHierarchy, out docId, out docData));
                    // Check if this document is the one we are looking for.
                    if ((docId == fileId) && (ownerHierarchy.Equals(docHierarchy))) {
                        documentData = docData;
                        docData = IntPtr.Zero;
                        break;
                    }
                } finally {
                    if (IntPtr.Zero != docData) {
                        Marshal.Release(docData);
                    }
                }
            }

            return documentData;
        }

        #endregion

    }
}
