//
// Copyright (c) Microsoft. All rights reserved.
// See https://aka.ms/csspeech/license201809 for the full license information.
//
// speechapi_cxx_dialog_service_connector.h: Public API declarations for DialogServiceConnector C++ base class
//

#pragma once
#include <memory>
#include <future>
#include <array>

#include <speechapi_c_common.h>
#include <speechapi_c_dialog_service_connector.h>
#include <speechapi_cxx_common.h>
#include <speechapi_cxx_enums.h>
#include <speechapi_cxx_utils.h>
#include <speechapi_cxx_audio_config.h>
#include <speechapi_cxx_keyword_recognition_model.h>
#include <speechapi_cxx_speech_config.h>
#include <speechapi_cxx_eventsignal.h>
#include <speechapi_cxx_session_eventargs.h>
#include <speechapi_cxx_speech_recognition_result.h>
#include <speechapi_cxx_speech_recognition_eventargs.h>
#include <speechapi_cxx_dialog_service_connector_eventargs.h>
#include <speechapi_cxx_dialog_service_config.h>
#include <speechapi_cxx_translation_eventargs.h>
#include <speechapi_cxx_translation_result.h>

namespace Microsoft {
namespace CognitiveServices {
namespace Speech {
namespace Dialog {

/// <summary>
/// Connects to a speech enabled dialog backend.
/// </summary>
class DialogServiceConnector : public std::enable_shared_from_this<DialogServiceConnector>, public Utils::NonCopyable, public Utils::NonMovable
{
public:
    /// <summary>
    /// Destroys the instance.
    /// </summary>
    virtual ~DialogServiceConnector()
    {
        SPX_DBG_TRACE_SCOPE(__FUNCTION__, __FUNCTION__);

        if (m_handle != SPXHANDLE_INVALID)
        {
            ::dialog_service_connector_handle_release(m_handle);
            SPX_DBG_TRACE_VERBOSE("%s: m_handle=0x%8p", __FUNCTION__, (void*)m_handle);
            m_handle = SPXHANDLE_INVALID;
        }
    }

    /// <summary>
    /// Creates a dialog service connector from a dialog service config and an audio config.
    /// Users should use this function to create a dialog service connector.
    /// </summary>
    /// <param name="connector_config">Speech translation config.</param>
    /// <param name="audio_input">Audio config.</param>
    /// <returns>The shared smart pointer of the created dialog service connector.</returns>
    static std::shared_ptr<DialogServiceConnector> FromConfig(std::shared_ptr<DialogServiceConfig> connector_config, std::shared_ptr<Audio::AudioConfig> audio_input = nullptr)
    {
        SPXRECOHANDLE h_connector;
        SPX_THROW_ON_FAIL(::dialog_service_connector_create_dialog_service_connector_from_config(
            &h_connector,
            Utils::HandleOrInvalid<SPXSPEECHCONFIGHANDLE, DialogServiceConfig>(connector_config),
            Utils::HandleOrInvalid<SPXAUDIOCONFIGHANDLE, Audio::AudioConfig>(audio_input)
        ));
        return std::shared_ptr<DialogServiceConnector> { new DialogServiceConnector(h_connector) };
    }

    /// <summary>
    /// Connects with the back end.
    /// </summary>
    /// <returns>An asynchronous operation that starts the connection.</returns>
    std::future<void> ConnectAsync()
    {
        auto keep_alive = this->shared_from_this();
        return std::async(std::launch::async, [keep_alive, this]()
        {
            SPX_THROW_ON_FAIL(::dialog_service_connector_connect(m_handle));
        });
    }

    /// <summary>
    /// Disconnects from the back end.
    /// </summary>
    /// <returns>An asynchronous operation that starts the disconnection.</returns>
    std::future<void> DisconnectAsync()
    {
        auto keep_alive = this->shared_from_this();
        return std::async(std::launch::async, [keep_alive, this]()
        {
            SPX_THROW_ON_FAIL(::dialog_service_connector_disconnect(m_handle));
        });
    }

    /// <summary>
    /// Sends an activity to the backing dialog.
    /// </summary>
    /// <param name="activity">Activity to send</param>
    /// <returns>An asynchronous operation that starts the operation.</returns>
    std::future<std::string> SendActivityAsync(const std::string& activity)
    {
        auto keep_alive = this->shared_from_this();
        return std::async(std::launch::async, [keep_alive, activity, this]()
        {
            std::array<char, 50> buffer;
            SPX_THROW_ON_FAIL(::dialog_service_connector_send_activity(m_handle, activity.c_str(), buffer.data()));
            return std::string{ buffer.data() };
        });
    }

