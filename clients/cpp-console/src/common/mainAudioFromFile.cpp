// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include <iostream>
#include <string> 
#include <fstream>
#include <thread>
#include <chrono>
#include "AgentConfiguration.h"
#include "DeviceStatusIndicators.h"
#include "speechapi_cxx.h"

//the pragma here suppresses warnings from the 3rd party header
#pragma warning(push, 0)
#include "json.hpp"
#pragma warning(pop)

#ifdef LINUX
#include "LinuxAudioPlayer.h"
#endif

#ifdef WINDOWS
#include "WindowsAudioPlayer.h"
#endif

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

// TODO Temporary method while TLS issues are diagnosed
void add_config_tls_workaround(shared_ptr<DialogServiceConfig> dialog_config)
{
    log_t("Adding TLS workaround");
    constexpr char baltimoreCyberTrustRoot[] =
        "-----BEGIN CERTIFICATE-----\n"
        "MIIDdzCCAl+gAwIBAgIEAgAAuTANBgkqhkiG9w0BAQUFADBaMQswCQYDVQQGEwJJ\n"
        "RTESMBAGA1UEChMJQmFsdGltb3JlMRMwEQYDVQQLEwpDeWJlclRydXN0MSIwIAYD\n"
        "VQQDExlCYWx0aW1vcmUgQ3liZXJUcnVzdCBSb290MB4XDTAwMDUxMjE4NDYwMFoX\n"
        "DTI1MDUxMjIzNTkwMFowWjELMAkGA1UEBhMCSUUxEjAQBgNVBAoTCUJhbHRpbW9y\n"
        "ZTETMBEGA1UECxMKQ3liZXJUcnVzdDEiMCAGA1UEAxMZQmFsdGltb3JlIEN5YmVy\n"
        "VHJ1c3QgUm9vdDCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBAKMEuyKr\n"
        "mD1X6CZymrV51Cni4eiVgLGw41uOKymaZN+hXe2wCQVt2yguzmKiYv60iNoS6zjr\n"
        "IZ3AQSsBUnuId9Mcj8e6uYi1agnnc+gRQKfRzMpijS3ljwumUNKoUMMo6vWrJYeK\n"
        "mpYcqWe4PwzV9/lSEy/CG9VwcPCPwBLKBsua4dnKM3p31vjsufFoREJIE9LAwqSu\n"
        "XmD+tqYF/LTdB1kC1FkYmGP1pWPgkAx9XbIGevOF6uvUA65ehD5f/xXtabz5OTZy\n"
        "dc93Uk3zyZAsuT3lySNTPx8kmCFcB5kpvcY67Oduhjprl3RjM71oGDHweI12v/ye\n"
        "jl0qhqdNkNwnGjkCAwEAAaNFMEMwHQYDVR0OBBYEFOWdWTCCR1jMrPoIVDaGezq1\n"
        "BE3wMBIGA1UdEwEB/wQIMAYBAf8CAQMwDgYDVR0PAQH/BAQDAgEGMA0GCSqGSIb3\n"
        "DQEBBQUAA4IBAQCFDF2O5G9RaEIFoN27TyclhAO992T9Ldcw46QQF+vaKSm2eT92\n"
        "9hkTI7gQCvlYpNRhcL0EYWoSihfVCr3FvDB81ukMJY2GQE/szKN+OMY3EU/t3Wgx\n"
        "jkzSswF07r51XgdIGn9w/xZchMB5hbgF/X++ZRGjD8ACtPhSNzkE1akxehi/oCr0\n"
        "Epn3o0WC4zxe9Z2etciefC7IpJ5OCBRLbf1wbWsaY71k5h+3zvDyny67G7fyUIhz\n"
        "ksLi4xaNmjICq44Y3ekQEe5+NauQrz4wlHrQMz2nZQ/1/I6eYs9HRCwBXbsdtTLS\n"
        "R9I4LtD+gdwyah617jzV/OeBHRnDJELqYzmp\n"
        "-----END CERTIFICATE-----\n";

    dialog_config->SetProperty("OPENSSL_SINGLE_TRUSTED_CERT", baltimoreCyberTrustRoot);
    dialog_config->SetProperty("OPENSSL_SINGLE_TRUSTED_CERT_CRL_CHECK", "false");
    log_t("TLS workaround completed");
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

int main (int argc, char** argv) 
{
    
    if(argc < 2){
        log("Usage:\n", argv[0] ," [config file path] [wav file path]\n");
        return 0;
    }
    
    bool volumeOn = false;
    
    if(argc >= 4)
    {
        if(strcmp(argv[3], "on") == 0){
            volumeOn = true;
        }
    }
    
    string configFilePath = argv[1];
    string audioFilePath = argv[2];
    string s;
    int rc;
    const char * device = "default";
    bool keywordListeningEnabled = false;
    shared_ptr<AgentConfiguration> agentConfig;
    shared_ptr<DialogServiceConnector> dialogServiceConnector;
    std::shared_ptr<PushAudioInputStream> pushStream;
    
    IAudioPlayer* player;
    if(volumeOn){
#ifdef LINUX
        player = new LinuxAudioPlayer();
#endif
#ifdef WINDOWS
        player = new WindowsAudioPlayer();
#endif
    }
    
    auto startKwsIfApplicable = [&]()
    {
        log_t("startKWS called");
        if (keywordListeningEnabled)
        {
            auto modelPath = agentConfig->KeywordModel();
            log_t("Initializing keyword recognition with: ", modelPath);
            auto model = KeywordRecognitionModel::FromFile(modelPath);
            dialogServiceConnector->StartKeywordRecognitionAsync(model);
            log_t("KWS initialized");
        }
        else {
            log_t("no model file specified. Cannot start keyword listening");
        }
    };
    
    auto initFromPath = [&](string path)
    {
        DeviceStatusIndicators::SetStatus(DeviceStatus::Initializing);
        log_t("Loading configuration from file: ", path);
        
        agentConfig = AgentConfiguration::LoadFromFile(path);
        
        if (agentConfig->LoadResult() != AgentConfigurationLoadResult::Success)
        {
            log_t("Unable to load config file.");
            return (int)agentConfig->LoadResult();
        }
        
        log_t("Configuration loaded. Creating connector...");
        
        shared_ptr<DialogServiceConfig> config = agentConfig->CreateDialogServiceConfig();
        pushStream = AudioInputStream::CreatePushStream();
        auto audioInput = AudioConfig::FromStreamInput(pushStream);
        
        dialogServiceConnector = DialogServiceConnector::FromConfig(config, audioInput);
        log_t("Connector created");
        dialogServiceConnector->ConnectAsync();
        log_t("Creating prime activity");
        nlohmann::json keywordPrimingActivity =
        {
            { "type", "event" },
            { "name", "KeywordPrefix" },
            { "value", agentConfig->KeywordDisplayName() }
        };
        auto keywordPrimingActivityText = keywordPrimingActivity.dump();
        log_t("Sending inform-of-keyword activity: ", keywordPrimingActivityText);
        dialogServiceConnector->SendActivityAsync(keywordPrimingActivityText);

        log_t("Connector successfully initialized!");
        
        if(agentConfig->KeywordModel().length() > 0){
            keywordListeningEnabled = true;
        }
        
        // Signals that indicates the start of a listening session.
        dialogServiceConnector->SessionStarted += [&](const SessionEventArgs& event) {
            printf("SESSION STARTED: %s ...\n", event.SessionId.c_str());
        };

        // Signals that indicates the end of a listening session.
        dialogServiceConnector->SessionStopped += [&](const SessionEventArgs& event) {
            printf("SESSION STOPPED: %s ...\n", event.SessionId.c_str());
            printf("Press ENTER to acknowledge...\n");
        };

        // Signal for events containing intermediate recognition results.
        dialogServiceConnector->Recognizing += [&](const SpeechRecognitionEventArgs& event) {
            printf("INTERMEDIATE: %s ...\n", event.Result->Text.c_str());
            DeviceStatusIndicators::SetStatus(DeviceStatus::Detecting);
        };

        // Signal for events containing speech recognition results.
        dialogServiceConnector->Recognized += [&](const SpeechRecognitionEventArgs& event) {
            printf("FINAL RESULT: '%s'\n", event.Result->Text.c_str());
            auto&& reason = event.Result->Reason;
            auto newStatus = reason == ResultReason::RecognizedKeyword
                    ? DeviceStatus::Listening
                    : reason == ResultReason::RecognizedSpeech
                    ? DeviceStatus::Thinking
                    : DeviceStatus::Idle;
                DeviceStatusIndicators::SetStatus(newStatus);
        };

        // Signal for events relating to the cancellation of an interaction. The event indicates if the reason is a direct cancellation or an error.
        dialogServiceConnector->Canceled += [&](const SpeechRecognitionCanceledEventArgs& event) {

            printf("CANCELED: Reason=%d\n", (int)event.Reason);
            DeviceStatusIndicators::SetStatus(DeviceStatus::Idle);
            if (event.Reason == CancellationReason::Error)
            {
                printf("CANCELED: ErrorDetails=%s\n", event.ErrorDetails.c_str());
                printf("CANCELED: Did you update the subscription info?\n");
                startKwsIfApplicable();
            }
        };

        // Signals that an activity was received from the service
        dialogServiceConnector->ActivityReceived += [&](const ActivityReceivedEventArgs& event) {
            auto activity = nlohmann::json::parse(event.GetActivity());

                log_t("ActivityReceived, type=", activity.value("type", ""), ", audio=", event.HasAudio() ? "true" : "false");

                if (activity.contains("text"))
                {
                    log_t("activity[\"text\"]: ", activity["text"].get<string>());
                }

                auto continue_multiturn = activity.value<string>("inputHint", "") == "expectingInput";

                uint32_t total_bytes_read = 0;
                if (event.HasAudio())
                {
                    log_t("Activity has audio, playing synchronously");

                    // TODO: AEC + Barge-in
                    // For now: no KWS during playback
                    log_t("stopping KWS for playback");
                    dialogServiceConnector->StopKeywordRecognitionAsync();
                    log_t("KWS stopped");

                    auto audio = event.GetAudio();
                    uint32_t bytes_read = 0;
                    int play_result = 0;
                    
                    if(volumeOn && player != nullptr){
                        play_result = player->Play(audio);
                    }

                    cout << endl;
                    log_t("Playback of ", total_bytes_read, " bytes complete.");

                    if (!continue_multiturn)
                    {
                        DeviceStatusIndicators::SetStatus(DeviceStatus::Idle);
                    }
                }

                if (continue_multiturn)
                {
                    log_t("Activity requested a continuation (ExpectingInput) -- listening again");
                    DeviceStatusIndicators::SetStatus(DeviceStatus::Listening);
                    dialogServiceConnector->ListenOnceAsync();
                }
                else
                {
                    //TODO remove once we have echo cancellation
                    int secondsOfAudio = total_bytes_read / 32000;
                    std::this_thread::sleep_for(std::chrono::milliseconds(secondsOfAudio*1000));
                    startKwsIfApplicable();
                }
            };
        
        DeviceStatusIndicators::SetStatus(DeviceStatus::Ready);
        return 0;
    };
    
    initFromPath(configFilePath);
    
    PushData(pushStream.get(), audioFilePath);
    dialogServiceConnector->ListenOnceAsync();
    
    
     cout << "Commands:" << endl;
    cout << "1 [listen once]" << endl;
    cout << "2 [start keyword listening]" << endl;
    cout << "3 [stop keyword listening]" << endl;
    cout << "x [exit]" << endl;
    
    s = "";
    while(s != "x")
    {
        cin >> s;
        if(s == "1"){
            log_t("Now listening...");
            dialogServiceConnector->ListenOnceAsync();
        }
        if(s == "2"){
            startKwsIfApplicable();
        }
        if(s == "3"){
            if(keywordListeningEnabled){
                log_t("Stopping keyword recognition");
                dialogServiceConnector->StopKeywordRecognitionAsync();
            }
            else{
                cout << "No model path specified. Cannot stop keyword listening.\n";
            }
        }
        cout << "Commands:" << endl;
        cout << "1 [listen once]" << endl;
        cout << "2 [start keyword listening]" << endl;
        cout << "3 [stop keyword listening]" << endl;
        cout << "x [exit]" << endl;
    }
    cout << "Closing down and freeing variables" << endl;
    
    if(volumeOn){
        player->Close();
    }
    free(buffer);
    
    return 0;
}
