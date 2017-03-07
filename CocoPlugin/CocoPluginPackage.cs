using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using EnvDTE80;
using System.IO;
using System.Collections;
using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;
using at.jku.ssw.Coco.VSPlugin.Language;
using at.jku.ssw.Coco.VSPlugin.OptionPages;
using at.jku.ssw.Coco.VSPlugin.Library;
using System.Collections.Generic;

namespace at.jku.ssw.Coco.VSPlugin {
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // A Visual Studio component can be registered under different regitry roots; for instance
    // when you debug your package you want to register it in the experimental hive. This
    // attribute specifies the registry root to use if no one is provided to regpkg.exe with
    // the /root switch.
    [DefaultRegistryRoot("Software\\Microsoft\\VisualStudio\\14.0Exp")]
    [ProvideService(typeof(CocoLanguageService))]
    [ProvideService(typeof(CocoLibraryManager))]
    [ProvideLanguageExtension(typeof(CocoLanguageService), CocoLanguageService.EXTENSION)]
    [ProvideLanguageService(typeof(CocoLanguageService), CocoLanguageService.NAME, 0,
        CodeSense = true,
        EnableCommenting = true,
        MatchBraces = true,
        ShowCompletion = true,
        ShowMatchingBrace = true,
        AutoOutlining = true,
        RequestStockColors = true,
        QuickInfo = true,
        EnableAdvancedMembersOption = true)]
    [ProvideOptionPage(typeof(OptionsPageGeneral), "CocoOptions", "General",
      101, 106, true)]
    [ProvideProfile(typeof(OptionsPageGeneral), "CocoOptions", "General",
        101, 106, true, DescriptionResourceID = 101)]
    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(GuidList.guidCocoPluginPkgString)]
    public sealed class CocoPluginPackage : Package, IOleComponent {       
        #region Variables
        private CocoLanguageService languageService;
        private CocoLibraryManager libraryManager;
        private BuildEvents buildEvents;
        private SolutionEvents solutionEvents;
        private ErrorListProvider errorListProvider;        
        private uint componentID;        
        #endregion

        #region Constructor
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public CocoPluginPackage() {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }

        #endregion

        /////////////////////////////////////////////////////////////////////////////
        // Overriden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initilaization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize() {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            IServiceContainer serviceContainer = (IServiceContainer)this;

            //add languageservice
            languageService = new CocoLanguageService();
            languageService.SetSite(this);            
            serviceContainer.AddService(typeof(CocoLanguageService), languageService, true);

            //add library
            libraryManager = new CocoLibraryManager(this);
            serviceContainer.AddService(typeof(CocoLibraryManager), libraryManager, true);

            //this is necessary because the Initialize method of the packages is called AFTER a solution that contains a grammar-file has been opened, so the Solution.Opened event won't be reaised anymore
            RegisterSolution();

            //checks periodically if files have changed (and rebuilds the library)
            RegisterForIdleTime();

            // Add build event handlers
            DTE2 dte = (DTE2)GetService(typeof(DTE));
            buildEvents = dte.Events.BuildEvents;
            buildEvents.OnBuildBegin += OnBuildBegin;
            buildEvents.OnBuildDone += OnBuildDone;

            //add solution events
            solutionEvents = dte.Events.SolutionEvents;            
            solutionEvents.Opened += new _dispSolutionEvents_OpenedEventHandler(SolutionEvents_Opened);
            solutionEvents.BeforeClosing +=new _dispSolutionEvents_BeforeClosingEventHandler(solutionEvents_BeforeClosing);
            solutionEvents.ProjectAdded += new _dispSolutionEvents_ProjectAddedEventHandler(solutionEvents_ProjectAdded);
            solutionEvents.ProjectRemoved += new _dispSolutionEvents_ProjectRemovedEventHandler(solutionEvents_ProjectRemoved);

            // Create error provider
            errorListProvider = new ErrorListProvider(this);
            errorListProvider.ProviderGuid = GuidList.guidErrorProvider;
            errorListProvider.ProviderName = "Coco/R Error Provider"; //name doesn't matter, nobody else needs/uses it           
        }

        protected override void Dispose(bool disposing) {
            try {
                if (componentID != 0) {
                    IOleComponentManager mgr = GetService(typeof(SOleComponentManager)) as IOleComponentManager;
                    if (mgr != null) {
                        mgr.FRevokeComponent(componentID);
                    }
                    componentID = 0;
                }
                if (null != libraryManager) {
                    libraryManager.Dispose();
                    libraryManager = null;
                }
            }
            finally {
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Methods
        
        private void RegisterForIdleTime() {
            IOleComponentManager mgr = GetService(typeof(SOleComponentManager)) as IOleComponentManager;
            if (componentID == 0 && mgr != null) {
                OLECRINFO[] crinfo = new OLECRINFO[1];
                crinfo[0].cbSize = (uint)Marshal.SizeOf(typeof(OLECRINFO));
                crinfo[0].grfcrf = (uint)_OLECRF.olecrfNeedIdleTime |
                                              (uint)_OLECRF.olecrfNeedPeriodicIdleTime;
                crinfo[0].grfcadvf = (uint)_OLECADVF.olecadvfModal |
                                              (uint)_OLECADVF.olecadvfRedrawOff |
                                              (uint)_OLECADVF.olecadvfWarningsOff;
                crinfo[0].uIdleTimeInterval = 3000;
                int hr = mgr.FRegisterComponent(this, crinfo, out componentID);
            }
        }

        /// <summary>
        /// Returns the options page of the given type.
        /// </summary>
        /// <typeparam name="T">The optionspage type.</typeparam>
        /// <returns>The options page.</returns>
        internal T GetOptionsPage<T>() where T : DialogPage {
            return (T)GetDialogPage(typeof(T));
        }

        private void RegisterSolution() {
            foreach (IVsHierarchy project in LoadedProjects) {
                libraryManager.RegisterHierarchy(project);
            }            
        }

        private void UnregisterSolution() {
            foreach (IVsHierarchy project in LoadedProjects) {
                libraryManager.UnregisterHierarchy(project);
            }                       
        }

        private IEnumerable<IVsHierarchy> LoadedProjects {
            get {
                IVsSolution solution = GetService(typeof(SVsSolution)) as IVsSolution;
                IEnumHierarchies enumerator = null;
                Guid guid = Guid.Empty;
                if (solution != null) {
                    solution.GetProjectEnum((uint)__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION, ref guid, out enumerator);
                    IVsHierarchy[] hierarchy = new IVsHierarchy[1] { null };
                    uint fetched = 0;
                    for (enumerator.Reset(); enumerator.Next(1, hierarchy, out fetched) == VSConstants.S_OK && fetched == 1; /*nothing*/) {
                        yield return hierarchy[0];
                    }
                }
            }
        }

        private IVsHierarchy GetIVsHierarchy(Project project) {
            var solution = GetService(typeof(SVsSolution)) as IVsSolution;
            IVsHierarchy hierarchy;
            solution.GetProjectOfUniqueName(project.FullName, out hierarchy);
            return hierarchy;
        }

                
        #endregion

        #region EventHandling

        private void SolutionEvents_Opened() {
            RegisterSolution();
        }

        private void solutionEvents_BeforeClosing() {
            UnregisterSolution();
        }

        private void solutionEvents_ProjectRemoved(Project Project) {
            libraryManager.UnregisterHierarchy(GetIVsHierarchy(Project));
        }

        private void solutionEvents_ProjectAdded(Project Project) {
            libraryManager.RegisterHierarchy(GetIVsHierarchy(Project));
        }

        private void NavigateDocument(object sender, EventArgs e) {
            ErrorTask task = sender as ErrorTask;
            if (task == null)
                throw new ArgumentException("sender");

            // Get the doc data for the task's document
            if (String.IsNullOrEmpty(task.Document)) {
                return;
            }

            IVsUIShellOpenDocument openDoc =
                GetService(typeof(IVsUIShellOpenDocument)) as
                    IVsUIShellOpenDocument;

            if (openDoc == null) {
                return;
            }

            IVsWindowFrame frame;
            Microsoft.VisualStudio.OLE.Interop.IServiceProvider sp;
            IVsUIHierarchy hier;
            uint itemid;
            Guid logicalView = VSConstants.LOGVIEWID_Code;

            if (
                openDoc.OpenDocumentViaProject(task.Document, ref logicalView,
                    out sp, out hier, out itemid, out frame) != VSConstants.S_OK
                        || frame == null) {
                return;
            }

            object docData;
            frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out docData);

            // Get the VsTextBuffer
            VsTextBuffer buffer = docData as VsTextBuffer;
            if (buffer == null) {
                IVsTextBufferProvider bufferProvider =
                    docData as IVsTextBufferProvider;
                if (bufferProvider != null) {
                    IVsTextLines lines;
                    bufferProvider.GetTextBuffer(out lines);
                    buffer = lines as VsTextBuffer;
                    Debug.Assert(buffer != null,
                        "IVsTextLines does not implement IVsTextBuffer");

                    if (buffer == null) {
                        return;
                    }
                }
            }

            // Finally, perform the navigation.
            IVsTextManager mgr =
                GetService(typeof(VsTextManagerClass)) as IVsTextManager;

            if (mgr == null) {
                return;
            }

            mgr.NavigateToLineAndColumn(buffer, ref logicalView, task.Line,
                task.Column, task.Line, task.Column);
        }

        private void OnBuildBegin(vsBuildScope Scope, vsBuildAction Action) {
            errorListProvider.Tasks.Clear();
        }

        // adds the mappings between errors in the generated parser-file and their corresponding position in the grammar file (semantic actions).
        private void OnBuildDone(vsBuildScope Scope, vsBuildAction Action) {
            DTE2 dte = (DTE2)GetService(typeof(DTE));
            ErrorItems errors = dte.ToolWindows.ErrorList.ErrorItems;
            Hashtable mapCache = new Hashtable();

            bool grammarErrorFound = false;

            for (uint i = 1; i <= errors.Count; i++) {
                ErrorItem error = errors.Item(i);

                string fn = Path.GetFileName(error.FileName);
                string dir = Path.GetDirectoryName(error.FileName);
                if (fn.ToLower() == "parser.cs") {                    
                    string atgmap = Path.Combine(dir, "parser.atgmap");
                    Mapping map = null;
                    if (mapCache.ContainsKey(atgmap)) {
                        map = (Mapping)mapCache[atgmap];
                    }
                    else {
                        if (File.Exists(atgmap)) {
                            map = new Mapping();
                            map.Read(atgmap);
                            mapCache[atgmap] = map;
                        }
                    }

                    if (map != null) {
                        int line, column;
                        if (map.Get(error.Line - 1, error.Column - 1, out line,
                            out column)) {
                            ErrorTask task = new ErrorTask();
                            task.ErrorCategory = TaskErrorCategory.Error;
                            task.Priority = TaskPriority.Normal;
                            task.Text = error.Description;
                            task.Column = column;
                            task.Line = line;
                            task.Document = map.Grammar;
                            task.Navigate += NavigateDocument;
                            errorListProvider.Tasks.Add(task);
                            grammarErrorFound = true;
                        }
                    }
                }
            }

            if(grammarErrorFound)
                errorListProvider.Show();
        }


        #endregion

        #region IOleComponent Members

        public int FContinueMessageLoop(uint uReason, IntPtr pvLoopData, MSG[] pMsgPeeked) {
            return 1;
        }

        public int FDoIdle(uint grfidlef) {
            CocoLanguageService pl = GetService(typeof(CocoLanguageService)) as CocoLanguageService;
            bool periodic = (grfidlef & (uint)_OLEIDLEF.oleidlefPeriodic) != 0;
            if (pl != null) {
                pl.OnIdle(periodic);
            }
            if (periodic && null != libraryManager) { //it is WANTED that the library only runs periodicly
                libraryManager.OnIdle();
            }
            return 0;
        }

        public int FPreTranslateMessage(MSG[] pMsg) {
            return 0;
        }

        public int FQueryTerminate(int fPromptUser) {
            return 1;
        }

        public int FReserved1(uint dwReserved, uint message, IntPtr wParam, IntPtr lParam) {
            return 1;
        }

        public IntPtr HwndGetWindow(uint dwWhich, uint dwReserved) {
            return IntPtr.Zero;
        }

        public void OnActivationChange(IOleComponent pic, int fSameComponent, OLECRINFO[] pcrinfo, int fHostIsActivating, OLECHOSTINFO[] pchostinfo, uint dwReserved) {
        }

        public void OnAppActivate(int fActive, uint dwOtherThreadID) {
        }

        public void OnEnterState(uint uStateID, int fEnter) {
        }

        public void OnLoseActivation() {
        }

        public void Terminate() {
        }

        #endregion
    }
}