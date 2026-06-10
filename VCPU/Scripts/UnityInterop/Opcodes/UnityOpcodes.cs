

namespace VirtualCPU.UnityInterop
{
    /// <summary>
    /// This enum provides a non-magic way to refer to the UNITY specific opcodes.
    /// Each opcode is represented by a unique byte value, which the VirtualCPU will interpret during execution.
    /// TODO: Make these  work, they dont do anything as of now
    /// </summary>
    public enum UnityOpcodes : byte
    {
        // <-----------Program opcodes--------------->

        // <-----------Locational / Object opcodes------------>

        /// <summary>
        /// SPWN PrefabID DestReg
        /// Location is determined by three consecutive registers (R0..R2) at the time of execution.
        /// Spawns a prefab (by index) at the specified coordinates.
        /// Stores the local object id/handle in the provided register.
        /// </summary>
        SPWN = 0x20,

        /// <summary>
        /// SPOS SourceType(Immediate, reg, mem) Source(register index, memory location)
        /// Sets position from three consecutive registers (R0..R2) into the object specified by LocalObjectID.
        /// </summary>
        SPOS = 0x21,

        /// <summary>
        /// LPOS SourceType(Immediate, reg, mem) Source(register index, memory location)
        /// Loads position of LocalObjectID into three consecutive registers (R0..R2).
        /// </summary>
        LPOS = 0x22,

        /// <summary>
        /// DEST SourceType(Immediate, reg, mem) Source(register index, memory location)
        /// Destroys the specified local object by id which is determined by the source type and value (immediate, register, or memory).
        /// </summary>
        DEST = 0x23,

        /// <summary>
        /// SROT LocalObjectID
        /// Sets rotation (Euler X,Y,Z) from the first three registers into LocalObjectID.
        /// </summary>
        SROT = 0x24,

        /// <summary>
        /// UROT LocalObjectID
        /// Reads rotation into three registers from LocalObjectID.
        /// </summary>
        UROT = 0x25,

        /// <summary>
        /// SSCL LocalObjectID
        /// Sets scale (X,Y,Z) from three registers into LocalObjectID.
        /// </summary>
        SSCL = 0x26,

        /// <summary>
        /// USCL LocalObjectID
        /// Reads scale into three registers from LocalObjectID.
        /// </summary>
        USCL = 0x27,

        /// <summary>
        /// SETACT LocalObjectID, BoolReg
        /// Sets active state of object using boolean value in BoolReg (0 = false, non-zero = true).
        /// </summary>
        SETACT = 0x28,

        /// <summary>
        /// GETACT LocalObjectID, DestReg
        /// Writes object's active state (0/1) into DestReg.
        /// </summary>
        GETACT = 0x29,

        // <-----------Physics / Movement opcodes------------>

        /// <summary>
        /// SETVEL LocalObjectID, RegX, RegY, RegZ
        /// Sets rigidbody velocity from three registers.
        /// </summary>
        SETVEL = 0x2A,

        /// <summary>
        /// GETVEL LocalObjectID, DestRegX, DestRegY, DestRegZ
        /// Reads rigidbody velocity into three registers.
        /// </summary>
        GETVEL = 0x2B,

        /// <summary>
        /// APPLYF LocalObjectID, ForceRegX, ForceRegY, ForceRegZ, Mode
        /// Applies a force vector to the object's rigidbody. Mode can select Force/Impulse.
        /// </summary>
        APPLYF = 0x2C,

        /// <summary>
        /// RAYCST OriginRegX, OriginRegY, OriginRegZ, DirRegX, DirRegY, DirRegZ, MaxDist, HitReg
        /// Performs a raycast and writes a hit flag (0/1) to HitReg and optionally hit id/info to subsequent registers.
        /// </summary>
        RAYCST = 0x2D,

        // <-----------Animation / Audio / Visual------------>

        /// <summary>
        /// ANIMPL LocalObjectID, ClipID
        /// Plays an animation clip by index on the object's animator.
        /// </summary>
        ANIMPL = 0x2E,

        /// <summary>
        /// ANIMST LocalObjectID, ClipID
        /// Stops an animation clip.
        /// </summary>
        ANIMST = 0x2F,

        /// <summary>
        /// PLAYSND SoundID, VolumeReg
        /// Plays a 2D sound identified by SoundID. Volume optional via register.
        /// </summary>
        PLAYSND = 0x30,

