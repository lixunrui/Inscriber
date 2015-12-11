using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LXR.Recorder
{
    internal class Comparer : IComparer<DateTime>
    {

        #region IComparer<DateTime> Members

        public int Compare(DateTime x, DateTime y)
        {
            return x.CompareTo(y);
        }

        #endregion
    }
}
