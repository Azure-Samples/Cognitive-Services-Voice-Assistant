# Voice Assistant Test Generator

The Voice Assistant Test Generator (VATG) is a tool used to generate test files for the [Voice Assistant Test tool](../voice-assistant-test) (VAT).

Currently the VAT uses a json format that can be hard to edit en masse. This tool makes editing and creating the tests much easier. The input to this application will be a tab separated text file and the output will be a properly formatted test file used as input to the VAT. Currently this only supports text inputs to be sent as a message activity and does not support specifying wav files or custom activities. Only single turn tests are supported.

## Building the code

clone the repo.

In this folder run 
```
dotnet build
```

There will be an exe in the bin\Release\netcoreapp3.1\win-x64\publish titled "VoiceAssistantTestGenerator.exe".

## Using the Test Generator

From a command line run:
```
VoiceAssistantTestGenerator.exe -i myTestFile.txt
```
Or
```
VoiceAssistantTestGenerator.exe -inputFile myTestFile.txt
```

The default output file is titled "DefaultOutput.json" and will be in the same directory.

If you would like to specify the output file you can:

```
VoiceAssistantTestGenerator.exe -i myTestFile.txt -o myOutput.json
```
Or
```
VoiceAssistantTestGenerator.exe -inputFile myTestFile.txt -outputFile myOutput.json
```

## The Test File

The input test file will need to be a tab separate file where the first row is the headers of the columns. The first header must be "Utterance" and the first column will be the text sent as a message activity. This is useful for when you assume the speech recognition has recognized a specific text. Wav files are currently not supported in this format.

After the first row each row is used to specify a test case. The important values are after the first column. This is where you will specifiy the activity you expect to receive from the service. Currently only one activity is supported.

Each column's header should specify the schema of that property. Currently all values are assumed to be strings. Basic json types are not supported.

It is supported to have in depth objects. Currently if no braces are specified all properties are expected to be in the top level of the received activity. If you wish to specify depth simply add an opening brace to the object that has depth and closing brace to the last element.

```
Utterance | type | complexObject{ | subProperty1 | subProperty2}
```

Currently Lists are not supported.

Example test file ( '|' represents a tab). This test file defines messages that expect to correctly be interpreted to set a timer for 30 seconds and the service returns an activity with the extracted information as well as an added field about what the alarm should be.

```
Utterance                   | type  | details{ | duration | alarm}
Set a timer for 30 seconds  | timer |          | "30"     | "audible"
Make a timer for 30 seconds | timer |          | "30"     | "audible"
```