    /// <summary>
    /// Initiates keyword recognition.
    /// </summary>
    /// <param name="model">Specifies the keyword model to be used.</param>
    /// <returns>An asynchronous operation that starts the operation.</returns>
    std::future<void> StartKeywordRecognitionAsync(std::shared_ptr<KeywordRecognitionModel> model)
    {
        auto keep_alive = this->shared_from_this();
        auto h_model = Utils::HandleOrInvalid<SPXKEYWORDHANDLE, KeywordRecognitionModel>(model);
        return std::async(std::launch::async, [keep_alive, h_model, this]()
        {
            SPX_THROW_ON_FAIL(dialog_service_connector_start_keyword_recognition(m_handle, h_model));
        });
    }

    /// <summary>
    /// Stop keyword recognition.
    /// </summary>
    /// <returns>An asynchronous operation that starts the operation.</returns>
    std::future<void> StopKeywordRecognitionAsync()
    {
        auto keep_alive = this->shared_from_this();
        return std::async(std::launch::async, [keep_alive, this]()
        {
            SPX_THROW_ON_FAIL(dialog_service_connector_stop_keyword_recognition(m_handle));
        });
    }

    /// <summary>
    /// Starts a listening session that will terminate after the first utterance.
    /// </summary>
    /// <returns>An asynchronous operation that starts the operation.</returns>
    std::future<std::shared_ptr<SpeechRecognitionResult>> ListenOnceAsync()
    {
        auto keep_alive = this->shared_from_this();
        return std::async(std::launch::async, [keep_alive, this]()
        {
            SPX_INIT_HR(hr);

            SPXRECOHANDLE h_result = SPXHANDLE_INVALID;
            SPX_THROW_ON_FAIL(dialog_service_connector_listen_once(m_handle, &h_result));

            return std::make_shared<SpeechRecognitionResult>(h_result);
        });
    }

    /// <summary>
    /// Sets the authorization token that will be used for connecting to the service.
    /// Note: The caller needs to ensure that the authorization token is valid. Before the authorization token
    /// expires, the caller needs to refresh it by calling this setter with a new valid token.
    /// Otherwise, the connector will encounter errors during its operation.
    /// </summary>
    /// <param name="token">The authorization token.</param>
    void SetAuthorizationToken(const SPXSTRING& token)
    {
        Properties.SetProperty(PropertyId::SpeechServiceAuthorization_Token, token);
    }

    /// <summary>
    /// Gets the authorization token.
    /// </summary>
    /// <returns>Authorization token</returns>
    SPXSTRING GetAuthorizationToken()
    {
        return Properties.GetProperty(PropertyId::SpeechServiceAuthorization_Token, SPXSTRING());
    }


    /// <summary>
    /// Signal for events containing speech recognition results.
    /// </summary>
    EventSignal<const SpeechRecognitionEventArgs&> Recognized;

    /// <summary>
    /// Signal for events containing intermediate recognition results.
    /// </summary>
    EventSignal<const SpeechRecognitionEventArgs&> Recognizing;

    /// <summary>
    /// Signals that indicates the start of a listening session.
    /// </summary>
    EventSignal<const SessionEventArgs&> SessionStarted;

    /// <summary>
    /// Signals that indicates the end of a listening session.
    /// </summary>
    EventSignal<const SessionEventArgs&> SessionStopped;

    /// <summary>
    /// Signal for events relating to the cancellation of an interaction. The event indicates if the reason is a direct cancellation or an error.
    /// </summary>
    EventSignal<const SpeechRecognitionCanceledEventArgs&> Canceled;

    /// <summary>
    /// Signals that an activity was received from the backend
    /// </summary>
    EventSignal<const ActivityReceivedEventArgs&> ActivityReceived;
private:
    /*! \cond PROTECTED */
    template<typename T, typename F>
    std::function<void(const EventSignal<const T&>&)> Callback(F f)
    {
        return [=](const EventSignal<const T&>& evt)
        {
            (this->*f)(evt);
        };
    }

