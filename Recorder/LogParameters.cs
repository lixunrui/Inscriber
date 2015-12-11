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

        // Read only Maximum number of files to keep
        readonly int _maxFileNums;

        // Minimum number of files to keep, even if old
        // need this parameter to prevent we delete all logs
        int _minFileNums;

        // Maximum amount of space in MB() all log files should take up to
        // = bytes * 1024 => kb * 1024 => mb
        int _maxTotalSize;
        
        

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

        public int MaxFileNums
        {
            get { return _maxTotalSize/_maxLogSize; }
        }
        
        public int MinFileNums
        {
            get { return _minFileNums; }
            set { _minFileNums = value; }
        }

        public int MaxTotalSize
        {
            get { return _maxTotalSize; }
            set { _maxTotalSize = value; }
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
            MaxLogSize = 1024 * 1024; // default to 1m
            MaxTotalSize = 10 * 1024 * 1024; // default to 10m => upto 10 files
        }
    }
}
