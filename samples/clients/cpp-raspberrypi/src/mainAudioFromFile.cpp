#include <iostream>
#include <string> 
#include <fstream>
#include <alsa/asoundlib.h>
#include "speechapi_cxx.h"
#include "json.hpp"
#include "LinuxAudioPlayer.h"

using namespace std; 
using namespace Microsoft::CognitiveServices::Speech::Dialog;
using namespace Microsoft::CognitiveServices::Speech;
using namespace Microsoft::CognitiveServices::Speech::Audio;
using namespace AudioPlayer;

void log()
{
    cout << endl;
}

template<typename T, typename... Args>
void log(T v, Args... args)
{
    cout << v << flush;
    log(args...);
}

template<typename T, typename... Args>
void log_t(T v, Args... args)
{
    cout << chrono::duration_cast<chrono::milliseconds>(chrono::system_clock::now().time_since_epoch()).count() % 10000000 << "  ";
    log(v, args...);
}

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

int main () 
{
    string s;
    int rc;
    const char * device = "default";
    
    std::shared_ptr<DialogServiceConfig> config = BotFrameworkConfig::FromSubscription("c29bbe9dad89459dbaa31e47caa11f60", "westus");
    //std::shared_ptr<SpeechConfig> config = SpeechConfig::FromSubscription("c29bbe9dad89459dbaa31e47caa11f60", "westus");

    //config->SetProperty(PropertyId::Speech_LogFilename, "/sample/log.txt");
        // Creates a speech recognizer using file as audio input. The default language is "en-us".
    auto pushStream = AudioInputStream::CreatePushStream();
    auto audioInput = AudioConfig::FromStreamInput(pushStream);
    //auto recognizer = SpeechRecognizer::FromConfig(config, audioInput);

    std::shared_ptr<DialogServiceConnector> dialogServiceConnector = DialogServiceConnector::FromConfig(config, audioInput);
    
    IAudioPlayer* player = new LinuxAudioPlayer();
    int bufferSize = 1024;
    unsigned char * buffer = (unsigned char *)malloc(bufferSize);
    
    dialogServiceConnector->SessionStarted += [&](const SessionEventArgs& e) {
        printf("SESSION STARTED: %s ...\n", e.SessionId.c_str());
    };

    dialogServiceConnector->SessionStopped += [&](const SessionEventArgs& e) {
        printf("SESSION STOPPED: %s ...\n", e.SessionId.c_str());
        printf("Press ENTER to acknowledge...\n");
    };

    dialogServiceConnector->Recognizing += [&](const SpeechRecognitionEventArgs& e) {
        printf("INTERMEDIATE: %s ...\n", e.Result->Text.c_str());
    };

    dialogServiceConnector->Recognized += [&](const SpeechRecognitionEventArgs& e) {
        printf("FINAL RESULT: '%s'\n", e.Result->Text.c_str());
    };

    dialogServiceConnector->Canceled += [&](const SpeechRecognitionCanceledEventArgs& e) {

        printf("CANCELED: Reason=%d\n", (int)e.Reason);
        if (e.Reason == CancellationReason::Error)
        {
            printf("CANCELED: ErrorDetails=%s\n", e.ErrorDetails.c_str());
            printf("CANCELED: Did you update the subscription info?\n");
        }
    };

    dialogServiceConnector->ActivityReceived += [&](const ActivityReceivedEventArgs& event) {
        auto activity = nlohmann::json::parse(event.GetActivity());

            log_t("ActivityReceived, type=", activity.value("type", ""), ", audio=", event.HasAudio() ? "true" : "false");

            if (activity.contains("text"))
            {
                log_t("activity[\"text\"]: ", activity["text"].get<string>());
            }

            auto continue_multiturn = activity.value<string>("inputHint", "") == "expectingInput";

            if (event.HasAudio())
            {
                log_t("Activity has audio, playing synchronously");

                //log_t("Resetting audio...");
                
                //auto closeResult = player.Close();
                //auto openResult = player.Open();
                //cout << "Attempted reset complete: "
                   // << closeResult
                    //<< " "
                    //<< openResult
                    //<< endl << flush;

                // TODO: AEC + Barge-in
                // For now: no KWS during playback
                //dialogServiceConnector->StopKeywordRecognitionAsync();

                auto audio = event.GetAudio();
                uint32_t bytes_read = 0;
                uint32_t total_bytes_read = 0;
                  
                do
                {   
                    
                     bytes_read = audio->Read(buffer, bufferSize);
                     auto play_result = player->Play(buffer, bytes_read);
                     total_bytes_read += bytes_read;
                    

                    //cout << "Read " << bytes_read << " bytes. Play result: " << play_result << endl;
                    //cout << "Read " << bytes_read << endl;
                    //rc = player->play(buffer, bytes_read);
                    
                    cout << " ." << flush;

                    //if (play_result)
                    //{
                    //    log_t("Play didn't return expected: ", play_result);
                    //    break;
                    //}
                } while (bytes_read > 0);

                cout << endl;
                log_t("Playback of ", total_bytes_read, " bytes complete.");

                if (!continue_multiturn)
                {
                    // DeviceStatusIndicators::SetStatus(DeviceStatus::Idle);
                }
            }

            if (continue_multiturn)
            {
                log_t("Activity requested a continuation (ExpectingInput) -- listening again");
                // DeviceStatusIndicators::SetStatus(DeviceStatus::Listening);
                dialogServiceConnector->ListenOnceAsync().wait();
            }
            else
            {
                // startKwsIfApplicable();
            }
        };
    
    // Starts continuous recognition. Use StopContinuousRecognitionAsync() to stop recognition.
    PushData(pushStream.get(), "/sample/kwsfromfile/kws_whatstheweatherlike.wav");
    dialogServiceConnector->ListenOnceAsync();
    //cout << "Calling KeywordRecognitionModel::FromFile()\n";
    //auto model = KeywordRecognitionModel::FromFile("/sample/kws/kws.table");
    //cout << "Now listening ...\nPress x and return to exit\n";
    //dialogServiceConnector->StartKeywordRecognitionAsync(model).wait();
    
    
    cin  >> s;
    while(s != "x")
    {
      cin >> s;
    }
    cout << "Closing down and freeing variables" << endl;
    
    player->Close();
    free(buffer);
    //free(buffer2);
    
    return 0;
}
