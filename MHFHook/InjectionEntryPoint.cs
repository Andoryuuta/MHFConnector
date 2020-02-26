using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MHFHook
{
    /// <summary>
    /// The easyhook entry point class for this injected managed library.
    /// </summary>
    public class InjectionEntryPoint : EasyHook.IEntryPoint
    {
        // The IPC interface.
        ServerInterface _server = null;

        // stdout message queue.
        Queue<string> _messageQueue = new Queue<string>();

        public InjectionEntryPoint(EasyHook.RemoteHooking.IContext context, string channelName)
        {
            // Connect to server object using provided channel name
            _server = EasyHook.RemoteHooking.IpcConnectClient<ServerInterface>(channelName);

            // If Ping fails then the Run method will be not be called
            _server.Ping();
        }

        /// <summary>
        /// Run is the actual entry point method that easyhook calls.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="channelName"></param>
        public void Run(EasyHook.RemoteHooking.IContext context, string channelName)
        {
            // Injection is now complete and the server interface is connected
            _server.IsInstalled(EasyHook.RemoteHooking.GetCurrentProcessId());

            // Install hooks
            var createMutexHook = EasyHook.LocalHook.Create(
                EasyHook.LocalHook.GetProcAddress("kernel32.dll", "CreateMutexA"),
                new CreateMutexA_delegate(CreateMutexA_Hook),
                this);

            var openMutexHook = EasyHook.LocalHook.Create(
                EasyHook.LocalHook.GetProcAddress("kernel32.dll", "OpenMutexA"),
                new OpenMutexA_delegate(OpenMutexA_hook),
                this);


            // Activate hook on all threads except the current thread
            createMutexHook.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            openMutexHook.ThreadACL.SetExclusiveACL(new Int32[] { 0 });

            _server.ReportMessage("CreateMutexA hook installed");

            // Wake up the process (required if using RemoteHooking.CreateAndInject)
            EasyHook.RemoteHooking.WakeUpProcess();

            try
            {
                // Loop until IPC fails
                while (true)
                {
                    System.Threading.Thread.Sleep(500);

                    // Safely pull messages out of queue into an array.
                    string[] queued = null;
                    lock (_messageQueue)
                    {
                        queued = _messageQueue.ToArray();
                        _messageQueue.Clear();
                    }

                    // Send the messages.
                    if (queued != null && queued.Length > 0)
                    {
                        _server.ReportMessages(queued);
                    }

                    // Ping the server to check connection.
                    _server.Ping();
                }
            }
            catch
            {
                // Ping() or ReportMessages() will raise an exception if host is unreachable
            }

            // Remove hook
            createMutexHook.Dispose();
            openMutexHook.Dispose();

            // Finalise cleanup of hooks
            EasyHook.LocalHook.Release();
        }




        #region CreateMutexA hook
        /// <summary>
        /// The original function via P/Invoke
        /// </summary>
        /// <param name="lpMutexAttributes"></param>
        /// <param name="bInitialOwner"></param>
        /// <param name="lpName"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern IntPtr CreateMutexA(IntPtr lpMutexAttributes, bool bInitialOwner, string lpName);

        /// <summary>
        /// CreateMutexA delegate for our hook.
        /// </summary>
        /// <param name="lpMutexAttributes"></param>
        /// <param name="bInitialOwner"></param>
        /// <param name="lpName"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true, CharSet = CharSet.Ansi)]
        delegate IntPtr CreateMutexA_delegate(IntPtr lpMutexAttributes, bool bInitialOwner, string lpName);

        /// <summary>
        /// Our CreateMutexA hook.
        /// </summary>
        /// <param name="lpMutexAttributes"></param>
        /// <param name="bInitialOwner"></param>
        /// <param name="lpName"></param>
        /// <returns></returns>
        IntPtr CreateMutexA_Hook(IntPtr lpMutexAttributes, bool bInitialOwner, string lpName)
        {
            /*
            this._messageQueue.Enqueue(
                string.Format("[{0}]: CreateMutexA: \"{1}\"",
                EasyHook.RemoteHooking.GetCurrentProcessId(), lpName));
            */

            string newName = String.Format("{0}{1}", lpName, EasyHook.RemoteHooking.GetCurrentProcessId());
            return CreateMutexA(lpMutexAttributes, bInitialOwner, newName);
        }

        #endregion



        #region OpenMutexA hook
        /// <summary>
        /// The original function via P/Invoke
        /// </summary>
        /// <param name="dwDesiredAccess"></param>
        /// <param name="bInheritHandle"></param>
        /// <param name="lpName"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern IntPtr OpenMutexA(UInt32 dwDesiredAccess, bool bInheritHandle, string lpName);
        
        /// <summary>
        /// OpenMutexA delegate for our hook.
        /// </summary>
        /// <param name="dwDesiredAccess"></param>
        /// <param name="bInheritHandle"></param>
        /// <param name="lpName"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true, CharSet = CharSet.Ansi)]
        delegate IntPtr OpenMutexA_delegate(UInt32 dwDesiredAccess, bool bInheritHandle, string lpName);

        /// <summary>
        /// Our OpenMutexA hook.
        /// </summary>
        /// <param name="dwDesiredAccess"></param>
        /// <param name="bInheritHandle"></param>
        /// <param name="lpName"></param>
        /// <returns></returns>
        IntPtr OpenMutexA_hook(UInt32 dwDesiredAccess, bool bInheritHandle, string lpName)
        {
            /*
            this._messageQueue.Enqueue(
                string.Format("[{0}]: OpenMutexA: \"{1}\"",
                EasyHook.RemoteHooking.GetCurrentProcessId(), lpName));
            */

            string newName = String.Format("{0}{1}", lpName, EasyHook.RemoteHooking.GetCurrentProcessId());
            return OpenMutexA(dwDesiredAccess, bInheritHandle, newName);
        }

        #endregion


    }
}
