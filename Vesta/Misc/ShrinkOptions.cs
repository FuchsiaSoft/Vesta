using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vesta.Misc
{
    public enum SaveOption { Overwrite, SaveNew }

    class ShrinkOptions
    {
        public SaveOption SaveOption { get; set; }
        public string NewFolder { get; set; }
        public int EncodingQuality { get; set; }
        public bool RetainCreationDate { get; set; }
        public bool RetainAccessedDate { get; set; }
        public bool RetainModifiedDate { get; set; }
        public bool RetainAttributes { get; set; }
    }
}
