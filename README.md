# MHFConnector
MHFConnector is a (really) small little utility to change the MHF hosts in your `hosts` file to easily connect to a different MHF server, as well as allowing multiple clients to run.

**Requires a modified client with gameguard removed to work.**

![screenshot](https://github.com/Andoryuuta/MHFConnector/raw/master/ss/screenshot.png)

# About
Prebuilt versions are available on the [releases page](https://github.com/Andoryuuta/MHFConnector/releases).


## Internals:
This is basically just two existing libraries cobbled together:
* [PSHostsFile](https://github.com/fschwiet/PSHostsFile) for modifying the hosts file without overriding it.
* [EasyHook](https://github.com/EasyHook/EasyHook) for hooking `CreateMutexA` and `OpenMutexA` for multi-client purposes. (Almost copied directly from the [example code](https://github.com/EasyHook/EasyHook-Tutorials/tree/master/Managed/RemoteFileMonitor))


In specific, all this does is add the specified IP to the hosts file:

E.g.
```
127.0.0.1 mhfg.capcom.com.tw
127.0.0.1 mhf-n.capcom.com.tw
127.0.0.1 cog-members.mhf-z.jp
127.0.0.1 www.capcom-onlinegames.jp
127.0.0.1 srv-mhf.capcom-networks.jp
```

And essentially makes `CreateMutexA|OpenMutexA (mutexName)` calls into `CreateMutexA|OpenMutexA (mutexName+processID)` calls, breaking the multi-client dectection mechanism.
