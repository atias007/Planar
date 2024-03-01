# This folder contains the monitor hooks of planar

### In short...
Each hook is a console application with class implementing `BaseHook` abstract class.
Then in `Program.cs` class invoke the hook by calling `PlanarHook.Start<ClassName>();`

The hook is called by the monitor when the event is triggered. 
The hook can be used to send email, sms, slack message, publish pub sub message, etc.