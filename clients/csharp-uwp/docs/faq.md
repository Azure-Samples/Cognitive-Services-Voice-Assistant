
# Frequently asked questions

## General

### Where can I get more information on...

- [Custom lighting]()
- [Automatic enablement]()
- [Best practices for voice agents]()

## Implementation

### How do I create a voice application using my own voice agent?

The UWP Sample Application was developed using Direct Line Speech and the Speech Services SDK as a demonstration of how to use a dialog service with MVA. However, the implementation of the dialog service in the application can be easily replaced by implementing the DialogBackend interface and replacing all instances of DirectLineSpeechDialogBackend with your new implementation.

## Debugging

### How do I debug my app starting from when it is closed, then receives a 1st stage activation signal?

Open Visual Studio's Debug->Other Debug Targets->Debug Installed App Package. Check the box that says "Do not launch, but debug my code when it starts". Find the app called "MVADLSSample" in the list of applications, click it, and click "Start" in the bottom right corner of the window. Now, when you voice activate the application while it is closed, the debugger will attach as soon as the app is activated and hit any breakpoints you have set.

## Issues

### I can't get the sample app to voice activate

There are a number of reasons voice activation could be failing. First, make sure to follow all the steps in the [Prerequistes](link) section of the readme. When you run the app, check the voice activation icon in the top corner and ensure that all errors are resolved. Make sure that you are speaking the keyword slowly and clearly.
