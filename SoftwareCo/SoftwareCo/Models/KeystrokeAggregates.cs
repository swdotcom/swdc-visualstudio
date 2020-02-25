using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftwareCo
{
    public class KeystrokeAggregates
    {
        public long add { get; set; }
        public long close { get; set; }
        public long delete { get; set; }
        public long linesAdded { get; set; }
        public long linesRemoved { get; set; }
        public long open { get; set; }
        public long paste { get; set; }
        public long keystrokes { get; set; }
        public string directory { get; set; }

        public void Aggregate(FileInfoSummary fileInfo)
        {
            this.add += fileInfo.add;
            this.close += fileInfo.close;
            this.delete += fileInfo.delete;
            this.linesAdded += fileInfo.linesAdded;
            this.linesRemoved += fileInfo.linesRemoved;
            this.open += fileInfo.open;
            this.paste += fileInfo.paste;
            this.keystrokes += fileInfo.keystrokes;
            
        }
    }
}
