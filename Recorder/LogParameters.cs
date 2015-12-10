using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LXR.Recorder
{
    public class LogParameters
    {
        // Directory the logs will be placed in 
        String _logsDirectory;

        // The maximum number of days the logs will be kept
        int _maxSaveDays;

        // Maximum size of each log file
        int _maxLogSize;

        //// Maximum number of files to keep
        //int _maxFileNums;

        // Minimum number of files to keep, even if old
        // need this parameter to prevent we delete all logs
        int _minFileNums;
        

        public System.String LogsDirectory
        {
            get { return _logsDirectory; }
            set { _logsDirectory = value; }
        }

        public int MaxSaveDays
        {
            get { return _maxSaveDays; }
            set { _maxSaveDays = value; }
        }

        public int MaxLogSize
        {
            get { return _maxLogSize; }
            set { _maxLogSize = value; }
        }

        //public int MaxFileNums
        //{
        //    get { return _maxFileNums; }
        //    set { _maxFileNums = value; }
        //}
        
        public int MinFileNums
        {
            get { return _minFileNums; }
            set { _minFileNums = value; }
        }
    }

    /// <summary>
    /// Class for default set of log parameters
    /// </summary>
    public class DefaultParameters : LogParameters
    {
        public DefaultParameters()
        {
            MinFileNums = 2;
            MaxSaveDays = 10;
            MaxLogSize = 1000 * 1024;
        }
    }
}
