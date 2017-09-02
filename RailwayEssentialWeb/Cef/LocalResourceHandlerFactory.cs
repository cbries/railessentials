using CefSharp;

namespace RailwayEssentialWeb.Cef
{
    public class LocalResourceHandlerFactory : IResourceHandlerFactory
    {
        public bool HasHandlers { get { return true; } }

        public IResourceHandler GetResourceHandler(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request)
        {
            return new LocalResourceHandler();
        }
    }
}
