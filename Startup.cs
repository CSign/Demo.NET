using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(CSign.Integration.Example.Startup))]
namespace CSign.Integration.Example
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
