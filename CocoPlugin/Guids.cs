// Guids.cs
// MUST match guids.h
using System;

namespace at.jku.ssw.Coco.VSPlugin
{
    static class GuidList
    {
        //we need the guid-strings when using the guids in an attribute-declaration [Guid(..)]
        public const string guidCocoPluginPkgString = "63c2b4b8-8a51-4fc4-b8ed-0b49e24146c3";
        public const string guidCocoPluginCmdSetString = "1e883179-091f-40f1-81bc-df17fe3287e0";
        public const string guidPageGeneralString = "5B5BA0FC-C1C9-411B-8665-E679968CFC42";
        public const string guidAttributedGrammarServiceString = "DCDEB5D4-36A2-459D-A937-FA700CE38DD0";
        public const string guidLibraryString = "8A4FE235-727F-449A-9867-61218D393152";
        public const string guidErrorProviderString = "F0E8658C-0F04-46F6-861D-1A854B2AF014";

        public static readonly Guid guidCocoPluginPkg = new Guid(guidCocoPluginPkgString);
        public static readonly Guid guidCocoPluginCmdSet = new Guid(guidCocoPluginCmdSetString);
        public static readonly Guid guidPageGeneral = new Guid(guidPageGeneralString);
        public static readonly Guid guidAttributedGrammarService = new Guid(guidAttributedGrammarServiceString);
        public static readonly Guid guidLibrary = new Guid(guidLibraryString);
        public static readonly Guid guidErrorProvider = new Guid(guidErrorProviderString);
    };
}