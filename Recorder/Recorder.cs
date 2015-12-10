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

        public Recorder(String logBaseName, LogParameters logParams)
        {
            Init(logBaseName, logParams);
        }
        ~Recorder()
        {
            this.Dispose();
        }


        void Init(String baseName, LogParameters logParams)
        {
            _logBaseName = baseName;
            _params = logParams;
            _logFileOpened = false;
            LogPlain("-------------------------------");
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

        public void TestLogException(Exception e)
        {
            LogExceptions(e);
        }

        public void Log(LogLevel level, String message, bool withTime)
        {
            if (level >= _currentLogLevel) // current level is Normal
            {
                locker.WaitOne();
                try
                {
                    OpenFile();

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

        /// <summary>
        /// Check if the directory exists, otherwise we will create the directory
        /// </summary>
        void CheckDirectoryExists()
        {
            if (! Directory.Exists(_params.LogsDirectory))
            {
                Directory.CreateDirectory(_params.LogsDirectory);
            }
        }

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
