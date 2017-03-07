/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;

namespace at.jku.ssw.Coco.VSPlugin.Language {
    /// <summary>
    /// This class (contained in VS SDK 2010\VisualStudioIntegration\Common\Source\CSharp\Babel) represents a declaration which is used for code-completion.
    /// </summary>
    public struct CocoDeclaration : IComparable<CocoDeclaration> {
        public CocoDeclaration(string description, string displayText, int glyph, string name) {
            this.Description = description;
            this.DisplayText = displayText;
            this.Glyph = glyph;
            this.Name = name;
        }

        public string Description;
        public string DisplayText;
        public int Glyph;
        public string Name;

        public int CompareTo(CocoDeclaration other) {
            return Name.CompareTo(other.Name);
        }
    }
}