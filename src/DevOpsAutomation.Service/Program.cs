using System.ServiceProcess;

namespace DevOpsAutomation.Service
{
    static class Program
    {
        /// <summary>Ponto de entrada principal da aplicação</summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun = new ServiceBase[]
            {
                new DevOpsAutomationService()
            };

            ServiceBase.Run(ServicesToRun);
        }
    }
}
