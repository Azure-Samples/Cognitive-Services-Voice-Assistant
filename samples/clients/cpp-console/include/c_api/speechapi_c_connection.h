//
// Copyright (c) Microsoft. All rights reserved.
// See https://aka.ms/csspeech/license201809 for the full license information.
//

#pragma once
#include <speechapi_c_common.h>

SPXAPI connection_from_recognizer(SPXRECOHANDLE recognizerHandle, SPXCONNECTIONHANDLE* connectionHandle);

SPXAPI_(bool) connection_handle_is_valid(SPXCONNECTIONHANDLE handle);
SPXAPI connection_handle_release(SPXCONNECTIONHANDLE handle);

SPXAPI connection_open(SPXCONNECTIONHANDLE handle, bool forContinuousRecognition);
SPXAPI connection_close(SPXCONNECTIONHANDLE handle);
SPXAPI connection_set_message_property(SPXCONNECTIONHANDLE handle, const char* path, const char* name, const char* value);
SPXAPI connection_send_message(SPXCONNECTIONHANDLE handle, const char* path, const char* payload);
SPXAPI connection_send_message_data(SPXCONNECTIONHANDLE handle, const char* path, uint8_t* data, uint32_t size);

typedef void(*CONNECTION_CALLBACK_FUNC)(SPXEVENTHANDLE event, void* context);
SPXAPI connection_connected_set_callback(SPXCONNECTIONHANDLE connection, CONNECTION_CALLBACK_FUNC callback, void* context);
SPXAPI connection_disconnected_set_callback(SPXCONNECTIONHANDLE connection, CONNECTION_CALLBACK_FUNC callback, void* context);
SPXAPI connection_message_received_set_callback(SPXCONNECTIONHANDLE connection, CONNECTION_CALLBACK_FUNC callback, void* context);

SPXAPI_(bool) connection_message_received_event_handle_is_valid(SPXEVENTHANDLE hevent);
SPXAPI connection_message_received_event_handle_release(SPXEVENTHANDLE hevent);

SPXAPI connection_message_received_event_get_message(SPXEVENTHANDLE hevent, SPXCONNECTIONMESSAGEHANDLE* hcm);

SPXAPI_(bool) connection_message_handle_is_valid(SPXCONNECTIONMESSAGEHANDLE handle);
SPXAPI connection_message_handle_release(SPXCONNECTIONMESSAGEHANDLE handle);

SPXAPI connection_message_get_property_bag(SPXCONNECTIONMESSAGEHANDLE hcm, SPXPROPERTYBAGHANDLE* hpropbag);
SPXAPI connection_message_get_data(SPXCONNECTIONMESSAGEHANDLE hcm, uint8_t* data, uint32_t size);
SPXAPI_(uint32_t) connection_message_get_data_size(SPXCONNECTIONMESSAGEHANDLE hcm);
