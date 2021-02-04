﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace FDAInterface.FDAApplication {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(Namespace="FDAApplication", ConfigurationName="FDAApplication.IFDAApplication")]
    public interface IFDAApplication {
        
        [System.ServiceModel.OperationContractAttribute(Action="FDAApplication/IFDAApplication/SetConsoleVisible", ReplyAction="FDAApplication/IFDAApplication/SetConsoleVisibleResponse")]
        void SetConsoleVisible(bool visible);
        
        [System.ServiceModel.OperationContractAttribute(Action="FDAApplication/IFDAApplication/SetConsoleVisible", ReplyAction="FDAApplication/IFDAApplication/SetConsoleVisibleResponse")]
        System.Threading.Tasks.Task SetConsoleVisibleAsync(bool visible);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface IFDAApplicationChannel : FDAInterface.FDAApplication.IFDAApplication, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class FDAApplicationClient : System.ServiceModel.ClientBase<FDAInterface.FDAApplication.IFDAApplication>, FDAInterface.FDAApplication.IFDAApplication {
        
        public FDAApplicationClient() {
        }
        
        public FDAApplicationClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public FDAApplicationClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public FDAApplicationClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public FDAApplicationClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public void SetConsoleVisible(bool visible) {
            base.Channel.SetConsoleVisible(visible);
        }
        
        public System.Threading.Tasks.Task SetConsoleVisibleAsync(bool visible) {
            return base.Channel.SetConsoleVisibleAsync(visible);
        }
    }
}
