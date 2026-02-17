using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace MinecraftStorageApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public App()
        {
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
        }
    }

}
