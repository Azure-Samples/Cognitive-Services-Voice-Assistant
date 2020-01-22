//
// Copyright (c) Microsoft. All rights reserved.
// See https://aka.ms/csspeech/license201809 for the full license information.
//
// speechapi_cxx_conversation_transcription_eventargs.h: Public API declarations for ConversationTranscriptionEventArgs C++ class
//

#pragma once
#include <string>
#include <speechapi_cxx_common.h>
#include <speechapi_cxx_string_helpers.h>
#include <speechapi_cxx_recognition_eventargs.h>
#include <speechapi_cxx_conversation_transcription_result.h>
#include <spxdebug.h>

namespace Microsoft {
namespace CognitiveServices {
namespace Speech {
namespace Transcription {

/// <summary>
/// Class for conversation transcriber event arguments.
/// Added in version 1.5.0.
/// </summary>
class ConversationTranscriptionEventArgs : public RecognitionEventArgs
{
private:

    SPXEVENTHANDLE m_hevent;
    std::shared_ptr<ConversationTranscriptionResult> m_result;

public:

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="hevent">Event handle</param>
    explicit ConversationTranscriptionEventArgs(SPXEVENTHANDLE hevent) :
        RecognitionEventArgs(hevent),
        m_hevent(hevent),
        m_result(std::make_shared<ConversationTranscriptionResult>(ResultHandleFromEventHandle(hevent))),
        Result(m_result)
    {
        SPX_DBG_TRACE_VERBOSE("%s (this=0x%p, handle=0x%p)", __FUNCTION__, (void*)this, (void*)m_hevent);
    };

    /// <inheritdoc/>
    virtual ~ConversationTranscriptionEventArgs()
    {
        SPX_DBG_TRACE_VERBOSE("%s (this=0x%p, handle=0x%p)", __FUNCTION__, (void*)this, (void*)m_hevent);
        SPX_THROW_ON_FAIL(recognizer_event_handle_release(m_hevent));
    };

#if defined(SWIG) || defined(BINDING_OBJECTIVE_C)
private:
#endif
    /// <summary>
    /// Conversation transcriber result.
    /// </summary>
    std::shared_ptr<ConversationTranscriptionResult> Result;

#if defined(SWIG) || defined(BINDING_OBJECTIVE_C)
public:
#else
protected:
#endif

    /*! \cond PROTECTED */

    /// <summary>
    /// Conversation transcriber result.
    /// </summary>
    std::shared_ptr<ConversationTranscriptionResult> GetResult() const { return m_result; }

    /*! \endcond */

private:

    DISABLE_DEFAULT_CTORS(ConversationTranscriptionEventArgs);

    SPXRESULTHANDLE ResultHandleFromEventHandle(SPXEVENTHANDLE hevent)
    {
        SPXRESULTHANDLE hresult = SPXHANDLE_INVALID;
        SPX_THROW_ON_FAIL(recognizer_recognition_event_get_result(hevent, &hresult));
        return hresult;
    }
};


/// <summary>
/// Class for conversation transcriber canceled event arguments.
/// Added in version 1.5.0.
/// </summary>
class ConversationTranscriptionCanceledEventArgs : public ConversationTranscriptionEventArgs
{
private:

    std::shared_ptr<CancellationDetails> m_cancellation;
    CancellationReason m_cancellationReason;
    CancellationErrorCode m_errorCode;

public:

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="hevent">Event handle</param>
    explicit ConversationTranscriptionCanceledEventArgs(SPXEVENTHANDLE hevent) :
        ConversationTranscriptionEventArgs(hevent),
        m_cancellation(CancellationDetails::FromResult(GetResult())),
        m_cancellationReason(m_cancellation->Reason),
        m_errorCode(m_cancellation->ErrorCode),
        Reason(m_cancellationReason),
        ErrorCode(m_errorCode),
        ErrorDetails(m_cancellation->ErrorDetails)
    {
        SPX_DBG_TRACE_VERBOSE("%s (this=0x%p)", __FUNCTION__, (void*)this);
    };

    /// <inheritdoc/>
    virtual ~ConversationTranscriptionCanceledEventArgs()
    {
        SPX_DBG_TRACE_VERBOSE("%s (this=0x%p)", __FUNCTION__, (void*)this);
    };

#if defined(SWIG) || defined(BINDING_OBJECTIVE_C)
private:
#endif

#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wunused-private-field"
#endif
    /// <summary>
    /// The reason the result was canceled.
    /// </summary>
    const CancellationReason& Reason;

    /// <summary>
    /// The error code in case of an unsuccessful recognition (<see cref="Reason"/> is set to Error).
    /// If Reason is not Error, ErrorCode is set to NoError.
    /// </summary>
    const CancellationErrorCode& ErrorCode;

#ifdef __clang__
#pragma clang diagnostic pop
#endif

    /// <summary>
    /// The error message in case of an unsuccessful recognition (<see cref="Reason"/> is set to Error).
    /// </summary>
    const SPXSTRING ErrorDetails;

#if defined(SWIG) || defined(BINDING_OBJECTIVE_C)
public:
#else
private:
#endif
    /// <summary>
    /// CancellationDetails.
    /// </summary>
    std::shared_ptr<CancellationDetails> GetCancellationDetails() const { return m_cancellation; }

private:

    DISABLE_DEFAULT_CTORS(ConversationTranscriptionCanceledEventArgs);
};


} } } }  // Microsoft::CognitiveServices::Speech::Transcription
