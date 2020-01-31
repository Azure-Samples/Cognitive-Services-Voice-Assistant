//
// Copyright (c) Microsoft. All rights reserved.
// See https://aka.ms/csspeech/license201809 for the full license information.
//
// speechapi_c_common.h: Public API declarations for global C definitions and typedefs
//

#pragma once

#include <stdbool.h>
#include <spxerror.h>

#ifdef __cplusplus
#define SPX_EXTERN_C        extern "C"
#else
#define SPX_EXTERN_C        extern
#endif

#ifdef _WIN32

#ifdef SPX_CONFIG_EXPORTAPIS
#define SPXAPI_EXPORT       __declspec(dllexport)
#endif

#ifdef SPX_CONFIG_IMPORTAPIS
#define SPXAPI_EXPORT       __declspec(dllimport)
#endif

#ifndef SPXAPI_EXPORT
#define SPXAPI_EXPORT __declspec(dllimport)
#endif

#define SPXAPI_NOTHROW      __declspec(nothrow)
#define SPXAPI_RESULTTYPE   SPXHR
#define SPXAPI_CALLTYPE     __stdcall
#define SPXAPI_VCALLTYPE    __cdecl

#define SPXDLL_EXPORT       __declspec(dllexport)

#elif defined(SWIG)

#define SPXAPI_EXPORT
#define SPXAPI_NOTHROW
#define SPXAPI_RESULTTYPE   SPXHR
#define SPXAPI_CALLTYPE
#define SPXAPI_VCALLTYPE
#define SPXDLL_EXPORT

#else

#define SPXAPI_EXPORT       __attribute__ ((__visibility__("default")))

#define SPXAPI_NOTHROW      __attribute__((nothrow))
#define SPXAPI_RESULTTYPE   SPXHR
// when __attribute__((stdcall)) is set, gcc generates a warning : stdcall attribute ignored.
#define SPXAPI_CALLTYPE
#define SPXAPI_VCALLTYPE    __attribute__((cdecl))

#define SPXDLL_EXPORT       __attribute__ ((__visibility__("default")))

#endif

#define SPXAPI              SPX_EXTERN_C SPXAPI_EXPORT SPXAPI_RESULTTYPE SPXAPI_NOTHROW SPXAPI_CALLTYPE
#define SPXAPI_(type)       SPX_EXTERN_C SPXAPI_EXPORT type SPXAPI_NOTHROW SPXAPI_CALLTYPE
#define SPXAPI__(type)      SPX_EXTERN_C SPXAPI_EXPORT SPXAPI_NOTHROW type SPXAPI_CALLTYPE

#define SPXAPIV             SPX_EXTERN_C SPXAPI_EXPORT SPXAPI_NOTHROW SPXAPI_RESULTTYPE SPXAPI_VCALLTYPE
#define SPXAPIV_(type)      SPX_EXTERN_C SPXAPI_EXPORT SPXAPI_NOTHROW type SPXAPI_VCALLTYPE

#define SPXAPI_PRIVATE          SPX_EXTERN_C SPXAPI_RESULTTYPE SPXAPI_NOTHROW SPXAPI_CALLTYPE
#define SPXAPI_PRIVATE_(type)   SPX_EXTERN_C type SPXAPI_NOTHROW SPXAPI_CALLTYPE

struct _spx_empty {};
typedef struct _spx_empty* _spxhandle;
typedef _spxhandle SPXHANDLE;

typedef SPXHANDLE SPXASYNCHANDLE;
typedef SPXHANDLE SPXFACTORYHANDLE;
typedef SPXHANDLE SPXRECOHANDLE;
typedef SPXHANDLE SPXSYNTHHANDLE;
typedef SPXHANDLE SPXRESULTHANDLE;
typedef SPXHANDLE SPXEVENTHANDLE;
typedef SPXHANDLE SPXSESSIONHANDLE;
typedef SPXHANDLE SPXTRIGGERHANDLE;
typedef SPXHANDLE SPXLUMODELHANDLE;
typedef SPXHANDLE SPXKEYWORDHANDLE;
typedef SPXHANDLE SPXERRORHANDLE;
typedef SPXHANDLE SPXAUDIOSTREAMFORMATHANDLE;
typedef SPXHANDLE SPXAUDIOSTREAMHANDLE;
typedef SPXHANDLE SPXAUDIOCONFIGHANDLE;
typedef SPXHANDLE SPXPROPERTYBAGHANDLE;
typedef SPXHANDLE SPXSPEECHCONFIGHANDLE;
typedef SPXHANDLE SPXCONNECTIONHANDLE;
typedef SPXHANDLE SPXACTIVITYHANDLE;
typedef SPXHANDLE SPXACTIVITYJSONHANDLE;
typedef SPXHANDLE SPXGRAMMARHANDLE;
typedef SPXHANDLE SPXPHRASEHANDLE;
typedef SPXHANDLE SPXUSERHANDLE;
typedef SPXHANDLE SPXPARTICIPANTHANDLE;
typedef SPXHANDLE SPXAUTODETECTSOURCELANGCONFIGHANDLE;
typedef SPXHANDLE SPXSOURCELANGCONFIGHANDLE;
typedef SPXHANDLE SPXCONVERSATIONHANDLE;

#define SPXHANDLE_INVALID   ((SPXHANDLE)-1)
