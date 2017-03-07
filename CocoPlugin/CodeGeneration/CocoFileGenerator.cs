using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using VSLangProj80;
using IServiceProvider=Microsoft.VisualStudio.OLE.Interop.IServiceProvider;


namespace at.jku.ssw.Coco.VSPlugin.CodeGeneration {
    // todo  check is it posibble to implement BaseCodeGeneratorWithSite instead the interface IVsSingleFileGenerator directly?
    /// <summary>
    /// This is the generator class. 
    /// When setting the 'Custom Tool' property of a C# project item to "CocoFileGenerator", 
    /// the GenerateCode function will get called and will return the contents of the generated file 
    /// to the project system
    /// </summary>
    [ComVisible(true)]
    [Guid("31D7CD76-CB86-4097-B81A-E9FDE51B1B2E")]
    [CodeGeneratorRegistration(typeof(CocoFileGenerator),
        "C# Coco Parser/Scanner Generator",
        vsContextGuids.vsContextGuidVCSProject,
        GeneratesDesignTimeSource = false)]
    [ProvideObject(typeof(CocoFileGenerator))]
    public class CocoFileGenerator : IVsSingleFileGenerator, IObjectWithSite {
#pragma warning disable 0414
        //The name of this generator (use for 'Custom Tool' property of project item)
        internal static string name = "CocoFileGenerator";
#pragma warning restore 0414
        private object site;
        private ServiceProvider serviceProvider;

        #region IObjectWithSite Members

        /// <summary>
        /// GetSite method of IOleObjectWithSite
        /// </summary>
        /// <param name="riid">interface to get</param>
        /// <param name="ppvSite">IntPtr in which to stuff return value</param>
        void IObjectWithSite.GetSite(ref Guid riid, out IntPtr ppvSite) {
            if (site == null) {
                throw new COMException("object is not sited", VSConstants.E_FAIL);
            }

            IntPtr pUnknownPointer = Marshal.GetIUnknownForObject(site);
            IntPtr intPointer = IntPtr.Zero;
            Marshal.QueryInterface(pUnknownPointer, ref riid, out intPointer);

            if (intPointer == IntPtr.Zero) {
                throw new COMException(
                    "site does not support requested interface",
                    VSConstants.E_NOINTERFACE);
            }

            ppvSite = intPointer;
        }

        /// <summary>
        /// SetSite method of IOleObjectWithSite
        /// </summary>
        /// <param name="pUnkSite">site for this object to use</param>
        void IObjectWithSite.SetSite(object pUnkSite) {
            site = pUnkSite;
            serviceProvider = null;
        }

        #endregion

        /// <summary>
        /// Demand-creates a ServiceProvider
        /// </summary>
        private ServiceProvider SiteServiceProvider {
            get {
                if (serviceProvider == null) {
                    serviceProvider = new ServiceProvider(site as IServiceProvider);
                    Debug.Assert(serviceProvider != null,
                        "Unable to get ServiceProvider from site object.");
                }
                return serviceProvider;
            }
        }

        /// <summary>
        /// Method to get a service by its GUID
        /// </summary>
        /// <param name="serviceGuid">GUID of service to retrieve</param>
        /// <returns>An object that implements the requested service</returns>
        protected object GetService(Guid serviceGuid) {
            return SiteServiceProvider.GetService(serviceGuid);
        }

        /// <summary>
        /// Method to get a service by its Type
        /// </summary>
        /// <param name="serviceType">Type of service to retrieve</param>
        /// <returns>An object that implements the requested service</returns>
        protected object GetService(Type serviceType) {
            return SiteServiceProvider.GetService(serviceType);
        }

        #region IVsSingleFileGenerator Members

