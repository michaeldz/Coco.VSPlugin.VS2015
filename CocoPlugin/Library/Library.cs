/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/
using System;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using VSConstants = Microsoft.VisualStudio.VSConstants;

//NOTE: This class is from the VS SDK 2008 (with minor changes
namespace at.jku.ssw.Coco.VSPlugin.Library {
    /// <summary>
    /// The library communicates with the IVsObjectManager2-object (implemended by VS). The library is responsible for publishing all information to the object-manager so that this one can use it in the object-browser.
    /// The library is also used for GoTo-Definition commands executed from the object-browser and also for the find-symbol-results command.
    /// </summary>
    public class Library : IVsSimpleLibrary2 {
        #region Constants
        /// <summary>
        /// Set this value to the dwCustom field in the VSOBSEARCHCRITERIA2-struct to indicate that the this is a findreference-search.
        /// </summary>
        public const int DWCUSTOM_FINDREFSEARCH = 100;
        #endregion

        #region Variables
        private Guid guid;
        private _LIB_FLAGS2 capabilities;
        private LibraryNode root;
        LibraryNode cocoLibNode;
        private LibraryNode classLevelNode;
        #endregion

        #region Constructor
        public Library(Guid libraryGuid) {
            this.guid = libraryGuid;

            root = new LibraryNode("", LibraryNode.LibraryNodeType.Package); //the root node is not visible in the object browser
            cocoLibNode = new LibraryNode("Coco/R Grammar Library", LibraryNode.LibraryNodeType.Package);
            root.AddNode(cocoLibNode);
            classLevelNode = new LibraryNode("Coco/R Grammar Files", LibraryNode.LibraryNodeType.Namespaces);
            cocoLibNode.AddNode(classLevelNode);
        }
        #endregion

        #region Properties
        public _LIB_FLAGS2 LibraryCapabilities {
            get { return capabilities; }
            set { capabilities = value; }
        }
        #endregion

        #region Methods
        internal void AddNode(LibraryNode node) {
            lock (this) {
                //recreate every node
                root = new LibraryNode(root);
                cocoLibNode = new LibraryNode(cocoLibNode);
                classLevelNode = new LibraryNode(classLevelNode);
                classLevelNode.AddNode(node);
                root.children.Clear();
                cocoLibNode.children.Clear();
                cocoLibNode.AddNode(classLevelNode);
                root.AddNode(cocoLibNode);
            }
        }

        internal void RemoveNode(LibraryNode node) {
            lock (this) {
                //recreate every node
                root = new LibraryNode(root);
                cocoLibNode = new LibraryNode(cocoLibNode);
                classLevelNode = new LibraryNode(classLevelNode);
                classLevelNode.RemoveNode(node);
                root.children.Clear();
                cocoLibNode.children.Clear();
                cocoLibNode.AddNode(classLevelNode);
                root.AddNode(cocoLibNode);                
            }
        }
        #endregion

        #region IVsSimpleLibrary2 Members

        public int AddBrowseContainer(VSCOMPONENTSELECTORDATA[] pcdComponent, ref uint pgrfOptions, out string pbstrComponentAdded) {
            pbstrComponentAdded = null;
            return VSConstants.E_NOTIMPL;
        }

        public int CreateNavInfo(SYMBOL_DESCRIPTION_NODE[] rgSymbolNodes, uint ulcNodes, out IVsNavInfo ppNavInfo) {
            ppNavInfo = null;
            return VSConstants.E_NOTIMPL;
        }

        public int GetBrowseContainersForHierarchy(IVsHierarchy pHierarchy, uint celt, VSBROWSECONTAINER[] rgBrowseContainers, uint[] pcActual) {
            return VSConstants.E_NOTIMPL;
        }

        public int GetGuid(out Guid pguidLib) {
            pguidLib = guid;
            return VSConstants.S_OK;
        }

        public int GetLibFlags2(out uint pgrfFlags) {
            pgrfFlags = (uint)LibraryCapabilities;
            return VSConstants.S_OK;
        }

