using Wasmtime;
using Wazzy.Interop;
using Wazzy.WasiSnapshotPreview1.FileSystem;

namespace Wazzy.WasiSnapshotPreview1.Socket;

public class NonFunctionalSocket
    : BaseWasiSocket
{
    public override WasiError Accept(Caller caller, FileDescriptor fd, FdFlags flags)
    {
        // POSIX `accept(2)` (https://man7.org/linux/man-pages/man2/accept.2.html) specifies EPERM error as:
        //
        //  > Firewall rules forbid connection.
        //
        // Which is a good way to fail. Everything that uses networking should have some way to handle that kind of error.
        return WasiError.EPERM;
    }

    public override WasiError Receive(Caller caller, FileDescriptor fd, Buffer<Buffer<byte>> riData, RiFlags riFlags, Pointer<byte> roDataPtr, Pointer<RoFlags> roFlagsOut)
    {
        // POSIX `recv(2) (https://man7.org/linux/man-pages/man2/recv.2.html) specifies ENOTCONN error as:
        //
        // > The socket is associated with a connection-oriented
        // > protocol and has not been connected
        //
        // Since you could never connect in the first place, it follows that you're not connected!
        return WasiError.ENOTCONN;
    }

    public override WasiError Send(Caller caller, FileDescriptor fd, Buffer<Buffer<byte>> siData, SiFlags siFlags, out int sentBytes)
    {
        // POSIX `send(2) (https://man7.org/linux/man-pages/man2/send.2.html) specifies ENOTCONN error as:
        //
        // > The socket is not connected, and no target has been given.
        //
        // Since you could never connect in the first place, it follows that you're not connected!
        sentBytes = 0;
        return WasiError.ENOTCONN;
    }

    public override WasiError Shutdown(Caller caller, FileDescriptor fd, SdFlags how)
    {
        // POSIX `shutdown(2) (https://man7.org/linux/man-pages/man2/shutdown.2.html) specifies ENOTCONN error as:
        //
        // > The specified socket is not connected.
        //
        // Since you could never connect in the first place, it follows that you're not connected!
        return WasiError.ENOTCONN;
    }
}