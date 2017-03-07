using System;
using System.Globalization;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.ComponentModel;
using System.Runtime.InteropServices;
using MSVSIP = Microsoft.VisualStudio.Shell;
using System.IO;
using EnvDTE;
using System.Text;

namespace at.jku.ssw.Coco.VSPlugin.OptionPages {
    /// <summary>
    // Extends a standard dialog functionality for implementing the Coco Options pages, 
    // with support for the Visual Studio automation model, Windows Forms, and state 
    // persistence through the Visual Studio settings mechanism.
    /// </summary>
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid(GuidList.guidPageGeneralString)]
    [ComVisible(true)]
    public class OptionsPageGeneral : MSVSIP.DialogPage {

        #region Fields
        private string frameDirectory = "";
        private string outputDirectory = "";
        private string targetNamespace = "";

        private bool traceAutomaton = false;
        private bool listFirstFollowSets = false;
        private bool printSyntaxGraph = false;
        private bool traceComputationOfFirstSets = false;
        private bool listAnyAndSyncSets = false;
        private bool printStatistics = false;
        private bool listSymbolTable = false;
        private bool listCrossReferenceTable = false;
        #endregion Fields

        #region Properties
        [DisplayName("Trace Automaton")]
        [Category("Trace Output Options")]        
        [Description("A: prints the states of the scanner automaton")]
        public bool TraceAutomaton {
            get { 
                return traceAutomaton; 
            }
            set { 
                traceAutomaton = value; 
            }
        }

        [DisplayName("List First and Follow Sets")]
        [Category("Trace Output Options")]        
        [Description("F: prints the First and Follow sets of all nonterminals")]
        public bool ListFirstFollowSets {
            get { 
                return listFirstFollowSets; 
            }
            set { 
                listFirstFollowSets = value; 
            }
        }

        [DisplayName("Print Syntax Graph")]
        [Category("Trace Output Options")]
        [Description("G: prints the syntax graph of the productions")]
        public bool PrintSyntaxGraph {
            get { 
                return printSyntaxGraph; 
            }
            set { 
                printSyntaxGraph = value; 
            }
        }

        [DisplayName("Trace Compution of First Sets")]
        [Category("Trace Output Options")]
        [Description("I: traces the computation of the First sets")]
        public bool TraceComputationOfFirstSets {
            get { 
                return traceComputationOfFirstSets; 
            }
            set { 
                traceComputationOfFirstSets = value; 
            }
        }

        [DisplayName("List ANY and Sync Sets")]
        [Category("Trace Output Options")]
        [Description("J: prints the sets associated with ANYs and synchronisation sets")]
        public bool ListAnyAndSyncSets {
            get { 
                return listAnyAndSyncSets; 
            }
            set { 
                listAnyAndSyncSets = value; 
            }
        }

        [DisplayName("Print Statistics")]
        [Category("Trace Output Options")]
        [Description("P: prints statistics about the Coco run")]
        public bool PrintStatistics {
            get { 
                return printStatistics; 
            }
            set { 
                printStatistics = value; 
            }
        }

        [DisplayName("List Symbol Table")]
        [Category("Trace Output Options")]
        [Description("S: prints the symbol table (terminals, nonterminals, pragmas)")]
        public bool ListSymbolTable {
            get { 
                return listSymbolTable; 
            }
            set { 
                listSymbolTable = value; 
            }
        }

        [DisplayName("List Cross Reference Table")]
        [Category("Trace Output Options")]
        [Description("X: prints a cross reference list of all syntax symbols")]
        public bool ListCrossReferenceTable {
            get { 
                return listCrossReferenceTable; 
            }
            set { 
                listCrossReferenceTable = value; 
            }
        }

        [DisplayName("Frame Directory")]
        [Category("Directories")]
        [Description("Scanner.frame and Parser.frame files need to be in this directory. Leave empty to use ATG directory.")]
        public string FrameDirectory {
            get {
                return frameDirectory;
            }
            set {
                frameDirectory = value.Trim();
            }
        }

        [DisplayName("Output Directory")]
        [Category("Directories")]
        [Description("Output directory for parser.cs and scanner.cs. Leave empty to use ATG directory.")]
        public string OutputDirectory {
            get {
                return outputDirectory;
            }
            set {
                outputDirectory = value.Trim();
            }
        }

        [DisplayName("Target Namespace")]
        [Category("Options")]
        [Description("Target namespace for parser.cs and scanner.cs. Use '-' to omit the namespace argument on the Coco/R call, or leave this field empty to use the solution's default namespace.")]
        public string TargetNamespace
        {
          get
          {
            return targetNamespace;
          }
          set
          {
            targetNamespace = value.Trim();
          }
        }

        [Category("Coco arguments")]
        [Browsable(false)]
        public bool SkipNamespace
        {
           get { return "-".Equals(targetNamespace); }
        }
  
        [Category("Coco arguments")]
        [Browsable(false)]
        public string CocoArguments {
            get {
                StringBuilder sb = new StringBuilder();
                if (!String.IsNullOrEmpty(frameDirectory)) {
                    sb.Append(" -frames ");
                    sb.Append(frameDirectory);
                }

                if (!String.IsNullOrEmpty(outputDirectory)) {
                    sb.Append(" -o ");
                    sb.Append(outputDirectory);
                }

                if (!SkipNamespace && !String.IsNullOrEmpty(targetNamespace)) {
                    sb.Append(" -namespace ");
                    sb.Append(targetNamespace);
                }

                StringBuilder trace = new StringBuilder();
                if (traceAutomaton) {
                    trace.Append("A");
                }

                if (listAnyAndSyncSets) {
                    trace.Append("F");
                }

                if (printSyntaxGraph) {
                    trace.Append("G");
                }

                if (traceComputationOfFirstSets) {
                    trace.Append("I");
                }

                if (listAnyAndSyncSets) {
                    trace.Append("J");
                }

                if (printStatistics) {
                    trace.Append("P");
                }

                if (listSymbolTable) {
                    trace.Append("S");
                }

                if (listCrossReferenceTable) {
                    trace.Append("X");
                }

                if (trace.ToString().Length != 0) {
                    sb.Append(" -trace ");
                    sb.Append(trace.ToString());
                }

                return sb.ToString();
            }
        }
        
        #endregion Properties
    }
}