        public int GetList2(uint ListType, uint flags, VSOBSEARCHCRITERIA2[] pobSrch, out IVsSimpleObjectList2 ppIVsSimpleObjectList2) {           
            //the find-references command is implemented in this method
            //because it is not documented what exactly has to be implemented, we perform the search on our own (see CocoViewFilter - the request is initialized in there)
            if (((_LIB_LISTFLAGS)flags & _LIB_LISTFLAGS.LLF_USESEARCHFILTER) != 0 && pobSrch != null && pobSrch.Length > 0) {
                //when not filtering for members, the same list will be displayed three times (this is the same in the IrconPython sample of VS SDK 2008), so we handled it in the way to only support filtering for members (no namespaces, no hierarchys)
                if ((_LIB_LISTTYPE)ListType != _LIB_LISTTYPE.LLT_MEMBERS) {
                    //we just provide filter for members (not for namespaces, hierarchies, etc.)
                    ppIVsSimpleObjectList2 = null;
                    return VSConstants.S_OK;
                }
                //we only look at one search-criteria
                VSOBSEARCHCRITERIA2 criteria = pobSrch[0];

                //get the name and check if this search was initalized by us (the manually initialized 'find-references' search)
                string searchString = criteria.szName;
                bool isFindReferences = criteria.dwCustom == DWCUSTOM_FINDREFSEARCH;

                if (string.IsNullOrEmpty(searchString)) {
                    ppIVsSimpleObjectList2 = root as IVsSimpleObjectList2; //don't do anything, no filter
                    return VSConstants.S_OK;
                }

                searchString = searchString.ToUpperInvariant();
                
                //references will be found by the next searchstring because references start with the same uniquename as the definition followed by a whitespace
                string searchStringWithSpace = searchString + " ";

                //copy will be returned
                LibraryNode classLevelCopy = new LibraryNode(classLevelNode);

                for (int i = 0; i < classLevelCopy.children.Count; i++) {
                    //copy the node, because we have to modify it
                    CocoLibraryNode fileCopy = new CocoLibraryNode(classLevelCopy.children[i] as CocoLibraryNode);

                    for (int j = 0; j < fileCopy.children.Count; j++) {
                        if (isFindReferences) { //search for exact member name and for references
                            if ((fileCopy.children[j].NodeType == LibraryNode.LibraryNodeType.Members && fileCopy.children[j].UniqueName.ToUpperInvariant() == searchString)
                                || fileCopy.children[j].NodeType == LibraryNode.LibraryNodeType.References && fileCopy.children[j].UniqueName.ToUpperInvariant().StartsWith(searchStringWithSpace)) {
                                fileCopy.children[j] = new CocoLibraryNode(fileCopy.children[j] as CocoLibraryNode); //make a copy and make that visible
                                fileCopy.children[j].Visible = true;
                            }
                            else {
                                fileCopy.RemoveNode(fileCopy.children[j]);
                                j--;
                            }
                        }
                        else { //don't search for references, jsut for members which contain the search string
                            if (fileCopy.children[j].NodeType == LibraryNode.LibraryNodeType.Members && fileCopy.children[j].UniqueName.ToUpperInvariant().Contains(searchString)) {
                                fileCopy.children[j] = new CocoLibraryNode(fileCopy.children[j] as CocoLibraryNode); //make a copy and make that visible
                                fileCopy.children[j].Visible = true;                                
                            }
                            else {
                                fileCopy.RemoveNode(fileCopy.children[j]);
                                j--;
                            }
                        }
                    }

                    //if the current file doesn't contain and result, remove it
                    if (fileCopy.children.Count <= 0) {
                        classLevelCopy.RemoveNode(classLevelCopy.children[i]);
                        i--;
                    }
                    else {
                        classLevelCopy.children[i] = fileCopy; //assign the modified copy
                    }
                }
                
                //move search results to top-level (now they are on file-level, but search results have to be displayed at very top level)
                System.Collections.Generic.List<LibraryNode> children;

                if (classLevelCopy.children.Count > 0) {
                    if (classLevelCopy.children.Count == 1)
                        children = classLevelCopy.children[0].children; //this is the normal path (and the very fast one), because normally there is only one atg-file in a project
                    else { //more than one
                        children = new System.Collections.Generic.List<LibraryNode>();
                        for (int i = 0; i < classLevelCopy.children.Count; i++) {
                            children.AddRange(classLevelCopy.children[i].children);
                        }
                    }
                    classLevelCopy.children = children;
                }

                //return the modifed copy as result
                ppIVsSimpleObjectList2 = classLevelCopy as IVsSimpleObjectList2;
                return VSConstants.S_OK;
            }

            ppIVsSimpleObjectList2 = root as IVsSimpleObjectList2;
            return VSConstants.S_OK;
        }

        public int GetSeparatorStringWithOwnership(out string pbstrSeparator) {
            pbstrSeparator = ".";
            return VSConstants.S_OK;
        }

        public int GetSupportedCategoryFields2(int Category, out uint pgrfCatField) {
            pgrfCatField = (uint)_LIB_CATEGORY2.LC_HIERARCHYTYPE | (uint)_LIB_CATEGORY2.LC_PHYSICALCONTAINERTYPE;
            return VSConstants.S_OK;
        }

        public int LoadState(IStream pIStream, LIB_PERSISTTYPE lptType) {
            return VSConstants.S_OK;
        }

        public int RemoveBrowseContainer(uint dwReserved, string pszLibName) {
            return VSConstants.E_NOTIMPL;
        }

        public int SaveState(IStream pIStream, LIB_PERSISTTYPE lptType) {
            return VSConstants.S_OK;
        }

        public int UpdateCounter(out uint pCurUpdate) {
            return ((IVsSimpleObjectList2)root).UpdateCounter(out pCurUpdate);
        }

        #endregion
    }
}
