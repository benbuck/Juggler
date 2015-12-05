/*
 * Classes in this file are created for inter-process communication using names piped in WCF infrastructure.
 * 
 */

using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Juggler
{
    /// <summary>
    /// Contract to communicate from one instance to another. This will be used to notify existing instance 
    /// when user tries to start second instance of Juggler.
    /// Juggler will then show balloon tip to warn user about existing inctance.
    /// </summary>
    /// <remarks>
    /// 
    /// </remarks>
    [ServiceContract]
    internal interface ICommunicator
    {
        [OperationContract]
        void NotifyUser();
    }

    /// <summary>
    /// Contract implementation.
    /// </summary>
    internal class Communicator : ICommunicator
    {
        public void NotifyUser()
        {
            Program.ShowBalloonTip("Juggler is already running.\n\nPlease use this icon for changing wallpaper and configuring Juggler.");
        }
    }

    /// <summary>
    /// Proxy for Communicator class. Its created manually insteat of SvcUtil to create proxies for WCF services.
    /// </summary>
    internal class CommunicatorProxy : ClientBase<ICommunicator>, ICommunicator
    {
        public CommunicatorProxy(Binding binding, EndpointAddress remoteAddress)
            : base(binding, remoteAddress)
        { }

        public void NotifyUser()
        {
            base.Channel.NotifyUser();
        }
    }

    /// <summary>
    /// Utility class to:
    ///     1. host named pipe service host to recieve notification from future instances.
    ///     2. close the host.
    ///     3. send notification to first instance.
    /// Refer Program.Main method for usage.
    /// </summary>
    internal static class MultiInstanceHandler
    {
        static ServiceHost host;

        /// <summary>
        /// To start listner. It will called from FIRST instance when it starts.
        /// </summary>
        internal static void StartListner()
        {
            host = new ServiceHost(typeof(Communicator));
            NetNamedPipeBinding namedPipeBinding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
            host.AddServiceEndpoint(typeof(ICommunicator), namedPipeBinding, "net.pipe://localhost/JugglerHost");
            host.Open();
        }

        /// <summary>
        /// To stop listner. It will called from FIRST instance when it exits.
        /// </summary>
        internal static void StopListner()
        {
            if (host != null && host.State == CommunicationState.Opened)
            {
                host.Close();
            }
        }

        /// <summary>
        /// To send notification. It will be called from SECOND instance before quiting.
        /// </summary>
        internal static void NotifyExistingInstanxce()
        {
            NetNamedPipeBinding namedPipeBinding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
            EndpointAddress endpointAddress = new EndpointAddress("net.pipe://localhost/JugglerHost");

            CommunicatorProxy proxy = new CommunicatorProxy(namedPipeBinding, endpointAddress);
            proxy.NotifyUser();
            proxy.Close();
        }
    }
}
