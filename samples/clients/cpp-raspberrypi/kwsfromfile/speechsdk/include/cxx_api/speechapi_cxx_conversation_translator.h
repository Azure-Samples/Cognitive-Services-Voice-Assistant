
// Copyright (c) Microsoft. All rights reserved.
// See https://aka.ms/csspeech/license201809 for the full license information.
//
// speechapi_cxx_conversation_translator.h: Public API declarations for ConversationTranslator C++ class
//

#pragma once

#include <exception>
#include <future>
#include <memory>
#include <string>
#include <cstring>

#include <speechapi_c_conversation_translator.h>
#include <speechapi_cxx_eventsignal.h>
#include <speechapi_cxx_audio_config.h>
#include <speechapi_cxx_conversation_translator_events.h>
#include <speechapi_cxx_conversation_transcription_eventargs.h>

namespace Microsoft {
namespace CognitiveServices {
namespace Speech {
namespace Transcription {

    /// <summary>
    /// A conversation translator that enables a connected experience where participants can use their
    /// own devices to see everyone else's recognitions and IMs in their own languages. Participants
    /// can also speak and send IMs to others.
    /// Added in 1.9.0
    /// </summary>
    class ConversationTranslator : public std::enable_shared_from_this<ConversationTranslator>
    {
    public:
        /// <summary>
        /// Creates a conversation translator from an audio config
        /// </summary>
        /// <param name="audioConfig">Audio configuration.</param>
        /// <returns>Smart pointer to conversation translator instance.</returns>
        static std::shared_ptr<ConversationTranslator> FromConfig(std::shared_ptr<Audio::AudioConfig> audioConfig = nullptr)
        {
            SPXCONVERSATIONTRANSLATORHANDLE handle;
            SPX_THROW_ON_FAIL(::conversation_translator_create_from_config(
                &handle,
                Utils::HandleOrInvalid<SPXAUDIOCONFIGHANDLE, Audio::AudioConfig>(audioConfig)
            ));
            return std::shared_ptr<ConversationTranslator>(new ConversationTranslator(handle));
        }

        /// <summary>
        /// Destructor
        /// </summary>
        virtual ~ConversationTranslator()
        {
            SPX_DBG_TRACE_SCOPE(__FUNCTION__, __FUNCTION__);

            // disconnect callbacks in reverse order
            TextMessageReceived.DisconnectAll();
            Transcribed.DisconnectAll();
            Transcribing.DisconnectAll();
            ConversationExpiration.DisconnectAll();
            ParticipantsChanged.DisconnectAll();
            Canceled.DisconnectAll();
            SessionStopped.DisconnectAll();
            SessionStarted.DisconnectAll();

            ::conversation_translator_handle_release(m_handle);
            m_handle = SPXHANDLE_INVALID;

            m_participants.clear();
        }

        /// <summary>
        /// Signal for events indicating the start of a transcription session (operation).
        /// </summary>
        EventSignal<const SessionEventArgs&> SessionStarted;

        /// <summary>
        /// Signal for events indicating the end of a transcription session (operation).
        /// </summary>
        EventSignal<const SessionEventArgs&> SessionStopped;

        /// <summary>
        /// Signal for events containing canceled recognition results
        /// (indicating a recognition attempt that was canceled as a result or a direct cancellation request
        /// or, alternatively, a transport or protocol failure).
        /// </summary>
        EventSignal<const ConversationTranslationCanceledEventArgs&> Canceled;

        /// <summary>
        /// Signal for events indicating the conversation participants have changed.
        /// </summary>
        EventSignal<const ConversationParticipantsChangedEventArgs&> ParticipantsChanged;

        /// <summary>
        /// Signal for event indicating how many minutes are left until a conversation expires.
        /// </summary>
        EventSignal<const ConversationExpirationEventArgs&> ConversationExpiration;

        /// <summary>
        /// Signal for events containing intermediate translated conversation transcription results.
        /// </summary>
        EventSignal<const ConversationTranslationEventArgs&> Transcribing;

        /// <summary>
        /// Signal for events containing final translated conversation transcription results.
        /// (indicating a successful recognition attempt).
        /// </summary>
        EventSignal<const ConversationTranslationEventArgs&> Transcribed;

        /// <summary>
        /// Raised when a text message is received from the conversation.
        /// </summary>
        EventSignal<const ConversationTranslationEventArgs&> TextMessageReceived;

        /// <summary>
        /// The current participants in the conversation
        /// </summary>
        const std::vector<std::shared_ptr<Participant>>& Participants;

        /// <summary>
        /// Joins a conversation. After you call this, you will start receiving events.
        /// </summary>
        /// <param name="conversation">The conversation instance to use. This instance can be used by the
        /// host to manage the conversation.</param>
        /// <param name="nickname">The display name to use for the current participant in the conversation.</param>
        /// <returns>An asynchronous operation.</returns>
        std::future<void> JoinConversationAsync(std::shared_ptr<Conversation> conversation, const SPXSTRING& nickname)
        {
            return RunAsync([conversation, nickname](auto handle)
            {
                return ::conversation_translator_join(
                    handle,
                    Utils::HandleOrInvalid<SPXCONVERSATIONHANDLE>(conversation),
                    Utils::ToUTF8(nickname).c_str());
            });
        }

