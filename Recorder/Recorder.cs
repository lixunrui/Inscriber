//////////////////////////////////////////////////////////////////////////
/* Read Me
 * The logger will request some parameters like file lifetime, max size, file directory 
 * which will be provided by constructor parameters or a class
 * 
 * */
//////////////////////////////////////////////////////////////////////////


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Reflection;

namespace LXR.Recorder
{
    public class Recorder : IDisposable
    {
        /// <summary>
        /// Level of logging
        /// </summary>
        public enum LogLevel
        {
            // Log all debug messages
            Debug = 0,
            // Log all detail messages
            Detail,
            // Log only normal messages
            Normal,
        }

#region Properties
        // locker: protecting thread safe
        Mutex locker = new Mutex(false);

        // Log file settings
        FileStream _fileStream;
        StreamWriter _fileWriter;
        String _logBaseName;
        String _logFilePath;

        LogParameters _params;

        // set a default logging level
        LogLevel _currentLogLevel = LogLevel.Normal;

        // flags
        Boolean _logFileOpened;
#endregion

#region Constructions & Deconstructions
        public Recorder(String logBaseName, LogParameters logParams)
        {
            Init(logBaseName, logParams);
        }
        ~Recorder()
        {
            this.Dispose();
        }
#endregion

#region Support Functions
        void Init(String baseName, LogParameters logParams)
        {
            _logBaseName = baseName;
            _params = logParams;
            _logFileOpened = false;
            LogPlain("-------------------------------");
        }

        /// <summary>
        /// Check if the directory exists, otherwise we will create the directory
        /// </summary>
        void CheckDirectoryExists()
        {
            if (!Directory.Exists(_params.LogsDirectory))
            {
                Directory.CreateDirectory(_params.LogsDirectory);
            }
        }

        void OpenFile()
        {
            if (!_logFileOpened)
            {
                CheckDirectoryExists();

                _logFilePath = Path.Combine(_params.LogsDirectory, _logBaseName + ".log");

                _fileStream = new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);

                _fileWriter = new StreamWriter(_fileStream);

                // make sure the FileSteam and FileWrite will not be closed, unless we tell the GC to do so
                // will use Dispose to close them
                GC.SuppressFinalize(_fileStream);
                GC.SuppressFinalize(_fileWriter);

                _logFileOpened = true;
            }
        }

        /// <summary>
        /// This is a testing function, should be the same as OpenFile() function, but we will see...
        /// </summary>
        /// <param name="test"></param>
        void OpenFile(bool test)
        {
            CheckDirectoryExists();
            _logFilePath = Path.Combine(_params.LogsDirectory, _logBaseName + ".log");

            if (!_fileStream.CanWrite)
            {
                _fileStream = new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            }
            if (_fileWriter.BaseStream == null)
            {
                _fileWriter = new StreamWriter(_fileStream);
            }
        }

        /// <summary>
        /// Close the file stream
        /// </summary>
        void CloseFile()
        {
            try
            {
                if (_fileWriter != null)
                {
                    _fileWriter.Close();
                    _fileWriter.Dispose();
                    _fileStream.Dispose();
                }
            }
            catch (System.Exception)
            {

            }
            _logFileOpened = false;
        }

        /// <summary>
        /// Archive current log file and delete old logs
        /// Note: If current log greater than max log size
        /// </summary>
        void ArchiveFiles()
        {
            bool differentDay = File.GetLastWriteTime(_logFilePath).Day != DateTime.Now.Day;

            if (_fileWriter.BaseStream.Length > _params.MaxLogSize)
            {
                string dateName = File.GetLastWriteTime(_logFilePath).ToString("yyyyMMdd_HHmm");

                WriteLogCompleteMessage();

                CloseFile();

                int sequence = -1;
                string archiveFileName;

                do 
                {
                    sequence++;
                    archiveFileName = Path.Combine(_params.LogsDirectory, string.Format("{0}_{1}_{2}.log", _logBaseName, dateName, sequence.ToString()));
                } while (File.Exists(archiveFileName));

                // rename the current log file
                File.Move(_logFilePath, archiveFileName);

                DeleteOldFiles();

                OpenFile();

                LogDateTimeMarker();

                LogPlain("Log continues from " + Path.GetFileName(archiveFileName));
            }
            else if (differentDay)
            {
                LogDateTimeMarker();
            }
        }

