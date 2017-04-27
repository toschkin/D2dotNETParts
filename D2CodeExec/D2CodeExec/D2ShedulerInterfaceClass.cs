using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace D2
{
    [Guid("6FC67071-39CF-432C-9598-42232DDB93A9"),InterfaceType(ComInterfaceType.InterfaceIsDual)]
    interface ID2ShedulerInterface
    {

        void SetMessageFromTask([In, MarshalAs(UnmanagedType.BStr)] string message, [In] int taskId);

        int ReportTaskEnd([In, MarshalAs(UnmanagedType.BStr)] string reportString, [In] int taskId);

        void SetMessage([In, MarshalAs(UnmanagedType.BStr)] string login,
            [In, MarshalAs(UnmanagedType.BStr)] string password, 
            [In, MarshalAs(UnmanagedType.BStr)] string Ip, 
            [In] IStream streamMessage, 
            [Out] int retResult);

        void SetDataTI([In, MarshalAs(UnmanagedType.BStr)] string login,
            [In, MarshalAs(UnmanagedType.BStr)] string password,
            [In, MarshalAs(UnmanagedType.BStr)] string Ip,
            [In] IStream streamMessage,
            [Out] int retResult);

        void SetDataTS([In, MarshalAs(UnmanagedType.BStr)] string login,
            [In, MarshalAs(UnmanagedType.BStr)] string password,
            [In, MarshalAs(UnmanagedType.BStr)] string Ip,
            [In] IStream streamMessage,
            [Out] int retResult);
    }

    [ComImport, Guid("0182CCA8-F996-469B-9080-4EFE078169D7")]     
    class D2ShedulerInterfaceClass
    {        
    }
}