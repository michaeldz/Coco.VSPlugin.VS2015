using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace at.jku.ssw.Coco {
    public class AddTokenInfo {
        public AddTokenInfo() {

        }
        public AddTokenInfo(string name, int line, int col)
            : this(name, line, col, null) {
        }
        public AddTokenInfo(string name, int line, int col, string additionalInfo) {
            Name = name;
            Line = line;
            Col = col;
            AdditionalInfo = additionalInfo;
        }
        public string Name { get; set; }
        public int Line { get; set; }
        public int Col { get; set; }
        public string AdditionalInfo { get; set; }
    }
}
