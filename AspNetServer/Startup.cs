using Microsoft.Owin;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

[assembly: OwinStartup(typeof(AspNetServer.Startup))]

namespace AspNetServer
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Registra los hubs de SignalR
            app.MapSignalR();
        }
    }
}