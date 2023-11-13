namespace Wazzy.WasiSnapshotPreview1;

/// <summary>
/// A complete list of possible WASI return codes.
/// </summary>
public enum WasiError
    : ushort
{
    SUCCESS = 0,

    /// <summary>Arg list too long</summary>
    E2BIG = 1,

    /// <summary>Permission denied</summary>
    EACCES = 2,

    /// <summary>Address already in use</summary>
    EADDRINUSE = 3,

    /// <summary>Cannot assign requested address</summary>
    EADDRNOTAVAIL = 4,

    /// <summary>Address family not supported by protocol</summary>
    EAFNOSUPPORT = 5,

    /// <summary>Try again</summary>
    EAGAIN = 6,

    /// <summary>Operation already in progress</summary>
    EALREADY = 7,

    /// <summary>Bad file number</summary>
    EBADF = 8,

    /// <summary>Bad message</summary>
    EBADMSG = 9,

    /// <summary>Device or resource busy</summary>
    EBUSY = 10,

    /// <summary>Operation canceled</summary>
    ECANCELED = 11,

    /// <summary>No child processes</summary>
    ECHILD = 12,

    /// <summary>Connection aborted</summary>
    ECONNABORTED = 13,

    /// <summary>Connection refused</summary>
    ECONNREFUSED = 14,

    /// <summary>Connection reset</summary>
    ECONNRESET = 15,

    /// <summary>Resource deadlock would occur</summary>
    EDEADLK = 16,

    /// <summary>Destination address required</summary>
    EDESTADDRREQ = 17,

    /// <summary>Mathematics argument out of domain of function</summary>
    EDOM = 18,

    ///// <summary>Reserved</summary>
    //EDQUOT = 19,

    /// <summary>File exists</summary>
    EEXIST = 20,

    /// <summary>Bad address</summary>
    EFAULT = 21,

    /// <summary>File too large</summary>
    EFBIG = 22,

    /// <summary>Host is unreachable</summary>
    EHOSTUNREACH = 23,

    /// <summary>Identifier removed</summary>
    EIDRM = 24,

    /// <summary>Illegal byte sequence</summary>
    EILSEQ = 25,

    /// <summary>Operation in progress</summary>
    EINPROGRESS = 26,

    /// <summary>Interrupted function</summary>
    EINTR = 27,

    /// <summary>Invalid argument</summary>
    EINVAL = 28,

    /// <summary>I/O error</summary>
    EIO = 29,

    /// <summary>Socket is connected</summary>
    EISCONN = 30,

    /// <summary>Is a directory</summary>
    EISDIR = 31,

    /// <summary>Too many levels of symbolic links</summary>
    ELOOP = 32,

    /// <summary>File descriptor value too large</summary>
    EMFILE = 33,

    /// <summary>Too many links</summary>
    EMLINK = 34,

    /// <summary>Message too large</summary>
    EMSGSIZE = 35,

    ///// <summary>Reserved</summary>
    //EMULTIHOP = 36,

    /// <summary>Filename too long</summary>
    ENAMETOOLONG = 37,

    /// <summary>Network is down</summary>
    ENETDOWN = 38,

    /// <summary>Connection aborted by network</summary>
    ENETRESET = 39,

    /// <summary>Network unreachable</summary>
    ENETUNREACH = 40,

    /// <summary>Too many files open</summary>
    ENFILE = 41,

    /// <summary>No buffer space available</summary>
    ENOBUFS = 42,

    /// <summary>No such device</summary>
    ENODEV = 43,

    /// <summary>No Entity</summary>
    ENOENT = 44,

    /// <summary>Executable file format error</summary>
    ENOEXEC = 45,

    /// <summary>No locks available</summary>
    ENOLCK = 46,

    ///// <summary>Reserved</summary>
    //ENOLINK = 47,

    /// <summary>Not enough space</summary>
    ENOMEM = 48,

    /// <summary>No message of the desired type</summary>
    ENOMSG = 49,

    /// <summary>Protocol not available</summary>
    ENOPROTOOPT = 50,

    /// <summary>No space left on device</summary>
    ENOSPC = 51,

    /// <summary>Function not supported</summary>
    ENOSYS = 52,

    /// <summary>The socket is not connected</summary>
    ENOTCONN = 53,

    /// <summary>Not a directory or a symbolic link to a directory</summary>
    ENOTDIR = 54,

    /// <summary>Directory not empty</summary>
    ENOTEMPTY = 55,

    /// <summary>State not recoverable</summary>
    ENOTRECOVERABLE = 56,

    /// <summary>Not a socket</summary>
    ENOTSOCK = 57,

    /// <summary>Not supported, or operation not supported on socket</summary>
    ENOTSUP = 58,

    /// <summary>Inappropriate I/O control operation</summary>
    ENOTTY = 59,

    /// <summary>No such device or address</summary>
    ENXIO = 60,

    /// <summary>Value too large to be stored in data type</summary>
    EOVERFLOW = 61,

    /// <summary>Previous owner died</summary>
    EOWNERDEAD = 62,

    /// <summary>Operation not permitted</summary>
    EPERM = 63,

    /// <summary>Broken pipe</summary>
    EPIPE = 64,

    /// <summary>Protocol error</summary>
    EPROTO = 65,

    /// <summary>Protocol not supported</summary>
    EPROTONOSUPPORT = 66,

    /// <summary>Protocol wrong type for socket</summary>
    EPROTOTYPE = 67,

    /// <summary>Result too large</summary>
    ERANGE = 68,

    /// <summary>Read-only file system</summary>
    EROFS = 69,

    /// <summary>Invalid seek</summary>
    ESPIPE = 70,

    /// <summary>No such process</summary>
    ESRCH = 71,

    ///// <summary>Reserved</summary>
    //ESTALE = 72,

    /// <summary>Connection timed out</summary>
    ETIMEDOUT = 73,

    /// <summary>Text file busy</summary>
    ETXTBSY = 74,

    /// <summary>Cross-device link</summary>
    EXDEV = 75,

    /// <summary>Extension: Capabilities insufficient</summary>
    ENOTCAPABLE = 76
}