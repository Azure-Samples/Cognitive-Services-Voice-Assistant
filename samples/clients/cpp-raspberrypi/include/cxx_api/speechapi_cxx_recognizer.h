//
// Copyright (c) Microsoft. All rights reserved.
// See https://aka.ms/csspeech/license201809 for the full license information.
//
// speechapi_cxx_recognizer.h: Public API declarations for Recognizer C++ base class
//

#pragma once
#include <speechapi_cxx_common.h>


namespace Microsoft {
namespace CognitiveServices {
namespace Speech {

/// <summary>
/// Recognizer base class.
/// </summary>
class Recognizer : public std::enable_shared_from_this<Recognizer>
{
    friend class Connection;
public:

    /// <summary>
    /// Internal operator used to get underlying handle value.
    /// </summary>
    /// <returns>A handle.</returns>
    explicit operator SPXRECOHANDLE() const { return m_hreco; }

    /// <summary>
    /// Returns true if the recognizer is enabled.
    /// </summary>
    /// <returns>A Boolean indicating whether the recognizer is enabled.</returns>
    virtual bool IsEnabled()
    {
        bool enabled = false;
        SPX_INIT_HR(hr);
        SPX_THROW_ON_FAIL(hr = recognizer_is_enabled(m_hreco, &enabled));
        return enabled;
    };

    /// <summary>
    /// Enables the recognizer.
    /// </summary>
    virtual void Enable()
    {
        SPX_INIT_HR(hr);
        SPX_THROW_ON_FAIL(hr = recognizer_enable(m_hreco));
    };

    /// <summary>
    /// Disables the recognizer.
    /// </summary>
    virtual void Disable()
    {
        SPX_INIT_HR(hr);
        SPX_THROW_ON_FAIL(hr = recognizer_disable(m_hreco));
    };

protected:

    /*! \cond PROTECTED */

    explicit Recognizer(SPXRECOHANDLE hreco) :
        m_hreco(hreco)
    {
        SPX_DBG_TRACE_SCOPE(__FUNCTION__, __FUNCTION__);
        SPX_DBG_TRACE_VERBOSE("%s: m_hreco=0x%8p", __FUNCTION__, (void*)m_hreco);
    }

    virtual ~Recognizer()
    {
        SPX_DBG_TRACE_SCOPE(__FUNCTION__, __FUNCTION__);
        TermRecognizer();
    }

    virtual void TermRecognizer()
    {
        SPX_DBG_TRACE_SCOPE(__FUNCTION__, __FUNCTION__);

        if (m_hreco != SPXHANDLE_INVALID)
        {
            ::recognizer_handle_release(m_hreco);
            m_hreco = SPXHANDLE_INVALID;
            SPX_DBG_TRACE_VERBOSE("%s: m_hreco=0x%8p", __FUNCTION__, (void*)m_hreco);
        }
    }

    SPXRECOHANDLE m_hreco;

    /*! \endcond */

private:

    DISABLE_DEFAULT_CTORS(Recognizer);
};


} } } // Microsoft::CognitiveServices::Speech
