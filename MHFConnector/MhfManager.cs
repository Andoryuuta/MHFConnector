using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels.Ipc;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace MHFConnector
{
    class MhfManager {
        readonly string redirectionHost;
        readonly string execPath;

        private string channelName = null;
        private IpcServerChannel ipcServer = null;
        private Int32 remotePID;
        
        public MhfManager(string redirectionHost, string execPath)
        {
            this.redirectionHost = redirectionHost;
            this.execPath = execPath;
        }

        public void LaunchGame()
        {
            // Create the IPC server using the FileMonitorIPC.ServiceInterface class as a singleton
            ipcServer = EasyHook.RemoteHooking.IpcCreateServer<MHFHook.ServerInterface>(ref channelName, System.Runtime.Remoting.WellKnownObjectMode.Singleton);

            // Get the full path to the assembly we want to inject into the target process
            string injectionLibrary = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "MHFHook.dll");

            // start and inject into a new process
            EasyHook.RemoteHooking.CreateAndInject(
                execPath,           // executable to run
                "",                 // command line arguments for target
                0,                  // additional process creation flags to pass to CreateProcess
                EasyHook.InjectionOptions.DoNotRequireStrongName, // allow injectionLibrary to be unsigned
                injectionLibrary,   // 32-bit library to inject (if target is 32-bit)
                injectionLibrary,   // 64-bit library to inject (if target is 64-bit)
                out remotePID,      // retrieve the newly created process ID
                channelName         // the parameters to pass into injected library
                                    // ...
            );
        }
    }
}
