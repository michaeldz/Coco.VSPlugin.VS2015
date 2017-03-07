using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

namespace at.jku.ssw.Coco.VSPlugin.Language {
    /// <summary>
    /// Source implementation specific to coco grammar files.
    /// </summary>
    public class CocoSource : Source {
        // this custom source class could be used to implement the contained language (for semantic actions)
        // every instance of this class corresponds to a single source-file
        // the language-service is responsible for creating an instance of this class

        #region Constructor
        
        public CocoSource(CocoLanguageService service, IVsTextLines textLines,
            Colorizer colorizer)
            : base(service, textLines, colorizer) {       
        }
        #endregion

        #region Methods
               
        public override CommentInfo GetCommentFormat() {
            CommentInfo info = new CommentInfo();
            info.LineStart = "//";
            info.BlockStart = "/*";
            info.BlockEnd = "*/";
            info.UseLineComments = true;
            return info;
        }
        #endregion          
    }
}