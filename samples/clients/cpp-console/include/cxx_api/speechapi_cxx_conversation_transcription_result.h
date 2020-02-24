//
// Copyright (c) Microsoft. All rights reserved.
// See https://aka.ms/csspeech/license201809 for the full license information.
//
// speechapi_cxx_conversation_transcriber_result.h: Public API declarations for ConversationTranscription C++ class
//

#pragma once
#include <string>
#include <speechapi_cxx_common.h>
#include <speechapi_cxx_string_helpers.h>
#include <speechapi_cxx_recognition_result.h>
#include <speechapi_c.h>


namespace Microsoft {
namespace CognitiveServices {
namespace Speech {
namespace Transcription {

/// <summary>
/// Represents the result of a conversation transcriber.
/// Added in version 1.5.0.
/// </summary>
class ConversationTranscriptionResult final : public RecognitionResult
{
public:

    /// <summary>
    /// Internal constructor. Creates a new instance using the provided handle.
    /// </summary>
    /// <param name="hresult">Result handle.</param>
    explicit ConversationTranscriptionResult(SPXRESULTHANDLE hresult) :
        RecognitionResult(hresult),
        UserId(m_userId)
    {
        PopulateSpeakerFields(hresult, &m_userId);
        SPX_DBG_TRACE_VERBOSE("%s (this=0x%p, handle=0x%p) -- resultid=%s; reason=0x%x; text=%s, userid=%s", __FUNCTION__, (void*)this, (void*)Handle, Utils::ToUTF8(ResultId).c_str(), Reason, Utils::ToUTF8(Text).c_str(), Utils::ToUTF8(UserId).c_str());
    }

    /// <summary>
    /// Destructor.
    /// </summary>
    ~ConversationTranscriptionResult()
    {
        SPX_DBG_TRACE_VERBOSE("%s (this=0x%p, handle=0x%p)", __FUNCTION__, (void*)this, (void*)Handle);
    }

    /// <summary>
    /// Unique Speaker id.
    /// </summary>
    const SPXSTRING& UserId;

private:
    DISABLE_DEFAULT_CTORS(ConversationTranscriptionResult);

    void PopulateSpeakerFields(SPXRESULTHANDLE hresult, SPXSTRING* puserId)
    {
        SPX_INIT_HR(hr);

        const size_t maxCharCount = 1024;
        char sz[maxCharCount+1];

        if (puserId != nullptr && recognizer_result_handle_is_valid(hresult))
        {
            SPX_THROW_ON_FAIL(hr = conversation_transcription_result_get_user_id(hresult, sz, maxCharCount));
            *puserId = Utils::ToSPXString(sz);
        }
    }

    SPXSTRING m_userId;
};


} } } }  // Microsoft::CognitiveServices::Speech::Transcription
