//
// Copyright (c) Microsoft. All rights reserved.
// See https://aka.ms/csspeech/license201809 for the full license information.
//

#pragma once
#include <speechapi_c_common.h>

SPXAPI recognizer_create_speech_recognizer_from_config(SPXRECOHANDLE* phreco, SPXSPEECHCONFIGHANDLE hspeechconfig, SPXAUDIOCONFIGHANDLE haudioInput);
SPXAPI recognizer_create_speech_recognizer_from_auto_detect_source_lang_config(SPXRECOHANDLE* phreco, SPXSPEECHCONFIGHANDLE hspeechconfig, SPXAUTODETECTSOURCELANGCONFIGHANDLE hautoDetectSourceLangConfig, SPXAUDIOCONFIGHANDLE haudioInput);
SPXAPI recognizer_create_speech_recognizer_from_source_lang_config(SPXRECOHANDLE* phreco, SPXSPEECHCONFIGHANDLE hspeechconfig, SPXSOURCELANGCONFIGHANDLE hSourceLangConfig, SPXAUDIOCONFIGHANDLE haudioInput);
SPXAPI recognizer_create_translation_recognizer_from_config(SPXRECOHANDLE* phreco, SPXSPEECHCONFIGHANDLE hspeechconfig, SPXAUDIOCONFIGHANDLE haudioInput);
SPXAPI recognizer_create_intent_recognizer_from_config(SPXRECOHANDLE* phreco, SPXSPEECHCONFIGHANDLE hspeechconfig, SPXAUDIOCONFIGHANDLE haudioInput);
SPXAPI synthesizer_create_speech_synthesizer_from_config(SPXSYNTHHANDLE* phsynth, SPXSPEECHCONFIGHANDLE hspeechconfig, SPXAUDIOCONFIGHANDLE haudioconfig);
SPXAPI dialog_service_connector_create_dialog_service_connector_from_config(SPXRECOHANDLE* phreco, SPXSPEECHCONFIGHANDLE hspeechconfig, SPXAUDIOCONFIGHANDLE haudioInput);
SPXAPI recognizer_create_conversation_transcriber_from_config(SPXRECOHANDLE* phreco, SPXAUDIOCONFIGHANDLE haudioInput);
SPXAPI recognizer_join_conversation(SPXCONVERSATIONHANDLE hconv, SPXRECOHANDLE hreco);
SPXAPI recognizer_leave_conversation(SPXRECOHANDLE hreco);
SPXAPI transcriber_get_participants_list(SPXRECOHANDLE hreco, SPXPARTICIPANTHANDLE* participants, int size);
