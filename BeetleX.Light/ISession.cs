﻿using BeetleX.Light.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light
{
    public interface ISession
    {
        string Name { get; set; }
        void Connected(NetContext context);

        AuthenticationType Authentication { get; set; }

        void Receive(NetContext context, object message);

        void Dispose(NetContext context);

        void SencCompleted(int bytes);

        void ReceiveCompleted(int bytes);

    }

    public class SesionBase : ISession
    {
        public string Name { get; set; }
        public AuthenticationType Authentication { get; set; }

        public virtual void Connected(NetContext context)
        {

        }

        public virtual void Dispose(NetContext context)
        {

        }

        public virtual void Receive(NetContext context, object message)
        {
        }

        public virtual void ReceiveCompleted(int bytes)
        {

        }

        public virtual void SencCompleted(int bytes)
        {

        }
    }
}