    static void FireEvent_Recognized(SPXRECOHANDLE, SPXEVENTHANDLE h_event, void* pv_context)
    {
        auto keep_alive = static_cast<DialogServiceConnector*>(pv_context)->shared_from_this();
        SpeechRecognitionEventArgs event{ h_event };
        keep_alive->Recognized.Signal(event);
        /* Not releasing the handle as SpeechRecognitionEventArgs manages it */
    }

    static void FireEvent_Recognizing(SPXRECOHANDLE, SPXEVENTHANDLE h_event, void* pv_context)
    {
        auto keep_alive = static_cast<DialogServiceConnector*>(pv_context)->shared_from_this();
        SpeechRecognitionEventArgs event{ h_event };
        keep_alive->Recognizing.Signal(event);
        /* Not releasing the handle as SpeechRecognitionEventArgs manages it */
    }

    void RecognizerEventConnectionChanged(const EventSignal<const SpeechRecognitionEventArgs&>& reco_event)
    {
        if (m_handle != SPXHANDLE_INVALID)
        {
            SPX_DBG_TRACE_VERBOSE("%s: m_handle=0x%8p", __FUNCTION__, (void*)m_handle);
            SPX_DBG_TRACE_VERBOSE_IF(!::dialog_service_connector_handle_is_valid(m_handle), "%s: m_handle is INVALID!!!", __FUNCTION__);

            if (&reco_event == &Recognizing)
            {
                ::dialog_service_connector_recognizing_set_callback(m_handle, Recognizing.IsConnected() ? DialogServiceConnector::FireEvent_Recognizing : nullptr, this);
            }
            else if (&reco_event == &Recognized)
            {
                ::dialog_service_connector_recognized_set_callback(m_handle, Recognized.IsConnected() ? DialogServiceConnector::FireEvent_Recognized : nullptr, this);
            }
        }
    }

    static void FireEvent_SessionStarted(SPXRECOHANDLE, SPXEVENTHANDLE h_event, void* pv_context)
    {
        auto keep_alive = static_cast<DialogServiceConnector*>(pv_context)->shared_from_this();
        SessionEventArgs event{ h_event };
        keep_alive->SessionStarted.Signal(event);

        SPX_DBG_ASSERT(::recognizer_event_handle_is_valid(h_event));
        /* Releasing the event handle as SessionEventArgs doesn't keep the handle */
        ::recognizer_event_handle_release(h_event);
    }

    static void FireEvent_SessionStopped(SPXRECOHANDLE, SPXEVENTHANDLE h_event, void* pv_context)
    {
        auto keep_alive = static_cast<DialogServiceConnector*>(pv_context)->shared_from_this();
        SessionEventArgs event{ h_event };
        keep_alive->SessionStopped.Signal(event);

        SPX_DBG_ASSERT(::recognizer_event_handle_is_valid(h_event));
        /* Releasing the event handle as SessionEventArgs doesn't keep the handle */
        ::recognizer_event_handle_release(h_event);
    }

    void SessionEventConnectionChanged(const EventSignal<const SessionEventArgs&>& session_event)
    {
        if (m_handle != SPXHANDLE_INVALID)
        {
            SPX_DBG_TRACE_VERBOSE("%s: m_handle=0x%8p", __FUNCTION__, (void*)m_handle);
            SPX_DBG_TRACE_VERBOSE_IF(!::dialog_service_connector_handle_is_valid(m_handle), "%s: m_handle is INVALID!!!", __FUNCTION__);

            if (&session_event == &SessionStarted)
            {
                ::dialog_service_connector_session_started_set_callback(m_handle, SessionStarted.IsConnected() ? DialogServiceConnector::FireEvent_SessionStarted : nullptr, this);
            }
            else if (&session_event == &SessionStopped)
            {
                ::dialog_service_connector_session_stopped_set_callback(m_handle, SessionStopped.IsConnected() ? DialogServiceConnector::FireEvent_SessionStopped : nullptr, this);
            }
        }
    }

    static void FireEvent_Canceled(SPXRECOHANDLE, SPXEVENTHANDLE h_event, void* pv_context)
    {
        auto keep_alive = static_cast<DialogServiceConnector*>(pv_context)->shared_from_this();
        SpeechRecognitionCanceledEventArgs event{ h_event };
        keep_alive->Canceled.Signal(event);
        /* Not releasing the handle as SpeechRecognitionCanceledEventArgs manages it */
    }

