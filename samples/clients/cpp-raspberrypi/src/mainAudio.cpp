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

int main () 
{
    string s;
    int rc;
    const char * device = "default";
    
    std::shared_ptr<DialogServiceConfig> config = BotFrameworkConfig::FromSubscription("c29bbe9dad89459dbaa31e47caa11f60", "westus");
    //std::shared_ptr<SpeechConfig> config = SpeechConfig::FromSubscription("c29bbe9dad89459dbaa31e47caa11f60", "westus");

    //config->SetProperty(PropertyId::Speech_LogFilename, "/sample/log.txt");
        // Creates a speech recognizer using file as audio input. The default language is "en-us".
    //auto recognizer = SpeechRecognizer::FromConfig(config, audioInput);

    std::shared_ptr<DialogServiceConnector> dialogServiceConnector = DialogServiceConnector::FromConfig(config);
    
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
    
    //dialogServiceConnector->ListenOnceAsync();
    cout << "Calling KeywordRecognitionModel::FromFile()\n";
    //auto model = KeywordRecognitionModel::FromFile("/sample/kws/computer.table");
    auto model = KeywordRecognitionModel::FromFile("/sample/kws/Raspberry_pie.table");
    cout << "Now listening ...\nPress x and return to exit\n";
    dialogServiceConnector->StartKeywordRecognitionAsync(model);
    
    
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
