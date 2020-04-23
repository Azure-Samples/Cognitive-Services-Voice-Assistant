# Understanding the sample app
This is a detailed breakdown of the steps to build a Windows voice assistant using the Windows Conversational Agent system along with an explanation of how the UWP Voice Assistant sample implements each step. Please read the [readme](https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/README.md) for this sample for more information on the Windows Conversational Agent system, Direct Line Speech, and the UWP Voice Assistant Sample.

## Steps

- Setup: Prepare your app to receive an activation signal from the Windows Conversational Agent system when the application's keyword is spoken.
- Keyword verification: Verify the activation signal using local and cloud keyword verification models.
- Turn logic: Use a dialog service to enable your user to converse with your agent and update the Windows Conversational Agent system with the state of your voice agent.
- Enable above-lock: 