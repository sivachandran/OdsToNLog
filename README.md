Introduction
============
OdsToNLog is a very simple program which captures Win32 OutputDebugString messages and converts them into [NLog](http://nlog-project.org/) log messages. 

Log Levels
==========
OutputDebugString messages doesn't carry any log level(i.e. warning, info, error) information. But it is general practice prefix strings like "[WARN]", "[INFO]" and "[ERROR]" to the log messages to denote a particular message as warning, info or error. In OdsToNLog we can define(in OdsToNLog.exe.config) regex patterns to identify the log levels from OutputDebugString messages.

Log Targets
===========
OutputDebugString messages can be redirected to one or more targets supported NLog. We can define these targets(e.g. Network, File, Mail) in OdsToNLog.exe.config. For more information about NLog targets please visit [here](http://nlog-project.org/wiki/Targets)

Capturing OutputDebugString Messages
===========================================
OdsToNLog uses [DbMon.NET](http://www.codeproject.com/Articles/13345/DbMon-NET-A-simple-NET-OutputDebugString-capturer) by Christian Birkl to capture Win32 OutputDebugString messages in managed code. The DbMon.NET is modified to capture both local and global scope messages.

Capturing Messages From Global Scope
===========================================
OdsToNLog can capture OutputDebugString messages sent from processes in Windows "Global" scope. Typical example for this is OutputDebugString messages sent from Windows services. We can instruct OdsToNLog to capture gloabl scope messages by setting the config "CaptureGlobal" to "true"(default is "false") in OdsToNlog.exe.config file. To capture global scope messages OdsToNLog.exe needs to be run in administrator's privilages(i.e. Run as administrator).

Download
========
You can find the prebuilt binary in Downloads section.