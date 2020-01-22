//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

#include <iostream>
#include <fstream>
#include <speechapi_cxx.h>

using namespace std;
using namespace Microsoft::CognitiveServices::Speech;
using namespace Microsoft::CognitiveServices::Speech::Audio;

fstream OpenFile(const string& filename)
{
    if (filename.empty())
    {
        throw invalid_argument("Audio filename is empty");
    }

    fstream fs;
    fs.open(filename, ios_base::binary | ios_base::in);
    if (!fs.good())
    {
        throw invalid_argument("Failed to open the specified audio file.");
    }

    return fs;
}

int ReadBuffer(fstream& fs, uint8_t* dataBuffer, uint32_t size)
{
    if (fs.eof())
    {
        // returns 0 to indicate that the stream reaches end.
        return 0;
    }

    fs.read((char*)dataBuffer, size);

    if (!fs.eof() && !fs.good())
    {
        // returns 0 to close the stream on read error.
        return 0;
    }
    else
    {
        // returns the number of bytes that have been read.
        return (int)fs.gcount();
    }
}

void PushData(PushAudioInputStream* pushStream, const string& filename)
{
    fstream fs;
    try
    {
        fs = OpenFile(filename);
        //skip the wave header
        fs.seekg(44);
    }
    catch (const exception& e)
    {
        cerr << "Error: exception in pushData, %s." << e.what() << endl;
        cerr << "  can't open " << filename << endl;
        throw e;
        return;
    }

    std::array<uint8_t, 1000> buffer;
    while (1)
    {
        auto readSamples = ReadBuffer(fs, buffer.data(), (uint32_t)buffer.size());
        if (readSamples == 0)
        {
            break;
        }
        pushStream->Write(buffer.data(), readSamples);
    }
    fs.close();
    pushStream->Close();
}

void recognizeKeywordSpeech()
{
    // Creates an instance of a speech config with specified subscription key and service region.
    // Replace with your own subscription key and service region (e.g., "westus").
    auto config = SpeechConfig::FromSubscription("YourSubscriptionKey", "YourServiceRegion");
    config->SetProperty(PropertyId::Speech_LogFilename, "SpeechSDK.log");

    // Creates a speech recognizer using file as audio input. The default language is "en-us".
    auto pushStream = AudioInputStream::CreatePushStream();
    auto audioInput = AudioConfig::FromStreamInput(pushStream);
    auto recognizer = SpeechRecognizer::FromConfig(config, audioInput);

    // Promise for synchronization of recognition end.
    promise<void> recognitionEnd;

    // Subscribes to events.
    recognizer->Recognizing.Connect([] (const SpeechRecognitionEventArgs& e)
    {
        if (e.Result->Reason == ResultReason::RecognizingSpeech)
        {
            cout << "RECOGNIZING: Text=" << e.Result->Text << std::endl;
        }
        else if (e.Result->Reason == ResultReason::RecognizingKeyword)
        {
            cout << "RECOGNIZING KEYWORD: Text=" << e.Result->Text << std::endl;
        }
    });

    recognizer->Recognized.Connect([] (const SpeechRecognitionEventArgs& e)
    {
        if (e.Result->Reason == ResultReason::RecognizedKeyword)
        {
            cout << "RECOGNIZED KEYWORD: Text=" << e.Result->Text << std::endl;
        }
        else if (e.Result->Reason == ResultReason::RecognizedSpeech)
        {
            cout << "RECOGNIZED: Text=" << e.Result->Text << std::endl;
        }
        else if (e.Result->Reason == ResultReason::NoMatch)
        {
            cout << "NOMATCH: Speech could not be recognized." << std::endl;
        }
    });

    recognizer->Canceled.Connect([&recognitionEnd](const SpeechRecognitionCanceledEventArgs& e)
    {
        cout << "CANCELED: Reason=" << (int)e.Reason << std::endl;

        if (e.Reason == CancellationReason::Error)
        {
            cout << "CANCELED: ErrorCode=" << (int)e.ErrorCode << "\n"
                 << "CANCELED: ErrorDetails=" << e.ErrorDetails << "\n";

            recognitionEnd.set_value(); // Notify to stop recognition.
        }
    });

    recognizer->SessionStarted.Connect([&recognitionEnd](const SessionEventArgs& e)
    {
        cout << "SESSION STARTED: SessionId=" << e.SessionId << std::endl;
    });

    recognizer->SessionStopped.Connect([&recognitionEnd](const SessionEventArgs& e)
    {
        cout << "SESSION STOPPED: SessionId=" << e.SessionId << std::endl;

        recognitionEnd.set_value(); // Notify to stop recognition.
    });

    // Creates an instance of a keyword recognition model. Update this to
    // point to the location of your keyword recognition model.
    auto model = KeywordRecognitionModel::FromFile("kws.table");

    // Starts continuous recognition. Use StopContinuousRecognitionAsync() to stop recognition.
    PushData(pushStream.get(), "kws_whatstheweatherlike.wav");
    recognizer->StartKeywordRecognitionAsync(model).get();

    // Waits for a single successful keyword-triggered speech recognition (or error).
    recognitionEnd.get_future().get();

    // Stops recognition.
    cout << "Stopping...\n";
    recognizer->StopKeywordRecognitionAsync().get();
    cout << "Stopped.\n";
}


int main(int argc, char **argv) {
    setlocale(LC_ALL, "");
    recognizeKeywordSpeech();
    return 0;
}