        /// <summary>
        /// Joins a conversation. After you call this, you will start receiving events.
        /// </summary>
        /// <param name="conversationId">The identifier of the conversation you want to join.</param>
        /// <param name="nickname">The display name of the current participant in the conversation.</param>
        /// <param name="language">The language the participant is using.</param>
        /// <returns>An asynchronous operation.</returns>
        std::future<void> JoinConversationAsync(const SPXSTRING& conversationId, const SPXSTRING& nickname, const SPXSTRING& language)
        {
            return RunAsync([conversationId, nickname, language](auto handle)
            {
                return ::conversation_translator_join_with_id(
                    handle,
                    Utils::ToUTF8(conversationId).c_str(),
                    Utils::ToUTF8(nickname).c_str(),
                    Utils::ToUTF8(language).c_str());
            });
        }

        /// <summary>
        /// Starts sending audio to the conversation service for speech recognition.
        /// </summary>
        /// <returns>An asynchronous operation.</returns>
        std::future<void> StartTranscribingAsync()
        {
            return RunAsync(::conversation_translator_start_transcribing);
        }

        /// <summary>
        /// Stops sending audio to the conversation service.
        /// </summary>
        /// <returns>An asynchronous operation.</returns>
        std::future<void> StopTranscribingAsync()
        {
            return RunAsync(::conversation_translator_stop_transcribing);
        }

        /// <summary>
        /// Sends an instant message to all participants in the conversation. This instant message
        /// will be translated into each participant's text language.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <returns>An asynchronous operation.</returns>
        std::future<void> SendTextMessageAsync(const SPXSTRING& message)
        {
            return RunAsync([message](auto handle)
            {
                return ::conversation_translator_send_text_message(
                    handle,
                    Utils::ToUTF8(message).c_str());
            });
        }

        /// <summary>
        /// Leaves the current conversation. After this is called, you will no longer receive any events.
        /// </summary>
        /// <returns>An asynchronous operation.</returns>
        std::future<void> LeaveConversationAsync()
        {
            return RunAsync(::conversation_translator_leave);
        }

    protected:
        explicit ConversationTranslator(SPXCONVERSATIONTRANSLATORHANDLE handle) :
            SessionStarted(BindHandler(&ConversationTranslator::OnSessionEventChanged)),
            SessionStopped(BindHandler(&ConversationTranslator::OnSessionEventChanged)),
            Canceled(BindHandler(&ConversationTranslator::OnCanceledEventChanged)),
            ParticipantsChanged(BindHandler(&ConversationTranslator::OnParticipantsEventChanged)),
            ConversationExpiration(BindHandler(&ConversationTranslator::OnExpirationEventChanged)),
            Transcribing(BindHandler(&ConversationTranslator::OnTranscriptionEventChanged)),
            Transcribed(BindHandler(&ConversationTranslator::OnTranscriptionEventChanged)),
            TextMessageReceived(BindHandler(&ConversationTranslator::OnTextMessageEventChanged)),
            Participants(m_participants),
            m_handle(handle),
            m_properties(handle),
            m_participants()
        {
            SPX_DBG_TRACE_SCOPE(__FUNCTION__, __FUNCTION__);
        }

        static inline bool ValidateHandle(SPXCONVERSATIONTRANSLATORHANDLE handle, const char* function)
        {
            UNUSED(function); // not used in release builds
            SPX_DBG_TRACE_VERBOSE("%s: handle=0x%8p", function, (void*)handle);
            bool valid = ::conversation_translator_handle_is_valid(handle);
            SPX_DBG_TRACE_VERBOSE_IF(!valid, "%s: handle is INVALID!!!", function);
            return valid;
        }

        void OnSessionEventChanged(const EventSignal<const SessionEventArgs&>& evt)
        {
            if (!ValidateHandle(m_handle, __FUNCTION__)) return;

            PCONV_TRANS_CALLBACK callback = nullptr;

            if (&evt == &SessionStarted)
            {
                if (SessionStarted.IsConnected())
                {
                    callback = [](auto, auto b, auto c)  { FireEvent(b, c, &ConversationTranslator::SessionStarted); };
                }

                conversation_translator_session_started_set_callback(m_handle, callback, this);
            }
            else if (&evt == &SessionStopped)
            {
                if (SessionStarted.IsConnected())
                {
                    callback = [](auto, auto b, auto c) { FireEvent(b, c, &ConversationTranslator::SessionStopped); };
                }

                conversation_translator_session_stopped_set_callback(m_handle, SessionStopped.IsConnected() ? callback : nullptr, this);
            }
        }

