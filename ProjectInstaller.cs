using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace GameServerService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        private ServiceProcessInstaller serviceProcessInstaller;
        private ServiceInstaller serviceInstaller;

        public ProjectInstaller()
        {
            serviceProcessInstaller = new ServiceProcessInstaller();
            serviceInstaller = new ServiceInstaller();

            // Set account type
            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;

            // Configure the service
            serviceInstaller.ServiceName = "GameServerService";
            serviceInstaller.StartType = ServiceStartMode.Manual;

            Installers.Add(serviceProcessInstaller);
            Installers.Add(serviceInstaller);
        }
    }
}