    void CanceledEventConnectionChanged(const EventSignal<const SpeechRecognitionCanceledEventArgs&>& canceled_event)
    {
        if (m_handle != SPXHANDLE_INVALID)
        {
            SPX_DBG_TRACE_VERBOSE("%s: m_handle=0x%8p", __FUNCTION__, (void*)m_handle);
            SPX_DBG_TRACE_VERBOSE_IF(!::dialog_service_connector_handle_is_valid(m_handle), "%s: m_handle is INVALID!!!", __FUNCTION__);

            if (&canceled_event == &Canceled)
            {
                ::dialog_service_connector_canceled_set_callback(m_handle, Canceled.IsConnected() ? DialogServiceConnector::FireEvent_Canceled : nullptr, this);
            }
        }
    }

    static void FireEvent_ActivityReceived(SPXRECOHANDLE, SPXEVENTHANDLE h_event, void* pv_context)
    {
        auto keep_alive = static_cast<DialogServiceConnector*>(pv_context)->shared_from_this();
        ActivityReceivedEventArgs event{ h_event };
        keep_alive->ActivityReceived.Signal(event);
        /* Not releasing the handle as ActivityReceivedEventArgs manages it */
    }

    void ActivityReceivedConnectionChanged(const EventSignal<const ActivityReceivedEventArgs&>& activity_event)
    {
        if (m_handle != SPXHANDLE_INVALID)
        {
            SPX_DBG_TRACE_VERBOSE("%s: m_hreco=0x%8p", __FUNCTION__, (void*)m_handle);
            SPX_DBG_TRACE_VERBOSE_IF(!::dialog_service_connector_handle_is_valid(m_handle), "%s: m_handle is INVALID!!!", __FUNCTION__);

            if (&activity_event == &ActivityReceived)
            {
                ::dialog_service_connector_activity_received_set_callback(m_handle, ActivityReceived.IsConnected() ? DialogServiceConnector::FireEvent_ActivityReceived : nullptr, this);
            }
        }
    }

    class PrivatePropertyCollection : public PropertyCollection
    {
    public:
        PrivatePropertyCollection(SPXRECOHANDLE h_connector) :
            PropertyCollection(
                [=](){
                SPXPROPERTYBAGHANDLE h_prop_bag = SPXHANDLE_INVALID;
                dialog_service_connector_get_property_bag(h_connector, &h_prop_bag);
                return h_prop_bag;
            }())
        {
        }
    };

    inline explicit DialogServiceConnector(SPXRECOHANDLE handle) :
        Recognized{ Callback<SpeechRecognitionEventArgs>(&DialogServiceConnector::RecognizerEventConnectionChanged),
                    Callback<SpeechRecognitionEventArgs>(&DialogServiceConnector::RecognizerEventConnectionChanged), false },
        Recognizing{ Callback<SpeechRecognitionEventArgs>(&DialogServiceConnector::RecognizerEventConnectionChanged),
                     Callback<SpeechRecognitionEventArgs>(&DialogServiceConnector::RecognizerEventConnectionChanged), false },
        SessionStarted{ Callback<SessionEventArgs>(&DialogServiceConnector::SessionEventConnectionChanged),
                        Callback<SessionEventArgs>(&DialogServiceConnector::SessionEventConnectionChanged), false },
        SessionStopped{ Callback<SessionEventArgs>(&DialogServiceConnector::SessionEventConnectionChanged),
                        Callback<SessionEventArgs>(&DialogServiceConnector::SessionEventConnectionChanged), false },
        Canceled{ Callback<SpeechRecognitionCanceledEventArgs>(&DialogServiceConnector::CanceledEventConnectionChanged),
                  Callback<SpeechRecognitionCanceledEventArgs>(&DialogServiceConnector::CanceledEventConnectionChanged), false },
        ActivityReceived{ Callback<ActivityReceivedEventArgs>(&DialogServiceConnector::ActivityReceivedConnectionChanged),
                          Callback<ActivityReceivedEventArgs>(&DialogServiceConnector::ActivityReceivedConnectionChanged), false },
        m_handle{ handle },
        m_properties{ handle },
        Properties{ m_properties }
    {
    }

    SPXRECOHANDLE m_handle;
    PrivatePropertyCollection m_properties;
    /*! \endcond */
public:
    /// <summary>
    /// A collection of properties and their values defined for this <see cref="DialogServiceConnector"/>.
    /// </summary>
    PropertyCollection& Properties;
};

} } } }
