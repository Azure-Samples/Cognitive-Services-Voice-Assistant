// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/// <summary>
/// Abstract object used to define the interface to a MicMuter
/// </summary>
/// <remarks>
/// </remarks>
class IMicMuter
{
public:
    /// <summary>
    /// The destructor should be defined to clean up any variables or resources.
    /// </summary>
    /// <remarks>
    /// </remarks>
    virtual ~IMicMuter() = default;

    /// <summary>
    /// Initialize will initialize the MicMuter with any specific OS dependent 
    /// settings. If called without parameters it should assume some appropriate
    /// defaults.
    /// </summary>
    /// <returns>A return code with a value of 0 is success, and another other value is failure</returns>
    /// <remarks>
    /// </remarks>
    virtual int Initialize() = 0;

    /// <summary>
    /// This method is used to actually mute and unmute the default microphone.
    /// </summary>
    /// <returns>A return code with a value of 0 is success, and another other value is failure</returns>
    /// <remarks>
    /// </remarks>
    virtual int MuteUnmute() = 0;

    /// <summary>
    /// This method is used to actually return the mute state of the default microphone.
    /// </summary>
    /// <returns>A returned true as muted and false as unmuted</returns>
    /// <remarks>
    /// </remarks>
    virtual bool IsMuted() = 0;
};