        public int DefaultExtension(out string pbstrDefaultExtension) {
            pbstrDefaultExtension = ".log";
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Returns the EnvDTE.ProjectItem object that corresponds to the project item the code 
        /// generator was called on
        /// </summary>
        /// <returns>The EnvDTE.ProjectItem of the project item the code generator was called on</returns>
        protected ProjectItem GetProjectItem() {
            object p = GetService(typeof(ProjectItem));
            Debug.Assert(p != null, "Unable to get Project Item.");
            return (ProjectItem)p;
        }

        private string[] GetCocoArguments(string wszInputFilePath,
            string wszDefaultNamespace) {
            // Get the associated Projetect Item
            ProjectItem pi = GetProjectItem();

            ArrayList al = new ArrayList();
            al.Add(wszInputFilePath);

            try {
                Properties props = pi.DTE.get_Properties("CocoOptions", "General");
                string argstr = (string)props.Item("CocoArguments").Value;
                string[] arg = Regex.Split(argstr, "(\".*\"| )");
                foreach (string token in arg) {
                    if (token.Trim().Length != 0) {
                        if (token.StartsWith("\"") && token.EndsWith("\"")) {
                            al.Add(token.Trim(new char[] { '"' }));
                        }
                        else {
                            al.Add(token);
                        }
                    }
                }
                if (!(bool) props.Item("SkipNamespace").Value && !al.Contains("-namespace"))
                {
                    al.Add("-namespace");
                    al.Add(wszDefaultNamespace);                  
                }
            }
            catch (Exception e) {
                Console.WriteLine("GetCocoArguments failed: " + e.Message);
            }

            return (string[])al.ToArray(typeof(string));
        }

        public int Generate(string wszInputFilePath,
            string bstrInputFileContents, string wszDefaultNamespace,
            IntPtr[] rgbOutputFileContents, out uint pcbOutput,
            IVsGeneratorProgress pGenerateProgress) {
            // Redirect console output
            StringWriter writer = new StringWriter();
            Console.SetOut(writer);



            // Execute coco
            string[] args = GetCocoArguments(wszInputFilePath, wszDefaultNamespace);
            Console.Write("Console Arguments: ");
            foreach (var s in args) { Console.Write(s + " "); }
            Console.WriteLine();

            int retVal = Coco.Main(args);

            // If success, add parser and scanner
            if (retVal == 0) {
                // Get the associated Projetect Item
                ProjectItem pi = GetProjectItem();

                // Check if there are already scanner and parser project items
                bool hasScanner = false;
                bool hasParser = false;
                bool hasTrace = false;

                foreach (ProjectItem sub in pi.ProjectItems) {
                    if (sub.Name.EndsWith("Scanner.cs")) {
                        hasScanner = true;
                    }
                    if (sub.Name.EndsWith("Parser.cs")) {
                        hasParser = true;
                    }
                    if (sub.Name.EndsWith("trace.txt")) {
                        hasTrace = true;
                    }
                }

                if (!hasParser) {
                    pi.ProjectItems.AddFromFile(
                        Path.Combine(Path.GetDirectoryName(wszInputFilePath),
                            "Parser.cs"));
                }

                if (!hasScanner) {
                    pi.ProjectItems.AddFromFile(
                        Path.Combine(Path.GetDirectoryName(wszInputFilePath),
                            "Scanner.cs"));
                }

                if (!hasTrace
                    &&
                    File.Exists(
                        Path.Combine(Path.GetDirectoryName(wszInputFilePath),
                            "trace.txt"))) {
                    pi.ProjectItems.AddFromFile(
                        Path.Combine(Path.GetDirectoryName(wszInputFilePath),
                            "trace.txt"));
                }
            }
            else {
                // Parse console output for error messages
                string errorText = writer.ToString();
                MatchCollection mc = Regex.Matches(errorText,
                    "-- line ([0-9]*) col ([0-9]*)\\: (.*)");

                // Generate error messages
                foreach (Match match in mc) {
                    pGenerateProgress.GeneratorError(0, 1, match.Groups[3].Value,
                        uint.Parse(match.Groups[1].Value) - 1,
                        uint.Parse(match.Groups[2].Value) - 1);
                }
            }

            //Get the Encoding used by the writer. We're getting the WindowsCodePage encoding, 
            //which may not work with all languages
            Encoding enc = Encoding.GetEncoding(writer.Encoding.WindowsCodePage);

            //Get the preamble (byte-order mark) for our encoding
            byte[] preamble = enc.GetPreamble();
            int preambleLength = preamble.Length;

            //Convert the writer contents to a byte array
            byte[] body = enc.GetBytes(writer.ToString());

            //Prepend the preamble to body (store result in resized preamble array)
            Array.Resize(ref preamble, preambleLength + body.Length);
            Array.Copy(body, 0, preamble, preambleLength, body.Length);

            int outputLength = preamble.Length;
            rgbOutputFileContents[0] = Marshal.AllocCoTaskMem(outputLength);
            Marshal.Copy(preamble, 0, rgbOutputFileContents[0], outputLength);
            pcbOutput = (uint)outputLength;

            return (retVal == 0) ? VSConstants.S_OK : VSConstants.S_FALSE;
        }

        #endregion
    }
}