        /// <summary>
        /// Delete old archive files of same base name as 
        /// </summary>
        void DeleteOldFiles()
        {
            // get all logs that share the same base name
            string[] dirList = Directory.GetFiles(_params.LogsDirectory, _logBaseName + "*_*_*.log");

            if (dirList.Length <= _params.MinFileNums) return;

            SortedList<DateTime, string> sortedFileListByDate = new SortedList<DateTime, string>(new Comparer());

            DateTime oldestDate = DateTime.Now.AddDays(-_params.MaxSaveDays);

            foreach (string fileName in dirList)
            {
                // A file name should be like: 
                // XXXName_Date_Time_sequence.log
                // Date:        yyyyMMdd
                // Time:        HHmm
                // Sequence:    #.log
                string[] fileParts = fileName.Split('_');
                if (fileParts.Length != 4) continue;

                if (fileParts[1].Length != 8 ||
                    fileParts[2].Length != 4 ||
                    fileParts[3].Length < 5) continue;

                DateTime fileTime = File.GetLastWriteTime(fileName);

                try
                {
                    // in case if there is any duplicate time 
                    sortedFileListByDate.Add(fileTime, fileName);
                }
                catch (System.Exception ex)
                {
                    LogExceptions(ex);
                }
            } // end foreach


            foreach (KeyValuePair<DateTime, string> kv in sortedFileListByDate)
            {
                if (kv.Key < oldestDate )
                {
                    try
                    {
                        locker.WaitOne();
                        File.Delete(kv.Value);
                        locker.ReleaseMutex();
                    }
                    catch (System.Exception ex)
                    {
                        LogExceptions(ex);
                    }
                }
            }

            if (sortedFileListByDate.Count > _params.MaxFileNums)
            {
                for (int index = _params.MaxFileNums - 1; index < sortedFileListByDate.Count; index++ )
                {
                    try
                    {
                        locker.WaitOne();
                        File.Delete(sortedFileListByDate.Values[index]);
                        locker.ReleaseMutex();
                    }
                    catch (System.Exception ex)
                    {
                        LogExceptions(ex);
                    }
                    
                }
            }

        }

        /// <summary>
        /// 
        /// </summary>
        void WriteLogCompleteMessage()
        {
            try
            {
                LogDateTimeMarker();
                _fileWriter.WriteLine("Log archived");
            }
            catch (System.Exception ex)
            {
                LogExceptions(ex);
            }
        }

#endregion

#region Write Functions
        /// <summary>
        /// Log a start message in log files using the assembly reference
        /// </summary>
        /// <param name="assem"></param>
        void LogStart(System.Reflection.Assembly assem)
        {
            try
            {
                AssemblyName assembly = assem.GetName();

                String message = String.Format("Starting : {0} Version {1}", assembly.Name, assembly.Version);

                Log(LogLevel.Normal, message, true);
            }
            catch (System.Exception ex)
            {
                LogExceptions(ex);
            }
        }

        /// <summary>
        /// Write normal message line to log file, without any timestamps in front
        /// </summary>
        /// <param name="message"></param>
        /// <param name="parameters"></param>
        void LogPlain(String message, params object[] parameters)
        {
            try
            {
                if (parameters.Length == 0)
                {
                    Log(LogLevel.Normal, message, false);
                }
                else
                {
                    Log(LogLevel.Normal, String.Format(message, parameters), false);
                }
            }
            catch (System.Exception ex)
            {
                LogExceptions(ex);
            }
        }