        void OnCanceledEventChanged(const EventSignal<const ConversationTranslationCanceledEventArgs&>&)
        {
            if (!ValidateHandle(m_handle, __FUNCTION__)) return;

            PCONV_TRANS_CALLBACK callback = nullptr;
            if (Canceled.IsConnected())
            {
                callback = [](auto, auto b, auto c) { FireEvent(b, c, &ConversationTranslator::Canceled); };
            }

            conversation_translator_canceled_set_callback(m_handle, callback, this);
        }

        void OnParticipantsEventChanged(const EventSignal<const ConversationParticipantsChangedEventArgs&>&)
        {
            if (!ValidateHandle(m_handle, __FUNCTION__)) return;

            PCONV_TRANS_CALLBACK callback = nullptr;
            if (Canceled.IsConnected())
            {
                callback = [](auto, auto b, auto c) { FireEvent(b, c, &ConversationTranslator::ParticipantsChanged); };
            }

            conversation_translator_participants_changed_set_callback(m_handle, callback, this);
        }

        void OnExpirationEventChanged(const EventSignal<const ConversationExpirationEventArgs&>&)
        {
            if (!ValidateHandle(m_handle, __FUNCTION__)) return;

            PCONV_TRANS_CALLBACK callback = nullptr;
            if (Canceled.IsConnected())
            {
                callback = [](auto, auto b, auto c) { FireEvent(b, c, &ConversationTranslator::ConversationExpiration); };
            }

            conversation_translator_conversation_expiration_set_callback(m_handle, callback, this);
        }

        void OnTranscriptionEventChanged(const EventSignal<const ConversationTranslationEventArgs&>& evt)
        {
            if (!ValidateHandle(m_handle, __FUNCTION__)) return;

            PCONV_TRANS_CALLBACK callback = nullptr;
            if (&evt == &Transcribing)
            {
                if (Transcribing.IsConnected())
                {
                    callback = [](auto, auto b, auto c) { FireEvent(b, c, &ConversationTranslator::Transcribing); };
                }

                conversation_translator_transcribing_set_callback(m_handle, callback, this);
            }
            else
            {
                if (Transcribed.IsConnected())
                {
                    callback = [](auto, auto b, auto c) { FireEvent(b, c, &ConversationTranslator::Transcribed); };
                }

                conversation_translator_transcribed_set_callback(m_handle, callback, this);
            }
        }

        void OnTextMessageEventChanged(const EventSignal<const ConversationTranslationEventArgs&>&)
        {
            if (!ValidateHandle(m_handle, __FUNCTION__)) return;

            PCONV_TRANS_CALLBACK callback = nullptr;
            if (Transcribing.IsConnected())
            {
                callback = [](auto, auto b, auto c) { FireEvent(b, c, &ConversationTranslator::TextMessageReceived); };
            }

            conversation_translator_text_message_recevied_set_callback(m_handle, callback, this);
        }

    private:
        /*! \cond PRIVATE */

        DISABLE_DEFAULT_CTORS(ConversationTranslator);

        class PrivatePropertyCollection : public PropertyCollection
        {
        public:
            PrivatePropertyCollection(SPXRECOHANDLE hreco) :
                PropertyCollection(
                    [=]() {
                SPXPROPERTYBAGHANDLE hpropbag = SPXHANDLE_INVALID;
                recognizer_get_property_bag(hreco, &hpropbag);
                return hpropbag;
            }())
            {
            }
        };

        inline std::future<void> RunAsync(std::function<SPXHR(SPXCONVERSATIONHANDLE)> func)
        {
            auto keepalive = this->shared_from_this();
            return std::async(std::launch::async, [keepalive, this, func]()
            {
                SPX_THROW_ON_FAIL(func(m_handle));
            });
        }

        template<typename TArg>
        inline std::function<void(TArg)> BindHandler(void (ConversationTranslator::*func)(TArg))
        {
            return [this, func](TArg arg)
            {
                (this->*func)(arg);
            };
        }

        static inline void FreeEventHandle(SPXEVENTHANDLE hEvt)
        {
            if (::conversation_translator_event_handle_is_valid(hEvt))
            {
                ::conversation_translator_event_handle_release(hEvt);
            }
        }

        template<typename T>
        static inline void FireEvent(SPXEVENTHANDLE hEvt, void* pCtxt, EventSignal<const T&> ConversationTranslator::*pEvent)
        {
            try
            {
                auto pThis = static_cast<ConversationTranslator*>(pCtxt);
                SPX_DBG_ASSERT(pThis != nullptr);
                auto keepAlive = pThis->shared_from_this();

                T eventArgs(hEvt);
                (pThis->*pEvent).Signal(eventArgs);

                // event classes don't properly release the handles so do that here
                FreeEventHandle(hEvt);
            }
            catch (std::exception& ex)
            {
                UNUSED(ex);
                FreeEventHandle(hEvt);
                throw;
            }
            catch (...)
            {
                FreeEventHandle(hEvt);
                throw;
            }
        }

        /*! \endcond */

    private:
        SPXCONVERSATIONTRANSLATORHANDLE m_handle;
        PrivatePropertyCollection m_properties;
        std::vector<std::shared_ptr<Participant>> m_participants;
    };

}}}}
