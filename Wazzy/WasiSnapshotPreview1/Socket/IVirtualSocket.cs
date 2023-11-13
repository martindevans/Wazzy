using Wasmtime;
using Wazzy.Interop;
using Wazzy.WasiSnapshotPreview1.FileSystem;

namespace Wazzy.WasiSnapshotPreview1.Socket;

public abstract class BaseWasiSocket
    : IWasiFeature
{
    /// <summary>
    /// The module which the exports of this feature are defined in
    /// </summary>
    public static readonly string Module = "wasi_snapshot_preview1";

    /// <summary>
    /// Accept a new incoming connection.
    /// This is similar to accept in POSIX.
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="fd"></param>
    /// <param name="flags"></param>
    /// <returns></returns>
    public abstract WasiError Accept(Caller caller, FileDescriptor fd, FdFlags flags);

    /// <summary>
    /// Receive a message from a socket.
    /// This is similar to recv in POSIX, though it also supports reading the data into multiple buffers in the manner of readv.
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="fd"></param>
    /// <param name="riData"></param>
    /// <param name="riFlags"></param>
    /// <param name="roDataPtr"></param>
    /// <param name="roFlagsOut"></param>
    /// <returns></returns>
    public abstract WasiError Receive(Caller caller, FileDescriptor fd, Buffer<Buffer<byte>> riData, RiFlags riFlags, Pointer<byte> roDataPtr, Pointer<RoFlags> roFlagsOut);

    /// <summary>
    /// Send a message on a socket.
    /// This is similar to send in POSIX, though it also supports writing the data from multiple buffers in the manner of writev.
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="fd"></param>
    /// <param name="siData"></param>
    /// <param name="siFlags"></param>
    /// <param name="sentBytes"></param>
    /// <returns></returns>
    public abstract WasiError Send(Caller caller, FileDescriptor fd, Buffer<Buffer<byte>> siData, SiFlags siFlags, out int sentBytes);

    /// <summary>
    /// Shut down socket send and receive channels.
    /// This is similar to shutdown in POSIX.
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="fd"></param>
    /// <param name="how"></param>
    /// <returns></returns>
    public abstract WasiError Shutdown(Caller caller, FileDescriptor fd, SdFlags how);

    public void DefineOn(Linker linker)
    {
#nullable disable
        linker.DefineFunction(Module, "sock_accept",
            (Caller caller, int fd, int flags) => (int)Accept(
                caller,
                new FileDescriptor(fd),
                (FdFlags)flags
            )
        );

        linker.DefineFunction(Module, "sock_recv",
            (Caller caller, int fd, int riDataAddr, int riDataCount, int riFlags, int roDataAddr, int roFlagsAddr) => (int)Receive(
                caller,
                new FileDescriptor(fd),
                new Buffer<Buffer<byte>>(riDataAddr, (uint)riDataCount),
                (RiFlags)riFlags,
                new Pointer<byte>(roDataAddr),
                new Pointer<RoFlags>(roFlagsAddr)
            )
        );

        linker.DefineFunction(Module, "sock_send",
            (Caller caller, int fd, int siDataAddr, int sdDataCount, int siFlags, int resultAddr) => (int)Send(
                caller,
                new FileDescriptor(fd),
                new Buffer<Buffer<byte>>(siDataAddr, (uint)sdDataCount),
                (SiFlags)siFlags,
                out new Pointer<int>(resultAddr).Deref(caller)
            )
        );

        linker.DefineFunction(Module, "sock_shutdown",
            (Caller caller, int fd, int flags) => (int)Shutdown(
                caller,
                new FileDescriptor(fd),
                (SdFlags)flags
            )
        );
#nullable restore
    }
}