        void LogDateTimeMarker()
        {
            try
            {
                _fileWriter.WriteLine("*** Time: "+DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                _fileWriter.Flush();
            }
            catch (System.Exception ex)
            {
                LogExceptions(ex);
            }
        }

        /// <summary>
        /// Log message into to the log file    
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="message"></param>
        /// <param name="withTime">True: Log the message with time stamp</param>
        private void Log(LogLevel level, String message, bool withTime)
        {
            if (level >= _currentLogLevel) // current level is Normal
            {
                locker.WaitOne();
                try
                {
                    OpenFile();

                    ArchiveFiles();

                    if (withTime)
                    {
                        _fileWriter.WriteLine(String.Format("{0} {1}", DateTime.Now.ToString("HH:mm:ss.fff"), message));
                    }
                    else
                    {
                        _fileWriter.WriteLine(message);
                    }

                    // in here we always Flush the content out, will change later
                    #region TODO: change the flush

                    #endregion
                    _fileWriter.Flush();

                }
                catch (FileNotFoundException)
                {
                    CloseFile();
                    throw;
                }
                catch (IOException)
                {
                    CloseFile();
                    throw;
                }

                locker.ReleaseMutex();
            }
        }

#region Public Log functions
        /// <summary>
        /// Log debug message into log files
        /// </summary>
        /// <param name="module">Name of module logging</param>
        /// <param name="message"></param>
        /// <param name="parameters"></param>
        public void LogDebug(string module, string message, params object[] parameters)
        {
            try
            {
                if (parameters.Length == 0)
                {
                    Log(LogLevel.Debug, module + " : " + message, true);
                }
                else
                    Log(LogLevel.Debug, module + " : " + string.Format(message, parameters), true);
            }
            catch (System.Exception ex)
            {
                LogExceptions(ex);
            }
        }

        /// <summary>
        /// Log Normal message into log files
        /// </summary>
        /// <param name="module">Name of module logging</param>
        /// <param name="message"></param>
        /// <param name="parameters"></param>
        public void LogNormal(string module, string message, params object[] parameters)
        {
            try
            {
                if (parameters.Length == 0)
                {
                    Log(LogLevel.Normal, module + " : " + message, true);
                }
                else
                    Log(LogLevel.Normal, module + " : " + string.Format(message, parameters), true);
            }
            catch (System.Exception ex)
            {
                LogExceptions(ex);
            }
        }

        /// <summary>
        /// Log message line to log files
        /// </summary>
        /// <param name="level"></param>
        /// <param name="format"></param>
        /// <param name="parameters"></param>
        public void LogLine(LogLevel level, string format, params object[] parameters)
        {
            try
            {
                if (parameters.Length == 0)
                {
                    Log(level, format, true);
                }
                else
                    Log(level, string.Format(format, parameters), true);
            }
            catch (System.Exception ex)
            {
                LogExceptions(ex);
            }
        }
#endregion

#endregion

        #region Exception Handler
        // Public function to test exception 
        // HACK: remove this later
        public void TestLogException(Exception e)
        {
            LogExceptions(e);
        }

        /// <summary>
        /// Write exception details to another exception file named as: Recorder_Exception.log
        /// </summary>
        /// <param name="e"></param>
        void LogExceptions(Exception e)
        {
            try
            {
                CheckDirectoryExists();

                String exceptionFilePath = Path.Combine(_params.LogsDirectory, "Recorder_Exception.log");

                FileStream fs = new FileStream(exceptionFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);

                StreamWriter exceptionStream = new StreamWriter(fs);

                // If the file is too big, then we drop the old one and re-create a new file.
                if (fs.Length > 10000)
                {
                    exceptionStream.Dispose();
                    fs.Dispose();

                    fs = new FileStream(exceptionFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                    exceptionStream = new StreamWriter(fs);
                }

                exceptionStream.WriteLine("*** Time: " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.hhh") + " ***");

                exceptionStream.WriteLine("Exception : " + e.Message);

                exceptionStream.WriteLine("Stack Trace:");

                exceptionStream.WriteLine(e.StackTrace);

                exceptionStream.WriteLine("--------------------------------------------------------------------");

                exceptionStream.Flush();

                exceptionStream.Close();

                fs.Close();
            }
            catch (System.Exception)
            {
                // ignore 
            }
        }
#endregion

#region TODO: IDisposable Members

        public void Dispose()
        {
            locker.WaitOne();
            if (_fileWriter!=null)
            {
                LogPlain("Logging Closed");
                CloseFile();
            }
        }

#endregion
    }
}
