﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Scripting;
using Common;

namespace FDA
{

    public class ScriptablePubSubConnection : ScriptableObject
    {
        public static List<ScriptablePubSubConnection> WrapConn(Dictionary<Guid, PubSubConnectionManager> toWrap)
        {
            List<ScriptablePubSubConnection> output = new List<ScriptablePubSubConnection>();
            foreach (PubSubConnectionManager connMgr in toWrap.Values)
            {
                output.Add(new ScriptablePubSubConnection(connMgr));
            }

            return output;
        }

   


        private PubSubConnectionManager _connMgr;


        public bool ConnectionEnabled { get => _connMgr.ConnectionEnabled; set { _connMgr.ConnectionEnabled = value; OnPropertyChanged(); } }
        public bool CommsEnabled { get => _connMgr.CommunicationsEnabled; set { _connMgr.CommunicationsEnabled = value; OnPropertyChanged(); } }

        public override event PropertyChangedEventHandler PropertyChanged;

        public ScriptablePubSubConnection(PubSubConnectionManager connMgr) : base(connMgr.ID)
        {
            _connMgr = connMgr;
            _connMgr.PropertyChanged += ConnMgr_PropertyChanged;
        }


        private void ConnMgr_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

