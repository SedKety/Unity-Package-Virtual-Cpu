using VirtualCPU;

/// <summary>
/// Call IDs for the Unity interop host library (library ID 0x01).
/// Use: HOSTCALL 0x01 &lt;functionIndex&gt;
/// </summary>
public enum UnityLibrarySyscall : byte
{
    // <-----------Object lifecycle------------>

    /// <summary>Spawns a prefab at the position in R0/R1/R2. ECX=prefabId, EDX=destReg (receives object ID).</summary>
    SysSpawn        = 0x00,

    /// <summary>Destroys the object whose ID is in ECX.</summary>
    SysDestroy      = 0x01,

    // <-----------Transform------------>

    /// <summary>Sets position of object ECX from R0/R1/R2.</summary>
    SysSetPosition  = 0x02,

    /// <summary>Loads position of object ECX into R0/R1/R2.</summary>
    SysLoadPosition = 0x03,

    /// <summary>Sets rotation (Euler X,Y,Z) of object ECX from R0/R1/R2.</summary>
    SysSetRotation  = 0x04,

    /// <summary>Reads rotation (Euler X,Y,Z) of object ECX into R0/R1/R2.</summary>
    SysGetRotation  = 0x05,

    /// <summary>Sets scale (X,Y,Z) of object ECX from R0/R1/R2.</summary>
    SysSetScale     = 0x06,

    /// <summary>Reads scale (X,Y,Z) of object ECX into R0/R1/R2.</summary>
    SysGetScale     = 0x07,

    /// <summary>Sets the active state of object ECX. EDX=0 deactivates, non-zero activates.</summary>
    SysSetActive    = 0x08,

    /// <summary>Writes the active state (0 or 1) of object ECX into EDX.</summary>
    SysGetActive    = 0x09,

    // <-----------Physics------------>

    /// <summary>Sets the rigidbody velocity of object ECX from R0/R1/R2.</summary>
    SysSetVelocity  = 0x0A,

    /// <summary>Reads the rigidbody velocity of object ECX into R0/R1/R2.</summary>
    SysGetVelocity  = 0x0B,

    /// <summary>Applies a force vector R0/R1/R2 to object ECX. EDX=force mode (Force=0, Impulse=1).</summary>
    SysApplyForce   = 0x0C,

    /// <summary>Casts a ray from R0/R1/R2 in direction R3/R4/R5 with max distance ECX. Writes hit flag (0/1) into EDX.</summary>
    SysRaycast      = 0x0D,

    // <-----------Animation------------>

    /// <summary>Plays animation clip ECX on object EDX.</summary>
    SysPlayAnimation = 0x0E,

    /// <summary>Stops animation clip ECX on object EDX.</summary>
    SysStopAnimation = 0x0F,

    // <-----------Audio------------>

    /// <summary>Plays 2D sound ECX at volume EDX.</summary>
    SysPlaySound    = 0x10,

    /// <summary>Stops sound ECX.</summary>
    SysStopSound    = 0x11,

    // <-----------Rendering------------>

    /// <summary>Sets material color of object ECX from R0(R) R1(G) R2(B) R3(A).</summary>
    SysSetColor     = 0x12,

    /// <summary>Reads material color of object ECX into R0(R) R1(G) R2(B) R3(A).</summary>
    SysGetColor     = 0x13,

    // <-----------Scene / Resource------------>

    /// <summary>Loads scene ECX. EDX=load mode (Single=0, Additive=1).</summary>
    SysLoadScene    = 0x14,

    /// <summary>Unloads scene ECX.</summary>
    SysUnloadScene  = 0x15,

    /// <summary>Finds the first object with tag ECX and writes its ID into EDX. EDX=0 if not found.</summary>
    SysFindByTag    = 0x16,

    /// <summary>Finds the first object with name ECX and writes its ID into EDX. EDX=0 if not found.</summary>
    SysFindByName   = 0x17,

    // <-----------UI------------>

    /// <summary>Shows UI element ECX.</summary>
    SysUIShow       = 0x18,

    /// <summary>Hides UI element ECX.</summary>
    SysUIHide       = 0x19,

    /// <summary>Sets the text of UI element ECX from the string at memory address EDX.</summary>
    SysUISetText    = 0x1A,

    /// <summary>Reads the text handle of UI element ECX into EDX.</summary>
    SysUIGetText    = 0x1B,

    // <-----------Component / Messaging------------>

    /// <summary>Adds component ECX to object EDX.</summary>
    SysAddComponent    = 0x1C,

    /// <summary>Removes component ECX from object EDX.</summary>
    SysRemoveComponent = 0x1D,

    /// <summary>Gets the handle of component ECX on object EDX and writes it into ESI. ESI=0 if not found.</summary>
    SysGetComponent    = 0x1E,

    /// <summary>Sends message ECX to object EDX with argument in ESI.</summary>
    SysSendMessage     = 0x1F,

    // <-----------Camera------------>

    /// <summary>Sets the position of camera object ECX from R0/R1/R2.</summary>
    SysCameraSet    = 0x20,

    /// <summary>Makes camera object ECX look at target object EDX.</summary>
    SysCameraLookAt = 0x21,

    // <-----------Light------------>

    /// <summary>Sets the intensity of light object ECX to the value in EDX.</summary>
    SysLightSet     = 0x22,

    // <-----------Particles------------>

    /// <summary>Plays the particle system attached to object ECX.</summary>
    SysParticlePlay = 0x23,

    /// <summary>Stops the particle system attached to object ECX.</summary>
    SysParticleStop = 0x24,

    // <-----------Host / Debug------------>

    /// <summary>Generic host call. ECX=call ID, remaining args in EDX/ESI/EDI.</summary>
    SysHostCall     = 0x25,

    /// <summary>Logs the null-terminated string at memory address ECX to the Unity console at level EDX.</summary>
    SysLogString    = 0x26,
}
