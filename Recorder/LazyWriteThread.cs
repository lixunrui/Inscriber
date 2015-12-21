using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace LXR.Recorder
{
    internal class LazyWriteThread
    {
        Thread _workThread;

        bool _timeToFlush = false;
        bool _lazyerRunning = false;

        WeakReference _Recorder;

        // use to set a flag, indicate that the writer can be stopped
        EventWaitHandle endEvent = new EventWaitHandle(false, EventResetMode.ManualReset);

        // handlers has two event wait handler:
        // 1: the EWH passed by the Start function from Recorder, indicate it's time to start write
        // 2: a EWH uses to indicate the lazy writer should stop
        EventWaitHandle[] handles = new EventWaitHandle[2];

        int _flushTimer;

        /// <summary>
        /// Init a lazy writer
        /// </summary>
        /// <param name="recorder">a weak reference to the recorder</param>
        /// <param name="startWriteHandler"></param>
        /// <param name="flushTimer">In Seconds</param>
        public void Start(Recorder recorder, EventWaitHandle startWriteHandler, int flushTimer)
        {
            _Recorder = new WeakReference(recorder);
            handles[0] = startWriteHandler;
            handles[1] = endEvent;

            _flushTimer = flushTimer * 1000;

            _lazyerRunning = true;

            _workThread = new Thread(LazyWriterStart);
            _workThread.IsBackground = true;
            _workThread.Start();
        }

        void LazyWriterStart()
        {
            try
            {
            	while (_lazyerRunning)
            	{
                    int waitCommand = EventWaitHandle.WaitAny(handles, _flushTimer, false);

                    switch (waitCommand)
                    {
                        case 0: // start write handler
                            _timeToFlush = true;
                            continue;
                        case 1: // exit handler
                            _lazyerRunning = false;
                            break;
                        default: // timeout
                            if (_timeToFlush)
                            {
                                if (_Recorder.IsAlive)
                                {
                                    (_Recorder.Target as Recorder).FileWriter.Flush();
                                }
                                else // the record object has been collected
                                {
                                    _lazyerRunning = false;
                                }
                                _timeToFlush = false;
                            }
                            break;
                    }
            	}
            }
            catch (System.Exception ex)
            {
                (_Recorder.Target as Recorder).LogExceptions(ex);
            }
        }

        internal void EndLazyWriter()
        {
            try
            {
                endEvent.Set(); // tell the lazy writer to stop
                _workThread.Join(1000);
            }
            catch (Exception)
            {
                // don't care 
            }
        }
    }
}
