namespace System.Runtime.InteropServices
{
    public static class GCHandleExt
    {
        public static void Dispose(this GCHandle handle)
        {
            try
            {
                handle.Free();
            }
            catch (Exception e)
            {
            }
        }
    }
}