        /// <summary>
        /// STOPSND SoundID
        /// Stops a playing sound.
        /// </summary>
        STOPSND = 0x31,

        /// <summary>
        /// SETCLR LocalObjectID, RegR, RegG, RegB, RegA
        /// Sets material color (if present) from RGBA registers.
        /// </summary>
        SETCLR = 0x32,

        /// <summary>
        /// GETCLR LocalObjectID, DestR, DestG, DestB, DestA
        /// Reads material color into registers.
        /// </summary>
        GETCLR = 0x33,

        // <-----------Scene / Resource / UI opcodes------------>

        /// <summary>
        /// LOADSC SceneNameID, Mode
        /// Loads a scene by name/index. Mode can specify additive or single.
        /// </summary>
        LOADSC = 0x34,

        /// <summary>
        /// UNLOADSC SceneNameID
        /// Unloads a scene by name/index.
        /// </summary>
        UNLOADSC = 0x35,

        /// <summary>
        /// FINDTAG TagID, DestReg
        /// Finds the first object with tag and stores its local id in DestReg (0 = none).
        /// </summary>
        FINDTAG = 0x36,

        /// <summary>
        /// FINDNAM NameID, DestReg
        /// Finds the first object by name and stores its local id in DestReg (0 = none).
        /// </summary>
        FINDNAM = 0x37,

        /// <summary>
        /// UI_SHOW UIElementID
        /// Shows a UI element referenced by id.
        /// </summary>
        UI_SHOW = 0x38,

        /// <summary>
        /// UI_HIDE UIElementID
        /// Hides a UI element referenced by id.
        /// </summary>
        UI_HIDE = 0x39,

        /// <summary>
        /// UI_SETTXT UIElementID, SourceReg
        /// Sets UI text content from a memory/string referenced by SourceReg.
        /// </summary>
        UI_SETTXT = 0x3A,

        /// <summary>
        /// UI_GETTXT UIElementID, DestReg
        /// Reads UI text id/handle into DestReg (actual string retrieval may require host call).
        /// </summary>
        UI_GETTXT = 0x3B,

        // <-----------Component / Messaging------------>

        /// <summary>
        /// ADDCMP LocalObjectID, ComponentID
        /// Adds a component by id to the object (returns component handle in register if applicable).
        /// </summary>
        ADDCMP = 0x3C,

        /// <summary>
        /// REMCMP LocalObjectID, ComponentID
        /// Removes a component by id from the object.
        /// </summary>
        REMCMP = 0x3D,

        /// <summary>
        /// GETCMP LocalObjectID, ComponentID, DestReg
        /// Gets a component handle/reference into DestReg (0 = none).
        /// </summary>
        GETCMP = 0x3E,

        /// <summary>
        /// SENDMSG LocalObjectID, MessageID, ArgReg0..N
        /// Sends a message (SendMessage-like) to the object with optional args in registers.
        /// </summary>
        SENDMSG = 0x3F,

        // <-----------Camera / Light / Particle------------>

        /// <summary>
        /// CAMSET LocalObjectID, RegX, RegY, RegZ
        /// Moves or positions a camera object.
        /// </summary>
        CAMSET = 0x40,

        /// <summary>
        /// CAMLOOK LocalObjectID, TargetLocalID
        /// Makes camera look at a target object.
        /// </summary>
        CAMLOOK = 0x41,

        /// <summary>
        /// LIGHTSET LocalObjectID, IntensityReg
        /// Sets a light's intensity from a register.
        /// </summary>
        LIGHTSET = 0x42,

        /// <summary>
        /// PRTPLAY LocalObjectID
        /// Plays particle system attached to object.
        /// </summary>
        PRTPLAY = 0x43,

        /// <summary>
        /// PRTSTOP LocalObjectID
        /// Stops particle system attached to object.
        /// </summary>
        PRTSTOP = 0x44,

        // <-----------Host/Interop / Debug opcodes------------>

        /// <summary>
        /// HCALL HostCallID, ArgReg0..N
        /// Generic host call opcode: delegates special operations to the Unity host.
        /// Use for complex tasks that are better implemented in managed Unity code.
        /// </summary>
        HCALL = 0x45,

        /// <summary>
        /// LOGSTR StrAddrReg, Level
        /// Logs a string from virtual memory to the Unity console at the provided level.
        /// </summary>
        LOGSTR = 0x46,
    }
}