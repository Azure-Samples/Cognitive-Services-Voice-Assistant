//
// Copyright (c) Microsoft. All rights reserved.
// See https://aka.ms/csspeech/license201809 for the full license information.
//
// speechapi_c_dialog_service_connector.h: Public API declaration for Dialog Service Connector related C methods.
//

#pragma once
#include <speechapi_c_common.h>

SPXAPI_(bool) dialog_service_connector_handle_is_valid(SPXRECOHANDLE h_connector);
SPXAPI dialog_service_connector_handle_release(SPXRECOHANDLE h_connector);

SPXAPI_(bool) dialog_service_connector_async_handle_is_valid(SPXASYNCHANDLE h_async);
SPXAPI dialog_service_connector_async_handle_release(SPXASYNCHANDLE h_async);

SPXAPI_(bool) dialog_service_connector_async_void_handle_is_valid(SPXASYNCHANDLE h_async);
SPXAPI dialog_service_connector_async_void_handle_release(SPXASYNCHANDLE h_async);

SPXAPI_(bool) dialog_service_connector_async_string_handle_is_valid(SPXASYNCHANDLE h_async);
SPXAPI dialog_service_connector_async_string_handle_release(SPXASYNCHANDLE h_async);

SPXAPI_(bool) dialog_service_connector_async_reco_result_handle_is_valid(SPXASYNCHANDLE h_async);
SPXAPI dialog_service_connector_async_reco_result_handle_release(SPXASYNCHANDLE h_async);

SPXAPI_(bool) dialog_service_connector_activity_received_event_handle_is_valid(SPXEVENTHANDLE h_event);
SPXAPI dialog_service_connector_activity_received_event_release(SPXEVENTHANDLE h_event);

SPXAPI dialog_service_connector_get_property_bag(SPXRECOHANDLE h_connector, SPXPROPERTYBAGHANDLE* h_prop_bag);

SPXAPI dialog_service_connector_connect(SPXRECOHANDLE h_connector);
SPXAPI dialog_service_connector_connect_async(SPXRECOHANDLE h_connector, SPXASYNCHANDLE* p_async);
SPXAPI dialog_service_connector_connect_async_wait_for(SPXASYNCHANDLE h_async, uint32_t milliseconds);

SPXAPI dialog_service_connector_disconnect(SPXRECOHANDLE h_connector);
SPXAPI dialog_service_connector_disconnect_async(SPXRECOHANDLE h_connector, SPXASYNCHANDLE* p_async);
SPXAPI dialog_service_connector_disconnect_async_wait_for(SPXASYNCHANDLE h_async, uint32_t milliseconds);

SPXAPI dialog_service_connector_send_activity(SPXRECOHANDLE h_connector, const char* activity, char* interaction_id);
SPXAPI dialog_service_connector_send_activity_async(SPXRECOHANDLE h_connector, const char* activity, SPXASYNCHANDLE* p_async);
SPXAPI dialog_service_connector_send_activity_async_wait_for(SPXASYNCHANDLE h_async, uint32_t milliseconds, char* interaction_id);

SPXAPI dialog_service_connector_start_keyword_recognition(SPXRECOHANDLE h_connector, SPXKEYWORDHANDLE h_keyword);
SPXAPI dialog_service_connector_start_keyword_recognition_async(SPXRECOHANDLE h_connector, SPXKEYWORDHANDLE h_keyword, SPXASYNCHANDLE* p_async);
SPXAPI dialog_service_connector_start_keyword_recognition_async_wait_for(SPXASYNCHANDLE h_async, uint32_t milliseconds);

SPXAPI dialog_service_connector_stop_keyword_recognition(SPXRECOHANDLE h_connector);
SPXAPI dialog_service_connector_stop_keyword_recognition_async(SPXRECOHANDLE h_connector, SPXASYNCHANDLE* p_async);
SPXAPI dialog_service_connector_stop_keyword_recognition_async_wait_for(SPXASYNCHANDLE h_async, uint32_t milliseconds);

SPXAPI dialog_service_connector_listen_once(SPXRECOHANDLE h_connector, SPXRESULTHANDLE* p_result);
SPXAPI dialog_service_connector_listen_once_async(SPXRECOHANDLE h_connector, SPXASYNCHANDLE* p_async);
SPXAPI dialog_service_connector_listen_once_async_wait_for(SPXASYNCHANDLE h_async, uint32_t milliseconds, SPXRESULTHANDLE* p_result);

typedef void(*PSESSION_CALLBACK_FUNC)(SPXRECOHANDLE h_connector, SPXEVENTHANDLE h_event, void* pv_context);

SPXAPI dialog_service_connector_session_started_set_callback(SPXRECOHANDLE h_connector, PSESSION_CALLBACK_FUNC p_callback, void *pv_context);
SPXAPI dialog_service_connector_session_stopped_set_callback(SPXRECOHANDLE h_connector, PSESSION_CALLBACK_FUNC p_callback, void *pv_context);

typedef void(*PRECOGNITION_CALLBACK_FUNC)(SPXRECOHANDLE h_connector, SPXEVENTHANDLE h_event, void* pv_context);

SPXAPI dialog_service_connector_recognized_set_callback(SPXRECOHANDLE h_connector, PRECOGNITION_CALLBACK_FUNC p_callback, void *pv_context);
SPXAPI dialog_service_connector_recognizing_set_callback(SPXRECOHANDLE h_connector, PRECOGNITION_CALLBACK_FUNC p_callback, void *pv_context);
SPXAPI dialog_service_connector_canceled_set_callback(SPXRECOHANDLE h_connector, PRECOGNITION_CALLBACK_FUNC p_callback, void *pv_context);
SPXAPI dialog_service_connector_activity_received_set_callback(SPXRECOHANDLE h_connector, PRECOGNITION_CALLBACK_FUNC p_callback, void *pv_context);

SPXAPI dialog_service_connector_activity_received_event_get_activity_size(SPXEVENTHANDLE h_event, size_t* size);
SPXAPI dialog_service_connector_activity_received_event_get_activity(SPXEVENTHANDLE h_event, char* p_activity, size_t size);
SPXAPI_(bool) dialog_service_connector_activity_received_event_has_audio(SPXEVENTHANDLE h_event);
SPXAPI dialog_service_connector_activity_received_event_get_audio(SPXEVENTHANDLE h_event, SPXAUDIOSTREAMHANDLE* p_audio);
SPXAPI dialog_service_connector_recognized_size(SPXEVENTHANDLE h_event, uint32_t* size);
SPXAPI dialog_service_connector_recognized_get_result(SPXEVENTHANDLE h_event, uint32_t* size);
